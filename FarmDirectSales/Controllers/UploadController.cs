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
        /// 删除上传的文件
        /// </summary>
        [HttpDelete("{uploadId}")]
        public async Task<IActionResult> DeleteUpload(int uploadId)
        {
            try
            {
                var success = await _uploadService.DeleteUploadAsync(uploadId);
                if (success)
                {
                    return Ok(new { code = 200, message = "删除成功" });
                }
                else
                {
                    return NotFound(new { code = 404, message = "文件不存在" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = 500, message = $"删除失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 上传单张农场照片到指定位置
        /// </summary>
        [HttpPost("farm-photo/{farmerId}/{photoIndex}")]
        public async Task<IActionResult> UploadSingleFarmPhoto(int farmerId, int photoIndex, [FromForm] IFormFile photo)
        {
            try
            {
                Console.WriteLine($"收到单张农场照片上传请求: 农户ID={farmerId}, 照片位置={photoIndex}");

                if (photo == null || photo.Length == 0)
                {
                    return BadRequest(new { code = 400, message = "请选择照片" });
                }

                if (photoIndex < 1 || photoIndex > 3)
                {
                    return BadRequest(new { code = 400, message = "照片位置必须在1-3之间" });
                }

                // 验证农户是否存在
                var farmer = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 验证权限（农户只能上传自己的照片，管理员可以上传任何农户的照片）
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || (currentUser.UserId != farmerId && currentUser.Role != "admin"))
                {
                    return Forbid(new { code = 403, message = "无权限上传此农户的照片" }.ToString());
                }

                // 验证文件类型
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                if (!allowedTypes.Contains(fileExtension))
                {
                    return BadRequest(new { code = 400, message = $"不支持的文件类型: {fileExtension}" });
                }

                // 验证文件大小（限制为5MB）
                if (photo.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { code = 400, message = "文件大小不能超过5MB" });
                }

                // 上传照片
                var upload = await _uploadService.UploadFileAsync(photo, null);

                // 确保FarmerProfile存在
                if (farmer.FarmerProfile == null)
                {
                    farmer.FarmerProfile = new Models.FarmerProfile
                    {
                        UserId = farmerId,
                        FarmName = "未设置",
                        Location = "未设置"
                    };
                    _context.FarmerProfiles.Add(farmer.FarmerProfile);
                }

                // 删除旧照片（如果存在）
                string oldPhotoPath = null;
                switch (photoIndex)
                {
                    case 1:
                        oldPhotoPath = farmer.FarmerProfile.FarmPhoto1;
                        farmer.FarmerProfile.FarmPhoto1 = upload.FilePath;
                        break;
                    case 2:
                        oldPhotoPath = farmer.FarmerProfile.FarmPhoto2;
                        farmer.FarmerProfile.FarmPhoto2 = upload.FilePath;
                        break;
                    case 3:
                        oldPhotoPath = farmer.FarmerProfile.FarmPhoto3;
                        farmer.FarmerProfile.FarmPhoto3 = upload.FilePath;
                        break;
                }

                farmer.FarmerProfile.UpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();

                // 尝试删除旧照片文件
                if (!string.IsNullOrEmpty(oldPhotoPath))
                {
                    try
                    {
                        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPhotoPath.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                        {
                            System.IO.File.Delete(physicalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"删除旧照片文件失败: {ex.Message}");
                        // 不阻止操作继续
                    }
                }

                Console.WriteLine($"单张农场照片上传成功: 农户ID={farmerId}, 照片位置={photoIndex}, 路径={upload.FilePath}");

                return Ok(new
                {
                    code = 200,
                    message = "照片上传成功",
                    data = new
                    {
                        farmerId,
                        photoIndex,
                        photoPath = upload.FilePath
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"单张农场照片上传失败: {ex.Message}");
                return StatusCode(500, new { code = 500, message = $"上传失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 上传农场照片
        /// </summary>
        [HttpPost("farm-photos/{farmerId}")]
        public async Task<IActionResult> UploadFarmPhotos(int farmerId, [FromForm] List<IFormFile> photos)
        {
            try
            {
                Console.WriteLine($"收到农场照片上传请求: 农户ID={farmerId}, 照片数量={photos?.Count}");

                if (photos == null || !photos.Any())
                {
                    return BadRequest(new { code = 400, message = "请选择照片" });
                }

                if (photos.Count > 3)
                {
                    return BadRequest(new { code = 400, message = "最多只能上传3张照片" });
                }

                // 验证农户是否存在
                var farmer = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (farmer == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 验证权限（农户只能上传自己的照片，管理员可以上传任何农户的照片）
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || (currentUser.UserId != farmerId && currentUser.Role != "admin"))
                {
                    return Forbid(new { code = 403, message = "无权限上传此农户的照片" }.ToString());
                }

                var uploadedPhotos = new List<string>();

                // 上传每张照片
                for (int i = 0; i < photos.Count; i++)
                {
                    var photo = photos[i];
                    if (photo.Length > 0)
                    {
                        // 验证文件类型
                        var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                        if (!allowedTypes.Contains(fileExtension))
                        {
                            return BadRequest(new { code = 400, message = $"不支持的文件类型: {fileExtension}" });
                        }

                        // 验证文件大小（限制为5MB）
                        if (photo.Length > 5 * 1024 * 1024)
                        {
                            return BadRequest(new { code = 400, message = "文件大小不能超过5MB" });
                        }

                        var upload = await _uploadService.UploadFileAsync(photo, null);
                        uploadedPhotos.Add(upload.FilePath);
                    }
                }

                // 确保FarmerProfile存在
                if (farmer.FarmerProfile == null)
                {
                    farmer.FarmerProfile = new Models.FarmerProfile
                    {
                        UserId = farmerId,
                        FarmName = "未设置",
                        Location = "未设置"
                    };
                    _context.FarmerProfiles.Add(farmer.FarmerProfile);
                }

                // 更新FarmerProfile中的照片信息
                for (int i = 0; i < uploadedPhotos.Count && i < 3; i++)
                {
                    switch (i)
                    {
                        case 0:
                            farmer.FarmerProfile.FarmPhoto1 = uploadedPhotos[i];
                            break;
                        case 1:
                            farmer.FarmerProfile.FarmPhoto2 = uploadedPhotos[i];
                            break;
                        case 2:
                            farmer.FarmerProfile.FarmPhoto3 = uploadedPhotos[i];
                            break;
                    }
                }

                farmer.FarmerProfile.UpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();

                Console.WriteLine($"农场照片上传成功: 农户ID={farmerId}, 成功上传{uploadedPhotos.Count}张照片");

                return Ok(new
                {
                    code = 200,
                    message = $"成功上传 {uploadedPhotos.Count} 张农场照片",
                    data = new
                    {
                        farmerId,
                        photos = uploadedPhotos.Select((path, index) => new
                        {
                            index = index + 1,
                            path
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"农场照片上传失败: {ex.Message}");
                return StatusCode(500, new { code = 500, message = $"上传失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 删除农场照片
        /// </summary>
        [HttpDelete("farm-photos/{farmerId}/{photoIndex}")]
        public async Task<IActionResult> DeleteFarmPhoto(int farmerId, int photoIndex)
        {
            try
            {
                Console.WriteLine($"收到删除农场照片请求: 农户ID={farmerId}, 照片索引={photoIndex}");

                if (photoIndex < 1 || photoIndex > 3)
                {
                    return BadRequest(new { code = 400, message = "照片索引必须在1-3之间" });
                }

                // 验证农户是否存在
                var farmer = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");

                if (farmer == null || farmer.FarmerProfile == null)
                {
                    return NotFound(new { code = 404, message = "农户或农户资料不存在" });
                }

                // 验证权限
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || (currentUser.UserId != farmerId && currentUser.Role != "admin"))
                {
                    return Forbid(new { code = 403, message = "无权限删除此农户的照片" }.ToString());
                }

                // 获取要删除的照片路径
                string photoPath = null;
                switch (photoIndex)
                {
                    case 1:
                        photoPath = farmer.FarmerProfile.FarmPhoto1;
                        farmer.FarmerProfile.FarmPhoto1 = null;
                        break;
                    case 2:
                        photoPath = farmer.FarmerProfile.FarmPhoto2;
                        farmer.FarmerProfile.FarmPhoto2 = null;
                        break;
                    case 3:
                        photoPath = farmer.FarmerProfile.FarmPhoto3;
                        farmer.FarmerProfile.FarmPhoto3 = null;
                        break;
                }

                if (string.IsNullOrEmpty(photoPath))
                {
                    return NotFound(new { code = 404, message = "照片不存在" });
                }

                farmer.FarmerProfile.UpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();

                // 删除物理文件（如果需要）
                try
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除物理文件失败: {ex.Message}");
                    // 不阻止操作继续，数据库记录已删除
                }

                Console.WriteLine($"农场照片删除成功: 农户ID={farmerId}, 照片索引={photoIndex}");

                return Ok(new
                {
                    code = 200,
                    message = "照片删除成功",
                    data = new { farmerId, photoIndex }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除农场照片失败: {ex.Message}");
                return StatusCode(500, new { code = 500, message = $"删除失败: {ex.Message}" });
            }
        }
    }
} 
 
 