using FluentAssertions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VituraOrdersApi.Models;
using VituraOrdersApi.Services;

namespace VituraOrdersApiTests
{
    public class NeedsReviewFlagTest
    {
        private static OrderService CreateService(IList<Order> seed, int thresholdCents = 100)
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderService>.Instance;
            var opts = Options.Create(new ReviewOptions { DailyOrderThresholdCents = thresholdCents });
            return new OrderService(logger, seed, opts);
        }

        [Fact]
        public async Task NeedsReview_flag_applies_against_config_threshold()
        {
            var now = DateTimeOffset.UtcNow;
            var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), PharmacyId = "ph1", Status = Status.Pending, CreatedAt = now, TotalCents = 50,  ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
            new() { Id = Guid.NewGuid(), PharmacyId = "ph1", Status = Status.Pending, CreatedAt = now, TotalCents = 150, ItemCount = 1, PaymentMethod = PaymentMethod.Card, DeliveryType = DeliveryType.Standard },
        };
            var svc = CreateService(orders, thresholdCents: 100);
    
            var res = await svc.GetAllAsync(CancellationToken.None, correlationId: "test", pharmacyId: null,
                                            status: null, from: null, to: null,
                                            sort: "createdAt", dir: "desc", page: 1, pageSize: 10);

            res.Items.Should().HaveCount(2);
            var flags = res.Items.Select(i => (i.TotalCents, i.NeedsReview)).ToArray();
            flags.Should().ContainEquivalentOf((150, true));
            flags.Should().ContainEquivalentOf((50, false));
        }
    }
}
