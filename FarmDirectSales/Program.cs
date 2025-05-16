using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FarmDirectSales.Data;
using FarmDirectSales.Services;
using FarmDirectSales.Middlewares;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 配置端口：显式设置为5003
builder.WebHost.UseUrls("http://localhost:5003");

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

Console.WriteLine($"应用已启动在: http://localhost:5003");
app.Run(); 
 