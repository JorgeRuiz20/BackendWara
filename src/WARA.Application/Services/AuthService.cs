using System.Threading.Tasks;
using WARA.Application.Exceptions;
using WARA.Domain.Entities;
using WARA.Domain.Ports;

namespace WARA.Application.Services
{
    /// <summary>
    /// Implementación del caso de uso de autenticación.
    /// Depende únicamente de puertos (interfaces) del dominio, nunca de infraestructura concreta.
    /// </summary>
    public class AuthService : IAuthService
    {
        private const int LongitudMinimaPassword = 8;

        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            IPasswordHasher passwordHasher,
            ITokenGenerator tokenGenerator)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<LoginResult> LoginAsync(string nombreUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(password))
            {
                return LoginResult.Fallido("El usuario y la contraseña son obligatorios.");
            }

            var usuario = await _usuarioRepository.ObtenerPorNombreUsuarioAsync(nombreUsuario);

            if (usuario is null || !usuario.Activo)
            {
                return LoginResult.Fallido("Credenciales inválidas.");
            }

            var passwordValido = _passwordHasher.Verificar(password, usuario.PasswordHash);

            if (!passwordValido)
            {
                return LoginResult.Fallido("Credenciales inválidas.");
            }

            var token = _tokenGenerator.GenerarToken(usuario.Id, usuario.NombreUsuario);

            return LoginResult.Exitoso(token, usuario.NombreUsuario);
        }

        public async Task RegistrarAsync(string nombreUsuario, string password)
        {
            ValidarDatosRegistro(nombreUsuario, password);

            var nombreNormalizado = nombreUsuario.Trim();

            var yaExiste = await _usuarioRepository.ExisteUsuarioAsync(nombreNormalizado);
            if (yaExiste)
            {
                throw new BusinessRuleException($"Ya existe un usuario registrado con el nombre '{nombreNormalizado}'.");
            }

            var usuario = new Usuario
            {
                NombreUsuario = nombreNormalizado,
                PasswordHash = _passwordHasher.Hashear(password),
                Activo = true
            };

            await _usuarioRepository.CrearAsync(usuario);
        }

        private static void ValidarDatosRegistro(string nombreUsuario, string password)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                throw new BusinessRuleException("El nombre de usuario es obligatorio.");

            if (nombreUsuario.Trim().Length < 3)
                throw new BusinessRuleException("El nombre de usuario debe tener al menos 3 caracteres.");

            if (string.IsNullOrWhiteSpace(password))
                throw new BusinessRuleException("La contraseña es obligatoria.");

            if (password.Length < LongitudMinimaPassword)
                throw new BusinessRuleException($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");
        }
    }
}
