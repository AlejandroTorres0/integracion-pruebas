// En src/ApiDePapas.Infrastructure/Repositories/LocalityRepository.cs
using ApiDePapas.Domain.Entities;
using ApiDePapas.Domain.Repositories;
using ApiDePapas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq; // Necesario para Linq

namespace ApiDePapas.Infrastructure.Repositories
{
    public class LocalityRepository : ILocalityRepository
    {
        private readonly ApplicationDbContext _context;

        public LocalityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Locality?> GetByCompositeKeyAsync(string postalCode, string localityName)
        {
            return await _context.Localities
                                 .FirstOrDefaultAsync(l =>
                                     l.postal_code == postalCode &&
                                     l.locality_name == localityName);
        }

        // IMPLEMENTACIÓN REINTRODUCIDA (De la rama VIEJA)
        public async Task<IEnumerable<Locality>> SearchAsync(
            string? state = null,
            string? localityName = null,
            string? postalCode = null,
            int page = 1,
            int limit = 50)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 50;

            IQueryable<Locality> q = _context.Localities.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(state))
                q = q.Where(l => l.state_name.ToUpper() == state.ToUpper());

            if (!string.IsNullOrWhiteSpace(localityName))
                q = q.Where(l => l.locality_name.ToUpper().Contains(localityName.ToUpper()));

            if (!string.IsNullOrWhiteSpace(postalCode))
                q = q.Where(l => l.postal_code == postalCode);

            q = q.OrderBy(l => l.state_name)
                 .ThenBy(l => l.locality_name)
                 .ThenBy(l => l.postal_code);

            return await q.Skip((page - 1) * limit)
                          .Take(limit)
                          .ToListAsync();
        }

        // IMPLEMENTACIÓN MANTENIDA/AÑADIDA (De la rama ACTUAL)
        public async Task<List<Locality>> GetByPostalCodeAsync(string postalCode)
        {
            return await _context.Localities
                                 .Where(l => l.postal_code == postalCode)
                                 .ToListAsync();
        }

        // --- Implementaciones de IGenericRepository<Locality> van aquí ---
        public async Task<IEnumerable<Locality>> GetAllAsync()
        {
            return await _context.Localities.ToListAsync();
        }
        public Task<Locality?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task AddAsync(Locality entity) => throw new NotImplementedException();
        public void Update(Locality entity) => throw new NotImplementedException();
        public void Delete(Locality entity) => throw new NotImplementedException();
        public Task<IEnumerable<Locality>> FindAsync(Expression<Func<Locality, bool>> predicate) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(Expression<Func<Locality, bool>> predicate) => throw new NotImplementedException();
    }
}
