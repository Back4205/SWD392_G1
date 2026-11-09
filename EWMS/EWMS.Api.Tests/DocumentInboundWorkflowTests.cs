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

// Kiểm tra theo SD03/SD04/SD05 và UC50/UC56, không yêu cầu Purchasing khai báo serial.
[Collection("Receiving SQL")]
public class DocumentInboundWorkflowTests
{
    private readonly SqlServerFixture _fixture;
    public DocumentInboundWorkflowTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Fact]
    public async Task AcceptedCommitmentUpdatesConfirmedVariantAndQuantityWithoutCreatingSerialOrStock()
    {
        var sample = await PendingAsync();
        var payload = Payload(sample);
        payload.Items[0].ProductVariantId = sample.Data.SecondVariantId;
        payload.Items[0].ExpectedQty = 3;
        using (var db = _fixture.CreateContext())
            await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, payload);
        using var check = _fixture.CreateContext();
        var detail = await Service(check).GetByIdAsync(sample.Data.InboundId);
        Assert.Equal("Accepted", detail!.SupplierCommitmentStatus);
        Assert.Equal("Pending", detail.Status);
        Assert.Equal(sample.Data.SecondVariantId, detail.Items.Single().ProductVariantId);
        Assert.Equal(3, detail.Items.Single().ExpectedQty);
        Assert.Equal(sample.SupplierId, detail.SupplierId);
        Assert.Equal(DateTime.Today.AddDays(2), detail.ExpectedDeliveryDate);
        Assert.False(await check.InboundExpectedSerials.AnyAsync(x => x.InboundRequestId == detail.Id));
        Assert.False(await check.SerialNumbers.AnyAsync(x => x.ProductVariantId == sample.Data.SecondVariantId));
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.SecondVariantId));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    [InlineData("duplicate")]
    [InlineData("zero")]
    [InlineData("unknown-variant")]
    [InlineData("past-date")]
    [InlineData("missing-date")]
    public async Task InvalidCommitmentRollsBackHeaderAndAllConfirmedLines(string scenario)
    {
        var sample = await PendingAsync(secondSku: true);
        await ConfirmAsync(sample);
        var payload = Payload(sample);
        payload.Items[0].ExpectedQty = 7;
        payload.ExpectedDeliveryDate = DateTime.Today.AddDays(30);
        if (scenario == "missing") payload.Items.RemoveAt(1);
        if (scenario == "foreign") payload.Items[1].ItemId = int.MaxValue;
        if (scenario == "duplicate") payload.Items[1].ProductVariantId = payload.Items[0].ProductVariantId;
        if (scenario == "zero") payload.Items[1].ExpectedQty = 0;
        if (scenario == "unknown-variant") payload.Items[1].ProductVariantId = int.MaxValue;
        if (scenario == "past-date") payload.ExpectedDeliveryDate = DateTime.Today.AddDays(-1);
        if (scenario == "missing-date") payload.ExpectedDeliveryDate = null;
        using (var db = _fixture.CreateContext())
            await Assert.ThrowsAsync<ArgumentException>(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, payload));
        using var check = _fixture.CreateContext();
        var unchanged = await Service(check).GetByIdAsync(sample.Data.InboundId);
        Assert.Equal("Accepted", unchanged!.SupplierCommitmentStatus);
        Assert.Equal(DateTime.Today.AddDays(2), unchanged.ExpectedDeliveryDate);
        Assert.Equal(sample.Items.Select(x => x.ExpectedQty), unchanged.Items.Select(x => x.ExpectedQty));
        Assert.Equal(sample.Items.Select(x => x.ProductVariantId), unchanged.Items.Select(x => x.ProductVariantId));
    }

    [Fact]
    public async Task SupplierRejectionCancelsRequestAndBlocksReceivingReviewAndFurtherCommitment()
    {
        var sample = await PendingAsync();
        await ConfirmAsync(sample);
        using (var db = _fixture.CreateContext())
            await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, new UpdateSupplierCommitmentDto
            { SupplierId = sample.SupplierId, CommitmentStatus = "Rejected" });
        using (var db = _fixture.CreateContext())
        {
            var row = await db.InboundRequests.FindAsync(sample.Data.InboundId);
            Assert.Equal("Canceled", row!.Status);
            Assert.Equal("Rejected", row.SupplierCommitmentStatus);
            Assert.Null(row.ExpectedDeliveryDate);
            await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).ReviewInboundRequestAsync(row.Id,
                new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId));
            await Assert.ThrowsAsync<InvalidOperationException>(() => Service(db).UpdateSupplierCommitmentAsync(row.Id, Payload(sample)));
        }
        using var staff = new ReceivingSession(_fixture);
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => staff.Receipts.CreateAsync(
            new CreateGoodsReceiptDto { InboundRequestId = sample.Data.InboundId }, sample.Data.StaffId));
        Assert.Equal("MSG91", error.Code);
        Assert.False(await staff.Context.GoodsReceipts.AnyAsync(x => x.InboundRequestId == sample.Data.InboundId));
    }

    [Fact]
    public async Task ManagerCannotApproveBeforeSupplierAcceptsAndApprovalLocksPurchasingEdits()
    {
        var sample = await PendingAsync();
        using (var db = _fixture.CreateContext())
        {
            var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId));
            Assert.Equal("IN_SUPPLIER_NOT_ACCEPTED", error.Code);
        }
        await ConfirmAsync(sample);
        await ApproveAsync(sample);
        using var check = _fixture.CreateContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(check).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample)));
        var row = await check.InboundRequests.FindAsync(sample.Data.InboundId);
        Assert.Equal("Approved", row!.Status);
        Assert.Null(row.ApprovalNote);
        Assert.Equal(sample.Data.ManagerId, row.ApprovedByUserId);
    }

    [Fact]
    public async Task OldPurchasingAndManagerViewsCannotOverwriteNewConfirmedDetails()
    {
        var sample = await PendingAsync();
        await ConfirmAsync(sample);
        string oldRevision;
        using (var db = _fixture.CreateContext()) oldRevision = (await Service(db).GetByIdAsync(sample.Data.InboundId))!.Revision;
        var replacement = Payload(sample);
        replacement.ExpectedRevision = oldRevision;
        replacement.Items[0].ExpectedQty = 4;
        using (var db = _fixture.CreateContext()) await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, replacement);
        using (var db = _fixture.CreateContext())
        {
            var stale = Payload(sample); stale.ExpectedRevision = oldRevision;
            await Stale(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, stale));
            await Stale(() => Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = "Approved", ExpectedRevision = oldRevision }, sample.Data.ManagerId));
        }
        using var fresh = _fixture.CreateContext();
        var current = await Service(fresh).GetByIdAsync(sample.Data.InboundId);
        Assert.NotEqual(oldRevision, current!.Revision);
        Assert.Equal("Pending", current.Status);
        Assert.Equal(4, current.Items.Single().ExpectedQty);
        await Service(fresh).ReviewInboundRequestAsync(sample.Data.InboundId,
            new ReviewInboundRequestDto { Decision = "Approved", ExpectedRevision = current.Revision }, sample.Data.ManagerId);
    }

    [Theory]
    [InlineData("warehouse")]
    [InlineData("product")]
    [InlineData("weight")]
    [InlineData("sku")]
    public async Task CreateCommitmentAndApprovalRevalidateActiveStorageAndProducts(string scenario)
    {
        var sample = await PendingAsync();
        await ConfirmAsync(sample);
        using (var db = _fixture.CreateContext())
        {
            if (scenario == "warehouse") (await db.Warehouses.FindAsync(sample.Data.WarehouseId))!.Status = "Inactive";
            else if (scenario == "weight") (await db.ProductVariants.FindAsync(sample.Data.VariantId))!.WeightKg = 0;
            else if (scenario == "sku") (await db.ProductVariants.FindAsync(sample.Data.VariantId))!.SKU = "\t";
            else (await db.ProductVariants.Include(x => x.Product).SingleAsync(x => x.Id == sample.Data.VariantId)).Product!.Status = "Inactive";
            await db.SaveChangesAsync();
        }
        using (var db = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateAsync(CreatePayload(sample.Data), sample.Data.ManagerId));
            await Assert.ThrowsAsync<ArgumentException>(() => Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample)));
            await Assert.ThrowsAsync<ArgumentException>(() => Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId));
            // Cho phép kết thúc yêu cầu lỗi bằng nhánh từ chối, không khóa cleanup.
            await Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = "Rejected", RejectionReason = "Không thể nhập mặt hàng này." }, sample.Data.ManagerId);
            if (scenario == "sku")
            {
                (await db.ProductVariants.FindAsync(sample.Data.VariantId))!.SKU = sample.Data.SKU;
                await db.SaveChangesAsync();
            }
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("other-warehouse")]
    [InlineData("manager-role")]
    public async Task CreateRejectsAssignedStaffWhoCannotWorkInSelectedWarehouse(string scenario)
    {
        var data = await _fixture.SeedAsync();
        var dto = CreatePayload(data);
        dto.AssignedStaffUserId = scenario switch { "missing" => null, "inactive" => data.InactiveUserId,
            "other-warehouse" => data.OtherUserId, _ => data.ManagerId };
        using var db = _fixture.CreateContext();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(db).CreateAsync(dto, data.ManagerId));
        Assert.Equal(1, await db.InboundRequests.CountAsync(x => x.WarehouseId == data.WarehouseId));
    }

    [Fact]
    public async Task CreatePersistsAssignedStaffAndLookupExcludesInactiveOrUnassignedStaff()
    {
        var data = await _fixture.SeedAsync();
        using (var db = _fixture.CreateContext())
        {
            int id = await Service(db).CreateAsync(CreatePayload(data), data.ManagerId);
            var detail = await Service(db).GetByIdAsync(id);
            Assert.Equal(data.StaffId, detail!.AssignedStaffUserId);
            Assert.Equal("staff", detail.AssignedStaffName);
            Assert.Equal("Pending", detail.Status);
        }
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var check = _fixture.CreateContext();
        await AuthTestHelper.LoginAsync(client, (await check.Users.FindAsync(data.ManagerId))!.UserName);
        var lookup = await client.GetFromJsonAsync<JsonElement>("/api/inbound-requests/lookups");
        var staff = lookup.GetProperty("warehouseStaff").EnumerateArray().ToList();
        Assert.Contains(staff, x => x.GetProperty("userId").GetInt32() == data.StaffId && x.GetProperty("warehouseId").GetInt32() == data.WarehouseId);
        Assert.DoesNotContain(staff, x => x.GetProperty("userId").GetInt32() == data.InactiveUserId || x.GetProperty("userId").GetInt32() == data.OtherUserId);
        await app.StopAsync();
    }

    [Fact]
    public async Task LookupExcludesWhitespaceSkuAndZeroWeightProducts()
    {
        var data = await _fixture.SeedAsync();
        using var db = _fixture.CreateContext();
        // Khác chuỗi tab ở ca validate để không đụng unique index SKU của fixture dùng chung.
        (await db.ProductVariants.FindAsync(data.VariantId))!.SKU = "\t\t";
        (await db.ProductVariants.FindAsync(data.SecondVariantId))!.WeightKg = 0;
        await db.SaveChangesAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(data.ManagerId))!.UserName);
        var lookup = await client.GetFromJsonAsync<JsonElement>("/api/inbound-requests/lookups");
        var variants = lookup.GetProperty("productVariants").EnumerateArray().ToList();
        Assert.DoesNotContain(variants, x => x.GetProperty("id").GetInt32() == data.VariantId || x.GetProperty("id").GetInt32() == data.SecondVariantId);
        await app.StopAsync();
        (await db.ProductVariants.FindAsync(data.VariantId))!.SKU = data.SKU;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CommitmentHttpRemainsPurchasingOnlyAndScopedToWarehouse()
    {
        var sample = await PendingAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        async Task Login(int id) => await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(id))!.UserName);
        string route = $"/api/inbound-requests/{sample.Data.InboundId}";
        await Login(sample.Data.StaffId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        await Login(sample.Data.ManagerId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        await Login(sample.Data.PurchasingUserId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/review", new { decision = "Approved" })).StatusCode);
        db.UserWarehouses.Remove(await db.UserWarehouses.SingleAsync(x => x.UserId == sample.Data.PurchasingUserId && x.WarehouseId == sample.Data.WarehouseId));
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync(route + "/supplier-commitment", Payload(sample))).StatusCode);
        await Login(sample.Data.ManagerId);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(route + "/review", new { decision = "Approved" })).StatusCode);
        await app.StopAsync();
    }

    private static async Task Stale(Func<Task> action)
    {
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(action);
        Assert.Equal(409, error.StatusCode); Assert.Equal("IN_STALE_REVISION", error.Code);
    }
    internal static InboundRequestService Service(EWMSDbContext db) => new(new InboundRequestRepository(db), db, new ConfigurationBuilder().Build());
    private static CreateInboundRequestDto CreatePayload(TestData data) => new()
    {
        WarehouseId = data.WarehouseId, AssignedStaffUserId = data.StaffId,
        Items = new() { new() { ProductVariantId = data.VariantId, ExpectedQty = 2 } }
    };
    internal sealed class Sample
    {
        public TestData Data { get; set; } = null!;
        public int SupplierId { get; set; }
        public List<ConfirmedInboundItemDto> Items { get; set; } = new();
    }
    internal async Task<Sample> PendingAsync(bool secondSku = false)
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using var db = _fixture.CreateContext();
        var inbound = await db.InboundRequests.FindAsync(data.InboundId);
        inbound!.Status = "Pending"; inbound.ApprovedByUserId = null; inbound.SupplierCommitmentStatus = null;
        inbound.AssignedStaffUserId = data.StaffId;
        var supplier = new Supplier { SupplierCode = "RP-" + data.Suffix, SupplierName = "Document workflow supplier", Status = "Active" };
        db.Suppliers.Add(supplier);
        if (secondSku) db.InboundRequestItems.Add(new InboundRequestItem { InboundRequestId = data.InboundId, ProductVariantId = data.SecondVariantId, ExpectedQty = 1 });
        await db.SaveChangesAsync();
        return new Sample
        {
            Data = data, SupplierId = supplier.Id,
            Items = await db.InboundRequestItems.Where(x => x.InboundRequestId == data.InboundId).OrderBy(x => x.Id)
                .Select(x => new ConfirmedInboundItemDto { ItemId = x.Id, ProductVariantId = x.ProductVariantId, ExpectedQty = x.ExpectedQty }).ToListAsync()
        };
    }
    internal static UpdateSupplierCommitmentDto Payload(Sample sample) => new()
    {
        SupplierId = sample.SupplierId, CommitmentStatus = "Accepted", ExpectedDeliveryDate = DateTime.Today.AddDays(2),
        Items = sample.Items.Select(x => new ConfirmedInboundItemDto { ItemId = x.ItemId, ProductVariantId = x.ProductVariantId, ExpectedQty = x.ExpectedQty }).ToList()
    };
    internal async Task ConfirmAsync(Sample sample)
    {
        using var db = _fixture.CreateContext(); await Service(db).UpdateSupplierCommitmentAsync(sample.Data.InboundId, Payload(sample));
    }
    private async Task ApproveAsync(Sample sample)
    {
        using var db = _fixture.CreateContext(); await Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
            new ReviewInboundRequestDto { Decision = "Approved" }, sample.Data.ManagerId);
    }
}
