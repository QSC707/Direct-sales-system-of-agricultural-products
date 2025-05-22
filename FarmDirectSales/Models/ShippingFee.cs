using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 运费配置模型
    /// </summary>
    public class ShippingFee
    {
        /// <summary>
        /// 运费配置ID
        /// </summary>
        [Key]
        public int ShippingFeeId { get; set; }

        /// <summary>
        /// 配送区域ID（可空，为空表示默认运费）
        /// </summary>
        public int? DeliveryAreaId { get; set; }

        /// <summary>
        /// 基础运费
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseFee { get; set; } = 10.0m;

        /// <summary>
        /// 免运费订单金额（订单金额超过此值免运费）
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FreeShippingThreshold { get; set; } = 100.0m;

        /// <summary>
        /// 每公斤额外费用（按重量计费时使用）
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExtraFeePerKg { get; set; }

        /// <summary>
        /// 是否启用（禁用则不使用此运费规则）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 规则优先级（数字越大优先级越高）
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// 规则名称
        /// </summary>
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 规则描述
        /// </summary>
        [StringLength(200)]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新人（管理员ID）
        /// </summary>
        public int? UpdateBy { get; set; }

        /// <summary>
        /// 关联的配送区域（导航属性）
        /// </summary>
        [ForeignKey("DeliveryAreaId")]
        public virtual DeliveryArea? DeliveryArea { get; set; }
    }
} 