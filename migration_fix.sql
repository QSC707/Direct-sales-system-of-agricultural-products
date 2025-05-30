-- ===================================
-- 农产品直销系统数据库迁移修复脚本
-- 用于解决外键关系冲突问题
-- ===================================

USE FarmDirectSales;
GO

-- 检查并修复OrderGroupId类型问题
IF EXISTS (
    SELECT * 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Orders' 
    AND COLUMN_NAME = 'OrderGroupId' 
    AND DATA_TYPE = 'nvarchar'
)
BEGIN
    PRINT '正在修复Orders表中OrderGroupId字段类型...';
    
    -- 1. 删除相关约束
    DECLARE @constraintName NVARCHAR(128);
    SELECT @constraintName = name 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID('Orders') 
    AND referenced_object_id = OBJECT_ID('OrderGroups');
    
    IF @constraintName IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(500) = 'ALTER TABLE Orders DROP CONSTRAINT ' + @constraintName;
        EXEC sp_executesql @sql;
        PRINT '已删除外键约束: ' + @constraintName;
    END
    
    -- 2. 更改列类型
    ALTER TABLE Orders ALTER COLUMN OrderGroupId INT NULL;
    PRINT '已将OrderGroupId列类型更改为INT';
    
    -- 3. 重新添加外键约束
    ALTER TABLE Orders ADD CONSTRAINT FK_Orders_OrderGroups 
    FOREIGN KEY (OrderGroupId) REFERENCES OrderGroups(OrderGroupId) ON DELETE SET NULL;
    PRINT '已重新添加外键约束: FK_Orders_OrderGroups';
END
ELSE
BEGIN
    PRINT 'OrderGroupId字段类型正常，无需修复';
END

-- 检查并修复ShippingFee与DeliveryArea的关系
IF EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_ShippingFees_DeliveryAreas' 
    AND delete_referential_action = 1 -- CASCADE
)
BEGIN
    PRINT '正在修复ShippingFees与DeliveryAreas的关系...';
    
    -- 删除旧约束
    ALTER TABLE ShippingFees DROP CONSTRAINT FK_ShippingFees_DeliveryAreas;
    
    -- 添加新约束
    ALTER TABLE ShippingFees ADD CONSTRAINT FK_ShippingFees_DeliveryAreas 
    FOREIGN KEY (DeliveryAreaId) REFERENCES DeliveryAreas(DeliveryAreaId) ON DELETE SET NULL;
    
    PRINT '已修复ShippingFees与DeliveryAreas的关系';
END
ELSE
BEGIN
    PRINT 'ShippingFees与DeliveryAreas的关系正常，无需修复';
END

-- 检查并修复Orders表的外键关系
IF NOT EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Orders_ShippingFees'
)
BEGIN
    PRINT '正在添加Orders表到ShippingFee的外键关系...';
    
    -- 添加约束
    ALTER TABLE Orders ADD CONSTRAINT FK_Orders_ShippingFees 
    FOREIGN KEY (ShippingFeeId) REFERENCES ShippingFees(ShippingFeeId) ON DELETE SET NULL;
    
    PRINT '已添加Orders表到ShippingFee的外键关系';
END
ELSE
BEGIN
    PRINT 'Orders表到ShippingFee的外键关系已存在，无需添加';
END

-- 检查并修复Orders表与Users和Products的外键关系
IF EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Orders_Users' 
    AND delete_referential_action = 0 -- NO ACTION
)
BEGIN
    PRINT 'Orders表与Users的外键关系正常，无需修复';
END
ELSE
BEGIN
    PRINT '正在修复Orders表与Users的外键关系...';
    
    -- 获取当前约束名称
    DECLARE @usersConstraint NVARCHAR(128);
    SELECT @usersConstraint = name 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID('Orders') 
    AND referenced_object_id = OBJECT_ID('Users');
    
    -- 删除旧约束
    IF @usersConstraint IS NOT NULL
    BEGIN
        DECLARE @dropUsersSql NVARCHAR(500) = 'ALTER TABLE Orders DROP CONSTRAINT ' + @usersConstraint;
        EXEC sp_executesql @dropUsersSql;
    END
    
    -- 添加新约束
    ALTER TABLE Orders ADD CONSTRAINT FK_Orders_Users 
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE NO ACTION;
    
    PRINT '已修复Orders表与Users的外键关系';
END

IF EXISTS (
    SELECT * 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Orders_Products' 
    AND delete_referential_action = 0 -- NO ACTION
)
BEGIN
    PRINT 'Orders表与Products的外键关系正常，无需修复';
END
ELSE
BEGIN
    PRINT '正在修复Orders表与Products的外键关系...';
    
    -- 获取当前约束名称
    DECLARE @productsConstraint NVARCHAR(128);
    SELECT @productsConstraint = name 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID('Orders') 
    AND referenced_object_id = OBJECT_ID('Products');
    
    -- 删除旧约束
    IF @productsConstraint IS NOT NULL
    BEGIN
        DECLARE @dropProductsSql NVARCHAR(500) = 'ALTER TABLE Orders DROP CONSTRAINT ' + @productsConstraint;
        EXEC sp_executesql @dropProductsSql;
    END
    
    -- 添加新约束
    ALTER TABLE Orders ADD CONSTRAINT FK_Orders_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE NO ACTION;
    
    PRINT '已修复Orders表与Products的外键关系';
END

PRINT '数据库迁移修复完成！';
GO 