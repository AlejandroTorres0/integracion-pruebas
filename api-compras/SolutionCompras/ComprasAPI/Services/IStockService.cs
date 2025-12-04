// Services/IStockService.cs
using ComprasAPI.Models.DTOs;

namespace ComprasAPI.Services
{
    public interface IStockService
    {
        Task<ReservaOutput> CrearReservaAsync(ReservaInput reserva);
        Task<bool> CancelarReservaAsync(int idReserva, string motivo); // ← string en lugar de int
        Task<ProductoStock> GetProductoAsync(int productoId);
        Task<List<ProductoStock>> GetAllProductsAsync();
        Task<ProductoStock> GetProductByIdAsync(int id);
    }
}