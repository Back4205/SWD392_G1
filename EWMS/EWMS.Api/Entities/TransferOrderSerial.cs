namespace EWMS.Api.Entities
{
    public class TransferOrderSerial
    {
        public int Id { get; set; }

        public int TransferOrderItemId { get; set; }

        public int SerialNumberId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? DispatchedAt { get; set; }

        public DateTime? ReceivedAt { get; set; }

        public TransferOrderItem TransferOrderItem { get; set; } = null!;

        public SerialNumber SerialNumber { get; set; } = null!;
    }
}
