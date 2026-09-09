/*
    Módulo: BARBERÍA — Tablas, permisos y seed inicial
    Prefijo de tablas: BRB_

    ORDEN OBLIGATORIO:
      1. CFG_Modules (FK padre de CFG_Permissions.ModuleCode)
      2. Tablas BRB_*
      3. CFG_Permissions (FK a CFG_Modules)
      4. CFG_RolePermissions seed
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- 1. Registrar el módulo en el catálogo
-- =========================================================
INSERT INTO dbo.CFG_Modules (Code, Name, Description, IconName, IsAvailable, SortOrder, CreatedBy)
SELECT 'barbershop',
       'Barberia',
       'Modulo de gestion de citas para barberias y salones de belleza',
       'Scissors',
       1,
       50,
       'system'
WHERE NOT EXISTS (SELECT 1 FROM dbo.CFG_Modules WHERE Code = 'barbershop');
GO

-- =========================================================
-- 2. Tablas del módulo (idempotentes con IF NOT EXISTS)
-- =========================================================
IF OBJECT_ID('dbo.BRB_Services', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BRB_Services (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        LandlordId       UNIQUEIDENTIFIER NOT NULL,
        Name             NVARCHAR(150)    NOT NULL,
        Description      NVARCHAR(500)    NULL,
        Price            DECIMAL(18,2)    NOT NULL DEFAULT 0,
        DurationMinutes  INT              NOT NULL DEFAULT 30,
        IsActive         BIT              NOT NULL DEFAULT 1,
        CreatedAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy        NVARCHAR(200)    NOT NULL,
        UpdatedAt        DATETIME2        NULL,
        UpdatedBy        NVARCHAR(200)    NULL
    );
END
GO

IF OBJECT_ID('dbo.BRB_Stylists', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BRB_Stylists (
        Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        LandlordId  UNIQUEIDENTIFIER NOT NULL,
        FullName    NVARCHAR(150)    NOT NULL,
        Phone       NVARCHAR(30)     NULL,
        Email       NVARCHAR(200)    NULL,
        IsActive    BIT              NOT NULL DEFAULT 1,
        CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy   NVARCHAR(200)    NOT NULL,
        UpdatedAt   DATETIME2        NULL,
        UpdatedBy   NVARCHAR(200)    NULL
    );
END
GO

IF OBJECT_ID('dbo.BRB_PortalLinks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BRB_PortalLinks (
        Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        LandlordId  UNIQUEIDENTIFIER NOT NULL,
        StylistId   UNIQUEIDENTIFIER NULL REFERENCES dbo.BRB_Stylists(Id),
        Title       NVARCHAR(150)    NOT NULL,
        Slug        NVARCHAR(100)    NOT NULL,
        IsActive    BIT              NOT NULL DEFAULT 1,
        CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy   NVARCHAR(200)    NOT NULL,
        UpdatedAt   DATETIME2        NULL,
        UpdatedBy   NVARCHAR(200)    NULL,
        CONSTRAINT UQ_BRB_PortalLinks_Slug UNIQUE (Slug)
    );
END
GO

IF OBJECT_ID('dbo.BRB_Appointments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BRB_Appointments (
        Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        LandlordId      UNIQUEIDENTIFIER NOT NULL,
        StylistId       UNIQUEIDENTIFIER NULL REFERENCES dbo.BRB_Stylists(Id),
        ServiceId       INT              NULL REFERENCES dbo.BRB_Services(Id),
        PortalLinkId    UNIQUEIDENTIFIER NULL REFERENCES dbo.BRB_PortalLinks(Id),
        TrackingToken   NVARCHAR(100)    NOT NULL DEFAULT CONVERT(NVARCHAR(100), NEWID()),
        ClientName      NVARCHAR(150)    NOT NULL,
        ClientPhone     NVARCHAR(30)     NULL,
        ClientEmail     NVARCHAR(200)    NULL,
        Notes           NVARCHAR(500)    NULL,
        ScheduledAt     DATETIME2        NOT NULL,
        Source          NVARCHAR(20)     NOT NULL DEFAULT 'Manual',
        Status          NVARCHAR(20)     NOT NULL DEFAULT 'Scheduled',
        CreatedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy       NVARCHAR(200)    NOT NULL,
        UpdatedAt       DATETIME2        NULL,
        UpdatedBy       NVARCHAR(200)    NULL
    );
END
GO

-- =========================================================
-- 3. Permisos granulares (DESPUÉS del módulo: FK a CFG_Modules.Code)
-- =========================================================
INSERT INTO dbo.CFG_Permissions (Code, Module, Description, ModuleCode)
SELECT v.Code, v.Module, v.Description, 'barbershop'
FROM (VALUES
    ('barbershop.board.view',     'Barberia - Tablero',   'Ver el tablero de citas'),
    ('barbershop.board.manage',   'Barberia - Tablero',   'Crear citas y cancelarlas desde el sistema'),
    ('barbershop.board.work',     'Barberia - Tablero',   'Avanzar el estado de las citas'),
    ('barbershop.catalog.view',   'Barberia - Catalogo',  'Ver catalogo de servicios'),
    ('barbershop.catalog.manage', 'Barberia - Catalogo',  'Crear, editar y eliminar servicios'),
    ('barbershop.stylists.view',  'Barberia - Estilistas','Ver directorio de estilistas'),
    ('barbershop.stylists.manage','Barberia - Estilistas','Agregar y editar estilistas'),
    ('barbershop.portal.view',    'Barberia - Portal',    'Ver links de portal publico'),
    ('barbershop.portal.manage',  'Barberia - Portal',    'Crear, activar y eliminar links de portal'),
    ('barbershop.reports',        'Barberia - Reportes',  'Ver metricas y reportes de la barberia')
) AS v(Code, Module, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions p WHERE p.Code = v.Code);
GO

-- =========================================================
-- 4. Asignar todos los permisos barbershop al rol Administrador
--    de cada negocio registrado
-- =========================================================
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT DISTINCT r.Id, p.Id
FROM dbo.CFG_Roles r
CROSS JOIN dbo.CFG_Permissions p
WHERE r.IsSystem = 1
  AND r.Name = 'Administrador'
  AND p.ModuleCode = 'barbershop'
  AND NOT EXISTS (
    SELECT 1 FROM dbo.CFG_RolePermissions rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

SELECT 'Modulo Barberia instalado correctamente' AS Resultado;
GO
