namespace WARA.Domain.Entities
{
    /// <summary>
    /// Entidad de dominio que representa a un usuario del sistema.
    /// No conoce nada de base de datos, HTTP ni ningún detalle de infraestructura.
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
