-- ===============================
-- 农产品直销系统数据库初始化脚本
-- 创建日期: 2025年1月
-- 适用于: SQL Server 2019+
-- ===============================

-- 创建数据库
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FarmDirectSales')
BEGIN
    CREATE DATABASE FarmDirectSales;
END
GO

USE FarmDirectSales;
GO

-- 创建用户表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId] int IDENTITY(1,1) NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Role] nvarchar(20) NOT NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [LastLoginTime] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
    );
    
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END
GO

-- 创建农户档案表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FarmerProfiles' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[FarmerProfiles] (
        [FarmerProfileId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [FarmName] nvarchar(100) NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [LicenseNumber] nvarchar(50) NULL,
        [EstablishedDate] datetime2 NULL,
        [ProductCategory] nvarchar(200) NULL,
        [FarmPhoto1] nvarchar(500) NULL,
        [FarmPhoto2] nvarchar(500) NULL,
        [FarmPhoto3] nvarchar(500) NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_FarmerProfiles] PRIMARY KEY ([FarmerProfileId]),
        CONSTRAINT [FK_FarmerProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_FarmerProfiles_UserId] ON [FarmerProfiles] ([UserId]);
END
GO

-- 创建产品表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Products] (
        [ProductId] int IDENTITY(1,1) NOT NULL,
        [FarmerId] int NOT NULL,
        [ProductName] nvarchar(100) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Price] decimal(10,2) NOT NULL,
        [Stock] int NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [ImageUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [ActiveTime] datetime2 NULL,
        [InactiveTime] datetime2 NULL,
        [IsOrganic] bit NOT NULL DEFAULT 0,
        [HarvestDate] datetime2 NULL,
        [ShelfLife] int NULL,
        [Specification] nvarchar(200) NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
        CONSTRAINT [FK_Products_Users] FOREIGN KEY ([FarmerId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Products_FarmerId] ON [Products] ([FarmerId]);
    CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
    CREATE INDEX [IX_Products_IsActive] ON [Products] ([IsActive]);
    CREATE INDEX [IX_Products_CreateTime] ON [Products] ([CreateTime] DESC);
END
GO

-- 创建订单表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Orders' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [OrderId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [TotalPrice] decimal(10,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT '待支付',
        [OrderGroupId] int NULL,
        [ShippingAddress] nvarchar(500) NOT NULL,
        [ContactPhone] nvarchar(20) NOT NULL,
        [DeliveryContact] nvarchar(50) NULL,
        [DeliveryPhone] nvarchar(20) NULL,
        [DeliveryInfo] nvarchar(1000) NULL,
        [ShippingFeeId] int NULL,
        [ShippingFeeAmount] decimal(10,2) NULL,
        [EstimatedDeliveryTime] datetime2 NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [PayTime] datetime2 NULL,
        [ShipTime] datetime2 NULL,
        [CompleteTime] datetime2 NULL,
        [CancelTime] datetime2 NULL,
        [CancelReason] nvarchar(500) NULL,
        [CancelBy] int NULL,
        [CancelByType] nvarchar(20) NULL,
        [RefundRequestTime] datetime2 NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeleteTime] datetime2 NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderId]),
        CONSTRAINT [FK_Orders_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE NO ACTION
    );
    
    CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
    CREATE INDEX [IX_Orders_ProductId] ON [Orders] ([ProductId]);
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
    CREATE INDEX [IX_Orders_CreateTime] ON [Orders] ([CreateTime] DESC);
END
GO

