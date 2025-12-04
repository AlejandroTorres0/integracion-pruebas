// En src/ApiDePapas.Application/Interfaces/IShippingService.cs (Fusionado)
using System;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Application.Interfaces
{
    public interface IShippingService
    {
        // Método de Creación (se mantiene)
        Task<CreateShippingResponse?> CreateNewShipping(CreateShippingRequest request);
        
        // MÉTODOS REINTRODUCIDOS de la rama desactualizada para permitir lectura/cancelación en el service
        Task<ShippingDetailResponse?> GetByIdAsync(int id);
        Task<CancelShippingResponse> CancelAsync(int id, DateTime whenUtc);
        Task<ShippingListResponse> List(int? userId,ShippingStatus? status,DateOnly? fromDate,DateOnly? toDate,int page,int limit);

        Task<bool> UpdateStatusAsync(int shippingId, UpdateStatusRequest request);
    }
}