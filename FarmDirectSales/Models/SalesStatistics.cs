using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 销售总览数据
    /// </summary>
    public class SalesOverview
    {
        /// <summary>
        /// 总订单数
        /// </summary>
        public int TotalOrders { get; set; }
        
        /// <summary>
        /// 总销售额
        /// </summary>
        public decimal TotalSales { get; set; }
        
        /// <summary>
        /// 总销售量
        /// </summary>
        public int TotalQuantity { get; set; }
        
        /// <summary>
        /// 平均订单价值
        /// </summary>
        public decimal AverageOrderValue { get; set; }
    }
    
    /// <summary>
    /// 销售趋势数据
    /// </summary>
    public class SalesTrend
    {
        /// <summary>
        /// 时间段（日期或月份）
        /// </summary>
        public string Period { get; set; } = string.Empty;
        
        /// <summary>
        /// 销售额
        /// </summary>
        public decimal Sales { get; set; }
        
        /// <summary>
        /// 订单数
        /// </summary>
        public int Orders { get; set; }
    }
    
    /// <summary>
    /// 产品销售排名
    /// </summary>
    public class ProductSalesRank
    {
        /// <summary>
        /// 产品ID
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// 产品名称
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// 销售额
        /// </summary>
        public decimal Sales { get; set; }
        
        /// <summary>
        /// 销售量
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// 产品类别
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 农户销售排名
    /// </summary>
    public class FarmerSalesRank
    {
        /// <summary>
        /// 农户ID
        /// </summary>
        public int FarmerId { get; set; }
        
        /// <summary>
        /// 农户名称
        /// </summary>
        public string FarmerName { get; set; } = string.Empty;
        
        /// <summary>
        /// 销售额
        /// </summary>
        public decimal Sales { get; set; }
        
        /// <summary>
        /// 销售量
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// 产品数量
        /// </summary>
        public int ProductCount { get; set; }
    }
    
    /// <summary>
    /// 销售数据筛选条件
    /// </summary>
    public class SalesFilter
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// 产品ID
        /// </summary>
        public int? ProductId { get; set; }
        
        /// <summary>
        /// 农户ID
        /// </summary>
        public int? FarmerId { get; set; }
    }
} 
 
 