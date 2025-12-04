using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Services;
using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Controllers
{
    [ApiController]
    [Route("shipping/transport-methods")]
    [Authorize(Roles = "compras-be, logistica-be")]
    public class ShippingTransportMethodsController : ControllerBase
    {
        private readonly TransportService _transportService;

        public ShippingTransportMethodsController(TransportService transportService)
        {
            _transportService = transportService;
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TransportMethodsResponse), StatusCodes.Status200OK)]
        public ActionResult<TransportMethodsResponse> Get()
        {
            var resp = _transportService.GetAll();
            return Ok(resp);
        }
    }
}
