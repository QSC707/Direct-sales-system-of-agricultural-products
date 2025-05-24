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
                // 先查找精确匹配的配送区域
                var area = await _context.DeliveryAreas
                    .Where(a => a.Province == request.Province && 
                           a.City == request.City && 
                           a.District == request.District)
                    .FirstOrDefaultAsync();

                // 如果没找到精确匹配，尝试按层级查找
                if (area == null)
                {
                    // 尝试查找市级匹配
                    area = await _context.DeliveryAreas
                        .Where(a => a.Province == request.Province && 
                               a.City == request.City && 
                               string.IsNullOrEmpty(a.District))
                        .FirstOrDefaultAsync();

                    // 尝试查找省级匹配
                    if (area == null)
                    {
                        area = await _context.DeliveryAreas
                            .Where(a => a.Province == request.Province && 
                                   string.IsNullOrEmpty(a.City))
                            .FirstOrDefaultAsync();
                        
                        // 最后尝试查找全国配送
                        if (area == null)
                        {
                            area = await _context.DeliveryAreas
                                .Where(a => a.IsNationwide)
                                .FirstOrDefaultAsync();
                        }
                    }
                }

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

                // 验证至少有一个区域信息或选择了全国配送
                if (!request.IsNationwide && string.IsNullOrWhiteSpace(request.Province) && 
                    string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.District))
                {
                    return BadRequest(new { code = 400, message = "请填写至少一个地区信息或选择全国配送" });
                }

                // 如果是全国配送，清空区域信息
                if (request.IsNationwide)
                {
                    request.Province = string.Empty;
                    request.City = string.Empty;
                    request.District = string.Empty;
                }

                // 检查是否已存在相同区域
                var query = _context.DeliveryAreas.AsQueryable();

                if (request.IsNationwide)
                {
                    // 检查是否已存在全国配送
                    var existingArea = await query.FirstOrDefaultAsync(a => a.IsNationwide);
                    if (existingArea != null)
                    {
                        return BadRequest(new { code = 400, message = "已存在全国配送区域" });
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.Province))
                {
                    query = query.Where(a => a.Province == request.Province);
                    
                    if (!string.IsNullOrWhiteSpace(request.City))
                    {
                        query = query.Where(a => a.City == request.City);
                        
                        if (!string.IsNullOrWhiteSpace(request.District))
                        {
                            query = query.Where(a => a.District == request.District);
                        }
                    }
                    
                    var existingArea = await query.FirstOrDefaultAsync();
                if (existingArea != null)
                {
                    return BadRequest(new { code = 400, message = "该配送区域已存在" });
                    }
                }

                // 创建新配送区域
                var newArea = new DeliveryArea
                {
                    IsNationwide = request.IsNationwide,
                    Province = request.Province,
                    City = request.City,
                    District = request.District,
                    SupportSameDayDelivery = request.SupportSameDayDelivery,
                    DeliveryFee = request.DeliveryFee,
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

                // 验证至少有一个区域信息或选择了全国配送
                if (!request.IsNationwide && string.IsNullOrWhiteSpace(request.Province) && 
                    string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.District))
                {
                    return BadRequest(new { code = 400, message = "请填写至少一个地区信息或选择全国配送" });
                }

                // 查找配送区域
                var area = await _context.DeliveryAreas.FindAsync(areaId);
                if (area == null)
                {
                    return NotFound(new { code = 404, message = "配送区域不存在" });
                }

                // 如果是全国配送，清空区域信息
                if (request.IsNationwide)
                {
                    request.Province = string.Empty;
                    request.City = string.Empty;
                    request.District = string.Empty;
                }

                // 检查是否与其他区域冲突
                if (request.IsNationwide)
                {
                    // 检查是否已存在其他全国配送区域
                    var existingArea = await _context.DeliveryAreas
                        .Where(a => a.DeliveryAreaId != areaId && a.IsNationwide)
                        .FirstOrDefaultAsync();
                    
                    if (existingArea != null)
                    {
                        return BadRequest(new { code = 400, message = "已存在全国配送区域" });
                    }
                }
                else if (!string.IsNullOrWhiteSpace(request.Province))
                {
                    var query = _context.DeliveryAreas
                        .Where(a => a.DeliveryAreaId != areaId && a.Province == request.Province);
                    
                    if (!string.IsNullOrWhiteSpace(request.City))
                    {
                        query = query.Where(a => a.City == request.City);
                        
                        if (!string.IsNullOrWhiteSpace(request.District))
                        {
                            query = query.Where(a => a.District == request.District);
                        }
                    }
                    
                    var existingArea = await query.FirstOrDefaultAsync();
                    if (existingArea != null)
                    {
                        return BadRequest(new { code = 400, message = "该配送区域已存在" });
                    }
                }

                // 更新配送区域信息
                area.IsNationwide = request.IsNationwide;
                area.Province = request.Province;
                area.City = request.City;
                area.District = request.District;
                area.SupportSameDayDelivery = request.SupportSameDayDelivery;
                area.DeliveryFee = request.DeliveryFee;
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
                bool hasNationwide = await _context.DeliveryAreas.AnyAsync(a => a.IsNationwide);

                foreach (var request in requests)
                {
                    // 验证至少有一个区域信息或选择了全国配送
                    if (!request.IsNationwide && string.IsNullOrWhiteSpace(request.Province) && 
                        string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.District))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    // 如果是全国配送且已存在全国配送区域，则跳过
                    if (request.IsNationwide && hasNationwide)
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    // 如果是全国配送，清空区域信息
                    if (request.IsNationwide)
                    {
                        request.Province = string.Empty;
                        request.City = string.Empty;
                        request.District = string.Empty;
                    }

                    // 检查是否已存在相同区域
                    var query = _context.DeliveryAreas.AsQueryable();

                    if (!request.IsNationwide && !string.IsNullOrWhiteSpace(request.Province))
                    {
                        query = query.Where(a => a.Province == request.Province);
                        
                        if (!string.IsNullOrWhiteSpace(request.City))
                        {
                            query = query.Where(a => a.City == request.City);
                            
                            if (!string.IsNullOrWhiteSpace(request.District))
                            {
                                query = query.Where(a => a.District == request.District);
                            }
                        }
                        
                        var existingArea = await query.FirstOrDefaultAsync();
                        if (existingArea != null)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    // 创建新配送区域
                    var newArea = new DeliveryArea
                    {
                        IsNationwide = request.IsNationwide,
                        Province = request.Province,
                        City = request.City,
                        District = request.District,
                        SupportSameDayDelivery = request.SupportSameDayDelivery,
                        DeliveryFee = request.DeliveryFee,
                        CreateTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                        UpdateBy = currentUser.UserId
                    };

                    _context.DeliveryAreas.Add(newArea);
                    addedCount++;
                    
                    // 如果添加了全国配送区域，记录标志
                    if (request.IsNationwide)
                    {
                        hasNationwide = true;
                    }
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


    }

    /// <summary>
    /// 添加配送区域请求模型
    /// </summary>
    public class AddDeliveryAreaRequest
    {
        /// <summary>
        /// 是否全国配送（不限制区域）
        /// </summary>
        public bool IsNationwide { get; set; } = false;
        
        /// <summary>
        /// 省份
        /// </summary>
        [StringLength(20, ErrorMessage = "省份名称最多20个字符")]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [StringLength(20, ErrorMessage = "城市名称最多20个字符")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
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
        /// 是否全国配送（不限制区域）
        /// </summary>
        public bool IsNationwide { get; set; } = false;
        
        /// <summary>
        /// 省份
        /// </summary>
        [StringLength(20, ErrorMessage = "省份名称最多20个字符")]
        public string Province { get; set; } = string.Empty;

        /// <summary>
        /// 城市
        /// </summary>
        [StringLength(20, ErrorMessage = "城市名称最多20个字符")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// 区/县
        /// </summary>
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