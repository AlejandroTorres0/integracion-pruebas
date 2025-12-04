using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using ApiDePapas.Domain.Entities;


namespace ApiDePapas.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IShippingRepository _shippingRepository;
        private readonly IShippingService _shippingService; // <--- 1. Inyectamos esto para usar CancelAsync

        public DashboardService(IShippingRepository shippingRepository, IShippingService shippingService)
        {
            _shippingRepository = shippingRepository;
            _shippingService = shippingService;
        }
        public async Task<IEnumerable<DashboardShipmentDto>> GetDashboardShipmentsAsync(
            int page, int pageSize, string? id, string? city, string? status, DateTime? startDate, DateTime? endDate)
        {
            IQueryable<ApiDePapas.Domain.Entities.ShippingDetail> query = _shippingRepository.GetAllQueryable()
                .Include(s => s.DeliveryAddress)
                    .ThenInclude(da => da.Locality)
                .Include(s => s.Travel)
                    .ThenInclude(t => t.TransportMethod)
                .Include(s => s.Travel)
                    .ThenInclude(t => t.DistributionCenter)
                        .ThenInclude(dc => dc.Address);

            // Apply filters
            if (!string.IsNullOrEmpty(id))
            {
                query = query.Where(s => s.shipping_id.ToString().Contains(id));
            }
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(s => s.DeliveryAddress.locality_name.Contains(city));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            if (startDate.HasValue)
            {
                query = query.Where(s => s.created_at >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(s => s.created_at <= endDate.Value);
            }

            var shipments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return shipments.Select(s => new DashboardShipmentDto
            {
                shipping_id = s.shipping_id,
                order_id = s.order_id,
                user_id = s.user_id,
                products = s.products,
                status = s.status,
                transport_type = s.Travel.TransportMethod.transport_type,
                estimated_delivery_at = s.estimated_delivery_at,
                created_at = s.created_at,
                delivery_address = new AddressReadDto
                {
                    address_id = s.DeliveryAddress.address_id,
                    street = s.DeliveryAddress.street,
                    number = s.DeliveryAddress.number,
                    postal_code = s.DeliveryAddress.postal_code,
                    locality_name = s.DeliveryAddress.locality_name,
                },
                departure_address = new AddressReadDto
                {
                    address_id = s.Travel.DistributionCenter.Address.address_id,
                    street = s.Travel.DistributionCenter.Address.street,
                    number = s.Travel.DistributionCenter.Address.number,
                    postal_code = s.Travel.DistributionCenter.Address.postal_code,
                    locality_name = s.Travel.DistributionCenter.Address.locality_name,
                }
            });
        }

        public async Task<int> GetTotalDashboardShipmentsCountAsync(
            string? id, string? city, string? status, DateTime? startDate, DateTime? endDate)
        {
            var query = _shippingRepository.GetAllQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(id))
            {
                query = query.Where(s => s.shipping_id.ToString().Contains(id));
            }
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(s => s.DeliveryAddress.locality_name.Contains(city));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
            }
            if (startDate.HasValue)
            {
                query = query.Where(s => s.created_at >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(s => s.created_at <= endDate.Value);
            }

            return await query.CountAsync();
        }

        public async Task UpdateShipmentStatusAsync(int id, ShippingStatus newStatus, string? message = null)
        {
            // 1. Buscar el pedido
            var shipping = await _shippingRepository.GetByIdAsync(id);
            if (shipping == null)
            {
                throw new KeyNotFoundException($"Pedido con ID {id} no encontrado.");
            }

            // --- INICIO DE LÓGICA ROBUSTA TRAÍDA DE SHIPPING SERVICE ---
            // Si el nuevo estado es 'Cancelled', delegamos al servicio de envíos.
            // Esto asegura que se envíe la notificación al módulo de Compras.
            if (newStatus == ShippingStatus.cancelled)
            {
                await _shippingService.CancelAsync(id, DateTime.UtcNow);
                return; // Salimos porque CancelAsync ya guardó los cambios
            }

            // 3. Validar estados inmutables
            // Si ya está Cancelado, no se puede tocar.
            if (shipping.status == ShippingStatus.cancelled)
            {
                throw new InvalidOperationException($"El envío {id} está cancelado y no puede modificarse.");
            }

            // Si ya está Entregado, no se puede cambiar.
            if (shipping.status == ShippingStatus.delivered)
            {
                throw new InvalidOperationException($"El envío {id} ya fue entregado y no puede cambiar de estado.");
            }

            // 4. Validar coherencia temporal (No volver al pasado)
            if (newStatus == ShippingStatus.created && shipping.status != ShippingStatus.created)
            {
                throw new InvalidOperationException($"No se puede revertir el envío {id} al estado 'Created'.");
            }

            // --- FIN DE LÓGICA ROBUSTA ---

            // 5. Validar si realmente cambió el estado (Optimización)
            if (shipping.status == newStatus) return;

            // 6. Actualizar Estado y Fecha
            shipping.status = newStatus;
            shipping.updated_at = DateTime.UtcNow;

            // 7. AGREGAR LOG 
            if (shipping.logs == null) shipping.logs = new List<ShippingLog>();

            // Usamos el mensaje personalizado si viene, sino el default
            string logMessage = !string.IsNullOrEmpty(message) 
                ? message 
                : $"Estado actualizado manualmente desde Dashboard a {newStatus}.";
            
            shipping.logs.Add(new ShippingLog(
                Timestamp: DateTime.UtcNow,
                Status: newStatus,
                Message: logMessage
            ));

            // 8. Guardar
            _shippingRepository.Update(shipping);
        }
    }
}
