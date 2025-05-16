using System;
using System.ComponentModel.DataAnnotations;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 用户模型类
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Key]
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码（加密存储）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（普通用户/农户/管理员）
        /// </summary>
        [Required]
        public string Role { get; set; } = string.Empty;
        
        /// <summary>
        /// 电子邮箱
        /// </summary>
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        
        /// <summary>
        /// 手机号码
        /// </summary>
        [StringLength(20)]
        [Phone]
        public string? Phone { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginTime { get; set; }

        // 导航属性
        /// <summary>
        /// 用户发布的产品列表（仅当用户是农户时有效）
        /// </summary>
        public virtual ICollection<Product>? Products { get; set; }

        /// <summary>
        /// 用户下的订单列表
        /// </summary>
        public virtual ICollection<Order>? Orders { get; set; }

        /// <summary>
        /// 用户操作日志
        /// </summary>
        public virtual ICollection<Log>? Logs { get; set; }
        
        /// <summary>
        /// 用户发表的评价
        /// </summary>
        public virtual ICollection<Review>? Reviews { get; set; }
    }
} 