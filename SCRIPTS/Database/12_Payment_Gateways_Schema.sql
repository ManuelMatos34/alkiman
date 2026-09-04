/*
    Módulo: PROVEEDORES DE PAGO SANDBOX (Stripe + PayPal)
    Tablas afectadas: TRX_Payments, PRT_PortalRentals

    QUÉ ES:
    Reemplaza el mock de tarjeta del checkout del Portal de Rentas (que nunca
    guardó el número real, solo marca + últimos 4 dígitos) por una verificación
    server-to-server real contra proveedores de pago en modo SANDBOX/TEST:
    Stripe (PaymentIntents) y PayPal (Orders v2). Nada de esto es productivo:
    ambos proveedores corren contra sus entornos de pruebas.

    TRX_Payments suma Provider ("Stripe"/"PayPal"/manual) y ExternalReference
    (el PaymentIntentId de Stripe o el CaptureId de PayPal) para poder rastrear
    cada movimiento de ingreso generado desde el checkout público hasta la
    transacción real en el proveedor.

    PRT_PortalRentals deja de guardar CardholderName/CardBrand/CardLast4/
    CardExpiry (ya no hay datos de tarjeta que mockear) y pasa a guardar
    PaymentProvider/PaymentReference: el mismo proveedor/referencia verificados
    server-side antes de crear la renta.
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- TRX_Payments: columnas nuevas para rastrear el proveedor de pago
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TRX_Payments') AND name = 'Provider')
BEGIN
    ALTER TABLE dbo.TRX_Payments ADD Provider NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.TRX_Payments') AND name = 'ExternalReference')
BEGIN
    ALTER TABLE dbo.TRX_Payments ADD ExternalReference NVARCHAR(255) NULL;
END
GO

-- =========================================================
-- PRT_PortalRentals: fuera el mock de tarjeta, adentro proveedor/referencia real
-- =========================================================
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'CardholderName')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals DROP COLUMN CardholderName;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'CardBrand')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals DROP COLUMN CardBrand;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'CardLast4')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals DROP COLUMN CardLast4;
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'CardExpiry')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals DROP COLUMN CardExpiry;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'PaymentProvider')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals ADD PaymentProvider NVARCHAR(20) NOT NULL CONSTRAINT DF_PRT_PortalRentals_PaymentProvider DEFAULT ('Stripe');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.PRT_PortalRentals') AND name = 'PaymentReference')
BEGIN
    ALTER TABLE dbo.PRT_PortalRentals ADD PaymentReference NVARCHAR(255) NOT NULL CONSTRAINT DF_PRT_PortalRentals_PaymentReference DEFAULT ('');
END
GO
