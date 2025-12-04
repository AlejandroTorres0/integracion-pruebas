using System;
using System.Linq;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using System.Threading.Tasks;
using ApiDePapas.Domain.Entities;
using ApiDePapas.Domain.Repositories;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ApiDePapas.Application.Services
{
    public class ShippingService : IShippingService
    {
        // Se mantienen todas las inyecciones de dependencia
        private readonly ICalculateCost _calculate_cost;
        private readonly IShippingRepository _shipping_repository;
        private readonly ILocalityRepository _locality_repository;
        private readonly IAddressRepository _address_repository;
        private readonly ITravelRepository _travel_repository;
        private readonly IPurchasingService _purchasing_service;
        private readonly IDistanceService _distance_service;

        // Constructor se mantiene
        public ShippingService(
            ICalculateCost calculateCost,
            IShippingRepository shippingRepository,
            ILocalityRepository localityRepository,
            IAddressRepository addressRepository,
            ITravelRepository travelRepository,
            IPurchasingService purchasingService,
            IDistanceService distanceService)
        {
            _calculate_cost = calculateCost;
            _shipping_repository = shippingRepository;
            _locality_repository = localityRepository;
            _address_repository = addressRepository;
            _travel_repository = travelRepository;
            _purchasing_service = purchasingService;
            _distance_service = distanceService;
        }

        // IMPLEMENTACIÓN DE CREACIÓN (ACTUALIZADA: Manejo de errores con null)
        public async Task<CreateShippingResponse?> CreateNewShipping(CreateShippingRequest req)
        {
            // CAMBIO: Unificación de validación y retorno de null en lugar de lanzar ArgumentException
            if (req == null || req.products == null || req.products.Count == 0)
                return null;

            var costReq = new CalculateCostRequest(
                req.delivery_address,
                req.transport_type,
                req.products.Select(p => new ProductQty(p.id, p.quantity)).ToList()
            );
            var cost = await _calculate_cost.CalculateShippingCostAsync(costReq);

            // 🔽 NUEVO: los días estimados vienen de CalculateCost (máximo según tipo de transporte)
            int estimated_days = CalculateCost.GetMaxEstimatedDays(req.transport_type);

            var locality = await _locality_repository.GetByCompositeKeyAsync(
                req.delivery_address.postal_code,
                req.delivery_address.locality_name);

            if (locality == null) return null;

            // ... (Resto de la lógica de creación de Address, Travel, y mapeo a ShippingDetail es igual a tu código ACTUAL)
            // ...

            var existingAddress = await _address_repository.FindExistingAddressAsync(
                req.delivery_address.street,
                req.delivery_address.number,
                req.delivery_address.postal_code,
                req.delivery_address.locality_name);

            if (existingAddress == null)
            {
                var newAddress = new Address
                {
                    street = req.delivery_address.street,
                    number = req.delivery_address.number,
                    postal_code = req.delivery_address.postal_code,
                    locality_name = req.delivery_address.locality_name
                };
                await _address_repository.AddAsync(newAddress);
                existingAddress = newAddress;
            }
            int delivery_address_id = existingAddress.address_id;

            // ========================================================================
            // 🚛 LÓGICA DE SELECCIÓN DE CENTRO DE DISTRIBUCIÓN más cercano al cliente
            // ==========================================================================
            
            // A. Obtener todos los CDs disponibles
            var distributionCenters = await _travel_repository.GetAllDistributionCentersAsync();
            
            int bestDcId = 1; // Default por si acaso
            double minDistance = double.MaxValue;

            // B. Iterar y buscar el más cercano
            foreach (var dc in distributionCenters)
            {
                // Si el CD no tiene dirección, lo saltamos
                if (dc.Address == null) continue;

                // Calculamos distancia: CD PostalCode -> Cliente PostalCode
                double dist = await _distance_service.GetDistanceKm(
                    dc.Address.postal_code, 
                    req.delivery_address.postal_code
                );

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestDcId = dc.distribution_center_id;
                }
            }
            // ==========================================================

            // Mapeo del transporte (tu código de antes)
            int transportMethodId = req.transport_type switch
            {
                TransportType.road => 1,
                TransportType.air  => 2001,
                TransportType.rail => 3,
                TransportType.sea  => 1001,
            
            };

            int travel_id = await _travel_repository.AssignToExistingOrCreateNewTravelAsync(
                distributionCenterId: bestDcId, // 
                transportMethodId: transportMethodId
            );

            var newShipping = new ShippingDetail
            {
                order_id = req.order_id,
                user_id = req.user_id,

                travel_id = travel_id,
                delivery_address_id = delivery_address_id,

                products = req.products.Select(p => new ProductQty(p.id, p.quantity)).ToList(),

                status = ShippingStatus.created,
                total_cost = (float)cost.total_cost,
                currency = cost.currency,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow,

                // 🔽 AQUÍ USAMOS LOS DÍAS CALCULADOS SEGÚN EL TIPO DE TRANSPORTE
                estimated_delivery_at = DateTime.UtcNow.AddDays(estimated_days),

                tracking_number = Guid.NewGuid().ToString(),
                carrier_name = "PENDIENTE",
                logs = new List<ShippingLog>(new[] { new ShippingLog(DateTime.UtcNow, ShippingStatus.created, "Shipping created in DB.") })
            };

            await _shipping_repository.AddAsync(newShipping);

            return new CreateShippingResponse(
                shipping_id: newShipping.shipping_id,
                status: newShipping.status,
                transport_type: req.transport_type,
                estimated_delivery_at: newShipping.estimated_delivery_at
            );
        }

        // IMPLEMENTACIÓN REINTRODUCIDA (De la rama VIEJA)
        public async Task<ShippingDetailResponse?> GetByIdAsync(int id)
        {
            var data = await _shipping_repository.GetByIdAsync(id);
            if (data is null)
            {
                return null; 
            }

            var departureAddressEntity = data.Travel?.DistributionCenter?.Address;

            // Mapeo al DTO 
            var responseDto = new ShippingDetailResponse
            {
                shipping_id = data.shipping_id,
                order_id = data.order_id,
                user_id = data.user_id,
                status = data.status,
                tracking_number = data.tracking_number,
                carrier_name = data.carrier_name,
                total_cost = data.total_cost,
                currency = data.currency,
                estimated_delivery_at = data.estimated_delivery_at,
                created_at = data.created_at,
                updated_at = data.updated_at,
                
                transport_type = data.Travel?.TransportMethod?.transport_type.ToString() ?? string.Empty, 

                delivery_address = new AddressReadDto
                {
                    address_id = data.DeliveryAddress?.address_id ?? 0,
                    street = data.DeliveryAddress?.street ?? string.Empty,
                    number = data.DeliveryAddress?.number ?? 0,
                    postal_code = data.DeliveryAddress?.postal_code ?? string.Empty,
                    locality_name = data.DeliveryAddress?.locality_name ?? string.Empty,
                },
                
                departure_address = new AddressReadDto 
                {
                    address_id = departureAddressEntity?.address_id ?? 0, 
                    street = departureAddressEntity?.street ?? string.Empty,
                    number = departureAddressEntity?.number ?? 0,
                    postal_code = departureAddressEntity?.postal_code ?? string.Empty,
                    locality_name = departureAddressEntity?.locality_name ?? string.Empty,
                },
                
                // --- CORRECCIÓN CRÍTICA ---
                // Tu DTO usa (int id, int quantity)
                products = data.products.Select(p => new ProductQtyReadDto(product_id: p.id, quantity: p.quantity)).ToList(),
                
                // --- CORRECCIÓN CRÍTICA ---
                // Tu DTO usa (DateTime timestamp, ShippingStatus status, string message)
                logs = data.logs.Select(l => new ShippingLogReadDto(
                    timestamp: l.Timestamp ?? DateTime.MinValue, 
                    status: l.Status ?? ShippingStatus.created, 
                    message: l.Message
                )).ToList()
            };
            
            return responseDto;
        }
        private void TransitionToStatus(ShippingDetail shipping, ShippingStatus newStatus, string message)
        {
            // 1. Actualizar estado y fecha
            shipping.status = newStatus;
            shipping.updated_at = DateTime.UtcNow;

            // 2. Asegurar que la lista de logs exista
            if (shipping.logs == null) 
                shipping.logs = new List<ShippingLog>();

            // 3. Agregar el log histórico
            shipping.logs.Add(new ShippingLog(
                Timestamp: DateTime.UtcNow,
                Status: newStatus,
                Message: message
            ));
        }
        // IMPLEMENTACIÓN REINTRODUCIDA (De la rama VIEJA)
        public async Task<CancelShippingResponse> CancelAsync(int id, DateTime whenUtc)
        {
            // 1. Obtener entidad
            var s = await _shipping_repository.GetByIdAsync(id);
            if (s is null)
                throw new KeyNotFoundException($"Shipping {id} not found");

            // 2. Validar estado actual
            if (s.status is ShippingStatus.delivered or ShippingStatus.cancelled)
                throw new InvalidOperationException($"Shipping {id} cannot be cancelled in state '{s.status}'.");

            // 3. Usar el método auxiliar para cambiar estado y agregar LOG
            TransitionToStatus(s, ShippingStatus.cancelled, "The shipment was cancelled");

            // 4. Guardar entidad completa 
            // Usamos el Update genérico del repositorio para que EF Core detecte el cambio en la columna JSON 'logs'
            _shipping_repository.Update(s);

            // 5. Notificar (Fuego y olvido)
            _ = _purchasing_service.NotifyShippingCancellationAsync(id);

            return new CancelShippingResponse(
                shipping_id: id,
                status: ShippingStatus.cancelled,
                cancelled_at: whenUtc
            );
        }

        // IMPLEMENTACIÓN REINTRODUCIDA (De la rama VIEJA)
        public async Task<ShippingListResponse> List(
            int? userId, ShippingStatus? status, DateOnly? fromDate, DateOnly? toDate, int page, int limit)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 20;

            // Utilizamos GetAllQueryable() de ShippingRepository (versión ACTUAL)
            var query = _shipping_repository.GetAllQueryable();

            // Aplicamos los filtros
            if (userId.HasValue) query = query.Where(s => s.user_id == userId.Value);
            if (status.HasValue) query = query.Where(s => s.status == status.Value);
            if (fromDate.HasValue)
            {
                var from = fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(s => s.created_at >= from);
            }
            if (toDate.HasValue)
            {
                var to = toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(s => s.created_at <= to);
            }

            // Orden y paginado
            var ordered = query.OrderByDescending(s => s.created_at);
            var total = await ordered.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)limit);
            var slice = await ordered.Skip((page - 1) * limit).Take(limit).ToListAsync();

            var summaries = slice.Select(s => new ShipmentSummary(
                ShippingId: s.shipping_id,
                OrderId: s.order_id,
                UserId: s.user_id,
                Products: s.products?.ToList() ?? new List<ProductQty>(),
                Status: s.status,
                TransportType: (s.Travel != null && s.Travel.TransportMethod != null)
                                ? s.Travel.TransportMethod.transport_type
                                : TransportType.road,
                EstimatedDeliveryAt: s.estimated_delivery_at,
                CreatedAt: s.created_at
            )).ToList();

            var pagination = new PaginationData(
                current_page: page,
                total_pages: totalPages,
                total_items: total,
                items_per_page: limit
            );

            return new ShippingListResponse(summaries, pagination);
        }

        public async Task<bool> UpdateStatusAsync(int shippingId, UpdateStatusRequest request)
        {
            var shipping = await _shipping_repository.GetByIdAsync(shippingId);
            if (shipping == null)
            {
                return false; // O lanzar KeyNotFoundException si se prefiere
            }

            // 1. Si ya está Cancelado, no se puede tocar.
            if (shipping.status == ShippingStatus.cancelled)
            {
                throw new InvalidOperationException($"El envío {shippingId} está cancelado y no puede modificarse.");
            }

            // 2. Si ya está Entregado, no se puede cambiar (a menos que implementes lógica de devoluciones).
            if (shipping.status == ShippingStatus.delivered)
            {
                throw new InvalidOperationException($"El envío {shippingId} ya fue entregado y no puede cambiar de estado.");
            }
            // 3. No permitir revertir a 'Created' desde otros estados.
            if (request.NewStatus == ShippingStatus.created && shipping.status != ShippingStatus.created)
            {
                throw new InvalidOperationException($"No se puede revertir el envío {shippingId} al estado 'Created'.");
            }
            if (request.NewStatus == ShippingStatus.cancelled)
            {
                // CancelAsync se encarga de validar si se puede cancelar 
                // y de enviar la notificación a Compras.
                await CancelAsync(shippingId, DateTime.UtcNow);
                return true;
            }

            // Usar el método helper para aplicar el cambio de estado y el log
            TransitionToStatus(shipping, request.NewStatus, request.Message);

            // El método Update del repositorio ya se encarga de llamar a SaveChanges.
            _shipping_repository.Update(shipping);

            return true;
        }
    }
}