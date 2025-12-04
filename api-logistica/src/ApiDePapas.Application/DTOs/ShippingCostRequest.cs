using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Application.DTOs
{
    public record ShippingCostRequest(
        [property: JsonPropertyName("delivery_address")]
        [Required]
        DeliveryAddressRequest delivery_address,

        [property: JsonPropertyName("products")]
        [Required]  
        List<ProductQty> products
    );
}
