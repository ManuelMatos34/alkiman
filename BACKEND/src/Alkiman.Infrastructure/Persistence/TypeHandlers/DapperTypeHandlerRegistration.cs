using Alkiman.Domain.Enums;
using Dapper;

namespace Alkiman.Infrastructure.Persistence.TypeHandlers;

/// <summary>Registro centralizado de los TypeHandlers de Dapper usados por Infrastructure.</summary>
public static class DapperTypeHandlerRegistration
{
    public static void RegisterAll()
    {
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<AssetStatus>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<RentalTypeOption>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<RentalStatus>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<PaymentType>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<AuditActionType>());
    }
}
