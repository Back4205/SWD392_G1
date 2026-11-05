using System.Net;
using System.Net.Http.Json;
using EWMS.Api.Common;
using EWMS.Api.Controllers;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Entities;
using EWMS.Api.Interfaces;
using EWMS.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class GoodsReceiptLookupTests
{
    private readonly SqlServerFixture _fixture;

    public GoodsReceiptLookupTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SessionReturnsDatabaseIdentityAndOnlyAssignedWarehouses()
    {
        var data = await _fixture.SeedAsync();
        using var session = new ReceivingSession(_fixture);
        var staff = await session.Receipts.GetSessionAsync(data.StaffId);
        Assert.Equal(data.StaffId, staff.UserId);
        Assert.Equal("staff", staff.FullName);
        Assert.Equal("Warehouse Staff", staff.Role);
        Assert.Equal(new[] { data.WarehouseId }, staff.WarehouseIds);
        var manager = await session.Receipts.GetSessionAsync(data.ManagerId);
        Assert.Equal("Manager", manager.Role);
        Assert.Equal(new[] { data.WarehouseId }, manager.WarehouseIds);
        var other = await session.Receipts.GetSessionAsync(data.OtherUserId);
        Assert.Equal(new[] { data.OtherWarehouseId }, other.WarehouseIds);
    }

    [Fact]
    public async Task SessionRejectsMissingInactiveAndUnsupportedRoles()
    {
        var data = await _fixture.SeedAsync();
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.GetSessionAsync(int.MaxValue), "GR_USER_INACTIVE", 403);
        await Error(() => session.Receipts.GetSessionAsync(data.InactiveUserId), "GR_USER_INACTIVE", 403);
        await Error(() => session.Receipts.GetSessionAsync(data.PurchasingUserId), "GR_ROLE", 403);
    }

    [Fact]
    public async Task InboundLookupFiltersStateWarehouseAndFullyReceivedRequests()
    {
        var data = await _fixture.SeedAsync();
        int partialId;
        using (var context = _fixture.CreateContext())
        {
            partialId = await AddInboundAsync(context, data, "PartiallyReceived", data.WarehouseId);
            int fullId = await AddInboundAsync(context, data, "Approved", data.WarehouseId);
            await AddInboundAsync(context, data, "Pending", data.WarehouseId);
            await AddInboundAsync(context, data, "Rejected", data.WarehouseId);
            await AddInboundAsync(context, data, "Completed", data.WarehouseId);
            await AddInboundAsync(context, data, "Approved", data.OtherWarehouseId);
            context.GoodsReceipts.AddRange(
                Receipt(data, partialId, "Approved", 3),
                Receipt(data, partialId, "PendingApproval", 6),
                Receipt(data, partialId, "Draft", 4),
                Receipt(data, partialId, "Rejected", 2),
                Receipt(data, fullId, "Approved", 10));
            await context.SaveChangesAsync();
        }
        using var session = new ReceivingSession(_fixture);
        var results = await session.Receipts.GetReceivableInboundsAsync(data.StaffId);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, x => x.Id == data.InboundId);
        var partial = Assert.Single(results, x => x.Id == partialId);
        Assert.Equal(data.WarehouseId, partial.WarehouseId);
        Assert.Equal("A-", partial.WarehouseName);
        Assert.Equal("PartiallyReceived", partial.Status);
        var line = Assert.Single(partial.Lines);
        Assert.Equal(data.VariantId, line.ProductVariantId);
        Assert.Equal(data.SKU, line.SKU);
        Assert.Equal(10, line.ExpectedQty);
        Assert.Equal(3, line.ReceivedQty);
        Assert.Equal(7, line.RemainingQty);
    }

    [Fact]
    public async Task InboundLookupRejectsManagerAndDoesNotExposeOtherWarehouse()
    {
        var data = await _fixture.SeedAsync();
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.GetReceivableInboundsAsync(data.ManagerId), "GR_ROLE", 403);
        await Error(() => session.Receipts.GetReceivableInboundsAsync(data.InactiveUserId), "GR_USER_INACTIVE", 403);
        Assert.Empty(await session.Receipts.GetReceivableInboundsAsync(data.OtherUserId));
    }

    [Fact]
    public async Task InactiveWarehouseIsNotSelectableAndBinsCannotBeRead()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            (await context.Warehouses.FindAsync(data.WarehouseId))!.Status = "Inactive";
            await context.SaveChangesAsync();
        }
        using var session = new ReceivingSession(_fixture);
        Assert.Empty(await session.Receipts.GetReceivableInboundsAsync(data.StaffId));
        await Error(() => session.Receipts.GetBinsAsync(data.WarehouseId, data.StaffId), "GR_WAREHOUSE_INACTIVE", 409);
    }

    [Fact]
    public async Task BinLookupUsesPhysicalOnHandWeightAndKeepsQueriesReadOnly()
    {
        var data = await _fixture.SeedAsync();
        using (var context = _fixture.CreateContext())
        {
            context.Stocks.AddRange(
                new Stock { ProductVariantId = data.VariantId, BinId = data.BinId, OnHandQty = 5, ReservedQty = 4 },
                new Stock { ProductVariantId = data.SecondVariantId, BinId = data.BinId, OnHandQty = 2, ReservedQty = 0 });
            await context.SaveChangesAsync();
        }
        using var session = new ReceivingSession(_fixture);
        var bins = await session.Receipts.GetBinsAsync(data.WarehouseId, data.StaffId);
        Assert.Equal(2, bins.Count);
        Assert.DoesNotContain(bins, x => x.Id == data.OtherBinId);
        var bin = Assert.Single(bins, x => x.Id == data.BinId);
        Assert.Equal("BIN-" + data.Suffix, bin.BinCode);
        Assert.Equal("R-" + data.Suffix, bin.RackCode);
        Assert.Equal(1000m, bin.MaxWeightKg);
        Assert.Equal(984m, bin.AvailableWeightKg); // 1000 - (5 * 2 + 2 * 3), Reserved vẫn chiếm chỗ.
        Assert.Equal(1000m, Assert.Single(bins, x => x.Id == data.SecondBinId).AvailableWeightKg);
        Assert.Equal(2, (await session.Receipts.GetBinsAsync(data.WarehouseId, data.ManagerId)).Count);
        using var verify = _fixture.CreateContext();
        var stock = await verify.Stocks.SingleAsync(x => x.BinId == data.BinId && x.ProductVariantId == data.VariantId);
        Assert.Equal(5, stock.OnHandQty);
        Assert.Equal(4, stock.ReservedQty);
        Assert.False(await verify.GoodsReceipts.AnyAsync(x => x.InboundRequestId == data.InboundId));
    }

    [Fact]
    public async Task BinLookupChecksActorAndWarehouseBeforeReturningLocations()
    {
        var data = await _fixture.SeedAsync();
        using var session = new ReceivingSession(_fixture);
        await Error(() => session.Receipts.GetBinsAsync(data.WarehouseId, data.OtherUserId), "MSG56", 403);
        await Error(() => session.Receipts.GetBinsAsync(data.OtherWarehouseId, data.StaffId), "MSG56", 403);
        await Error(() => session.Receipts.GetBinsAsync(data.WarehouseId, data.PurchasingUserId), "GR_ROLE", 403);
        await Error(() => session.Receipts.GetBinsAsync(data.WarehouseId, data.InactiveUserId), "GR_USER_INACTIVE", 403);
    }

    [Fact]
    public async Task HttpLookupRoutesAuthenticateAndReturnTheFrontendContract()
    {
        var data = await _fixture.SeedAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
            ApplicationName = typeof(GoodsReceiptController).Assembly.GetName().Name
        });
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoodsReceipts:UseDevelopmentUserHeader"] = "true"
        });
        builder.Services.AddDbContext<EWMSDbContext>(options => options.UseSqlServer(_fixture.ConnectionString));
        builder.Services.AddControllers().AddApplicationPart(typeof(GoodsReceiptController).Assembly);
        builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        builder.Services.AddScoped<GoodsReceiptService>();
        builder.Services.AddScoped<SerialNumberService>();
        builder.Services.AddScoped<GoodsReceiptApprovalService>();
        builder.Services.AddScoped<IStockService, MockStockService>();
        builder.Services.AddScoped<IInboundService, InboundReceivingService>();
        await using var app = builder.Build();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        foreach (string route in new[] { "session", "lookups/inbounds", "lookups/bins?warehouseId=" + data.WarehouseId })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/goods-receipts/" + route)).StatusCode);
        }
        client.DefaultRequestHeaders.Add("X-Development-UserId", data.StaffId.ToString());
        var actor = await client.GetFromJsonAsync<GoodsReceiptSessionDto>("/api/goods-receipts/session");
        Assert.Equal(data.StaffId, actor!.UserId);
        Assert.Equal("Warehouse Staff", actor.Role);
        var inbounds = await client.GetFromJsonAsync<List<ReceivableInboundDto>>("/api/goods-receipts/lookups/inbounds");
        Assert.Equal(data.InboundId, Assert.Single(inbounds!).Id);
        var bins = await client.GetFromJsonAsync<List<ReceiptBinLookupDto>>("/api/goods-receipts/lookups/bins?warehouseId=" + data.WarehouseId);
        Assert.Equal(2, bins!.Count);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/goods-receipts/lookups/bins?warehouseId=" + data.OtherWarehouseId)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Development-UserId");
        client.DefaultRequestHeaders.Add("X-Development-UserId", data.ManagerId.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/goods-receipts/lookups/inbounds")).StatusCode);
        await app.StopAsync();
    }

    private static GoodsReceipt Receipt(TestData data, int inboundId, string status, int quantity)
    {
        return new GoodsReceipt
        {
            InboundRequestId = inboundId,
            ReceivedByUserId = data.StaffId,
            ReceivedDate = DateTime.UtcNow,
            Status = status,
            Items = new List<GoodsReceiptItem>
            {
                new GoodsReceiptItem { ProductVariantId = data.VariantId, BinId = data.BinId, ReceivedQty = quantity }
            }
        };
    }

    private static async Task<int> AddInboundAsync(EWMSDbContext context, TestData data, string status, int warehouseId)
    {
        var inbound = new InboundRequest
        {
            WarehouseId = warehouseId,
            RequestedByUserId = data.PurchasingUserId,
            Status = status,
            CreatedDate = DateTime.UtcNow
        };
        context.InboundRequestItems.Add(new InboundRequestItem
        {
            InboundRequest = inbound,
            ProductVariantId = data.VariantId,
            ExpectedQty = 10
        });
        await context.SaveChangesAsync();
        return inbound.Id;
    }

    private static async Task Error(Func<Task> operation, string code, int status)
    {
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(operation);
        Assert.Equal(code, error.Code);
        Assert.Equal(status, error.StatusCode);
    }
}
