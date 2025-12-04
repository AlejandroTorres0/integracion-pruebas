// Controllers/OrderController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComprasAPI.Data;
using ComprasAPI.Models;
using ComprasAPI.Models.DTOs;
using ComprasAPI.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ComprasAPI.Controllers
{
    [ApiController]
    [Route("api/shopcart")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;
        private readonly ILogisticaService _logisticaService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            IStockService stockService,
            ILogisticaService logisticaService,
            ILogger<OrderController> logger)
        {
            _context = context;
            _stockService = stockService;
            _logisticaService = logisticaService;
            _logger = logger;
        }

        // POST: api/shopcart/checkout
        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(" Iniciando proceso de checkout...");

                var userId = 1;
                
                // 1. Obtener carrito del usuario
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                    return BadRequest(new { error = "Carrito vacío", code = "EMPTY_CART" });

                _logger.LogInformation($" Carrito obtenido: {cart.Items.Count} productos");

                // 2. ✅ PRIMERO: Crear reserva en Stock API
                _logger.LogInformation(" Creando reserva en Stock API...");
                var reservaInput = new ReservaInput
                {
                    IdCompra = Guid.NewGuid().ToString(),
                    UsuarioId = userId,
                    Productos = cart.Items.Select(item => new ProductoReserva
                    {
                        IdProducto = item.ProductId,
                        Cantidad = item.Quantity
                    }).ToList()
                };

                var reserva = await _stockService.CrearReservaAsync(reservaInput);

                // 3.  VALIDAR RESERVA ANTES DE CONTINUAR
                if (reserva == null || reserva.Estado != "confirmado")
                {
                    _logger.LogWarning($" Reserva falló. Estado: {reserva?.Estado ?? "null"}");

                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        error = "No se pudo reservar el stock",
                        code = "STOCK_RESERVATION_FAILED",
                        reservaStatus = reserva?.Estado ?? "error"
                    });
                }

                _logger.LogInformation($" Reserva confirmada: {reserva.IdReserva}");

                // 4.  SEGUNDO: Calcular costo de envío
                _logger.LogInformation(" Calculando costo de envío...");
                var shippingCostRequest = new ShippingCostRequest
                {
                    DeliveryAddress = request.DeliveryAddress,
                    Products = cart.Items.Select(item => new ProductRequest
                    {
                        Id = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                var shippingCost = await _logisticaService.CalcularCostoEnvioAsync(shippingCostRequest);
                _logger.LogInformation($" Costo de envío calculado: {shippingCost.TotalCost} {shippingCost.Currency}");

                // 5.  TERCERO: Crear envío en Logística
                _logger.LogInformation(" Creando envío en Logística API...");
                var shippingRequest = new CreateShippingRequest
                {
                    OrderId = 0, // Temporal, se actualizará después
                    UserId = userId,
                    DeliveryAddress = request.DeliveryAddress,
                    TransportType = request.TransportType ?? "road",
                    Products = cart.Items.Select(item => new ProductRequest
                    {
                        Id = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                var shipping = await _logisticaService.CrearEnvioAsync(shippingRequest);

                if (shipping == null || shipping.ShippingId <= 0)
                {
                    _logger.LogWarning(" Envío falló en Logística API");

                    //  IMPORTANTE: Cancelar la reserva si el envío falla
                    await _stockService.CancelarReservaAsync(reserva.IdReserva, userId);

                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        error = "No se pudo crear el envío",
                        code = "SHIPPING_CREATION_FAILED"
                    });
                }

                _logger.LogInformation($" Envío creado: {shipping.ShippingId}");


                // 7.  QUINTO: Vaciar carrito
                _context.CartItems.RemoveRange(cart.Items);
                cart.Items.Clear();
                cart.Total = 0;
                await _context.SaveChangesAsync();

                //  CONFIRMAR TRANSACCIÓN
                await transaction.CommitAsync();

                _logger.LogInformation(" Checkout completado exitosamente");

                return Ok(new CheckoutResponse
                {
                    //OrderId = order.Id,
                    ReservaId = reserva.IdReserva,
                    ShippingId = shipping.ShippingId,
                    //Total = order.Total,
                    ShippingCost = shippingCost.TotalCost,
                    EstimatedDelivery = shipping.EstimatedDeliveryAt,
                    ReservaStatus = reserva.Estado, // ← Incluir estado de la reserva
                    Message = "Pedido creado exitosamente"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, " Error durante el checkout");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor durante el checkout",
                    code = "CHECKOUT_ERROR"
                });
            }
        
        }

        // GET: api/shopcart/transport-methods
        [HttpGet("transport-methods")]
        public async Task<IActionResult> GetTransportMethods()
        {
            try
            {
                _logger.LogInformation(" Obteniendo métodos de transporte...");
                var methods = await _logisticaService.ObtenerMetodosTransporteAsync();
                return Ok(methods);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error obteniendo métodos de transporte");
                return StatusCode(500, new
                {
                    error = "Error obteniendo métodos de transporte",
                    code = "TRANSPORT_METHODS_ERROR"
                });
            }
        }

        // GET: api/shopcart/history
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetOrderHistory()
        {
            try
            {
                var userId = await GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });

                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .Where(o => o.UserId == userId.Value)
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error obteniendo historial de pedidos");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // GET: api/shopcart/history/{id}
        [HttpGet("history/{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            try
            {
                var userId = await GetCurrentUserId();
                if (userId == null)
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });

                var order = await _context.Orders
                    .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId.Value);

                if (order == null)
                    return NotFound(new { error = "Pedido no encontrado", code = "ORDER_NOT_FOUND" });

                // Obtener información de seguimiento si existe shippingId
                if (order.ShippingId.HasValue)
                {
                    try
                    {
                        var tracking = await _logisticaService.ObtenerSeguimientoAsync(order.ShippingId.Value);
                        order.TrackingInfo = System.Text.Json.JsonSerializer.Serialize(tracking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, " No se pudo obtener información de seguimiento");
                    }
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $" Error obteniendo detalles del pedido {id}");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        private async Task<int?> GetCurrentUserId()
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                           ?? User.FindFirst("email")?.Value
                           ?? User.FindFirst("preferred_username")?.Value;

                if (string.IsNullOrEmpty(email))
                    return null;

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = email,
                        FirstName = User.FindFirst(ClaimTypes.GivenName)?.Value ?? "Usuario",
                        LastName = User.FindFirst(ClaimTypes.Surname)?.Value ?? "Keycloak",
                        PasswordHash = "keycloak_user",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al obtener userId");
                return 1; // Fallback para desarrollo
            }
        }
    }

    // DTOs para checkout
    public class CheckoutRequest
    {
        public Address DeliveryAddress { get; set; }
        public string TransportType { get; set; }
    }

    public class CheckoutResponse
    {
        //public int OrderId { get; set; }
        public int ReservaId { get; set; }
        public int ShippingId { get; set; }
        //public decimal Total { get; set; }
        public decimal ShippingCost { get; set; }
        public string EstimatedDelivery { get; set; }
        public string Message { get; set; }
        public string ReservaStatus { get; set; }
    }
}