using Microsoft.EntityFrameworkCore;
using FarmDirectSales.Models;

namespace FarmDirectSales.Data
{
    /// <summary>
    /// 应用程序数据库上下文
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// 用户表
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// 产品表
        /// </summary>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// 订单表
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// 溯源表
        /// </summary>
        public DbSet<Trace> Traces { get; set; }

        /// <summary>
        /// 日志表
        /// </summary>
        public DbSet<Log> Logs { get; set; }
        
        /// <summary>
        /// 上传文件表
        /// </summary>
        public DbSet<Upload> Uploads { get; set; }
        
        /// <summary>
        /// 商品评价表
        /// </summary>
        public DbSet<Review> Reviews { get; set; }
        
        /// <summary>
        /// 购物车项表
        /// </summary>
        public DbSet<CartItem> CartItems { get; set; }

        /// <summary>
        /// 模型创建
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置关系
            // 用户-订单关系
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // 产品-订单关系
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // 农户-产品关系
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Farmer)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.FarmerId)
                .OnDelete(DeleteBehavior.Cascade);

            // 产品-溯源关系
            modelBuilder.Entity<Trace>()
                .HasOne(t => t.Product)
                .WithMany(p => p.Traces)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // 产品-上传文件关系
            modelBuilder.Entity<Upload>()
                .HasOne(u => u.Product)
                .WithMany(p => p.Uploads)
                .HasForeignKey(u => u.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // 用户-日志关系
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 产品-评价关系
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // 用户-评价关系
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                
            // 订单-评价关系
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 订单-评价关系（一对一）
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Review)
                .WithOne()
                .HasForeignKey<Review>(r => r.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // 用户-购物车项关系
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                
            // 产品-购物车项关系
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
} 
 
 