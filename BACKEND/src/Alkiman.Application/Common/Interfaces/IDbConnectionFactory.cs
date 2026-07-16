using System.Data;

namespace Alkiman.Application.Common.Interfaces;

/// <summary>
/// Abstrae la creación de conexiones ADO.NET para que la capa Application
/// no dependa directamente de Microsoft.Data.SqlClient (eso vive en Infrastructure).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Crea y abre una nueva conexión a la base de datos.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
