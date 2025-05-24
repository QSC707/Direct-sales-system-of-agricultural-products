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
        public async Task<IActionResult> GetAllFarmers()
        {
            try
            {
                // 获取所有农户，包含FarmerProfile
                var farmers = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .Where(u => u.Role == "farmer")
                    .ToListAsync();

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
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取农户的产品列表
        /// </summary>
        [HttpGet("{farmerId}/products")]
        public async Task<IActionResult> GetFarmerProducts(int farmerId)
        {
            try
            {
                // 检查农户是否存在
                var farmerExists = await _context.Users
                    .AnyAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (!farmerExists)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户的产品
                var products = await _context.Products
                    .Where(p => p.FarmerId == farmerId)
                    .OrderByDescending(p => p.CreateTime)
                    .ToListAsync();

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
                        p.CreateTime,
                        p.UpdateTime
                    })
                });
            }
            catch (Exception ex)
            {
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