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
    [Route("api/[controller]")]
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
        /// 获取销售总览数据
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>销售总览数据</returns>
        [HttpPost("overview")]
        public async Task<IActionResult> GetSalesOverview([FromBody] SalesFilter filter)
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
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Status == "completed" && !o.IsDeleted);
                
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
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 计算统计数据
                var overview = new SalesOverview
                {
                    TotalOrders = orders.Count,
                    TotalSales = orders.Sum(o => o.TotalPrice),
                    TotalQuantity = orders.Sum(o => o.Quantity),
                    AverageOrderValue = orders.Count > 0 ? orders.Sum(o => o.TotalPrice) / orders.Count : 0
                };
                
                // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取销售总览",
                    description: $"获取销售总览数据: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "所有时间"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
                
                return Ok(new { code = 200, message = "获取销售总览成功", data = overview });
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
        /// <returns>每日销售趋势数据</returns>
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
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Status == "completed" && !o.IsDeleted);
                
                // 应用日期筛选
                query = query.Where(o => o.CreateTime >= filter.StartDate.Value && 
                                       o.CreateTime < filter.EndDate.Value.AddDays(1));
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 按日期分组计算每日销售数据
                var dailyTrends = orders
                    .GroupBy(o => o.CreateTime.Date)
                    .Select(g => new SalesTrend
                    {
                        Period = g.Key.ToString("MM-dd"),
                        Sales = g.Sum(o => o.TotalPrice),
                        Orders = g.Count()
                    })
                    .OrderBy(t => DateTime.ParseExact(t.Period, "MM-dd", null))
                    .ToList();
                
                // 补充无数据的日期
                var result = new List<SalesTrend>();
                var currentDate = filter.StartDate.Value.Date;
                var endDate = filter.EndDate.Value.Date;
                
                while (currentDate <= endDate)
                {
                    var period = currentDate.ToString("MM-dd");
                    var existingData = dailyTrends.FirstOrDefault(t => t.Period == period);
                    
                    if (existingData != null)
                    {
                        result.Add(existingData);
                    }
                    else
                    {
                        result.Add(new SalesTrend
                        {
                            Period = period,
                            Sales = 0,
                            Orders = 0
                        });
                    }
                    
                    currentDate = currentDate.AddDays(1);
                }
                
                // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取销售趋势",
                    description: $"获取每日销售趋势数据: {filter.StartDate?.ToString("yyyy-MM-dd")} - {filter.EndDate?.ToString("yyyy-MM-dd")}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
                
                return Ok(new { code = 200, message = "获取销售趋势成功", data = result });
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
        /// <returns>每月销售趋势数据</returns>
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
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Status == "completed" && !o.IsDeleted);
                
                // 应用日期筛选
                query = query.Where(o => o.CreateTime >= filter.StartDate.Value && 
                                      o.CreateTime < filter.EndDate.Value.AddDays(1));
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 按月份分组计算销售数据
                var monthlyTrends = orders
                    .GroupBy(o => new { Year = o.CreateTime.Year, Month = o.CreateTime.Month })
                    .Select(g => new SalesTrend
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Sales = g.Sum(o => o.TotalPrice),
                        Orders = g.Count()
                    })
                    .OrderBy(t => t.Period)
                    .ToList();
                
                // 记录日志
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取每月销售趋势",
                    description: $"获取每月销售趋势数据: {filter.StartDate?.ToString("yyyy-MM")} - {filter.EndDate?.ToString("yyyy-MM")}",
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
                
                // 查询符合条件的订单
                var query = _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Status == "completed" && !o.IsDeleted);
                
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
                
                // 如果指定了农户ID，则只查询该农户的产品订单
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product != null && o.Product.FarmerId == filter.FarmerId.Value);
                }
                
                // 获取订单数据
                var orders = await query.ToListAsync();
                
                // 按产品分组计算销售排名
                var topProducts = orders
                    .GroupBy(o => new { o.ProductId, ProductName = o.Product?.ProductName ?? "未知产品" })
                    .Select(g => new ProductSalesRank
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        Sales = g.Sum(o => o.TotalPrice),
                        Quantity = g.Sum(o => o.Quantity),
                        Category = g.FirstOrDefault()?.Product?.Category ?? "未分类"
                    })
                    .OrderByDescending(p => p.Sales)
                    .Take(top)
                    .ToList();
                
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
                    .Where(o => o.Status == "completed" && !o.IsDeleted);
                
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
    }
} 
 
 