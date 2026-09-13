using System.Collections.Generic;
using System.Threading.Tasks;
using WARA.Domain.Entities;

namespace WARA.Domain.Ports
{
    /// <summary>
    /// Resultado de una consulta paginada de trabajadores.
    /// </summary>
    public class TrabajadoresPaginados
    {
        public IEnumerable<Trabajador> Items { get; set; } = new List<Trabajador>();
        public int TotalRegistros { get; set; }
    }

    /// <summary>
    /// Puerto de salida (Output Port) para el acceso a datos de Trabajador.
    /// </summary>
    public interface ITrabajadorRepository
    {
        /// <summary>
        /// Lista trabajadores paginados. Si se proporciona un DNI (o parte de él), filtra por ese criterio.
        /// </summary>
        Task<TrabajadoresPaginados> ListarAsync(string? filtroDni, int pagina, int tamanoPagina);

        /// <summary>
        /// Obtiene un trabajador activo por su Id. Retorna null si no existe o está inactivo.
        /// </summary>
        Task<Trabajador?> ObtenerPorIdAsync(int id);

        /// <summary>
        /// Inserta un nuevo trabajador y retorna el trabajador creado (con su Id generado).
        /// </summary>
        Task<Trabajador> AgregarAsync(Trabajador trabajador);

        /// <summary>
        /// Actualiza los datos editables de un trabajador activo. Retorna null si no existe o está inactivo.
        /// </summary>
        Task<Trabajador?> ActualizarAsync(Trabajador trabajador);

        /// <summary>
        /// Da de baja lógica a un trabajador (Activo = 0). Retorna true si afectó algún registro.
        /// </summary>
        Task<bool> EliminarAsync(int id);

        /// <summary>
        /// Verifica si ya existe un trabajador activo con el DNI indicado.
        /// </summary>
        Task<bool> ExisteDniAsync(string dni);
    }
}
