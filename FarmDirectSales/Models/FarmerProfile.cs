using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 农户资料模型类
    /// </summary>
    public class FarmerProfile
    {
        /// <summary>
        /// 农户资料ID
        /// </summary>
        [Key]
        public int FarmerProfileId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 农场名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FarmName { get; set; } = string.Empty;

        /// <summary>
        /// 农场位置
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 农场简介
        /// </summary>
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 主营产品类别
        /// </summary>
        [StringLength(100)]
        public string ProductCategory { get; set; } = string.Empty;

        /// <summary>
        /// 农场成立时间
        /// </summary>
        public DateTime? EstablishedDate { get; set; }

        /// <summary>
        /// 农场营业执照号
        /// </summary>
        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// 资料创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 资料更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 用户（导航属性）
        /// </summary>
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
} 