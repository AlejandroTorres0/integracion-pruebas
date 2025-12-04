using ComprasAPI.Models.DTOs;

namespace ComprasAPI.Services
{
    public interface ILogisticaService
    {
        Task<ShippingCostResponse> CalcularCostoEnvioAsync(ShippingCostRequest request);
        Task<CreateShippingResponse> CrearEnvioAsync(CreateShippingRequest request);
        Task<ShippingDetail> ObtenerSeguimientoAsync(int shippingId);
        Task<List<TransportMethod>> ObtenerMetodosTransporteAsync();

    }


}