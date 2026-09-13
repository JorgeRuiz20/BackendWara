using FluentAssertions;
using Moq;
using WARA.Application.Exceptions;
using WARA.Application.Services;
using WARA.Domain.Entities;
using WARA.Domain.Ports;
using Xunit;

namespace WARA.Tests.Services
{
    /// <summary>
    /// Tests unitarios de TrabajadorService. El repositorio se mockea con Moq para
    /// aislar por completo la lógica de negocio de cualquier acceso real a base de datos:
    /// estos tests corren en memoria, en milisegundos, sin SQL Server levantado.
    /// </summary>
    public class TrabajadorServiceTests
    {
        private readonly Mock<ITrabajadorRepository> _repositorioMock;
        private readonly TrabajadorService _service;

        public TrabajadorServiceTests()
        {
            _repositorioMock = new Mock<ITrabajadorRepository>();
            _service = new TrabajadorService(_repositorioMock.Object);
        }

        [Fact]
        public async Task AgregarTrabajadorAsync_ConDatosValidos_RetornaTrabajadorCreado()
        {
            // Arrange
            _repositorioMock
                .Setup(r => r.ExisteDniAsync("71234567"))
                .ReturnsAsync(false);

            _repositorioMock
                .Setup(r => r.AgregarAsync(It.IsAny<Trabajador>()))
                .ReturnsAsync((Trabajador t) => { t.Id = 1; return t; });

            // Act
            var resultado = await _service.AgregarTrabajadorAsync("Juan", "Pérez", "71234567", 28);

            // Assert
            resultado.Id.Should().Be(1);
            resultado.Nombre.Should().Be("Juan");
            resultado.Dni.Should().Be("71234567");
            _repositorioMock.Verify(r => r.AgregarAsync(It.IsAny<Trabajador>()), Times.Once);
        }

        [Fact]
        public async Task AgregarTrabajadorAsync_ConDniYaExistente_LanzaBusinessRuleException()
        {
            // Arrange: ya existe un trabajador activo con ese DNI
            _repositorioMock
                .Setup(r => r.ExisteDniAsync("71234567"))
                .ReturnsAsync(true);

            // Act
            Func<Task> accion = () => _service.AgregarTrabajadorAsync("Juan", "Pérez", "71234567", 28);

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*71234567*");

            // No debe intentar insertar si la validación de negocio falla antes
            _repositorioMock.Verify(r => r.AgregarAsync(It.IsAny<Trabajador>()), Times.Never);
        }

        [Theory]
        [InlineData("", "Pérez", "71234567", 28)]      // nombre vacío
        [InlineData("Juan", "", "71234567", 28)]        // apellido vacío
        [InlineData("Juan", "Pérez", "123", 28)]        // DNI con menos de 8 dígitos
        [InlineData("Juan", "Pérez", "7123456A", 28)]   // DNI no numérico
        [InlineData("Juan", "Pérez", "71234567", 17)]   // edad menor al mínimo permitido
        [InlineData("Juan", "Pérez", "71234567", 81)]   // edad mayor al máximo permitido
        public async Task AgregarTrabajadorAsync_ConDatosInvalidos_LanzaBusinessRuleException(
            string nombre, string apellido, string dni, int edad)
        {
            // Act
            Func<Task> accion = () => _service.AgregarTrabajadorAsync(nombre, apellido, dni, edad);

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>();

            // Ninguna validación de negocio inválida debe siquiera consultar la BD
            _repositorioMock.Verify(r => r.ExisteDniAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ListarTrabajadoresAsync_ConTamanoPaginaFueraDeRango_LoNormalizaAntesDeConsultar()
        {
            // Arrange
            _repositorioMock
                .Setup(r => r.ListarAsync(null, 1, 100))
                .ReturnsAsync(new TrabajadoresPaginados { Items = new List<Trabajador>(), TotalRegistros = 0 });

            // Act: se piden 500 registros por página (fuera de rango) y página 0 (inválida)
            await _service.ListarTrabajadoresAsync(null, pagina: 0, tamanoPagina: 500);

            // Assert: el service debe haber normalizado a página 1 y tope de 100
            _repositorioMock.Verify(r => r.ListarAsync(null, 1, 100), Times.Once);
        }

        [Fact]
        public async Task ObtenerTrabajadorPorIdAsync_ConIdInexistente_RetornaNull()
        {
            // Arrange
            _repositorioMock
                .Setup(r => r.ObtenerPorIdAsync(999))
                .ReturnsAsync((Trabajador?)null);

            // Act
            var resultado = await _service.ObtenerTrabajadorPorIdAsync(999);

            // Assert
            resultado.Should().BeNull();
        }

        [Fact]
        public async Task ActualizarTrabajadorAsync_ConEdadInvalida_LanzaBusinessRuleException()
        {
            // Act
            Func<Task> accion = () => _service.ActualizarTrabajadorAsync(1, "Juan", "Pérez", edad: 10);

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>();
            _repositorioMock.Verify(r => r.ActualizarAsync(It.IsAny<Trabajador>()), Times.Never);
        }

        [Fact]
        public async Task EliminarTrabajadorAsync_DelegaEnElRepositorioYRetornaSuResultado()
        {
            // Arrange
            _repositorioMock
                .Setup(r => r.EliminarAsync(5))
                .ReturnsAsync(true);

            // Act
            var resultado = await _service.EliminarTrabajadorAsync(5);

            // Assert
            resultado.Should().BeTrue();
        }
    }
}
