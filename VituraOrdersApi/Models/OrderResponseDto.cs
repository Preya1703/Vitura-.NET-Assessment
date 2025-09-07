namespace VituraOrdersApi.Models
{
    public class OrderResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public IEnumerable<OrderItemsDto> Items { get; set; } = Enumerable.Empty<OrderItemsDto>();
    }
}
