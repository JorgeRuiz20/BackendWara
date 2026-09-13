using WARA.Domain.Ports;

namespace WARA.Infrastructure.Security
{
    /// <summary>
    /// Adaptador de salida que implementa IPasswordHasher usando el algoritmo BCrypt.
    /// </summary>
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hashear(string passwordPlano)
        {
            return BCrypt.Net.BCrypt.HashPassword(passwordPlano);
        }

        public bool Verificar(string passwordPlano, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(passwordPlano, passwordHash);
        }
    }
}
