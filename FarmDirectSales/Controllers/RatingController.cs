using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Models;
using FarmDirectSales.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 评分管理控制器
    /// </summary>
    [ApiController]
    [Route("api/rating")]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 提交订单评分
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitRating([FromBody] RatingRequest request)
        {
            try
            {
                Console.WriteLine($"提交订单评分：订单ID={request.OrderId}, 用户ID={request.UserId}, 评分={request.Rating}");
                
                // 验证评分范围
                if (request.Rating < 1 || request.Rating > 5)
                {
                    return BadRequest(new { code = 400, message = "评分必须在1-5之间" });
                }
                
                // 查询订单
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);
                
                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }
                
                // 验证订单所属用户
                if (order.UserId != request.UserId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限评价此订单" });
                }
                
                // 验证订单状态
                if (order.Status != "已完成")
                {
                    return BadRequest(new { code = 400, message = "只能评价已完成的订单" });
                }
                
                // 检查是否已经评分
                if (order.IsRated)
                {
                    return BadRequest(new { code = 400, message = "此订单已评分，不能重复评分" });
                }
                
                // 更新订单评分信息
                order.Rating = request.Rating;
                order.IsRated = true;
                order.RateTime = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                return Ok(new { code = 200, message = "评分成功" });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"评分失败: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { code = 500, message = "服务器内部错误" });
            }
        }
        
        /// <summary>
        /// 获取订单评分
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderRating(int orderId)
        {
            try
            {
                // 查询订单评分
                var order = await _context.Orders
                    .Where(o => o.OrderId == orderId)
                    .Select(o => new { o.Rating, o.IsRated, o.RateTime })
                    .FirstOrDefaultAsync();
                
                if (order == null)
                {
                    return NotFound(new { code = 404, message = "订单不存在" });
                }
                
                return Ok(new { code = 200, data = order });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取订单评分失败: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { code = 500, message = "服务器内部错误" });
            }
        }
        
        /// <summary>
        /// 获取产品评分统计
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductRatings(int productId)
        {
            try
            {
                // 查询产品的所有评分
                var ratings = await _context.Orders
                    .Where(o => o.ProductId == productId && o.IsRated)
                    .Select(o => new { o.Rating, o.RateTime })
                    .ToListAsync();
                
                // 计算平均评分
                double averageRating = 0;
                if (ratings.Count > 0)
                {
                    // 使用显式类型转换处理可空类型
                    averageRating = ratings.Average(r => r.Rating.HasValue ? (double)r.Rating.Value : 0);
                }
                
                // 统计各星级数量
                var ratingStats = new int[6]; // 索引0不使用，1-5表示对应星级数量
                foreach (var rating in ratings)
                {
                    if (rating.Rating.HasValue)
                    {
                        ratingStats[rating.Rating.Value]++;
                    }
                }
                
                return Ok(new { 
                    code = 200, 
                    data = new { 
                        totalCount = ratings.Count, 
                        averageRating = Math.Round(averageRating, 1),
                        ratingStats = new {
                            star1 = ratingStats[1],
                            star2 = ratingStats[2],
                            star3 = ratingStats[3],
                            star4 = ratingStats[4],
                            star5 = ratingStats[5]
                        }
                    } 
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取产品评分统计失败: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { code = 500, message = "服务器内部错误" });
            }
        }
    }

    /// <summary>
    /// 评分请求参数
    /// </summary>
    public class RatingRequest
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required]
        public int OrderId { get; set; }
        
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public int UserId { get; set; }
        
        /// <summary>
        /// 评分(1-5)
        /// </summary>
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
    }
} 