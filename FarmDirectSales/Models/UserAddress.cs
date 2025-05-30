using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 用户地址模型
    /// </summary>
    public class UserAddress
    {
        /// <summary>
        /// 地址ID
        /// </summary>
        [Key]
        public int AddressId { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 收货人姓名
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>
        /// 联系电话
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// 省份
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [Required]
        [StringLength(20)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
        [Required]
        [StringLength(20)]
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// 详细地址
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DetailAddress { get; set; } = string.Empty;

        /// <summary>
        /// 是否为默认地址
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 用户导航属性
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
} 