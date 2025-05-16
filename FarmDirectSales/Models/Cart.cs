using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 购物车项模型
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// 购物车项ID
        /// </summary>
        [Key]
        public int CartItemId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; }

        /// <summary>
        /// 产品导航属性
        /// </summary>
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
} 
 
 