namespace SistemaParkingMahischa.Data;

public static class DatabaseSchema
{
    public const string Script = """
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        RoleName nvarchar(60) NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE
    );
END
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username nvarchar(50) NOT NULL,
        IdentificationNumber nvarchar(30) NULL,
        PasswordHash varbinary(32) NOT NULL,
        PasswordSalt varbinary(32) NOT NULL,
        FullName nvarchar(120) NOT NULL,
        RoleId int NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT UQ_Users_Username UNIQUE(Username),
        CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId)
    );
END
GO

IF COL_LENGTH('dbo.Users', 'IdentificationNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD IdentificationNumber nvarchar(30) NULL;
END
GO

IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD MustChangePassword bit NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT(0);
END
GO

IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RolePermissionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolePermissions PRIMARY KEY,
        RoleId int NOT NULL,
        PermissionKey nvarchar(80) NOT NULL,
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT UQ_RolePermissions UNIQUE(RoleId, PermissionKey)
    );
END
GO

IF OBJECT_ID('dbo.UserPermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermissions
    (
        UserPermissionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserPermissions PRIMARY KEY,
        UserId int NOT NULL,
        PermissionKey nvarchar(80) NOT NULL,
        CONSTRAINT FK_UserPermissions_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_UserPermissions UNIQUE(UserId, PermissionKey)
    );
END
GO

IF OBJECT_ID('dbo.ParkingRates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParkingRates
    (
        RateId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ParkingRates PRIMARY KEY,
        RateName nvarchar(80) NOT NULL,
        RateType nvarchar(30) NOT NULL,
        Amount decimal(18,2) NOT NULL,
        GraceMinutes int NOT NULL CONSTRAINT DF_ParkingRates_GraceMinutes DEFAULT(0),
        IsActive bit NOT NULL CONSTRAINT DF_ParkingRates_IsActive DEFAULT(1),
        SortOrder int NOT NULL CONSTRAINT DF_ParkingRates_SortOrder DEFAULT(0),
        UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_ParkingRates_UpdatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT CK_ParkingRates_Amount CHECK(Amount >= 0),
        CONSTRAINT CK_ParkingRates_Grace CHECK(GraceMinutes >= 0)
    );
END
GO

IF OBJECT_ID('dbo.ParkingSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParkingSessions
    (
        SessionId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ParkingSessions PRIMARY KEY,
        TicketCode uniqueidentifier NOT NULL CONSTRAINT DF_ParkingSessions_TicketCode DEFAULT(NEWID()),
        Plate nvarchar(20) NOT NULL,
        PlateNormalized nvarchar(20) NOT NULL,
        EntryAt datetime2(0) NOT NULL CONSTRAINT DF_ParkingSessions_EntryAt DEFAULT(SYSDATETIME()),
        ExitAt datetime2(0) NULL,
        RateId int NOT NULL,
        EnteredByUserId int NOT NULL,
        ExitedByUserId int NULL,
        Status char(1) NOT NULL CONSTRAINT DF_ParkingSessions_Status DEFAULT('A'),
        ChargedAmount decimal(18,2) NULL,
        Notes nvarchar(250) NULL,
        CONSTRAINT UQ_ParkingSessions_TicketCode UNIQUE(TicketCode),
        CONSTRAINT FK_ParkingSessions_Rates FOREIGN KEY(RateId) REFERENCES dbo.ParkingRates(RateId),
        CONSTRAINT FK_ParkingSessions_EnteredBy FOREIGN KEY(EnteredByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ParkingSessions_ExitedBy FOREIGN KEY(ExitedByUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_ParkingSessions_Status CHECK(Status IN ('A','C'))
    );
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        SessionId bigint NOT NULL,
        Amount decimal(18,2) NOT NULL,
        PaidAt datetime2(0) NOT NULL CONSTRAINT DF_Payments_PaidAt DEFAULT(SYSDATETIME()),
        UserId int NOT NULL,
        PaymentMethod nvarchar(30) NOT NULL CONSTRAINT DF_Payments_Method DEFAULT('Efectivo'),
        Reference nvarchar(80) NULL,
        EmployeeClosureId bigint NULL,
        CONSTRAINT UQ_Payments_SessionId UNIQUE(SessionId),
        CONSTRAINT FK_Payments_Sessions FOREIGN KEY(SessionId) REFERENCES dbo.ParkingSessions(SessionId),
        CONSTRAINT FK_Payments_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT CK_Payments_Amount CHECK(Amount >= 0)
    );
END
GO

IF OBJECT_ID('dbo.EmployeeClosures', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeClosures
    (
        EmployeeClosureId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeClosures PRIMARY KEY,
        UserId int NOT NULL,
        FromAt datetime2(0) NOT NULL,
        ToAt datetime2(0) NOT NULL,
        ExpectedAmount decimal(18,2) NOT NULL,
        DeliveredAmount decimal(18,2) NOT NULL,
        DifferenceAmount AS (DeliveredAmount - ExpectedAmount) PERSISTED,
        CreatedByUserId int NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_EmployeeClosures_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_EmployeeClosures_User FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_EmployeeClosures_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID('dbo.CashClosures', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashClosures
    (
        CashClosureId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashClosures PRIMARY KEY,
        ClosureDate date NOT NULL,
        MinimumCashAmount decimal(18,2) NOT NULL,
        SystemAmount decimal(18,2) NOT NULL,
        CountedAmount decimal(18,2) NOT NULL,
        DifferenceAmount AS (CountedAmount - SystemAmount - MinimumCashAmount) PERSISTED,
        CreatedByUserId int NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_CashClosures_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_CashClosures_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID('dbo.CashClosureDenominations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashClosureDenominations
    (
        CashClosureDenominationId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashClosureDenominations PRIMARY KEY,
        CashClosureId bigint NOT NULL,
        Denomination decimal(18,2) NOT NULL,
        Quantity int NOT NULL,
        TotalAmount AS (Denomination * Quantity) PERSISTED,
        CONSTRAINT FK_CashClosureDenominations_CashClosures FOREIGN KEY(CashClosureId) REFERENCES dbo.CashClosures(CashClosureId),
        CONSTRAINT CK_CashClosureDenominations_Qty CHECK(Quantity >= 0)
    );
END
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId int NULL,
        ActionKey nvarchar(80) NOT NULL,
        EntityName nvarchar(80) NOT NULL,
        EntityId nvarchar(80) NULL,
        Details nvarchar(500) NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSessions_ActivePlate' AND object_id = OBJECT_ID('dbo.ParkingSessions'))
    CREATE INDEX IX_ParkingSessions_ActivePlate ON dbo.ParkingSessions(PlateNormalized, EntryAt DESC) INCLUDE(TicketCode, RateId) WHERE Status = 'A';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSessions_EntryAt' AND object_id = OBJECT_ID('dbo.ParkingSessions'))
    CREATE INDEX IX_ParkingSessions_EntryAt ON dbo.ParkingSessions(EntryAt DESC) INCLUDE(PlateNormalized, Status, ChargedAmount);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSessions_ExitAt' AND object_id = OBJECT_ID('dbo.ParkingSessions'))
    CREATE INDEX IX_ParkingSessions_ExitAt ON dbo.ParkingSessions(ExitAt DESC) INCLUDE(Status, ChargedAmount, ExitedByUserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Payments_UserPaidAt' AND object_id = OBJECT_ID('dbo.Payments'))
    CREATE INDEX IX_Payments_UserPaidAt ON dbo.Payments(UserId, PaidAt DESC) INCLUDE(Amount, PaymentMethod);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_IdentificationNumber' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_IdentificationNumber ON dbo.Users(IdentificationNumber) WHERE IdentificationNumber IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Administrador')
    INSERT INTO dbo.Roles(RoleName) VALUES (N'Administrador');
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Empleado')
    INSERT INTO dbo.Roles(RoleName) VALUES (N'Empleado');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ParkingRates)
BEGIN
    INSERT INTO dbo.ParkingRates(RateName, RateType, Amount, GraceMinutes, SortOrder)
    VALUES
        (N'Por hora', N'Hora', 1000, 10, 1),
        (N'Por día', N'Dia', 6000, 15, 2),
        (N'Semanal', N'Semana', 30000, 30, 3),
        (N'Mensual', N'Mes', 90000, 60, 4);
END
GO
""";
}
