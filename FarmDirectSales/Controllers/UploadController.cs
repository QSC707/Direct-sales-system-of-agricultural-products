using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Services;
using FarmDirectSales.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

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
                Console.WriteLine($"收到文件上传请求: 文件名={file?.FileName}, 大小={file?.Length}, 产品ID={productId}");
                
                // 检查表单数据
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"表单字段: {key} = {Request.Form[key]}");
                }
                
                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("文件为空或大小为0，返回400错误");
                    return BadRequest(new { code = 400, message = "请选择文件" });
                }

                // 验证文件类型
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                Console.WriteLine($"文件扩展名: {fileExtension}, 文件类型: {file.ContentType}");
                
                // 如果指定了产品ID，验证产品是否存在
                if (productId.HasValue)
                {
                    var product = await _context.Products.FindAsync(productId.Value);
                    Console.WriteLine($"查找产品ID={productId}结果: {(product != null ? "找到" : "未找到")}");
                    
                    if (product == null)
                    {
                        return NotFound(new { code = 404, message = "产品不存在" });
                    }

                    // 临时注释掉用户验证，方便测试上传功能
                    /*
                    // 验证当前用户是否是产品的所有者（农户）
                    var currentUser = HttpContext.Items["User"] as Models.User;
                    if (currentUser == null || (currentUser.Role != "admin" && currentUser.UserId != product.FarmerId))
                    {
                        return BadRequest(new { code = 403, message = "无权为此产品上传图片" });
                    }
                    */
                }

                Console.WriteLine("开始调用上传服务...");
                var upload = await _uploadService.UploadFileAsync(file, productId);
                Console.WriteLine($"文件上传成功: ID={upload.UploadId}, 路径={upload.FilePath}");

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
                Console.WriteLine($"参数错误: {ex.Message}");
                return BadRequest(new { code = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"上传失败: {ex.Message}");
                Console.WriteLine($"异常详情: {ex}");
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
                Console.WriteLine($"收到多文件上传请求: 文件数量={files?.Count}, 产品ID={productId}");
                
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
                Console.WriteLine($"多文件上传成功: 成功上传{uploads.Count()}个文件");

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
                Console.WriteLine($"多文件上传失败: {ex.Message}");
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
                var uploads = await _uploadService.GetProductUploadsAsync(productId);
                return Ok(new
                {
                    code = 200,
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
                return StatusCode(500, new { code = 500, message = $"获取产品图片失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        [HttpDelete("{uploadId}")]
        public async Task<IActionResult> DeleteUpload(int uploadId)
        {
            try
            {
                var result = await _uploadService.DeleteUploadAsync(uploadId);
                if (result)
                {
                    return Ok(new { code = 200, message = "删除成功" });
                }
                else
                {
                    return NotFound(new { code = 404, message = "图片不存在或删除失败" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"删除图片失败: {ex.Message}" });
            }
        }
    }
} 
 
 