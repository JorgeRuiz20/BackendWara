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
    /// Tests unitarios de AuthService. Se mockean los tres puertos de salida
    /// (repositorio, hasher, generador de token) para probar la lógica de negocio
    /// de login/registro de forma completamente aislada.
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _usuarioRepositoryMock = new Mock<IUsuarioRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _tokenGeneratorMock = new Mock<ITokenGenerator>();

            _service = new AuthService(
                _usuarioRepositoryMock.Object,
                _passwordHasherMock.Object,
                _tokenGeneratorMock.Object);
        }

        [Fact]
        public async Task LoginAsync_ConCredencialesValidas_RetornaResultadoExitosoConToken()
        {
            // Arrange
            var usuario = new Usuario { Id = 1, NombreUsuario = "jperez", PasswordHash = "hash123", Activo = true };

            _usuarioRepositoryMock
                .Setup(r => r.ObtenerPorNombreUsuarioAsync("jperez"))
                .ReturnsAsync(usuario);

            _passwordHasherMock
                .Setup(h => h.Verificar("ClaveSecreta123@", "hash123"))
                .Returns(true);

            _tokenGeneratorMock
                .Setup(t => t.GenerarToken(1, "jperez"))
                .Returns("token-jwt-simulado");

            // Act
            var resultado = await _service.LoginAsync("jperez", "ClaveSecreta123@");

            // Assert
            resultado.EsExitoso.Should().BeTrue();
            resultado.Token.Should().Be("token-jwt-simulado");
            resultado.NombreUsuario.Should().Be("jperez");
        }

        [Fact]
        public async Task LoginAsync_ConUsuarioInexistente_RetornaResultadoFallido()
        {
            // Arrange
            _usuarioRepositoryMock
                .Setup(r => r.ObtenerPorNombreUsuarioAsync("noexiste"))
                .ReturnsAsync((Usuario?)null);

            // Act
            var resultado = await _service.LoginAsync("noexiste", "CualquierClave123@");

            // Assert
            resultado.EsExitoso.Should().BeFalse();
            resultado.Token.Should().BeNull();
            resultado.MensajeError.Should().Be("Credenciales inválidas.");
            // No debe intentar generar token si el usuario no existe
            _tokenGeneratorMock.Verify(t => t.GenerarToken(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ConPasswordIncorrecto_RetornaResultadoFallido()
        {
            // Arrange
            var usuario = new Usuario { Id = 1, NombreUsuario = "jperez", PasswordHash = "hash123", Activo = true };

            _usuarioRepositoryMock
                .Setup(r => r.ObtenerPorNombreUsuarioAsync("jperez"))
                .ReturnsAsync(usuario);

            _passwordHasherMock
                .Setup(h => h.Verificar("ClaveIncorrecta123@", "hash123"))
                .Returns(false);

            // Act
            var resultado = await _service.LoginAsync("jperez", "ClaveIncorrecta123@");

            // Assert
            resultado.EsExitoso.Should().BeFalse();
            resultado.MensajeError.Should().Be("Credenciales inválidas.");
        }

        [Fact]
        public async Task LoginAsync_ConUsuarioInactivo_RetornaResultadoFallido()
        {
            // Arrange: usuario existe pero fue desactivado
            var usuario = new Usuario { Id = 1, NombreUsuario = "jperez", PasswordHash = "hash123", Activo = false };

            _usuarioRepositoryMock
                .Setup(r => r.ObtenerPorNombreUsuarioAsync("jperez"))
                .ReturnsAsync(usuario);

            // Act
            var resultado = await _service.LoginAsync("jperez", "CualquierClave123@");

            // Assert
            resultado.EsExitoso.Should().BeFalse();
            // No debe ni intentar verificar el password de un usuario inactivo
            _passwordHasherMock.Verify(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("", "ClaveValida123@")]      // usuario vacío
        [InlineData("jperez", "")]              // password vacío
        public async Task LoginAsync_ConCamposVacios_RetornaResultadoFallidoSinConsultarRepositorio(
            string nombreUsuario, string password)
        {
            // Act
            var resultado = await _service.LoginAsync(nombreUsuario, password);

            // Assert
            resultado.EsExitoso.Should().BeFalse();
            _usuarioRepositoryMock.Verify(
                r => r.ObtenerPorNombreUsuarioAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ConPasswordSinMayusculaOSimbolo_RetornaResultadoFallido()
        {
            // Act
            var resultado1 = await _service.LoginAsync("jperez", "clavesinmayus123");
            var resultado2 = await _service.LoginAsync("jperez", "ClaveSinSimbolo123");

            // Assert
            resultado1.EsExitoso.Should().BeFalse();
            resultado1.MensajeError.Should().Contain("mayúscula");
            resultado2.EsExitoso.Should().BeFalse();
            resultado2.MensajeError.Should().Contain("símbolo");
        }

        [Fact]
        public async Task RegistrarAsync_ConNombreUsuarioYaExistente_LanzaBusinessRuleException()
        {
            // Arrange
            _usuarioRepositoryMock
                .Setup(r => r.ExisteUsuarioAsync("jperez"))
                .ReturnsAsync(true);

            // Act
            Func<Task> accion = () => _service.RegistrarAsync("jperez", "ClaveValida123@");

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*jperez*");

            _usuarioRepositoryMock.Verify(r => r.CrearAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarAsync_ConPasswordCortoDeMenosDe8Caracteres_LanzaBusinessRuleException()
        {
            // Act
            Func<Task> accion = () => _service.RegistrarAsync("nuevoUsuario", "Co@1");

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>();
            _usuarioRepositoryMock.Verify(r => r.CrearAsync(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarAsync_SinMayuscula_LanzaBusinessRuleException()
        {
            // Act
            Func<Task> accion = () => _service.RegistrarAsync("nuevoUsuario", "claveminuscula123@");

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*mayúscula*");
        }

        [Fact]
        public async Task RegistrarAsync_SinSimbolo_LanzaBusinessRuleException()
        {
            // Act
            Func<Task> accion = () => _service.RegistrarAsync("nuevoUsuario", "ClaveSinSimbolo123");

            // Assert
            await accion.Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("*símbolo*");
        }

        [Fact]
        public async Task RegistrarAsync_ConDatosValidos_HasheaLaPasswordAntesDeGuardar()
        {
            // Arrange
            _usuarioRepositoryMock
                .Setup(r => r.ExisteUsuarioAsync("nuevoUsuario"))
                .ReturnsAsync(false);

            _passwordHasherMock
                .Setup(h => h.Hashear("ClaveValida123@"))
                .Returns("hashGenerado");

            Usuario? usuarioCreado = null;
            _usuarioRepositoryMock
                .Setup(r => r.CrearAsync(It.IsAny<Usuario>()))
                .Callback<Usuario>(u => usuarioCreado = u)
                .Returns(Task.CompletedTask);

            // Act
            await _service.RegistrarAsync("nuevoUsuario", "ClaveValida123@");

            // Assert: la contraseña que se persiste es el HASH, nunca el texto plano
            usuarioCreado.Should().NotBeNull();
            usuarioCreado!.PasswordHash.Should().Be("hashGenerado");
            usuarioCreado.NombreUsuario.Should().Be("nuevoUsuario");
        }
    }
}
