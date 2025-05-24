using System.ComponentModel.DataAnnotations;

namespace FarmDirectSales.Models.DTOs
{
    /// <summary>
    /// 创建配送预设请求模型
    /// </summary>
    public class CreateDeliveryPresetRequest
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        [Required(ErrorMessage = "预设名称不能为空")]
        [StringLength(50, ErrorMessage = "预设名称长度不能超过50个字符")]
        public string PresetName { get; set; } = string.Empty;

        /// <summary>
        /// 配送信息备注
        /// </summary>
        [StringLength(500, ErrorMessage = "配送信息备注长度不能超过500个字符")]
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        [StringLength(50, ErrorMessage = "配送联系人长度不能超过50个字符")]
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        [StringLength(20, ErrorMessage = "配送联系电话长度不能超过20个字符")]
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        [StringLength(100, ErrorMessage = "预计送达时间长度不能超过100个字符")]
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// 是否设为默认预设
        /// </summary>
        public bool SetAsDefault { get; set; } = false;
    }

    /// <summary>
    /// 更新配送预设请求模型
    /// </summary>
    public class UpdateDeliveryPresetRequest
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        [Required(ErrorMessage = "预设名称不能为空")]
        [StringLength(50, ErrorMessage = "预设名称长度不能超过50个字符")]
        public string PresetName { get; set; } = string.Empty;

        /// <summary>
        /// 配送信息备注
        /// </summary>
        [StringLength(500, ErrorMessage = "配送信息备注长度不能超过500个字符")]
        public string? DeliveryInfo { get; set; }

        /// <summary>
        /// 配送联系人
        /// </summary>
        [StringLength(50, ErrorMessage = "配送联系人长度不能超过50个字符")]
        public string? DeliveryContact { get; set; }

        /// <summary>
        /// 配送联系电话
        /// </summary>
        [StringLength(20, ErrorMessage = "配送联系电话长度不能超过20个字符")]
        public string? DeliveryPhone { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        [StringLength(100, ErrorMessage = "预计送达时间长度不能超过100个字符")]
        public string? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// 是否设为默认预设
        /// </summary>
        public bool SetAsDefault { get; set; } = false;
    }
} 