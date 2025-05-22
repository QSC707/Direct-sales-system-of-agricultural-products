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
                    .Where(u => u.UserId == userId && u.Role == "farmer")
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { code = 404, message = "农户不存在" });
                }

                // 获取农户资料 - 显式选择字段，避免SQL Server使用SELECT *
                var profile = await _context.FarmerProfiles
                    .Where(fp => fp.UserId == userId)
                    .Select(fp => new {
                        fp.FarmerProfileId,
                        fp.UserId,
                        fp.FarmName,
                        fp.Location,
                        fp.Description,
                        fp.ProductCategory,
                        fp.LicenseNumber,
                        fp.EstablishedDate,
                        fp.CreateTime,
                        fp.UpdateTime
                    })
                    .FirstOrDefaultAsync();
                
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
                    profile.Description = request.Description ?? string.Empty;
                    profile.ProductCategory = request.ProductCategory ?? string.Empty;
                    profile.LicenseNumber = request.LicenseNumber ?? string.Empty;
                    profile.EstablishedDate = request.EstablishedDate;
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
    }
} 
 
 