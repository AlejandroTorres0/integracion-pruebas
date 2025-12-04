using System.Text.Json.Serialization;

namespace ComprasAPI.Models.DTOs
{
    public class ProductoStock
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int StockDisponible { get; set; }
        public decimal PesoKg { get; set; }
        public Dimensiones Dimensiones { get; set; }
        public UbicacionAlmacen Ubicacion { get; set; }
        public List<Categoria> Categorias { get; set; }
        public List<ImagenProducto> Imagenes { get; set; }
    }

    public class Dimensiones
    {
        public int Id { get; set; }
        public decimal LargoCm { get; set; }
        public decimal AnchoCm { get; set; }
        public decimal AltoCm { get; set; }
    }

    public class UbicacionAlmacen
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; } // ← AGREGAR
        public DateTime FechaActualizacion { get; set; } // ← AGREGAR
        public bool Activa { get; set; } // ← AGREGAR
    }

    public class ImagenProducto
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public bool EsPrincipal { get; set; }
        public int ProductoId { get; set; }
    }

    // Request para crear reserva
    public class ReservaInput
    {
        public string IdCompra { get; set; }
        public int UsuarioId { get; set; }
        public List<ProductoReserva> Productos { get; set; }
    }

    public class ProductoReserva
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
    }

    // Response de reserva
    // Response de reserva
    public class ReservaOutput
    {
        [JsonPropertyName("id")]  // ← AGREGAR ESTO
        public int IdReserva { get; set; }

        [JsonPropertyName("idCompra")]  // ← AGREGAR ESTO
        public string IdCompra { get; set; }

        [JsonPropertyName("usuarioId")]  // ← AGREGAR ESTO
        public int UsuarioId { get; set; }

        [JsonPropertyName("estado")]  // ← AGREGAR ESTO
        public string Estado { get; set; }

        [JsonPropertyName("expiresAt")]  // ← AGREGAR ESTO
        public string ExpiresAt { get; set; }

        [JsonPropertyName("fechaCreacion")]  // ← AGREGAR ESTO
        public string FechaCreacion { get; set; }
    }

    // Detalle completo de reserva
    public class ReservaCompleta
    {
        public int IdReserva { get; set; }
        public string IdCompra { get; set; }
        public int UsuarioId { get; set; }
        public string Estado { get; set; }
        public string ExpiresAt { get; set; }
        public string FechaCreacion { get; set; }
        public List<ProductoReservaDetalle> Productos { get; set; }
    }

    public class ProductoReservaDetalle
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }


}