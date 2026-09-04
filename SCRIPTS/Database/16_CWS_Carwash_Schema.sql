/*
    Módulo: CARWASH (MVP) -- cola de lavado presencial + auto-registro por portal público
    Tablas: CWS_Services, CWS_PortalLinks, CWS_Tickets

    QUÉ ES:
    Gestión de la cola de vehículos de un carwash. Un Encargado registra
    vehículos presencialmente, o el cliente se auto-registra desde un link
    público (CWS_PortalLinks) sin estar físicamente presente -- en ese caso
    el ticket nace con un ArrivalDeadline y debe confirmarse su llegada. El
    estado del vehículo (CWS_Tickets.Status) se actualiza a medida que avanza
    el lavado y dispara una notificación best-effort al cliente en cada
    cambio (reutiliza IEmailSender, sin tabla de historial propia). No
    incluye foodshop ni cobro/POS -- eso queda para una fase posterior.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Drops (tablas nuevas, sin riesgo de pérdida de datos)
-- =========================================================
IF OBJECT_ID(N'dbo.CWS_Tickets', N'U') IS NOT NULL
    DROP TABLE dbo.CWS_Tickets;
GO
IF OBJECT_ID(N'dbo.CWS_PortalLinks', N'U') IS NOT NULL
    DROP TABLE dbo.CWS_PortalLinks;
GO
IF OBJECT_ID(N'dbo.CWS_Services', N'U') IS NOT NULL
    DROP TABLE dbo.CWS_Services;
GO

-- =========================================================
-- Tabla: CWS_Services
-- Catálogo de servicios de lavado por negocio (ej: "Lavado básico",
-- "Lavado + encerado"). Análogo a CFG_Categories pero propio de Carwash.
-- =========================================================
CREATE TABLE dbo.CWS_Services
(
    Id                INT              IDENTITY(1,1) NOT NULL,
    LandlordId        UNIQUEIDENTIFIER NOT NULL,
    Name              NVARCHAR(100)    NOT NULL,
    Description       NVARCHAR(300)    NULL,
    Price             DECIMAL(10,2)    NOT NULL CONSTRAINT DF_CWS_Services_Price DEFAULT (0),
    EstimatedMinutes  INT              NOT NULL CONSTRAINT DF_CWS_Services_EstimatedMinutes DEFAULT (30),
    IsActive          BIT              NOT NULL CONSTRAINT DF_CWS_Services_IsActive DEFAULT (1),
    CreatedAt         DATETIME2        NOT NULL CONSTRAINT DF_CWS_Services_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy         NVARCHAR(255)    NOT NULL,
    UpdatedAt         DATETIME2        NULL,
    UpdatedBy         NVARCHAR(255)    NULL,
    CONSTRAINT PK_CWS_Services PRIMARY KEY (Id),
    CONSTRAINT FK_CWS_Services_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id)
);
GO
CREATE UNIQUE INDEX UQ_CWS_Services_Landlord_Name ON dbo.CWS_Services (LandlordId, Name);
GO

-- =========================================================
-- Tabla: CWS_PortalLinks
-- Link público por negocio para que un cliente se auto-registre en la cola.
-- Mismo patrón que PRT_PortalLinks (Slug único, sin AssetGroup porque acá no
-- aplica el concepto de grupo de activos).
-- =========================================================
CREATE TABLE dbo.CWS_PortalLinks
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CWS_PortalLinks_Id DEFAULT NEWID(),
    LandlordId  UNIQUEIDENTIFIER NOT NULL,
    Title       NVARCHAR(150)    NOT NULL,
    Slug        NVARCHAR(80)     NOT NULL,
    IsActive    BIT              NOT NULL CONSTRAINT DF_CWS_PortalLinks_IsActive DEFAULT (1),
    CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_CWS_PortalLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy   NVARCHAR(255)    NOT NULL,
    UpdatedAt   DATETIME2        NULL,
    UpdatedBy   NVARCHAR(255)    NULL,
    CONSTRAINT PK_CWS_PortalLinks PRIMARY KEY (Id),
    CONSTRAINT FK_CWS_PortalLinks_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id)
);
GO
CREATE UNIQUE INDEX UQ_CWS_PortalLinks_Slug ON dbo.CWS_PortalLinks (Slug);
GO

-- =========================================================
-- Tabla: CWS_Tickets
-- Un vehículo en la cola. Source distingue si lo registró el Encargado
-- (Presencial) o el propio cliente (Portal). AccessToken resuelve la página
-- pública de estado del turno (como TRX_Rentals.AccessToken).
-- =========================================================
CREATE TABLE dbo.CWS_Tickets
(
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CWS_Tickets_Id DEFAULT NEWID(),
    LandlordId           UNIQUEIDENTIFIER NOT NULL,
    CustomerId           UNIQUEIDENTIFIER NOT NULL,
    ServiceId            INT              NOT NULL,
    AssignedToUserId      UNIQUEIDENTIFIER NULL,
    QueueNumber          INT              NOT NULL,
    VehiclePlate         NVARCHAR(20)     NOT NULL,
    VehicleDescription   NVARCHAR(150)    NULL,
    Status               NVARCHAR(20)     NOT NULL CONSTRAINT DF_CWS_Tickets_Status DEFAULT ('Waiting'),
    Source                NVARCHAR(20)     NOT NULL CONSTRAINT DF_CWS_Tickets_Source DEFAULT ('Presencial'),
    AccessToken           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CWS_Tickets_AccessToken DEFAULT NEWID(),
    ArrivalDeadline       DATETIME2        NULL,
    ArrivedAt             DATETIME2        NULL,
    StartedAt             DATETIME2        NULL,
    ReadyAt               DATETIME2        NULL,
    DeliveredAt           DATETIME2        NULL,
    CancelledAt           DATETIME2        NULL,
    Notes                 NVARCHAR(500)    NULL,
    CreatedAt             DATETIME2        NOT NULL CONSTRAINT DF_CWS_Tickets_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy             NVARCHAR(255)    NOT NULL,
    UpdatedAt             DATETIME2        NULL,
    UpdatedBy             NVARCHAR(255)    NULL,
    CONSTRAINT PK_CWS_Tickets PRIMARY KEY (Id),
    CONSTRAINT FK_CWS_Tickets_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_CWS_Tickets_CRM_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT FK_CWS_Tickets_CWS_Services FOREIGN KEY (ServiceId) REFERENCES dbo.CWS_Services (Id),
    CONSTRAINT FK_CWS_Tickets_CFG_Users FOREIGN KEY (AssignedToUserId) REFERENCES dbo.CFG_Users (Id),
    CONSTRAINT CK_CWS_Tickets_Status CHECK (Status IN ('Waiting','ArrivalPending','InProgress','Drying','Ready','Delivered','Cancelled','Expired')),
    CONSTRAINT CK_CWS_Tickets_Source CHECK (Source IN ('Presencial','Portal'))
);
GO
CREATE UNIQUE INDEX UQ_CWS_Tickets_AccessToken ON dbo.CWS_Tickets (AccessToken);
GO
CREATE INDEX IX_CWS_Tickets_Landlord_Status ON dbo.CWS_Tickets (LandlordId, Status);
GO

-- =========================================================
-- Permisos: carwash.view / carwash.manage (patrón idéntico al de
-- RentalRequests en 11_TRX_RentalRequests_Schema.sql)
-- =========================================================
INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
SELECT v.Code, v.Module, v.Description
FROM (VALUES
    ('carwash.view',   N'Carwash', N'Ver la cola y el catálogo de servicios de Carwash'),
    ('carwash.manage', N'Carwash', N'Registrar vehículos, cambiar estados y administrar servicios/links de Carwash')
) AS v(Code, Module, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions p WHERE p.Code = v.Code);
GO

INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.CFG_Roles r
CROSS JOIN dbo.CFG_Permissions p
WHERE r.IsSystem = 1
  AND p.Code IN ('carwash.view','carwash.manage')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- =========================================================
-- Habilitar el módulo "carwash" en el catálogo (deja de ser "Próximamente").
-- No se hace backfill de CFG_LandlordModules: cada negocio ya existente lo
-- habilita manualmente desde el selector (botón "Habilitar" -> POST
-- /api/modules/{code}/enable), igual criterio que a futuro cualquier
-- módulo. Solo "alquileres" se auto-habilita al registrarse.
-- =========================================================
UPDATE dbo.CFG_Modules SET IsAvailable = 1 WHERE Code = 'carwash';
GO
