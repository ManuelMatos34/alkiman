/*
    Módulo: CARWASH — Permisos granulares por sección
    Tablas afectadas: CFG_Permissions, CFG_RolePermissions

    QUÉ RESUELVE:
    Los permisos carwash.view y carwash.manage eran demasiado amplios: un negocio
    no puede darle a un empleado acceso al catálogo sin darle también acceso a los
    links de portal y al directorio de lavadores (y viceversa). Con los nuevos
    permisos granulares cada sección del módulo se puede habilitar de forma
    independiente al armar un rol.

    PERMISOS QUE SE ELIMINAN:
      carwash.view    -> reemplazado por carwash.board.view, carwash.catalog.view,
                         carwash.washers.view, carwash.portal.view
      carwash.manage  -> reemplazado por carwash.board.manage, carwash.catalog.manage,
                         carwash.washers.manage, carwash.portal.manage
      carwash.work    -> reemplazado por carwash.board.work

    PERMISOS QUE SE MANTIENEN:
      carwash.caja    -> sin cambios
      carwash.reports -> sin cambios

    MIGRACIÓN DE ROLES EXISTENTES:
    Los roles que tenían carwash.view o carwash.manage reciben automáticamente
    todos los permisos granulares equivalentes (equivalencia total). Los roles que
    tenían carwash.work reciben carwash.board.work.

    SINCRONIZACIÓN con PermissionCatalog.cs:
    Este script refleja el catálogo definido en:
    Alkiman.Application.Common.Permissions.PermissionCatalog
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- 0. CLEANUP: Eliminar permisos garbled de migraciones anteriores
--    (UTF-8 encoding issues: Â·, Ã¡, Ã³, etc.)
-- =========================================================
DELETE FROM dbo.CFG_RolePermissions
WHERE PermissionId IN (
  SELECT Id FROM dbo.CFG_Permissions
  WHERE Code IN ('carwash.board.view', 'carwash.board.work', 'carwash.board.manage',
                 'carwash.catalog.view', 'carwash.catalog.manage',
                 'carwash.washers.view', 'carwash.washers.manage',
                 'carwash.portal.view', 'carwash.portal.manage')
);
GO

DELETE FROM dbo.CFG_Permissions
WHERE Code IN ('carwash.board.view', 'carwash.board.work', 'carwash.board.manage',
               'carwash.catalog.view', 'carwash.catalog.manage',
               'carwash.washers.view', 'carwash.washers.manage',
               'carwash.portal.view', 'carwash.portal.manage');
GO

-- =========================================================
-- 1. Insertar nuevos permisos granulares (idempotente)
-- =========================================================
DECLARE @carwash NVARCHAR(30) = 'carwash';

INSERT INTO dbo.CFG_Permissions (Code, Module, Description, ModuleCode)
SELECT v.Code, v.Module, v.Description, @carwash
FROM (VALUES
    ('carwash.board.view',    'Carwash - Tablero',   'Ver el tablero y la cola de vehiculos'),
    ('carwash.board.work',    'Carwash - Tablero',   'Avanzar y retroceder el estado de los vehiculos'),
    ('carwash.board.manage',  'Carwash - Tablero',   'Registrar vehiculos, asignar lavadores y cancelar turnos'),
    ('carwash.catalog.view',  'Carwash - Catalogo',  'Ver servicios y agregados del catalogo'),
    ('carwash.catalog.manage','Carwash - Catalogo',  'Crear, editar y eliminar servicios y agregados'),
    ('carwash.washers.view',  'Carwash - Lavadores', 'Ver el directorio de lavadores'),
    ('carwash.washers.manage','Carwash - Lavadores', 'Agregar y editar lavadores, configurar propinas'),
    ('carwash.portal.view',   'Carwash - Portal',    'Ver los links de portal publico'),
    ('carwash.portal.manage', 'Carwash - Portal',    'Crear, activar y eliminar links de portal')
) AS v(Code, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CFG_Permissions p WHERE p.Code = v.Code
);
GO

-- =========================================================
-- 2. Migrar roles existentes: carwash.view → todos los *.view + board.view
-- =========================================================

-- Roles que tenían carwash.view reciben los nuevos permisos granulares
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT DISTINCT rp.RoleId, new_p.Id
FROM dbo.CFG_RolePermissions rp
INNER JOIN dbo.CFG_Permissions old_p ON old_p.Id = rp.PermissionId AND old_p.Code = 'carwash.view'
CROSS JOIN dbo.CFG_Permissions new_p
WHERE new_p.Code IN (
    'carwash.board.view',
    'carwash.catalog.view',
    'carwash.washers.view',
    'carwash.portal.view'
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.CFG_RolePermissions x
    WHERE x.RoleId = rp.RoleId AND x.PermissionId = new_p.Id
);
GO

-- Roles que tenían carwash.manage reciben los nuevos permisos de gestión
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT DISTINCT rp.RoleId, new_p.Id
FROM dbo.CFG_RolePermissions rp
INNER JOIN dbo.CFG_Permissions old_p ON old_p.Id = rp.PermissionId AND old_p.Code = 'carwash.manage'
CROSS JOIN dbo.CFG_Permissions new_p
WHERE new_p.Code IN (
    'carwash.board.manage',
    'carwash.catalog.manage',
    'carwash.washers.manage',
    'carwash.portal.manage'
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.CFG_RolePermissions x
    WHERE x.RoleId = rp.RoleId AND x.PermissionId = new_p.Id
);
GO

-- Roles que tenían carwash.work reciben carwash.board.work
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT DISTINCT rp.RoleId, new_p.Id
FROM dbo.CFG_RolePermissions rp
INNER JOIN dbo.CFG_Permissions old_p ON old_p.Id = rp.PermissionId AND old_p.Code = 'carwash.work'
CROSS JOIN dbo.CFG_Permissions new_p
WHERE new_p.Code = 'carwash.board.work'
AND NOT EXISTS (
    SELECT 1 FROM dbo.CFG_RolePermissions x
    WHERE x.RoleId = rp.RoleId AND x.PermissionId = new_p.Id
);
GO

-- =========================================================
-- 2.5. Actualizar descripciones con caracteres ASCII-safe para permisos mantenidos
-- =========================================================
UPDATE dbo.CFG_Permissions
SET Description = 'Procesar entregas de vehiculos y cobrar propinas en la caja de Carwash'
WHERE Code = 'carwash.caja';

UPDATE dbo.CFG_Permissions
SET Description = 'Ver metricas, facturacion y ranking de lavadores de Carwash'
WHERE Code = 'carwash.reports';
GO

-- =========================================================
-- 3. Eliminar permisos obsoletos (y sus asignaciones en roles)
-- =========================================================

DELETE rp
FROM dbo.CFG_RolePermissions rp
INNER JOIN dbo.CFG_Permissions p ON p.Id = rp.PermissionId
WHERE p.Code IN ('carwash.view', 'carwash.manage', 'carwash.work');
GO

DELETE FROM dbo.CFG_Permissions
WHERE Code IN ('carwash.view', 'carwash.manage', 'carwash.work');
GO
