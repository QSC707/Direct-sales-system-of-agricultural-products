using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Services;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 农户控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FarmerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public FarmerController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取农户信息
        /// </summary>
        [HttpGet("{farmerId}")]
        public async Task<IActionResult> GetFarmerInfo(int farmerId)
        {
            try
            {
                // 获取农户信息，包含FarmerProfile
                var farmer = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .Where(u => u.UserId == farmerId && u.Role == "farmer")
                    .FirstOrDefaultAsync();

                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户的产品数量
                var productCount = await _context.Products
                    .Where(p => p.FarmerId == farmerId)
                    .CountAsync();

                // 获取农户的订单数量
                var orderCount = await _context.Orders
                    .Include(o => o.Product)
                    .Where(o => o.Product.FarmerId == farmerId)
                    .CountAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取农户信息成功",
                    data = new
                    {
                        farmer.UserId,
                        farmer.Username,
                        farmer.Email,
                        farmer.Phone,
                        farmer.CreateTime,
                        farmer.LastLoginTime,
                        ProductCount = productCount,
                        OrderCount = orderCount,
                        // 农户资料信息
                        FarmName = farmer.FarmerProfile?.FarmName,
                        Location = farmer.FarmerProfile?.Location,
                        Description = farmer.FarmerProfile?.Description,
                        ProductCategory = farmer.FarmerProfile?.ProductCategory,
                        LicenseNumber = farmer.FarmerProfile?.LicenseNumber,
                        EstablishedDate = farmer.FarmerProfile?.EstablishedDate,
                        // 农场照片
                        FarmPhotos = new string?[]
                        {
                            farmer.FarmerProfile?.FarmPhoto1,
                            farmer.FarmerProfile?.FarmPhoto2,
                            farmer.FarmerProfile?.FarmPhoto3
                        }.Where(p => !string.IsNullOrEmpty(p)).ToArray()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有农户列表
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllFarmers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? keyword = null,
            [FromQuery] string? location = null,
            [FromQuery] string? category = null)
        {
            try
            {
                // 参数验证
                if (pageSize <= 0 || pageSize > 50)
                {
                    pageSize = 12; // 限制每页最大50条记录
                }
                if (page <= 0)
                {
                    page = 1;
                }

                Console.WriteLine($"=== GetAllFarmers 开始调试 ===");
                Console.WriteLine($"分页参数 - 页码: {page}, 每页大小: {pageSize}");
                Console.WriteLine($"筛选参数 - 关键词: '{keyword}', 位置: '{location}', 分类: '{category}'");

                // 获取所有农户，包含FarmerProfile
                var query = _context.Users
                    .Include(u => u.FarmerProfile)
                    .Where(u => u.Role == "farmer")
                    .AsQueryable();

                // 关键词搜索（用户名、农场名称、描述、位置）
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.Username.Contains(keyword) ||
                                           (u.FarmerProfile != null && u.FarmerProfile.FarmName != null && u.FarmerProfile.FarmName.Contains(keyword)) ||
                                           (u.FarmerProfile != null && u.FarmerProfile.Description != null && u.FarmerProfile.Description.Contains(keyword)) ||
                                           (u.FarmerProfile != null && u.FarmerProfile.Location != null && u.FarmerProfile.Location.Contains(keyword)));
                    Console.WriteLine($"应用关键词搜索: keyword = '{keyword}' (搜索用户名、农场名称、描述、位置)");
                }

                // 位置筛选
                if (!string.IsNullOrEmpty(location) && location != "all")
                {
                    query = query.Where(u => u.FarmerProfile != null && 
                                           u.FarmerProfile.Location != null && 
                                           u.FarmerProfile.Location.Contains(location));
                    Console.WriteLine($"应用位置筛选: location = '{location}'");
                }

                // 产品分类筛选
                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    query = query.Where(u => u.FarmerProfile != null && 
                                           u.FarmerProfile.ProductCategory != null && 
                                           u.FarmerProfile.ProductCategory.Contains(category));
                    Console.WriteLine($"应用分类筛选: category = '{category}'");
                }

                // 获取总数量（用于分页）
                var totalCount = await query.CountAsync();
                Console.WriteLine($"查询总数量: {totalCount}");

                // 应用分页并执行查询，按创建时间倒序排列
                var farmers = await query
                    .OrderByDescending(u => u.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Console.WriteLine($"查询结果: 找到 {farmers.Count} 个农户（第{page}页，每页{pageSize}条）");
                Console.WriteLine($"=== GetAllFarmers 调试结束 ===");

                return Ok(new
                {
                    code = 200,
                    message = "获取农户列表成功",
                    data = farmers.Select(f => new
                    {
                        f.UserId,
                        f.Username,
                        f.Email,
                        f.Phone,
                        f.CreateTime,
                        // 农户资料信息
                        FarmName = f.FarmerProfile?.FarmName,
                        Location = f.FarmerProfile?.Location,
                        Description = f.FarmerProfile?.Description,
                        ProductCategory = f.FarmerProfile?.ProductCategory,
                        LicenseNumber = f.FarmerProfile?.LicenseNumber,
                        EstablishedDate = f.FarmerProfile?.EstablishedDate,
                        // 农场照片
                        FarmPhotos = new string?[]
                        {
                            f.FarmerProfile?.FarmPhoto1,
                            f.FarmerProfile?.FarmPhoto2,
                            f.FarmerProfile?.FarmPhoto3
                        }.Where(p => !string.IsNullOrEmpty(p)).ToArray()
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        hasNext = page * pageSize < totalCount,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllFarmers错误: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取农户的产品列表
        /// </summary>
        [HttpGet("{farmerId}/products")]
        public async Task<IActionResult> GetFarmerProducts(
            int farmerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] string? keyword = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                // 参数验证
                if (pageSize <= 0 || pageSize > 50)
                {
                    pageSize = 12; // 限制每页最大50条记录
                }
                if (page <= 0)
                {
                    page = 1;
                }

                Console.WriteLine($"=== GetFarmerProducts 开始调试 ===");
                Console.WriteLine($"农户ID: {farmerId}, 分页参数 - 页码: {page}, 每页大小: {pageSize}");
                Console.WriteLine($"筛选参数 - 分类: '{category}', 关键词: '{keyword}', 是否上架: {isActive}");

                // 检查农户是否存在
                var farmerExists = await _context.Users
                    .AnyAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (!farmerExists)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户的产品
                var query = _context.Products
                    .Where(p => p.FarmerId == farmerId)
                    .AsQueryable();

                // 分类筛选
                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    query = query.Where(p => p.Category == category);
                    Console.WriteLine($"应用分类筛选: category = '{category}'");
                }

                // 关键词搜索
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(p => p.ProductName.Contains(keyword) || 
                                           p.Description.Contains(keyword));
                    Console.WriteLine($"应用关键词搜索: keyword = '{keyword}'");
                }

                // 上架状态筛选
                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                    Console.WriteLine($"应用状态筛选: isActive = {isActive.Value}");
                }

                // 获取总数量（用于分页）
                var totalCount = await query.CountAsync();
                Console.WriteLine($"查询总数量: {totalCount}");

                // 应用分页并执行查询
                var products = await query
                    .OrderByDescending(p => p.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Console.WriteLine($"查询结果: 找到 {products.Count} 个产品（第{page}页，每页{pageSize}条）");
                Console.WriteLine($"=== GetFarmerProducts 调试结束 ===");

                return Ok(new
                {
                    code = 200,
                    message = "获取农户产品列表成功",
                    data = products.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Stock,
                        p.ImageUrl,
                        p.Category,
                        p.IsActive,
                        p.CreateTime,
                        p.UpdateTime
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        hasNext = page * pageSize < totalCount,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFarmerProducts错误: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取农户的订单列表
        /// </summary>
        [HttpGet("{farmerId}/orders")]
        [Authorize]
        public async Task<IActionResult> GetFarmerOrders(int farmerId)
        {
            try
            {
                // 验证当前用户是否有权限查看此农户的订单
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != farmerId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限查看此农户的订单" });
                }

                // 检查农户是否存在
                var farmerExists = await _context.Users
                    .AnyAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (!farmerExists)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户的订单
                var orders = await _context.Orders
                    .Include(o => o.Product)
                    .Include(o => o.User)
                    .Where(o => o.Product.FarmerId == farmerId)
                    .OrderByDescending(o => o.CreateTime)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取农户订单列表成功",
                    data = orders.Select(o => new
                    {
                        o.OrderId,
                        o.Status,
                        o.TotalPrice,
                        o.Quantity,
                        o.CreateTime,
                        o.PayTime,
                        o.ShipTime,
                        o.CompleteTime,
                        Product = new
                        {
                            o.Product.ProductId,
                            o.Product.ProductName,
                            o.Product.Price
                        },
                        User = new
                        {
                            o.User.UserId,
                            o.User.Username
                        }
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }
} 