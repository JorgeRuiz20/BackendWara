namespace WARA.Domain.Ports
{
    /// <summary>
    /// Puerto de salida para el hashing y verificación de contraseñas.
    /// El dominio no sabe (ni le importa) si esto se implementa con BCrypt, PBKDF2, etc.
    /// </summary>
    public interface IPasswordHasher
    {
        string Hashear(string passwordPlano);

        bool Verificar(string passwordPlano, string passwordHash);
    }
}
