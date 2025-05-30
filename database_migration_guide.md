# 农产品直销系统 - 数据库迁移指南

## 概述

本文档提供在新环境中迁移和设置数据库的详细步骤。系统使用SQL Server作为数据库，当将项目部署到新环境时，可能会遇到外键约束不一致的问题。

## 已知问题

在项目迁移到新环境后，使用VS2022执行数据库迁移时可能遇到的问题：

1. **外键关系冲突**: 代码模型与数据库初始化脚本中的外键定义不完全一致
2. **OrderGroupId字段类型不匹配**: 模型中为int类型，但初始化脚本中可能为nvarchar类型
3. **删除行为(ON DELETE)不一致**: 代码中定义的删除行为与数据库实际情况不匹配

## 解决方案

### 方法1: 使用修复脚本

1. **执行初始化脚本**:
   ```sql
   -- 使用修正后的database_init_script.sql创建数据库
   ```

2. **执行修复脚本**:
   ```sql
   -- 使用migration_fix.sql修复可能的外键问题
   ```

### 方法2: 使用Entity Framework迁移

如果使用Entity Framework Code First迁移：

1. **删除现有Migrations文件夹中的所有迁移文件**
2. **打开Package Manager Console**
3. **执行新的迁移**:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```

### 方法3: 手动调整外键关系

如果仍然遇到问题，可以尝试手动调整外键关系：

1. 确保所有表格已经创建
2. 删除所有外键约束
3. 按照`ApplicationDbContext.cs`中的定义重新添加外键约束

## 关键外键关系

以下是系统中的关键外键关系和正确的删除行为：

1. **订单-用户关系**: `ON DELETE NO ACTION`
2. **订单-产品关系**: `ON DELETE NO ACTION`
3. **订单-订单组关系**: `ON DELETE SET NULL`
4. **订单组-用户关系**: `ON DELETE NO ACTION`
5. **订单组-运费规则关系**: `ON DELETE SET NULL`
6. **产品-农户关系**: `ON DELETE CASCADE`
7. **运费-配送区域关系**: `ON DELETE SET NULL`
8. **订单-运费规则关系**: `ON DELETE SET NULL`

## 问题排查

如果迁移后仍然出现外键问题，请检查以下几点：

1. SQL Server版本是否兼容
2. 数据库模型中`OrderGroupId`字段的类型是否为int
3. 所有外键约束是否正确定义
4. 表结构是否与模型类完全匹配

## 重要说明

- 修改数据库前务必备份现有数据
- 在生产环境应用任何脚本前，先在测试环境验证
- 如果有数据存在，请谨慎操作外键关系修改 