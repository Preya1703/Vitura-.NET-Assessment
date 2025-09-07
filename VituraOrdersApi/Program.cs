using System.Text.Json;
using System.Text.Json.Serialization;
using VituraOrdersApi.Middleware;
using VituraOrdersApi.Models;
using VituraOrdersApi.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ReviewOptions>(builder.Configuration.GetSection("Review"));

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IList<Order>>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var jsonPath = Path.Combine(env.ContentRootPath, "sample-orders.json");
    if (!File.Exists(jsonPath))
    {
        Console.WriteLine($"[WARN] sample-orders.json not found at {jsonPath}. Using empty dataset.");
        return new List<Order>();
    }

    using var stream = File.OpenRead(jsonPath);
    var orders = JsonSerializer.Deserialize<List<Order>>(stream, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    }) ?? new List<Order>();

    return orders;
});

builder.Services.AddScoped<IOrdersService, OrderService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
