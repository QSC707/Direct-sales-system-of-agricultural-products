using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        public async Task<int> LogAction(int? userId, string actionType, string description, string ipAddress, int? targetId = null, string? targetType = null, bool isSuccess = true)
        {
            var log = new Log
            {
                UserId = userId,
                ActionType = actionType,
                Description = description,
                IpAddress = ipAddress,
                TargetId = targetId,
                TargetType = targetType,
                IsSuccess = isSuccess,
                ActionTime = DateTime.Now
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
            return log.LogId;
        }

        /// <summary>
        /// 获取用户日志
        /// </summary>
        public async Task<IEnumerable<Log>> GetUserLogs(int userId, int pageSize = 50, int pageIndex = 0)
        {
            return await _context.Logs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.ActionTime)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 获取所有日志
        /// </summary>
        public async Task<IEnumerable<Log>> GetAllLogs(int pageSize = 50, int pageIndex = 0)
        {
            return await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.ActionTime)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// 根据操作类型查询日志
        /// </summary>
        public async Task<IEnumerable<Log>> GetLogsByActionType(string actionType, int pageSize = 50, int pageIndex = 0)
        {
            return await _context.Logs
                .Include(l => l.User)
                .Where(l => l.ActionType == actionType)
                .OrderByDescending(l => l.ActionTime)
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
} 
 
 