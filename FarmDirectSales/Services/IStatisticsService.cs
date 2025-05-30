using FarmDirectSales.Models;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 销售统计服务接口
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// 获取销售总览数据
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>销售总览数据</returns>
        Task<SalesOverview> GetSalesOverviewAsync(SalesFilter filter);
        
        /// <summary>
        /// 获取销售趋势数据（日）
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每日销售趋势数据</returns>
        Task<IEnumerable<SalesTrend>> GetDailySalesTrendAsync(SalesFilter filter);
        
        /// <summary>
        /// 获取销售趋势数据（月）
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每月销售趋势数据</returns>
        Task<IEnumerable<SalesTrend>> GetMonthlySalesTrendAsync(SalesFilter filter);
        
        /// <summary>
        /// 获取产品销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>产品销售排名</returns>
        Task<IEnumerable<ProductSalesRank>> GetTopProductsAsync(SalesFilter filter, int top = 10);
        
        /// <summary>
        /// 获取农户销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>农户销售排名</returns>
        Task<IEnumerable<FarmerSalesRank>> GetTopFarmersAsync(SalesFilter filter, int top = 10);
    }
}
 
 