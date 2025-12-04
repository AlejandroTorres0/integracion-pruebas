using System.Threading.Tasks;
using ApiDePapas.Domain.Entities;
using ApiDePapas.Application.Interfaces;

/*
 * Servicio de Stock FAKE para el sandbox.
 * En el proyecto real, acá se harían las llamadas HTTP al módulo de Stock.
 */

namespace ApiDePapas.Application.Services
{
    public class FakeStockService : IStockService
    {
        public Task<ProductDetail> GetProductDetailAsync(ProductQty product)
        {
            ProductDetail detail;

            switch (product.id)
            {
                case 1:
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 10,    // liviano
                        length = 10,
                        width  = 5,
                        height = 2,
                        warehouse_postal_code = "H3500AAA" // Resistencia (Chaco)
                    };
                    break;

                case 2:
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 50,    // pesado
                        length = 20,
                        width  = 10,
                        height = 5,
                        warehouse_postal_code = "C1000AAA" // CABA
                    };
                    break;

                case 3:
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 5,     // pequeño
                        length = 5,
                        width  = 5,
                        height = 5,
                        warehouse_postal_code = "X5000AAA" // Córdoba
                    };
                    break;

                case 4:
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 30,    // intermedio
                        length = 15,
                        width  = 10,
                        height = 4,
                        warehouse_postal_code = "B1708AAA" // zona GBA oeste (ejemplo)
                    };
                    break;

                case 5:
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 15,
                        length = 12,
                        width  = 8,
                        height = 6,
                        warehouse_postal_code = "N3300AAA" // Posadas (Misiones)
                    };
                    break;

                default:
                    // Fallback por si llega un id que no está configurado
                    detail = new ProductDetail
                    {
                        id = product.id,
                        weight = 20,
                        length = 10,
                        width  = 5,
                        height = 2,
                        warehouse_postal_code = "H3500AAA" // origen por defecto
                    };
                    break;
            }

            return Task.FromResult(detail);
        }
    }
}
