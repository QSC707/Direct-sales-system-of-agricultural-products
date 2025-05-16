using FarmDirectSales.Models;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 商品评价服务接口
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// 获取产品的所有评价
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>评价列表</returns>
        Task<IEnumerable<Review>> GetProductReviewsAsync(int productId);
        
        /// <summary>
        /// 获取用户的所有评价
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>评价列表</returns>
        Task<IEnumerable<Review>> GetUserReviewsAsync(int userId);
        
        /// <summary>
        /// 获取订单的评价
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>评价信息</returns>
        Task<Review?> GetOrderReviewAsync(int orderId);
        
        /// <summary>
        /// 添加产品评价
        /// </summary>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        Task<bool> AddReviewAsync(Review review);
        
        /// <summary>
        /// 更新产品评价
        /// </summary>
        /// <param name="review">评价信息</param>
        /// <returns>操作结果</returns>
        Task<bool> UpdateReviewAsync(Review review);
        
        /// <summary>
        /// 删除产品评价
        /// </summary>
        /// <param name="reviewId">评价ID</param>
        /// <returns>操作结果</returns>
        Task<bool> DeleteReviewAsync(int reviewId);
        
        /// <summary>
        /// 计算产品平均评分
        /// </summary>
        /// <param name="productId">产品ID</param>
        /// <returns>平均评分</returns>
        Task<decimal> CalculateAverageRatingAsync(int productId);
        
        /// <summary>
        /// 检查用户是否已评价订单
        /// </summary>
        /// <param name="orderId">订单ID</param>
        /// <returns>是否已评价</returns>
        Task<bool> IsOrderReviewedAsync(int orderId);
    }
} 
 
 