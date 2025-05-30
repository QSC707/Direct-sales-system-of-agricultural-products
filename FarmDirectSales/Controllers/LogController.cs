using Microsoft.AspNetCore.Mvc;
using FarmDirectSales.Services;
using FarmDirectSales.Data;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 系统日志管理控制器 - 简化版
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
        /// 获取日志列表 - 简化版
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 验证管理员权限
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "需要管理员权限" });
                }

                // 参数验证
                pageSize = Math.Max(1, Math.Min(pageSize, 100));
                page = Math.Max(1, page);

                // 构建查询
                var query = _context.Logs.Include(l => l.User).AsQueryable();

                // 应用筛选条件
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(l =>
                        l.Description.Contains(keyword) ||
                        l.ActionType.Contains(keyword) ||
                        (l.User != null && l.User.Username.Contains(keyword)) ||
                        (l.IpAddress != null && l.IpAddress.Contains(keyword)));
                }

                if (!string.IsNullOrEmpty(actionType))
                {
                    query = query.Where(l => l.ActionType.StartsWith(actionType));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(l => l.ActionTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    var endDateInclusive = endDate.Value.AddDays(1);
                    query = query.Where(l => l.ActionTime < endDateInclusive);
                }

                // 获取总数
                var totalCount = await query.CountAsync();

                // 应用分页
                var logs = await query
                    .OrderByDescending(l => l.ActionTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 构建响应数据
                var result = new
                {
                    code = 200,
                    message = "获取日志列表成功",
                    data = new
                    {
                        items = logs.Select(l => new
                        {
                            l.LogId,
                            l.UserId,
                            Username = l.User?.Username ?? "系统",
                            l.ActionType,
                            l.Description,
                            l.IpAddress,
                            l.IsSuccess,
                            l.ActionTime,
                            // 简单的操作类型格式化
                            DisplayType = GetSimpleActionType(l.ActionType),
                            StatusText = l.IsSuccess ? "成功" : "失败"
                        }),
                        pagination = new
                        {
                            currentPage = page,
                            pageSize = pageSize,
                            totalCount = totalCount,
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                        }
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = $"获取日志失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取简单统计信息
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetSimpleStats()
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "需要管理员权限" });
                }

                // 获取今日日志统计
                var today = DateTime.Today;
                var todayLogs = await _context.Logs
                    .Where(l => l.ActionTime >= today)
                    .CountAsync();

                var todayErrors = await _context.Logs
                    .Where(l => l.ActionTime >= today && !l.IsSuccess)
                    .CountAsync();

                // 获取总计统计
                var totalLogs = await _context.Logs.CountAsync();

                return Ok(new
                {
                    code = 200,
                    message = "获取统计成功",
                    data = new
                    {
                        todayLogs,
                        todayErrors,
                        totalLogs
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = $"获取统计失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取日志详情
        /// </summary>
        [HttpGet("{logId}")]
        public async Task<IActionResult> GetLogDetail(int logId)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "需要管理员权限" });
                }

                var log = await _context.Logs
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.LogId == logId);

                if (log == null)
                {
                    return NotFound(new { code = 404, message = "日志不存在" });
                }

                return Ok(new
                {
                    code = 200,
                    message = "获取日志详情成功",
                    data = new
                    {
                        log.LogId,
                        log.UserId,
                        Username = log.User?.Username ?? "系统",
                        log.ActionType,
                        log.Description,
                        log.IpAddress,
                        log.TargetId,
                        log.TargetType,
                        log.IsSuccess,
                        log.ActionTime,
                        DisplayType = GetSimpleActionType(log.ActionType),
                        StatusText = log.IsSuccess ? "成功" : "失败"
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = $"获取日志详情失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 清理旧日志 - 简化版
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupLogs([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var currentUser = HttpContext.Items["User"] as Models.User;
                if (currentUser == null || currentUser.Role != "admin")
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { code = 403, message = "需要管理员权限" });
                }

                // 简化参数验证：最少保留3天，最多365天
                daysToKeep = Math.Max(3, Math.Min(daysToKeep, 365));
                
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                
                // 先查询要删除的日志数量
                var logsToDeleteCount = await _context.Logs
                    .Where(l => l.ActionTime < cutoffDate)
                    .CountAsync();
                
                // 如果没有要删除的日志，直接返回
                if (logsToDeleteCount == 0)
                {
                    return Ok(new
                    {
                        code = 200,
                        message = $"没有需要清理的日志。当前保留{daysToKeep}天内的日志。",
                        data = new { deletedCount = 0, cutoffDate }
                    });
                }

                // 执行删除操作
                var deletedCount = await _context.Logs
                    .Where(l => l.ActionTime < cutoffDate)
                    .ExecuteDeleteAsync();

                // 记录清理操作
                await _logService.LogAction(
                    currentUser.UserId,
                    "SYSTEM:CLEANUP",
                    $"清理了{deletedCount}条超过{daysToKeep}天的日志记录",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    null,
                    "Log",
                    true
                );

                return Ok(new
                {
                    code = 200,
                    message = $"日志清理完成！删除了{deletedCount}条记录",
                    data = new { deletedCount, cutoffDate, daysToKeep }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { code = 400, message = $"清理日志失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 简化的操作类型显示
        /// </summary>
        private string GetSimpleActionType(string actionType)
        {
            if (string.IsNullOrEmpty(actionType)) return "未知操作";

            if (actionType.StartsWith("GET")) return "查询";
            if (actionType.StartsWith("POST")) return "创建";
            if (actionType.StartsWith("PUT")) return "更新";
            if (actionType.StartsWith("DELETE")) return "删除";
            if (actionType.Contains("LOGIN")) return "登录";
            if (actionType.Contains("LOGOUT")) return "退出";

            return "其他";
        }
    }
} 
 
 