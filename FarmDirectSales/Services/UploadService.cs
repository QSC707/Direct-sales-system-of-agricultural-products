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
                throw new ArgumentException("文件不能为空");
            }

            // 验证文件大小
            if (file.Length > _maxFileSize)
            {
                throw new ArgumentException($"文件大小不能超过{_maxFileSize / (1024 * 1024)}MB");
            }

            // 验证文件类型
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(_allowedExtensions, ext => ext == extension))
            {
                throw new ArgumentException($"文件类型不支持，允许的类型: {string.Join(", ", _allowedExtensions)}");
            }

            // 创建上传路径
            var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 生成唯一的文件名
            var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 保存文件
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

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

                _context.Uploads.Add(upload);
                await _context.SaveChangesAsync();

                return upload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上传文件失败");
                // 如果保存文件失败，删除已保存的文件
                if (File.Exists(filePath))
                {
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
            try
            {
                var upload = await _context.Uploads.FindAsync(uploadId);
                if (upload == null)
                {
                    return false;
                }

                // 删除物理文件
                var filePath = Path.Combine(_environment.ContentRootPath, "wwwroot", upload.FilePath.TrimStart('/'));
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
                _logger.LogError(ex, $"删除上传文件 ID={uploadId} 失败");
                return false;
            }
        }
    }
} 
 
 