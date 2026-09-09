USE Alkiman_Dev;
GO

-- Agrega CustomerName como snapshot del nombre al registrar el turno,
-- igual que ServicePrice snapshottea el precio. Así el nombre del ticket
-- no cambia aunque el registro CRM_Customers se modifique después.
ALTER TABLE dbo.CWS_Tickets
    ADD CustomerName NVARCHAR(150) NULL;
GO

-- Rellena registros históricos con el nombre actual del cliente.
UPDATE t
SET    t.CustomerName = c.FullName
FROM   dbo.CWS_Tickets t
JOIN   dbo.CRM_Customers c ON c.Id = t.CustomerId;
GO
