using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FarmDirectSales.Models;
using FarmDirectSales.Services;
using FarmDirectSales.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 销售统计分析控制器
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "admin,farmer")]
    public class StatisticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogService _logService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="logService">日志服务</param>
        public StatisticsController(ApplicationDbContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }
        
        /// <summary>
        /// 获取销售总览
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>销售总览数据</returns>
        [HttpPost("overview")]
        public async Task<IActionResult> GetSalesOverview([FromBody] SalesFilter filter)
        {
            try
        {
            int userId = 0;
                bool isAdmin = User.IsInRole("admin");
                
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                        return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                filter.FarmerId = userId;
            }
            
                // 构建基础查询，只筛选已完成的订单
                var baseQuery = _context.Orders
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
                
                // 应用日期筛选
                if (filter.StartDate.HasValue)
                {
                    baseQuery = baseQuery.Where(o => o.CreateTime >= filter.StartDate.Value);
                }
                
                if (filter.EndDate.HasValue)
                {
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    baseQuery = baseQuery.Where(o => o.CreateTime < endDatePlusOne);
                }
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    baseQuery = baseQuery.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 使用数据库级别的聚合查询，避免加载所有数据到内存
                var overview = await baseQuery
                    .GroupBy(o => 1) // 虚拟分组以便进行聚合
                    .Select(g => new
                    {
                        TotalOrders = g.Count(),
                        TotalSales = g.Sum(o => o.TotalPrice),
                        TotalQuantity = g.Sum(o => o.Quantity),
                        AverageOrderValue = g.Average(o => o.TotalPrice)
                    })
                    .FirstOrDefaultAsync();
                        
                // 如果没有数据，返回默认值
                if (overview == null)
                {
                    overview = new
                {
                        TotalOrders = 0,
                        TotalSales = 0m,
                        TotalQuantity = 0,
                        AverageOrderValue = 0m
                };
                }
            
            // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取销售总览",
                    description: $"查询时间范围: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "所有时间"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            
                return Ok(new { 
                    code = 200, 
                    message = "获取销售总览成功", 
                    data = new
                    {
                        totalOrders = overview.TotalOrders,
                        totalSales = overview.TotalSales,
                        totalQuantity = overview.TotalQuantity,
                        averageOrderValue = overview.AverageOrderValue,
                        showShippingFees = false // 农户查看时不显示运费
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取每日销售趋势
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每日销售趋势</returns>
        [HttpPost("trend/daily")]
        public async Task<IActionResult> GetDailySalesTrend([FromBody] SalesFilter filter)
        {
            try
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                        return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                filter.FarmerId = userId;
            }
            
                // 确保日期范围有效
                if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
                {
                    return BadRequest(new { code = 400, message = "开始日期和结束日期不能为空" });
                }
                
                // 构建基础查询
                var query = _context.Orders
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
                
                // 应用日期筛选
                query = query.Where(o => o.CreateTime >= filter.StartDate.Value && 
                                       o.CreateTime < filter.EndDate.Value.AddDays(1));
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 先获取原始分组数据（不进行字符串格式化）
                var rawDailyTrends = await query
                    .GroupBy(o => new { 
                        Year = o.CreateTime.Year,
                        Month = o.CreateTime.Month,
                        Day = o.CreateTime.Day
                    })
                    .Select(g => new 
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Sales = g.Sum(o => o.TotalPrice),
                        Orders = g.Count()
                    })
                    .ToListAsync();
                
                // 在内存中进行格式化和排序
                var dailyTrends = rawDailyTrends
                    .Select(x => new SalesTrend
                        {
                        Period = $"{x.Month:D2}-{x.Day:D2}",
                        Sales = x.Sales,
                        Orders = x.Orders
                    })
                    .OrderBy(t => t.Period)
                    .ToList();
            
            // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取每日销售趋势",
                    description: $"查询时间范围: {filter.StartDate?.ToString("yyyy-MM-dd")} - {filter.EndDate?.ToString("yyyy-MM-dd")}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            
                return Ok(new { code = 200, message = "获取每日销售趋势成功", data = dailyTrends });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取每月销售趋势
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每月销售趋势</returns>
        [HttpPost("trend/monthly")]
        public async Task<IActionResult> GetMonthlySalesTrend([FromBody] SalesFilter filter)
        {
            try
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                        return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                filter.FarmerId = userId;
            }
            
                // 确保日期范围有效
                if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
                {
                    return BadRequest(new { code = 400, message = "开始日期和结束日期不能为空" });
                }
                
                // 构建基础查询
                var query = _context.Orders
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
                
                // 应用日期筛选
                query = query.Where(o => o.CreateTime >= filter.StartDate.Value && 
                                      o.CreateTime < filter.EndDate.Value.AddDays(1));
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 先获取原始分组数据（不进行字符串格式化）
                var rawMonthlyTrends = await query
                    .GroupBy(o => new { 
                        Year = o.CreateTime.Year, 
                        Month = o.CreateTime.Month 
                    })
                    .Select(g => new 
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Sales = g.Sum(o => o.TotalPrice),
                        Orders = g.Count()
                    })
                    .ToListAsync();
                
                // 在内存中进行格式化和排序
                var monthlyTrends = rawMonthlyTrends
                    .Select(x => new SalesTrend
                    {
                        Period = $"{x.Year}-{x.Month:D2}",
                        Sales = x.Sales,
                        Orders = x.Orders
                    })
                    .OrderBy(t => t.Period)
                    .ToList();
            
            // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取每月销售趋势",
                    description: $"查询时间范围: {filter.StartDate?.ToString("yyyy-MM")} - {filter.EndDate?.ToString("yyyy-MM")}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            
                return Ok(new { code = 200, message = "获取每月销售趋势成功", data = monthlyTrends });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取产品销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>产品销售排名</returns>
        [HttpPost("products/top/{top=10}")]
        public async Task<IActionResult> GetTopProducts([FromBody] SalesFilter filter, int top = 10)
        {
            try
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                        return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                filter.FarmerId = userId;
            }
            
                // 构建基础查询，使用数据库级别的聚合
                var query = _context.Orders
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
                
                // 应用日期筛选
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreateTime >= filter.StartDate.Value);
                }
                
                if (filter.EndDate.HasValue)
                {
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.CreateTime < endDatePlusOne);
                }
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 在数据库级别进行分组和排序，只选择需要的字段
                var topProducts = await query
                    .GroupBy(o => new { o.ProductId, o.Product.ProductName, o.Product.Category })
                    .Select(g => new ProductSalesRank
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName ?? "未知产品",
                        Category = g.Key.Category ?? "未分类",
                        Sales = g.Sum(o => o.TotalPrice),
                        Quantity = g.Sum(o => o.Quantity)
                    })
                    .OrderByDescending(p => p.Sales)
                    .Take(top)
                    .ToListAsync();
            
            // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取产品销售排名",
                    description: $"获取产品销售排名数据(Top {top})",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            
                return Ok(new { code = 200, message = "获取产品销售排名成功", data = topProducts });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取农户销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>农户销售排名</returns>
        [HttpPost("farmers/top/{top=10}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetTopFarmers([FromBody] SalesFilter filter, int top = 10)
        {
            try
            {
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    int.TryParse(userIdClaim.Value, out userId);
                }
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .ThenInclude(p => p.Farmer)
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
            
                // 应用日期筛选
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreateTime >= filter.StartDate.Value);
                }
                
                if (filter.EndDate.HasValue)
                {
                    // 包含结束日期的整天
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.CreateTime < endDatePlusOne);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 按农户分组计算销售排名
                var topFarmers = orders
                    .Where(o => o.Product?.Farmer != null)
                    .GroupBy(o => new { 
                        FarmerId = o.Product.FarmerId, 
                        FarmerName = o.Product.Farmer.Username 
                    })
                    .Select(g => new FarmerSalesRank
                    {
                        FarmerId = g.Key.FarmerId,
                        FarmerName = g.Key.FarmerName,
                        Sales = g.Sum(o => o.TotalPrice),
                        Quantity = g.Sum(o => o.Quantity),
                        ProductCount = g.Select(o => o.ProductId).Distinct().Count()
                    })
                    .OrderByDescending(f => f.Sales)
                    .Take(top)
                    .ToList();
            
            // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取农户销售排名",
                    description: $"获取农户销售排名数据(Top {top})",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
                
                return Ok(new { code = 200, message = "获取农户销售排名成功", data = topFarmers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取农户商品销量详情
        /// </summary>
        /// <param name="farmerId">农户ID，如果不提供则使用当前登录用户ID</param>
        /// <param name="filter">筛选条件</param>
        /// <returns>农户所有商品的销量统计</returns>
        [HttpPost("products/details")]
        public async Task<IActionResult> GetProductSalesDetails([FromQuery] int? farmerId, [FromBody] SalesFilter filter)
        {
            try
            {
                // 验证权限
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    // 非管理员只能查看自己的数据
                    if (!User.IsInRole("admin") && farmerId.HasValue && farmerId.Value != userId)
                    {
                        return Unauthorized(new { code = 401, message = "只能查看自己的商品销售统计" });
                    }
                }
                else
                {
                    return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                // 设置农户ID
                if (!farmerId.HasValue && User.IsInRole("farmer"))
                {
                    farmerId = userId;
                }
                
                // 确保有农户ID
                if (!farmerId.HasValue)
                {
                    return BadRequest(new { code = 400, message = "必须提供农户ID" });
                }
                
                filter.FarmerId = farmerId;
                
                // 构建基础查询
                var query = _context.Orders
                    .Where(o => o.Status == "已完成" && !o.IsDeleted);
                
                // 应用日期筛选
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreateTime >= filter.StartDate.Value);
                }
                
                if (filter.EndDate.HasValue)
                {
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.CreateTime < endDatePlusOne);
                }
                
                // 只查询该农户的产品订单
                query = query.Where(o => o.Product != null && o.Product.FarmerId == farmerId.Value);
                
                // 在数据库级别进行分组计算，避免加载大量数据到内存
                var productDetails = await query
                    .GroupBy(o => new { 
                        o.ProductId, 
                        o.Product.ProductName, 
                        o.Product.Category,
                        o.Product.Price,
                        o.Product.IsActive
                    })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName ?? "未知商品",
                        Category = g.Key.Category ?? "未分类",
                        TotalQuantity = g.Sum(o => o.Quantity),
                        TotalSales = g.Sum(o => o.TotalPrice),
                        AveragePrice = g.Sum(o => o.TotalPrice) / g.Sum(o => o.Quantity),
                        OrderCount = g.Count(),
                        CurrentPrice = g.Key.Price,
                        LastSoldDate = g.Max(o => o.CreateTime),
                        IsActive = g.Key.IsActive
                    })
                    .OrderByDescending(p => p.TotalSales)
                    .ToListAsync();
                
                // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取商品销量详情",
                    description: $"农户ID: {farmerId}，查询时间范围: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "所有时间"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
                
                return Ok(new { code = 200, message = "获取商品销量详情成功", data = productDetails });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取商品价格分析数据
        /// </summary>
        /// <param name="productId">商品ID</param>
        /// <param name="filter">筛选条件</param>
        /// <returns>商品价格与销量分析数据</returns>
        [HttpPost("price-analysis/{productId}")]
        public async Task<IActionResult> GetPriceAnalysis(int productId, [FromBody] SalesFilter filter)
        {
            try
            {
                // 验证权限
                int userId = 0;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    // 非管理员只能查看自己的数据
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                    if (product == null)
                    {
                        return NotFound(new { code = 404, message = "商品不存在" });
                    }
                    
                    if (!User.IsInRole("admin") && User.IsInRole("farmer") && product.FarmerId != userId)
                    {
                        return Unauthorized(new { code = 401, message = "只能查看自己的商品价格分析" });
                    }
                }
                else
                {
                    return Unauthorized(new { code = 401, message = "未授权访问" });
                }
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Status == "已完成" && !o.IsDeleted && o.ProductId == productId);
                
                // 应用日期筛选
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreateTime >= filter.StartDate.Value);
                }
                
                if (filter.EndDate.HasValue)
                {
                    var endDatePlusOne = filter.EndDate.Value.AddDays(1);
                    query = query.Where(o => o.CreateTime < endDatePlusOne);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 按月份分组计算价格和销量
                var priceAnalysis = orders
                    .GroupBy(o => new { 
                        Year = o.CreateTime.Year, 
                        Month = o.CreateTime.Month 
                    })
                    .Select(g => new
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        AveragePrice = g.Average(o => o.TotalPrice / o.Quantity),
                        TotalQuantity = g.Sum(o => o.Quantity),
                        TotalSales = g.Sum(o => o.TotalPrice),
                        OrderCount = g.Count()
                    })
                    .OrderBy(p => p.Period)
                    .ToList();
                
                // 获取当前商品信息
                var productInfo = await _context.Products
                    .Where(p => p.ProductId == productId)
                    .Select(p => new
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Category = p.Category,
                        CurrentPrice = p.Price,
                        IsOrganic = p.IsOrganic,
                        IsActive = p.IsActive
                    })
                    .FirstOrDefaultAsync();
                
                // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取商品价格分析",
                    description: $"商品ID: {productId}，查询时间范围: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "所有时间"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
                
                return Ok(new { 
                    code = 200, 
                    message = "获取商品价格分析成功", 
                    data = new { 
                        product = productInfo,
                        analysis = priceAnalysis
                    } 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }
} 
 
 