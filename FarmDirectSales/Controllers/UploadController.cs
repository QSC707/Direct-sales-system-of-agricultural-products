using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Services;
using FarmDirectSales.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 文件上传控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IUploadService _uploadService;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UploadController(IUploadService uploadService, ApplicationDbContext context)
        {
            _uploadService = uploadService;
            _context = context;
        }

        /// <summary>
        /// 上传单个文件
        /// </summary>
        [HttpPost("single")]
        public async Task<IActionResult> UploadSingleFile([FromForm] IFormFile file, [FromForm] int? productId = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { code = 400, message = "请选择文件" });
                }

                // 如果指定了产品ID，验证产品是否存在
                if (productId.HasValue)
                {
                    var product = await _context.Products.FindAsync(productId.Value);
                    if (product == null)
                    {
                        return NotFound(new { code = 404, message = "产品不存在" });
                    }

                    // 验证当前用户是否是产品的所有者（农户）
                    var currentUser = HttpContext.Items["User"] as Models.User;
                    if (currentUser == null || (currentUser.Role != "admin" && currentUser.UserId != product.FarmerId))
                    {
                        return BadRequest(new { code = 403, message = "无权为此产品上传图片" });
                    }
                }

                var upload = await _uploadService.UploadFileAsync(file, productId);

                return Ok(new
                {
                    code = 200,
                    message = "上传成功",
                    data = new
                    {
                        upload.UploadId,
                        upload.FileName,
                        upload.FilePath,
                        upload.FileType,
                        upload.FileSize,
                        upload.ProductId,
                        upload.UploadTime
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"上传失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 上传多个文件
        /// </summary>
        [HttpPost("multiple")]
        public async Task<IActionResult> UploadMultipleFiles([FromForm] List<IFormFile> files, [FromForm] int? productId = null)
        {
            try
            {
                if (files == null || !files.Any())
                {
                    return BadRequest(new { code = 400, message = "请选择文件" });
                }

                // 如果指定了产品ID，验证产品是否存在
                if (productId.HasValue)
                {
                    var product = await _context.Products.FindAsync(productId.Value);
                    if (product == null)
                    {
                        return NotFound(new { code = 404, message = "产品不存在" });
                    }

                    // 验证当前用户是否是产品的所有者（农户）
                    var currentUser = HttpContext.Items["User"] as Models.User;
                    if (currentUser == null || (currentUser.Role != "admin" && currentUser.UserId != product.FarmerId))
                    {
                        return BadRequest(new { code = 403, message = "无权为此产品上传图片" });
                    }
                }

                var uploads = await _uploadService.UploadFilesAsync(files, productId);

                return Ok(new
                {
                    code = 200,
                    message = $"成功上传 {uploads.Count()} 个文件",
                    data = uploads.Select(u => new
                    {
                        u.UploadId,
                        u.FileName,
                        u.FilePath,
                        u.FileType,
                        u.FileSize,
                        u.ProductId,
                        u.UploadTime
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"上传失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取产品图片列表
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductUploads(int productId)
        {
            try
            {
                // 检查产品是否存在
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(new { code = 404, message = "产品不存在" });
                }

                var uploads = await _uploadService.GetProductUploadsAsync(productId);

                return Ok(new
                {
                    code = 200,
                    message = "获取成功",
                    data = uploads.Select(u => new
                    {
                        u.UploadId,
                        u.FileName,
                        u.FilePath,
                        u.FileType,
                        u.FileSize,
                        u.ProductId,
                        u.UploadTime
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"获取失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 删除上传的文件
        /// </summary>
        [HttpDelete("{uploadId}")]
        public async Task<IActionResult> DeleteUpload(int uploadId)
        {
            try
            {
                // 查找要删除的上传
                var upload = await _context.Uploads
                    .Include(u => u.Product)
                    .FirstOrDefaultAsync(u => u.UploadId == uploadId);

                if (upload == null)
                {
                    return NotFound(new { code = 404, message = "上传文件不存在" });
                }

                // 验证当前用户是否有权限删除
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || 
                    (currentUser.Role != "admin" && 
                     (upload.Product == null || currentUser.UserId != upload.Product.FarmerId)))
                {
                    return BadRequest(new { code = 403, message = "无权删除此文件" });
                }

                bool result = await _uploadService.DeleteUploadAsync(uploadId);
                if (result)
                {
                    return Ok(new { code = 200, message = "删除成功" });
                }
                else
                {
                    return StatusCode(500, new { code = 500, message = "删除失败" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"删除失败: {ex.Message}" });
            }
        }
    }
} 
 
 