using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmDirectSales.Models
{
    /// <summary>
    /// 图片上传模型类
    /// </summary>
    public class Upload
    {
        /// <summary>
        /// 上传ID
        /// </summary>
        [Key]
        public int UploadId { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        [Required]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        [Required]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 关联产品ID（外键）
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UploadTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 关联产品（导航属性）
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
} 
 
 