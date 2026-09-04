/*
    Módulo: CONFIGURACIÓN (CFG_)
    Tabla afectada: CFG_Permissions (nueva columna ModuleCode)

    QUÉ RESUELVE:
    Hasta ahora CFG_Permissions.Module era solo una etiqueta de AGRUPACIÓN VISUAL
    para el editor de roles ('Activos', 'Clientes', 'Carwash'). No decía a qué
    módulo de la plataforma (CFG_Modules) pertenece el permiso, así que:

      1) el editor de roles ofrecía permisos de módulos que el negocio no compró;
      2) el rol "Administrador" recibía TODOS los permisos al registrarse,
         incluidos los de módulos no habilitados;
      3) la API no tenía forma de saber si una request pertenece a un módulo
         que el negocio realmente contrató.

    ModuleCode cierra ese hueco: es la pertenencia real del permiso.

    CONVENCIÓN DE ModuleCode:
      - NOT NULL -> el permiso pertenece a ese módulo y solo aplica si el
        negocio lo tiene habilitado en CFG_LandlordModules.
      - NULL     -> permiso de PLATAFORMA (transversal): existe siempre, sin
        importar qué módulos se hayan comprado. Son los de administración del
        negocio (ajustes, usuarios, roles, bitácora) y los de clientes, porque
        CRM_Customers es compartido: un mismo cliente puede tener rentas en
        Alquileres y turnos en Carwash (ver FK_CWS_Tickets_CRM_Customers).

    Se mantiene sincronizado a mano con
    Alkiman.Application.Common.Permissions.PermissionCatalog.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Columna ModuleCode (idempotente)
-- =========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Permissions') AND name = N'ModuleCode'
)
BEGIN
    ALTER TABLE dbo.CFG_Permissions ADD ModuleCode NVARCHAR(30) NULL;
END
GO

-- =========================================================
-- Backfill: pertenencia de cada permiso.
-- Se ejecuta siempre (no solo al crear la columna) para que agregar un permiso
-- nuevo al catálogo no quede sin clasificar por olvido.
-- =========================================================

-- Plataforma (NULL): administración del negocio + clientes (CRM compartido).
UPDATE dbo.CFG_Permissions
SET ModuleCode = NULL
WHERE Code IN (
    'settings.manage',
    'users.manage',
    'roles.manage',
    'audit.view',
    'customers.view',
    'customers.manage'
);
GO

-- Alquileres
UPDATE dbo.CFG_Permissions
SET ModuleCode = 'alquileres'
WHERE Code IN (
    'assets.view',          'assets.manage',
    'assetgroups.view',     'assetgroups.manage',
    'rentals.view',         'rentals.manage',
    'payments.view',        'payments.manage',
    'categories.view',      'categories.manage',
    'reports.view',
    'emails.view',          'emails.manage',
    'portal.view',          'portal.manage',
    'contracts.view',       'contracts.manage',
    'rentalrequests.view',  'rentalrequests.manage'
);
GO

-- Carwash
UPDATE dbo.CFG_Permissions
SET ModuleCode = 'carwash'
WHERE Code LIKE 'carwash.%';
GO

-- =========================================================
-- FK contra el catálogo de módulos. Se agrega DESPUÉS del backfill para que
-- no falle con filas viejas apuntando a un código inexistente.
-- =========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CFG_Permissions_CFG_Modules'
)
BEGIN
    ALTER TABLE dbo.CFG_Permissions
        ADD CONSTRAINT FK_CFG_Permissions_CFG_Modules
        FOREIGN KEY (ModuleCode) REFERENCES dbo.CFG_Modules (Code);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Permissions') AND name = N'IX_CFG_Permissions_ModuleCode'
)
BEGIN
    CREATE INDEX IX_CFG_Permissions_ModuleCode ON dbo.CFG_Permissions (ModuleCode);
END
GO

-- =========================================================
-- Limpieza de permisos mal repartidos.
-- Los scripts 16 y 17 le dieron carwash.* a TODO rol IsSystem = 1 de TODO
-- negocio, incluso los que nunca habilitaron el módulo. Se quitan de los roles
-- de los negocios sin Carwash habilitado; el provisioner de módulos se los
-- volverá a dar al rol Administrador el día que lo compren.
--
-- No toca roles de negocios que SÍ tienen Carwash, ni el rol "Lavador".
-- =========================================================
DELETE rp
FROM dbo.CFG_RolePermissions rp
INNER JOIN dbo.CFG_Roles r      ON r.Id = rp.RoleId
INNER JOIN dbo.CFG_Permissions p ON p.Id = rp.PermissionId
WHERE p.ModuleCode IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_LandlordModules lm
      WHERE lm.LandlordId = r.LandlordId
        AND lm.ModuleCode = p.ModuleCode
  );
GO
