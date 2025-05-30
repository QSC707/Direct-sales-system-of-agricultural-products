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
        /// 订单组表
        /// </summary>
        public DbSet<OrderGroup> OrderGroups { get; set; }

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
        /// 购物车项表
        /// </summary>
        public DbSet<CartItem> CartItems { get; set; }

        /// <summary>
        /// 农户资料表
        /// </summary>
        public DbSet<FarmerProfile> FarmerProfiles { get; set; }
        
        /// <summary>
        /// 用户地址表
        /// </summary>
        public DbSet<UserAddress> UserAddresses { get; set; }
        
        /// <summary>
        /// 配送区域表
        /// </summary>
        public DbSet<DeliveryArea> DeliveryAreas { get; set; }

        /// <summary>
        /// 运费配置表
        /// </summary>
        public DbSet<ShippingFee> ShippingFees { get; set; }

        /// <summary>
        /// 配送信息预设表
        /// </summary>
        public DbSet<DeliveryPreset> DeliveryPresets { get; set; }

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
                
            // 订单组-订单关系
            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderGroup)
                .WithMany(g => g.Orders)
                .HasForeignKey(o => o.OrderGroupId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 用户-订单组关系
            modelBuilder.Entity<OrderGroup>()
                .HasOne(g => g.User)
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction);
                
            // 订单组-运费规则关系
            modelBuilder.Entity<OrderGroup>()
                .HasOne(g => g.ShippingFee)
                .WithMany()
                .HasForeignKey(g => g.ShippingFeeId)
                .OnDelete(DeleteBehavior.SetNull);

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

            // 用户-农户资料关系（一对一）
            modelBuilder.Entity<User>()
                .HasOne(u => u.FarmerProfile)
                .WithOne(f => f.User)
                .HasForeignKey<FarmerProfile>(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // 用户-地址关系
            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 配送区域-运费规则关系
            modelBuilder.Entity<ShippingFee>()
                .HasOne(s => s.DeliveryArea)
                .WithMany()
                .HasForeignKey(s => s.DeliveryAreaId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 订单-运费规则关系
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingFee)
                .WithMany()
                .HasForeignKey(o => o.ShippingFeeId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // 农户-配送预设关系
            modelBuilder.Entity<DeliveryPreset>()
                .HasOne(d => d.Farmer)
                .WithMany()
                .HasForeignKey(d => d.FarmerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 
 
 