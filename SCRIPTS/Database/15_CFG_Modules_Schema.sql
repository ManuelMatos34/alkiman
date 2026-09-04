/*
    Módulo: PLATAFORMA MULTI-MÓDULO (catálogo de módulos + habilitación por negocio)
    Tablas: CFG_Modules, CFG_LandlordModules

    QUÉ ES:
    Alkiman pasa de ser una app de un solo propósito (Alquileres) a una
    plataforma con varios módulos (Alquileres, Citas, Carwash, Inventario,
    Facturación Electrónica, etc.). CFG_Modules es el catálogo global de
    módulos existentes (builtin, sembrado por esta migración). CFG_LandlordModules
    registra qué módulos tiene habilitados cada negocio -- hoy se habilita
    automáticamente "alquileres" al registrarse (gratis, como siempre), y el
    resto queda como catálogo "Próximamente" sin habilitar. La lógica de
    cobro por módulo (mensualidad) queda para una fase posterior; esta
    migración solo deja la estructura lista para sostenerla.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Drops (tablas nuevas, sin riesgo de pérdida de datos)
-- =========================================================
IF OBJECT_ID(N'dbo.CFG_LandlordModules', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_LandlordModules;
GO

IF OBJECT_ID(N'dbo.CFG_Modules', N'U') IS NOT NULL
    DROP TABLE dbo.CFG_Modules;
GO

-- =========================================================
-- Tabla: CFG_Modules
-- Catálogo global de módulos de la plataforma. Code es el identificador
-- estable usado por el front (rutas, íconos) y por CFG_LandlordModules.
-- =========================================================
CREATE TABLE dbo.CFG_Modules
(
    Code            NVARCHAR(30)    NOT NULL,
    Name            NVARCHAR(100)   NOT NULL,
    Description     NVARCHAR(300)   NULL,
    IconName        NVARCHAR(50)    NOT NULL,
    IsAvailable     BIT             NOT NULL CONSTRAINT DF_CFG_Modules_IsAvailable DEFAULT (0),
    SortOrder       INT             NOT NULL CONSTRAINT DF_CFG_Modules_SortOrder DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL CONSTRAINT DF_CFG_Modules_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)   NOT NULL,
    UpdatedAt       DATETIME2       NULL,
    UpdatedBy       NVARCHAR(255)   NULL,
    CONSTRAINT PK_CFG_Modules PRIMARY KEY (Code)
);
GO

-- =========================================================
-- Tabla: CFG_LandlordModules
-- Qué módulos tiene habilitados cada negocio. Hoy solo existe la fila
-- "alquileres" (habilitada automáticamente al registrarse). Preparada para
-- que a futuro habilitar un módulo dependa de un pago (billing, fase 2).
-- =========================================================
CREATE TABLE dbo.CFG_LandlordModules
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_CFG_LandlordModules_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    ModuleCode      NVARCHAR(30)       NOT NULL,
    EnabledAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_LandlordModules_EnabledAt DEFAULT SYSUTCDATETIME(),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_CFG_LandlordModules_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_CFG_LandlordModules PRIMARY KEY (Id),
    CONSTRAINT FK_CFG_LandlordModules_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_CFG_LandlordModules_CFG_Modules FOREIGN KEY (ModuleCode)
        REFERENCES dbo.CFG_Modules (Code)
);
GO

CREATE UNIQUE INDEX UQ_CFG_LandlordModules_Landlord_Module
    ON dbo.CFG_LandlordModules (LandlordId, ModuleCode);
GO

-- =========================================================
-- Seed: catálogo de módulos. "alquileres" es el único disponible hoy (lo que
-- ya existe); el resto queda como catálogo "Próximamente".
-- =========================================================
INSERT INTO dbo.CFG_Modules (Code, Name, Description, IconName, IsAvailable, SortOrder, CreatedAt, CreatedBy)
VALUES
    ('alquileres',  N'Alquileres',              N'Gestión de activos, clientes, rentas y pagos.', 'CalendarClock', 1, 1, SYSUTCDATETIME(), 'system'),
    ('citas',       N'Citas',                   N'Agenda y turnos para tu negocio.',               'CalendarCheck', 0, 2, SYSUTCDATETIME(), 'system'),
    ('carwash',     N'Carwash',                 N'Gestión de lavadero de vehículos.',               'Car',           0, 3, SYSUTCDATETIME(), 'system'),
    ('inventario',  N'Inventario',              N'Control de stock e insumos.',                     'Boxes',         0, 4, SYSUTCDATETIME(), 'system'),
    ('facturacion', N'Facturación Electrónica', N'Emisión de comprobantes electrónicos.',           'Receipt',       0, 5, SYSUTCDATETIME(), 'system');
GO

-- =========================================================
-- Backfill: todo negocio ya existente recibe "alquileres" habilitado (los
-- que se registren de acá en más lo reciben vía AuthService al alta).
-- =========================================================
INSERT INTO dbo.CFG_LandlordModules (LandlordId, ModuleCode, CreatedAt, CreatedBy)
SELECT l.Id, 'alquileres', SYSUTCDATETIME(), 'system'
FROM dbo.CFG_Landlords l
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CFG_LandlordModules lm
    WHERE lm.LandlordId = l.Id AND lm.ModuleCode = 'alquileres'
);
GO
