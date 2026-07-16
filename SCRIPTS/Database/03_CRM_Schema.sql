/*
    Módulo: CLIENTES (CRM_)
    Tablas: CRM_Customers
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Tabla: CRM_Customers
-- Directorio de inquilinos/clientes que rentan los activos.
-- =========================================================
IF OBJECT_ID(N'dbo.CRM_Customers', N'U') IS NOT NULL
    DROP TABLE dbo.CRM_Customers;
GO

CREATE TABLE dbo.CRM_Customers
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_CRM_Customers_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    FullName        NVARCHAR(150)      NOT NULL,
    IdentityNumber  NVARCHAR(50)       NOT NULL,
    Phone           NVARCHAR(20)       NULL,
    Email           NVARCHAR(100)      NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CRM_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CRM_Customers PRIMARY KEY (Id),
    CONSTRAINT FK_CRM_Customers_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT UQ_CRM_Customers_Landlord_Identity UNIQUE (LandlordId, IdentityNumber)
);
GO

CREATE INDEX IX_CRM_Customers_LandlordId ON dbo.CRM_Customers (LandlordId);
GO
