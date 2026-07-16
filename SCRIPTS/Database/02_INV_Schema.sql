/*
    Módulo: INVENTARIO (INV_)
    Tablas: INV_Assets, INV_AssetBlocks

    NOTA DE DISEÑO:
    INV_AssetBlocks no está en el diccionario de datos original.
    Se agrega para cubrir RF-B2 (bloqueo manual de fechas en el
    calendario sin que exista un cliente/contrato de por medio,
    ej. "voy a pintar el apartamento" o "reparar el auto").
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Tabla: INV_Assets
-- Tabla híbrida central: modela inmuebles (largo plazo)
-- y objetos/vehículos (corto plazo).
-- =========================================================
IF OBJECT_ID(N'dbo.INV_Assets', N'U') IS NOT NULL
    DROP TABLE dbo.INV_Assets;
GO

CREATE TABLE dbo.INV_Assets
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_INV_Assets_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    CategoryId      INT                NOT NULL,
    Name            NVARCHAR(150)      NOT NULL,
    Description     NVARCHAR(MAX)      NULL,
    ImageUrl        NVARCHAR(500)      NULL,
    Status          NVARCHAR(20)       NOT NULL CONSTRAINT DF_INV_Assets_Status DEFAULT ('Available'),
    RentalType      NVARCHAR(20)       NOT NULL,
    BasePrice       DECIMAL(18,2)      NOT NULL,
    Stock           INT                NOT NULL CONSTRAINT DF_INV_Assets_Stock DEFAULT (1),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_INV_Assets_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_INV_Assets PRIMARY KEY (Id),
    CONSTRAINT FK_INV_Assets_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_INV_Assets_CFG_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.CFG_Categories (Id),
    CONSTRAINT CK_INV_Assets_Status CHECK (Status IN ('Available', 'Rented', 'Maintenance')),
    CONSTRAINT CK_INV_Assets_RentalType CHECK (RentalType IN ('LongTerm', 'ShortTerm')),
    CONSTRAINT CK_INV_Assets_BasePrice CHECK (BasePrice >= 0),
    CONSTRAINT CK_INV_Assets_Stock CHECK (Stock >= 0)
);
GO

CREATE INDEX IX_INV_Assets_LandlordId ON dbo.INV_Assets (LandlordId);
CREATE INDEX IX_INV_Assets_CategoryId ON dbo.INV_Assets (CategoryId);
GO

-- =========================================================
-- Tabla: INV_AssetBlocks  (NUEVA - cubre RF-B2)
-- Bloqueos manuales de fechas sobre un activo, sin cliente
-- ni contrato asociado (mantenimiento, reparación, etc.).
-- =========================================================
IF OBJECT_ID(N'dbo.INV_AssetBlocks', N'U') IS NOT NULL
    DROP TABLE dbo.INV_AssetBlocks;
GO

CREATE TABLE dbo.INV_AssetBlocks
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_INV_AssetBlocks_Id DEFAULT NEWID(),
    AssetId         UNIQUEIDENTIFIER   NOT NULL,
    StartDate       DATETIME2          NOT NULL,
    EndDate         DATETIME2          NOT NULL,
    Reason          NVARCHAR(255)      NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_INV_AssetBlocks_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_INV_AssetBlocks PRIMARY KEY (Id),
    CONSTRAINT FK_INV_AssetBlocks_INV_Assets FOREIGN KEY (AssetId)
        REFERENCES dbo.INV_Assets (Id),
    CONSTRAINT CK_INV_AssetBlocks_Dates CHECK (EndDate > StartDate)
);
GO

CREATE INDEX IX_INV_AssetBlocks_AssetId ON dbo.INV_AssetBlocks (AssetId);
GO
