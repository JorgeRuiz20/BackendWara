using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using WARA.Domain.Entities;
using WARA.Domain.Ports;
using WARA.Infrastructure.Persistence;

namespace WARA.Infrastructure.Repositories
{
    /// <summary>
    /// Adaptador de salida que implementa ITrabajadorRepository usando ADO.NET (vía Dapper)
    /// contra procedimientos almacenados de SQL Server.
    /// </summary>
    public class TrabajadorRepository : ITrabajadorRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public TrabajadorRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<TrabajadoresPaginados> ListarAsync(string? filtroDni, int pagina, int tamanoPagina)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@FiltroDni", string.IsNullOrWhiteSpace(filtroDni) ? null : filtroDni.Trim(), DbType.String);
            parametros.Add("@Pagina", pagina, DbType.Int32);
            parametros.Add("@TamanoPagina", tamanoPagina, DbType.Int32);

            // El SP devuelve dos result sets: la página de datos y el total de registros.
            using var multi = await connection.QueryMultipleAsync(
                sql: "sp_Trabajador_Listar",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            var items = await multi.ReadAsync<Trabajador>();
            var totalRegistros = await multi.ReadSingleAsync<int>();

            return new TrabajadoresPaginados
            {
                Items = items,
                TotalRegistros = totalRegistros
            };
        }

        public async Task<Trabajador?> ObtenerPorIdAsync(int id)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@Id", id, DbType.Int32);

            var trabajador = await connection.QuerySingleOrDefaultAsync<Trabajador>(
                sql: "sp_Trabajador_ObtenerPorId",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return trabajador;
        }

        public async Task<Trabajador> AgregarAsync(Trabajador trabajador)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@Nombre", trabajador.Nombre, DbType.String);
            parametros.Add("@Apellido", trabajador.Apellido, DbType.String);
            parametros.Add("@Dni", trabajador.Dni, DbType.String);
            parametros.Add("@Edad", trabajador.Edad, DbType.Int32);

            // El SP retorna el registro insertado completo (incluyendo el Id generado)
            var trabajadorCreado = await connection.QuerySingleAsync<Trabajador>(
                sql: "sp_Trabajador_Agregar",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return trabajadorCreado;
        }

        public async Task<Trabajador?> ActualizarAsync(Trabajador trabajador)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@Id", trabajador.Id, DbType.Int32);
            parametros.Add("@Nombre", trabajador.Nombre, DbType.String);
            parametros.Add("@Apellido", trabajador.Apellido, DbType.String);
            parametros.Add("@Edad", trabajador.Edad, DbType.Int32);

            // El SP retorna el registro actualizado; si el Id no existe o está inactivo, no retorna filas.
            var trabajadorActualizado = await connection.QuerySingleOrDefaultAsync<Trabajador>(
                sql: "sp_Trabajador_Actualizar",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return trabajadorActualizado;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@Id", id, DbType.Int32);

            var filasAfectadas = await connection.QuerySingleAsync<int>(
                sql: "sp_Trabajador_Eliminar",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return filasAfectadas > 0;
        }

        public async Task<bool> ExisteDniAsync(string dni)
        {
            using IDbConnection connection = _connectionFactory.CrearConexion();

            var parametros = new DynamicParameters();
            parametros.Add("@Dni", dni, DbType.String);
            parametros.Add("@Existe", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                sql: "sp_Trabajador_ExisteDni",
                param: parametros,
                commandType: CommandType.StoredProcedure);

            return parametros.Get<bool>("@Existe");
        }
    }
}
