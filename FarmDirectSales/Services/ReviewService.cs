using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 商品评价服务实现
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// 获取产品的所有评价
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>评价列表</returns>
        public async Task<IEnumerable<Review>> GetProductReviewsAsync(int productId)
        {
            return await _context.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }
        
        /// <summary>
        /// 获取用户的所有评价
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>评价列表</returns>
        public async Task<IEnumerable<Review>> GetUserReviewsAsync(int userId)
        {
            return await _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();
        }
        
        /// <summary>
        /// 获取订单的评价
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>评价信息</returns>
        public async Task<Review?> GetOrderReviewAsync(int orderId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }
        
        /// <summary>
        /// 添加产品评价
        /// </summary>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        public async Task<bool> AddReviewAsync(Review review)
        {
            try
            {
                // 检查订单是否存在
                var order = await _context.Orders.FindAsync(review.OrderId);
                if (order == null)
                {
                    return false;
                }
                
                // 检查订单是否已评价
                if (await IsOrderReviewedAsync(order.OrderId))
                {
                    return false;
                }
                
                // 添加评价
                _context.Reviews.Add(review);
                
                // 更新订单评价状态
                order.IsReviewed = true;
                _context.Orders.Update(order);
                
                // 更新产品评分
                await UpdateProductRatingAsync(review.ProductId);
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 更新产品评价
        /// </summary>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        public async Task<bool> UpdateReviewAsync(Review review)
        {
            try
            {
                var existingReview = await _context.Reviews.FindAsync(review.ReviewId);
                if (existingReview == null)
                {
                    return false;
                }
                
                // 更新评价内容
                existingReview.Rating = review.Rating;
                existingReview.Content = review.Content;
                existingReview.IsAnonymous = review.IsAnonymous;
                
                _context.Reviews.Update(existingReview);
                
                // 更新产品评分
                await UpdateProductRatingAsync(existingReview.ProductId);
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 删除产品评价
        /// </summary>
        /// <param name="reviewId">评价ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null)
                {
                    return false;
                }
                
                int productId = review.ProductId;
                
                // 获取关联的订单
                if (review.OrderId.HasValue)
                {
                    var order = await _context.Orders.FindAsync(review.OrderId.Value);
                    if (order != null)
                    {
                        order.IsReviewed = false;
                        _context.Orders.Update(order);
                    }
                }
                
                _context.Reviews.Remove(review);
                
                // 更新产品评分
                await UpdateProductRatingAsync(productId);
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 计算产品平均评分
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>平均评分</returns>
        public async Task<decimal> CalculateAverageRatingAsync(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .ToListAsync();
            
            if (reviews.Count == 0)
            {
                return 0;
            }
            
            return (decimal)reviews.Average(r => r.Rating);
        }
        
        /// <summary>
        /// 检查用户是否已评价订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>是否已评价</returns>
        public async Task<bool> IsOrderReviewedAsync(int orderId)
        {
            return await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
        }
        
        /// <summary>
        /// 更新产品评分
        /// </summary>
        /// <param name="productId">产品ID</param>
        private async Task UpdateProductRatingAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var averageRating = await CalculateAverageRatingAsync(productId);
                var reviewCount = await _context.Reviews.CountAsync(r => r.ProductId == productId);
                
                product.AverageRating = averageRating;
                product.ReviewCount = reviewCount;
                
                _context.Products.Update(product);
            }
        }
    }
} 
 
 