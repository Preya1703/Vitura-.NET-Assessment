namespace VituraOrdersApi.Models
{
    public enum PaymentMethod { Card = 1, HICAPS = 2, BPAY = 3, Invoice = 4 }
    public enum Status { Pending = 1, Packed = 2, Delivered = 3, Shipped = 4, Cancelled = 5 }
    public enum DeliveryType { ClickAndCollect = 1, Standard = 2, Express = 3 }

    public static class Constants
    {
        public const string DEFAULT_SORT_BY = "createdAt";
        public const string DEFAULT_DIRECTION = "desc";
        public const int DEFAULT_PAGE_START = 1;
        public const int DEFAULT_PAGE_SIZE = 20;
        public const int MIN_PAGE_SIZE = 1;     
        public const int MAX_PAGE_SIZE = 100;
        public const string X_CORRELATION_ID = "X-Correlation-ID";
    }

    public sealed class ReviewOptions
    {
        public int DailyOrderThresholdCents { get; set; } = 100_00;
    }
}