using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 溯源信息模型类
    /// </summary>
    public class Trace
    {
        /// <summary>
        /// 溯源ID
        /// </summary>
        [Key]
        public int TraceId { get; set; }

        /// <summary>
        /// 产品ID（外键）
        /// </summary>
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// 产地信息
        /// </summary>
        [Required]
        public string SourcePlace { get; set; } = string.Empty;

        /// <summary>
        /// 种植/养殖方式
        /// </summary>
        public string PlantingMethod { get; set; } = string.Empty;

        /// <summary>
        /// 种植/养殖时间
        /// </summary>
        public DateTime PlantingTime { get; set; }

        /// <summary>
        /// 收获时间
        /// </summary>
        public DateTime HarvestTime { get; set; }

        /// <summary>
        /// 质量等级
        /// </summary>
        public string QualityLevel { get; set; } = string.Empty;

        /// <summary>
        /// 是否有机认证
        /// </summary>
        public bool IsOrganic { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 附加信息
        /// </summary>
        public string AdditionalInfo { get; set; } = string.Empty;

        // 导航属性
        /// <summary>
        /// 关联产品
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
} 
 
 