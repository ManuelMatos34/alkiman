/*
    Módulo: FIRMA DE LA EMPRESA (Landlord)
    Tablas afectadas: CFG_Landlords

    QUÉ ES:
    Permite que el negocio (landlord) dibuje y guarde su propia firma digital desde
    Ajustes, reutilizando el mismo componente SignaturePad que ya se usa para la firma
    del cliente en Contratos. Esa firma se persiste como PNG en Base64 para poder
    imprimirla junto a la firma del cliente en el PDF del contrato (dos firmas: empresa
    y cliente).
*/
USE Alkiman_Dev;
GO

-- =========================================================
-- CFG_Landlords: firma de la empresa en Base64
-- =========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.CFG_Landlords') AND name = 'SignatureBase64')
BEGIN
    ALTER TABLE dbo.CFG_Landlords ADD SignatureBase64 NVARCHAR(MAX) NULL;
END
GO
