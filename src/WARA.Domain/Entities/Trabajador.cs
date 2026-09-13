namespace WARA.Domain.Entities
{
    /// <summary>
    /// Entidad de dominio que representa a un trabajador de la empresa WARA.
    /// </summary>
    public class Trabajador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public int Edad { get; set; }
    }
}
