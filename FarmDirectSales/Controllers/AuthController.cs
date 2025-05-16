using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Services;
using FarmDirectSales.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 认证控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _userService.RegisterAsync(request.Username, request.Password, request.Role);
                return Ok(new { code = 200, message = "注册成功", data = user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var (user, token) = await _userService.LoginAsync(request.Username, request.Password);
                
                // 返回更完整的用户数据
                return Ok(new { 
                    code = 200, 
                    message = "登录成功", 
                    data = new { 
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email ?? "",
                        phone = user.Phone ?? "",
                        role = user.Role,
                        token = token 
                    } 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 获取用户信息
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }
                
                return Ok(new { 
                    code = 200, 
                    message = "获取用户信息成功", 
                    data = new {
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email ?? "",
                        phone = user.Phone ?? "",
                        role = user.Role,
                        createTime = user.CreateTime,
                        lastLoginTime = user.LastLoginTime
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
        
        /// <summary>
        /// 更新用户信息
        /// </summary>
        [HttpPut("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUserInfo(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }
                
                // 只更新提供的字段，不影响其他字段
                if (request.Username != null)
                {
                    user.Username = request.Username;
                }
                
                // 对于邮箱和电话，如果提供了null，表示删除当前值
                user.Email = request.Email;
                user.Phone = request.Phone;
                
                // 我们不更新密码，保持现有密码不变
                
                var updatedUser = await _userService.UpdateUserAsync(user);
                
                return Ok(new { 
                    code = 200, 
                    message = "更新用户信息成功", 
                    data = new {
                        userId = updatedUser.UserId,
                        username = updatedUser.Username,
                        email = updatedUser.Email ?? "",
                        phone = updatedUser.Phone ?? "",
                        role = updatedUser.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        [HttpPut("user/{userId}/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                // 验证当前用户是否有权限修改此用户的密码
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限修改此用户密码" });
                }

                // 获取用户
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 验证旧密码（管理员可以跳过此步骤）
                if (currentUser.Role != "admin")
                {
                    var loginResult = await _userService.ValidatePasswordAsync(userId, request.OldPassword);
                    if (!loginResult)
                    {
                        return BadRequest(new { code = 400, message = "旧密码不正确" });
                    }
                }

                // 更新密码
                await _userService.ChangePasswordAsync(userId, request.NewPassword);

                return Ok(new
                {
                    code = 200,
                    message = "密码修改成功"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }

    /// <summary>
    /// 注册请求模型
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色
        /// </summary>
        [Required(ErrorMessage = "角色不能为空")]
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登录请求模型
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 更新用户信息请求模型
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }
        
        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email { get; set; }
        
        /// <summary>
        /// 手机号
        /// </summary>
        [Phone(ErrorMessage = "手机号格式不正确")]
        public string? Phone { get; set; }
    }

    /// <summary>
    /// 修改密码请求模型
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// 旧密码
        /// </summary>
        public string? OldPassword { get; set; }

        /// <summary>
        /// 新密码
        /// </summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")]
        public string NewPassword { get; set; } = string.Empty;
    }
} 