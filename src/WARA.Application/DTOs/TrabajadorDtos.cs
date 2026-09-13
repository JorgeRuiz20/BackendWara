using System.ComponentModel.DataAnnotations;

namespace WARA.Application.DTOs
{
    /// <summary>
    /// DTO de salida: representa un trabajador tal como se expone al cliente (app móvil).
    /// </summary>
    public class TrabajadorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public int Edad { get; set; }
    }

    /// <summary>
    /// DTO de entrada: datos que llegan desde el formulario de "Agregar Trabajador".
    /// Las DataAnnotations permiten que ASP.NET rechace el request con 400 y el
    /// detalle de ModelState ANTES de que el Service reciba el dato (defensa en profundidad:
    /// la validación de negocio en TrabajadorService.ValidarCampos se mantiene igual como
    /// segunda capa, por si el DTO se construye desde otro punto que no pase por el binding).
    /// </summary>
    public class CrearTrabajadorRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El DNI es obligatorio.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe tener exactamente 8 dígitos numéricos.")]
        public string Dni { get; set; } = string.Empty;

        [Range(18, 80, ErrorMessage = "La edad debe estar entre 18 y 80 años.")]
        public int Edad { get; set; }
    }

    /// <summary>
    /// DTO de entrada para PUT /api/trabajadores/{id}. El DNI no se incluye a propósito:
    /// se trata como identificador de negocio inmutable una vez creado el registro.
    /// </summary>
    public class ActualizarTrabajadorRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Range(18, 80, ErrorMessage = "La edad debe estar entre 18 y 80 años.")]
        public int Edad { get; set; }
    }

    /// <summary>
    /// Respuesta genérica que confirma si la operación de creación fue exitosa,
    /// tal como lo pide el requerimiento: "retornar si se creó exitosamente".
    /// Reutilizada también para la respuesta de actualización.
    /// </summary>
    public class CrearTrabajadorResponseDto
    {
        public bool EsExitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public TrabajadorDto? Trabajador { get; set; }
    }

    /// <summary>
    /// Envoltura de paginación genérica para GET /api/trabajadores.
    /// </summary>
    public class ResultadoPaginadoDto<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
    }
}
