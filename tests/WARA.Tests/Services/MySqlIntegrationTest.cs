using System;
using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using MySqlConnector;
using WARA.Domain.Entities;
using WARA.Infrastructure.Persistence;
using WARA.Infrastructure.Repositories;
using Xunit;

namespace WARA.Tests.Services
{
    public class MySqlIntegrationTest
    {
        private const string MySqlConnectionString = "Server=localhost;Port=3306;Database=WaraDB;User Id=root;Password=1234;";

        private class TestMySqlConnectionFactory : ISqlConnectionFactory
        {
            public IDbConnection CrearConexion()
            {
                return new MySqlConnection(MySqlConnectionString);
            }
        }

        [Fact]
        public async Task Test_MySql_Repositories_EndToEnd()
        {
            var factory = new TestMySqlConnectionFactory();

            // 1. Health check / Connection test
            using (var conn = factory.CrearConexion())
            {
                conn.Open();
                conn.State.Should().Be(ConnectionState.Open);
            }

            var trabajadorRepo = new TrabajadorRepository(factory);
            var usuarioRepo = new UsuarioRepository(factory);

            // 2. Test UsuarioRepository.ExisteUsuarioAsync
            var existe = await usuarioRepo.ExisteUsuarioAsync("usuario_inexistente_123");
            existe.Should().BeFalse();

            // 3. Test UsuarioRepository.CrearAsync & ObtenerPorNombreUsuarioAsync
            var randomUser = "testuser_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            await usuarioRepo.CrearAsync(new Usuario
            {
                NombreUsuario = randomUser,
                PasswordHash = "hash123",
                Activo = true
            });

            var userExiste = await usuarioRepo.ExisteUsuarioAsync(randomUser);
            userExiste.Should().BeTrue();

            var userObtenido = await usuarioRepo.ObtenerPorNombreUsuarioAsync(randomUser);
            userObtenido.Should().NotBeNull();
            userObtenido!.NombreUsuario.Should().Be(randomUser);

            // 4. Test TrabajadorRepository.AgregarAsync
            var randomDni = new Random().Next(10000000, 99999999).ToString();
            var nuevoTrabajador = await trabajadorRepo.AgregarAsync(new Trabajador
            {
                Nombre = "Elena",
                Apellido = "Ramos",
                Dni = randomDni,
                Edad = 27
            });

            nuevoTrabajador.Should().NotBeNull();
            nuevoTrabajador.Id.Should().BeGreaterThan(0);
            nuevoTrabajador.Nombre.Should().Be("Elena");

            // 5. Test TrabajadorRepository.ObtenerPorIdAsync
            var trabajadorPorId = await trabajadorRepo.ObtenerPorIdAsync(nuevoTrabajador.Id);
            trabajadorPorId.Should().NotBeNull();
            trabajadorPorId!.Dni.Should().Be(randomDni);

            // 6. Test TrabajadorRepository.ListarAsync (Multiple result sets)
            var listado = await trabajadorRepo.ListarAsync(null, 1, 10);
            listado.Should().NotBeNull();
            listado.TotalRegistros.Should().BeGreaterThan(0);
            listado.Items.Should().NotBeEmpty();

            // 7. Test TrabajadorRepository.ActualizarAsync
            nuevoTrabajador.Nombre = "Elena Sofía";
            nuevoTrabajador.Edad = 28;
            var actualizado = await trabajadorRepo.ActualizarAsync(nuevoTrabajador);
            actualizado.Should().NotBeNull();
            actualizado!.Nombre.Should().Be("Elena Sofía");
            actualizado.Edad.Should().Be(28);

            // 8. Test TrabajadorRepository.EliminarAsync (Soft delete)
            var eliminado = await trabajadorRepo.EliminarAsync(nuevoTrabajador.Id);
            eliminado.Should().BeTrue();

            var despuesDeEliminar = await trabajadorRepo.ObtenerPorIdAsync(nuevoTrabajador.Id);
            despuesDeEliminar.Should().BeNull();
        }
    }
}
