using ApiDePapas.Domain.Entities;
using ApiDePapas.Application.DTOs;
using System.Collections.Generic;

namespace ApiDePapas.Application.Services
{
    // Nombre de la clase ajustado
    public class TransportService
    {
        public TransportMethodsResponse GetAll()
        {
            return new TransportMethodsResponse
            {
                transport_methods = new List<TransportMethods>
                {
                    new() { type = TransportType.air, name = "Air Freigth", estimated_days = "1-3" },
                    new() { type = TransportType.road, name = "Road Transport", estimated_days = "3-7" },
                    new() { type = TransportType.rail, name = "Rail Freight", estimated_days = "5-10" },
                    new() { type = TransportType.sea, name = "Sea Freight", estimated_days = "15-30" }
                }
            };
        }
    }
}