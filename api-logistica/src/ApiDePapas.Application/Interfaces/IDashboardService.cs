using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiDePapas.Application.DTOs;
using ApiDePapas.Domain.Entities;

namespace ApiDePapas.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<IEnumerable<DashboardShipmentDto>> GetDashboardShipmentsAsync(
            int page,
            int pageSize,
            string? id,
            string? city,
            string? status,
            DateTime? startDate,
            DateTime? endDate);

        Task<int> GetTotalDashboardShipmentsCountAsync(
            string? id,
            string? city,
            string? status,
            DateTime? startDate,
            DateTime? endDate);

        Task UpdateShipmentStatusAsync(int id, ShippingStatus newStatus, string? message = null);
    }
}
