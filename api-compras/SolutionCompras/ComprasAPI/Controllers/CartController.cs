using ComprasAPI.Data;
using ComprasAPI.Models;
using ComprasAPI.Models.DTOs;
using ComprasAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ComprasAPI.Controllers
{
    [ApiController]
    [Route("api/shopcart")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;
        private readonly ILogger<CartController> _logger;
        private readonly ILogisticaService _logisticaService;

        public CartController(ApplicationDbContext context, IStockService stockService, ILogger<CartController> logger, ILogisticaService logisticaService)
        {
            _context = context;
            _stockService = stockService;
            _logger = logger;
            _logisticaService = logisticaService;
        }

        // GET: api/shopcart
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                _logger.LogInformation("Obteniendo carrito del usuario...");

                var userId = await GetCurrentUserId();
                _logger.LogInformation($"UserId obtenido: {userId}");

                if (userId == null)
                {
                    _logger.LogWarning("UserId es null - Usuario no autorizado o no encontrado en BD local");
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });
                }

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    _logger.LogInformation("Creando carrito vacío para nuevo usuario");
                    // Crear carrito vacío si no existe
                    cart = new Cart { UserId = userId.Value, Items = new List<CartItem>() };
                    return Ok(new CartDto
                    {
                        Id = 0,
                        Total = 0,
                        UserId = userId.Value,
                        Items = new List<CartItemDto>()
                    });
                }

                // Calcular total actualizado
                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);
                _logger.LogInformation($"Carrito obtenido: {cart.Items.Count} items, Total: {cart.Total}");

                // USAR DTO PARA EVITAR CICLOS
                var cartDto = new CartDto
                {
                    Id = cart.Id,
                    Total = cart.Total,
                    UserId = cart.UserId,
                    Items = cart.Items.Select(item => new CartItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Product = new ProductDto
                        {
                            Id = item.Product.Id,
                            Name = item.Product.Name,
                            Description = item.Product.Description,
                            Price = item.Product.Price,
                            Stock = item.Product.Stock,
                            Category = item.Product.Category
                        }
                    }).ToList()
                };

                return Ok(cartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener carrito");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // POST: api/shopcart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando AddToCart...");

                var userId = 1;
                _logger.LogInformation($"UserId obtenido: {userId}");

                _logger.LogInformation($"Usuario autenticado: {userId}");

                // 1. Verificar que el producto existe en Stock API
                _logger.LogInformation($"Verificando producto {request.ProductId} en Stock API...");
                var stockProduct = await _stockService.GetProductByIdAsync(request.ProductId);
                if (stockProduct == null)
                {
                    _logger.LogWarning($"Producto {request.ProductId} no encontrado en Stock API");
                    return NotFound(new { error = "Producto no encontrado", code = "PRODUCT_NOT_FOUND" });
                }

                // 2. Verificar stock disponible
                if (stockProduct.StockDisponible < request.Quantity)
                {
                    _logger.LogWarning($"Stock insuficiente. Solicitado: {request.Quantity}, Disponible: {stockProduct.StockDisponible}");
                    return BadRequest(new
                    {
                        error = "Stock insuficiente",
                        code = "INSUFFICIENT_STOCK",
                        available = stockProduct.StockDisponible
                    });
                }

                _logger.LogInformation($"Stock disponible: {stockProduct.StockDisponible}");

                // 3. Obtener o crear carrito
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync(); // Guardar para obtener ID
                    _logger.LogInformation($" Carrito creado: {cart.Id}");
                }
                else
                {
                    _logger.LogInformation($" Carrito existente: {cart.Id} con {cart.Items.Count} items");
                }

                // 4. Buscar o crear producto local (snapshot)
                var localProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId);

                if (localProduct == null)
                {
                    // Crear snapshot del producto en nuestra base local
                    localProduct = new Product
                    {
                        Id = stockProduct.Id,
                        Name = stockProduct.Nombre,
                        Description = stockProduct.Descripcion,
                        Price = stockProduct.Precio,
                        Stock = stockProduct.StockDisponible,
                        Category = stockProduct.Categorias?.FirstOrDefault()?.Nombre ?? "General"
                    };
                    _context.Products.Add(localProduct);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($" Producto local creado: {localProduct.Name} (ID: {localProduct.Id})");
                }

                // 5. Agregar o actualizar item en carrito
                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    _logger.LogInformation($" Producto actualizado: {existingItem.Product.Name} - Cantidad: {existingItem.Quantity}");
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        Product = localProduct
                    };
                    cart.Items.Add(cartItem);
                    _logger.LogInformation($" Producto agregado: {localProduct.Name} - Cantidad: {request.Quantity}");
                }

                // 6. Actualizar total del carrito
                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);

                await _context.SaveChangesAsync();

                _logger.LogInformation($" Carrito guardado. Total: {cart.Total}, Items: {cart.Items.Count}");

                return Ok(new
                {
                    message = "Producto agregado al carrito",
                    cartId = cart.Id,
                    total = cart.Total,
                    itemsCount = cart.Items.Count
                });
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                _logger.LogError(ex, " Error al conectar con Stock API");
                return StatusCode(502, new
                {
                    error = "Servicio Stock no disponible",
                    code = "STOCK_SERVICE_UNAVAILABLE"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al agregar producto al carrito");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // PUT: api/shopcart
        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartRequest request)
        {
            try
            {
                _logger.LogInformation(" Actualizando item del carrito...");

                var userId = await GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning(" UserId es null - Usuario no autorizado");
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });
                }

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    _logger.LogWarning(" Carrito no encontrado");
                    return NotFound(new { error = "Carrito no encontrado", code = "CART_NOT_FOUND" });
                }

                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (cartItem == null)
                {
                    _logger.LogWarning($" Producto {request.ProductId} no encontrado en el carrito");
                    return NotFound(new { error = "Producto no encontrado en el carrito", code = "CART_ITEM_NOT_FOUND" });
                }

                // Verificar stock si se aumenta la cantidad
                if (request.Quantity > cartItem.Quantity)
                {
                    var stockProduct = await _stockService.GetProductByIdAsync(request.ProductId);
                    if (stockProduct.StockDisponible < request.Quantity)
                    {
                        _logger.LogWarning($" Stock insuficiente al actualizar. Solicitado: {request.Quantity}, Disponible: {stockProduct.StockDisponible}");
                        return BadRequest(new
                        {
                            error = "Stock insuficiente",
                            code = "INSUFFICIENT_STOCK",
                            available = stockProduct.StockDisponible
                        });
                    }
                }

                if (request.Quantity <= 0)
                {
                    // Eliminar item si cantidad es 0 o negativa
                    cart.Items.Remove(cartItem);
                    _context.CartItems.Remove(cartItem);
                    _logger.LogInformation($" Producto {cartItem.Product.Name} removido del carrito");
                }
                else
                {
                    cartItem.Quantity = request.Quantity;
                    _logger.LogInformation($" Producto {cartItem.Product.Name} actualizado a {request.Quantity} unidades");
                }

                // Actualizar total
                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);

                await _context.SaveChangesAsync();

                _logger.LogInformation($" Carrito actualizado. Total: {cart.Total}");

                return Ok(new { message = "Carrito actualizado", total = cart.Total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al actualizar carrito");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // DELETE: api/shopcart/{productId}
        [HttpDelete("{productId}")]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            try
            {
                _logger.LogInformation($" Removiendo producto {productId} del carrito...");

                var userId = await GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning(" UserId es null - Usuario no autorizado");
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });
                }

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    _logger.LogWarning(" Carrito no encontrado");
                    return NotFound(new { error = "Carrito no encontrado", code = "CART_NOT_FOUND" });
                }

                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (cartItem == null)
                {
                    _logger.LogWarning($" Producto {productId} no encontrado en el carrito");
                    return NotFound(new { error = "Producto no encontrado en el carrito", code = "CART_ITEM_NOT_FOUND" });
                }

                cart.Items.Remove(cartItem);
                _context.CartItems.Remove(cartItem);

                // Actualizar total
                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);

                await _context.SaveChangesAsync();

                _logger.LogInformation($" Producto {productId} removido. Nuevo total: {cart.Total}");

                return Ok(new { message = "Producto removido del carrito", total = cart.Total });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al remover producto del carrito");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // DELETE: api/shopcart
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                _logger.LogInformation(" Vaciando carrito...");

                var userId = await GetCurrentUserId();
                if (userId == null)
                {
                    _logger.LogWarning(" UserId es null - Usuario no autorizado");
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });
                }

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                {
                    _logger.LogInformation(" Carrito ya está vacío");
                    return Ok(new { message = "Carrito ya está vacío" });
                }

                _logger.LogInformation($" Eliminando {cart.Items.Count} items del carrito");
                _context.CartItems.RemoveRange(cart.Items);
                cart.Items.Clear();
                cart.Total = 0;

                await _context.SaveChangesAsync();

                _logger.LogInformation(" Carrito vaciado exitosamente");

                return Ok(new { message = "Carrito vaciado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al vaciar carrito");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // 🔧 MÉTODO ACTUALIZADO - OBTENER USERID DE BASE DE DATOS LOCAL
        private async Task<int?> GetCurrentUserId()
        {
            try
            {
                _logger.LogInformation(" Buscando userId en base de datos local...");

                // 1. Obtener el email del token de Keycloak
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                           ?? User.FindFirst("email")?.Value
                           ?? User.FindFirst("preferred_username")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning(" No se encontró email en el token");
                    return null;
                }

                _logger.LogInformation($" Email del usuario: {email}");

                // 2. Buscar el usuario en tu base de datos por email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                _logger.LogInformation($" UserId de base de datos local: {user.Id}");
                return user.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al obtener userId de base de datos local");

                // Fallback: usar userId 1 para desarrollo
                _logger.LogInformation(" Usando userId 1 como fallback");
                return 1;
            }
        }

        // 🔍 ENDPOINT DEBUG - PARA VERIFICAR EL TOKEN Y USER
        [HttpGet("debug-token")]
        [Authorize]
        public async Task<IActionResult> DebugToken()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var userId = await GetCurrentUserId();

            return Ok(new
            {
                message = " Debug de Token y Usuario",
                userIdFromDatabase = userId,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                preferred_username = User.FindFirst("preferred_username")?.Value,
                keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                allClaims = claims
            });
        }

        // 🧪 ENDPOINT TEMPORAL SIN AUTENTICACIÓN - PARA DESARROLLO
        [HttpPost("test")]
        [AllowAnonymous]
        public async Task<IActionResult> AddToCartTest([FromBody] AddToCartRequest request)
        {
            try
            {
                // Usar un userId fijo para testing
                int testUserId = 1;

                _logger.LogInformation($" TEST - Agregando producto {request.ProductId} al carrito");

                var stockProduct = await _stockService.GetProductByIdAsync(request.ProductId);
                if (stockProduct == null)
                    return NotFound(new { error = "Producto no encontrado", code = "PRODUCT_NOT_FOUND" });

                if (stockProduct.StockDisponible < request.Quantity)
                    return BadRequest(new
                    {
                        error = "Stock insuficiente",
                        code = "INSUFFICIENT_STOCK",
                        available = stockProduct.StockDisponible
                    });

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == testUserId);

                if (cart == null)
                {
                    cart = new Cart { UserId = testUserId };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($" Carrito TEST creado: {cart.Id}");
                }

                var localProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId);

                if (localProduct == null)
                {
                    localProduct = new Product
                    {
                        Id = stockProduct.Id,
                        Name = stockProduct.Nombre,
                        Description = stockProduct.Descripcion,
                        Price = stockProduct.Precio,
                        Stock = stockProduct.StockDisponible,
                        Category = stockProduct.Categorias?.FirstOrDefault()?.Nombre ?? "General"
                    };
                    _context.Products.Add(localProduct);
                    await _context.SaveChangesAsync();
                }

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        Product = localProduct
                    };
                    cart.Items.Add(cartItem);
                }

                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = " Producto agregado al carrito (MODO TEST)",
                    cartId = cart.Id,
                    total = cart.Total,
                    itemsCount = cart.Items.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error en carrito test");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        //  VER CARRITO TEST
        [HttpGet("test")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCartTest()
        {
            try
            {
                int testUserId = 1;

                _logger.LogInformation(" TEST - Obteniendo carrito");

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == testUserId);

                if (cart == null)
                {
                    _logger.LogInformation(" Carrito TEST vacío creado");
                    cart = new Cart { UserId = testUserId, Items = new List<CartItem>() };
                    return Ok(cart);
                }

                cart.Total = cart.Items.Sum(item => item.Product.Price * item.Quantity);
                _logger.LogInformation($" Carrito TEST obtenido: {cart.Items.Count} items, Total: {cart.Total}");

                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error al obtener carrito test");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    code = "INTERNAL_ERROR"
                });
            }
        }

        // EN CartController.cs - VERSIÓN CORREGIDA:

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                _logger.LogInformation("🛒 Procesando checkout...");

                var userId = await GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "No autorizado", code = "UNAUTHORIZED" });
                }

                // 1. Obtener carrito CON productos incluidos
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product) // IMPORTANTE: Incluir Product para precio
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                {
                    return BadRequest(new { message = "El carrito está vacío" });
                }

                // ✅ CALCULAR TOTAL PRODUCTOS ANTES DE CONTINUAR
                var totalProductos = cart.Items.Sum(item =>
                    (item.Product?.Price ?? 0) * item.Quantity);

                _logger.LogInformation($"📦 Carrito: {cart.Items.Count} items, Productos: ${totalProductos}");

                ReservaOutput reservaOutput = null;
                CreateShippingResponse envioOutput = null;

                try
                {
                    // 2. CREAR RESERVA EN STOCK API
                    var reservaInput = new ReservaInput
                    {
                        IdCompra = $"COMPRA-{DateTime.UtcNow:yyyyMMddHHmmss}",
                        UsuarioId = userId.Value,
                        Productos = cart.Items.Select(item => new ProductoReserva
                        {
                            IdProducto = item.ProductId,
                            Cantidad = item.Quantity
                        }).ToList()
                    };

                    reservaOutput = await _stockService.CrearReservaAsync(reservaInput);
                    _logger.LogInformation($"✅ Reserva creada en Stock API: {reservaOutput.IdReserva}");

                    // 3. CREAR ENVÍO EN LOGÍSTICA API
                    var deliveryAddressForApi = new DeliveryAddress
                    {
                        Street = request.DeliveryAddress.Street,
                        Number = ExtractNumberFromStreet(request.DeliveryAddress.Street),
                        PostalCode = request.DeliveryAddress.PostalCode,
                        LocalityName = request.DeliveryAddress.City,
                    };

                    var envioInput = new CreateShippingRequest
                    {
                        OrderId = reservaOutput.IdReserva,
                        UserId = userId.Value,
                        DeliveryAddress = deliveryAddressForApi,
                        TransportType = request.TransportType?.ToLower() ?? "truck",
                        Products = cart.Items.Select(item => new ShippingProduct
                        {
                            Id = item.ProductId,
                            Quantity = item.Quantity
                        }).ToList()
                    };

                    _logger.LogInformation($"📦 Creando envío para reserva {reservaOutput.IdReserva}");
                    _logger.LogInformation($"📍 Dirección: {deliveryAddressForApi.Street}, {deliveryAddressForApi.PostalCode}, {deliveryAddressForApi.LocalityName}");

                    envioOutput = await _logisticaService.CrearEnvioAsync(envioInput);
                    _logger.LogInformation($"✅ Envío creado en Logística API: {envioOutput.ShippingId}");

                    // 4. Limpiar carrito
                    await ClearCartInternal(userId.Value);
                    _logger.LogInformation("🛒 Carrito limpiado");

                    // ✅ 5. CALCULAR COSTOS COMPLETOS
                    var costoTotal = totalProductos + envioOutput.ShippingCost;

                    _logger.LogInformation($"💰 RESUMEN DE COSTOS:");
                    _logger.LogInformation($"   Productos: ${totalProductos}");
                    _logger.LogInformation($"   Envío: ${envioOutput.ShippingCost}");
                    _logger.LogInformation($"   Total: ${costoTotal}");

                    // ✅ 6. RETORNAR RESPUESTA CON TODOS LOS COSTOS
                    var response = new
                    {
                        // IDs de referencia
                        reservaId = reservaOutput.IdReserva,
                        shippingId = envioOutput.ShippingId,

                        // ✅ SECCIÓN DE COSTOS DESGLOSADOS
                        costos = new
                        {
                            productos = totalProductos,
                            envio = envioOutput.ShippingCost,
                            total = costoTotal,
                            currency = "ARS"
                        },

                        // Información de entrega
                        estimatedDelivery = envioOutput.EstimatedDeliveryAt,
                        deliveryAddress = new
                        {
                            street = deliveryAddressForApi.Street,
                            locality = deliveryAddressForApi.LocalityName,
                            postalCode = deliveryAddressForApi.PostalCode,
                            number = deliveryAddressForApi.Number
                        },

                        // Estado
                        message = "✅ Checkout completado exitosamente",
                        reservaStatus = reservaOutput.Estado,
                        shippingStatus = "created",

                        // ✅ DETALLE DE PRODUCTOS COMPRADOS
                        productos = cart.Items.Select(item => new
                        {
                            id = item.ProductId,
                            nombre = item.Product?.Name ?? $"Producto {item.ProductId}",
                            precioUnitario = item.Product?.Price ?? 0,
                            cantidad = item.Quantity,
                            subtotal = (item.Product?.Price ?? 0) * item.Quantity
                        }).ToList(),

                        // Información de transporte
                        transportType = request.TransportType?.ToLower() ?? "truck",
                        fecha = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    };

                    _logger.LogInformation($"🎉 Checkout completado: Reserva #{reservaOutput.IdReserva}, Envío #{envioOutput.ShippingId}");
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error durante el checkout - Ejecutando rollback...");

                    // Rollback en caso de error
                    if (reservaOutput != null)
                    {
                        try
                        {
                            await _stockService.CancelarReservaAsync(reservaOutput.IdReserva, "Falla en creación de envío con Logística API");
                            _logger.LogInformation($"✅ Reserva {reservaOutput.IdReserva} cancelada por rollback");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, $"⚠️ Error cancelando reserva {reservaOutput.IdReserva}");
                        }
                    }

                    return StatusCode(500, new
                    {
                        message = $"Error durante el checkout: {ex.Message}",
                        details = ex.InnerException?.Message,
                        productosEnCarrito = totalProductos // Incluir para debug
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error crítico en el proceso de checkout");
                return StatusCode(500, new
                {
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // ✅ MÉTODO MEJORADO PARA EXTRAER NÚMERO
        private int ExtractNumberFromStreet(string street)
        {
            if (string.IsNullOrEmpty(street))
                return 0;

            // Buscar número al final de la cadena (ej: "Av. Siempre Viva 742")
            var match = Regex.Match(street, @"\d+$");
            if (match.Success && int.TryParse(match.Value, out int number))
            {
                return number <= 9999 ? number : 0;
            }

            // Si no encuentra al final, buscar cualquier número
            match = Regex.Match(street, @"\d+");
            if (match.Success && int.TryParse(match.Value, out number))
            {
                return number <= 9999 ? number : 0;
            }

            return 0;
        }

        private async Task ClearCartInternal(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
                cart.Items.Clear();
                cart.Total = 0;
                await _context.SaveChangesAsync();
            }
        }

        // Modelos para las requests
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdateCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        // 🔥 AGREGAR ESTO AL FINAL DE CartController.cs (antes de la última llave)

        // Método helper para extraer número de la calle
    }

}