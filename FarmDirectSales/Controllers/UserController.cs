using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FarmDirectSales.Data;
using FarmDirectSales.Models;

namespace FarmDirectSales.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly ApplicationDbContext _context;

        public UserController(ILogger<UserController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 临时API: 修改用户角色(仅用于测试)
        /// </summary>
        [HttpPut("change-role/{userId}")]
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                // 查找用户
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }
                
                // 记录原始角色
                var originalRole = user.Role;
                
                // 修改角色
                user.Role = request.NewRole;
                await _context.SaveChangesAsync();
                
                // 记录操作日志
                _logger.LogInformation($"用户角色已修改: UserId={userId}, 原角色={originalRole}, 新角色={request.NewRole}");
                
                return Ok(new 
                { 
                    code = 200, 
                    message = "用户角色已修改", 
                    data = new 
                    { 
                        userId = user.UserId, 
                        username = user.Username,
                        originalRole = originalRole,
                        newRole = user.Role
                    } 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"修改用户角色失败: {ex.Message}");
                return StatusCode(500, new { code = 500, message = $"修改用户角色失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 修改角色请求
        /// </summary>
        public class ChangeRoleRequest
        {
            /// <summary>
            /// 新角色
            /// </summary>
            [Required(ErrorMessage = "新角色不能为空")]
            public string NewRole { get; set; }
        }
    }
} 
 
 