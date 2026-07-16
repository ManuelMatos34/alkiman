/*
    Módulo: AUDITORÍA (AUD_)
    Tablas: AUD_AuditLogs
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- Tabla: AUD_AuditLogs
-- Bitácora global de inserciones, mutaciones y eliminaciones.
-- =========================================================
IF OBJECT_ID(N'dbo.AUD_AuditLogs', N'U') IS NOT NULL
    DROP TABLE dbo.AUD_AuditLogs;
GO

CREATE TABLE dbo.AUD_AuditLogs
(
    Id              BIGINT IDENTITY(1,1)   NOT NULL,
    UserId          NVARCHAR(255)          NOT NULL,
    Type            NVARCHAR(50)           NOT NULL,
    TableName       NVARCHAR(100)          NOT NULL,
    PrimaryKey      NVARCHAR(255)          NOT NULL,
    OldValues       NVARCHAR(MAX)          NULL,
    NewValues       NVARCHAR(MAX)          NULL,
    Date            DATETIME2              NOT NULL CONSTRAINT DF_AUD_AuditLogs_Date DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AUD_AuditLogs PRIMARY KEY (Id),
    CONSTRAINT CK_AUD_AuditLogs_Type CHECK (Type IN ('Create', 'Update', 'Delete'))
);
GO

CREATE INDEX IX_AUD_AuditLogs_Table_PrimaryKey ON dbo.AUD_AuditLogs (TableName, PrimaryKey);
CREATE INDEX IX_AUD_AuditLogs_UserId ON dbo.AUD_AuditLogs (UserId);
GO
