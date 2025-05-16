using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Services;
using FarmDirectSales.Data;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 日志控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogController(ILogService logService, ApplicationDbContext context)
        {
            _logService = logService;
            _context = context;
        }

        /// <summary>
        /// 获取用户日志
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLogs(int userId, [FromQuery] int pageSize = 50, [FromQuery] int pageIndex = 0)
        {
            try
            {
                // 检查用户是否存在
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { code = 404, message = "用户不存在" });
                }

                // 只允许管理员和用户本人查看日志
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || (currentUser.UserId != userId && currentUser.Role != "admin"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限查看此日志" });
                }

                var logs = await _logService.GetUserLogs(userId, pageSize, pageIndex);

                return Ok(new
                {
                    code = 200,
                    message = "获取用户日志成功",
                    data = logs.Select(l => new
                    {
                        l.LogId,
                        l.UserId,
                        l.ActionType,
                        l.Description,
                        l.IpAddress,
                        l.TargetId,
                        l.TargetType,
                        l.IsSuccess,
                        l.ActionTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有日志（仅管理员）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllLogs([FromQuery] int pageSize = 50, [FromQuery] int pageIndex = 0)
        {
            try
            {
                // 只允许管理员查看所有日志
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限查看此日志" });
                }

                var logs = await _logService.GetAllLogs(pageSize, pageIndex);

                return Ok(new
                {
                    code = 200,
                    message = "获取所有日志成功",
                    data = logs.Select(l => new
                    {
                        l.LogId,
                        l.UserId,
                        Username = l.User?.Username,
                        l.ActionType,
                        l.Description,
                        l.IpAddress,
                        l.TargetId,
                        l.TargetType,
                        l.IsSuccess,
                        l.ActionTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }

        /// <summary>
        /// 根据操作类型查询日志（仅管理员）
        /// </summary>
        [HttpGet("type/{actionType}")]
        public async Task<IActionResult> GetLogsByActionType(string actionType, [FromQuery] int pageSize = 50, [FromQuery] int pageIndex = 0)
        {
            try
            {
                // 只允许管理员查看日志
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "无权限查看此日志" });
                }

                var logs = await _logService.GetLogsByActionType(actionType, pageSize, pageIndex);

                return Ok(new
                {
                    code = 200,
                    message = "获取日志成功",
                    data = logs.Select(l => new
                    {
                        l.LogId,
                        l.UserId,
                        Username = l.User?.Username,
                        l.ActionType,
                        l.Description,
                        l.IpAddress,
                        l.TargetId,
                        l.TargetType,
                        l.IsSuccess,
                        l.ActionTime
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = ex.Message });
            }
        }
    }
} 
 
 