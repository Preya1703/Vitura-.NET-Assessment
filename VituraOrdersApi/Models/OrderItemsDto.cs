namespace VituraOrdersApi.Models
{
    public class OrderItemsDto
    {
        public Guid Id { get; init; }
        public string PharmacyId { get; init; } = string.Empty;
        public Status Status { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public int TotalCents { get; init; }
        public int ItemCount { get; init; }
        public PaymentMethod PaymentMethod { get; init; } 
        public DeliveryType DeliveryType { get; init; } 
        public string? Notes { get; init; }
        public bool NeedsReview { get; init; }
    }
}
