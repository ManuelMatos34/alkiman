/*
    Módulo: CONTRATOS (dentro de COM_, junto a EmailMessages/Reminders)
    Tablas: COM_ContractTemplates, COM_Contracts

    QUÉ ES:
    COM_ContractTemplates guarda las plantillas de contrato de un negocio,
    asociadas a una CFG_Categories. Puede haber varias plantillas por
    categoría (historial de versiones), pero solo una activa a la vez por
    categoría (IsActive = 1); esa es la que se usa para generar el contrato
    cuando se crea una renta de un activo de esa categoría.

    COM_Contracts es el contrato ya generado (uno por renta, automático):
    guarda una foto del contenido usado (ContentSnapshot) y el PDF ya
    renderizado (PdfContent, todavía sin blob storage configurado, se
    persiste directo en la fila) para que ediciones futuras de la plantilla
    no alteren contratos ya emitidos. También guarda la firma del cliente
    (SignatureImageBase64) cuando corresponde.

    FLUJO DE FIRMA:
    - Portal (checkout público): la firma es obligatoria antes de confirmar
      la renta -> el contrato nace con Status = 'Signed'.
    - Alta manual desde el panel: el contrato nace con Status = 'Pending' y
      se firma después vía POST /api/contracts/{id}/sign.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- CFG_Permissions: nuevos permisos del módulo Contratos
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'contracts.view')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('contracts.view', 'Contratos', 'Ver contratos y plantillas de contrato');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'contracts.manage')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('contracts.manage', 'Contratos', 'Administrar plantillas, firmar y gestionar contratos');
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
  AND p.Code IN ('contracts.view', 'contracts.manage')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- =========================================================
-- Drops (en orden inverso de dependencia)
-- =========================================================
IF OBJECT_ID(N'dbo.COM_Contracts', N'U') IS NOT NULL
    DROP TABLE dbo.COM_Contracts;
GO

IF OBJECT_ID(N'dbo.COM_ContractTemplates', N'U') IS NOT NULL
    DROP TABLE dbo.COM_ContractTemplates;
GO

-- =========================================================
-- Tabla: COM_ContractTemplates
-- Plantillas de contrato por categoría; solo una activa a la vez por
-- categoría (regla de negocio validada en ContractTemplateService, no acá).
-- =========================================================
CREATE TABLE dbo.COM_ContractTemplates
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    CategoryId      INT                NOT NULL,
    Name            NVARCHAR(150)      NOT NULL,
    Content         NVARCHAR(MAX)      NOT NULL,
    IsActive        BIT                NOT NULL CONSTRAINT DF_COM_ContractTemplates_IsActive DEFAULT (0),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_COM_ContractTemplates_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_COM_ContractTemplates PRIMARY KEY (Id),
    CONSTRAINT FK_COM_ContractTemplates_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_COM_ContractTemplates_CFG_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.CFG_Categories (Id)
);
GO

CREATE INDEX IX_COM_ContractTemplates_LandlordId ON dbo.COM_ContractTemplates (LandlordId);
CREATE INDEX IX_COM_ContractTemplates_CategoryId ON dbo.COM_ContractTemplates (CategoryId);
GO

-- Solo puede haber UNA plantilla activa por categoría: índice único filtrado
-- (permite múltiples IsActive = 0, pero como máximo un IsActive = 1 por categoría).
CREATE UNIQUE INDEX UQ_COM_ContractTemplates_Category_Active
    ON dbo.COM_ContractTemplates (CategoryId)
    WHERE IsActive = 1;
GO

-- =========================================================
-- Tabla: COM_Contracts
-- Contrato generado automáticamente al crear una renta (manual o Portal).
-- =========================================================
CREATE TABLE dbo.COM_Contracts
(
    Id                      UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_COM_Contracts_Id DEFAULT NEWID(),
    LandlordId              UNIQUEIDENTIFIER   NOT NULL,
    RentalId                UNIQUEIDENTIFIER   NOT NULL,
    CustomerId              UNIQUEIDENTIFIER   NOT NULL,
    ContractTemplateId      INT                NULL,
    ContentSnapshot         NVARCHAR(MAX)      NOT NULL,
    PdfContent              VARBINARY(MAX)     NOT NULL,
    SignatureImageBase64    NVARCHAR(MAX)      NULL,
    Status                  NVARCHAR(20)       NOT NULL,
    SignedAt                DATETIME2          NULL,
    EmailSent               BIT                NOT NULL CONSTRAINT DF_COM_Contracts_EmailSent DEFAULT (0),
    CreatedAt               DATETIME2          NOT NULL CONSTRAINT DF_COM_Contracts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy               NVARCHAR(255)      NOT NULL,
    UpdatedAt               DATETIME2          NULL,
    UpdatedBy               NVARCHAR(255)      NULL,
    CONSTRAINT PK_COM_Contracts PRIMARY KEY (Id),
    CONSTRAINT FK_COM_Contracts_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_COM_Contracts_TRX_Rentals FOREIGN KEY (RentalId)
        REFERENCES dbo.TRX_Rentals (Id),
    CONSTRAINT FK_COM_Contracts_CRM_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT FK_COM_Contracts_COM_ContractTemplates FOREIGN KEY (ContractTemplateId)
        REFERENCES dbo.COM_ContractTemplates (Id),
    CONSTRAINT CK_COM_Contracts_Status CHECK (Status IN ('Pending', 'Signed'))
);
GO

CREATE INDEX IX_COM_Contracts_LandlordId ON dbo.COM_Contracts (LandlordId);
CREATE INDEX IX_COM_Contracts_RentalId ON dbo.COM_Contracts (RentalId);
CREATE INDEX IX_COM_Contracts_CustomerId ON dbo.COM_Contracts (CustomerId);
CREATE INDEX IX_COM_Contracts_ContractTemplateId ON dbo.COM_Contracts (ContractTemplateId);
GO
