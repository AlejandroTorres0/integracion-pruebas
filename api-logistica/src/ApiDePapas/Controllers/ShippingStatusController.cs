using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipments")] // Ruta base para todo lo relacionado con shipments
    [Authorize(Roles = "logistica-be")] // Proteger todo el controlador para el rol "logistica"
    public class ShippingStatusController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingStatusController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        /// <summary>
        /// Actualiza el estado de un pedido específico.
        /// </summary>
        /// <param name="id">El ID del pedido a actualizar.</param>
        /// <param name="request">El cuerpo de la solicitud con el nuevo estado y un mensaje opcional.</param>
        /// <returns>Un resultado 200 OK si fue exitoso, o 404 Not Found si el pedido no existe.</returns>
        [HttpPut("{id}/status")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try 
            {
                var success = await _shippingService.UpdateStatusAsync(id, request);

                if (!success)
                {
                    return NotFound(new { message = $"Shipment with ID {id} not found." });
                }

                return Ok(new { message = $"Status for shipment {id} updated to {request.NewStatus}." });
            }
            catch (InvalidOperationException ex)
            {
                // Aquí capturamos las reglas de negocio (ej. intentar mover un Delivered)
                return Conflict(new { code = "invalid_state_transition", message = ex.Message });
            }
        }
    }
}
