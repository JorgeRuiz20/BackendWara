using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WARA.Application.DTOs;
using WARA.Domain.Ports;

namespace WARA.API.Controllers
{
    /// <summary>
    /// EndPoints de autenticación y gestión de usuarios (Tabla Usuarios).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// POST api/auth/login
        /// Autentica a un usuario existente en la base de datos.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var resultado = await _authService.LoginAsync(request.NombreUsuario, request.Password);

            var response = new LoginResponseDto
            {
                EsExitoso = resultado.EsExitoso,
                Token = resultado.Token,
                NombreUsuario = resultado.NombreUsuario,
                Mensaje = resultado.EsExitoso ? "Inicio de sesión exitoso." : resultado.MensajeError
            };

            if (!resultado.EsExitoso)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// POST api/auth/register
        /// Registra un nuevo usuario del sistema. Es un endpoint público: el sistema
        /// no maneja roles ni jerarquía de usuarios (el requerimiento solo pide login
        /// contra la tabla Usuarios), así que cualquier persona puede crear su cuenta
        /// para luego loguearse y usar los endpoints protegidos de /api/trabajadores.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            await _authService.RegistrarAsync(request.NombreUsuario, request.Password);

            var response = new RegisterResponseDto
            {
                EsExitoso = true,
                NombreUsuario = request.NombreUsuario.Trim(),
                Mensaje = "Usuario registrado exitosamente."
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>
        /// GET api/auth/me
        /// Retorna los datos del usuario autenticado actual, leídos directamente de los
        /// claims del JWT (Sub = Id, UniqueName = NombreUsuario) — no consulta la base de
        /// datos. Útil para que la app móvil "salude" al usuario tras un login previo
        /// (token guardado localmente) sin tener que volver a pedir credenciales.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UsuarioActualDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ObtenerUsuarioActual()
        {
            var idClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var nombreUsuarioClaim = User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

            if (idClaim is null || nombreUsuarioClaim is null || !int.TryParse(idClaim, out var id))
            {
                // No debería ocurrir con un JWT válido emitido por esta misma API,
                // pero se maneja explícitamente en vez de asumir que los claims siempre existen.
                return Unauthorized(new { esExitoso = false, mensaje = "Token inválido o incompleto." });
            }

            var response = new UsuarioActualDto
            {
                Id = id,
                NombreUsuario = nombreUsuarioClaim
            };

            return Ok(response);
        }
    }
}
