using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Application.DTOs
{
    public record CalculateCostRequest(
        [property: JsonPropertyName("delivery_address")]
        [Required]
        DeliveryAddressRequest delivery_address,

        [property: JsonPropertyName("transport_type")]
        [Required]
        TransportType? transport_type,

        [property: JsonPropertyName("products")]
        [Required]  
        List<ProductQty> products
    );
}
