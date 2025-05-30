using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 订单组模型，用于将多个订单关联到同一个提交批次
    /// </summary>
    public class OrderGroup
    {
        /// <summary>
        /// 订单组ID
        /// </summary>
        [Key]
        public int OrderGroupId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 订单组编号，方便前端展示
        /// </summary>
        [Required]
        public string GroupNumber { get; set; } = string.Empty;

        /// <summary>
        /// 订单总数
        /// </summary>
        public int OrderCount { get; set; } = 0;

        /// <summary>
        /// 商品总金额（不含运费）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalProductAmount { get; set; } = 0;

        /// <summary>
        /// 运费总金额
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFeeAmount { get; set; } = 0;

        /// <summary>
        /// 订单总金额（含运费）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        /// <summary>
        /// 配送地址
        /// </summary>
        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// 收货人姓名
        /// </summary>
        [Required]
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>
        /// 配送区域ID
        /// </summary>
        public int? DeliveryAreaId { get; set; }

        /// <summary>
        /// 运费规则ID
        /// </summary>
        public int? ShippingFeeId { get; set; }

        // 导航属性
        /// <summary>
        /// 用户信息（导航属性）
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 该组中的所有订单（导航属性）
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        /// <summary>
        /// 运费规则（导航属性）
        /// </summary>
        [ForeignKey("ShippingFeeId")]
        public virtual ShippingFee? ShippingFee { get; set; }
    }
} 