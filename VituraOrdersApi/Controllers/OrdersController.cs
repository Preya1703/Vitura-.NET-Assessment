using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VituraOrdersApi.Models;
using VituraOrdersApi.Services;

namespace VituraOrdersApi.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : Controller
    {
        private static readonly string[] AllowedSort = ["createdAt", "totalCents"];
        private static readonly string[] AllowedDir = ["asc", "desc"];

        private readonly IOrdersService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrdersService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<OrderResponseDto>> GetAsync(
            [FromQuery] string? pharmacyId,
            [FromQuery] List<Status>? status,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] string? sort = Constants.DEFAULT_SORT_BY,
            [FromQuery] string? dir = Constants.DEFAULT_DIRECTION,
            [FromQuery] int page = Constants.DEFAULT_PAGE_START,
            [FromQuery] int pageSize = Constants.DEFAULT_PAGE_SIZE,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var correlationId = HttpContext.Items["CorrelationId"] as string ?? "-";

            // Validate query params 
            if (page < 1)
                return BadRequest("page must be >= 1");

            if (pageSize is < Constants.MIN_PAGE_SIZE or > Constants.MAX_PAGE_SIZE)
                return BadRequest($"pageSize must be between {Constants.MIN_PAGE_SIZE} and {Constants.MAX_PAGE_SIZE}.");

            if (sort is not null && !AllowedSort.Contains(sort, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"sort must be one of: {string.Join(", ", AllowedSort)}");

            if (dir is not null && !AllowedDir.Contains(dir, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"dir must be one of: {string.Join(", ", AllowedDir)}");

            if (from.HasValue && to.HasValue && from.Value > to.Value)
                return BadRequest("from must be <= to");

            var result = await _orderService.GetAllAsync(ct, correlationId, pharmacyId, status, from, to, sort, dir, page, pageSize);

            sw.Stop();
            _logger.LogInformation(
                "GET /orders -> 200 cid={cid} ms={ms} page={page} pageSize={pageSize} total={total} items={items} sort={sort} dir={dir}",
                correlationId, sw.ElapsedMilliseconds, result.Page, result.PageSize, result.Total, result.Items.Count(), sort, dir);

            return Ok(result);
        }
    }
}