// Controllers/TestIntegrationController.cs (corregido)
using ComprasAPI.Models.DTOs;
using ComprasAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComprasAPI.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestIntegrationController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogisticaService _logisticaService;
        private readonly ILogger<TestIntegrationController> _logger;

        public TestIntegrationController(
            IStockService stockService,
            ILogisticaService logisticaService,
            ILogger<TestIntegrationController> logger)
        {
            _stockService = stockService;
            _logisticaService = logisticaService;
            _logger = logger;
        }

        [HttpGet("stock")]
        public async Task<IActionResult> TestStock()
        {
            try
            {
                _logger.LogInformation(" Probando conexión con Stock...");

                var productos = await _stockService.GetAllProductsAsync();

                return Ok(new
                {
                    message = " Stock Service funcionando",
                    productosCount = productos.Count,
                    productos = productos.Take(3),
                    source = productos.Any() ? "Stock API" : "Datos de prueba"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = " Error con Stock Service",
                    details = ex.Message,
                    source = "Fallback a datos de prueba"
                });
            }
        }

        [HttpPost("stock/create-reservation")]
        public async Task<IActionResult> TestCreateReservation()
        {
            try
            {
                _logger.LogInformation(" Probando creación de reserva...");

                var reservaInput = new ReservaInput
                {
                    IdCompra = "TEST-" + Guid.NewGuid().ToString(),
                    UsuarioId = 1,
                    Productos = new List<ProductoReserva>
                    {
                        new ProductoReserva { IdProducto = 1, Cantidad = 2 },
                        new ProductoReserva { IdProducto = 2, Cantidad = 1 }
                    }
                };

                var resultado = await _stockService.CrearReservaAsync(reservaInput);

                return Ok(new
                {
                    message = " Creación de reserva funcionando",
                    reservaInput = reservaInput,
                    resultado = resultado,
                    source = resultado.IdReserva > 0 ? "Stock API" : "Datos de prueba"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = " Error creando reserva",
                    details = ex.Message,
                    source = "Fallback a datos de prueba"
                });
            }
        }

        [HttpGet("logistica/transport-methods")]
        public async Task<IActionResult> TestLogisticaTransport()
        {
            try
            {
                _logger.LogInformation(" Probando métodos de transporte...");

                var metodos = await _logisticaService.ObtenerMetodosTransporteAsync();

                return Ok(new
                {
                    message = " Logística Service funcionando",
                    metodosCount = metodos.Count,
                    metodos = metodos,
                    source = metodos.Any() ? "Logística API" : "Datos de prueba"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = " Error con Logística Service",
                    details = ex.Message,
                    source = "Fallback a datos de prueba"
                });
            }
        }

        [HttpPost("logistica/calculate-shipping")]
        public async Task<IActionResult> TestCalculateShipping()
        {
            try
            {
                _logger.LogInformation(" Probando cálculo de envío...");

                var request = new ShippingCostRequest
                {
                    DeliveryAddress = new Address
                    {
                        Street = "Av. Siempre Viva 123",
                        City = "Resistencia",
                        State = "Chaco",
                        PostalCode = "H3500ABC",
                        Country = "AR"
                    },
                    Products = new List<ProductRequest>
                    {
                        new ProductRequest { Id = 1, Quantity = 2 },
                        new ProductRequest { Id = 2, Quantity = 1 }
                    }
                };

                var resultado = await _logisticaService.CalcularCostoEnvioAsync(request);

                return Ok(new
                {
                    message = " Cálculo de envío funcionando",
                    request = request,
                    resultado = resultado,
                    source = resultado.TotalCost > 0 ? "Logística API" : "Datos de prueba"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = " Error calculando envío",
                    details = ex.Message,
                    source = "Fallback a datos de prueba"
                });
            }
        }

        [HttpGet("integration-status")]
        public async Task<IActionResult> GetIntegrationStatus()
        {
            var status = new
            {
                Timestamp = DateTime.UtcNow,
                ComprasAPI = " Running",
                StockService = await TestStockInternal(),
                LogisticaService = await TestLogisticaInternal(),
                NextSteps = new[]
                {
                    "1. Verificar que los servicios responden",
                    "2. Probar endpoints individuales",
                    "3. Probar flujo completo de checkout",
                    "4. Configurar APIs externas cuando estén listas"
                }
            };

            return Ok(status);
        }

        private async Task<string> TestStockInternal()
        {
            try
            {
                var productos = await _stockService.GetAllProductsAsync();
                return productos.Any() ? " Con datos" : " Sin datos (usando fallback)";
            }
            catch
            {
                return " Error (usando fallback)";
            }
        }

        private async Task<string> TestLogisticaInternal()
        {
            try
            {
                var metodos = await _logisticaService.ObtenerMetodosTransporteAsync();
                return metodos.Any() ? " Con datos" : " Sin datos (usando fallback)";
            }
            catch
            {
                return " Error (usando fallback)";
            }
        }
    }
}