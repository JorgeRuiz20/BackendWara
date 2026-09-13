using Microsoft.Extensions.Diagnostics.HealthChecks;
using WARA.Infrastructure.Persistence;

namespace WARA.API.HealthChecks
{
    /// <summary>
    /// Health check que verifica que la API puede efectivamente abrir una conexión
    /// y ejecutar una consulta trivial ("SELECT 1") contra SQL Server.
    /// No depende de ningún paquete NuGet de terceros: reutiliza el mismo
    /// ISqlConnectionFactory que usan los repositorios de Infrastructure, así que
    /// si este check pasa, la API está genuinamente lista para atender requests
    /// que necesiten base de datos (no solo "el proceso está vivo").
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlServerHealthCheck(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = _connectionFactory.CrearConexion();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                command.CommandTimeout = 5;
                command.ExecuteScalar();

                return HealthCheckResult.Healthy($"Conexión a base de datos operativa ({connection.GetType().Name}).");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos.", ex);
            }
        }
    }
}
