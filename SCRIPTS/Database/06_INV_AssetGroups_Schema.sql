/*
    Módulo: INVENTARIO (INV_) - Grupos de Activos
    Tablas: INV_AssetGroups, INV_AssetGroupAssets

    NOTA DE DISEÑO:
    Permite agrupar activos de CUALQUIER categoría bajo un nombre libre
    definido por el negocio (ej. "Herramientas", "Edificio A - Apartamentos").
    Relación N:N entre INV_AssetGroups e INV_Assets vía INV_AssetGroupAssets.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Drops (en orden inverso de dependencia)
-- =========================================================
IF OBJECT_ID(N'dbo.INV_AssetGroupAssets', N'U') IS NOT NULL
    DROP TABLE dbo.INV_AssetGroupAssets;
GO

IF OBJECT_ID(N'dbo.INV_AssetGroups', N'U') IS NOT NULL
    DROP TABLE dbo.INV_AssetGroups;
GO

-- =========================================================
-- Tabla: INV_AssetGroups
-- Grupo definido libremente por el negocio para organizar activos
-- de cualquier categoría (ej. "Herramientas", "Edificio A").
-- =========================================================
CREATE TABLE dbo.INV_AssetGroups
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    Name            NVARCHAR(150)      NOT NULL,
    Description     NVARCHAR(500)      NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_INV_AssetGroups_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_INV_AssetGroups PRIMARY KEY (Id),
    CONSTRAINT FK_INV_AssetGroups_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT UQ_INV_AssetGroups_Landlord_Name UNIQUE (LandlordId, Name)
);
GO

CREATE INDEX IX_INV_AssetGroups_LandlordId ON dbo.INV_AssetGroups (LandlordId);
GO

-- =========================================================
-- Tabla: INV_AssetGroupAssets
-- Relación N:N entre grupos y activos (un activo puede pertenecer
-- a más de un grupo; un grupo puede tener activos de cualquier categoría).
-- =========================================================
CREATE TABLE dbo.INV_AssetGroupAssets
(
    AssetGroupId    INT                NOT NULL,
    AssetId         UNIQUEIDENTIFIER   NOT NULL,
    AddedAt         DATETIME2          NOT NULL CONSTRAINT DF_INV_AssetGroupAssets_AddedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_INV_AssetGroupAssets PRIMARY KEY (AssetGroupId, AssetId),
    CONSTRAINT FK_INV_AssetGroupAssets_INV_AssetGroups FOREIGN KEY (AssetGroupId)
        REFERENCES dbo.INV_AssetGroups (Id) ON DELETE CASCADE,
    CONSTRAINT FK_INV_AssetGroupAssets_INV_Assets FOREIGN KEY (AssetId)
        REFERENCES dbo.INV_Assets (Id) ON DELETE CASCADE
);
GO

CREATE INDEX IX_INV_AssetGroupAssets_AssetId ON dbo.INV_AssetGroupAssets (AssetId);
GO
