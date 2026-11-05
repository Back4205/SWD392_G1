using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EWMS.Api.Common;
using EWMS.Api.Data;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Entities;
using EWMS.Api.Repositories;
using EWMS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class SupplierManifestTests
{
    private readonly SqlServerFixture _fixture;
    public SupplierManifestTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Theory]
    [InlineData("missing-item", "IN_MANIFEST_ITEMS")]
    [InlineData("foreign-item", "IN_MANIFEST_ITEMS")]
    [InlineData("wrong-sku", "IN_MANIFEST_SKU")]
    [InlineData("missing-serial", "IN_MANIFEST_COUNT")]
    [InlineData("extra-serial", "IN_MANIFEST_COUNT")]
    [InlineData("duplicate", "IN_MANIFEST_DUPLICATE")]
    [InlineData("duplicate-case", "IN_MANIFEST_DUPLICATE")]
    [InlineData("format", "GR_SERIAL_FORMAT")]
    public async Task InvalidDeclarationCannotConfirmOrPersistPartialData(string scenario, string expectedCode)
    {
        var sample = await PendingAsync(secondSku: true);
        var dto = Payload(sample);
        if (scenario == "missing-item") dto.Items.RemoveAt(1);
        if (scenario == "foreign-item") dto.Items[0].ItemId = int.MaxValue;
        if (scenario == "wrong-sku") dto.Items[0].SKU = "OTHER-SKU";
        if (scenario == "missing-serial") dto.Items[0].SerialCodes.RemoveAt(0);
        if (scenario == "extra-serial") dto.Items[0].SerialCodes.Add("EXTRA-" + sample.Data.Suffix);
        if (scenario == "duplicate") dto.Items[1].SerialCodes[0] = dto.Items[0].SerialCodes[0];
        if (scenario == "duplicate-case") dto.Items[1].SerialCodes[0] = dto.Items[0].SerialCodes[0].ToLowerInvariant();
        if (scenario == "format") dto.Items[0].SerialCodes[0] = "SERIAL WITH SPACE";
        using (var db = _fixture.CreateContext())
            await Error(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, dto), expectedCode);
        using var check = _fixture.CreateContext();
        var inbound = await check.InboundRequests.FindAsync(sample.Data.InboundId);
        Assert.False(inbound!.HasSupplierManifest);
        Assert.Null(inbound.SupplierId);
        Assert.Null(inbound.SupplierCommitmentStatus);
        Assert.Empty(await check.InboundExpectedSerials.Where(x => x.InboundRequestId == inbound.Id).ToListAsync());
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.VariantId));
    }

    [Fact]
    public async Task ValidDeclarationPreservesSerialTextAndFailedReplacementKeepsPreviousCommitment()
    {
        var sample = await PendingAsync();
        await ConfirmAsync(sample);
        using (var db = _fixture.CreateContext())
        {
            var detail = await Service(db).GetByIdAsync(sample.Data.InboundId);
            Assert.True(detail!.HasSupplierManifest);
            Assert.Equal(sample.Items[0].SKU, detail.Items.Single().SKU);
            Assert.Equal(sample.Items[0].SerialCodes, detail.Items.Single().ExpectedSerials);
            Assert.StartsWith("00A-", detail.Items.Single().ExpectedSerials[0]);
            Assert.False(await db.SerialNumbers.AnyAsync(x => sample.Items[0].SerialCodes.Contains(x.SerialCode)));
            Assert.False(await db.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.VariantId));
        }
        var invalid = Payload(sample);
        invalid.ExpectedDeliveryDate = DateTime.Today.AddDays(30);
        invalid.Items[0].SerialCodes[1] = invalid.Items[0].SerialCodes[0];
        using (var db = _fixture.CreateContext())
            await Error(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, invalid), "IN_MANIFEST_DUPLICATE");
        using var check = _fixture.CreateContext();
        var unchanged = await check.InboundRequests.FindAsync(sample.Data.InboundId);
        Assert.Equal(DateTime.Today.AddDays(2), unchanged!.ExpectedDeliveryDate);
        Assert.Equal(sample.Items[0].SerialCodes, await check.InboundExpectedSerials
            .Where(x => x.InboundRequestId == unchanged.Id).OrderBy(x => x.Id).Select(x => x.SerialCode).ToListAsync());
    }

    [Fact]
    public async Task ExistingStockAndOtherActiveInboundCannotPromiseTheSameSerial()
    {
        var sample = await PendingAsync();
        using (var db = _fixture.CreateContext())
        {
            db.SerialNumbers.Add(new SerialNumber { SerialCode = sample.Items[0].SerialCodes[0], ProductVariantId = sample.Data.VariantId, CurrentStatus = "IN_STOCK", CurrentBinId = sample.Data.BinId });
            await db.SaveChangesAsync();
        }
        using (var db = _fixture.CreateContext())
            await Error(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample)), "IN_MANIFEST_SERIAL_IN_USE");

        var first = await PendingAsync();
        var second = await PendingAsync();
        await ConfirmAsync(first);
        var dto = Payload(second);
        dto.Items[0].SerialCodes[0] = first.Items[0].SerialCodes[0];
        using var other = _fixture.CreateContext();
        await Error(() => Service(other).UpdateSupplierCommitmentAsync(second.Data.InboundId, dto), "IN_MANIFEST_SERIAL_IN_USE");
    }

    [Fact]
    public async Task SupplierRejectionClearsManifestAndManagerCannotApproveIt()
    {
        var sample = await PendingAsync();
        await ConfirmAsync(sample);
        using (var db = _fixture.CreateContext())
            await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, new UpdateSupplierCommitmentDto
            { SupplierId = sample.SupplierId, CommitmentStatus = "Rejected" });
        using (var db = _fixture.CreateContext())
        {
            var request = await db.InboundRequests.FindAsync(sample.Data.InboundId);
            Assert.False(request!.HasSupplierManifest);
            Assert.Null(request.ExpectedDeliveryDate);
            Assert.Empty(await db.InboundExpectedSerials.Where(x => x.InboundRequestId == request.Id).ToListAsync());
            await Error(() => Service(db).ReviewInboundRequestAsync(request.Id, new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId), "IN_MANIFEST_REQUIRED");
        }
    }

    [Fact]
    public async Task ApprovalRequiresDeclarationThenLocksItAgainstPurchasingEdits()
    {
        var sample = await PendingAsync();
        using (var db = _fixture.CreateContext())
            await Error(() => Service(db).ReviewInboundRequestAsync(sample.Data.InboundId, new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId), "IN_MANIFEST_REQUIRED");
        await ConfirmAsync(sample);
        await ApproveInboundAsync(sample);
        using var locked = _fixture.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(locked).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample)));
        Assert.Equal("Approved", (await locked.InboundRequests.FindAsync(sample.Data.InboundId))!.Status);
    }

    [Fact]
    public async Task ScanUnknownOrWrongSkuNeverChangesCountVersionOrStagingRows()
    {
        var sample = await ReadyAsync(secondSku: true);
        var receipt = await CreateAsync(sample);
        var item = receipt.Items.Single(x => x.ProductVariantId == sample.Data.VariantId);
        foreach (var invalid in new[] { (Code: "NOT-DECLARED-" + sample.Data.Suffix, Error: "GR_SERIAL_NOT_EXPECTED"),
            (Code: sample.Items[1].SerialCodes[0], Error: "GR_SERIAL_WRONG_SKU") })
        {
            using var staff = new ReceivingSession(_fixture);
            await Error(() => staff.Serials.AddAsync(receipt.Id, item.Id, new AddReceiptSerialDto
            { ExpectedVersion = receipt.Version, SKU = item.SKU, SerialCode = invalid.Code }, sample.Data.StaffId), invalid.Error);
            var unchanged = await staff.Receipts.GetByIdAsync(receipt.Id, sample.Data.StaffId);
            Assert.Equal(receipt.Version, unchanged.Version);
            Assert.All(unchanged.Items, x => Assert.Equal(0, x.ReceivedQty));
            Assert.False(await staff.Context.GoodsReceiptSerials.AnyAsync(x => x.GoodsReceiptId == receipt.Id));
        }
    }

    [Fact]
    public async Task DeclaredSerialCannotBeClaimedByTwoActiveReceiptsAndRejectReleasesIt()
    {
        var sample = await ReadyAsync();
        var first = await ScanAsync(sample, await CreateAsync(sample), sample.Items[0].SerialCodes[0]);
        var second = await CreateAsync(sample);
        using (var staff = new ReceivingSession(_fixture))
        {
            var item = second.Items.Single();
            await Error(() => staff.Serials.AddAsync(second.Id, item.Id, new AddReceiptSerialDto
            { ExpectedVersion = second.Version, SKU = item.SKU, SerialCode = sample.Items[0].SerialCodes[0] }, sample.Data.StaffId), "GR_SERIAL_IN_ANOTHER_RECEIPT");
            var view = await staff.Receipts.GetByIdAsync(second.Id, sample.Data.StaffId);
            var declared = view.Items.Single().ExpectedSerials.Single(x => x.SerialCode == sample.Items[0].SerialCodes[0]);
            Assert.Equal("ScannedOtherReceipt", declared.Status);
            Assert.Equal(first.Id, declared.ReceiptId);
        }
        first = await PendingReceiptAsync(sample, first);
        using (var manager = new ReceivingSession(_fixture))
            await manager.Approval.RejectAsync(first.Id, new RejectGoodsReceiptDto { ExpectedVersion = first.Version, Reason = "Không đúng tình trạng giao nhận" }, sample.Data.ManagerId);
        second = await ScanAsync(sample, second, sample.Items[0].SerialCodes[0]);
        Assert.Equal(1, second.Items.Single().ReceivedQty);
        using var check = _fixture.CreateContext();
        Assert.False(await check.SerialNumbers.AnyAsync(x => sample.Items[0].SerialCodes.Contains(x.SerialCode)));
    }

    [Fact]
    public async Task ReceiveDeclaredDevicesInTwoDeliveriesShowsReconciliationAndCompletesOfficialStock()
    {
        var sample = await ReadyAsync(secondSku: true);
        var first = await CreateAsync(sample);
        Assert.True(first.HasSupplierManifest);
        Assert.All(first.Items.SelectMany(x => x.ExpectedSerials), x => Assert.Equal("AwaitingReceipt", x.Status));
        string scannedText = sample.Items[0].SerialCodes[0].ToLowerInvariant();
        first = await ScanAsync(sample, first, scannedText);
        var comparison = first.Items.Single(x => x.ProductVariantId == sample.Data.VariantId).ExpectedSerials
            .Single(x => x.SerialCode == sample.Items[0].SerialCodes[0]);
        Assert.Equal(scannedText, comparison.ScannedSerialCode);
        first = await ScanAsync(sample, first, sample.Items[1].SerialCodes[0], sample.Data.SecondVariantId);
        Assert.Equal(2, first.Items.SelectMany(x => x.ExpectedSerials).Count(x => x.Status == "ScannedThisReceipt"));
        first = await PendingReceiptAsync(sample, first);
        using (var manager = new ReceivingSession(_fixture)) first = await manager.Approval.ApproveAsync(first.Id, Version(first), sample.Data.ManagerId);
        Assert.Equal(2, first.Items.SelectMany(x => x.ExpectedSerials).Count(x => x.Status == "Received"));
        using (var db = _fixture.CreateContext()) Assert.Equal("PartiallyReceived", (await db.InboundRequests.FindAsync(sample.Data.InboundId))!.Status);

        var second = await CreateAsync(sample);
        Assert.Equal(2, second.Items.SelectMany(x => x.ExpectedSerials).Count(x => x.Status == "Received"));
        second = await ScanAsync(sample, second, sample.Items[0].SerialCodes[1]);
        second = await PendingReceiptAsync(sample, second);
        using (var manager = new ReceivingSession(_fixture)) second = await manager.Approval.ApproveAsync(second.Id, Version(second), sample.Data.ManagerId);
        Assert.All(second.Items.SelectMany(x => x.ExpectedSerials), x => Assert.Equal("Received", x.Status));
        using var check = _fixture.CreateContext();
        Assert.Equal("Completed", (await check.InboundRequests.FindAsync(sample.Data.InboundId))!.Status);
        Assert.Equal(2, (await check.Stocks.SingleAsync(x => x.ProductVariantId == sample.Data.VariantId && x.BinId == sample.Data.BinId)).OnHandQty);
        Assert.Equal(1, (await check.Stocks.SingleAsync(x => x.ProductVariantId == sample.Data.SecondVariantId && x.BinId == sample.Data.BinId)).OnHandQty);
        Assert.Equal(3, await check.SerialNumbers.CountAsync(x => x.ProductVariantId == sample.Data.VariantId || x.ProductVariantId == sample.Data.SecondVariantId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitAndApproveRevalidatePersistedScanInsteadOfTrustingEarlierSuccess(bool afterSubmit)
    {
        var sample = await ReadyAsync();
        var receipt = await ScanAsync(sample, await CreateAsync(sample), sample.Items[0].SerialCodes[0]);
        if (afterSubmit) receipt = await PendingReceiptAsync(sample, receipt);
        else receipt = await PlaceAsync(sample, receipt);
        // Mô phỏng dữ liệu bị thay đổi ngoài endpoint sau khi Staff quét thành công.
        using (var db = _fixture.CreateContext())
        {
            var scan = await db.GoodsReceiptSerials.SingleAsync(x => x.GoodsReceiptId == receipt.Id);
            scan.SerialCode = "CORRUPTED-" + sample.Data.Suffix;
            await db.SaveChangesAsync();
        }
        using (var actor = new ReceivingSession(_fixture))
        {
            if (afterSubmit) await Error(() => actor.Approval.ApproveAsync(receipt.Id, Version(receipt), sample.Data.ManagerId), "GR_SERIAL_NOT_EXPECTED");
            else await Error(() => actor.Receipts.SubmitAsync(receipt.Id, Version(receipt), sample.Data.StaffId), "GR_SERIAL_NOT_EXPECTED");
        }
        using var check = _fixture.CreateContext();
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.VariantId));
        Assert.False(await check.SerialMovements.AnyAsync(x => x.RefType == "GOODS_RECEIPT" && x.RefId == receipt.Id));
        Assert.Equal(afterSubmit ? "PendingApproval" : "Draft", (await check.GoodsReceipts.FindAsync(receipt.Id))!.Status);
    }

    [Fact]
    public async Task MissingPersistedManifestCannotBeUsedToBypassReceiptApproval()
    {
        var sample = await ReadyAsync();
        var receipt = await PendingReceiptAsync(sample, await ScanAsync(sample, await CreateAsync(sample), sample.Items[0].SerialCodes[0]));
        using (var db = _fixture.CreateContext())
        {
            var declared = await db.InboundExpectedSerials.FirstAsync(x => x.InboundRequestId == sample.Data.InboundId);
            db.InboundExpectedSerials.Remove(declared); await db.SaveChangesAsync();
        }
        using var manager = new ReceivingSession(_fixture);
        await Error(() => manager.Approval.ApproveAsync(receipt.Id, Version(receipt), sample.Data.ManagerId), "IN_MANIFEST_REQUIRED");
        Assert.False(await manager.Context.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.VariantId));
    }

    [Fact]
    public async Task HttpManifestIsPurchasingOnlyScopedToWarehouseAndManagerApprovalIsGated()
    {
        var sample = await PendingAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        async Task Login(int id) { await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(id))!.UserName); }
        string route = $"/api/inbound-requests/{sample.Data.InboundId}";
        await Login(sample.Data.StaffId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        await Login(sample.Data.ManagerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        var blocked = await client.PutAsJsonAsync(route + "/review", new { decision = "Approved" });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("IN_MANIFEST_REQUIRED", (await blocked.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await Login(sample.Data.PurchasingUserId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        var detail = await client.GetFromJsonAsync<InboundRequestDetailDto>(route);
        Assert.True(detail!.HasSupplierManifest);
        Assert.Equal(sample.Items[0].SerialCodes, detail.Items.Single().ExpectedSerials);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/review", new { decision = "Approved" })).StatusCode);
        // Cùng role Purchasing nhưng bị bỏ phân công kho: request phải bị chặn.
        var assignment = await db.UserWarehouses.SingleAsync(x => x.UserId == sample.Data.PurchasingUserId && x.WarehouseId == sample.Data.WarehouseId);
        db.UserWarehouses.Remove(assignment); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        await Login(sample.Data.ManagerId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(route + "/review", new { decision = "Approved" })).StatusCode);
        await app.StopAsync();
    }

    private InboundRequestService Service(EWMSDbContext db) => new(new InboundRequestRepository(db), db, new ConfigurationBuilder().Build());
    private static ReceiptVersionDto Version(GoodsReceiptDetailDto receipt) => new() { ExpectedVersion = receipt.Version };
    private static async Task Error(Func<Task> operation, string code)
    {
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(operation);
        Assert.Equal(code, error.Code);
    }
    private sealed class ManifestSample
    {
        public TestData Data { get; set; } = null!;
        public int SupplierId { get; set; }
        public List<SupplierManifestItemDto> Items { get; set; } = new();
    }
    private async Task<ManifestSample> PendingAsync(bool secondSku = false)
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using var db = _fixture.CreateContext();
        var inbound = await db.InboundRequests.FindAsync(data.InboundId);
        inbound!.Status = "Pending"; inbound.ApprovedByUserId = null; inbound.SupplierCommitmentStatus = null;
        var supplier = new Supplier { SupplierCode = "MF-" + data.Suffix, SupplierName = "Supplier manifest", Status = "Active" };
        db.Suppliers.Add(supplier);
        if (secondSku) db.InboundRequestItems.Add(new InboundRequestItem { InboundRequestId = data.InboundId, ProductVariantId = data.SecondVariantId, ExpectedQty = 1 });
        await db.SaveChangesAsync();
        var lines = await db.InboundRequestItems.Where(x => x.InboundRequestId == data.InboundId).Include(x => x.ProductVariant).OrderBy(x => x.Id).ToListAsync();
        var sample = new ManifestSample { Data = data, SupplierId = supplier.Id };
        foreach (var line in lines)
        {
            var item = new SupplierManifestItemDto { ItemId = line.Id, SKU = line.ProductVariant.SKU };
            for (int i = 0; i < line.ExpectedQty; i++) item.SerialCodes.Add($"00{(char)('A' + i)}-{line.Id}-{data.Suffix}");
            sample.Items.Add(item);
        }
        return sample;
    }
    private static UpdateSupplierCommitmentDto Payload(ManifestSample sample) => new()
    {
        SupplierId = sample.SupplierId, CommitmentStatus = "Confirmed", ExpectedDeliveryDate = DateTime.Today.AddDays(2),
        Items = sample.Items.Select(x => new SupplierManifestItemDto { ItemId = x.ItemId, SKU = x.SKU, SerialCodes = x.SerialCodes.ToList() }).ToList()
    };
    private async Task ConfirmAsync(ManifestSample sample)
    {
        using var db = _fixture.CreateContext();
        await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample));
    }
    private async Task ApproveInboundAsync(ManifestSample sample)
    {
        using var db = _fixture.CreateContext();
        await Service(db).ReviewInboundRequestAsync(sample.Data.InboundId, new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId);
    }
    private async Task<ManifestSample> ReadyAsync(bool secondSku = false)
    {
        var sample = await PendingAsync(secondSku); await ConfirmAsync(sample); await ApproveInboundAsync(sample); return sample;
    }
    private async Task<GoodsReceiptDetailDto> CreateAsync(ManifestSample sample)
    {
        using var staff = new ReceivingSession(_fixture);
        return await staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = sample.Data.InboundId }, sample.Data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> ScanAsync(ManifestSample sample, GoodsReceiptDetailDto receipt, string serial, int? variant = null)
    {
        using var staff = new ReceivingSession(_fixture);
        var item = receipt.Items.Single(x => x.ProductVariantId == (variant ?? sample.Data.VariantId));
        await staff.Serials.AddAsync(receipt.Id, item.Id, new AddReceiptSerialDto { SKU = item.SKU, SerialCode = serial, ExpectedVersion = receipt.Version }, sample.Data.StaffId);
        return await staff.Receipts.GetByIdAsync(receipt.Id, sample.Data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> PlaceAsync(ManifestSample sample, GoodsReceiptDetailDto receipt)
    {
        using var staff = new ReceivingSession(_fixture);
        return await staff.Receipts.UpdateDraftAsync(receipt.Id, new UpdateGoodsReceiptDto
        {
            ExpectedVersion = receipt.Version, Items = receipt.Items.Select(x => new UpdateReceiptBinDto { ItemId = x.Id, BinId = x.ReceivedQty > 0 ? sample.Data.BinId : null }).ToList()
        }, sample.Data.StaffId);
    }
    private async Task<GoodsReceiptDetailDto> PendingReceiptAsync(ManifestSample sample, GoodsReceiptDetailDto receipt)
    {
        receipt = await PlaceAsync(sample, receipt);
        using var staff = new ReceivingSession(_fixture);
        return await staff.Receipts.SubmitAsync(receipt.Id, Version(receipt), sample.Data.StaffId);
    }
}
