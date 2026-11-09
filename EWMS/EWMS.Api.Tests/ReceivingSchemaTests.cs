using System.Text.Json;
using EWMS.Api.Common;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class ReceivingSchemaTests
{
    private readonly SqlServerFixture _fixture;
    public ReceivingSchemaTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task DatabaseConstraintsRejectDuplicateOfficialSerialAndStockKey()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            context.Stocks.Add(new Stock { ProductVariantId = data.VariantId, BinId = data.BinId, OnHandQty = 2 });
            context.SerialNumbers.Add(new SerialNumber { ProductVariantId = data.VariantId, CurrentBinId = data.BinId, SerialCode = "UNIQUE-" + data.Suffix, CurrentStatus = "IN_STOCK" });
            await context.SaveChangesAsync();
        }
        using (var context = _fixture.CreateContext())
        {
            context.Stocks.Add(new Stock { ProductVariantId = data.VariantId, BinId = data.BinId, OnHandQty = 2 });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
        using (var context = _fixture.CreateContext())
        {
            context.SerialNumbers.Add(new SerialNumber { ProductVariantId = data.SecondVariantId, CurrentBinId = data.OtherBinId, SerialCode = "UNIQUE-" + data.Suffix, CurrentStatus = "IN_STOCK" });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    public void GR16_JsonCannotInjectReceivedQuantity()
    {
        const string json = """{"expectedVersion":"AAAA","items":[{"itemId":1,"binId":1,"receivedQty":500}]}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UpdateGoodsReceiptDto>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    [Fact]
    public async Task GR36_ProgressCannotHideOneSkuExcessWithAnotherSkuShortage()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using var context = _fixture.CreateContext();
        context.InboundRequestItems.Add(new InboundRequestItem
        {
            InboundRequestId = data.InboundId, ProductVariantId = data.SecondVariantId, ExpectedQty = 2
        });
        var receipt = new GoodsReceipt
        {
            InboundRequestId = data.InboundId, ReceivedByUserId = data.StaffId,
            ReceivedDate = DateTime.UtcNow, Status = "Approved"
        };
        context.GoodsReceipts.Add(receipt);
        await context.SaveChangesAsync();
        context.GoodsReceiptItems.Add(new GoodsReceiptItem
        {
            GoodsReceiptId = receipt.Id, ProductVariantId = data.VariantId,
            BinId = data.BinId, ReceivedQty = 4
        });
        await context.SaveChangesAsync();
        var service = new InboundReceivingService(context);
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => service.RefreshReceivingProgressAsync(data.InboundId));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal("Approved", (await context.InboundRequests.FindAsync(data.InboundId))!.Status);
    }
}
