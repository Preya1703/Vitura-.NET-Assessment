using Microsoft.Extensions.Options;
using VituraOrdersApi.Models;

namespace VituraOrdersApi.Services
{
    public class OrderService : IOrdersService
    {
        private readonly IList<Order> _orders;
        private readonly ILogger<OrderService> _logger;
        private readonly ReviewOptions _review;

        public OrderService(ILogger<OrderService> logger,
                            IList<Order> orders,
                            IOptions<ReviewOptions> review)
        {
            _orders = orders;
            _logger = logger;
            _review = review.Value;
        }

        public Task<OrderResponseDto> GetAllAsync(
            CancellationToken ct,
            string correlationId,
            string? pharmacyId,
            List<Status>? status,
            DateOnly? from,
            DateOnly? to,
            string? sort,
            string? dir,
            int page = Constants.DEFAULT_PAGE_START,
            int pageSize = Constants.DEFAULT_PAGE_SIZE)
        {
            // Filtering
            IEnumerable<Order> q = _orders;

            if (!string.IsNullOrWhiteSpace(pharmacyId))
                q = q.Where(o => string.Equals(o.PharmacyId, pharmacyId, StringComparison.OrdinalIgnoreCase));

            if (status?.Count > 0)
            {
                var set = new HashSet<Status>(status);
                q = q.Where(o => set.Contains(o.Status));
            }

            if (from.HasValue)
                q = q.Where(o => o.CreatedAt.Date >= from.Value.ToDateTime(TimeOnly.MinValue));
            if (to.HasValue)
                q = q.Where(o => o.CreatedAt.Date <= to.Value.ToDateTime(TimeOnly.MinValue));

            // Sort
            var asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            var byTotal = string.Equals(sort, "totalCents", StringComparison.OrdinalIgnoreCase);

            IOrderedEnumerable<Order> ordered = (byTotal, asc) switch
            {
                (true, true) => q.OrderBy(o => o.TotalCents).ThenBy(o => o.CreatedAt).ThenBy(o => o.Id),
                (true, false) => q.OrderByDescending(o => o.TotalCents).ThenByDescending(o => o.CreatedAt).ThenBy(o => o.Id),
                (false, true) => q.OrderBy(o => o.CreatedAt).ThenBy(o => o.Id),
                _ => q.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id)
            };

            var filtered = ordered.ToArray();
            var total = filtered.Length;

            var skip = (page - 1) * pageSize;
            if (skip > total) skip = total;

            var pageItems = filtered.Skip(skip).Take(pageSize);

            // Map + business rule (needsReview)
            var threshold = _review.DailyOrderThresholdCents;
            var items = pageItems.Select(o => new OrderItemsDto
            {
                Id = o.Id,
                PharmacyId = o.PharmacyId,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                TotalCents = o.TotalCents,
                ItemCount = o.ItemCount,
                PaymentMethod = o.PaymentMethod,
                DeliveryType = o.DeliveryType,
                Notes = o.Notes,
                NeedsReview = o.TotalCents > threshold
            }).ToArray();

            var result = new OrderResponseDto
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };

            return Task.FromResult(result);
        }
    }
}