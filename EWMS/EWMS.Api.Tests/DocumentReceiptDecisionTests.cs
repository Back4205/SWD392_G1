using System.Net;
using System.Net.Http.Json;
using EWMS.Api.Common;
using EWMS.Api.DTOs.GoodsReceipts;
using EWMS.Api.DTOs.InboundRequests;
using EWMS.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EWMS.Api.Tests;

[Collection("Receiving SQL")]
public class DocumentReceiptDecisionTests
{
    private const string Reason = "Đã đối chiếu chứng từ và hàng thực nhận.\nThông tin giao hàng chưa chính xác.";
    private readonly SqlServerFixture _fixture;
    public DocumentReceiptDecisionTests(SqlServerFixture fixture) { _fixture = fixture; }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("oversized")]
    public async Task ReceiptRejectRequiresReasonAndFailurePreservesPendingStateAndInventory(string? value)
    {
        var (data, receipt) = await PendingReceiptAsync();
        using (var manager = new ReceivingSession(_fixture))
        {
            var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => manager.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = value == "oversized" ? new string('X', 256) : value! }, data.ManagerId));
            Assert.Equal(400, error.StatusCode); Assert.Equal("GR_REJECTION_REASON", error.Code);
        }
        using var check = _fixture.CreateContext();
        var row = await check.GoodsReceipts.FindAsync(receipt.Id);
        Assert.Equal("PendingApproval", row!.Status);
        Assert.Equal(receipt.Version, Convert.ToBase64String(row.Version));
        Assert.Null(row.RejectionReason); Assert.Null(row.ApprovedByUserId); Assert.Null(row.ApprovalDate);
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == data.VariantId));
        Assert.False(await check.SerialNumbers.AnyAsync(x => x.OriginGoodsReceiptItem != null && x.OriginGoodsReceiptItem.GoodsReceiptId == receipt.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("oversized")]
    public async Task InboundRejectRequiresReasonAndFailurePreservesPendingState(string? value)
    {
        var tests = new DocumentInboundWorkflowTests(_fixture);
        var sample = await tests.PendingAsync(); await tests.ConfirmAsync(sample);
        using (var context = _fixture.CreateContext())
        {
            var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => DocumentInboundWorkflowTests.Service(context).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = "Rejected", RejectionReason = value == "oversized" ? new string('X', 256) : value }, sample.Data.ManagerId));
            Assert.Equal(400, error.StatusCode); Assert.Equal("IN_REJECTION_REASON", error.Code);
        }
        using var check = _fixture.CreateContext();
        var row = await check.InboundRequests.FindAsync(sample.Data.InboundId);
        Assert.Equal("Pending", row!.Status); Assert.Null(row.RejectionReason);
        Assert.Null(row.ApprovedByUserId); Assert.Null(row.DecisionDate);
        Assert.Equal("Accepted", row.SupplierCommitmentStatus);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReceiptDecisionPersistsIdentityAndDateAndCannotBeReplayed(bool approve)
    {
        var (data, receipt) = await PendingReceiptAsync();
        DateTime before = DateTime.UtcNow;
        using (var manager = new ReceivingSession(_fixture))
        {
            if (approve) await manager.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId);
            else await manager.Approval.RejectAsync(receipt.Id,
                new RejectGoodsReceiptDto { ExpectedVersion = receipt.Version, Reason = "  " + Reason + "  " }, data.ManagerId);
        }
        using var fresh = new ReceivingSession(_fixture);
        var detail = await fresh.Receipts.GetByIdAsync(receipt.Id, data.StaffId);
        Assert.Equal(approve ? "Approved" : "Rejected", detail.Status);
        Assert.Null(detail.ApprovalNote);
        Assert.Equal(approve ? null : Reason, detail.RejectionReason);
        Assert.Equal(data.ManagerId, detail.DecidedByUserId); Assert.Equal("manager", detail.DecidedByName);
        Assert.NotNull(detail.DecisionDate); Assert.InRange(detail.DecisionDate!.Value, before, DateTime.UtcNow);
        var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => fresh.Approval.ApproveAsync(receipt.Id, Version(detail), data.ManagerId));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(approve ? 1 : 0, await fresh.Context.SerialNumbers.CountAsync(x => x.OriginGoodsReceiptItem != null && x.OriginGoodsReceiptItem.GoodsReceiptId == receipt.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InboundDecisionPersistsReasonIdentityAndDateAndCannotBeReplayed(bool approve)
    {
        var tests = new DocumentInboundWorkflowTests(_fixture);
        var sample = await tests.PendingAsync(); await tests.ConfirmAsync(sample);
        DateTime before = DateTime.UtcNow;
        using (var db = _fixture.CreateContext())
            await DocumentInboundWorkflowTests.Service(db).ReviewInboundRequestAsync(sample.Data.InboundId,
                new ReviewInboundRequestDto { Decision = approve ? "Approved" : "Rejected", RejectionReason = approve ? null : "  " + Reason + "  " }, sample.Data.ManagerId);
        using var check = _fixture.CreateContext();
        var detail = await DocumentInboundWorkflowTests.Service(check).GetByIdAsync(sample.Data.InboundId);
        Assert.Equal(approve ? "Approved" : "Rejected", detail!.Status); Assert.Null(detail.ApprovalNote);
        Assert.Equal(approve ? null : Reason, detail.RejectionReason);
        Assert.Equal(sample.Data.ManagerId, detail.ApprovedByUserId); Assert.Equal("manager", detail.DecidedByName);
        Assert.NotNull(detail.DecisionDate); Assert.InRange(detail.DecisionDate!.Value, before, DateTime.UtcNow);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DocumentInboundWorkflowTests.Service(check).ReviewInboundRequestAsync(detail.Id,
            new ReviewInboundRequestDto { Decision = "Rejected", RejectionReason = "Không ghi đè quyết định." }, sample.Data.ManagerId));
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == sample.Data.VariantId));
    }

    [Theory]
    [InlineData(FailurePoint.AfterSerial)]
    [InlineData(FailurePoint.AfterStock)]
    [InlineData(FailurePoint.AfterApproved)]
    public async Task FailedApprovalRollsBackDecisionTogetherWithSerialMovementAndStock(FailurePoint failure)
    {
        var (data, receipt) = await PendingReceiptAsync();
        using (var manager = new ReceivingSession(_fixture, failure))
            await Assert.ThrowsAsync<InjectedFailureException>(() => manager.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId));
        using var check = _fixture.CreateContext();
        var row = await check.GoodsReceipts.FindAsync(receipt.Id);
        Assert.Equal("PendingApproval", row!.Status); Assert.Equal(receipt.Version, Convert.ToBase64String(row.Version));
        Assert.Null(row.ApprovalNote); Assert.Null(row.ApprovedByUserId); Assert.Null(row.ApprovalDate);
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == data.VariantId));
        Assert.False(await check.SerialMovements.AnyAsync(x => x.RefType == "GOODS_RECEIPT" && x.RefId == receipt.Id));
        Assert.False(await check.SerialNumbers.AnyAsync(x => x.OriginGoodsReceiptItem != null && x.OriginGoodsReceiptItem.GoodsReceiptId == receipt.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReceiptHttpApprovesWithoutNoteOrRequiresRejectionReasonThenStaffReadsDecision(bool approve)
    {
        var (data, receipt) = await PendingReceiptAsync();
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(data.ManagerId))!.UserName);
        string route = $"/api/goods-receipts/{receipt.Id}";
        if (!approve) Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(route + "/reject", new { expectedVersion = receipt.Version })).StatusCode);
        object payload = approve ? new { expectedVersion = receipt.Version } : new { expectedVersion = receipt.Version, reason = Reason };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(route + (approve ? "/approve" : "/reject"), payload)).StatusCode);
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(data.StaffId))!.UserName);
        var detail = await client.GetFromJsonAsync<GoodsReceiptDetailDto>(route);
        Assert.Null(detail!.ApprovalNote); Assert.Equal(approve ? null : Reason, detail.RejectionReason);
        Assert.Equal("manager", detail.DecidedByName); Assert.NotNull(detail.DecisionDate);
        await app.StopAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InboundHttpApprovesWithoutNoteOrRequiresReasonAndPurchasingReadsDecision(bool approve)
    {
        var tests = new DocumentInboundWorkflowTests(_fixture);
        var sample = await tests.PendingAsync(); await tests.ConfirmAsync(sample);
        await using var app = await new ReceivingHttpTests(_fixture).StartAsync();
        using var client = ReceivingHttpTests.Client(app);
        using var db = _fixture.CreateContext();
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(sample.Data.ManagerId))!.UserName);
        string route = $"/api/inbound-requests/{sample.Data.InboundId}";
        if (!approve) Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(route + "/review", new { decision = "Rejected" })).StatusCode);
        object payload = approve ? new { decision = "Approved" } : new { decision = "Rejected", rejectionReason = Reason };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(route + "/review", payload)).StatusCode);
        await AuthTestHelper.LoginAsync(client, (await db.Users.FindAsync(sample.Data.PurchasingUserId))!.UserName);
        var detail = await client.GetFromJsonAsync<InboundRequestDetailDto>(route);
        Assert.Null(detail!.ApprovalNote); Assert.Equal(approve ? null : Reason, detail.RejectionReason);
        Assert.Equal("manager", detail.DecidedByName); Assert.NotNull(detail.DecisionDate);
        await app.StopAsync();
    }

    [Fact]
    public async Task ActualSerialNeedNotBePredeclaredButWrongSkuAndDuplicateCannotIncreaseReceivedQuantity()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using var staff = new ReceivingSession(_fixture);
        var receipt = await staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId);
        var item = receipt.Items.Single();
        var wrongSku = await Assert.ThrowsAsync<GoodsReceiptException>(() => staff.Serials.AddAsync(receipt.Id, item.Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SKU = "OTHER-SKU", SerialCode = "ACTUAL-" + data.Suffix }, data.StaffId));
        Assert.Equal("GR_SKU", wrongSku.Code);
        var scan = await staff.Serials.AddAsync(receipt.Id, item.Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SKU = item.SKU, SerialCode = "00Actual-" + data.Suffix }, data.StaffId);
        var duplicate = await Assert.ThrowsAsync<GoodsReceiptException>(() => staff.Serials.AddAsync(receipt.Id, item.Id,
            new AddReceiptSerialDto { ExpectedVersion = scan.Version, SKU = item.SKU, SerialCode = scan.SerialCode.ToLowerInvariant() }, data.StaffId));
        Assert.Equal("MSG158", duplicate.Code);
        var detail = await staff.Receipts.GetByIdAsync(receipt.Id, data.StaffId);
        Assert.Equal(scan.Version, detail.Version); Assert.Equal(1, detail.Items.Single().ReceivedQty);
        Assert.Equal("00Actual-" + data.Suffix, detail.Items.Single().Serials.Single().SerialCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitAndApproveRevalidatePersistedSerialFormatAndRollBackInvalidData(bool afterSubmit)
    {
        var (data, receipt) = await PendingReceiptAsync(submit: afterSubmit);
        using (var db = _fixture.CreateContext())
        {
            var scan = await db.GoodsReceiptSerials.SingleAsync(x => x.GoodsReceiptId == receipt.Id);
            scan.SerialCode = "INVALID SERIAL WITH SPACE";
            await db.SaveChangesAsync();
        }
        using (var actor = new ReceivingSession(_fixture))
        {
            var error = afterSubmit
                ? await Assert.ThrowsAsync<GoodsReceiptException>(() => actor.Approval.ApproveAsync(receipt.Id, Version(receipt), data.ManagerId))
                : await Assert.ThrowsAsync<GoodsReceiptException>(() => actor.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId));
            Assert.Equal("GR_SERIAL_FORMAT", error.Code);
        }
        using var check = _fixture.CreateContext();
        Assert.Equal(afterSubmit ? "PendingApproval" : "Draft", (await check.GoodsReceipts.FindAsync(receipt.Id))!.Status);
        Assert.False(await check.Stocks.AnyAsync(x => x.ProductVariantId == data.VariantId));
        Assert.False(await check.SerialMovements.AnyAsync(x => x.RefType == "GOODS_RECEIPT" && x.RefId == receipt.Id));
    }

    [Fact]
    public async Task HistoricalSupplierManifestIsPreservedAndDoesNotRestrictActualSerialScan()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using (var db = _fixture.CreateContext())
        {
            (await db.InboundRequests.FindAsync(data.InboundId))!.HasSupplierManifest = true;
            var item = await db.InboundRequestItems.SingleAsync(x => x.InboundRequestId == data.InboundId);
            db.InboundExpectedSerials.Add(new InboundExpectedSerial
            { InboundRequestId = data.InboundId, InboundRequestItemId = item.Id, SerialCode = "LEGACY-" + data.Suffix });
            await db.SaveChangesAsync();
        }
        using var staff = new ReceivingSession(_fixture);
        var receipt = await staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId);
        var scan = await staff.Serials.AddAsync(receipt.Id, receipt.Items.Single().Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SKU = data.SKU, SerialCode = "ACTUAL-" + data.Suffix }, data.StaffId);
        Assert.Equal(1, scan.ReceivedQty);
        Assert.Equal("LEGACY-" + data.Suffix, (await staff.Context.InboundExpectedSerials.SingleAsync(x => x.InboundRequestId == data.InboundId)).SerialCode);
    }

    [Fact]
    public async Task TwoDraftsMayScanSamePhysicalSerialButOnlyOneApprovalCanRegisterIt()
    {
        var data = await _fixture.SeedAsync(expected: 2);
        var receipts = new List<GoodsReceiptDetailDto>();
        for (int index = 0; index < 2; index++)
        {
            using var staff = new ReceivingSession(_fixture);
            var receipt = await staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId);
            var item = receipt.Items.Single();
            var scan = await staff.Serials.AddAsync(receipt.Id, item.Id,
                new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SKU = item.SKU, SerialCode = "SAME-" + data.Suffix }, data.StaffId);
            receipt = await staff.Receipts.UpdateDraftAsync(receipt.Id, new UpdateGoodsReceiptDto
            { ExpectedVersion = scan.Version, Items = new() { new() { ItemId = item.Id, BinId = data.BinId } } }, data.StaffId);
            receipts.Add(await staff.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId));
        }
        using (var manager = new ReceivingSession(_fixture))
            await manager.Approval.ApproveAsync(receipts[0].Id, Version(receipts[0]), data.ManagerId);
        using (var manager = new ReceivingSession(_fixture))
        {
            var error = await Assert.ThrowsAsync<GoodsReceiptException>(() => manager.Approval.ApproveAsync(receipts[1].Id, Version(receipts[1]), data.ManagerId));
            Assert.Equal("MSG158", error.Code);
        }
        using var check = _fixture.CreateContext();
        Assert.Equal(1, await check.SerialNumbers.CountAsync(x => x.ProductVariantId == data.VariantId));
        Assert.Equal(1, (await check.Stocks.SingleAsync(x => x.ProductVariantId == data.VariantId)).OnHandQty);
        Assert.Equal("PendingApproval", (await check.GoodsReceipts.FindAsync(receipts[1].Id))!.Status);
    }

    private static ReceiptVersionDto Version(GoodsReceiptDetailDto receipt) => new() { ExpectedVersion = receipt.Version };
    private async Task<(TestData, GoodsReceiptDetailDto)> PendingReceiptAsync(bool submit = true)
    {
        var data = await _fixture.SeedAsync(expected: 2);
        using var staff = new ReceivingSession(_fixture);
        var receipt = await staff.Receipts.CreateAsync(new CreateGoodsReceiptDto { InboundRequestId = data.InboundId }, data.StaffId);
        var item = receipt.Items.Single();
        var scan = await staff.Serials.AddAsync(receipt.Id, item.Id,
            new AddReceiptSerialDto { ExpectedVersion = receipt.Version, SKU = item.SKU, SerialCode = "RP-" + data.Suffix }, data.StaffId);
        receipt = await staff.Receipts.UpdateDraftAsync(receipt.Id, new UpdateGoodsReceiptDto
        {
            ExpectedVersion = scan.Version, Items = new List<UpdateReceiptBinDto> { new() { ItemId = item.Id, BinId = data.BinId } }
        }, data.StaffId);
        if (submit) receipt = await staff.Receipts.SubmitAsync(receipt.Id, Version(receipt), data.StaffId);
        return (data, receipt);
    }
}
