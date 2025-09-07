using System.Diagnostics;

namespace VituraOrdersApi.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        private const string HeaderName = "x-correlation-id";
        private readonly RequestDelegate _next;
        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;
        public async Task InvokeAsync(HttpContext context)
        {
            var id = context.Request.Headers.TryGetValue(HeaderName, out var values) && !string.IsNullOrWhiteSpace(values)
                ? values.ToString()
                : Guid.NewGuid().ToString();

            context.Response.Headers[HeaderName] = id;
            context.Items["CorrelationId"] = id; // scope for logging filter
            await _next(context);
        }

    }
}
