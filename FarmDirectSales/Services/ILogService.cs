using FarmDirectSales.Models;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录操作日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="actionType">操作类型</param>
        /// <param name="description">操作描述</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="targetId">操作对象ID</param>
        /// <param name="targetType">操作对象类型</param>
        /// <param name="isSuccess">是否成功</param>
        /// <returns>日志ID</returns>
        Task<int> LogAction(int? userId, string actionType, string description, string ipAddress, int? targetId = null, string? targetType = null, bool isSuccess = true);

        /// <summary>
        /// 获取用户日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<Log>> GetUserLogs(int userId, int pageSize = 50, int pageIndex = 0);

        /// <summary>
        /// 获取所有日志
        /// </summary>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">页码</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<Log>> GetAllLogs(int pageSize = 50, int pageIndex = 0);

        /// <summary>
        /// 根据操作类型查询日志
        /// </summary>
        /// <param name="actionType">操作类型</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="pageIndex">页码</param>
        /// <returns>日志列表</returns>
        Task<IEnumerable<Log>> GetLogsByActionType(string actionType, int pageSize = 50, int pageIndex = 0);
    }
} 
 
 