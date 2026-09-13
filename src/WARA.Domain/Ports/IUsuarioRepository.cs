using System.Threading.Tasks;
using WARA.Domain.Entities;

namespace WARA.Domain.Ports
{
    /// <summary>
    /// Puerto de salida (Output Port) para el acceso a datos de Usuario.
    /// La capa de dominio define el contrato; la infraestructura lo implementa.
    /// </summary>
    public interface IUsuarioRepository
    {
        /// <summary>
        /// Busca un usuario activo por su nombre de usuario.
        /// </summary>
        Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario);

        /// <summary>
        /// Indica si ya existe un usuario (activo o no) con ese nombre de usuario.
        /// Usado por el seeding automático para no duplicar el admin al reiniciar la API.
        /// </summary>
        Task<bool> ExisteUsuarioAsync(string nombreUsuario);

        /// <summary>
        /// Crea un nuevo usuario. El PasswordHash ya debe venir hasheado
        /// (la capa de dominio/infraestructura nunca recibe passwords en texto plano para persistir).
        /// </summary>
        Task CrearAsync(Usuario usuario);
    }
}
