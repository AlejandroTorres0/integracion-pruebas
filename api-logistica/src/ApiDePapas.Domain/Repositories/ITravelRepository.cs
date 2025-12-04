using ApiDePapas.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ApiDePapas.Domain.Repositories
{
    // Usaremos esta interfaz para determinar a qué viaje asignar el envío
    public interface ITravelRepository : IGenericRepository<Travel>
    {
        // Método para buscar un viaje activo o crear uno nuevo (lógica compleja)
        Task<int> AssignToExistingOrCreateNewTravelAsync(int distributionCenterId, int transportMethodId);
        Task<List<DistributionCenter>> GetAllDistributionCentersAsync();
    }
}