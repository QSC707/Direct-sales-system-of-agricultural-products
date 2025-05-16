using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FarmDirectSales.Data;
using FarmDirectSales.Services;
using FarmDirectSales.Models;
using FarmDirectSales.Middlewares;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 配置端口：显式设置为5004
builder.WebHost.UseUrls("http://localhost:5004");

// 添加数据库上下文
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加服务
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "defaultissuer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "defaultaudience",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "defaultkeythatshouldbe32charslong"))
        };
    });

// 添加授权
builder.Services.AddAuthorization();

// 添加控制器
builder.Services.AddControllers();

// 添加API Explorer终结点，这对于Swagger是必需的
builder.Services.AddEndpointsApiExplorer();

// 添加Swagger生成器
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "农产品直销管理系统API",
        Version = "v1",
        Description = "农产品直销管理系统的API文档",
        Contact = new OpenApiContact
        {
            Name = "管理员",
            Email = "admin@example.com"
        }
    });

    // 添加JWT认证配置
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT认证，请输入 'Bearer' + 空格 + token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 启用XML注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// 构建应用
var app = builder.Build();

// 使用开发者异常页
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // 在开发环境中配置Swagger
    app.UseSwagger(c => {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "农产品直销管理系统API v1");
        c.RoutePrefix = "swagger"; // 修改为 swagger 路径，不再使用根路径
        c.DocumentTitle = "农产品直销管理系统API文档";
        c.DefaultModelsExpandDepth(-1); // 隐藏Models
    });
}
else
{
    app.UseExceptionHandler("/error");
}

// 配置默认文件，确保当访问根目录时返回index.html
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "index.html" }
});

// 先注册静态文件中间件，确保前端文件可以被访问
app.UseStaticFiles();

// 使用CORS
app.UseCors();

// 认证与授权中间件
app.UseAuthentication();
app.UseAuthorization();

// 使用JWT中间件
app.UseMiddleware<JwtMiddleware>();

// 路由映射 - 必须在中间件之后，处理程序之前
app.MapControllers();

// 使用日志中间件 - 避免直接注入ILogService
app.UseLogging();

// 确保创建管理员账号
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogService>();
    
    await CreateAdminUserIfNotExists(context, logger);
}

// 异步创建管理员账号
async Task CreateAdminUserIfNotExists(ApplicationDbContext context, ILogService logger)
{
    try
    {
        // 检查是否存在管理员账号
        if (!context.Users.Any(u => u.Username == "admin"))
        {
            // 删除可能存在的错误的管理员账号
            var existingAdmins = context.Users.Where(u => u.Role == "admin").ToList();
            foreach (var admin in existingAdmins)
            {
                context.Users.Remove(admin);
            }
            
            await context.SaveChangesAsync();
            
            // 创建管理员账号
            var adminPassword = "123456"; // 默认密码
            var passwordHash = HashPassword(adminPassword);
            
            Console.WriteLine($"管理员密码哈希值: {passwordHash}");
            
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
            
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            
            await logger.LogAction(
                adminUser.UserId,         // 用户ID (int?)
                "系统初始化",             // 操作类型 (string)
                "创建默认管理员账号",      // 操作描述 (string)
                "系统",                   // IP地址 (string)
                adminUser.UserId,         // 操作对象ID (int?)
                "用户",                   // 操作对象类型 (string?)
                true                      // 是否成功 (bool)
            );
            
            Console.WriteLine($"已成功创建默认管理员账号 (admin/123456), 用户ID: {adminUser.UserId}");
        }
        else
        {
            var admin = await context.Users.FirstAsync(u => u.Username == "admin");
            Console.WriteLine($"管理员账号已存在 ID: {admin.UserId}, 角色: {admin.Role}");
            
            // 确保密码正确
            admin.Password = HashPassword("123456");
            await context.SaveChangesAsync();
            
            Console.WriteLine("已重置管理员密码为 123456");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"创建管理员账号时出错: {ex.Message}");
        Console.WriteLine($"异常详情: {ex}");
    }
}

// 密码哈希函数
string HashPassword(string password)
{
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}

Console.WriteLine($"应用已启动在: http://localhost:5004");
app.Run(); 
 