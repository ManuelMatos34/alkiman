/*
    Módulo: PEDIDOS DE RENTA (dentro de TRX_, junto a TRX_Rentals/TRX_Payments)
    Tabla: TRX_RentalRequests

    QUÉ ES:
    Permite que un cliente, sin necesidad de loguearse, entre a un link público
    propio de su renta (/mi-renta/{AccessToken}) y pida una prórroga (solo para
    rentas de largo plazo: Monthly/Annual) o una cancelación anticipada. El
    negocio revisa y aprueba/rechaza esos pedidos desde el panel autenticado
    existente. Ningún cobro se dispara automáticamente: los pagos siguen siendo
    100% manuales, igual que en el resto de la aplicación.

    ACCESO PÚBLICO A LA RENTA:
    TRX_Rentals.AccessToken es el identificador opaco del link público de cada
    renta. AccessFailedAttempts/AccessLockedUntil implementan un lockout simple
    (5 intentos fallidos de verificación de identidad -> bloqueo de 15 minutos)
    para dificultar que alguien intente adivinar la identidad del cliente a
    fuerza bruta a partir de un AccessToken filtrado/compartido.

    TRX_Rentals.Status suma el valor 'Cancelled' (renta cancelada por un pedido
    de cancelación aprobado).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- CFG_Permissions: nuevos permisos del módulo Pedidos de Renta
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'rentalrequests.view')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('rentalrequests.view', 'Pedidos de Renta', 'Ver pedidos de prórroga y cancelación de rentas');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions WHERE Code = 'rentalrequests.manage')
BEGIN
    INSERT INTO dbo.CFG_Permissions (Code, Module, Description)
    VALUES ('rentalrequests.manage', 'Pedidos de Renta', 'Aprobar o rechazar pedidos de prórroga y cancelación');
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
  AND p.Code IN ('rentalrequests.view', 'rentalrequests.manage')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- =========================================================
-- TRX_Rentals: columnas nuevas para el link público "mi-renta"
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TRX_Rentals') AND name = 'AccessToken')
BEGIN
    ALTER TABLE dbo.TRX_Rentals ADD AccessToken UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TRX_Rentals_AccessToken DEFAULT NEWID();
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_TRX_Rentals_AccessToken' AND object_id = OBJECT_ID(N'dbo.TRX_Rentals'))
BEGIN
    CREATE UNIQUE INDEX UQ_TRX_Rentals_AccessToken ON dbo.TRX_Rentals (AccessToken);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TRX_Rentals') AND name = 'AccessFailedAttempts')
BEGIN
    ALTER TABLE dbo.TRX_Rentals ADD AccessFailedAttempts INT NOT NULL CONSTRAINT DF_TRX_Rentals_AccessFailedAttempts DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TRX_Rentals') AND name = 'AccessLockedUntil')
BEGIN
    ALTER TABLE dbo.TRX_Rentals ADD AccessLockedUntil DATETIME2 NULL;
END
GO

-- =========================================================
-- TRX_Rentals.Status: sumar 'Cancelled' al CHECK existente
-- =========================================================
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_TRX_Rentals_Status' AND parent_object_id = OBJECT_ID(N'dbo.TRX_Rentals')
)
BEGIN
    ALTER TABLE dbo.TRX_Rentals DROP CONSTRAINT CK_TRX_Rentals_Status;
END
GO

ALTER TABLE dbo.TRX_Rentals
    ADD CONSTRAINT CK_TRX_Rentals_Status CHECK (Status IN ('Active', 'Completed', 'Overdue', 'Cancelled'));
GO

-- =========================================================
-- Drops (tabla nueva, sin riesgo de pérdida de datos)
-- =========================================================
IF OBJECT_ID(N'dbo.TRX_RentalRequests', N'U') IS NOT NULL
    DROP TABLE dbo.TRX_RentalRequests;
GO

-- =========================================================
-- Tabla: TRX_RentalRequests
-- Un pedido de prórroga o cancelación hecho por el cliente desde su link
-- público. Solo puede haber un pedido "Pending" abierto por renta a la vez
-- (ver índice único filtrado más abajo).
-- =========================================================
CREATE TABLE dbo.TRX_RentalRequests
(
    Id                  UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_TRX_RentalRequests_Id DEFAULT NEWID(),
    LandlordId          UNIQUEIDENTIFIER   NOT NULL,
    RentalId            UNIQUEIDENTIFIER   NOT NULL,
    Type                NVARCHAR(20)       NOT NULL,
    Status              NVARCHAR(20)       NOT NULL CONSTRAINT DF_TRX_RentalRequests_Status DEFAULT ('Pending'),
    RequestedPeriods    INT                NULL,
    ProposedEndDate     DATETIME2          NULL,
    Reason              NVARCHAR(500)      NULL,
    StaffNote           NVARCHAR(500)      NULL,
    ReviewedAt          DATETIME2          NULL,
    ReviewedBy          NVARCHAR(255)      NULL,
    CreatedAt           DATETIME2          NOT NULL CONSTRAINT DF_TRX_RentalRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy           NVARCHAR(255)      NOT NULL,
    UpdatedAt           DATETIME2          NULL,
    UpdatedBy           NVARCHAR(255)      NULL,
    CONSTRAINT PK_TRX_RentalRequests PRIMARY KEY (Id),
    CONSTRAINT FK_TRX_RentalRequests_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_TRX_RentalRequests_TRX_Rentals FOREIGN KEY (RentalId)
        REFERENCES dbo.TRX_Rentals (Id),
    CONSTRAINT CK_TRX_RentalRequests_Type CHECK (Type IN ('Extension', 'Cancellation')),
    CONSTRAINT CK_TRX_RentalRequests_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
);
GO

CREATE INDEX IX_TRX_RentalRequests_LandlordId ON dbo.TRX_RentalRequests (LandlordId);
CREATE INDEX IX_TRX_RentalRequests_RentalId ON dbo.TRX_RentalRequests (RentalId);
GO

-- Solo puede haber UN pedido pendiente por renta: índice único filtrado
-- (mismo criterio que UQ_COM_ContractTemplates_Category_Active), red de
-- seguridad a nivel de base de datos además de la validación en el servicio.
CREATE UNIQUE INDEX UQ_TRX_RentalRequests_Rental_Pending
    ON dbo.TRX_RentalRequests (RentalId)
    WHERE Status = 'Pending';
GO
