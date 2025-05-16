using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 商品评价模型类
    /// </summary>
    public class Review
    {
        /// <summary>
        /// 评价ID
        /// </summary>
        [Key]
        public int ReviewId { get; set; }

        /// <summary>
        /// 产品ID（外键）
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 订单ID（外键）
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// 评分（1-5）
        /// </summary>
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        /// <summary>
        /// 评价内容
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 是否匿名评价
        /// </summary>
        public bool IsAnonymous { get; set; } = false;

        /// <summary>
        /// 评价时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 关联产品（导航属性）
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        /// <summary>
        /// 关联用户（导航属性）
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 关联订单（导航属性）
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
} 
 
 