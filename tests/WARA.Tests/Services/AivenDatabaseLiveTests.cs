using System;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using WARA.Infrastructure.Persistence;
using WARA.Infrastructure.Repositories;
using Xunit;

namespace WARA.Tests.Services
{
    public class AivenDatabaseLiveTests
    {
        private static readonly string? ConnectionString =
            Environment.GetEnvironmentVariable("WARA_DB_CONNECTION") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__WaraDatabase");

        [Fact]
        public void SqlConnectionFactory_ShouldCreateAndOpenMySqlConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                // Si no hay cadena de conexión en el entorno, validar únicamente la detección del provider
                var testFactory = new SqlConnectionFactory("Server=mysql-host.aivencloud.com;Port=27274;Database=WaraDB;User Id=avnadmin;Password=fake;SslMode=Required;", "MySQL");
                using var conn = testFactory.CrearConexion();
                conn.GetType().Name.Should().Be("MySqlConnection");
                return;
            }

            var factory = new SqlConnectionFactory(ConnectionString, "MySQL");
            using var connection = factory.CrearConexion();

            connection.GetType().Name.Should().Be("MySqlConnection");
            connection.Open();
            connection.State.Should().Be(ConnectionState.Open);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DATABASE(), VERSION()";
            using var reader = cmd.ExecuteReader();
            reader.Read().Should().BeTrue();
            var dbName = reader.GetString(0);
            dbName.Should().Be("WaraDB");
        }

        [Fact]
        public async Task Repositories_CanQueryAivenDatabaseWithoutErrors()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                // Omite la llamada remota si no está seteada la variable de entorno
                return;
            }

            var factory = new SqlConnectionFactory(ConnectionString, "MySQL");
            var usuarioRepo = new UsuarioRepository(factory);
            var trabajadorRepo = new TrabajadorRepository(factory);

            // Test Usuario query
            var existe = await usuarioRepo.ExisteUsuarioAsync("usuario_inexistente_de_prueba_123");
            existe.Should().BeFalse();

            // Test Trabajador query
            var lista = await trabajadorRepo.ListarAsync(null, 1, 10);
            lista.Should().NotBeNull();
            lista.TotalRegistros.Should().BeGreaterOrEqualTo(0);
        }
    }
}
