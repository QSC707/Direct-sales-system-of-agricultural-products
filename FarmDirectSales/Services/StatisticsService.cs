using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 销售统计服务实现
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库上下文</param>
        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// 获取销售总览数据
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>销售总览数据</returns>
        public async Task<SalesOverview> GetSalesOverviewAsync(SalesFilter filter)
        {
            var query = GetFilteredOrdersQuery(filter);
            
            var orders = await query.ToListAsync();
            
            if (orders.Count == 0)
            {
                return new SalesOverview
                {
                    TotalOrders = 0,
                    TotalSales = 0,
                    TotalQuantity = 0,
                    AverageOrderValue = 0
                };
            }
            
            var totalOrders = orders.Count;
            var totalSales = orders.Sum(o => o.TotalPrice);
            var totalQuantity = orders.Sum(o => o.Quantity);
            var averageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0;
            
            return new SalesOverview
            {
                TotalOrders = totalOrders,
                TotalSales = totalSales,
                TotalQuantity = totalQuantity,
                AverageOrderValue = averageOrderValue
            };
        }
        
        /// <summary>
        /// 获取销售趋势数据（日）
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每日销售趋势数据</returns>
        public async Task<IEnumerable<SalesTrend>> GetDailySalesTrendAsync(SalesFilter filter)
        {
            var query = GetFilteredOrdersQuery(filter);
            
            // 确保日期范围
            var startDate = filter.StartDate ?? DateTime.Now.AddDays(-30);
            var endDate = filter.EndDate ?? DateTime.Now;
            
            // 如果日期范围太大，则限制为30天
            if ((endDate - startDate).TotalDays > 90)
            {
                startDate = endDate.AddDays(-90);
            }
            
            var orders = await query
                .Where(o => o.CreateTime >= startDate && o.CreateTime <= endDate)
                .ToListAsync();
            
            // 按日期分组
            var result = orders
                .GroupBy(o => o.CreateTime.Date)
                .Select(g => new SalesTrend
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    Sales = g.Sum(o => o.TotalPrice),
                    Orders = g.Count()
                })
                .OrderBy(s => s.Period)
                .ToList();
            
            // 填充没有数据的日期
            var allDates = new List<SalesTrend>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dateString = date.ToString("yyyy-MM-dd");
                var existing = result.FirstOrDefault(r => r.Period == dateString);
                
                if (existing != null)
                {
                    allDates.Add(existing);
                }
                else
                {
                    allDates.Add(new SalesTrend
                    {
                        Period = dateString,
                        Sales = 0,
                        Orders = 0
                    });
                }
            }
            
            return allDates;
        }
        
        /// <summary>
        /// 获取销售趋势数据（月）
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每月销售趋势数据</returns>
        public async Task<IEnumerable<SalesTrend>> GetMonthlySalesTrendAsync(SalesFilter filter)
        {
            var query = GetFilteredOrdersQuery(filter);
            
            // 确保日期范围
            var startDate = filter.StartDate ?? DateTime.Now.AddMonths(-12);
            var endDate = filter.EndDate ?? DateTime.Now;
            
            // 如果日期范围太大，则限制为24个月
            if ((endDate - startDate).TotalDays > 730)
            {
                startDate = endDate.AddMonths(-24);
            }
            
            var orders = await query
                .Where(o => o.CreateTime >= startDate && o.CreateTime <= endDate)
                .ToListAsync();
            
            // 按月份分组
            var result = orders
                .GroupBy(o => new { Year = o.CreateTime.Year, Month = o.CreateTime.Month })
                .Select(g => new SalesTrend
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Sales = g.Sum(o => o.TotalPrice),
                    Orders = g.Count()
                })
                .OrderBy(s => s.Period)
                .ToList();
            
            // 填充没有数据的月份
            var allMonths = new List<SalesTrend>();
            for (var date = new DateTime(startDate.Year, startDate.Month, 1); 
                 date <= new DateTime(endDate.Year, endDate.Month, 1); 
                 date = date.AddMonths(1))
            {
                var monthString = $"{date.Year}-{date.Month:D2}";
                var existing = result.FirstOrDefault(r => r.Period == monthString);
                
                if (existing != null)
                {
                    allMonths.Add(existing);
                }
                else
                {
                    allMonths.Add(new SalesTrend
                    {
                        Period = monthString,
                        Sales = 0,
                        Orders = 0
                    });
                }
            }
            
            return allMonths;
        }
        
        /// <summary>
        /// 获取产品销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>产品销售排名</returns>
        public async Task<IEnumerable<ProductSalesRank>> GetTopProductsAsync(SalesFilter filter, int top = 10)
        {
            var query = GetFilteredOrdersQuery(filter);
            
            var orders = await query
                .Include(o => o.Product)
                .ToListAsync();
            
            var productRanks = orders
                .GroupBy(o => new { o.ProductId, ProductName = o.Product?.ProductName ?? "未知产品" })
                .Select(g => new ProductSalesRank
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Sales = g.Sum(o => o.TotalPrice),
                    Quantity = g.Sum(o => o.Quantity)
                })
                .OrderByDescending(p => p.Sales)
                .Take(top)
                .ToList();
            
            return productRanks;
        }
        
        /// <summary>
        /// 获取农户销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>农户销售排名</returns>
        public async Task<IEnumerable<FarmerSalesRank>> GetTopFarmersAsync(SalesFilter filter, int top = 10)
        {
            var query = GetFilteredOrdersQuery(filter);
            
            var orders = await query
                .Include(o => o.Product)
                .ThenInclude(p => p.Farmer)
                .ToListAsync();
            
            var farmerRanks = orders
                .GroupBy(o => new 
                { 
                    FarmerId = o.Product?.FarmerId ?? 0, 
                    FarmerName = o.Product?.Farmer?.Username ?? "未知农户" 
                })
                .Where(g => g.Key.FarmerId != 0)
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
            
            return farmerRanks;
        }
        
        /// <summary>
        /// 根据筛选条件获取订单查询
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>订单查询</returns>
        private IQueryable<Order> GetFilteredOrdersQuery(SalesFilter filter)
        {
            var query = _context.Orders.AsQueryable();
            
            // 只考虑已完成的订单
            query = query.Where(o => o.Status == "已完成");
            
            // 应用筛选条件
            if (filter != null)
            {
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreateTime >= filter.StartDate);
                }
                
                if (filter.EndDate.HasValue)
                {
                    // 设置结束日期为当天的最后一刻
                    var endDate = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(o => o.CreateTime <= endDate);
                }
                
                if (filter.ProductId.HasValue)
                {
                    query = query.Where(o => o.ProductId == filter.ProductId);
                }
                
                if (filter.FarmerId.HasValue)
                {
                    query = query.Where(o => o.Product.FarmerId == filter.FarmerId);
                }
            }
            
            return query;
        }
    }
} 
 
 