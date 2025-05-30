using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using FarmDirectSales.Models.DTOs;
using FarmDirectSales.Services;
using System.Security.Claims;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 配送预设管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryPresetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogService _logService;

        public DeliveryPresetController(ApplicationDbContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        /// <summary>
        /// 获取农户的配送预设列表
        /// </summary>
        /// <param name="farmerId">农户ID</param>
        /// <returns>配送预设列表</returns>
        [HttpGet("farmer/{farmerId}")]
        public async Task<IActionResult> GetFarmerDeliveryPresets(int farmerId)
        {
            try
            {
                // 验证当前用户是否为该农户
                var currentUserId = GetCurrentUserId();
                
                if (currentUserId == 0)
                {
                    return Unauthorized("用户未认证");
                }
                
                if (currentUserId != farmerId)
                {
                    return StatusCode(403, "只能查看自己的配送预设");
                }

                // 验证农户是否存在
                var farmer = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == farmerId && u.Role == "farmer");
                
                if (farmer == null)
                {
                    return NotFound("农户不存在");
                }

                // 获取农户的配送预设
                var presets = await _context.DeliveryPresets
                    .Where(p => p.FarmerId == farmerId)
                    .OrderByDescending(p => p.IsUserDefault)
                    .ThenByDescending(p => p.CreateTime)
                    .Select(p => new
                    {
                        p.PresetId,
                        p.PresetName,
                        p.DeliveryInfo,
                        p.DeliveryContact,
                        p.DeliveryPhone,
                        p.EstimatedDeliveryTime,
                        p.IsUserDefault,
                        p.CreateTime
                    })
                    .ToListAsync();

                // 如果没有预设，返回系统默认预设
                if (!presets.Any())
                {
                    var systemDefaults = new[]
                    {
                        new
                        {
                            PresetId = 0,
                            PresetName = "标准配送",
                            DeliveryInfo = "货到付款，请保持电话畅通，配送员会提前联系您确认收货时间。",
                            DeliveryContact = "配送员",
                            DeliveryPhone = "将在配送前联系",
                            EstimatedDeliveryTime = "1-3个工作日内送达",
                            IsUserDefault = false,
                            CreateTime = DateTime.Now
                        }
                    };
                    return Ok(systemDefaults);
                }

                await _logService.LogAction(currentUserId, "获取配送预设", "获取配送预设列表", GetClientIPAddress(), farmerId, "DeliveryPreset", true);
                return Ok(presets);
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId > 0)
                {
                    await _logService.LogAction(currentUserId, "获取配送预设", $"获取配送预设失败: {ex.Message}", GetClientIPAddress(), farmerId, "DeliveryPreset", false);
                }
                return StatusCode(500, "获取配送预设失败");
            }
        }

        /// <summary>
        /// 创建配送预设
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> CreateDeliveryPreset([FromBody] CreateDeliveryPresetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // 验证当前用户是否为农户
                var farmer = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == currentUserId && u.Role == "farmer");
                
                if (farmer == null)
                {
                    return StatusCode(403, "只有农户可以创建配送预设");
                }

                // 检查预设名称是否重复
                var existingPreset = await _context.DeliveryPresets
                    .FirstOrDefaultAsync(p => p.FarmerId == currentUserId && p.PresetName == request.PresetName);
                
                if (existingPreset != null)
                {
                    return BadRequest("预设名称已存在");
                }

                // 如果设为默认，先取消其他默认预设
                if (request.SetAsDefault)
                {
                    var defaultPresets = await _context.DeliveryPresets
                        .Where(p => p.FarmerId == currentUserId && p.IsUserDefault)
                        .ToListAsync();
                    
                    foreach (var preset in defaultPresets)
                    {
                        preset.IsUserDefault = false;
                        preset.UpdateTime = DateTime.Now;
                    }
                }

                // 创建新预设
                var newPreset = new DeliveryPreset
                {
                    FarmerId = currentUserId,
                    PresetName = request.PresetName,
                    DeliveryInfo = request.DeliveryInfo,
                    DeliveryContact = request.DeliveryContact,
                    DeliveryPhone = request.DeliveryPhone,
                    EstimatedDeliveryTime = request.EstimatedDeliveryTime,
                    IsUserDefault = request.SetAsDefault,
                    CreateTime = DateTime.Now
                };

                _context.DeliveryPresets.Add(newPreset);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, "创建配送预设", $"创建配送预设: {request.PresetName}", GetClientIPAddress(), newPreset.PresetId, "DeliveryPreset", true);
                
                return Ok(new { message = "配送预设创建成功", presetId = newPreset.PresetId });
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                await _logService.LogAction(currentUserId, "创建配送预设", $"创建配送预设失败: {ex.Message}", GetClientIPAddress(), 0, "DeliveryPreset", false);
                return StatusCode(500, "创建配送预设失败");
            }
        }

        /// <summary>
        /// 更新配送预设
        /// </summary>
        /// <param name="presetId">预设ID</param>
        /// <param name="request">更新请求</param>
        /// <returns>更新结果</returns>
        [HttpPut("{presetId}")]
        public async Task<IActionResult> UpdateDeliveryPreset(int presetId, [FromBody] UpdateDeliveryPresetRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // 查找预设
                var preset = await _context.DeliveryPresets
                    .FirstOrDefaultAsync(p => p.PresetId == presetId && p.FarmerId == currentUserId);
                
                if (preset == null)
                {
                    return NotFound("配送预设不存在");
                }

                // 检查预设名称是否重复（除了当前预设）
                var existingPreset = await _context.DeliveryPresets
                    .FirstOrDefaultAsync(p => p.FarmerId == currentUserId && 
                                           p.PresetName == request.PresetName && 
                                           p.PresetId != presetId);
                
                if (existingPreset != null)
                {
                    return BadRequest("预设名称已存在");
                }

                // 如果设为默认，先取消其他默认预设
                if (request.SetAsDefault && !preset.IsUserDefault)
                {
                    var defaultPresets = await _context.DeliveryPresets
                        .Where(p => p.FarmerId == currentUserId && p.IsUserDefault)
                        .ToListAsync();
                    
                    foreach (var defaultPreset in defaultPresets)
                    {
                        defaultPreset.IsUserDefault = false;
                        defaultPreset.UpdateTime = DateTime.Now;
                    }
                }

                // 更新预设信息
                preset.PresetName = request.PresetName;
                preset.DeliveryInfo = request.DeliveryInfo;
                preset.DeliveryContact = request.DeliveryContact;
                preset.DeliveryPhone = request.DeliveryPhone;
                preset.EstimatedDeliveryTime = request.EstimatedDeliveryTime;
                preset.IsUserDefault = request.SetAsDefault;
                preset.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, "更新配送预设", $"更新配送预设: {request.PresetName}", GetClientIPAddress(), presetId, "DeliveryPreset", true);
                
                return Ok(new { message = "配送预设更新成功" });
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                await _logService.LogAction(currentUserId, "更新配送预设", $"更新配送预设失败: {ex.Message}", GetClientIPAddress(), presetId, "DeliveryPreset", false);
                return StatusCode(500, "更新配送预设失败");
            }
        }

        /// <summary>
        /// 删除配送预设
        /// </summary>
        /// <param name="presetId">预设ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{presetId}")]
        public async Task<IActionResult> DeleteDeliveryPreset(int presetId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // 查找预设
                var preset = await _context.DeliveryPresets
                    .FirstOrDefaultAsync(p => p.PresetId == presetId && p.FarmerId == currentUserId);
                
                if (preset == null)
                {
                    return NotFound("配送预设不存在");
                }

                var presetName = preset.PresetName;
                
                _context.DeliveryPresets.Remove(preset);
                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, "删除配送预设", $"删除配送预设: {presetName}", GetClientIPAddress(), presetId, "DeliveryPreset", true);
                
                return Ok(new { message = "配送预设删除成功" });
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                await _logService.LogAction(currentUserId, "删除配送预设", $"删除配送预设失败: {ex.Message}", GetClientIPAddress(), presetId, "DeliveryPreset", false);
                return StatusCode(500, "删除配送预设失败");
            }
        }

        /// <summary>
        /// 设置默认配送预设
        /// </summary>
        /// <param name="presetId">预设ID</param>
        /// <returns>设置结果</returns>
        [HttpPost("{presetId}/set-default")]
        public async Task<IActionResult> SetDefaultPreset(int presetId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // 查找预设
                var preset = await _context.DeliveryPresets
                    .FirstOrDefaultAsync(p => p.PresetId == presetId && p.FarmerId == currentUserId);
                
                if (preset == null)
                {
                    return NotFound("配送预设不存在");
                }

                // 取消其他默认预设
                var defaultPresets = await _context.DeliveryPresets
                    .Where(p => p.FarmerId == currentUserId && p.IsUserDefault)
                    .ToListAsync();
                
                foreach (var defaultPreset in defaultPresets)
                {
                    defaultPreset.IsUserDefault = false;
                    defaultPreset.UpdateTime = DateTime.Now;
                }

                // 设置新的默认预设
                preset.IsUserDefault = true;
                preset.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                await _logService.LogAction(currentUserId, "设置默认预设", $"设置默认配送预设: {preset.PresetName}", GetClientIPAddress(), presetId, "DeliveryPreset", true);
                
                return Ok(new { message = "默认配送预设设置成功" });
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                await _logService.LogAction(currentUserId, "设置默认预设", $"设置默认预设失败: {ex.Message}", GetClientIPAddress(), presetId, "DeliveryPreset", false);
                return StatusCode(500, "设置默认预设失败");
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        /// <returns>用户ID</returns>
        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // 如果NameIdentifier为空，尝试其他可能的claims
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    userIdClaim = User.FindFirst("userId")?.Value ?? User.FindFirst("id")?.Value;
                }
                
                return int.TryParse(userIdClaim, out int userId) ? userId : 0;
            }
            catch (Exception ex)
            {
                // 记录错误但不暴露详细信息
                Console.WriteLine($"获取用户ID时发生异常: {ex.Message}");
                return 0;
            }
        }

        private string GetClientIPAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
} 