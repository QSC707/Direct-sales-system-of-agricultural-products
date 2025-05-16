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
    /// 管理员控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AdminController(IUserService userService, ApplicationDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        /// <summary>
        /// 获取所有用户列表（仅管理员）
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取所有用户
                var users = await _context.Users.ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取用户列表成功",
                    data = users.Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.Phone,
                        u.Role,
                        u.CreateTime,
                        u.LastLoginTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 按角色获取用户列表（仅管理员）
        /// </summary>
        [HttpGet("users/role/{role}")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 验证角色是否有效
                if (role != "admin" && role != "farmer" && role != "user")
                {
                    return BadRequest(new { code = 400, message = "无效的角色类型" });
                }

                // 获取指定角色的用户
                var users = await _context.Users.Where(u => u.Role == role).ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = $"获取{GetRoleDisplayName(role)}列表成功",
                    data = users.Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.Phone,
                        u.Role,
                        u.CreateTime,
                        u.LastLoginTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更改用户角色（仅管理员）
        /// </summary>
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] ChangeRoleRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 验证角色是否有效
                if (request.Role != "admin" && request.Role != "farmer" && request.Role != "user")
                {
                    return BadRequest(new { code = 400, message = "无效的角色类型" });
                }

                // 获取用户
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 防止管理员修改自己的角色
                if (userId == currentUser.UserId)
                {
                    return BadRequest(new { code = 400, message = "不能修改自己的角色" });
                }

                // 更新角色
                user.Role = request.Role;
                await _userService.UpdateUserAsync(user);

                return Ok(new
                {
                    code = 200,
                    message = "更新用户角色成功",
                    data = new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 删除用户（仅管理员）
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 防止删除自己
                if (userId == currentUser.UserId)
                {
                    return BadRequest(new { code = 400, message = "不能删除自己的账户" });
                }

                // 删除用户
                var result = await _userService.DeleteUserAsync(userId);
                if (!result)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "删除用户成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        // 辅助方法：获取角色显示名称
        private string GetRoleDisplayName(string role)
        {
            return role switch
            {
                "admin" => "管理员",
                "farmer" => "农户",
                "user" => "普通用户",
                _ => role
            };
        }
    }

    /// <summary>
    /// 更改角色请求模型
    /// </summary>
    public class ChangeRoleRequest
    {
        /// <summary>
        /// 新角色
        /// </summary>
        [Required(ErrorMessage = "角色不能为空")]
        public string Role { get; set; } = string.Empty;
    }
} 