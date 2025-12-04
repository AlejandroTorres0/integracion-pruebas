using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Application.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http; // Added for StatusCodes
using ApiDePapas.Domain.Entities;


namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(Roles = "logistica-be")] // Solo accesible para backend de logística
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("shipments")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PaginatedDashboardShipmentsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedDashboardShipmentsResponse>> GetDashboardShipments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? id = null,
            [FromQuery] string? city = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var shipments = await _dashboardService.GetDashboardShipmentsAsync(page, pageSize, id, city, status, startDate, endDate);
            var totalItems = await _dashboardService.GetTotalDashboardShipmentsCountAsync(id, city, status, startDate, endDate);

            var response = new PaginatedDashboardShipmentsResponse(
                shipments,
                new PaginationData(
                    total_items: totalItems,
                    total_pages: (int)Math.Ceiling((double)totalItems / pageSize),
                    current_page: page,
                    items_per_page: pageSize
                )
            );

            return Ok(response);
        }
        [HttpPatch("shipments/{id}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Error), StatusCodes.Status409Conflict)] // <--- 1. Nuevo: Documentamos el conflicto
        public async Task<IActionResult> UpdateShipmentStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new Error { code = "bad_request", message = "Datos inválidos" });

            try
            {
                // 2. Pasamos el mensaje al servicio
                // El servicio ya tiene la lógica para usar un mensaje default si este es null
                await _dashboardService.UpdateShipmentStatusAsync(id, request.NewStatus, request.Message);
                
                return NoContent(); 
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new Error { code = "not_found", message = ex.Message });
            }
            catch (InvalidOperationException ex) // <--- 3. Nuevo: Capturamos reglas de negocio
            {
                // Esto maneja: "No se puede cancelar un pedido entregado", etc.
                return Conflict(new Error { code = "conflict", message = ex.Message });
            }
        }
    }
}
