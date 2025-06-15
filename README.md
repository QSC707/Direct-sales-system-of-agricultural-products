#                   🌾 农产品直销管理系统

<div align="center">

![GitHub stars](https://img.shields.io/github/stars/QSC707/Direct-sales-system-of-agricultural-products?style=social)
![GitHub forks](https://img.shields.io/github/forks/QSC707/Direct-sales-system-of-agricultural-products?style=social)
![GitHub license](https://img.shields.io/github/license/QSC707/Direct-sales-system-of-agricultural-products)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red)

**基于ASP.NET Core 8.0的现代化农产品直销电商平台**

[在线演示](http://localhost:5004) | [API文档](http://localhost:5004/swagger) | [项目文档](./docs) | [部署指南](./database_migration_guide.md)

</div>

---

## 📋 项目概述

这是一个功能完整的农产品直销管理系统，采用现代化技术栈，为农户和消费者提供安全、便捷的农产品交易平台。系统支持**三种用户角色**（管理员、农户、消费者），实现从产品管理到订单配送的完整电商业务流程。

### 🎯 核心价值
- **🌱 直连农户与消费者** - 减少中间环节，保证产品新鲜度
- **📱 现代化用户体验** - 响应式设计，支持多设备访问
- **🔒 安全可靠** - JWT认证，完整的权限管理
- **📊 数据驱动** - 完整的销售统计和数据分析

---

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    🌐 前端表现层                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │  管理员界面  │ │   农户界面   │ │ 用户购物界面 │           │
│  │   (7页面)   │ │   (7页面)   │ │   (8页面)   │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
│               ┌─────────────────────────────┐               │
│               │    网站公共页面 (9页面)      │               │
│               └─────────────────────────────┘               │
└─────────────────────────────────────────────────────────────┘
                              │
                        HTTP/HTTPS + JWT
                              │
┌─────────────────────────────────────────────────────────────┐
│                    🎮 API控制器层                            │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│  │ 认证授权 │ │ 商品订单 │ │ 农户功能 │ │ 配送物流 │          │
│  │ (3控制器)│ │ (4控制器)│ │ (3控制器)│ │ (5控制器)│          │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘          │
│               ┌─────────────────────────────┐               │
│               │    系统功能 (4控制器)        │               │
│               └─────────────────────────────┘               │
└─────────────────────────────────────────────────────────────┘
                              │
                        依赖注入 (DI)
                              │
┌─────────────────────────────────────────────────────────────┐
│                    🔧 业务服务层                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│  │   用户服务   │ │   统计服务   │ │   日志服务   │          │
│  └─────────────┘ └─────────────┘ └─────────────┘          │
│               ┌─────────────────────────────┐               │
│               │      上传服务              │               │
│               └─────────────────────────────┘               │
└─────────────────────────────────────────────────────────────┘
                              │
                      Entity Framework Core
                              │
┌─────────────────────────────────────────────────────────────┐
│                    🗄️ 数据访问层                             │
│               ┌─────────────────────────────┐               │
│               │  ApplicationDbContext       │               │
│               │     (EF Core 8.0)          │               │
│               └─────────────────────────────┘               │
└─────────────────────────────────────────────────────────────┘
                              │
                            SQL连接 + 索引优化
                              │
┌─────────────────────────────────────────────────────────────┐
│                    💾 数据存储层                             │
│               ┌─────────────────────────────┐               │
│               │    SQL Server Database      │               │
│               │      (Docker 部署)          │               │
│               │   + 索引优化 + 迁移管理      │               │
│               └─────────────────────────────┘               │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 技术栈

### 🏗️ 后端技术

| 技术 | 版本 | 用途 |
|------|------|------|
| ![.NET](https://img.shields.io/badge/.NET-8.0-purple) | **ASP.NET Core 8.0** | Web API框架 |
| ![Entity Framework](https://img.shields.io/badge/EF%20Core-8.0-green) | **Entity Framework Core** | ORM数据访问 |
| ![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red) | **SQL Server** | 关系型数据库 |
| ![JWT](https://img.shields.io/badge/JWT-Auth-orange) | **JWT Bearer** | 身份认证 |
| ![Swagger](https://img.shields.io/badge/Swagger-API-brightgreen) | **OpenAPI 3.0** | API文档 |
| ![Docker](https://img.shields.io/badge/Docker-Container-blue) | **Docker** | 容器化部署 |

### 🎨 前端技术

| 技术 | 版本 | 用途 |
|------|------|------|
| ![HTML5](https://img.shields.io/badge/HTML-5-orange) | **HTML5** | 页面结构 |
| ![CSS3](https://img.shields.io/badge/CSS-3-blue) | **CSS3** | 样式设计 |
| ![JavaScript](https://img.shields.io/badge/JavaScript-ES6+-yellow) | **JavaScript ES6+** | 交互逻辑 |
| ![Bootstrap](https://img.shields.io/badge/Bootstrap-5-purple) | **Bootstrap 5** | 响应式框架 |
| ![jQuery](https://img.shields.io/badge/jQuery-3.x-blue) | **jQuery** | DOM操作 |

### 🔧 开发工具

- **IDE**: Visual Studio 2022 / VS Code
- **版本控制**: Git + GitHub
- **包管理**: NuGet + npm
- **数据库工具**: SQL Server Management Studio
- **图片处理**: SixLabors.ImageSharp 3.1.8

---

## ✨ 核心功能

### 👤 用户管理系统
- **🔐 三角色权限**: 管理员、农户、消费者
- **🔑 JWT认证**: 安全的令牌认证机制
- **👤 个人中心**: 完整的用户资料管理
- **📱 用户地址**: 多地址管理和配送支持

### 🛒 电商核心功能
- **📦 产品管理**: 分类、搜索、筛选、详情展示
- **🛍️ 购物车**: 添加、修改、批量结算
- **💳 订单系统**: 下单、支付、状态跟踪
- **🚚 配送管理**: 配送区域、运费计算、预设模板

### 🌾 农户专属功能
- **🏪 店铺管理**: 农场档案、产品上架
- **📊 销售统计**: 详细的销售数据分析
- **📋 订单处理**: 订单确认、发货、完成
- **⚙️ 配送设置**: 自定义配送信息和模板

### 👑 管理员功能
- **👥 用户管理**: 用户审核、角色分配
- **🗂️ 产品管理**: 产品审核、分类管理
- **🚚 配送管理**: 配送区域、运费规则
- **📈 系统统计**: 全局数据统计和分析
- **📝 日志管理**: 系统操作日志和审计

---

## 📊 项目统计

| 指标 | 数量 | 说明 |
|------|------|------|
| **🎮 API控制器** | 19个 | ~8,000行代码 |
| **🗂️ 数据模型** | 14个 | ~2,500行代码 |
| **🔧 业务服务** | 8个 | ~1,200行代码 |
| **🎨 前端页面** | 32个 | ~35,000行代码 |
| **📋 数据库迁移** | 85个 | ~3,000行代码 |
| **📚 项目文档** | 10+个 | ~1,000行代码 |
| **💽 总代码量** | **50,000+行** | 完整功能实现 |

---

## 🚀 快速开始

### 📋 环境要求

- **.NET 8.0 SDK** 或更高版本
- **SQL Server 2019+** (推荐使用Docker)
- **Visual Studio 2022** 或 **VS Code**
- **Node.js** (可选，用于前端构建工具)

### 🔧 安装步骤

```bash
# 1. 克隆项目
git clone https://github.com/QSC707/Direct-sales-system-of-agricultural-products.git
cd Direct-sales-system-of-agricultural-products

# 2. 启动数据库（Docker方式）
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" \
  -p 1433:1433 --name sql_server -d mcr.microsoft.com/mssql/server:2022-latest

# 3. 配置数据库连接
# 修改 appsettings.json 中的连接字符串

# 4. 安装依赖并运行迁移
cd FarmDirectSales
dotnet restore
dotnet ef database update

# 5. 启动应用
dotnet run
```

### 🌐 访问应用

- **系统首页**: http://localhost:5004
- **API文档**: http://localhost:5004/swagger
- **管理员账号**: admin / 123456

---

## 📱 功能展示

### 🛍️ 用户购物流程
1. **浏览产品** → 产品列表、分类筛选、详情查看
2. **加入购物车** → 购物车管理、数量调整
3. **订单结算** → 地址选择、配送方式、订单确认
4. **订单跟踪** → 状态更新、物流信息

### 🌾 农户业务流程
1. **产品管理** → 上架产品、库存管理、价格调整
2. **订单处理** → 接收订单、确认发货、订单完成
3. **数据分析** → 销售统计、产品分析、收益报表
4. **配送设置** → 配送区域、运费模板、联系信息

### 👑 管理员管理流程
1. **用户管理** → 用户审核、角色分配、权限控制
2. **系统监控** → 日志查看、系统统计、性能监控
3. **业务管理** → 产品审核、配送管理、数据分析

---

## 📁 项目结构

```
📦 农产品直销管理系统
├── 📂 FarmDirectSales/              # 主项目目录
│   ├── 📂 Controllers/              # API控制器层 (19个)
│   ├── 📂 Models/                   # 数据模型层 (14个)
│   ├── 📂 Services/                 # 业务服务层 (8个)
│   ├── 📂 Data/                     # 数据访问层
│   ├── 📂 Migrations/               # 数据库迁移 (85个)
│   ├── 📂 wwwroot/                  # 静态文件和前端
│   │   ├── 📂 pages/                # 页面文件
│   │   │   ├── 📂 admin/           # 管理员页面 (7个)
│   │   │   ├── 📂 farmer/          # 农户页面 (7个)
│   │   │   ├── 📂 user/            # 用户页面 (2个)
│   │   │   └── 📄 其他公共页面 (15个)
│   │   ├── 📂 css/                  # 样式文件
│   │   ├── 📂 js/                   # JavaScript文件
│   │   └── 📂 uploads/              # 上传文件
│   ├── 📄 Program.cs                # 应用入口
│   └── 📄 appsettings.json          # 配置文件
├── 📂 docs/                         # 项目文档
├── 📄 database_init_script.sql      # 数据库初始化脚本
├── 📄 pca-code.json                 # 行政区划数据
└── 📄 README.md                     # 项目说明
```

---

## 🔗 相关链接

- **📚 完整文档**: [项目文档目录](./docs/)
- **🔧 部署指南**: [数据库迁移指南](./database_migration_guide.md)
- **📋 更新日志**: [ImageSharp更新说明](./ImageSharp更新说明.md)
- **🧹 项目清理**: [清理工作总结](./cleanup_summary.md)

---

## 🤝 贡献指南

我们欢迎社区贡献！请查看 [CONTRIBUTING.md](./CONTRIBUTING.md) 了解如何参与项目开发。

### 📝 提交规范
- 🐛 **Bug修复**: `fix: 修复用户登录问题`
- ✨ **新功能**: `feat: 添加产品批量导入功能`
- 📚 **文档更新**: `docs: 更新部署文档`
- 🎨 **代码优化**: `refactor: 重构订单处理逻辑`

---

## 📄 开源协议

本项目采用 [MIT License](./LICENSE) 开源协议。

---

## ⭐ 支持项目

如果这个项目对您有帮助，请点击 ⭐ **Star** 支持我们！

<div align="center">

**Built with ❤️ by [QSC707](https://github.com/QSC707)**

</div> 
