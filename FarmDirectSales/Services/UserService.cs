using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Data;
using FarmDirectSales.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 用户服务实现类
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// 构造函数
        /// </summary>
        public UserService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        public async Task<User> RegisterAsync(string username, string password, string role)
        {
            // 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                throw new Exception("用户名已存在");
            }

            // 创建新用户
            var user = new User
            {
                Username = username,
                Password = HashPassword(password),
                Role = role,
                CreateTime = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<(User user, string token)> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || user.Password != HashPassword(password))
            {
                throw new Exception("用户名或密码错误");
            }

            // 更新最后登录时间
            user.LastLoginTime = DateTime.Now;
            await _context.SaveChangesAsync();

            // 生成JWT令牌
            var token = GenerateJwtToken(user);

            return (user, token);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<User> UpdateUserAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.UserId);
            if (existingUser == null)
            {
                throw new Exception("用户不存在");
            }

            // 只更新非空字段
            if (!string.IsNullOrEmpty(user.Username))
            {
                existingUser.Username = user.Username;
            }
            
            // 更新邮箱和电话
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;
            
            /* 暂时禁用密码更新，防止在普通用户信息更新时影响密码
            // 只有明确提供了密码才更新密码
            if (!string.IsNullOrEmpty(user.Password))
            {
                existingUser.Password = HashPassword(user.Password);
            }
            */
            
            // 只有明确提供了角色才更新角色
            if (!string.IsNullOrEmpty(user.Role))
            {
                existingUser.Role = user.Role;
            }

            await _context.SaveChangesAsync();
            return existingUser;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 密码加密
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "defaultkeythatshouldbe32charslong");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // 令牌有效期7天
                Issuer = _configuration["Jwt:Issuer"] ?? "defaultissuer",
                Audience = _configuration["Jwt:Audience"] ?? "defaultaudience",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 验证用户密码
        /// </summary>
        public async Task<bool> ValidatePasswordAsync(int userId, string? password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            return user.Password == HashPassword(password);
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                throw new Exception("新密码不能为空");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 