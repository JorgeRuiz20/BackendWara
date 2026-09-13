using System;

namespace WARA.Application.Exceptions
{
    /// <summary>
    /// Excepción lanzada cuando se viola una regla de negocio (ej: DNI duplicado,
    /// credenciales inválidas, datos inconsistentes). Se distingue de errores técnicos
    /// para que la capa API pueda responder con el código HTTP adecuado (400/401/409)
    /// en lugar de un 500 genérico.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Excepción específica para fallos de autenticación (credenciales inválidas).
    /// </summary>
    public class AutenticacionException : Exception
    {
        public AutenticacionException(string message) : base(message)
        {
        }
    }
}
