using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 配送信息预设模型类
    /// </summary>
    public class DeliveryPreset
    {
        /// <summary>
        /// 预设ID
        /// </summary>
        [Key]
        public int PresetId { get; set; }

        /// <summary>
        /// 农户ID（外键）
        /// </summary>
        [Required]
        public int FarmerId { get; set; }

        /// <summary>
        /// 预设名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string PresetName { get; set; } = string.Empty;

        /// <summary>
        /// 配送信息备注
        /// </summary>
        [StringLength(500)]
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        [StringLength(50)]
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        [StringLength(20)]
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        [StringLength(100)]
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// 是否为系统默认预设
        /// </summary>
        public bool IsSystemDefault { get; set; } = false;

        /// <summary>
        /// 是否为用户默认预设
        /// </summary>
        public bool IsUserDefault { get; set; } = false;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        // 导航属性
        /// <summary>
        /// 农户信息（导航属性）
        /// </summary>
        [ForeignKey("FarmerId")]
        public virtual User? Farmer { get; set; }
    }
} 