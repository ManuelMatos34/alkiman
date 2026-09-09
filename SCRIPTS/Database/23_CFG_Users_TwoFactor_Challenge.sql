/*
    23_CFG_Users_TwoFactor_Challenge.sql

    POR QUÉ
    -------
    CFG_Users.TwoFactorEnabled ya existía (script 01) y la pantalla de Seguridad ya
    dejaba prenderlo, pero era un interruptor que no hacía nada: el login emitía el
    JWT igual, sin pedir nada más. Este script agrega lo que faltaba para que el
    segundo factor exista de verdad: el estado del desafío pendiente.

    El desafío vive en columnas de CFG_Users y no en una tabla aparte porque un
    usuario sólo puede tener UN desafío abierto a la vez: pedir un código nuevo
    invalida el anterior. Una tabla hija permitiría varios vivos en paralelo, que es
    justo lo que no queremos (multiplicaría los intentos disponibles para adivinar).

    Columnas:
      - TwoFactorChallengeToken: identificador opaco que el frontend recibe al pasar
        la contraseña y devuelve junto con el código. Es lo que evita tener que
        reenviar la contraseña en el segundo paso. Se guarda en claro, igual que
        ResetToken: son 32 bytes aleatorios y viven minutos.
      - TwoFactorCodeHash: el código de 6 dígitos NUNCA se guarda en claro. Va con el
        mismo PBKDF2 que las contraseñas (IPasswordHasher), así que un volcado de la
        tabla no entrega códigos usables.
      - TwoFactorCodeExpiresAt: vencimiento del código.
      - TwoFactorAttempts: intentos fallidos del desafío actual. Al pasarse del tope
        el desafío se borra y hay que volver a loguearse. Sin esto, 6 dígitos son
        un millón de combinaciones que un script prueba en minutos.

    Idempotente: se puede correr varias veces sin efecto adicional.
*/

SET NOCOUNT ON;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = N'TwoFactorChallengeToken'
)
BEGIN
    ALTER TABLE dbo.CFG_Users ADD TwoFactorChallengeToken NVARCHAR(100) NULL;
    PRINT 'CFG_Users.TwoFactorChallengeToken agregada.';
END
ELSE
    PRINT 'CFG_Users.TwoFactorChallengeToken ya existia.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = N'TwoFactorCodeHash'
)
BEGIN
    ALTER TABLE dbo.CFG_Users ADD TwoFactorCodeHash NVARCHAR(400) NULL;
    PRINT 'CFG_Users.TwoFactorCodeHash agregada.';
END
ELSE
    PRINT 'CFG_Users.TwoFactorCodeHash ya existia.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = N'TwoFactorCodeExpiresAt'
)
BEGIN
    ALTER TABLE dbo.CFG_Users ADD TwoFactorCodeExpiresAt DATETIME2(7) NULL;
    PRINT 'CFG_Users.TwoFactorCodeExpiresAt agregada.';
END
ELSE
    PRINT 'CFG_Users.TwoFactorCodeExpiresAt ya existia.';
GO

/*
    WITH VALUES es necesario: sin él, las filas que ya existen quedan en NULL pese al
    DEFAULT y la columna es NOT NULL.
*/
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = N'TwoFactorAttempts'
)
BEGIN
    ALTER TABLE dbo.CFG_Users
        ADD TwoFactorAttempts INT NOT NULL
            CONSTRAINT DF_CFG_Users_TwoFactorAttempts DEFAULT 0 WITH VALUES;
    PRINT 'CFG_Users.TwoFactorAttempts agregada.';
END
ELSE
    PRINT 'CFG_Users.TwoFactorAttempts ya existia.';
GO

/*
    El login por token de desafío busca por esta columna. Filtrado porque la enorme
    mayoría de las filas la tiene en NULL (nadie tiene un desafío abierto casi nunca).
    UNIQUE para que dos usuarios no puedan compartir token ni por accidente.
*/
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CFG_Users') AND name = N'UX_CFG_Users_TwoFactorChallengeToken'
)
BEGIN
    CREATE UNIQUE INDEX UX_CFG_Users_TwoFactorChallengeToken
        ON dbo.CFG_Users (TwoFactorChallengeToken)
        WHERE TwoFactorChallengeToken IS NOT NULL;
    PRINT 'UX_CFG_Users_TwoFactorChallengeToken creado.';
END
ELSE
    PRINT 'UX_CFG_Users_TwoFactorChallengeToken ya existia.';
GO
