/*
    Módulo: RentalType basado en PERÍODOS (INV_Assets.RentalType)

    QUÉ CAMBIA:
    El "tipo de renta" de un activo dejaba de ser una clasificación libre
    (LongTerm/ShortTerm) para pasar a ser la UNIDAD DE TIEMPO en la que se
    renta (Daily/Weekly/Biweekly/Monthly/Annual). Esa unidad determina:
      - El mínimo que un cliente puede rentar en el Portal público (no puede
        rentar menos de 1 período de la unidad configurada en el activo).
      - El cálculo del precio total: BasePrice x Periods x Quantity
        (ver Alkiman.Domain.Common.RentalPeriodCalculator).

    Mapeo de datos existentes: los activos que ya estaban en 'LongTerm' pasan
    a 'Monthly' (el caso de uso más común de largo plazo) y los 'ShortTerm'
    pasan a 'Daily' (el más común de corto plazo). Es una migración con
    pérdida de precisión aceptada: no había forma de inferir la unidad exacta
    a partir del valor anterior.

    También se agrega PRT_PortalRentals.Periods, para dejar registrado cuántos
    períodos contrató el cliente en cada checkout (mismo criterio que la
    columna Quantity ya existente).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- INV_Assets.RentalType: hay que soltar el CHECK viejo ANTES de reescribir
-- los datos (si no, el propio UPDATE viola la constraint anterior).
-- =========================================================
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_INV_Assets_RentalType' AND parent_object_id = OBJECT_ID(N'dbo.INV_Assets')
)
BEGIN
    ALTER TABLE dbo.INV_Assets DROP CONSTRAINT CK_INV_Assets_RentalType;
END
GO

UPDATE dbo.INV_Assets SET RentalType = 'Monthly' WHERE RentalType = 'LongTerm';
GO

UPDATE dbo.INV_Assets SET RentalType = 'Daily' WHERE RentalType = 'ShortTerm';
GO

ALTER TABLE dbo.INV_Assets
    ADD CONSTRAINT CK_INV_Assets_RentalType CHECK (RentalType IN ('Daily', 'Weekly', 'Biweekly', 'Monthly', 'Annual'));
GO

-- =========================================================
-- PRT_PortalRentals.Periods: cantidad de períodos contratados en el checkout
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'Periods')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals ADD Periods INT NOT NULL CONSTRAINT DF_PRT_PortalRentals_Periods DEFAULT (1);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_PRT_PortalRentals_Periods' AND parent_object_id = OBJECT_ID(N'dbo.PRT_PortalRentals')
)
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals ADD CONSTRAINT CK_PRT_PortalRentals_Periods CHECK (Periods > 0);
END
GO
