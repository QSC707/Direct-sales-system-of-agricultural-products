using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FarmDirectSales.Models;
using FarmDirectSales.Services;
using System.Security.Claims;

namespace FarmDirectSales.Controllers
{
    /// <summary>
    /// 销售统计分析控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,farmer")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;
        private readonly ILogService _logService;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="statisticsService">统计服务</param>
        /// <param name="logService">日志服务</param>
        public StatisticsController(IStatisticsService statisticsService, ILogService logService)
        {
            _statisticsService = statisticsService;
            _logService = logService;
        }
        
        /// <summary>
        /// 获取销售总览数据
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>销售总览数据</returns>
        [HttpPost("overview")]
        public async Task<IActionResult> GetSalesOverview(SalesFilter filter)
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                    return Unauthorized();
                }
                
                filter.FarmerId = userId;
            }
            
            var overview = await _statisticsService.GetSalesOverviewAsync(filter);
            
            // 记录日志
            var logUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (logUserIdClaim != null && int.TryParse(logUserIdClaim.Value, out userId))
            {
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取销售总览",
                    description: $"获取销售总览数据: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "所有时间"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            }
            
            return Ok(overview);
        }
        
        /// <summary>
        /// 获取每日销售趋势
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每日销售趋势数据</returns>
        [HttpPost("trend/daily")]
        public async Task<IActionResult> GetDailySalesTrend(SalesFilter filter)
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                    return Unauthorized();
                }
                
                filter.FarmerId = userId;
            }
            
            var dailyTrend = await _statisticsService.GetDailySalesTrendAsync(filter);
            
            // 记录日志
            var logUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (logUserIdClaim != null && int.TryParse(logUserIdClaim.Value, out userId))
            {
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取每日销售趋势",
                    description: $"获取每日销售趋势数据: {filter.StartDate?.ToString("yyyy-MM-dd") ?? "最近30天"} - {filter.EndDate?.ToString("yyyy-MM-dd") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            }
            
            return Ok(dailyTrend);
        }
        
        /// <summary>
        /// 获取每月销售趋势
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <returns>每月销售趋势数据</returns>
        [HttpPost("trend/monthly")]
        public async Task<IActionResult> GetMonthlySalesTrend(SalesFilter filter)
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                    return Unauthorized();
                }
                
                filter.FarmerId = userId;
            }
            
            var monthlyTrend = await _statisticsService.GetMonthlySalesTrendAsync(filter);
            
            // 记录日志
            var logUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (logUserIdClaim != null && int.TryParse(logUserIdClaim.Value, out userId))
            {
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取每月销售趋势",
                    description: $"获取每月销售趋势数据: {filter.StartDate?.ToString("yyyy-MM") ?? "最近12个月"} - {filter.EndDate?.ToString("yyyy-MM") ?? "至今"}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            }
            
            return Ok(monthlyTrend);
        }
        
        /// <summary>
        /// 获取产品销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>产品销售排名</returns>
        [HttpPost("products/top/{top=10}")]
        public async Task<IActionResult> GetTopProducts(SalesFilter filter, int top = 10)
        {
            int userId = 0;
            // 如果是农户，只能查看自己的产品销售情况
            if (User.IsInRole("farmer"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out userId))
                {
                    return Unauthorized();
                }
                
                filter.FarmerId = userId;
            }
            
            var topProducts = await _statisticsService.GetTopProductsAsync(filter, top);
            
            // 记录日志
            var logUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (logUserIdClaim != null && int.TryParse(logUserIdClaim.Value, out userId))
            {
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取产品销售排名",
                    description: $"获取产品销售排名数据(Top {top})",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            }
            
            return Ok(topProducts);
        }
        
        /// <summary>
        /// 获取农户销售排名
        /// </summary>
        /// <param name="filter">筛选条件</param>
        /// <param name="top">返回数量</param>
        /// <returns>农户销售排名</returns>
        [HttpPost("farmers/top/{top=10}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetTopFarmers(SalesFilter filter, int top = 10)
        {
            var topFarmers = await _statisticsService.GetTopFarmersAsync(filter, top);
            
            // 记录日志
            int userId = 0;
            var logUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (logUserIdClaim != null && int.TryParse(logUserIdClaim.Value, out userId))
            {
                await _logService.LogAction(
                    userId: userId,
                    actionType: "获取农户销售排名",
                    description: $"获取农户销售排名数据(Top {top})",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知"
                );
            }
            
            return Ok(topFarmers);
        }
    }
} 
 
 