# 农贸直销系统

## 项目说明
本项目是一个基于ASP.NET Core的农贸直销系统，采用前后端分离架构，提供产品展示、在线订购、订单管理等功能。

## 技术栈
- 后端：ASP.NET Core 7.0
- 数据库：SQL Server
- 认证：JWT Bearer Token
- API文档：Swagger

## 开发环境要求
- .NET SDK 7.0或更高版本
- SQL Server 2019或更高版本
- Visual Studio 2022或Visual Studio Code

## 项目设置步骤

1. 安装.NET SDK
```bash
# macOS
brew install dotnet-sdk

# Windows
# 从 https://dotnet.microsoft.com/download 下载安装包
```

2. 克隆项目
```bash
git clone [项目地址]
cd FarmDirectSales
```

3. 还原依赖包
```bash
dotnet restore
```

4. 配置数据库连接
- 在`appsettings.json`中修改数据库连接字符串
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FarmDirectSales;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

5. 运行数据库迁移
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

6. 启动项目
```bash
dotnet run
```

7. 访问API文档
- 打开浏览器访问：https://localhost:5001/swagger

## 项目结构
```
FarmDirectSales/
├── Controllers/     # API控制器
├── Models/         # 数据模型
├── Services/       # 业务逻辑服务
├── Data/           # 数据访问层
├── Middlewares/    # 中间件
└── Program.cs      # 应用程序入口
```

## API文档
详细的API文档请参考Swagger页面或`/swagger`路径。

## 开发团队
- [开发团队信息]

## 许可证
[许可证信息] 

## 系统功能升级与优化记录

### 1. 修复的问题

1. **端口冲突问题**：通过在`Program.cs`中显式设置`builder.WebHost.UseUrls("http://localhost:5003")`，确保应用在5003端口上运行。

2. **LoggingMiddleware流处理问题**：修复了"Cannot access a closed Stream"错误，通过重构中间件代码，添加了更多的异常处理和检查。

3. **依赖注入问题**：修复了`Cannot resolve scoped service 'FarmDirectSales.Services.ILogService' from root provider`错误，通过在LoggingMiddleware中从HttpContext.RequestServices获取ILogService实例。

4. **LogController中的Forbid方法使用问题**：将Forbid()方法调用替换为StatusCode(StatusCodes.Status403Forbidden)加自定义响应对象。

5. **前端页面导航问题**：修复了前端页面中的导航链接，将相对路径修改为绝对路径，确保点击按钮和链接时能正确导航。

6. **数据库列缺失问题**：解决了"Invalid column name 'AverageRating'"错误，通过添加数据库迁移和更新数据库。

7. **前端页面缺失问题**：创建了缺失的前端页面，包括登录页面、注册页面、产品列表页面、关于我们页面和农户展示页面。

8. **Swagger UI默认路径问题**：修改了Program.cs中的Swagger配置，将RoutePrefix从空字符串改为"swagger"，并添加了DefaultFiles中间件配置。

9. **登录状态显示问题**：修复了登录后状态不正确显示的问题，通过改进前端代码确保用户数据完整性，包括：
   - 修改api.js中的login方法，确保返回完整的用户数据（userId、username、role）
   - 改进login.html中的登录函数，确保正确存储用户信息
   - 增强checkAuthStatus函数，添加对不完整用户数据的处理
   - 添加了登录状态调试功能，方便测试

### 2. 新增功能

1. **销售数据统计与分析**：
   - 增加了销售数据统计页面 `/pages/statistics.html`
   - 实现了销售总览、销售趋势图表、产品销量排行和产品评分排行功能
   - 使用Chart.js创建交互式图表展示销售数据
   - 添加了不同时间周期(周/月/年)的数据展示

2. **JWT认证增强**：
   - 完善了JWT令牌生成逻辑，添加了用户ID、用户名和角色的Claims
   - 设置了合理的令牌有效期

3. **统一登录处理**：
   - 优化了前端登录状态管理逻辑，提升了用户体验
   - 增强了用户数据验证，防止不完整数据导致的前端显示问题

### 3. 下一步优化建议

1. **实现实际的统计数据API**：
   - 当前销售统计页面使用的是模拟数据，建议在后端实现真实的统计数据API
   - 需要添加`StatisticsController`和相应的Service层代码

2. **增加数据导出功能**：
   - 为统计页面添加导出Excel/PDF功能，方便用户分析数据

3. **完善数据可视化**：
   - 添加更多类型的图表，如饼图展示产品类别分布
   - 添加按农户、地区等维度的统计分析

4. **用户权限管理细化**：
   - 针对不同角色定制显示的统计数据范围和类型
   - 管理员可查看全局数据，农户只能查看自己的销售数据

5. **持续集成与测试**：
   - 添加自动化测试，提高代码质量和系统稳定性
   - 实现CI/CD流程，简化部署过程

### 4. 技术栈清单

* **后端**：ASP.NET Core、Entity Framework Core、SQL Server
* **前端**：HTML、CSS、JavaScript、Bootstrap 5、Chart.js
* **认证**：JWT Bearer Token
* **开发工具**：Visual Studio 2022/Visual Studio Code、Git
* **API文档**：Swagger/OpenAPI
* **部署**：Microsoft IIS/Docker 
 
 