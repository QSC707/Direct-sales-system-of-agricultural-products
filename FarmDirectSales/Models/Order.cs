using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 订单模型类
    /// </summary>
    public class Order
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Key]
        public int OrderId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 产品ID（外键）
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// 订单组ID（外键）
        /// </summary>
        public int? OrderGroupId { get; set; }

        /// <summary>
        /// 购买数量
        /// </summary>
        [Required]
        public int Quantity { get; set; }

        /// <summary>
        /// 订单总价
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 订单状态（未支付、待发货、已完成）
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime? PayTime { get; set; }

        /// <summary>
        /// 发货时间
        /// </summary>
        public DateTime? ShipTime { get; set; }

        /// <summary>
        /// 配送信息备注
        /// </summary>
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompleteTime { get; set; }
        
        /// <summary>
        /// 取消时间
        /// </summary>
        public DateTime? CancelTime { get; set; }
        
        /// <summary>
        /// 取消原因
        /// </summary>
        public string? CancelReason { get; set; }
        
        /// <summary>
        /// 取消人ID
        /// </summary>
        public int? CancelBy { get; set; }
        
        /// <summary>
        /// 取消人类型：user-用户取消，farmer-农户取消
        /// </summary>
        public string? CancelByType { get; set; }
        
        /// <summary>
        /// 退款申请时间
        /// </summary>
        public DateTime? RefundRequestTime { get; set; }

        /// <summary>
        /// 收货地址
        /// </summary>
        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// 单个订单的运费（通常为0，因为运费在OrderGroup中统一计算）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFeeAmount { get; set; } = 0;
        
        /// <summary>
        /// 运费规则ID
        /// </summary>
        public int? ShippingFeeId { get; set; }
        
        /// <summary>
        /// 是否已被删除（软删除）
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime? DeleteTime { get; set; }

        // 导航属性
        /// <summary>
        /// 用户信息（导航属性）
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// 产品信息（导航属性）
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
        
        /// <summary>
        /// 运费规则（导航属性）
        /// </summary>
        [ForeignKey("ShippingFeeId")]
        public virtual ShippingFee? ShippingFee { get; set; }
        
        /// <summary>
        /// 所属订单组（导航属性）
        /// </summary>
        [ForeignKey("OrderGroupId")]
        public virtual OrderGroup? OrderGroup { get; set; }
    }
} 
 
 