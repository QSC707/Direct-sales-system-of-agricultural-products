# 农贸直销系统 (Farm Direct Sales System)

这是一个基于ASP.NET Core开发的农贸直销系统，提供从农户到消费者的直接交易平台。

## 主要功能

- 用户认证与授权（普通用户、农户、管理员）
- 产品管理与展示
- 购物车功能
- 订单管理
- 评价系统
- 溯源信息
- 日志记录
- 销售统计

## 技术栈

- 后端: ASP.NET Core API (.NET 9.0)
- 前端: HTML, CSS, JavaScript (Bootstrap)
- 数据库: SQL Server
- 认证: JWT (JSON Web Tokens)

## 运行方式

```bash
cd FarmDirectSales
dotnet run
```

默认运行在 http://localhost:5004

## 主要API端点

- `/api/auth` - 用户认证
- `/api/product` - 产品管理
- `/api/order` - 订单管理
- `/api/review` - 评价系统
- `/api/trace` - 溯源信息
- `/api/admin` - 管理员功能
- `/api/farmer` - 农户功能
- `/api/statistics` - 统计功能 