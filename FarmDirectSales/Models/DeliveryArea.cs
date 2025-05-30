using System;
using System.ComponentModel.DataAnnotations;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 配送区域模型
    /// </summary>
    public class DeliveryArea
    {
        /// <summary>
        /// 配送区域ID
        /// </summary>
        [Key]
        public int DeliveryAreaId { get; set; }

        /// <summary>
        /// 是否全国配送（不限制区域）
        /// </summary>
        public bool IsNationwide { get; set; } = false;
        
        /// <summary>
        /// 省份
        /// </summary>
        [StringLength(20)]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [StringLength(20)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
        [StringLength(20)]
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// 是否支持当天配送
        /// </summary>
        public bool SupportSameDayDelivery { get; set; } = true;

        /// <summary>
        /// 配送费用
        /// </summary>
        public decimal DeliveryFee { get; set; } = 15.0m;

        /// <summary>
        /// 配送说明
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
    }
} 