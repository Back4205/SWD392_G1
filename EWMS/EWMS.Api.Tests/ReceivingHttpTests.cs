using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json;
using EWMS.Api.Controllers;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.Interfaces;
using EWMS.Api.Repositories;
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
public class ReceivingHttpTests
{
    private readonly SqlServerFixture _fixture;
    public ReceivingHttpTests(SqlServerFixture fixture) { _fixture = fixture; }

    private async Task<WebApplication> StartAsync(string environment = "Development")
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
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
        builder.Services.AddScoped<IInboundRequestRepository, InboundRequestRepository>();
        builder.Services.AddScoped<IInboundRequestService, InboundRequestService>();
        var app = builder.Build();
        app.UseAuthorization();
        app.MapControllers();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        return app;
    }

    private static HttpClient Client(WebApplication app)
    {
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    [Fact]
    public async Task AllEightRoutesBindValidateAuthorizeAndRunTheWorkflow()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        await using var app = await StartAsync();
        using var client = Client(app);
        void User(int id)
        {
            client.DefaultRequestHeaders.Remove("X-Development-UserId");
            client.DefaultRequestHeaders.Add("X-Development-UserId", id.ToString());
        }
        async Task Error(HttpResponseMessage response, HttpStatusCode expected, string? code = null)
        {
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {body}");
            if (code != null)
            {
                using var json = JsonDocument.Parse(body);
                Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
            }
        }
        async Task<T> Read<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
        {
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {body}");
            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }

        await Error(await client.GetAsync("/api/goods-receipts"), HttpStatusCode.Unauthorized, "GR_UNAUTHENTICATED");
        User(data.StaffId);
        await Error(await client.PostAsJsonAsync("/api/goods-receipts", new { inboundRequestId = 0 }), HttpStatusCode.BadRequest);
        await Error(await client.PostAsJsonAsync("/api/goods-receipts", new { inboundRequestId = data.InboundId, receivedByUserId = data.ManagerId }), HttpStatusCode.BadRequest);
        var receipt = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync("/api/goods-receipts",
            new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }), HttpStatusCode.Created);
        Assert.Equal("Draft", receipt.Status);
        string route = "/api/goods-receipts/" + receipt.Id;
        receipt = await Read<GoodsReceiptDetailDto>(await client.GetAsync(route));
        var item = receipt.Items.Single();
        await Error(await client.PostAsJsonAsync(route + "/items/" + item.Id + "/serials",
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "HTTP", SKU = "WRONG" }), HttpStatusCode.BadRequest);
        var scan = await Read<ReceiptSerialDto>(await client.PostAsJsonAsync(route + "/items/" + item.Id + "/serials",
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "HTTP-" + data.Suffix, SKU = data.SKU }));
        await Error(await client.PutAsJsonAsync(route, new
        {
            expectedVersion = scan.Version,
            items = new[] { new { itemId = item.Id, binId = data.BinId, receivedQty = 500 } }
        }), HttpStatusCode.BadRequest);
        await Error(await client.PutAsJsonAsync(route, new { expectedVersion = scan.Version, items = new object?[] { null } }), HttpStatusCode.BadRequest);
        receipt = await Read<GoodsReceiptDetailDto>(await client.PutAsJsonAsync(route, new UpdateGoodsReceiptDto
        {
            ExpectedVersion = scan.Version,
            Items = new List<UpdateReceiptBinDto> { new() { ItemId = item.Id, BinId = data.BinId } }
        }));
        receipt = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync(route + "/submit", new ReceiptVersionDto { ExpectedVersion = receipt.Version }));
        using (var context = _fixture.CreateContext())
        {
            Assert.False(await context.Stocks.AnyAsync(x => x.ProductVariantId == data.VariantId));
            Assert.False(await context.SerialNumbers.AnyAsync(x => x.ProductVariantId == data.VariantId));
        }
        await Error(await client.PostAsJsonAsync(route + "/approve", new ReceiptVersionDto { ExpectedVersion = receipt.Version }), HttpStatusCode.Forbidden);
        User(data.ManagerId);
        var pending = await Read<GoodsReceiptListDto>(await client.GetAsync("/api/goods-receipts?status=PendingApproval"));
        Assert.Contains(pending.Items, x => x.Id == receipt.Id);
        receipt = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync(route + "/approve", new ReceiptVersionDto { ExpectedVersion = receipt.Version }));
        Assert.Equal("Approved", receipt.Status);
        await Error(await client.PostAsJsonAsync(route + "/approve", new ReceiptVersionDto { ExpectedVersion = receipt.Version }), HttpStatusCode.Conflict);

        User(data.StaffId);
        var second = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync("/api/goods-receipts",
            new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }), HttpStatusCode.Created);
        string secondRoute = "/api/goods-receipts/" + second.Id;
        int secondItem = second.Items.Single().Id;
        scan = await Read<ReceiptSerialDto>(await client.PostAsJsonAsync(secondRoute + "/items/" + secondItem + "/serials",
            new AddReceiptSerialDto { ExpectedVersion = second.Version, SerialCode = "REJECT-" + data.Suffix, SKU = data.SKU }));
        second = await Read<GoodsReceiptDetailDto>(await client.PutAsJsonAsync(secondRoute,
            new UpdateGoodsReceiptDto { ExpectedVersion = scan.Version, Items = new List<UpdateReceiptBinDto> { new() { ItemId = secondItem, BinId = data.BinId } } }));
        second = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync(secondRoute + "/submit", new ReceiptVersionDto { ExpectedVersion = second.Version }));
        User(data.ManagerId);
        await Error(await client.PostAsJsonAsync(secondRoute + "/reject", new RejectGoodsReceiptDto { ExpectedVersion = second.Version, Reason = " " }), HttpStatusCode.BadRequest);
        second = await Read<GoodsReceiptDetailDto>(await client.PostAsJsonAsync(secondRoute + "/reject",
            new RejectGoodsReceiptDto { ExpectedVersion = second.Version, Reason = "Thiếu phụ kiện" }));
        Assert.Equal("Rejected", second.Status);
        Assert.Equal("Thiếu phụ kiện", second.RejectionReason);
        using (var context = _fixture.CreateContext())
            Assert.Equal(1, (await context.Stocks.SingleAsync(x => x.ProductVariantId == data.VariantId)).OnHandQty);
        await app.StopAsync();
    }

    [Fact]
    public async Task GR38_ProductionControllerNeverTrustsDevelopmentHeader()
    {
        var data = await _fixture.SeedAsync();
        await using var app = await StartAsync("Production");
        using var client = Client(app);
        client.DefaultRequestHeaders.Add("X-Development-UserId", data.ManagerId.ToString());
        var response = await client.GetAsync("/api/goods-receipts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task GR37_ScanAndBinSurviveStoppingAndStartingTheApi()
    {
        var data = await _fixture.SeedAsync();
        int receiptId;
        string version;
        await using (var firstApp = await StartAsync())
        {
            using var client = Client(firstApp);
            client.DefaultRequestHeaders.Add("X-Development-UserId", data.StaffId.ToString());
            var created = await client.PostAsJsonAsync("/api/goods-receipts",
                new CreateGoodsReceiptDto { InboundRequestId = data.InboundId });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var receipt = (await created.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!;
            receiptId = receipt.Id;
            var scanned = await client.PostAsJsonAsync($"/api/goods-receipts/{receiptId}/items/{receipt.Items.Single().Id}/serials",
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SerialCode = "RESTART-" + data.Suffix, SKU = data.SKU });
            Assert.Equal(HttpStatusCode.OK, scanned.StatusCode);
            var scan = (await scanned.Content.ReadFromJsonAsync<ReceiptSerialDto>())!;
            var saved = await client.PutAsJsonAsync("/api/goods-receipts/" + receiptId,
                new UpdateGoodsReceiptDto
                {
                    ExpectedVersion = scan.Version,
                    Items = new List<UpdateReceiptBinDto> { new() { ItemId = receipt.Items.Single().Id, BinId = data.BinId } }
                });
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
            version = (await saved.Content.ReadFromJsonAsync<GoodsReceiptDetailDto>())!.Version;
            await firstApp.StopAsync();
        }
        await using var secondApp = await StartAsync();
        using var restartedClient = Client(secondApp);
        restartedClient.DefaultRequestHeaders.Add("X-Development-UserId", data.StaffId.ToString());
        var reloaded = await restartedClient.GetFromJsonAsync<GoodsReceiptDetailDto>("/api/goods-receipts/" + receiptId);
        Assert.NotNull(reloaded);
        Assert.Equal(version, reloaded.Version);
        Assert.Equal(data.BinId, reloaded.Items.Single().BinId);
        Assert.Equal(1, reloaded.Items.Single().ReceivedQty);
        Assert.Equal("RESTART-" + data.Suffix, reloaded.Items.Single().Serials.Single().SerialCode);
        var submit = await restartedClient.PostAsJsonAsync($"/api/goods-receipts/{receiptId}/submit",
            new ReceiptVersionDto { ExpectedVersion = version });
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        await secondApp.StopAsync();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task GR38_ActualProgramRefusesToStartWithUnintegratedStockMock(string environment)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(typeof(GoodsReceiptController).Assembly.Location);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["GoodsReceipts__UseDevelopmentUserHeader"] = "true";
        start.Environment["ConnectionStrings__EWMSDatabase"] = _fixture.ConnectionString;
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await process.WaitForExitAsync(timeout.Token);
            string text = await output + await errors;
            Assert.NotEqual(0, process.ExitCode);
            Assert.Contains("Register the real IStockService", text);
        }
        finally
        {
            if (!process.HasExited) process.Kill(true);
        }
    }
}
