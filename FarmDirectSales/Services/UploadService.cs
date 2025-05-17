using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 文件上传服务实现
    /// </summary>
    public class UploadService : IUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadService> _logger;
        
        // 允许的文件类型
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        
        // 文件大小限制（10MB）
        private readonly long _maxFileSize = 10 * 1024 * 1024;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UploadService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<UploadService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// 上传单个文件
        /// </summary>
        public async Task<Upload> UploadFileAsync(IFormFile file, int? productId)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("上传失败: 文件为空");
                throw new ArgumentException("文件不能为空");
            }

            // 记录文件详细信息用于调试
            _logger.LogInformation($"接收到文件: 名称={file.FileName}, 大小={file.Length}字节, 类型={file.ContentType}");

            // 验证文件大小
            if (file.Length > _maxFileSize)
            {
                _logger.LogError($"上传失败: 文件大小超限 {file.Length} > {_maxFileSize}");
                throw new ArgumentException($"文件大小不能超过{_maxFileSize / (1024 * 1024)}MB");
            }

            // 验证文件类型
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            _logger.LogInformation($"上传文件扩展名: {extension}, 文件内容类型: {file.ContentType}, 允许的扩展名: {string.Join(", ", _allowedExtensions)}");
            
            // 根据MIME类型判断文件类型，解决扩展名不匹配问题
            if (!Array.Exists(_allowedExtensions, ext => ext == extension))
            {
                // 根据Content-Type尝试确定正确的扩展名
                if (file.ContentType == "image/png")
                {
                    _logger.LogWarning($"文件内容类型为PNG但扩展名不匹配: {extension}，自动更正为.png");
                    extension = ".png";
                }
                else if (file.ContentType == "image/jpeg" || file.ContentType == "image/jpg")
                {
                    _logger.LogWarning($"文件内容类型为JPEG但扩展名不匹配: {extension}，自动更正为.jpg");
                    extension = ".jpg";
                }
                else if (file.ContentType == "image/gif")
                {
                    _logger.LogWarning($"文件内容类型为GIF但扩展名不匹配: {extension}，自动更正为.gif");
                    extension = ".gif";
                }
                else
                {
                    _logger.LogError($"上传失败: 不支持的文件类型 {extension}，ContentType={file.ContentType}");
                    throw new ArgumentException($"文件类型不支持，允许的类型: {string.Join(", ", _allowedExtensions)}");
                }
            }

            // 创建上传路径
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            _logger.LogInformation($"上传目录: {uploadsFolder}");
            
            if (!Directory.Exists(uploadsFolder))
            {
                _logger.LogInformation("上传目录不存在，正在创建...");
                // 创建目录并设置权限
                Directory.CreateDirectory(uploadsFolder);
                try
                {
                    // 在Unix系统上设置权限
                    if (!OperatingSystem.IsWindows())
                    {
                        var directoryInfo = new DirectoryInfo(uploadsFolder);
                        var permissions = directoryInfo.UnixFileMode;
                        _logger.LogInformation($"上传目录原始权限: {permissions}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"获取目录权限失败: {ex.Message}");
                }
            }

            // 生成唯一的文件名
            var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            _logger.LogInformation($"生成的文件路径: {filePath}");

            // 保存文件
            try
            {
                _logger.LogInformation("开始保存文件...");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("文件已保存到磁盘");

                // 创建上传记录
                var upload = new Upload
                {
                    FileName = file.FileName,
                    FilePath = $"/uploads/{uniqueFileName}",
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    ProductId = productId,
                    UploadTime = DateTime.Now
                };

                _logger.LogInformation("添加上传记录到数据库");
                _context.Uploads.Add(upload);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"上传记录已保存，ID: {upload.UploadId}");

                return upload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传文件失败: {ex.Message}");
                // 如果保存文件失败，删除已保存的文件
                if (File.Exists(filePath))
                {
                    _logger.LogInformation($"删除部分上传的文件: {filePath}");
                    File.Delete(filePath);
                }
                throw;
            }
        }

        /// <summary>
        /// 上传多个文件
        /// </summary>
        public async Task<IEnumerable<Upload>> UploadFilesAsync(IEnumerable<IFormFile> files, int? productId)
        {
            if (files == null)
            {
                throw new ArgumentException("文件集合不能为空");
            }

            var uploadResults = new List<Upload>();
            
            foreach (var file in files)
            {
                try
                {
                    var upload = await UploadFileAsync(file, productId);
                    uploadResults.Add(upload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"上传文件 {file.FileName} 失败");
                    // 继续上传其他文件
                }
            }

            return uploadResults;
        }

        /// <summary>
        /// 获取产品图片列表
        /// </summary>
        public async Task<IEnumerable<Upload>> GetProductUploadsAsync(int productId)
        {
            return await _context.Uploads
                .Where(u => u.ProductId == productId)
                .OrderByDescending(u => u.UploadTime)
                .ToListAsync();
        }

        /// <summary>
        /// 删除上传的文件
        /// </summary>
        public async Task<bool> DeleteUploadAsync(int uploadId)
        {
            var upload = await _context.Uploads.FindAsync(uploadId);
            if (upload == null)
            {
                return false;
            }

            try
            {
                // 删除物理文件
                var filePath = Path.Combine(_environment.WebRootPath, upload.FilePath.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 删除数据库记录
                _context.Uploads.Remove(upload);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除上传文件失败: {ex.Message}");
                return false;
            }
        }
    }
} 
 
 