/*
    Módulo: COMUNICACIONES (COM_)
    Tablas: COM_EmailMessages, COM_Reminders

    NOTA:
    El envío real de correo depende de un proveedor todavía no elegido
    (SendGrid, SMTP, Amazon SES, etc.). Mientras tanto, IEmailSender usa un
    NoOpEmailSender que no envía nada, pero COM_EmailMessages igual guarda el
    historial de intentos (Status = 'Failed', ErrorMessage explicando el motivo),
    para que la pantalla de Correos sea utilizable desde ya.
*/
USE Alkiman_Dev;
GO

IF OBJECT_ID(N'dbo.COM_Reminders', N'U') IS NOT NULL
    DROP TABLE dbo.COM_Reminders;
GO

IF OBJECT_ID(N'dbo.COM_EmailMessages', N'U') IS NOT NULL
    DROP TABLE dbo.COM_EmailMessages;
GO

-- =========================================================
-- Tabla: COM_EmailMessages
-- Historial de correos individuales y masivos enviados a clientes.
-- =========================================================
CREATE TABLE dbo.COM_EmailMessages
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_COM_EmailMessages_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    CustomerId      UNIQUEIDENTIFIER   NULL,
    Type            NVARCHAR(20)       NOT NULL,
    RecipientName   NVARCHAR(150)      NOT NULL,
    RecipientEmail  NVARCHAR(255)      NOT NULL,
    Subject         NVARCHAR(200)      NOT NULL,
    Body            NVARCHAR(MAX)      NOT NULL,
    Status          NVARCHAR(20)       NOT NULL,
    ErrorMessage    NVARCHAR(500)      NULL,
    SentAt          DATETIME2          NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_COM_EmailMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_COM_EmailMessages PRIMARY KEY (Id),
    CONSTRAINT FK_COM_EmailMessages_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_COM_EmailMessages_CRM_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT CK_COM_EmailMessages_Type CHECK (Type IN ('Individual', 'Mass')),
    CONSTRAINT CK_COM_EmailMessages_Status CHECK (Status IN ('Sent', 'Failed'))
);
GO

CREATE INDEX IX_COM_EmailMessages_LandlordId ON dbo.COM_EmailMessages (LandlordId);
CREATE INDEX IX_COM_EmailMessages_CustomerId ON dbo.COM_EmailMessages (CustomerId);
GO

-- =========================================================
-- Tabla: COM_Reminders
-- Recordatorios manuales de seguimiento (cliente y/o renta asociados).
-- =========================================================
CREATE TABLE dbo.COM_Reminders
(
    Id              UNIQUEIDENTIFIER   NOT NULL CONSTRAINT DF_COM_Reminders_Id DEFAULT NEWID(),
    LandlordId      UNIQUEIDENTIFIER   NOT NULL,
    CustomerId      UNIQUEIDENTIFIER   NULL,
    RentalId        UNIQUEIDENTIFIER   NULL,
    Title           NVARCHAR(150)      NOT NULL,
    Message         NVARCHAR(1000)     NULL,
    RemindAt        DATETIME2          NOT NULL,
    Status          NVARCHAR(20)       NOT NULL CONSTRAINT DF_COM_Reminders_Status DEFAULT ('Pending'),
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_COM_Reminders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(255)      NOT NULL,
    UpdatedAt       DATETIME2          NULL,
    UpdatedBy       NVARCHAR(255)      NULL,
    CONSTRAINT PK_COM_Reminders PRIMARY KEY (Id),
    CONSTRAINT FK_COM_Reminders_CFG_Landlords FOREIGN KEY (LandlordId)
        REFERENCES dbo.CFG_Landlords (Id),
    CONSTRAINT FK_COM_Reminders_CRM_Customers FOREIGN KEY (CustomerId)
        REFERENCES dbo.CRM_Customers (Id),
    CONSTRAINT FK_COM_Reminders_TRX_Rentals FOREIGN KEY (RentalId)
        REFERENCES dbo.TRX_Rentals (Id),
    CONSTRAINT CK_COM_Reminders_Status CHECK (Status IN ('Pending', 'Completed', 'Cancelled'))
);
GO

CREATE INDEX IX_COM_Reminders_LandlordId ON dbo.COM_Reminders (LandlordId);
CREATE INDEX IX_COM_Reminders_CustomerId ON dbo.COM_Reminders (CustomerId);
CREATE INDEX IX_COM_Reminders_RentalId ON dbo.COM_Reminders (RentalId);
GO
