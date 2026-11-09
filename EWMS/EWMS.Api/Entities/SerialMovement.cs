namespace EWMS.Api.Entities
{
    public class SerialMovement
    {
        public int Id { get; set; }

        public int SerialNumberId { get; set; }

        public int? FromBinId { get; set; }

        public int? ToBinId { get; set; }

        public int PerformedByUserId { get; set; }

        public string MovementType { get; set; } = string.Empty;

        public string? RefType { get; set; }

        public int? RefId { get; set; }

        public DateTime MovementDate { get; set; }

        public SerialNumber SerialNumber { get; set; } = null!;

        public StorageBin? FromBin { get; set; }

        public StorageBin? ToBin { get; set; }

        public User PerformedByUser { get; set; } = null!;
    }
}
