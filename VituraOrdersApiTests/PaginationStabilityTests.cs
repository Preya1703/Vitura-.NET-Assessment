using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VituraOrdersApi.Models;
using VituraOrdersApi.Services;

namespace VituraOrdersApi.Tests;

public sealed class OrderServiceTests
{

    private static OrderService CreateService(IList<Order> seed, int thresholdCents = 100)
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderService>.Instance;
        var opts = Options.Create(new ReviewOptions { DailyOrderThresholdCents = thresholdCents });
        return new OrderService(logger, seed, opts);
    }

    

    [Fact]
    public async Task Pagination_returns_stable_results_for_same_query()
    {
        var baseTime = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var orders = Enumerable.Range(1, 50).Select(i => new Order
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{i.ToString("D12")}"),
            PharmacyId = "ph1",
            Status = i % 2 == 0 ? Status.Pending : Status.Shipped,
            CreatedAt = baseTime.AddMinutes(i % 5), // repeat timestamps
            TotalCents = 1000 + (i % 3),            // repeat totals
            ItemCount = 1,
            PaymentMethod = PaymentMethod.Card,
            DeliveryType = DeliveryType.Standard,
            Notes = null
        }).ToList();
        var svc = CreateService(orders);

        var page = 2;
        var pageSize = 10;
        var args = (ct: CancellationToken.None, cid: "test", pid: (string?)null, status: (List<Status>?)null,
                    from: (DateOnly?)null, to: (DateOnly?)null, sort: "createdAt", dir: "desc");

        var r1 = await svc.GetAllAsync(args.ct, args.cid, args.pid, args.status, args.from, args.to, args.sort, args.dir, page, pageSize);
        var r2 = await svc.GetAllAsync(args.ct, args.cid, args.pid, args.status, args.from, args.to, args.sort, args.dir, page, pageSize);

        r1.Items.Select(x => x.Id).Should().Equal(r2.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task Filters_by_pharmacy_status_and_date_range_correctly()
    {
        
        var d1 = new DateTimeOffset(2025, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var d2 = new DateTimeOffset(2025, 9, 2, 10, 0, 0, TimeSpan.Zero);
        var d3 = new DateTimeOffset(2025, 9, 3, 10, 0, 0, TimeSpan.Zero);

        var a1 = Guid.NewGuid(); 
        var a2 = Guid.NewGuid(); 
        var a3 = Guid.NewGuid();
        var a4 = Guid.NewGuid(); 
        var a5 = Guid.NewGuid(); 

        var orders = new List<Order>
        {
            new() { Id = a1, PharmacyId = "ph1", Status = Status.Pending,   CreatedAt = d1, TotalCents = 1000, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
            new() { Id = a2, PharmacyId = "ph1", Status = Status.Shipped,   CreatedAt = d2, TotalCents = 1100, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
            new() { Id = a3, PharmacyId = "ph1", Status = Status.Cancelled, CreatedAt = d1, TotalCents = 1200, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
            new() { Id = a4, PharmacyId = "ph2", Status = Status.Pending,   CreatedAt = d2, TotalCents = 1300, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
            new() { Id = a5, PharmacyId = "ph1", Status = Status.Pending,   CreatedAt = d3.AddDays(5), TotalCents = 900, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
        };

        var svc = CreateService(orders);

        // Filter: ph1, statuses Pending or Shipped, between 2025-09-01 and 2025-09-03 inclusive
        var from = new DateOnly(2025, 9, 1);
        var to = new DateOnly(2025, 9, 3);
        var statuses = new List<Status> { Status.Pending, Status.Shipped };

        var res = await svc.GetAllAsync(CancellationToken.None, "cid", "ph1", statuses, from, to, sort: "createdAt", dir: "asc", page: 1, pageSize: 50);

        res.Total.Should().Be(2);           
        res.Items.Should().HaveCount(2);
        res.Items.Select(i => i.Id).Should().BeEquivalentTo(new[] { a1, a2 });

        res.Items.All(i => i.CreatedAt.Date >= from.ToDateTime(TimeOnly.MinValue).Date &&
                           i.CreatedAt.Date <= to.ToDateTime(TimeOnly.MinValue).Date).Should().BeTrue();
        res.Items.Select(i => i.Status).Should().OnlyContain(s => s == Status.Pending || s == Status.Shipped);
    }
}

