// ApiDePapas/Controllers/ShippingCancelController.cs (Volviendo a la lógica original)
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipping")]
    [Authorize(Roles = "compras-be, stock-be, logistica-be")]
    public class ShippingCancelController : ControllerBase
    {
        // Revertimos la inyección a IShippingService
        private readonly IShippingService _service; 

        public ShippingCancelController(IShippingService service)
        {
            _service = service;
        }

        [HttpPost("{id:int}/cancel")] // Usamos POST, como sugería el YAML
        [Produces("application/json")]
        [ProducesResponseType(typeof(CancelShippingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)]
        // Usamos async Task y GetAsync/CancelAsync de IShippingService reintroducido
        public async Task<ActionResult<CancelShippingResponse>> Cancel([FromRoute] int id)
        {
            // 1) buscar en DB (Usando el método reintroducido)
            var shipping = await _service.GetByIdAsync(id); 
            if (shipping is null)
            {
                return NotFound(new Error
                {
                    code = "not_found",
                    message = $"Shipping {id} not found."
                });
            }

            // 2) validar estado
            if (shipping.status is ShippingStatus.delivered or ShippingStatus.cancelled)
            {
                return Conflict(new Error
                {
                    code = "conflict",
                    message = $"Shipping {id} cannot be cancelled in state '{shipping.status}'."
                });
            }

            // 3) cancelar (DB) (Usando el método reintroducido)
            var resp = await _service.CancelAsync(id, DateTime.UtcNow);
            return Ok(resp);
        }
    }
}