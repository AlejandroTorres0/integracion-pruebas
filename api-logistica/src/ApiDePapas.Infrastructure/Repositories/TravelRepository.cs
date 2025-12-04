using ApiDePapas.Domain.Entities;
using ApiDePapas.Domain.Repositories;
using ApiDePapas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ApiDePapas.Infrastructure.Repositories
{
    public class TravelRepository : ITravelRepository
    {
        private readonly ApplicationDbContext _context;

        public TravelRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<DistributionCenter>> GetAllDistributionCentersAsync()
        {
            return await _context.DistributionCenters
                .Include(dc => dc.Address) // ¡Importante! Traer la dirección asociada
                .ToListAsync();
        }
        // --- MÉTODOS DE IGenericRepository<Travel> (SOLUCIÓN DEL ERROR CS0535) ---

        // 1. Implementación de FindAsync
        public async Task<IEnumerable<Travel>> FindAsync(Expression<Func<Travel, bool>> predicate)
        {
            return await _context.Travels.Where(predicate).ToListAsync();
        }

        // 2. Implementación de ExistsAsync
        public async Task<bool> ExistsAsync(Expression<Func<Travel, bool>> predicate)
        {
            return await _context.Travels.AnyAsync(predicate);
        }

        // --- MÉTODOS DE ITravelRepository Y CRUD BÁSICO ---

        // Método específico para el servicio (lógica simplificada)
        public async Task<int> AssignToExistingOrCreateNewTravelAsync(int distributionCenterId, int transportMethodId)
        {
            var existingTravel = await _context.Travels
                                            .FirstOrDefaultAsync(t => t.travel_id == 1);

            if (existingTravel == null)
            {
                var newTravel = new Travel
                {
                    // Nota: Si usas Identity/auto-increment en la BD, no asignes el ID manualmente
                    // travel_id = 1, 
                    transport_method_id = transportMethodId,
                    distribution_center_id = distributionCenterId,
                    departure_time = DateTime.UtcNow
                };
                await _context.Travels.AddAsync(newTravel);
                await _context.SaveChangesAsync();
                return newTravel.travel_id;
            }

            return existingTravel.travel_id;
        }
        
        // Implementaciones CRUD restantes (necesarias para IGenericRepository)

        public async Task<IEnumerable<Travel>> GetAllAsync() => await _context.Travels.ToListAsync();
        public async Task<Travel?> GetByIdAsync(int id) => await _context.Travels.FindAsync(id);
        
        public async Task AddAsync(Travel entity)
        {
            await _context.Travels.AddAsync(entity);
            await _context.SaveChangesAsync();
        }
        
        public void Update(Travel entity)
        {
            _context.Travels.Update(entity);
            _context.SaveChanges();
        }
        
        public void Delete(Travel entity)
        {
            _context.Travels.Remove(entity);
            _context.SaveChanges();
        }
    }
}