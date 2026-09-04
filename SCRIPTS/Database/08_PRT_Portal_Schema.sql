/*
    Módulo: PORTAL DE RENTAS (PRT_)
    Tablas: PRT_PortalLinks, PRT_PortalRentals

    QUÉ ES:
    El "portal de rentas" es un link público (sin login) que el negocio genera a
    partir de un Grupo de Activos ya existente (INV_AssetGroups). Quien reciba el
    link ve solo los activos de ese grupo, con foto/descripción/precio, y puede
    completar una pasarela de 3 pasos (datos personales -> detalle de la renta ->
    tarjeta) para auto-rentar un activo. Si el cliente no existe, se crea al vuelo
    (igual que en la importación masiva de rentas).

    A DIFERENCIA de los demás módulos, este es accedido por endpoints PÚBLICOS
    (sin [Authorize]): cualquiera con el link puede leer el catálogo y hacer un
    checkout. El aislamiento por negocio se resuelve por el Slug del link, no por
    JWT.

    NOTA IMPORTANTE DE SEGURIDAD (tarjeta):
    Todavía no se eligió un proveedor de pagos (Stripe u otro). Mientras tanto,
    PRT_PortalRentals NUNCA guarda el número completo de la tarjeta ni el CVV:
    solo se persisten CardBrand (inferida) y CardLast4 (últimos 4 dígitos), a
    modo de comprobante/mock. Antes de cobrar de verdad hay que reemplazar esto
    por tokenización real de un proveedor (PCI-DSS).

    NOTA SOBRE CRM_Customers:
    Un cliente creado desde el portal puede no tener cédula/RNC a mano (no se
    pide en la pasarela), así que IdentityNumber pasa a ser opcional, y se
    agregan Address/Country (recolectados en el paso 1 de la pasarela). El alta
    manual de clientes desde el panel administrativo sigue pidiendo la cédula
    como obligatoria (eso se valida en la capa de aplicación, no en la BD).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- CRM_Customers: ajustes para admitir clientes creados desde el portal público
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CRM_Customers') AND name = 'Address')
BEGIN
    ALTER TABLE dbo.CRM_Customers ADD Address NVARCHAR(255) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CRM_Customers') AND name = 'Country')
BEGIN
    ALTER TABLE dbo.CRM_Customers ADD Country NVARCHAR(100) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CRM_Customers') AND name = 'IdentityNumber' AND is_nullable = 0
)
BEGIN
    -- El portal público no pide cédula/RNC; se vuelve opcional a nivel de columna.
    -- El alta manual desde el panel sigue exigiéndola vía validación de la app.
    ALTER TABLE dbo.CRM_Customers ALTER COLUMN IdentityNumber NVARCHAR(50) NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_CRM_Customers_Landlord_Identity' AND parent_object_id = OBJECT_ID(N'dbo.CRM_Customers')
)
BEGIN
    -- Un UNIQUE CONSTRAINT normal en SQL Server solo admite UN NULL por columna
    -- involucrada; con IdentityNumber ahora opcional necesitamos que NULL no
    -- cuente para la unicidad. Se reemplaza por un índice único filtrado.
    ALTER TABLE dbo.CRM_Customers DROP CONSTRAINT UQ_CRM_Customers_Landlord_Identity;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_CRM_Customers_Landlord_Identity' AND object_id = OBJECT_ID(N'dbo.CRM_Customers')
)
BEGIN
    CREATE UNIQUE INDEX UQ_CRM_Customers_Landlord_Identity
        ON dbo.CRM_Customers (LandlordId, IdentityNumber)
        WHERE IdentityNumber IS NOT NULL;
END
GO

-- =========================================================
-- CFG_Permissions: nuevos permisos del módulo Portal de Rentas
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'portal.view')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('portal.view', 'Portal de Rentas', 'Ver links del portal de rentas');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'portal.manage')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('portal.manage', 'Portal de Rentas', 'Generar y administrar links del portal de rentas');
END
GO

-- Retrocompatibilidad: el rol de sistema "Administrador" de cada negocio ya
-- existente debe recibir los permisos nuevos automáticamente (los negocios que
-- se registren de acá en más ya los reciben vía AuthService al alta).
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.CFG_Roles r
CROSS JOIN dbo.CFG_Permissions p
WHERE r.IsSystem = 1
  AND p.Code IN ('portal.view', 'portal.manage')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- =========================================================
-- Drops (en orden inverso de dependencia)
-- =========================================================
IF OBJECT_ID(N'dbo.PRT_PortalRentals', N'U') IS NOT NULL
    DROP TABLE dbo.PRT_PortalRentals;
GO

IF OBJECT_ID(N'dbo.PRT_PortalLinks', N'U') IS NOT NULL
    DROP TABLE dbo.PRT_PortalLinks;
GO

-- =========================================================
-- Tabla: PRT_PortalLinks
-- Un link público generado por el negocio a partir de un Grupo de Activos.
-- =========================================================
CREATE TABLE dbo.PRT_PortalLinks
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_PRT_PortalLinks_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    AssetGroupId    INT                NOT NULL,
    Title           NVARCHAR(150)      NOT NULL,
    Slug            NVARCHAR(40)       NOT NULL,
    IsActive        BIT                NOT NULL CONSTRAINT DF_PRT_PortalLinks_IsActive DEFAULT (1),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_PRT_PortalLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_PRT_PortalLinks PRIMARY KEY (Id),
    CONSTRAINT FK_PRT_PortalLinks_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_PRT_PortalLinks_INV_AssetGroups FOREIGN KEY (AssetGroupId)
        REFERENCES dbo.INV_AssetGroups (Id),
    CONSTRAINT UQ_PRT_PortalLinks_Slug UNIQUE (Slug)
);
GO

CREATE INDEX IX_PRT_PortalLinks_LandlordId ON dbo.PRT_PortalLinks (LandlordId);
CREATE INDEX IX_PRT_PortalLinks_AssetGroupId ON dbo.PRT_PortalLinks (AssetGroupId);
GO

-- =========================================================
-- Tabla: PRT_PortalRentals
-- Comprobante de cada checkout completado desde el portal: qué link se usó,
-- qué renta/cliente generó y un mock de los datos de la tarjeta (solo
-- marca + últimos 4 dígitos; NUNCA el número completo ni el CVV).
-- =========================================================
CREATE TABLE dbo.PRT_PortalRentals
(
    Id                  UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_PRT_PortalRentals_Id DEFAULT NEWID(),
    PortalLinkId        UNIQUEIDENTIFIER   NOT NULL,
    RentalId            UNIQUEIDENTIFIER   NOT NULL,
    CustomerId          UNIQUEIDENTIFIER   NOT NULL,
    Quantity            INT                NOT NULL CONSTRAINT DF_PRT_PortalRentals_Quantity DEFAULT (1),
    CardholderName      NVARCHAR(150)      NOT NULL,
    CardBrand           NVARCHAR(20)       NOT NULL,
    CardLast4           CHAR(4)            NOT NULL,
    CardExpiry          CHAR(7)            NOT NULL,
    CreatedAt           DATETIME2          NOT NULL CONSTRAINT DF_PRT_PortalRentals_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PRT_PortalRentals PRIMARY KEY (Id),
    CONSTRAINT FK_PRT_PortalRentals_PRT_PortalLinks FOREIGN KEY (PortalLinkId)
        REFERENCES dbo.PRT_PortalLinks (Id),
    CONSTRAINT FK_PRT_PortalRentals_TRX_Rentals FOREIGN KEY (RentalId)
        REFERENCES dbo.TRX_Rentals (Id),
    CONSTRAINT FK_PRT_PortalRentals_CRM_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT CK_PRT_PortalRentals_Quantity CHECK (Quantity > 0)
);
GO

CREATE INDEX IX_PRT_PortalRentals_PortalLinkId ON dbo.PRT_PortalRentals (PortalLinkId);
CREATE INDEX IX_PRT_PortalRentals_RentalId ON dbo.PRT_PortalRentals (RentalId);
GO
