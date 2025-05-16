using FarmDirectSales.Models;

namespace FarmDirectSales.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 用户注册
        /// </summary>
        Task<User> RegisterAsync(string username, string password, string role);

        /// <summary>
        /// 用户登录
        /// </summary>
        Task<(User user, string token)> LoginAsync(string username, string password);

        /// <summary>
        /// 获取用户信息
        /// </summary>
        Task<User> GetUserByIdAsync(int userId);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<User> UpdateUserAsync(User user);

        /// <summary>
        /// 删除用户
        /// </summary>
        Task<bool> DeleteUserAsync(int userId);

        /// <summary>
        /// 验证用户密码
        /// </summary>
        Task<bool> ValidatePasswordAsync(int userId, string? password);

        /// <summary>
        /// 修改用户密码
        /// </summary>
        Task<bool> ChangePasswordAsync(int userId, string newPassword);
    }
} 