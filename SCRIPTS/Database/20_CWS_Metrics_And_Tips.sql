/*
    Módulo: CARWASH -- métricas del negocio y propinas de lavadores
    Tablas alteradas: CWS_Tickets  (TipAmount, TipWasherId)
                      CWS_Settings (TipMode, TipSuggestedPercent)
    Permiso nuevo:    carwash.reports

    ---------------------------------------------------------------------------
    POR QUÉ LAS PROPINAS SE *REGISTRAN* Y NO SE *COBRAN*
    ---------------------------------------------------------------------------
    Carwash no procesa pagos. No hay filas en TRX_Payments para un ticket, no hay
    pasarela conectada al módulo y el precio del servicio se cobra en el mostrador,
    en efectivo. El software no mueve dinero acá: sólo deja constancia.

    Eso descarta la opción de "cobrar siempre la propina": no hay nada que cobrar
    desde el sistema. Y aunque lo hubiera, un cargo obligatorio agregado a todos
    los tickets no es una propina, es un aumento de precio con otro nombre —
    el lavador no lo percibe como reconocimiento y el cliente lo descubre en el
    total. Peor todavía para el dato: si el sistema asume una propina que en el
    mostrador nunca se entregó, el ranking de lavadores —que es justamente lo que
    esta función existe para producir— queda inflado y deja de servir para
    repartir nada.

    Entonces se modela la INTENCIÓN del negocio, con tres modos:

      Disabled  -> el negocio no maneja propinas. No se pregunta nada al entregar.
      Optional  -> al entregar el vehículo se pregunta, con el campo vacío.
                   Es el default: no presupone nada.
      Suggested -> igual que Optional pero el campo viene pre-cargado con
                   TipSuggestedPercent % del total. Sugiere, no impone: el cajero
                   igual confirma o corrige el monto que realmente recibió.

    En los tres casos el monto que queda guardado es el que alguien confirmó
    haber recibido. Nunca uno calculado a espaldas del mostrador.

    ---------------------------------------------------------------------------
    POR QUÉ TipWasherId SEPARADO DE AssignedToWasherId
    ---------------------------------------------------------------------------
    Mismo criterio que ServicePrice, que se congela en el ticket en vez de leerse
    de CWS_Services: lo que ya pasó no se recalcula.

    AssignedToWasherId es operativo y cambia — un lavador se va a mitad de turno y
    el ticket se reasigna. Si el ranking de propinas leyera esa columna, reasignar
    un ticket entregado le movería la plata ganada de una persona a otra sin que
    nadie lo pida ni lo vea. Al congelar a quién se le atribuyó la propina en el
    momento de entregar, el histórico deja de depender de una columna que sigue
    viva.

    IDEMPOTENTE: se puede correr varias veces (mismo criterio que 13, 14, 17 y 19).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- 1. CWS_Tickets: propina recibida
-- NULL = no se registró propina (ni se preguntó, o se preguntó y no hubo).
-- 0 explícito y NULL se guardan igual de bien; para las métricas ambos suman 0.
-- =========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = N'TipAmount'
)
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD TipAmount DECIMAL(10,2) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = N'TipWasherId'
)
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD TipWasherId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CWS_Tickets_TipAmount'
)
BEGIN
    ALTER TABLE dbo.CWS_Tickets
        ADD CONSTRAINT CK_CWS_Tickets_TipAmount CHECK (TipAmount IS NULL OR TipAmount >= 0);
END
GO

-- Sin ON DELETE: un lavador con propinas registradas no se borra, se desactiva
-- (IsActive), igual que en el script 19. Que la FK lo impida es lo correcto.
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CWS_Tickets_TipWasher'
)
BEGIN
    ALTER TABLE dbo.CWS_Tickets
        ADD CONSTRAINT FK_CWS_Tickets_TipWasher
        FOREIGN KEY (TipWasherId) REFERENCES dbo.CWS_Washers (Id);
END
GO

-- =========================================================
-- 2. CWS_Settings: política de propinas del negocio
-- =========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Settings') AND name = N'TipMode'
)
BEGIN
    -- Default 'Optional': los negocios que ya venían usando el módulo no eligieron
    -- una política, así que la que menos les cambia la operación es preguntar sin
    -- presuponer. Nunca 'Suggested' por omisión: sugerir un monto es una decisión
    -- comercial del dueño, no algo que le active una migración.
    ALTER TABLE dbo.CWS_Settings
        ADD TipMode NVARCHAR(20) NOT NULL
            CONSTRAINT DF_CWS_Settings_TipMode DEFAULT ('Optional');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Settings') AND name = N'TipSuggestedPercent'
)
BEGIN
    ALTER TABLE dbo.CWS_Settings
        ADD TipSuggestedPercent DECIMAL(5,2) NOT NULL
            CONSTRAINT DF_CWS_Settings_TipSuggestedPercent DEFAULT (10.00);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CWS_Settings_TipMode'
)
BEGIN
    ALTER TABLE dbo.CWS_Settings
        ADD CONSTRAINT CK_CWS_Settings_TipMode
        CHECK (TipMode IN ('Disabled','Optional','Suggested'));
END
GO

-- Tope en 100: un porcentaje sugerido mayor que el propio servicio es un error
-- de tipeo (poner 100 donde iba 10), no una política. Que reviente acá es más
-- barato que descubrirlo en la caja.
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CWS_Settings_TipSuggestedPercent'
)
BEGIN
    ALTER TABLE dbo.CWS_Settings
        ADD CONSTRAINT CK_CWS_Settings_TipSuggestedPercent
        CHECK (TipSuggestedPercent >= 0 AND TipSuggestedPercent <= 100);
END
GO

-- =========================================================
-- 3. Índices para las métricas
-- Todo el tablero filtra por (negocio, rango de fechas) y agrupa por servicio,
-- lavador o estado. Sin esto cada tarjeta es un scan de CWS_Tickets completo.
-- =========================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = N'IX_CWS_Tickets_Landlord_CreatedAt'
)
BEGIN
    CREATE INDEX IX_CWS_Tickets_Landlord_CreatedAt
        ON dbo.CWS_Tickets (LandlordId, CreatedAt)
        INCLUDE (Status, ServiceId, ServicePrice, AssignedToWasherId, TipAmount, TipWasherId, StartedAt, DeliveredAt);
END
GO

-- Filtrado: la mayoría de los tickets viejos no tienen lavador asignado y no
-- aportan nada al ranking.
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = N'IX_CWS_Tickets_Landlord_Washer'
)
BEGIN
    CREATE INDEX IX_CWS_Tickets_Landlord_Washer
        ON dbo.CWS_Tickets (LandlordId, AssignedToWasherId)
        INCLUDE (Status, DeliveredAt, ServicePrice, TipAmount)
        WHERE AssignedToWasherId IS NOT NULL;
END
GO

-- =========================================================
-- 4. Permiso 'carwash.reports'
-- Separado de carwash.view por la misma razón que reports.view lo está en
-- Alquileres: ver la cola del día es operativo y lo necesita cualquiera que
-- atienda el mostrador; ver facturación, propinas y el ranking de quién rinde
-- más es información del dueño.
--
-- Se inserta con ModuleCode ya puesto (la columna existe desde el script 18):
-- un permiso sin clasificar aparecería en el editor de roles de negocios que
-- no compraron Carwash.
-- =========================================================
INSERT INTO dbo.CFG_Permissions (Code, Module, Description, ModuleCode)
SELECT v.Code, v.Module, v.Description, v.ModuleCode
FROM (VALUES
    ('carwash.reports', N'Carwash', N'Ver métricas, facturación y ranking de lavadores de Carwash', 'carwash')
) AS v(Code, Module, Description, ModuleCode)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CFG_Permissions p WHERE p.Code = v.Code);
GO

-- Sólo a los roles de sistema (Administrador) de negocios que YA tienen Carwash
-- habilitado. El script 18 tuvo que limpiar el desastre de dárselo a todos;
-- no se repite acá. Al rol "Lavador" no se le da: un lavador no mira la
-- facturación del negocio.
INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.CFG_Roles r
CROSS JOIN dbo.CFG_Permissions p
WHERE r.IsSystem = 1
  AND r.Name <> N'Lavador'
  AND p.Code = 'carwash.reports'
  AND EXISTS (
      -- CFG_LandlordModules no tiene bandera: que exista la fila ES estar habilitado.
      SELECT 1 FROM dbo.CFG_LandlordModules lm
      WHERE lm.LandlordId = r.LandlordId AND lm.ModuleCode = 'carwash'
  )
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CFG_RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

PRINT 'Script 20 (CWS métricas y propinas) aplicado.';
GO
