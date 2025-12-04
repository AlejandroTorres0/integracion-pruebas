using ApiDePapas.Domain.Entities;
using System.Text.Json.Serialization;

namespace ApiDePapas.Application.DTOs
{
    public class UpdateStatusRequest
    {
        [JsonPropertyName("new_status")]
        public ShippingStatus NewStatus { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "Status updated by logistics operator.";
    }
}
