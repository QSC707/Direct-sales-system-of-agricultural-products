using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 购物车控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取用户购物车
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserCart(int userId)
        {
            try
            {
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreateTime)
                    .ToListAsync();

                decimal totalPrice = cartItems.Sum(c => c.Product.Price * c.Quantity);

                return Ok(new
                {
                    code = 200,
                    message = "获取购物车成功",
                    data = new
                    {
                        items = cartItems.Select(c => new
                        {
                            cartItemId = c.CartItemId,
                            userId = c.UserId,
                            product = new
                            {
                                productId = c.Product.ProductId,
                                productName = c.Product.ProductName,
                                description = c.Product.Description,
                                price = c.Product.Price,
                                stock = c.Product.Stock,
                                imageUrl = c.Product.ImageUrl
                            },
                            quantity = c.Quantity,
                            itemTotal = c.Product.Price * c.Quantity,
                            createTime = c.CreateTime,
                            updateTime = c.UpdateTime
                        }),
                        totalPrice
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加商品到购物车
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // 检查用户是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 检查产品是否存在
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.IsActive);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                // 检查库存是否足够
                if (product.Stock < request.Quantity)
                {
                    return BadRequest(new { code = 400, message = "库存不足" });
                }

                // 检查是否已经在购物车中
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == request.UserId && c.ProductId == request.ProductId);

                if (existingItem != null)
                {
                    // 如果已存在，则更新数量
                    existingItem.Quantity += request.Quantity;
                    existingItem.UpdateTime = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        code = 200,
                        message = "购物车商品数量已更新",
                        data = new
                        {
                            existingItem.CartItemId,
                            existingItem.UserId,
                            existingItem.ProductId,
                            existingItem.Quantity,
                            existingItem.CreateTime,
                            existingItem.UpdateTime
                        }
                    });
                }

                // 如果不存在，创建新的购物车项
                var cartItem = new CartItem
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加到购物车成功",
                    data = new
                    {
                        cartItem.CartItemId,
                        cartItem.UserId,
                        cartItem.ProductId,
                        cartItem.Quantity,
                        cartItem.CreateTime,
                        cartItem.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新购物车项数量
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.CartItemId == id);

                if (cartItem == null)
                {
                    return NotFound(new { code = 404, message = "购物车项不存在" });
                }

                // 检查是否是该用户的购物车项
                if (cartItem.UserId != request.UserId)
                {
                    return BadRequest(new { code = 400, message = "只能修改自己的购物车" });
                }

                // 检查库存是否足够
                if (cartItem.Product.Stock < request.Quantity)
                {
                    return BadRequest(new { code = 400, message = "库存不足" });
                }

                // 更新数量
                cartItem.Quantity = request.Quantity;
                cartItem.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新购物车成功",
                    data = new
                    {
                        cartItem.CartItemId,
                        cartItem.UserId,
                        cartItem.ProductId,
                        cartItem.Quantity,
                        cartItem.CreateTime,
                        cartItem.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除购物车项
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id, [FromBody] DeleteCartItemRequest request)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(id);
                if (cartItem == null)
                {
                    return NotFound(new { code = 404, message = "购物车项不存在" });
                }

                // 检查是否是该用户的购物车项
                if (cartItem.UserId != request.UserId)
                {
                    return BadRequest(new { code = 400, message = "只能删除自己的购物车项" });
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "删除购物车项成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 清空购物车
        /// </summary>
        [HttpDelete("clear/{userId}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    return Ok(new { code = 200, message = "购物车已为空" });
                }

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Ok(new { code = 200, message = "清空购物车成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 添加到购物车请求模型
    /// </summary>
    public class AddToCartRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        [Required(ErrorMessage = "产品ID不能为空")]
        public int ProductId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(1, int.MaxValue, ErrorMessage = "数量必须大于0")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 更新购物车项请求模型
    /// </summary>
    public class UpdateCartItemRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [Required(ErrorMessage = "数量不能为空")]
        [Range(1, int.MaxValue, ErrorMessage = "数量必须大于0")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 删除购物车项请求模型
    /// </summary>
    public class DeleteCartItemRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public int UserId { get; set; }
    }
} 
 
 