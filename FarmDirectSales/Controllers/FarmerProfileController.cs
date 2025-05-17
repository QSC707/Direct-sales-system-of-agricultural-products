using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Services;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 农户资料控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FarmerProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public FarmerProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取农户资料
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFarmerProfile(int userId)
        {
            try
            {
                // 检查用户是否存在且是农户
                var user = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "farmer");

                if (user == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户资料
                var profile = user.FarmerProfile;
                
                if (profile == null)
                {
                    return Ok(new
                    {
                        code = 200,
                        message = "农户尚未创建资料",
                        data = new { userId = user.UserId }
                    });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取农户资料成功",
                    data = new
                    {
                        userId = user.UserId,
                        farmName = profile.FarmName,
                        location = profile.Location,
                        description = profile.Description,
                        productCategory = profile.ProductCategory,
                        licenseNumber = profile.LicenseNumber,
                        logoUrl = profile.LogoUrl,
                        establishedDate = profile.EstablishedDate,
                        createTime = profile.CreateTime,
                        updateTime = profile.UpdateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 创建或更新农户资料
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveFarmerProfile([FromBody] FarmerProfileRequest request)
        {
            try
            {
                // 检查请求是否有效
                if (request == null || request.UserId <= 0)
                {
                    return BadRequest(new { code = 400, message = "请求数据无效" });
                }

                // 检查用户是否存在且是农户
                var user = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId && u.Role == "farmer");

                if (user == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 检查当前用户是否有权限修改该农户资料
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != request.UserId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限修改此农户资料" });
                }

                // 创建或更新农户资料
                if (user.FarmerProfile == null)
                {
                    // 创建新的农户资料
                    var newProfile = new FarmerProfile
                    {
                        UserId = request.UserId,
                        FarmName = request.FarmName,
                        Location = request.Location,
                        Description = request.Description ?? string.Empty,
                        ProductCategory = request.ProductCategory ?? string.Empty,
                        LicenseNumber = request.LicenseNumber ?? string.Empty,
                        LogoUrl = request.LogoUrl ?? "/images/default-farm-logo.png",
                        EstablishedDate = request.EstablishedDate,
                        CreateTime = DateTime.Now
                    };

                    _context.FarmerProfiles.Add(newProfile);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        code = 200,
                        message = "创建农户资料成功",
                        data = new
                        {
                            userId = newProfile.UserId,
                            farmName = newProfile.FarmName,
                            location = newProfile.Location,
                            description = newProfile.Description,
                            productCategory = newProfile.ProductCategory,
                            licenseNumber = newProfile.LicenseNumber,
                            logoUrl = newProfile.LogoUrl,
                            establishedDate = newProfile.EstablishedDate,
                            createTime = newProfile.CreateTime
                        }
                    });
                }
                else
                {
                    // 更新现有农户资料
                    var profile = user.FarmerProfile;
                    
                    profile.FarmName = request.FarmName;
                    profile.Location = request.Location;
                    profile.Description = request.Description ?? profile.Description;
                    profile.ProductCategory = request.ProductCategory ?? profile.ProductCategory;
                    profile.LicenseNumber = request.LicenseNumber ?? profile.LicenseNumber;
                    profile.LogoUrl = request.LogoUrl ?? profile.LogoUrl;
                    profile.EstablishedDate = request.EstablishedDate ?? profile.EstablishedDate;
                    profile.UpdateTime = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        code = 200,
                        message = "更新农户资料成功",
                        data = new
                        {
                            userId = profile.UserId,
                            farmName = profile.FarmName,
                            location = profile.Location,
                            description = profile.Description,
                            productCategory = profile.ProductCategory,
                            licenseNumber = profile.LicenseNumber,
                            logoUrl = profile.LogoUrl,
                            establishedDate = profile.EstablishedDate,
                            createTime = profile.CreateTime,
                            updateTime = profile.UpdateTime
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新农户LOGO
        /// </summary>
        [HttpPost("logo")]
        public async Task<IActionResult> UpdateLogo([FromForm] int userId, IFormFile file)
        {
            try
            {
                // 检查用户是否存在且是农户
                var user = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "farmer");

                if (user == null || user.FarmerProfile == null)
                {
                    return NotFound(new { code = 404, message = "农户或农户资料不存在" });
                }

                // 检查当前用户是否有权限修改该农户资料
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限修改此农户资料" });
                }

                // 检查文件
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { code = 400, message = "请选择文件" });
                }

                // 检查文件类型
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                {
                    return BadRequest(new { code = 400, message = "只支持JPG或PNG格式的图片" });
                }

                // 检查文件大小
                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest(new { code = 400, message = "文件大小不能超过5MB" });
                }

                // 生成文件名
                var fileName = $"farm_{userId}_{DateTime.Now.Ticks}{fileExtension}";
                var filePath = Path.Combine("wwwroot", "uploads", "logos", fileName);

                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 更新数据库
                var profile = user.FarmerProfile;
                profile.LogoUrl = $"/uploads/logos/{fileName}";
                profile.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "上传LOGO成功",
                    data = new
                    {
                        logoUrl = profile.LogoUrl
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 农户资料请求模型
    /// </summary>
    public class FarmerProfileRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// 农场名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FarmName { get; set; } = string.Empty;

        /// <summary>
        /// 农场位置
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 农场简介
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// 主营产品类别
        /// </summary>
        [StringLength(100)]
        public string? ProductCategory { get; set; }

        /// <summary>
        /// 农场成立时间
        /// </summary>
        public DateTime? EstablishedDate { get; set; }

        /// <summary>
        /// 农场营业执照号
        /// </summary>
        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        /// <summary>
        /// 农场LOGO或照片URL
        /// </summary>
        [StringLength(255)]
        public string? LogoUrl { get; set; }
    }
} 
 
 