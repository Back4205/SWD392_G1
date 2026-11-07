
namespace EWMS.Api.Entities
{
    public class InboundRequest
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }

        public int? SupplierId { get; set; }

        public int RequestedByUserId { get; set; }

        public int? AssignedStaffUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public string? SupplierCommitmentStatus { get; set; }

        // Dữ liệu mở rộng cũ được giữ để không mất lịch sử; không dùng làm điều kiện workflow.
        public bool HasSupplierManifest { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? RejectionReason { get; set; }

        public string? ApprovalNote { get; set; }

        public DateTime? DecisionDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        public Supplier? Supplier { get; set; }

        public User RequestedByUser { get; set; } = null!;

        public User? AssignedStaffUser { get; set; }

        public User? ApprovedByUser { get; set; }

        public void UpdateStatus(string newStatus)
        {
            if (newStatus != "Pending" && newStatus != "Approved" && newStatus != "Rejected"
                && newStatus != "Canceled" && newStatus != "PartiallyReceived" && newStatus != "Completed")
                throw new ArgumentException("Unknown inbound request status.", nameof(newStatus));
            bool allowed = Status == newStatus || (Status == "" && newStatus == "Pending")
                || (Status == "Pending" && (newStatus == "Approved" || newStatus == "Rejected" || newStatus == "Canceled"))
                || (Status == "Approved" && (newStatus == "PartiallyReceived" || newStatus == "Completed"))
                || (Status == "PartiallyReceived" && newStatus == "Completed");
            if (!allowed) throw new InvalidOperationException($"Cannot change inbound from {Status} to {newStatus}.");
            Status = newStatus;
        }

        public void RecordSupplierCommitment(string commitmentStatus, DateTime? expectedDate)
        {
            if (Status != "Pending")
                throw new InvalidOperationException("Only pending inbound requests can be updated.");
            if (commitmentStatus != "Accepted" && commitmentStatus != "Rejected")
                throw new ArgumentException("Commitment status must be Accepted or Rejected.");
            if (commitmentStatus == "Accepted" && !expectedDate.HasValue)
                throw new ArgumentException("Expected delivery date is required.");
            SupplierCommitmentStatus = commitmentStatus;
            ExpectedDeliveryDate = commitmentStatus == "Accepted" ? expectedDate : null;
            if (commitmentStatus == "Rejected") UpdateStatus("Canceled");
        }

        public void RecordApproval(int approvedByUserId)
        {
            if (Status != "Pending")
                throw new InvalidOperationException("Only pending inbound requests can be reviewed.");
            if (approvedByUserId <= 0) throw new ArgumentOutOfRangeException(nameof(approvedByUserId));
            if (SupplierCommitmentStatus != "Accepted" && SupplierCommitmentStatus != "Confirmed")
                throw new InvalidOperationException("Supplier acceptance is required before approval.");
            ApprovedByUserId = approvedByUserId;
            DecisionDate = DateTime.UtcNow;
            ApprovalNote = null;
            RejectionReason = null;
            UpdateStatus("Approved");
        }

        public void RecordRejection(string reason, int? decidedByUserId = null)
        {
            if (Status != "Pending")
                throw new InvalidOperationException("Only pending inbound requests can be reviewed.");
            reason = reason?.Trim() ?? "";
            if (reason.Length == 0 || reason.Length > 255)
                throw new ArgumentException("A rejection reason of 1-255 characters is required.", nameof(reason));
            ApprovedByUserId = decidedByUserId;
            DecisionDate = DateTime.UtcNow;
            ApprovalNote = null;
            RejectionReason = reason;
            UpdateStatus("Rejected");
        }
    }
}
