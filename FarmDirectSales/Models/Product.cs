using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 产品模型类
    /// </summary>
    public class Product
    {
        /// <summary>
        /// 产品ID
        /// </summary>
        [Key]
        public int ProductId { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 产品描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 产品价格
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Required]
        public int Stock { get; set; }

        /// <summary>
        /// 产品图片URL
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// 农户ID（外键）
        /// </summary>
        [Required]
        public int FarmerId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 产品状态（上架/下架）
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 上架时间
        /// </summary>
        public DateTime? ActiveTime { get; set; }
        
        /// <summary>
        /// 下架时间
        /// </summary>
        public DateTime? InactiveTime { get; set; }
        
        /// <summary>
        /// 产品分类
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// 平均评分
        /// </summary>
        [Column(TypeName = "decimal(3,1)")]
        public decimal AverageRating { get; set; } = 0;
        
        /// <summary>
        /// 评价数量
        /// </summary>
        public int ReviewCount { get; set; } = 0;

        // 导航属性
        /// <summary>
        /// 农户信息（导航属性）
        /// </summary>
        [ForeignKey("FarmerId")]
        public virtual User? Farmer { get; set; }

        /// <summary>
        /// 订单列表（导航属性）
        /// </summary>
        public virtual ICollection<Order>? Orders { get; set; }

        /// <summary>
        /// 溯源信息（导航属性）
        /// </summary>
        public virtual ICollection<Trace>? Traces { get; set; }
        
        /// <summary>
        /// 产品图片集合（导航属性）
        /// </summary>
        public virtual ICollection<Upload>? Uploads { get; set; }
        
        /// <summary>
        /// 商品评价集合（导航属性）
        /// </summary>
        public virtual ICollection<Review>? Reviews { get; set; }
    }
} 
 
 