using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipping")]
    [Authorize(Roles = "compras-be, logistica-be")]
    public class ShippingQueryController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingQueryController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        // ---
        // MÉTODO 1: Implementa 'GET /shipping' (con filtros)
        // ---
        [HttpGet]
        [ProducesResponseType(typeof(ShippingListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShippingListResponse>> Get(
            [FromQuery(Name = "user_id")] int? userId,
            [FromQuery(Name = "status")] string? statusStr,
            [FromQuery(Name = "from_date")] DateOnly? fromDate,
            [FromQuery(Name = "to_date")] DateOnly? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            // 1. Parse tolerante del status
            ShippingStatus? status = null;
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                var snake = statusStr.Trim().Replace("-", "_").Replace(" ", "_");
                if (!Enum.TryParse<ShippingStatus>(snake, true, out var parsed))
                {
                    var parts = snake.Split('_', StringSplitOptions.RemoveEmptyEntries);
                    var pascal = string.Concat(parts.Select(p => char.ToUpper(p[0]) + p.Substring(1)));
                    if (!Enum.TryParse<ShippingStatus>(pascal, true, out parsed))
                        return BadRequest(new Error { code = "invalid_status", message = "status inválido." });
                    status = parsed;
                }
                else
                {
                    status = parsed;
                }
            }

            // 2. Validación de fechas
            if (fromDate.HasValue && toDate.HasValue && toDate < fromDate)
                return BadRequest(new Error { code = "invalid_range", message = "to_date debe ser >= from_date." });

            // 3. Delegación TOTAL al servicio
            var result = await _shippingService.List(userId, status, fromDate, toDate, page, limit);
            return Ok(result);
        }

        // ---
        // MÉTODO 2: Implementa 'GET /shipping/{shipping_id}'
        // ---
        [HttpGet("{shipping_id:int}")]
        [AllowAnonymous]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ShippingDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShippingDetailResponse>> GetById([FromRoute] int shipping_id)
        {
            // 1. Delegación TOTAL al servicio
            var responseDto = await _shippingService.GetByIdAsync(shipping_id);

            // 2. Chequeo de Nulo
            if (responseDto is null)
            {
                return NotFound(new Error
                {
                    code = "not_found",
                    message = $"Shipping {shipping_id} not found."
                });
            }

            // 3. Retorno del DTO
            return Ok(responseDto);
        }
    }
}
