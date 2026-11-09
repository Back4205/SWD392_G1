using EWMS.Api.Entities;
using Xunit;

namespace EWMS.Api.Tests;

public class InboundEntityBehaviorTests
{
    [Fact]
    public void AcceptanceRecordsDateAndApprovalRecordsManagerWithoutReceivingStock()
    {
        var request = new InboundRequest();
        request.UpdateStatus("Pending");
        var delivery = DateTime.Today.AddDays(1);
        request.RecordSupplierCommitment("Accepted", delivery);
        Assert.Equal("Pending", request.Status);
        Assert.Equal(delivery, request.ExpectedDeliveryDate);
        request.RecordApproval(7);
        Assert.Equal("Approved", request.Status);
        Assert.Equal(7, request.ApprovedByUserId);
        Assert.NotNull(request.DecisionDate);
        Assert.Null(request.RejectionReason);
        Assert.Throws<InvalidOperationException>(() => request.RecordRejection("Changed mind", 7));
    }

    [Fact]
    public void SupplierRejectionCancelsAndPreventsLaterCommitment()
    {
        var request = new InboundRequest { Status = "Pending" };
        request.RecordSupplierCommitment("Rejected", DateTime.Today);
        Assert.Equal("Canceled", request.Status);
        Assert.Equal("Rejected", request.SupplierCommitmentStatus);
        Assert.Null(request.ExpectedDeliveryDate);
        Assert.Throws<InvalidOperationException>(() => request.RecordSupplierCommitment("Accepted", DateTime.Today));
        Assert.Throws<InvalidOperationException>(() => request.RecordApproval(7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyManagerRejectionDoesNotChangePendingRequest(string reason)
    {
        var request = new InboundRequest { Status = "Pending" };
        Assert.Throws<ArgumentException>(() => request.RecordRejection(reason, 7));
        Assert.Equal("Pending", request.Status);
        Assert.Null(request.ApprovedByUserId);
        Assert.Null(request.DecisionDate);
    }

    [Fact]
    public void ManagerRejectionStoresReasonAndBlocksApproval()
    {
        var request = new InboundRequest { Status = "Pending" };
        request.RecordRejection("  Wrong delivery quantities  ", 7);
        Assert.Equal("Rejected", request.Status);
        Assert.Equal("Wrong delivery quantities", request.RejectionReason);
        Assert.Equal(7, request.ApprovedByUserId);
        Assert.NotNull(request.DecisionDate);
        Assert.Throws<InvalidOperationException>(() => request.RecordApproval(7));
    }

    [Fact]
    public void ApprovalRequiresSupplierAcceptanceAndDoesNotMutateOnFailure()
    {
        var request = new InboundRequest { Status = "Pending" };
        Assert.Throws<InvalidOperationException>(() => request.RecordApproval(7));
        Assert.Equal("Pending", request.Status);
        Assert.Null(request.ApprovedByUserId);
        Assert.Null(request.DecisionDate);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(4, 0)]
    [InlineData(4, -1)]
    public void InvalidConfirmedDetailsDoNotReplaceExistingLine(int variantId, int quantity)
    {
        var item = new InboundRequestItem { ProductVariantId = 9, ExpectedQty = 5 };
        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateConfirmedDetails(variantId, quantity));
        Assert.Equal(9, item.ProductVariantId);
        Assert.Equal(5, item.ExpectedQty);
    }
}
