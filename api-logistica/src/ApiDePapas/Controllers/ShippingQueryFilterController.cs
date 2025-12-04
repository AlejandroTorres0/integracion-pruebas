// En src/ApiDePapas/Controllers/ShippingQueryFilterController.cs (Reintroducido)
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipping/filter")]
    [Authorize(Roles = "compras-be, logistica-be")]
    public class ShippingQueryFilterController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingQueryFilterController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        // GET /shipping/filter?user_id=&status=&from_date=&to_date=&page=&limit=
        [HttpGet]
        [ProducesResponseType(typeof(ShippingListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ShippingListResponse>> Get(
            [FromQuery(Name = "user_id")] int? userId,
            [FromQuery(Name = "status")] string? statusStr,
            [FromQuery(Name = "from_date(YYYY-MM-DD)")] DateOnly? fromDate,
            [FromQuery(Name = "to_date(YYYY-MM-DD)")] DateOnly? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            // Parse tolerante del status (in_transit, in-transit, InTransit, etc.)
            ShippingStatus? status = null;
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                var snake = statusStr.Trim()
                                       .Replace("-", "_")
                                       .Replace(" ", "_");

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

            if (fromDate.HasValue && toDate.HasValue && toDate < fromDate)
                return BadRequest(new Error { code = "invalid_range", message = "to_date debe ser >= from_date." });

            // Usa el método List reintroducido en IShippingService
            var result = await _shippingService.List(userId, status, fromDate, toDate, page, limit);
            return Ok(result);
        }
    }
}