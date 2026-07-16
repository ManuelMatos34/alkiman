/*
    Módulo: CONFIGURACIÓN (CFG_)
    Tablas: CFG_Landlords, CFG_Categories
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Tabla: CFG_Landlords
-- Propietarios que administran sus negocios en la plataforma.
-- =========================================================
IF OBJECT_ID(N'dbo.CFG_Landlords', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Landlords;
GO

CREATE TABLE dbo.CFG_Landlords
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_CFG_Landlords_Id DEFAULT NEWID(),
    Auth0UserId     NVARCHAR(255)      NOT NULL,
    BusinessName    NVARCHAR(150)      NOT NULL,
    Email           NVARCHAR(100)      NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Landlords_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Landlords PRIMARY KEY (Id),
    CONSTRAINT UQ_CFG_Landlords_Auth0UserId UNIQUE (Auth0UserId)
);
GO

-- =========================================================
-- Tabla: CFG_Categories
-- Agrupa los activos para su correcta organización.
-- =========================================================
IF OBJECT_ID(N'dbo.CFG_Categories', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Categories;
GO

CREATE TABLE dbo.CFG_Categories
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    Name            NVARCHAR(50)       NOT NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_Categories PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_Categories_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT UQ_CFG_Categories_Landlord_Name UNIQUE (LandlordId, Name)
);
GO

CREATE INDEX IX_CFG_Categories_LandlordId ON dbo.CFG_Categories (LandlordId);
GO
