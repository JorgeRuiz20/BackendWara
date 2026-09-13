using System.Collections.Generic;
using System.Threading.Tasks;
using WARA.Domain.Entities;

namespace WARA.Domain.Ports
{
    /// <summary>
    /// Puerto de entrada (Input Port) para los casos de uso de gestión de trabajadores.
    /// </summary>
    public interface ITrabajadorService
    {
        Task<TrabajadoresPaginados> ListarTrabajadoresAsync(string? filtroDni, int pagina, int tamanoPagina);

        Task<Trabajador?> ObtenerTrabajadorPorIdAsync(int id);

        Task<Trabajador> AgregarTrabajadorAsync(string nombre, string apellido, string dni, int edad);

        Task<Trabajador?> ActualizarTrabajadorAsync(int id, string nombre, string apellido, int edad);

        Task<bool> EliminarTrabajadorAsync(int id);
    }
}
