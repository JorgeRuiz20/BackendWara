using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace WARA.Infrastructure.Persistence
{
    /// <summary>
    /// Fábrica responsable de crear conexiones a la Base de Datos (SQL Server o MySQL).
    /// Centraliza el acceso a la cadena de conexión para que los repositorios
    /// no dependan directamente de la configuración ni del motor específico.
    /// </summary>
    public interface ISqlConnectionFactory
    {
        IDbConnection CrearConexion();
    }

    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        private readonly string _provider;

        public SqlConnectionFactory(string connectionString, string provider = "Auto")
        {
            _connectionString = connectionString;
            _provider = provider;
        }

        public IDbConnection CrearConexion()
        {
            if (EsMySql())
            {
                return new MySqlConnection(_connectionString);
            }

            return new SqlConnection(_connectionString);
        }

        private bool EsMySql()
        {
            if (string.Equals(_provider, "MySQL", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(_provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
                return false;

            // Detección automática según parámetros típicos de MySQL en la cadena de conexión
            return _connectionString.Contains("Port=3306", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("Port=27274", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("aivencloud", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("Uid=", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("User=root", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("charset=", StringComparison.OrdinalIgnoreCase) ||
                   _connectionString.Contains("AllowUserVariables", StringComparison.OrdinalIgnoreCase);
        }
    }
}
