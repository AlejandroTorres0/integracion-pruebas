/*

// Models/DTOs/LogisticaModels.cs
namespace ComprasAPI.Models.DTOs
{
    // Request para calcular costo de envío
    public class ShippingCostRequest
    {
        public Address DeliveryAddress { get; set; }
        public List<ProductRequest> Products { get; set; }
    }

    // Response de cálculo de costo
    public class ShippingCostResponse
    {
        public string Currency { get; set; }
        public decimal TotalCost { get; set; }
        public string TransportType { get; set; }
        public List<ProductCost> Products { get; set; }
    }

    // Request para crear envío
    public class CreateShippingRequest
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public Address DeliveryAddress { get; set; }
        public string TransportType { get; set; }
        public List<ProductRequest> Products { get; set; }
    }

    // Response de creación de envío
    public class CreateShippingResponse
    {
        public int ShippingId { get; set; }
        public string Status { get; set; }
        public string TransportType { get; set; }
        public string EstimatedDeliveryAt { get; set; }
    }

    // Detalle de seguimiento
    public class ShippingDetail
    {
        public int ShippingId { get; set; }
        public string Status { get; set; }
        public string EstimatedDeliveryAt { get; set; }
        public string TrackingNumber { get; set; }
        public string CarrierName { get; set; }
    }

    // Métodos de transporte
    public class TransportMethodsResponse
    {
        public List<TransportMethod> TransportMethods { get; set; }
    }

    public class TransportMethod
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string EstimatedDays { get; set; }
    }

    // Modelos comunes
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    public class ProductRequest
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductCost
    {
        public int Id { get; set; }
        public decimal Cost { get; set; }
    }
}

*/

// Models/DTOs/LogisticaModels.cs
using System.Text.Json.Serialization;

namespace ComprasAPI.Models.DTOs
{
    // 🔥 NUEVA CLASE - Para el wrapper "req" que espera Logística API
    public class ShippingRequestWrapper
    {
        [JsonPropertyName("req")]
        public CreateShippingRequest Request { get; set; }
    }

    // 🔥 ACTUALIZADA - Request para crear envío
    public class CreateShippingRequest
    {
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("delivery_address")]
        public DeliveryAddress DeliveryAddress { get; set; }

        [JsonPropertyName("transport_type")]
        public string TransportType { get; set; }

        [JsonPropertyName("products")]
        public List<ShippingProduct> Products { get; set; }
    }

    // 🔥 NUEVA CLASE - Dirección específica para envíos
    public class DeliveryAddress
    {
        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; }

        [JsonPropertyName("locality_name")]  // ← ESTE es el campo que espera la API
        public string LocalityName { get; set; }
    }

    // 🔥 NUEVA CLASE - Producto específico para envíos
    public class ShippingProduct
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    // Response de creación de envío
    public class CreateShippingResponse
    {
        public int ShippingId { get; set; }
        public string Status { get; set; }
        public string TransportType { get; set; }
        public string EstimatedDeliveryAt { get; set; }

        public decimal ShippingCost { get; set; }
    }

    // 🟡 MANTENER ESTAS - Para cálculo de costo (NO cambiar)
    public class ShippingCostRequest
    {
        public Address DeliveryAddress { get; set; }
        public List<ProductRequest> Products { get; set; }
    }

    public class ShippingCostResponse
    {
        public string Currency { get; set; }
        public decimal TotalCost { get; set; }
        public string TransportType { get; set; }
        public List<ProductCost> Products { get; set; }
    }

    // 🟡 MANTENER ESTAS - Para direcciones genéricas (NO cambiar)
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    // 🟡 MANTENER ESTAS - Para productos genéricos (NO cambiar)
    public class ProductRequest
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductCost
    {
        public int Id { get; set; }
        public decimal Cost { get; set; }
    }

    // 🟡 MANTENER ESTAS - Para seguimiento (NO cambiar)
    public class ShippingDetail
    {
        public int ShippingId { get; set; }
        public string Status { get; set; }
        public string EstimatedDeliveryAt { get; set; }
        public string TrackingNumber { get; set; }
        public string CarrierName { get; set; }
    }

    // 🟡 MANTENER ESTAS - Para métodos de transporte (NO cambiar)
    public class TransportMethodsResponse
    {
        public List<TransportMethod> TransportMethods { get; set; }
    }

    public class TransportMethod
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string EstimatedDays { get; set; }
    }

    public class CheckoutRequest
    {
        public Address DeliveryAddress { get; set; }
        public string TransportType { get; set; }
    }
}