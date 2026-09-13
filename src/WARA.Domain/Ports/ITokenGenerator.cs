namespace WARA.Domain.Ports
{
    /// <summary>
    /// Puerto de salida para la generación de tokens de sesión (ej: JWT).
    /// </summary>
    public interface ITokenGenerator
    {
        string GenerarToken(int usuarioId, string nombreUsuario);
    }
}
