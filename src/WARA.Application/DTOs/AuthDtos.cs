using System.ComponentModel.DataAnnotations;

namespace WARA.Application.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El usuario es obligatorio.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres, incluir al menos una letra mayúscula y un símbolo (ej. @, !).")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public bool EsExitoso { get; set; }
        public string? Token { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Mensaje { get; set; }
    }

    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(200, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres, incluir al menos una letra mayúscula y un símbolo (ej. @, !).")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterResponseDto
    {
        public bool EsExitoso { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Mensaje { get; set; }
    }

    /// <summary>
    /// DTO de salida de GET /api/auth/me. Se construye leyendo los claims del propio
    /// JWT ya validado por el middleware de autenticación — no requiere consultar la BD.
    /// </summary>
    public class UsuarioActualDto
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
    }
}
