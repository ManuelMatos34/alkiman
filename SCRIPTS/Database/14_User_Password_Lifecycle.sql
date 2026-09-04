/*
    Módulo: CICLO DE VIDA DE CONTRASEÑA (Usuarios)
    Tablas afectadas: CFG_Users

    QUÉ ES:
    Soporta dos flujos de seguridad sobre la contraseña de un usuario interno:
    1) Alta con contraseña generada por el sistema: al crear un usuario desde
       Ajustes > Usuarios, el admin ya no define la contraseña -- se genera una
       aleatoria en el backend y se le exige cambiarla en su primer inicio de
       sesión (MustChangePassword). Las cuentas existentes no se ven afectadas
       (default 0) y el dueño creado en el registro tampoco queda forzado.
    2) "Olvidé mi contraseña": el usuario puede pedir un link de recuperación
       por email; se persiste un token de un solo uso con vencimiento
       (ResetToken / ResetTokenExpiresAt) para poder validarlo sin depender de
       una sesión iniciada.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- CFG_Users: ciclo de vida de contraseña
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = 'MustChangePassword')
BEGIN
    ALTER TABLE dbo.CFG_Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_CFG_Users_MustChangePassword DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = 'ResetToken')
BEGIN
    ALTER TABLE dbo.CFG_Users ADD ResetToken NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = 'ResetTokenExpiresAt')
BEGIN
    ALTER TABLE dbo.CFG_Users ADD ResetTokenExpiresAt DATETIME2 NULL;
END
GO
