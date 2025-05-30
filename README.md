# 农产品直销管理系统 (Farm Direct Sales System)

## 项目概述

这是一个基于ASP.NET Core开发的农产品直销系统，旨在为农户和消费者搭建一个便捷、高效的直接交易平台。系统通过连接农户和消费者，减少中间环节，确保农产品的新鲜度和溯源性，同时帮助农户提高收益。

## 核心功能

### 用户管理
- 三种角色权限管理：普通用户、农户、管理员
- 完善的用户注册、登录和个人信息管理
- JWT认证和授权机制

### 产品管理
- 农户产品上架、定价、库存管理
- 多样化的产品分类和搜索功能
- 产品详情展示，包含图片、产地、有机认证等信息

### 订单与配送
- 购物车功能和结算流程
- 订单状态跟踪和历史记录查询
- 配送区域和运费管理

### 评价与互动
- 用户产品评价系统
- 农户与用户的消息交流
- 产品溯源信息展示

### 数据统计与分析
- 销售统计和趋势分析
- 用户行为数据收集
- 农户业绩报表

### 系统管理
- 完整的日志记录和审计功能
- 系统设置和配置管理
- 数据备份和安全保障

## 技术栈

### 后端
- ASP.NET Core API (.NET 9.0)
- Entity Framework Core ORM
- SQL Server 数据库
- JWT认证
- RESTful API设计
- Swagger API文档

### 前端
- HTML5, CSS3, JavaScript
- Bootstrap 5 响应式框架
- 原生JavaScript和AJAX
- 图表统计：Chart.js

### 部署与运维
- Docker容器支持
- CI/CD流程
- 数据库迁移

## 系统架构

- **表示层**：HTML/CSS/JS前端界面
- **应用服务层**：RESTful API控制器
- **业务逻辑层**：服务和业务逻辑
- **数据访问层**：EF Core和仓储模式
- **数据存储**：SQL Server数据库

## 本地开发环境设置

### 必要条件
- .NET 9.0 SDK
- SQL Server (可使用Docker容器)
- Visual Studio 2022或VS Code

### 运行步骤
```bash
# 克隆仓库
git clone https://github.com/yourusername/farm-direct-sales.git

# 进入项目目录
cd FarmDirectSales

# 恢复包依赖
dotnet restore

# 运行数据库迁移
dotnet ef database update

# 运行应用
dotnet run
```

默认运行在 `http://localhost:5014` (HTTP) 和 `https://localhost:5453` (HTTPS)

## API文档

项目集成了Swagger，可通过 `/swagger` 路径访问完整的API文档。

主要API端点:
- `/api/auth` - 认证与授权
- `/api/products` - 产品管理
- `/api/orders` - 订单管理
- `/api/reviews` - 用户评价
- `/api/farmers` - 农户相关操作
- `/api/admin` - 管理员功能
- `/api/statistics` - 数据统计

## 未来规划

- 移动应用开发
- 支付网关集成
- 多语言支持
- 人工智能推荐系统
- 农产品季节性预测 