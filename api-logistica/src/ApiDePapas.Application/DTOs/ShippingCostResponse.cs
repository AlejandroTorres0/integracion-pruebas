using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Application.DTOs
{
    public record ShippingCostResponse(
        [property: JsonPropertyName("currency")]
        [Required]
        string currency,

        [property: JsonPropertyName("total_cost")]
        [Required]
        double total_cost,

        [property: JsonPropertyName("transport_type")]
        [Required]
        TransportType transport_type,

        [property: JsonPropertyName("products")]
        [Required]
        List<ProductOutput> products,

        [property: JsonPropertyName("estimated_delivery_at")]
        [Required]
        DateTime estimated_delivery_at
    );
}
