using FarmDirectSales.Services;
using System.Text;

namespace FarmDirectSales.Middlewares
{
    /// <summary>
    /// 日志中间件
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 中间件处理方法
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // 保存请求的原始响应流
            var originalBodyStream = context.Response.Body;
            
            try
            {
                // 创建新的响应流
                using var responseBody = new MemoryStream();
                
                // 替换响应流
                context.Response.Body = responseBody;

                // 获取请求信息
                var method = context.Request.Method;
                var path = context.Request.Path;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var user = context.Items["User"] as Models.User;
                var userId = user?.UserId;

                bool isSuccess = true;
                string actionType = $"{method}:{path}";

                // 记录请求开始的时间
                var startTime = DateTime.Now;

                try
                {
                    // 继续执行管道中的其他中间件
                    await _next(context);
                }
                catch
                {
                    isSuccess = false;
                    throw;
                }
                finally
                {
                    try
                    {
                        // 计算请求处理的时间
                        var elapsed = DateTime.Now - startTime;
                        
                        // 获取状态码
                        var statusCode = context.Response.StatusCode;
                        
                        // 根据状态码判断操作是否成功
                        isSuccess = statusCode >= 200 && statusCode < 400;

                        // 在复制响应内容前，确保流位置重置
                        if (responseBody.CanSeek && responseBody.CanRead)
                        {
                            responseBody.Position = 0;
                            
                            // 复制响应内容到原始流
                            if (originalBodyStream.CanWrite)
                            {
                                await responseBody.CopyToAsync(originalBodyStream);
                            }
                        }

                        // 排除 Swagger 相关请求和静态文件请求
                        if (!path.ToString().StartsWith("/swagger") && 
                            !path.ToString().EndsWith(".js") && 
                            !path.ToString().EndsWith(".css") &&
                            !path.ToString().EndsWith(".png") &&
                            !path.ToString().EndsWith(".jpg") &&
                            !path.ToString().EndsWith(".ico"))
                        {
                            // 获取目标对象ID（如果路径中包含ID参数）
                            int? targetId = null;
                            string? targetType = null;

                            // 尝试从URL路径中提取目标ID
                            var pathSegments = path.ToString().Split('/');
                            if (pathSegments.Length > 2)
                            {
                                targetType = pathSegments[2];
                                if (pathSegments.Length > 3 && int.TryParse(pathSegments[3], out int id))
                                {
                                    targetId = id;
                                }
                            }

                            try
                            {
                                // 从请求作用域获取日志服务
                                var logService = context.RequestServices.GetService<ILogService>();
                                if (logService != null)
                                {
                                    // 记录日志
                                    await logService.LogAction(
                                        userId,
                                        actionType,
                                        $"Status: {statusCode}, Time: {elapsed.TotalMilliseconds}ms",
                                        ipAddress,
                                        targetId,
                                        targetType,
                                        isSuccess
                                    );
                                }
                            }
                            catch
                            {
                                // 忽略日志记录错误
                            }
                        }
                    }
                    catch
                    {
                        // 忽略任何流处理错误
                    }
                }
            }
            finally
            {
                // 确保响应流恢复为原始流
                context.Response.Body = originalBodyStream;
            }
        }
    }

    /// <summary>
    /// 日志中间件扩展方法
    /// </summary>
    public static class LoggingMiddlewareExtensions
    {
        /// <summary>
        /// 使用日志中间件
        /// </summary>
        public static IApplicationBuilder UseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<LoggingMiddleware>();
        }
    }
} 
 
 