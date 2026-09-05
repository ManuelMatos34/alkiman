/*
    Módulo: CARWASH -- se elimina el vínculo lavador <-> cuenta del sistema
    Tablas alteradas: CWS_Washers (se elimina la columna UserId)

    ---------------------------------------------------------------------------
    POR QUÉ SE ELIMINA
    ---------------------------------------------------------------------------
    CWS_Washers.UserId permitía vincular opcionalmente un lavador con una cuenta
    de CFG_Users. La idea era que quien además tuviera que entrar al software
    quedara identificado con su propia ficha.

    En la práctica el campo confundía más de lo que resolvía. En el formulario de
    alta aparecía un combo con formato "Nombre · email" que se lee como un
    selector de PERSONAS, cuando en realidad era un selector de ACCESOS. El
    usuario que carga a su personal no tiene por qué distinguir esas dos cosas —y
    no debería tener que hacerlo— así que la pregunta simplemente sobraba.

    La separación conceptual se mantiene intacta, y ahora sin excepciones:
    un lavador es una ficha del módulo (el mismo lugar que ocupa Customer en
    Alquileres) y nada más. No es, ni puede llegar a ser, una identidad del
    sistema.

    ---------------------------------------------------------------------------
    QUÉ NO SE ROMPE
    ---------------------------------------------------------------------------
    El rol de sistema "Lavador" sigue funcionando igual. Ese rol se apoya en el
    permiso carwash.work, que habilita el endpoint de avanzar la cola; nunca
    filtró los turnos por persona, así que no necesitaba el vínculo para nada.

    Lo único que sí lo usaba era el auto-asignado del modo Solitario: al iniciar
    un lavado el ticket se ponía a nombre del lavador vinculado a la cuenta que
    estaba operando. Se reemplaza por una regla que no necesita usuarios: en modo
    Solitario hay un único lavador activo, y es ese el que recibe el trabajo
    (ver CarwashService.GetSoloWasherAsync). Si hubiera más de uno la asignación
    queda manual, que es lo correcto: con varios lavadores el negocio ya no es
    Solitario y adivinar sería peor que preguntar.

    No hay migración de datos que hacer: UserId era informativo. Los turnos
    históricos apuntan a CWS_Washers.Id (AssignedToWasherId), no a la cuenta, así
    que el historial y las métricas por persona quedan intactos.
*/

SET NOCOUNT ON;
GO

-- 1) El índice único filtrado que impedía que dos lavadores compartieran cuenta.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_CWS_Washers_UserId' AND object_id = OBJECT_ID(N'dbo.CWS_Washers'))
BEGIN
    DROP INDEX UQ_CWS_Washers_UserId ON dbo.CWS_Washers;
    PRINT 'Índice UQ_CWS_Washers_UserId eliminado.';
END
GO

-- 2) La FK contra CFG_Users.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CWS_Washers_CFG_Users' AND parent_object_id = OBJECT_ID(N'dbo.CWS_Washers'))
BEGIN
    ALTER TABLE dbo.CWS_Washers DROP CONSTRAINT FK_CWS_Washers_CFG_Users;
    PRINT 'FK_CWS_Washers_CFG_Users eliminada.';
END
GO

-- 3) La columna. Va al final: mientras exista el índice o la FK, el DROP COLUMN falla.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Washers') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.CWS_Washers DROP COLUMN UserId;
    PRINT 'Columna CWS_Washers.UserId eliminada.';
END
GO

PRINT 'Script 21 (CWS quitar vínculo con cuentas del sistema) aplicado.';
GO
