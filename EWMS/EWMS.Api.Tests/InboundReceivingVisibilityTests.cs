using System.Net;
using System.Net.Http.Json;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Entities;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class InboundReceivingVisibilityTests
{
    private readonly SqlServerFixture _fixture;
    public InboundReceivingVisibilityTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task ManagerAndPurchasingOnlySeeReceivingStartedAfterStaffCreatesReceipt()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        string staffName, managerName, purchasingName;
        int otherInboundId;
        using (var context = _fixture.CreateContext())
        {
            staffName = (await context.Users.FindAsync(data.StaffId))!.UserName;
            managerName = (await context.Users.FindAsync(data.ManagerId))!.UserName;
            purchasingName = (await context.Users.FindAsync(data.PurchasingUserId))!.UserName;
            // Đơn khác cùng kho giúp kiểm tra cờ không bị ảnh hưởng bởi phiếu của đơn đầu.
            var otherInbound = new InboundRequest
            {
                WarehouseId = data.WarehouseId, RequestedByUserId = data.ManagerId,
                ApprovedByUserId = data.ManagerId, Status = "Approved", CreatedDate = DateTime.UtcNow
            };
            context.InboundRequestItems.Add(new InboundRequestItem
            {
                InboundRequest = otherInbound, ProductVariantId = data.VariantId, ExpectedQty = 2
            });
            await context.SaveChangesAsync();
            otherInboundId = otherInbound.Id;
        }

        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        string route = $"/api/inbound-requests/{data.InboundId}";
        foreach (string userName in new[] { managerName, purchasingName })
        {
            await AuthTestHelper.LoginAsync(client, userName);
            var detail = await client.GetFromJsonAsync<InboundRequestDetailDto>(route);
            Assert.Equal("Approved", detail!.Status);
            Assert.False(detail.HasGoodsReceipts); // Chưa có phiếu: giao diện không hiện tiến độ.
        }

        await AuthTestHelper.LoginAsync(client, staffName);
        var created = await client.PostAsJsonAsync("/api/goods-receipts",
            new CreateGoodsReceiptDto { InboundRequestId = data.InboundId });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = await created.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>();
        Assert.Equal("Draft", receipt!.Status);

        foreach (string userName in new[] { managerName, purchasingName })
        {
            await AuthTestHelper.LoginAsync(client, userName);
            var detail = await client.GetFromJsonAsync<InboundRequestDetailDto>(route);
            Assert.True(detail!.HasGoodsReceipts); // Có nháp đầu tiên: đã bắt đầu nhận hàng.
            var otherDetail = await client.GetFromJsonAsync<InboundRequestDetailDto>($"/api/inbound-requests/{otherInboundId}");
            Assert.False(otherDetail!.HasGoodsReceipts);
            var progress = await client.GetFromJsonAsync<InboundReceivingProgressDto>(route + "/receiving-progress");
            Assert.Equal(0, progress!.TotalReceivedQty); // Nháp chưa được tính đã nhận chính thức.
        }
        await app.StopAsync();
    }
}
