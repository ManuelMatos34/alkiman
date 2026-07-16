/*
    Módulo: TRANSACCIONES (TRX_)
    Tablas: TRX_Rentals, TRX_Payments
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Tabla: TRX_Rentals
-- Une a un cliente con un activo durante un lapso pactado.
-- =========================================================
IF OBJECT_ID(N'dbo.TRX_Rentals', N'U') IS NOT NULL
    DROP TABLE dbo.TRX_Rentals;
GO

CREATE TABLE dbo.TRX_Rentals
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_TRX_Rentals_Id DEFAULT NEWID(),
    AssetId         UNIQUEIDENTIFIER   NOT NULL,
    CustomerId      UNIQUEIDENTIFIER   NOT NULL,
    StartDate       DATETIME2          NOT NULL,
    EndDate         DATETIME2          NOT NULL,
    ContractPdfUrl  NVARCHAR(500)      NULL,
    TotalPrice      DECIMAL(18,2)      NOT NULL,
    Status          NVARCHAR(20)       NOT NULL CONSTRAINT DF_TRX_Rentals_Status DEFAULT ('Active'),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_TRX_Rentals_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_TRX_Rentals PRIMARY KEY (Id),
    CONSTRAINT FK_TRX_Rentals_INV_Assets FOREIGN KEY (AssetId)
        REFERENCES dbo.INV_Assets (Id),
    CONSTRAINT FK_TRX_Rentals_CRM_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT CK_TRX_Rentals_Dates CHECK (EndDate > StartDate),
    CONSTRAINT CK_TRX_Rentals_TotalPrice CHECK (TotalPrice >= 0),
    CONSTRAINT CK_TRX_Rentals_Status CHECK (Status IN ('Active', 'Completed', 'Overdue'))
);
GO

CREATE INDEX IX_TRX_Rentals_AssetId ON dbo.TRX_Rentals (AssetId);
CREATE INDEX IX_TRX_Rentals_CustomerId ON dbo.TRX_Rentals (CustomerId);
GO

-- =========================================================
-- Tabla: TRX_Payments
-- Libro diario de ingresos (rentas) y egresos (gastos).
-- =========================================================
IF OBJECT_ID(N'dbo.TRX_Payments', N'U') IS NOT NULL
    DROP TABLE dbo.TRX_Payments;
GO

CREATE TABLE dbo.TRX_Payments
(
    Id                      UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_TRX_Payments_Id DEFAULT NEWID(),
    RentalId                UNIQUEIDENTIFIER   NULL,
    LandlordId              UNIQUEIDENTIFIER   NOT NULL,
    Amount                  DECIMAL(18,2)      NOT NULL,
    Type                    NVARCHAR(10)       NOT NULL,
    PaymentDate             DATETIME2          NOT NULL,
    StripeTransactionId     NVARCHAR(255)      NULL,
    CreatedAt               DATETIME2          NOT NULL CONSTRAINT DF_TRX_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy               NVARCHAR(255)      NOT NULL,
    UpdatedAt               DATETIME2          NULL,
    UpdatedBy               NVARCHAR(255)      NULL,
    CONSTRAINT PK_TRX_Payments PRIMARY KEY (Id),
    CONSTRAINT FK_TRX_Payments_TRX_Rentals FOREIGN KEY (RentalId)
        REFERENCES dbo.TRX_Rentals (Id),
    CONSTRAINT FK_TRX_Payments_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT CK_TRX_Payments_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_TRX_Payments_Type CHECK (Type IN ('Income', 'Expense'))
);
GO

CREATE INDEX IX_TRX_Payments_RentalId ON dbo.TRX_Payments (RentalId);
CREATE INDEX IX_TRX_Payments_LandlordId ON dbo.TRX_Payments (LandlordId);
GO
