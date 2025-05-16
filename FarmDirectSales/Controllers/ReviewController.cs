using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FarmDirectSales.Models;
using FarmDirectSales.Services;
using System.Security.Claims;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 商品评价控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogService _logService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="reviewService">评价服务</param>
        /// <param name="logService">日志服务</param>
        public ReviewController(IReviewService reviewService, ILogService logService)
        {
            _reviewService = reviewService;
            _logService = logService;
        }
        
        /// <summary>
        /// 获取产品的所有评价
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>评价列表</returns>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId);
            return Ok(reviews);
        }
        
        /// <summary>
        /// 获取用户的所有评价
        /// </summary>
        /// <returns>评价列表</returns>
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserReviews()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            var reviews = await _reviewService.GetUserReviewsAsync(userId);
            return Ok(reviews);
        }
        
        /// <summary>
        /// 获取订单的评价
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>评价信息</returns>
        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderReview(int orderId)
        {
            var review = await _reviewService.GetOrderReviewAsync(orderId);
            if (review == null)
            {
                return NotFound();
            }
            
            return Ok(review);
        }
        
        /// <summary>
        /// 添加产品评价
        /// </summary>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(Review review)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            // 设置用户ID
            review.UserId = userId;
            review.CreateTime = DateTime.Now;
            
            // 检查订单是否已评价
            if (review.OrderId.HasValue)
            {
                var isReviewed = await _reviewService.IsOrderReviewedAsync(review.OrderId.Value);
                if (isReviewed)
                {
                    return BadRequest("该订单已评价");
                }
            }
            
            var result = await _reviewService.AddReviewAsync(review);
            if (!result)
            {
                return BadRequest("添加评价失败");
            }
            
            // 记录日志
            await _logService.LogAction(
                userId: userId,
                actionType: "添加评价",
                description: $"添加了对产品 {review.ProductId} 的评价",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
            );
            
            return Ok("评价成功");
        }
        
        /// <summary>
        /// 更新产品评价
        /// </summary>
        /// <param name="id">评价ID</param>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(int id, Review review)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            // 设置评价ID
            review.ReviewId = id;
            
            // 检查评价所有者
            var existingReview = await _reviewService.GetOrderReviewAsync(review.OrderId ?? 0);
            if (existingReview == null || existingReview.UserId != userId)
            {
                return Forbid();
            }
            
            var result = await _reviewService.UpdateReviewAsync(review);
            if (!result)
            {
                return BadRequest("更新评价失败");
            }
            
            // 记录日志
            await _logService.LogAction(
                userId: userId,
                actionType: "更新评价",
                description: $"更新了对产品 {review.ProductId} 的评价",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
            );
            
            return Ok("更新成功");
        }
        
        /// <summary>
        /// 删除产品评价
        /// </summary>
        /// <param name="id">评价ID</param>
        /// <returns>操作结果</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }
            
            // 检查评价所有者或管理员权限
            var existingReview = await _reviewService.GetOrderReviewAsync(id);
            var isAdmin = User.IsInRole("admin");
            
            if (existingReview == null || (existingReview.UserId != userId && !isAdmin))
            {
                return Forbid();
            }
            
            var result = await _reviewService.DeleteReviewAsync(id);
            if (!result)
            {
                return BadRequest("删除评价失败");
            }
            
            // 记录日志
            await _logService.LogAction(
                userId: userId,
                actionType: "删除评价",
                description: $"删除了评价 {id}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
            );
            
            return Ok("删除成功");
        }
        
        /// <summary>
        /// 计算产品平均评分
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>平均评分</returns>
        [HttpGet("rating/{productId}")]
        public async Task<IActionResult> GetProductRating(int productId)
        {
            var averageRating = await _reviewService.CalculateAverageRatingAsync(productId);
            return Ok(new { AverageRating = averageRating });
        }
    }
} 
 
 