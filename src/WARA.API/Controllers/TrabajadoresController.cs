using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WARA.Application.DTOs;
using WARA.Domain.Entities;
using WARA.Domain.Ports;

namespace WARA.API.Controllers
{
    /// <summary>
    /// EndPoints para gestionar Trabajadores (Tabla Trabajadores): listar (paginado),
    /// obtener por Id, agregar, actualizar y dar de baja lógica.
    /// Requiere JWT válido (obtenido en /api/auth/login) en todos sus métodos:
    /// la gestión de trabajadores es información interna de la empresa y no
    /// debe quedar expuesta sin autenticación.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrabajadoresController : ControllerBase
    {
        private readonly ITrabajadorService _trabajadorService;

        public TrabajadoresController(ITrabajadorService trabajadorService)
        {
            _trabajadorService = trabajadorService;
        }

        /// <summary>
        /// GET api/trabajadores?dni=12345678&amp;pagina=1&amp;tamanoPagina=10
        /// Lista los trabajadores activos, opcionalmente filtrados por DNI, de forma paginada.
        /// Requisito original de la evaluación: filtro por DNI. La paginación es una
        /// extensión razonable para que el endpoint escale más allá de un puñado de registros.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResultadoPaginadoDto<TrabajadorDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar(
            [FromQuery] string? dni,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10)
        {
            var resultado = await _trabajadorService.ListarTrabajadoresAsync(dni, pagina, tamanoPagina);

            var response = new ResultadoPaginadoDto<TrabajadorDto>
            {
                Items = resultado.Items.Select(MapearADto),
                Pagina = pagina < 1 ? 1 : pagina,
                TamanoPagina = tamanoPagina < 1 ? 10 : tamanoPagina,
                TotalRegistros = resultado.TotalRegistros
            };

            return Ok(response);
        }

        /// <summary>
        /// GET api/trabajadores/{id}
        /// Obtiene un trabajador puntual por su Id.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TrabajadorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var trabajador = await _trabajadorService.ObtenerTrabajadorPorIdAsync(id);

            if (trabajador is null)
            {
                return NotFound(new { esExitoso = false, mensaje = $"No se encontró un trabajador activo con Id {id}." });
            }

            return Ok(MapearADto(trabajador));
        }

        /// <summary>
        /// POST api/trabajadores
        /// Agrega un nuevo trabajador. Retorna si la operación fue exitosa.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CrearTrabajadorResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(CrearTrabajadorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Agregar([FromBody] CrearTrabajadorRequestDto request)
        {
            var trabajadorCreado = await _trabajadorService.AgregarTrabajadorAsync(
                request.Nombre,
                request.Apellido,
                request.Dni,
                request.Edad);

            var response = new CrearTrabajadorResponseDto
            {
                EsExitoso = true,
                Mensaje = "Trabajador registrado exitosamente.",
                Trabajador = MapearADto(trabajadorCreado)
            };

            return CreatedAtAction(nameof(ObtenerPorId), new { id = trabajadorCreado.Id }, response);
        }

        /// <summary>
        /// PUT api/trabajadores/{id}
        /// Actualiza Nombre, Apellido y Edad de un trabajador existente.
        /// El DNI no es editable: se trata como identificador de negocio inmutable.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CrearTrabajadorResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(CrearTrabajadorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarTrabajadorRequestDto request)
        {
            var trabajadorActualizado = await _trabajadorService.ActualizarTrabajadorAsync(
                id,
                request.Nombre,
                request.Apellido,
                request.Edad);

            if (trabajadorActualizado is null)
            {
                return NotFound(new { esExitoso = false, mensaje = $"No se encontró un trabajador activo con Id {id}." });
            }

            var response = new CrearTrabajadorResponseDto
            {
                EsExitoso = true,
                Mensaje = "Trabajador actualizado exitosamente.",
                Trabajador = MapearADto(trabajadorActualizado)
            };

            return Ok(response);
        }

        /// <summary>
        /// DELETE api/trabajadores/{id}
        /// Da de baja lógica a un trabajador (Activo = 0). No elimina el registro físicamente,
        /// preservando el histórico para trazabilidad.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var eliminado = await _trabajadorService.EliminarTrabajadorAsync(id);

            if (!eliminado)
            {
                return NotFound(new { esExitoso = false, mensaje = $"No se encontró un trabajador activo con Id {id}." });
            }

            return NoContent();
        }

        private static TrabajadorDto MapearADto(Trabajador trabajador)
        {
            return new TrabajadorDto
            {
                Id = trabajador.Id,
                Nombre = trabajador.Nombre,
                Apellido = trabajador.Apellido,
                Dni = trabajador.Dni,
                Edad = trabajador.Edad
            };
        }
    }
}