-- 创建订单组表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OrderGroups' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[OrderGroups] (
        [OrderGroupId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [GroupNumber] nvarchar(50) NOT NULL,
        [OrderCount] int NOT NULL DEFAULT 0,
        [TotalProductAmount] decimal(10,2) NOT NULL DEFAULT 0,
        [ShippingFeeAmount] decimal(10,2) NOT NULL DEFAULT 0,
        [TotalAmount] decimal(10,2) NOT NULL DEFAULT 0,
        [ShippingAddress] nvarchar(500) NOT NULL,
        [ContactPhone] nvarchar(20) NOT NULL,
        [ReceiverName] nvarchar(50) NOT NULL,
        [DeliveryAreaId] int NULL,
        [ShippingFeeId] int NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_OrderGroups] PRIMARY KEY ([OrderGroupId]),
        CONSTRAINT [FK_OrderGroups_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderGroups_ShippingFees] FOREIGN KEY ([ShippingFeeId]) REFERENCES [ShippingFees] ([ShippingFeeId]) ON DELETE SET NULL
    );
    
    CREATE INDEX [IX_OrderGroups_UserId] ON [OrderGroups] ([UserId]);
    CREATE INDEX [IX_OrderGroups_CreateTime] ON [OrderGroups] ([CreateTime] DESC);
END
GO

-- 添加Order表到OrderGroup的外键关系
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_OrderGroups')
BEGIN
    ALTER TABLE [dbo].[Orders] 
    ADD CONSTRAINT [FK_Orders_OrderGroups] 
    FOREIGN KEY ([OrderGroupId]) REFERENCES [OrderGroups] ([OrderGroupId]) 
    ON DELETE SET NULL;
END
GO

-- 修正ShippingFees表与DeliveryAreas的关系
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ShippingFees_DeliveryAreas')
BEGIN
    ALTER TABLE [dbo].[ShippingFees] DROP CONSTRAINT [FK_ShippingFees_DeliveryAreas];
    
    ALTER TABLE [dbo].[ShippingFees] 
    ADD CONSTRAINT [FK_ShippingFees_DeliveryAreas] 
    FOREIGN KEY ([DeliveryAreaId]) REFERENCES [DeliveryAreas] ([DeliveryAreaId]) 
    ON DELETE SET NULL;
END
GO

-- 添加Order表到ShippingFee的外键关系
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_ShippingFees')
BEGIN
    ALTER TABLE [dbo].[Orders] 
    ADD CONSTRAINT [FK_Orders_ShippingFees] 
    FOREIGN KEY ([ShippingFeeId]) REFERENCES [ShippingFees] ([ShippingFeeId]) 
    ON DELETE SET NULL;
END
GO

-- 创建购物车表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CartItems' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[CartItems] (
        [CartItemId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([CartItemId]),
        CONSTRAINT [FK_CartItems_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_CartItems_UserId] ON [CartItems] ([UserId]);
    CREATE INDEX [IX_CartItems_ProductId] ON [CartItems] ([ProductId]);
END
GO

-- 创建用户地址表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserAddresses' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserAddresses] (
        [AddressId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NOT NULL,
        [ReceiverName] nvarchar(50) NOT NULL,
        [ContactPhone] nvarchar(20) NOT NULL,
        [Province] nvarchar(50) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [District] nvarchar(50) NOT NULL,
        [DetailAddress] nvarchar(200) NOT NULL,
        [IsDefault] bit NOT NULL DEFAULT 0,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_UserAddresses] PRIMARY KEY ([AddressId]),
        CONSTRAINT [FK_UserAddresses_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_UserAddresses_UserId] ON [UserAddresses] ([UserId]);
END
GO

-- 创建配送区域表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DeliveryAreas' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[DeliveryAreas] (
        [DeliveryAreaId] int IDENTITY(1,1) NOT NULL,
        [Province] nvarchar(50) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [District] nvarchar(50) NULL,
        [DeliveryFee] decimal(10,2) NOT NULL,
        [SupportSameDayDelivery] bit NOT NULL DEFAULT 0,
        [IsNationwide] bit NOT NULL DEFAULT 0,
        [Description] nvarchar(200) NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateBy] int NULL,
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_DeliveryAreas] PRIMARY KEY ([DeliveryAreaId])
    );
END
GO

