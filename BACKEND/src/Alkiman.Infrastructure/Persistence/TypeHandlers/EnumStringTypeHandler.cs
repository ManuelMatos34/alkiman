using System.Data;
using Dapper;

namespace Alkiman.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Mapea un enum de C# hacia/desde una columna NVARCHAR que almacena su nombre como texto
/// (ej. INV_Assets.Status = 'Available'), en lugar del entero por defecto de Dapper.
/// </summary>
public class EnumStringTypeHandler<T> : SqlMapper.TypeHandler<T> where T : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    public override T Parse(object value)
    {
        return Enum.Parse<T>((string)value, ignoreCase: true);
    }
}
