using Microsoft.AspNetCore.Mvc;
using VituraOrdersApi.Models;

namespace VituraOrdersApi.Services
{
    public interface IOrdersService
    {
        Task<OrderResponseDto> GetAllAsync(
            CancellationToken ct,
            string correlationId,
            string? pharmacyId,
            List<Status>? status,
            DateOnly? from,
            DateOnly? to,
            string? sort,
            string? dir,
            int page = Constants.DEFAULT_PAGE_START,
            int pageSize = Constants.DEFAULT_PAGE_SIZE);
    }
}
