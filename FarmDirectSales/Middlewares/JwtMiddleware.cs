using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmDirectSales.Middlewares
{
    /// <summary>
    /// JWT认证中间件
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数
        /// </summary>
        public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        /// <summary>
        /// 中间件处理方法
        /// </summary>
        public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                await AttachUserToContext(context, dbContext, token);
            }

            await _next(context);
        }

        /// <summary>
        /// 将用户信息附加到上下文
        /// </summary>
        private async Task AttachUserToContext(HttpContext context, ApplicationDbContext dbContext, string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "defaultkeythatshouldbe32charslong");
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? "defaultissuer",
                    ValidAudience = _configuration["Jwt:Audience"] ?? "defaultaudience",
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                
                // 输出所有声明，用于调试
                Console.WriteLine("JWT令牌中的所有声明:");
                foreach (var claim in jwtToken.Claims)
                {
                    Console.WriteLine($"类型: {claim.Type}, 值: {claim.Value}");
                }
                
                // 尝试获取用户ID
                int userId = 0;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                {
                    Console.WriteLine($"从nameid声明中找到用户ID: {userId}");
                }
                
                if (userId == 0)
                {
                    userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out userId))
                    {
                        Console.WriteLine($"从ClaimTypes.NameIdentifier声明中找到用户ID: {userId}");
                    }
                }
                
                if (userId > 0)
                {
                    // 从数据库获取完整的用户信息
                    var user = await dbContext.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // 将用户信息附加到上下文
                        context.Items["User"] = user;
                        Console.WriteLine($"用户已附加到上下文: ID={user.UserId}, 角色={user.Role}");
                    }
                    else
                    {
                        Console.WriteLine($"找不到用户: ID={userId}");
                    }
                }
                else
                {
                    Console.WriteLine("无法从令牌中提取用户ID");
                }
            }
            catch (Exception ex)
            {
                // 令牌验证失败时记录异常
                Console.WriteLine($"令牌验证失败: {ex.Message}");
            }
        }
    }
} 
 
 