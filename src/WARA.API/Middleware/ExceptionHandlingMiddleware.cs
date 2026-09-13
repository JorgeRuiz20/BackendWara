using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WARA.Application.Exceptions;

namespace WARA.API.Middleware
{
    /// <summary>
    /// Middleware que centraliza el manejo de excepciones para toda la API.
    /// Traduce excepciones de negocio a respuestas HTTP consistentes,
    /// y evita filtrar detalles técnicos al cliente en caso de errores inesperados.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessRuleException ex)
            {
                await EscribirRespuestaAsync(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (AutenticacionException ex)
            {
                await EscribirRespuestaAsync(context, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado al procesar la solicitud.");
                await EscribirRespuestaAsync(context, HttpStatusCode.InternalServerError,
                    "Ocurrió un error inesperado. Intente nuevamente más tarde.");
            }
        }

        private static async Task EscribirRespuestaAsync(HttpContext context, HttpStatusCode statusCode, string mensaje)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = JsonSerializer.Serialize(new
            {
                esExitoso = false,
                mensaje
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
