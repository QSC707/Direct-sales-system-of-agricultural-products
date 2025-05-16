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
        public async Task<User> RegisterAsync(string username, string password, string role, string? email = null, string? phone = null)
        {
            // 检查用户名是否已存在
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                throw new Exception("用户名已存在");
            }

            // 验证手机号
            if (string.IsNullOrEmpty(phone))
            {
                throw new Exception("手机号码不能为空");
            }

            // 创建新用户
            var user = new User
            {
                Username = username,
                Password = HashPassword(password),
                Role = role,
                Email = email,
                Phone = phone,
                CreateTime = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 返回不包含密码的用户信息
            return new User
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                CreateTime = user.CreateTime
            };
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<(User user, string token)> LoginAsync(string username, string password)
        {
            // 添加管理员账号特殊逻辑
            if (username == "admin" && password == "123456")
            {
                // 查看是否存在admin账户
                var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (existingAdmin == null)
                {
                    // 创建管理员账号
                    var adminPassword = "123456"; // 默认密码
                    var passwordHash = HashPassword(adminPassword);
                    
                    Console.WriteLine($"为admin创建的密码哈希: {passwordHash}");
                    
                    var adminUser = new User
                    {
                        Username = "admin",
                        Password = passwordHash,
                        Role = "admin",
                        Email = "admin@farmdirectsales.com",
                        Phone = "13800000000",
                        CreateTime = DateTime.Now,
                        LastLoginTime = DateTime.Now
                    };
                    
                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();
                    
                    // 生成JWT令牌
                    var adminToken = GenerateJwtToken(adminUser);
                    
                    Console.WriteLine($"管理员账号创建成功: {adminUser.UserId}");
                    
                    return (adminUser, adminToken);
                }
                else
                {
                    Console.WriteLine($"找到现有管理员账号: {existingAdmin.UserId}");
                    
                    // 确保密码正确
                    existingAdmin.Password = HashPassword("123456");
                    
                    // 更新最后登录时间
                    existingAdmin.LastLoginTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    // 生成JWT令牌
                    var adminToken = GenerateJwtToken(existingAdmin);
                    return (existingAdmin, adminToken);
                }
            }
            
            // 正常的登录逻辑
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

            // 确保返回完整的用户信息
            return (new User
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                CreateTime = user.CreateTime,
                LastLoginTime = user.LastLoginTime
            }, token);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        public async Task<User> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.FarmerProfile) // 加载农户资料
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user == null)
            {
                return null;
            }

            // 返回不包含密码的用户信息，同时避免循环引用
            var result = new User
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                CreateTime = user.CreateTime,
                LastLoginTime = user.LastLoginTime
            };
            
            // 手动复制FarmerProfile，避免循环引用
            if (user.FarmerProfile != null)
            {
                result.FarmerProfile = new FarmerProfile
                {
                    FarmerProfileId = user.FarmerProfile.FarmerProfileId,
                    UserId = user.FarmerProfile.UserId,
                    FarmName = user.FarmerProfile.FarmName,
                    Location = user.FarmerProfile.Location,
                    Description = user.FarmerProfile.Description,
                    ProductCategory = user.FarmerProfile.ProductCategory,
                    LicenseNumber = user.FarmerProfile.LicenseNumber,
                    LogoUrl = user.FarmerProfile.LogoUrl,
                    EstablishedDate = user.FarmerProfile.EstablishedDate,
                    CreateTime = user.FarmerProfile.CreateTime,
                    UpdateTime = user.FarmerProfile.UpdateTime
                    // 不设置FarmerProfile.User属性，避免循环引用
                };
            }
            
            return result;
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

        /// <summary>
        /// 创建农户资料
        /// </summary>
        public async Task<FarmerProfile> CreateFarmerProfileAsync(int userId, string farmName, string location, string? description = null, string? productCategory = null, string? licenseNumber = null)
        {
            // 检查用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("用户不存在");
            }

            // 检查用户是否为农户
            if (user.Role != "farmer")
            {
                throw new Exception("只有农户才能创建农场资料");
            }

            // 检查是否已存在农户资料
            var existingProfile = await _context.FarmerProfiles.FirstOrDefaultAsync(f => f.UserId == userId);
            if (existingProfile != null)
            {
                throw new Exception("该用户已有农户资料");
            }

            // 创建农户资料
            var farmerProfile = new FarmerProfile
            {
                UserId = userId,
                FarmName = farmName,
                Location = location,
                Description = description ?? string.Empty,
                ProductCategory = productCategory ?? string.Empty,
                LicenseNumber = licenseNumber ?? string.Empty,
                LogoUrl = "/images/default-farm-logo.png", // 添加默认logo
                CreateTime = DateTime.Now
            };

            _context.FarmerProfiles.Add(farmerProfile);
            await _context.SaveChangesAsync();

            return farmerProfile;
        }
    }
} 