using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Application.Services;
using ApiDePapas.Application.DTOs;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipping/cost")]
    [Authorize(Roles = "compras-be, logistica-be")]
    public class ShippingCostController : ControllerBase
    {
        private readonly ICalculateCost _calculateCost;
        public ShippingCostController(ICalculateCost calculateCost) => _calculateCost = calculateCost;

        [HttpPost]
        [Produces("application/json")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ShippingCostResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShippingCostResponse>> Post([FromBody] ShippingCostRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Error { code = "bad_request", message = "Malformed request body." });

            var costReq = new CalculateCostRequest
            (
                request.delivery_address,
                null, // Si es null, usa 'road'.
                request.products
            );
            
            var cost = await _calculateCost.CalculateShippingCostAsync(costReq);

            return Ok(cost);
        }
    }
}
