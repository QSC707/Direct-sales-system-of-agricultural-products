using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 日志模型类
    /// </summary>
    public class Log
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        [Key]
        public int LogId { get; set; }

        /// <summary>
        /// 用户ID（外键）
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// 操作内容
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 操作对象ID
        /// </summary>
        public int? TargetId { get; set; }

        /// <summary>
        /// 操作对象类型
        /// </summary>
        public string? TargetType { get; set; }

        /// <summary>
        /// 操作状态（成功/失败）
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime ActionTime { get; set; } = DateTime.Now;

        // 导航属性
        /// <summary>
        /// 用户信息（导航属性）
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
} 
 
 