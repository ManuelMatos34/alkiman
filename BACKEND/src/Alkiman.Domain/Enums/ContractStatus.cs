namespace Alkiman.Domain.Enums;

/// <summary>Estado de un contrato generado para una renta (columna COM_Contracts.Status).</summary>
public enum ContractStatus
{
    /// <summary>Generado pero todavía sin firma del cliente (ej: renta creada manualmente por el negocio).</summary>
    Pending,
    /// <summary>Firmado digitalmente (por el cliente, ej: en el Portal, o cargado luego desde el mantenimiento de contratos).</summary>
    Signed
}
