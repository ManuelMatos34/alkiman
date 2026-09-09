/* ============================================================================
   22 - CWS_Tickets: pago online del turno tomado por el portal
   ----------------------------------------------------------------------------
   POR QUÉ
   ----------------------------------------------------------------------------
   Hasta acá Carwash no cobraba nada. El pago era siempre en efectivo en el
   mostrador, y por eso la propina se REGISTRABA (script 20) en vez de cobrarse:
   sin rail de pago, "cobrar la propina" no era algo que el software pudiera
   hacer cumplir.

   El portal público cambia eso para SUS turnos. El cliente que se auto-registra
   desde el celular ahora recorre una pasarela paso a paso y paga con Stripe
   antes de tomar el turno. Ahí sí hay rail, así que ahí sí la propina que elige
   se cobra junto con el servicio.

   Eso obliga a separar dos cosas que hasta ahora eran una sola:

     - TipAmount ya existía y significaba "propina en efectivo que el mostrador
       confirmó al entregar". Se sigue usando igual.
     - TipPrepaid dice que ESA propina ya entró por Stripe al reservar. Sin esta
       bandera el tablero volvería a preguntar la propina al entregar el
       vehículo y la contaría dos veces: una cobrada y otra "recibida en mano"
       que nunca existió. Justo el tipo de dato falso que el script 20 se cuidó
       de no producir.

   PaidAmount se guarda aparte del total del ticket a propósito. El total es
   servicio + extras a precios congelados; PaidAmount es lo que realmente se le
   cobró a la tarjeta, propina incluida. Son números distintos y mezclarlos
   rompería tanto la facturación como el ranking de propinas.

   PaymentProvider / PaymentReference siguen la convención de TRX_Payments y
   PRT_PortalRentals: el id del PaymentIntent queda guardado para poder
   auditar o reembolsar contra Stripe.

   Los turnos presenciales no tocan ninguna de estas columnas: quedan NULL y el
   flujo de efectivo sigue exactamente igual que antes.

   Idempotente: se puede correr varias veces sin efecto.
   ============================================================================ */

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'PaymentProvider')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD PaymentProvider NVARCHAR(30) NULL;
    PRINT 'Columna CWS_Tickets.PaymentProvider agregada.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'PaymentReference')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD PaymentReference NVARCHAR(200) NULL;
    PRINT 'Columna CWS_Tickets.PaymentReference agregada.';
END
GO

/* Lo efectivamente cobrado por el gateway (servicio + extras + propina).
   Distinto del total del ticket, que no incluye propina. */
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'PaidAmount')
BEGIN
    ALTER TABLE dbo.CWS_Tickets ADD PaidAmount DECIMAL(10,2) NULL;
    PRINT 'Columna CWS_Tickets.PaidAmount agregada.';
END
GO

/* NOT NULL con default 0: todos los tickets que ya existen son de efectivo.
   WITH VALUES para que las filas viejas queden en 0 y no en NULL. */
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CWS_Tickets') AND name = 'TipPrepaid')
BEGIN
    ALTER TABLE dbo.CWS_Tickets
        ADD TipPrepaid BIT NOT NULL CONSTRAINT DF_CWS_Tickets_TipPrepaid DEFAULT (0) WITH VALUES;
    PRINT 'Columna CWS_Tickets.TipPrepaid agregada.';
END
GO

/* Índice para poder buscar un turno por su referencia de Stripe (conciliación
   y soporte). Filtrado: la enorme mayoría de los turnos son presenciales y no
   tienen referencia, no tiene sentido indexarlos. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CWS_Tickets_PaymentReference' AND object_id = OBJECT_ID(N'dbo.CWS_Tickets'))
BEGIN
    CREATE INDEX IX_CWS_Tickets_PaymentReference
        ON dbo.CWS_Tickets (PaymentReference)
        WHERE PaymentReference IS NOT NULL;
    PRINT 'Índice IX_CWS_Tickets_PaymentReference creado.';
END
GO

PRINT 'Script 22 (CWS pago online del portal) aplicado.';
GO