-- 创建运费表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ShippingFees' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[ShippingFees] (
        [ShippingFeeId] int IDENTITY(1,1) NOT NULL,
        [DeliveryAreaId] int NOT NULL,
        [MinWeight] decimal(10,2) NOT NULL DEFAULT 0,
        [MaxWeight] decimal(10,2) NULL,
        [BasePrice] decimal(10,2) NOT NULL,
        [PricePerKg] decimal(10,2) NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateBy] int NULL,
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_ShippingFees] PRIMARY KEY ([ShippingFeeId]),
        CONSTRAINT [FK_ShippingFees_DeliveryAreas] FOREIGN KEY ([DeliveryAreaId]) REFERENCES [DeliveryAreas] ([DeliveryAreaId]) ON DELETE SET NULL
    );
    
    CREATE INDEX [IX_ShippingFees_DeliveryAreaId] ON [ShippingFees] ([DeliveryAreaId]);
END
GO

-- 创建溯源信息表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Traces' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Traces] (
        [TraceId] int IDENTITY(1,1) NOT NULL,
        [ProductId] int NOT NULL,
        [PlantingTime] datetime2 NULL,
        [HarvestTime] datetime2 NULL,
        [PlantingMethod] nvarchar(200) NULL,
        [SourcePlace] nvarchar(200) NULL,
        [QualityLevel] nvarchar(50) NULL,
        [IsOrganic] bit NOT NULL DEFAULT 0,
        [AdditionalInfo] nvarchar(1000) NULL,
        [CreateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        [UpdateTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Traces] PRIMARY KEY ([TraceId]),
        CONSTRAINT [FK_Traces_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Traces_ProductId] ON [Traces] ([ProductId]);
END
GO

-- 创建系统日志表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Logs' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Logs] (
        [LogId] int IDENTITY(1,1) NOT NULL,
        [UserId] int NULL,
        [ActionType] nvarchar(50) NOT NULL,
        [TargetType] nvarchar(50) NULL,
        [TargetId] int NULL,
        [Description] nvarchar(1000) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [IsSuccess] bit NOT NULL DEFAULT 1,
        [ActionTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Logs] PRIMARY KEY ([LogId])
    );
    
    CREATE INDEX [IX_Logs_UserId] ON [Logs] ([UserId]);
    CREATE INDEX [IX_Logs_ActionTime] ON [Logs] ([ActionTime] DESC);
END
GO

-- 创建文件上传记录表
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Uploads' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[Uploads] (
        [UploadId] int IDENTITY(1,1) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [UploadBy] int NOT NULL,
        [UploadTime] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Uploads] PRIMARY KEY ([UploadId]),
        CONSTRAINT [FK_Uploads_Users] FOREIGN KEY ([UploadBy]) REFERENCES [Users] ([UserId])
    );
    
    CREATE INDEX [IX_Uploads_UploadBy] ON [Uploads] ([UploadBy]);
END
GO

-- 插入默认管理员账户
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, Password, Phone, Role, CreateTime, LastLoginTime)
    VALUES ('admin', 'admin@farmdirest.com', 'AQAAAAEAACcQAAAAEK8xPrBfk+x8qZF3L3xzl8n2qZ8M5Q2x9K5L+wX7vN8k4J2M3Q6+dF9wH3K7vL2qZ8M5Q==', '13800138000', 'admin', GETDATE(), NULL);
END
GO

-- 插入示例配送区域
IF NOT EXISTS (SELECT * FROM DeliveryAreas WHERE Province = '北京市')
BEGIN
    INSERT INTO DeliveryAreas (Province, City, District, DeliveryFee, SupportSameDayDelivery, IsNationwide, Description, CreateTime, UpdateTime)
    VALUES 
    ('北京市', '北京市', '朝阳区', 10.00, 1, 0, '朝阳区配送', GETDATE(), GETDATE()),
    ('北京市', '北京市', '海淀区', 10.00, 1, 0, '海淀区配送', GETDATE(), GETDATE()),
    ('上海市', '上海市', '浦东新区', 12.00, 1, 0, '浦东新区配送', GETDATE(), GETDATE()),
    ('广东省', '深圳市', '南山区', 15.00, 0, 0, '深圳南山区配送', GETDATE(), GETDATE()),
    ('全国', '全国', '全国', 20.00, 0, 1, '全国配送', GETDATE(), GETDATE());
END
GO

PRINT '数据库初始化完成！';
PRINT '默认管理员账户: admin / 123456';
GO 