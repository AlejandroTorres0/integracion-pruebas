using System.Net;
//checkoutmodels.cs
namespace ComprasAPI.Models.DTOs
{
    public class InitCheckoutRequest
    {
        // Solo necesita el carrito (los productos vienen del carrito del usuario)
    }

    public class CalculateShippingRequest
    {
        public Address DeliveryAddress { get; set; }
        public string TransportType { get; set; } // "air", "road", etc.
    }

    public class ConfirmPurchaseRequest
    {
        public Address DeliveryAddress { get; set; }
        public string SelectedTransportType { get; set; }
        public decimal ShippingCost { get; set; }
    }

    public class CheckoutResponse
    {
        public string Message { get; set; }
        public int? BookingId { get; set; }
        public int? ShippingId { get; set; }
        public int? OrderId { get; set; }
        public decimal Total { get; set; }
        public decimal ShippingCost { get; set; }
        public string NextStep { get; set; }

        // ✅ AGREGAR ESTAS PROPIEDADES NUEVAS:
        public int ReservaId { get; set; }
        public string EstimatedDelivery { get; set; }
        public string ReservaStatus { get; set; }
    }

    public class ShippingOption
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string EstimatedDays { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
    }
}