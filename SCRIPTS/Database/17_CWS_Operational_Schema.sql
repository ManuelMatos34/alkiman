/*
    Módulo: CARWASH -- flujo operativo (extiende 16_CWS_Carwash_Schema.sql)
    Tablas nuevas: CWS_ServiceExtras, CWS_TicketExtras, CWS_Settings
    Tablas alteradas: CWS_Tickets

    QUÉ AGREGA:
    1. Datos estructurados del vehículo (marca/modelo/año/color) en reemplazo
       del campo libre VehicleDescription.
    2. Extras aditivos por ticket (encerado, ozono, ...): un catálogo por
       negocio (CWS_ServiceExtras) y los extras elegidos en cada ticket
       (CWS_TicketExtras) con nombre y precio CONGELADOS al momento del alta,
       para que cambiar la lista de precios no reescriba el historial.
    3. Estado 'Waxing' (Encerando), opcional entre Drying y Ready.
    4. Modo de operación por negocio (CWS_Settings): 'Empresa' (varios
       lavadores, se asigna el trabajo) o 'Solitario' (una sola persona, el
       ticket se auto-asigna). Se elige la primera vez que se entra al módulo.
    5. Permiso 'carwash.work': avanzar vehículos en la cola. Se separa de
       'carwash.manage' porque el rol Lavador debe poder mover la cola SIN
       poder borrar el catálogo de servicios ni los links públicos.

    IDEMPOTENTE: se puede correr varias veces (mismo criterio que los scripts
    13 y 14). NO hace DROP TABLE de lo que creó el script 16.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- 1. CWS_Tickets: datos estructurados del vehículo
-- Reemplazan a VehicleDescription (texto libre). Todas NULL: el alta rápida
-- del portal público puede no conocerlos, y el Encargado los completa después.
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'VehicleBrand')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD VehicleBrand NVARCHAR(60) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'VehicleModel')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD VehicleModel NVARCHAR(60) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'VehicleYear')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD VehicleYear INT NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'VehicleColor')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD VehicleColor NVARCHAR(40) NULL;
END
GO

-- Precio del servicio base congelado al momento del alta. Va junto con el
-- snapshot de CWS_TicketExtras: sin esto, el total de un ticket viejo cambiaría
-- al retocar la lista de precios, y las métricas de ingresos serían falsas.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'ServicePrice')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD ServicePrice DECIMAL(10,2) NOT NULL CONSTRAINT DF_CWS_Tickets_ServicePrice DEFAULT (0);
END
GO

-- Baja de VehicleDescription: queda reemplazada por los 4 campos de arriba.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'VehicleDescription')
BEGIN
    ALTER TABLE dbo.CWS_Tickets DROP COLUMN VehicleDescription;
END
GO

-- =========================================================
-- 2. CWS_Tickets: estado 'Waxing' (Encerando)
-- Hay que recrear el CHECK porque no se puede extender in situ.
-- =========================================================
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CWS_Tickets_Status' AND parent_object_id = OBJECT_ID(N'dbo.CWS_Tickets'))
BEGIN
    ALTER TABLE dbo.CWS_Tickets DROP CONSTRAINT CK_CWS_Tickets_Status;
END
GO
ALTER TABLE dbo.CWS_Tickets ADD CONSTRAINT CK_CWS_Tickets_Status
    CHECK (Status IN ('Waiting','ArrivalPending','InProgress','Drying','Waxing','Ready','Delivered','Cancelled','Expired'));
GO

-- =========================================================
-- 3. Tabla: CWS_ServiceExtras
-- Catálogo de agregados por negocio (Encerado, Ozono, ...). Se suman AL
-- servicio base en lugar de multiplicar el catálogo con una entrada por cada
-- combinación posible. Mismo patrón que CWS_Services.
-- =========================================================
IF OBJECT_ID(N'dbo.CWS_ServiceExtras', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CWS_ServiceExtras
    (
        Id                INT              IDENTITY(1,1) NOT NULL,
        LandlordId        UNIQUEIDENTIFIER NOT NULL,
        Name              NVARCHAR(100)    NOT NULL,
        Description       NVARCHAR(300)    NULL,
        Price             DECIMAL(10,2)    NOT NULL CONSTRAINT DF_CWS_ServiceExtras_Price DEFAULT (0),
        EstimatedMinutes  INT              NOT NULL CONSTRAINT DF_CWS_ServiceExtras_EstimatedMinutes DEFAULT (0),
        IsActive          BIT              NOT NULL CONSTRAINT DF_CWS_ServiceExtras_IsActive DEFAULT (1),
        CreatedAt         DATETIME2        NOT NULL CONSTRAINT DF_CWS_ServiceExtras_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy         NVARCHAR(255)    NOT NULL,
        UpdatedAt         DATETIME2        NULL,
        UpdatedBy         NVARCHAR(255)    NULL,
        CONSTRAINT PK_CWS_ServiceExtras PRIMARY KEY (Id),
        CONSTRAINT FK_CWS_ServiceExtras_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id)
    );

    CREATE UNIQUE INDEX UQ_CWS_ServiceExtras_Landlord_Name ON dbo.CWS_ServiceExtras (LandlordId, Name);
END
GO

-- =========================================================
-- Tabla: CWS_TicketExtras
-- Los extras elegidos en un ticket. Name/Price son un SNAPSHOT: se copian del
-- catálogo al dar de alta el ticket y no se vuelven a leer. Por eso un extra
-- ya usado se desactiva en vez de borrarse (no hay ON DELETE del lado del
-- catálogo), igual criterio que CWS_Services con los tickets.
-- =========================================================
IF OBJECT_ID(N'dbo.CWS_TicketExtras', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CWS_TicketExtras
    (
        TicketId   UNIQUEIDENTIFIER NOT NULL,
        ExtraId    INT              NOT NULL,
        Name       NVARCHAR(100)    NOT NULL,
        Price      DECIMAL(10,2)    NOT NULL,
        CONSTRAINT PK_CWS_TicketExtras PRIMARY KEY (TicketId, ExtraId),
        CONSTRAINT FK_CWS_TicketExtras_CWS_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.CWS_Tickets (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CWS_TicketExtras_CWS_ServiceExtras FOREIGN KEY (ExtraId) REFERENCES dbo.CWS_ServiceExtras (Id)
    );
END
GO

-- =========================================================
-- 4. Tabla: CWS_Settings
-- Configuración del módulo por negocio. Una fila por negocio (LandlordId es
-- la PK, no hay Id propio). Vive acá y no en CFG_LandlordModules porque esa
-- tabla es el catálogo genérico de módulos habilitados y no debe conocer los
-- campos particulares de Carwash.
--
-- La AUSENCIA de fila es significativa: quiere decir "el negocio todavía no
-- eligió modo", y es lo que dispara el diálogo de configuración inicial.
-- =========================================================
IF OBJECT_ID(N'dbo.CWS_Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CWS_Settings
    (
        LandlordId     UNIQUEIDENTIFIER NOT NULL,
        OperationMode  NVARCHAR(20)     NOT NULL,
        CreatedAt      DATETIME2        NOT NULL CONSTRAINT DF_CWS_Settings_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy      NVARCHAR(255)    NOT NULL,
        UpdatedAt      DATETIME2        NULL,
        UpdatedBy      NVARCHAR(255)    NULL,
        CONSTRAINT PK_CWS_Settings PRIMARY KEY (LandlordId),
        CONSTRAINT FK_CWS_Settings_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id),
        CONSTRAINT CK_CWS_Settings_OperationMode CHECK (OperationMode IN ('Empresa','Solitario'))
    );
END
GO

-- =========================================================
-- 5. Permiso 'carwash.work'
-- Separa "mover la cola" de "administrar el módulo": el rol Lavador necesita
-- lo primero pero no lo segundo. Los roles de sistema (Administrador) lo
-- reciben también para no perder la capacidad de avanzar tickets.
-- =========================================================
INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
SELECT v.Code, v.Module, v.Description
FROM (VALUES
    ('carwash.work', N'Carwash', N'Avanzar el estado de los vehículos en la cola de Carwash')
) AS v(Code, Module, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions p WHERE p.Code = v.Code);
GO

INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.CFG_Roles r
CROSS JOIN dbo.CFG_Permissions p
WHERE r.IsSystem = 1
  AND p.Code = 'carwash.work'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- NOTA: el rol "Lavador" (IsSystem = 1, no editable ni eliminable) NO se
-- siembra acá. Lo crea la aplicación al guardar la configuración inicial del
-- módulo (CarwashService.SaveSettingsAsync), porque todo negocio con Carwash
-- habilitado pasa por ese diálogo antes de poder usar la cola. Sembrarlo
-- también en este script duplicaría la regla en dos lugares.
GO
