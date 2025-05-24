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
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null, [FromQuery] string? role = null)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 构建查询
                var query = _context.Users.Include(u => u.FarmerProfile).AsQueryable();

                // 关键词搜索
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.Username.Contains(keyword) || 
                                           (u.Email != null && u.Email.Contains(keyword)) || 
                                           (u.Phone != null && u.Phone.Contains(keyword)));
                }

                // 角色筛选
                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.Role == role);
                }

                // 获取总数
                var totalUsers = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

                // 分页查询
                var users = await query
                    .OrderByDescending(u => u.CreateTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取用户列表成功",
                    data = new
                    {
                        users = users.Select(u => new
                        {
                            u.UserId,
                            u.Username,
                            u.Email,
                            u.Phone,
                            u.Role,
                            u.CreateTime,
                            u.LastLoginTime,
                            // 农户额外信息
                            FarmName = u.FarmerProfile?.FarmName,
                            FarmLocation = u.FarmerProfile?.Location,
                            ProductCategory = u.FarmerProfile?.ProductCategory
                        }),
                        totalUsers,
                        totalPages,
                        currentPage = page,
                        pageSize
                    }
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

        /// <summary>
        /// 获取产品类别分布
        /// </summary>
        [HttpGet("products/categories")]
        public async Task<IActionResult> GetProductCategories()
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取产品类别分布
                var productCategories = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取产品类别分布成功",
                    data = productCategories
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 按类别统计产品数
        /// </summary>
        [HttpGet("products/category/{category}")]
        public async Task<IActionResult> GetProductsByCategory(string category)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取指定类别的所有产品
                var products = await _context.Products.Where(p => p.Category == category).ToListAsync();

                // 按类别统计产品数
                var productsByCategory = products
                    .GroupBy(p => p.Category)
                    .Select(g => new
                    {
                        Category = g.Key ?? "未分类",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return Ok(new
                {
                    code = 200,
                    message = $"获取{category}类别的产品列表成功",
                    data = productsByCategory
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取用户详情（仅管理员）
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserDetail(int userId)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取用户详情，包含农户资料
                var user = await _context.Users
                    .Include(u => u.FarmerProfile)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取用户详情成功",
                    data = new
                    {
                        // 基本信息
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.Phone,
                        user.Role,
                        user.CreateTime,
                        user.LastLoginTime,
                        
                        // 农户资料（如果是农户）
                        FarmerProfile = user.FarmerProfile == null ? null : new
                        {
                            user.FarmerProfile.FarmName,
                            user.FarmerProfile.Location,
                            user.FarmerProfile.Description,
                            user.FarmerProfile.ProductCategory,
                            user.FarmerProfile.LicenseNumber,
                            user.FarmerProfile.EstablishedDate,
                            user.FarmerProfile.CreateTime,
                            user.FarmerProfile.UpdateTime
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新用户信息（仅管理员）
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                // 验证当前用户是否为管理员
                var currentUser = HttpContext.Items["User"] as User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限访问此接口" });
                }

                // 获取用户
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 防止管理员修改自己的角色
                if (userId == currentUser.UserId && request.Role != null && request.Role != user.Role)
                {
                    return BadRequest(new { code = 400, message = "不能修改自己的角色" });
                }

                // 更新用户信息
                if (request.Email != null) user.Email = request.Email;
                if (request.Phone != null) user.Phone = request.Phone;
                if (request.Role != null) user.Role = request.Role;

                // 如果提供了新密码，更新密码
                if (!string.IsNullOrEmpty(request.Password))
                {
                    await _userService.ChangePasswordAsync(userId, request.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    code = 200,
                    message = "用户更新成功",
                    data = new
                    {
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.Phone,
                        user.Role
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

    /// <summary>
    /// 更新用户请求模型
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// 新邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 新电话
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 新角色
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 新密码
        /// </summary>
        public string? Password { get; set; }
    }
} 