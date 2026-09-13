using System.Threading.Tasks;

namespace WARA.Domain.Ports
{
    /// <summary>
    /// Puerto de entrada (Input Port) para el caso de uso de autenticación.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Intenta autenticar a un usuario. Retorna el resultado de login con token si es válido.
        /// </summary>
        Task<LoginResult> LoginAsync(string nombreUsuario, string password);

        /// <summary>
        /// Registra un nuevo usuario del sistema. Lanza BusinessRuleException si el
        /// nombre de usuario ya existe o si los datos no cumplen las reglas mínimas.
        /// </summary>
        Task RegistrarAsync(string nombreUsuario, string password);
    }

    /// <summary>
    /// Resultado del caso de uso de login. Vive en el dominio porque es parte
    /// del contrato del caso de uso, no un detalle de infraestructura.
    /// </summary>
    public class LoginResult
    {
        public bool EsExitoso { get; private set; }
        public string? Token { get; private set; }
        public string? NombreUsuario { get; private set; }
        public string? MensajeError { get; private set; }

        public static LoginResult Exitoso(string token, string nombreUsuario) => new LoginResult
        {
            EsExitoso = true,
            Token = token,
            NombreUsuario = nombreUsuario
        };

        public static LoginResult Fallido(string mensajeError) => new LoginResult
        {
            EsExitoso = false,
            MensajeError = mensajeError
        };
    }
}
