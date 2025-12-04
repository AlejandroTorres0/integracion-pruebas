using System.ComponentModel.DataAnnotations;

namespace ApiDePapas.Domain.Entities
{
    // Detalle de producto tal como nos lo devuelve el módulo de Stock
    public class ProductDetail
    {
        [Required]
        public int id { get; set; }

        [Required]
        public float weight { get; set; }

        [Required]
        public float length { get; set; }

        [Required]
        public float width { get; set; }

        [Required]
        public float height { get; set; }

        // Código postal del depósito/almacén donde está el producto (ORIGEN del envío)
        // Puede ser null si Stock aún no lo tiene; en ese caso usamos DEFAULT_ORIGIN_CPA.
        public string? warehouse_postal_code { get; set; }
    }
}
