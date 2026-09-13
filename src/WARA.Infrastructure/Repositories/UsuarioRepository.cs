using System.Data;
using System.Threading.Tasks;
using Dapper;
using WARA.Domain.Entities;
using WARA.Domain.Ports;
using WARA.Infrastructure.Persistence;

namespace WARA.Infrastructure.Repositories
{
    /// <summary>
    /// Adaptador de salida que implementa IUsuarioRepository usando ADO.NET (vía Dapper)
    /// contra procedimientos almacenados de SQL Server.
    /// </summary>
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public UsuarioRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@NombreUsuario", nombreUsuario, DbType.String);

            var usuario = await connection.QuerySingleOrDefaultAsync<Usuario>(
                sql: "sp_Usuario_ObtenerPorNombreUsuario",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return usuario;
        }

        public async Task<bool> ExisteUsuarioAsync(string nombreUsuario)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@NombreUsuario", nombreUsuario, DbType.String);
            parametros.Add("@Existe", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                sql: "sp_Usuario_ExisteNombreUsuario",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return parametros.Get<bool>("@Existe");
        }

        public async Task CrearAsync(Usuario usuario)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@NombreUsuario", usuario.NombreUsuario, DbType.String);
            parametros.Add("@PasswordHash", usuario.PasswordHash, DbType.String);

            await connection.ExecuteAsync(
                sql: "sp_Usuario_Crear",
                param: parametros,
                commandType: CommandType.StoredProcedure);
        }
    }
}
