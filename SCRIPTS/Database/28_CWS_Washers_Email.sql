-- 28_CWS_Washers_Email.sql
-- Agrega Email opcional a los lavadores para enviarles notificaciones de propina.
ALTER TABLE dbo.CWS_Washers
    ADD Email NVARCHAR(254) NULL;
