using System.ComponentModel.DataAnnotations;

namespace VituraOrdersApi.Models
{
    public sealed class OrdersQuery
    {
        public string? PharmacyId { get; set; } = string.Empty;
        public List<Status>? Status { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public string? Sort { get; init; }
        public string? Dir { get; init; }

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = Constants.DEFAULT_PAGE_START;

        [Range(1, 100)]
        public int PageSize { get; init; } = Constants.DEFAULT_PAGE_SIZE;
    }
}
