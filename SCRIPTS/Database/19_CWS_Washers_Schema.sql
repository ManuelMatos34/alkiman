/*
    Módulo: CARWASH -- directorio propio de lavadores
    Tablas nuevas:    CWS_Washers
    Tablas alteradas: CWS_Tickets (AssignedToUserId -> AssignedToWasherId)

    POR QUÉ:
    Hasta acá un lavador ERA un usuario del sistema: la lista de lavadores se
    calculaba como "usuarios activos cuyo rol tiene carwash.work". Eso mezcla dos
    cosas distintas:

      - un USUARIO es una identidad: inicia sesión, tiene rol y permisos, ocupa
        un lugar en la administración del negocio;
      - un LAVADOR es un registro operativo: a quién se le asigna un vehículo y
        a nombre de quién queda el historial.

    Mezclarlas obligaba a crear una cuenta (con email y contraseña) para cada
    persona que lava un auto —personal que rota todo el tiempo—, metía al
    Administrador en el desplegable de lavadores porque su rol también tiene
    carwash.work, y hacía que dar de baja a un usuario borrara el nombre del
    lavador en tickets ya cerrados.

    Este script separa las dos cosas. El lavador pasa a ser una entidad DEL
    MÓDULO, igual que CRM_Customers lo es para Alquileres.

    EL VÍNCULO SIGUE SIENDO POSIBLE, PERO OPCIONAL:
    CWS_Washers.UserId es NULL por defecto. La mayoría de los lavadores no
    necesitan cuenta. Si el negocio quiere que uno entre al sistema y mueva su
    propia cola (el rol "Lavador" de siempre), se le vincula una cuenta y listo.
    Es un superconjunto de lo que había: no se pierde ninguna capacidad.

    IDEMPOTENTE: se puede correr varias veces (mismo criterio que 13, 14 y 17).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- 1. CWS_Washers
-- Directorio de lavadores del negocio. Un registro acá NO implica una cuenta:
-- UserId sólo se completa si además se le da acceso al sistema.
-- =========================================================
IF OBJECT_ID(N'dbo.CWS_Washers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CWS_Washers
    (
        Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CWS_Washers_Id DEFAULT NEWID(),
        LandlordId  UNIQUEIDENTIFIER NOT NULL,
        FullName    NVARCHAR(150)    NOT NULL,
        Phone       NVARCHAR(30)     NULL,
        -- Baja lógica: un lavador que se fue no puede recibir trabajo nuevo, pero
        -- su nombre tiene que seguir apareciendo en los tickets que ya lavó.
        IsActive    BIT              NOT NULL CONSTRAINT DF_CWS_Washers_IsActive DEFAULT (1),
        -- Vínculo OPCIONAL con una cuenta del sistema. NULL = el lavador no entra
        -- al software, sólo se le asigna trabajo desde el tablero.
        UserId      UNIQUEIDENTIFIER NULL,
        CreatedAt   DATETIME2        NOT NULL CONSTRAINT DF_CWS_Washers_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy   NVARCHAR(255)    NOT NULL,
        UpdatedAt   DATETIME2        NULL,
        UpdatedBy   NVARCHAR(255)    NULL,
        CONSTRAINT PK_CWS_Washers PRIMARY KEY (Id),
        CONSTRAINT FK_CWS_Washers_CFG_Landlords FOREIGN KEY (LandlordId) REFERENCES dbo.CFG_Landlords (Id),
        CONSTRAINT FK_CWS_Washers_CFG_Users FOREIGN KEY (UserId) REFERENCES dbo.CFG_Users (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CWS_Washers_LandlordId' AND object_id = OBJECT_ID(N'dbo.CWS_Washers'))
BEGIN
    CREATE INDEX IX_CWS_Washers_LandlordId ON dbo.CWS_Washers (LandlordId);
END
GO

-- Una cuenta no puede estar vinculada a dos lavadores: si no, "los tickets de
-- este usuario" deja de tener una respuesta única. Filtrado porque UserId es
-- NULL en la mayoría de las filas y varios NULL sí conviven.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_CWS_Washers_UserId' AND object_id = OBJECT_ID(N'dbo.CWS_Washers'))
BEGIN
    CREATE UNIQUE INDEX UQ_CWS_Washers_UserId ON dbo.CWS_Washers (UserId) WHERE UserId IS NOT NULL;
END
GO

-- =========================================================
-- 2. CWS_Tickets.AssignedToWasherId
-- Se agrega al lado de la columna vieja; recién al final se borra la vieja, así
-- el script se puede cortar por la mitad sin dejar tickets huérfanos.
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'AssignedToWasherId')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD AssignedToWasherId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CWS_Tickets_CWS_Washers')
BEGIN
    ALTER TABLE dbo.CWS_Tickets
        ADD CONSTRAINT FK_CWS_Tickets_CWS_Washers FOREIGN KEY (AssignedToWasherId) REFERENCES dbo.CWS_Washers (Id);
END
GO

-- =========================================================
-- 3. Migración de datos
-- Sólo corre si la columna vieja todavía existe.
-- =========================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'AssignedToUserId')
BEGIN
    -- 3.a. Un lavador por cada usuario que ya lavó algo. Obligatorio: sin esto
    -- los tickets históricos se quedarían sin a quién apuntar.
    INSERT INTO dbo.CWS_Washers (Id, LandlordId, FullName, IsActive, UserId, CreatedAt, CreatedBy)
    SELECT NEWID(), u.LandlordId, u.FullName, u.IsActive, u.Id, SYSUTCDATETIME(), 'migration-19'
    FROM dbo.CFG_Users u
    WHERE EXISTS (SELECT 1 FROM dbo.CWS_Tickets t WHERE t.AssignedToUserId = u.Id)
      AND NOT EXISTS (SELECT 1 FROM dbo.CWS_Washers w WHERE w.UserId = u.Id);

    -- 3.b. El resto del plantel: usuarios con el rol de sistema "Lavador" que
    -- todavía no lavaron nada. Se traen para que el negocio encuentre su lista
    -- tal como la dejó. A propósito NO se trae al Administrador aunque su rol
    -- tenga carwash.work: que el dueño apareciera como lavador era uno de los
    -- efectos raros del modelo viejo.
    INSERT INTO dbo.CWS_Washers (Id, LandlordId, FullName, IsActive, UserId, CreatedAt, CreatedBy)
    SELECT NEWID(), u.LandlordId, u.FullName, u.IsActive, u.Id, SYSUTCDATETIME(), 'migration-19'
    FROM dbo.CFG_Users u
    INNER JOIN dbo.CFG_Roles r ON r.Id = u.RoleId
    WHERE r.IsSystem = 1
      AND r.Name = N'Lavador'
      AND NOT EXISTS (SELECT 1 FROM dbo.CWS_Washers w WHERE w.UserId = u.Id);

    -- 3.c. Reapuntar los tickets.
    UPDATE t
    SET t.AssignedToWasherId = w.Id
    FROM dbo.CWS_Tickets t
    INNER JOIN dbo.CWS_Washers w ON w.UserId = t.AssignedToUserId
    WHERE t.AssignedToUserId IS NOT NULL
      AND t.AssignedToWasherId IS NULL;
END
GO

-- =========================================================
-- 4. Baja de la columna vieja
-- Después del backfill: a esta altura ningún ticket asignado quedó sin lavador.
-- =========================================================
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CWS_Tickets_CFG_Users')
BEGIN
    ALTER TABLE dbo.CWS_Tickets DROP CONSTRAINT FK_CWS_Tickets_CFG_Users;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'AssignedToUserId')
BEGIN
    ALTER TABLE dbo.CWS_Tickets DROP COLUMN AssignedToUserId;
END
GO

PRINT 'Script 19 (CWS_Washers) aplicado.';
GO
