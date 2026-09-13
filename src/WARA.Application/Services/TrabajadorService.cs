using System.Collections.Generic;
using System.Threading.Tasks;
using WARA.Application.Exceptions;
using WARA.Domain.Entities;
using WARA.Domain.Ports;

namespace WARA.Application.Services
{
    /// <summary>
    /// Implementación de los casos de uso de gestión de trabajadores.
    /// Contiene las reglas de negocio: validación de campos y unicidad de DNI.
    /// </summary>
    public class TrabajadorService : ITrabajadorService
    {
        private readonly ITrabajadorRepository _trabajadorRepository;

        public TrabajadorService(ITrabajadorRepository trabajadorRepository)
        {
            _trabajadorRepository = trabajadorRepository;
        }

        public async Task<TrabajadoresPaginados> ListarTrabajadoresAsync(string? filtroDni, int pagina, int tamanoPagina)
        {
            if (pagina < 1) pagina = 1;
            if (tamanoPagina < 1) tamanoPagina = 10;
            if (tamanoPagina > 100) tamanoPagina = 100;

            return await _trabajadorRepository.ListarAsync(filtroDni, pagina, tamanoPagina);
        }

        public async Task<Trabajador?> ObtenerTrabajadorPorIdAsync(int id)
        {
            return await _trabajadorRepository.ObtenerPorIdAsync(id);
        }

        public async Task<Trabajador> AgregarTrabajadorAsync(string nombre, string apellido, string dni, int edad)
        {
            ValidarCampos(nombre, apellido, dni, edad);

            var existeDni = await _trabajadorRepository.ExisteDniAsync(dni);
            if (existeDni)
            {
                throw new BusinessRuleException($"Ya existe un trabajador registrado con el DNI '{dni}'.");
            }

            var trabajador = new Trabajador
            {
                Nombre = nombre.Trim(),
                Apellido = apellido.Trim(),
                Dni = dni.Trim(),
                Edad = edad
            };

            return await _trabajadorRepository.AgregarAsync(trabajador);
        }

        public async Task<Trabajador?> ActualizarTrabajadorAsync(int id, string nombre, string apellido, int edad)
        {
            ValidarCamposEdicion(nombre, apellido, edad);

            var trabajador = new Trabajador
            {
                Id = id,
                Nombre = nombre.Trim(),
                Apellido = apellido.Trim(),
                Edad = edad
            };

            return await _trabajadorRepository.ActualizarAsync(trabajador);
        }

        public async Task<bool> EliminarTrabajadorAsync(int id)
        {
            return await _trabajadorRepository.EliminarAsync(id);
        }

        private static void ValidarCampos(string nombre, string apellido, string dni, int edad)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new BusinessRuleException("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(apellido))
                throw new BusinessRuleException("El apellido es obligatorio.");

            if (string.IsNullOrWhiteSpace(dni))
                throw new BusinessRuleException("El DNI es obligatorio.");

            if (dni.Trim().Length != 8 || !long.TryParse(dni.Trim(), out _))
                throw new BusinessRuleException("El DNI debe tener 8 dígitos numéricos.");

            if (edad < 18 || edad > 80)
                throw new BusinessRuleException("La edad debe estar entre 18 y 80 años.");
        }

        private static void ValidarCamposEdicion(string nombre, string apellido, int edad)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new BusinessRuleException("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(apellido))
                throw new BusinessRuleException("El apellido es obligatorio.");

            if (edad < 18 || edad > 80)
                throw new BusinessRuleException("La edad debe estar entre 18 y 80 años.");
        }
    }
}
