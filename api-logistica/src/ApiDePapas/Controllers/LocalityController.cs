using Microsoft.AspNetCore.Mvc;
using ApiDePapas.Domain.Repositories;
using ApiDePapas.Domain.Entities;
using System.Threading.Tasks;

namespace ApiDePapas.Controllers
{
    // Nombre del Controller y Ruta Base
    [ApiController]
    [Route("api/[controller]")]
    public class LocalityController : ControllerBase
    {
        // Inyectamos la interfaz del Repositorio
        private readonly ILocalityRepository _locality_repository;

        // Inyección de dependencias (constructor)
        public LocalityController(ILocalityRepository localityRepository)
        {
            _locality_repository = localityRepository;
        }

        /// <summary>
        /// Búsqueda flexible de localidades.
        /// </summary>
        /// <remarks>
        /// Comportamiento:
        /// - Si viene <c>postal_code</c> y <c>locality_name</c> → devuelve 1 localidad (detalle completo).
        /// - Si viene solo <c>state</c> → lista de localidades (postal_code, locality_name, state).
        /// - Si viene <c>state</c> + <c>locality_name</c> (sin postal) → lista filtrada por nombre y estado.
        /// - Si viene solo <c>locality_name</c> → lista de coincidencias por nombre (todos los estados).
        /// - Si viene solo <c>postal_code</c> → lista de localidades con ese CP.
        /// - Si no viene ningún filtro → 400.
        /// </remarks>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Locality), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocality(
            [FromQuery] string? postal_code,
            [FromQuery] string? locality_name,
            [FromQuery] string? state,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            // 1) Caso clave compuesta → 1 resultado o 404 (tu caso original)
            if (!string.IsNullOrWhiteSpace(postal_code) && !string.IsNullOrWhiteSpace(locality_name))
            {
                var one = await _locality_repository.GetByCompositeKeyAsync(postal_code, locality_name);
                if (one is null)
                    return NotFound(new {
                        code = "not_found",
                        message = $"Localidad con código postal '{postal_code}' y nombre '{locality_name}' no encontrada."
                    });

                return Ok(one);
            }

            // 2) Cualquier otra combinación → lista
            if (string.IsNullOrWhiteSpace(postal_code) &&
                string.IsNullOrWhiteSpace(locality_name) &&
                string.IsNullOrWhiteSpace(state))
            {
                return BadRequest(new {
                    code = "missing_parameters",
                    message = "Debe enviar al menos uno: 'state', 'locality_name' o 'postal_code'."
                });
            }

            var list = await _locality_repository.SearchAsync(state, locality_name, postal_code, page, limit);

            // Si la consulta fue “solo state”, devuelvo forma liviana; si no, devuelvo tal cual
            bool onlyState = !string.IsNullOrWhiteSpace(state)
                            && string.IsNullOrWhiteSpace(locality_name)
                            && string.IsNullOrWhiteSpace(postal_code);

            if (onlyState)
            {
                var light = list.Select(l => new {
                    postal_code   = l.postal_code,
                    locality_name = l.locality_name,
                    state         = l.state_name
                });
                return Ok(light);
            }

            // Resto de casos (state+locality, solo locality, solo postal) → devuelvo la entidad completa
            return Ok(list);
        }
        /// <summary>
        /// Obtiene todas las localidades.
        /// </summary>
        /// <returns>Una lista de todas las localidades.</returns>
        [HttpGet("getall")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<Locality>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLocalities()
        {
            var localities = await _locality_repository.GetAllAsync();
            return Ok(localities);
        }
    }
}