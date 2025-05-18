using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Models;
using FarmDirectSales.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 配送区域控制器
    /// </summary>
    [ApiController]
    [Route("api/delivery-area")]
    public class DeliveryAreaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeliveryAreaController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 获取所有配送区域
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDeliveryAreas()
        {
            try
            {
                var areas = await _context.DeliveryAreas
                    .OrderBy(a => a.Province)
                    .ThenBy(a => a.City)
                    .ThenBy(a => a.District)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取配送区域列表成功",
                    data = areas
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取支持当天配送的区域
        /// </summary>
        [HttpGet("sameday")]
        public async Task<IActionResult> GetSameDayDeliveryAreas()
        {
            try
            {
                var areas = await _context.DeliveryAreas
                    .Where(a => a.SupportSameDayDelivery)
                    .OrderBy(a => a.Province)
                    .ThenBy(a => a.City)
                    .ThenBy(a => a.District)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取当天配送区域成功",
                    data = areas
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 检查地址是否支持当天配送
        /// </summary>
        [HttpGet("check")]
        public async Task<IActionResult> CheckDeliveryAvailability([FromQuery] CheckDeliveryRequest request)
        {
            try
            {
                // 查找匹配的配送区域
                var area = await _context.DeliveryAreas
                    .Where(a => a.Province == request.Province && 
                           a.City == request.City && 
                           a.District == request.District)
                    .FirstOrDefaultAsync();

                bool isSameDayAvailable = false;
                decimal deliveryFee = 0;
                string message = "该地区不支持当天配送";

                if (area != null && area.SupportSameDayDelivery)
                {
                    isSameDayAvailable = true;
                    deliveryFee = area.DeliveryFee;
                    message = "该地区支持当天配送";
                }

                return Ok(new
                {
                    code = 200,
                    message = message,
                    data = new 
                    {
                        isSameDayAvailable,
                        deliveryFee,
                        area
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 添加配送区域（仅管理员）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddDeliveryArea([FromBody] AddDeliveryAreaRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 检查是否已存在相同区域
                var existingArea = await _context.DeliveryAreas
                    .FirstOrDefaultAsync(a => a.Province == request.Province && 
                                         a.City == request.City && 
                                         a.District == request.District);

                if (existingArea != null)
                {
                    return BadRequest(new { code = 400, message = "该配送区域已存在" });
                }

                // 创建新配送区域
                var newArea = new DeliveryArea
                {
                    Province = request.Province,
                    City = request.City,
                    District = request.District,
                    SupportSameDayDelivery = request.SupportSameDayDelivery,
                    DeliveryFee = request.DeliveryFee,
                    Description = request.Description,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    UpdateBy = currentUser.UserId
                };

                _context.DeliveryAreas.Add(newArea);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "添加配送区域成功",
                    data = newArea
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新配送区域（仅管理员）
        /// </summary>
        [HttpPut("{areaId}")]
        public async Task<IActionResult> UpdateDeliveryArea(int areaId, [FromBody] UpdateDeliveryAreaRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 查找配送区域
                var area = await _context.DeliveryAreas.FindAsync(areaId);
                if (area == null)
                {
                    return NotFound(new { code = 404, message = "配送区域不存在" });
                }

                // 更新配送区域信息
                area.SupportSameDayDelivery = request.SupportSameDayDelivery;
                area.DeliveryFee = request.DeliveryFee;
                area.Description = request.Description;
                area.UpdateTime = DateTime.Now;
                area.UpdateBy = currentUser.UserId;

                _context.DeliveryAreas.Update(area);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "更新配送区域成功",
                    data = area
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除配送区域（仅管理员）
        /// </summary>
        [HttpDelete("{areaId}")]
        public async Task<IActionResult> DeleteDeliveryArea(int areaId)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 查找配送区域
                var area = await _context.DeliveryAreas.FindAsync(areaId);
                if (area == null)
                {
                    return NotFound(new { code = 404, message = "配送区域不存在" });
                }

                // 删除配送区域
                _context.DeliveryAreas.Remove(area);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "删除配送区域成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 批量导入配送区域（仅管理员）
        /// </summary>
        [HttpPost("batch")]
        public async Task<IActionResult> BatchImportDeliveryAreas([FromBody] List<AddDeliveryAreaRequest> requests)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var request in requests)
                {
                    // 检查是否已存在相同区域
                    var existingArea = await _context.DeliveryAreas
                        .FirstOrDefaultAsync(a => a.Province == request.Province && 
                                            a.City == request.City && 
                                            a.District == request.District);

                    if (existingArea != null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // 创建新配送区域
                    var newArea = new DeliveryArea
                    {
                        Province = request.Province,
                        City = request.City,
                        District = request.District,
                        SupportSameDayDelivery = request.SupportSameDayDelivery,
                        DeliveryFee = request.DeliveryFee,
                        Description = request.Description,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    };

                    _context.DeliveryAreas.Add(newArea);
                    addedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"批量导入配送区域成功，添加 {addedCount} 个，跳过 {skippedCount} 个",
                    data = new { addedCount, skippedCount }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 初始化默认配送区域（仅管理员）
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeDefaultAreas()
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 定义默认配送区域
                var defaultAreas = new List<DeliveryArea>
                {
                    // 广州
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "广州市", 
                        District = "天河区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 15.0m,
                        Description = "当天2小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "广州市", 
                        District = "越秀区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 15.0m,
                        Description = "当天2小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "广州市", 
                        District = "海珠区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 15.0m,
                        Description = "当天2小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "广州市", 
                        District = "荔湾区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 15.0m,
                        Description = "当天2小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "广州市", 
                        District = "白云区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 15.0m,
                        Description = "当天2小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    
                    // 深圳
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "深圳市", 
                        District = "福田区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 20.0m,
                        Description = "当天4小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "深圳市", 
                        District = "南山区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 20.0m,
                        Description = "当天4小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "深圳市", 
                        District = "罗湖区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 20.0m,
                        Description = "当天4小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    },
                    new DeliveryArea 
                    { 
                        Province = "广东省", 
                        City = "深圳市", 
                        District = "宝安区", 
                        SupportSameDayDelivery = true, 
                        DeliveryFee = 20.0m,
                        Description = "当天4小时内配送",
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    }
                };

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var area in defaultAreas)
                {
                    // 检查是否已存在相同区域
                    var existingArea = await _context.DeliveryAreas
                        .FirstOrDefaultAsync(a => a.Province == area.Province && 
                                            a.City == area.City && 
                                            a.District == area.District);

                    if (existingArea != null)
                    {
                        skippedCount++;
                        continue;
                    }

                    _context.DeliveryAreas.Add(area);
                    addedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"初始化配送区域成功，添加 {addedCount} 个，跳过 {skippedCount} 个",
                    data = new { addedCount, skippedCount }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 添加配送区域请求模型
    /// </summary>
    public class AddDeliveryAreaRequest
    {
        /// <summary>
        /// 省份
        /// </summary>
        [Required(ErrorMessage = "省份不能为空")]
        [StringLength(20, ErrorMessage = "省份名称最多20个字符")]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [Required(ErrorMessage = "城市不能为空")]
        [StringLength(20, ErrorMessage = "城市名称最多20个字符")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
        [Required(ErrorMessage = "区/县不能为空")]
        [StringLength(20, ErrorMessage = "区/县名称最多20个字符")]
        public string District { get; set; } = string.Empty;

        /// <summary>
        /// 是否支持当天配送
        /// </summary>
        public bool SupportSameDayDelivery { get; set; } = true;

        /// <summary>
        /// 配送费用
        /// </summary>
        [Range(0, 1000, ErrorMessage = "配送费用必须在0-1000之间")]
        public decimal DeliveryFee { get; set; } = 15.0m;

        /// <summary>
        /// 配送说明
        /// </summary>
        [StringLength(200, ErrorMessage = "配送说明最多200个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新配送区域请求模型
    /// </summary>
    public class UpdateDeliveryAreaRequest
    {
        /// <summary>
        /// 是否支持当天配送
        /// </summary>
        public bool SupportSameDayDelivery { get; set; } = true;

        /// <summary>
        /// 配送费用
        /// </summary>
        [Range(0, 1000, ErrorMessage = "配送费用必须在0-1000之间")]
        public decimal DeliveryFee { get; set; } = 15.0m;

        /// <summary>
        /// 配送说明
        /// </summary>
        [StringLength(200, ErrorMessage = "配送说明最多200个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 检查配送区域请求模型
    /// </summary>
    public class CheckDeliveryRequest
    {
        /// <summary>
        /// 省份
        /// </summary>
        [Required(ErrorMessage = "省份不能为空")]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [Required(ErrorMessage = "城市不能为空")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
        [Required(ErrorMessage = "区/县不能为空")]
        public string District { get; set; } = string.Empty;
    }
} 