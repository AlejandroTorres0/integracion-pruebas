using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ApiDePapas.Application.DTOs;
using ApiDePapas.Application.Interfaces;
using ApiDePapas.Domain.Entities;

/* 
 * Quotes cost for a shipment without creating any resources.
 * Used by Order Management module to show shipping options to customers before purchase.
 *
 * Integration Flow:
 * 1. Order Management sends only: delivery_address + product IDs with quantities
 * 2. Logistics queries Stock module for EACH product:
 *    - GET /products/{id} → returns weight, dimensions, warehouse_postal_code
 * 3. Logistics calculates:
 *    - Total weight = sum(product.weight * quantity)
 *    - Total volume = sum(product dimensions * quantity)
 *    - Distance = from warehouse_postal_code to delivery_address.postal_code
 * 4. Returns estimated cost based on weight, volume, distance, and transport type
 * 5. NO data is persisted (quote only)
 */

namespace ApiDePapas.Application.Services
{
    public class CalculateCost : ICalculateCost
    {
        private readonly IStockService _stockService;
        private readonly IDistanceService _distance;

        // Origen por defecto (por si Stock no nos manda warehouse_postal_code)
        private const string DEFAULT_ORIGIN_CPA = "H3500";

        public CalculateCost(IStockService stockService, IDistanceService distance)
        {
            _stockService = stockService;
            _distance = distance;
        }

        /// <summary>
        /// Devuelve un factor multiplicador para el costo por distancia
        /// según el tipo de transporte elegido.
        ///
        /// Relación con los días estimados (TransportService):
        /// - Air   → "1-3"  días  → muy rápido   → factor más alto
        /// - Road  → "3-7"  días  → estándar     → intermedio
        /// - Rail  → "5-10" días  → económico    → más bajo
        /// - Sea   → "15-30"días  → muy lento    → factor mínimo
        /// </summary>
        private float GetTransportMultiplier(TransportType type)
        {
            return type switch
            {
                TransportType.air  => 2.0f,  // más caro, llega rápido (1–3 días)
                TransportType.road => 1.5f,  // estándar (3–7 días)
                TransportType.rail => 1.2f,  // económico (5–10 días)
                TransportType.sea  => 1.0f,  // muy barato pero lento (15–30 días)
                _                  => 1.0f
            };
        }

        /// <summary>
        /// Devuelve SIEMPRE la cantidad de días MÁXIMA estimada
        /// para cada tipo de transporte.
        ///
        /// Valores alineados con TransportService:
        /// - Air   → "1-3"   → 3
        /// - Road  → "3-7"   → 7
        /// - Rail  → "5-10"  → 10
        /// - Sea   → "15-30" → 30
        /// </summary>
        public static int GetMaxEstimatedDays(TransportType type)
        {
            return type switch
            {
                TransportType.air  => 3,
                TransportType.road => 7,
                TransportType.rail => 10,
                TransportType.sea  => 30,
                _                  => 7    // fallback razonable
            };
        }

        public async Task<ShippingCostResponse> CalculateShippingCostAsync(CalculateCostRequest request)
        {
            float total_cost = 0f;
            List<ProductOutput> products_with_cost = new();
            //Si es null, usa 'road'.
            TransportType transporttype = request.transport_type ?? TransportType.road;
            // multiplicador según el transporte que pidió Compras en el request
            float transportMultiplier = GetTransportMultiplier(transporttype);

            foreach (var prod in request.products)
            {
                // 1. Obtener datos del producto desde Stock (mock o real)
                ProductDetail prod_detail = await _stockService.GetProductDetailAsync(prod);

                // 2. Determinar ORIGEN del envío para este producto
                var origin = string.IsNullOrEmpty(prod_detail.warehouse_postal_code)
                    ? DEFAULT_ORIGIN_CPA
                    : prod_detail.warehouse_postal_code;

                // 3. Calcular distancia ORIGEN → DESTINO
                float distance_km = (float)await _distance.GetDistanceKm(
                    origin,
                    request.delivery_address.postal_code
                );

                // 4. Peso y volumen totales del producto (teniendo en cuenta la cantidad)
                float total_weight_grs = prod_detail.weight * prod.quantity;

                float prod_volume_cm3 = prod_detail.length * prod_detail.width * prod_detail.height;
                float total_volume = prod_volume_cm3 * prod.quantity;

                // 5. Fórmula de ejemplo para el costo del producto
                float partial_cost =
                    total_weight_grs * 1.2f +
                    total_volume * 0.5f +
                    distance_km * 8.0f * transportMultiplier;

                partial_cost /= request.products.Count;

                // 6. Acumular resultados
                total_cost += partial_cost;
                products_with_cost.Add(new ProductOutput(prod.id, partial_cost));
            }

            // 🔹 NUEVO: calcular fecha estimada usando el máximo de días para ese transporte
            int estimated_days = GetMaxEstimatedDays(transporttype);
            DateTime estimated_delivery_at = DateTime.UtcNow.AddDays(estimated_days);

            var response = new ShippingCostResponse(
                currency: "ARS",
                total_cost: total_cost,
                transport_type: transporttype,
                products: products_with_cost,
                estimated_delivery_at: estimated_delivery_at
            );
            return response;
        }
    }
}
