using System.Threading.Tasks;
using ApiDePapas.Application.DTOs;

namespace ApiDePapas.Application.Interfaces
{
    public interface ICalculateCost
    {
        Task<ShippingCostResponse> CalculateShippingCostAsync(CalculateCostRequest request);
    }
}
