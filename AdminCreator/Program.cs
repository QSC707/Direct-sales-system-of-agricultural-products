// 引入必要的命名空间
using System;
using System.Text;
using System.Security.Cryptography;
using System.Data.SqlClient;

namespace AdminCreator
{
    class Program
    {
        // 数据库连接字符串
        private static readonly string _connectionString = "Server=(localdb)\\mssqllocaldb;Database=FarmDirectSales;Trusted_Connection=True;";

        static void Main(string[] args)
        {
            Console.WriteLine("======= 管理员账号创建工具 =======");

            try
            {
                // 设置默认的管理员账号信息
                string username = "admin";
                string password = "123456";
                string email = "admin@farmdirectsales.com";
                string phone = "13800000000";

                // 允许用户自定义账号信息
                Console.WriteLine("将创建以下管理员账号：");
                Console.WriteLine($"用户名: {username}");
                Console.WriteLine($"密码: {password}");
                Console.WriteLine($"邮箱: {email}");
                Console.WriteLine($"电话: {phone}");
                
                Console.WriteLine("\n按任意键继续，或按Ctrl+C取消...");
                Console.ReadKey();
                
                // 检查账号是否已存在
                if (IsUserExists(username))
                {
                    Console.WriteLine("\n用户名已存在，跳过创建过程。");
                }
                else
                {
                    // 创建管理员账号
                    CreateAdminUser(username, password, email, phone);
                    Console.WriteLine($"\n管理员账号 '{username}' 创建成功!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n创建管理员账号时出错: {ex.Message}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        // 检查用户是否已存在
        private static bool IsUserExists(string username)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                string checkSql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (var command = new SqlCommand(checkSql, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        // 创建管理员账号
        private static void CreateAdminUser(string username, string password, string email, string phone)
        {
            string hashedPassword = HashPassword(password);
            DateTime now = DateTime.Now;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                string insertSql = @"
                    INSERT INTO Users (Username, Password, Role, Email, Phone, CreateTime) 
                    VALUES (@Username, @Password, 'admin', @Email, @Phone, @CreateTime)";
                
                using (var command = new SqlCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", hashedPassword);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@CreateTime", now);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        // 密码加密
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
} 