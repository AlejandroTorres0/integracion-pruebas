using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ApiDePapas.Application.Interfaces;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Services;

namespace ApiDePapas.Controllers
{
    // En ShippingCreateController.cs (versión refactorizada)   
    [ApiController]
    [Route("shipping")]
    [Authorize(Roles = "compras-be, logistica-be")]
    public class ShippingCreateController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingCreateController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [HttpPost]
        public async Task<ActionResult<CreateShippingResponse>> Post([FromBody] CreateShippingRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Error { code = "bad_request", message = "Malformed request body." });

            // Llamamos al servicio
            var createdShipping = await _shippingService.CreateNewShipping(req);

            // Comprobamos el resultado
            if (createdShipping == null)
            {
                // Si es null, hubo un error de validación de negocio
                return UnprocessableEntity(new Error
                {
                    code = "unprocessable_entity",
                    message = "Products list cannot be empty."
                });
            }

            // Si no es null, todo salió bien
            return Created($"/shipping/{createdShipping.shipping_id}", createdShipping);
        }
    }
}