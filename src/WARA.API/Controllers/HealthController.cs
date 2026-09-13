using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WARA.API.Controllers
{
    /// <summary>
    /// Endpoint público de comprobación de estado y disponibilidad (Health Check).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Comprueba que la API esté activa y respondiendo peticiones HTTP.
        /// </summary>
        /// <returns>Estado HTTP 200 OK con metadatos del servicio.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(new
            {
                estado = "OK",
                mensaje = "WARA API operativa",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
