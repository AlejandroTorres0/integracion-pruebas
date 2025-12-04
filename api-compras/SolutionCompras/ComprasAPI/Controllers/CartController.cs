using ComprasAPI.Data;
using ComprasAPI.Models;
using ComprasAPI.Models.DTOs;
using ComprasAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ComprasAPI.Controllers
{
    [ApiController]
    [Route("api/shopcart")]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, IStockService stockService, ILogger<CartController> logger)
        {
            _context = context;
            _stockService = stockService;
            _logger = logger;
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
}