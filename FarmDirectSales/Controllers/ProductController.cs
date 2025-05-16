using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 产品管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有产品
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Farmer)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取产品列表成功",
                    data = products.Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.Description,
                        p.Price,
                        p.Stock,
                        p.ImageUrl,
                        Farmer = new { p.Farmer.UserId, p.Farmer.Username },
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
        /// 获取产品详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Farmer)
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取产品详情成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        Farmer = new { product.Farmer.UserId, product.Farmer.Username },
                        product.CreateTime,
                        product.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加产品
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] AddProductRequest request)
        {
            try
            {
                // 检查用户是否存在且是农户
                var farmer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.FarmerId);
                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                if (farmer.Role != "farmer")
                {
                    return BadRequest(new { code = 400, message = "只有农户才能添加产品" });
                }

                var product = new Product
                {
                    ProductName = request.ProductName,
                    Description = request.Description,
                    Price = request.Price,
                    Stock = request.Stock,
                    ImageUrl = request.ImageUrl,
                    FarmerId = request.FarmerId,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加产品成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.FarmerId,
                        product.CreateTime,
                        product.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新产品
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能更新产品" });
                }

                // 更新产品信息
                product.ProductName = request.ProductName ?? product.ProductName;
                product.Description = request.Description ?? product.Description;
                product.Price = request.Price ?? product.Price;
                product.Stock = request.Stock ?? product.Stock;
                product.ImageUrl = request.ImageUrl ?? product.ImageUrl;
                product.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新产品成功",
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.Description,
                        product.Price,
                        product.Stock,
                        product.ImageUrl,
                        product.FarmerId,
                        product.CreateTime,
                        product.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除产品（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id, [FromBody] DeleteProductRequest request)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查是否是产品所有者
                if (product.FarmerId != request.FarmerId)
                {
                    return BadRequest(new { code = 400, message = "只有产品所有者才能删除产品" });
                }

                // 软删除
                product.IsActive = false;
                product.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "删除产品成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 添加产品请求模型
    /// </summary>
    public class AddProductRequest
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        [Required(ErrorMessage = "产品名称不能为空")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        [Required(ErrorMessage = "产品描述不能为空")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 产品价格
        /// </summary>
        [Required(ErrorMessage = "产品价格不能为空")]
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Required(ErrorMessage = "库存数量不能为空")]
        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int Stock { get; set; }

        /// <summary>
        /// 产品图片URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
    }

    /// <summary>
    /// 更新产品请求模型
    /// </summary>
    public class UpdateProductRequest
    {
        /// <summary>
        /// 产品名称
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// 产品描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 产品价格
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "价格必须大于0")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "库存不能为负数")]
        public int? Stock { get; set; }

        /// <summary>
        /// 产品图片URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
    }

    /// <summary>
    /// 删除产品请求模型
    /// </summary>
    public class DeleteProductRequest
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        [Required(ErrorMessage = "农户ID不能为空")]
        public int FarmerId { get; set; }
    }
} 
 
 