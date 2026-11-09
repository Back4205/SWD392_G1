namespace EWMS.Api.DTOs.GoodsReceipts
{
    // Thông tin người đang thao tác do backend xác định, không tin role frontend gửi lên.
    public class GoodsReceiptSessionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<int> WarehouseIds { get; set; } = new List<int>();
    }

    // Các yêu cầu nhập còn thiếu hàng để Staff chọn khi tạo phiếu.
    public class ReceivableInboundDto
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ReceivingLineDto> Lines { get; set; } = new List<ReceivingLineDto>();
    }

    public class ReceiptBinLookupDto
    {
        public int Id { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public string BinName { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public decimal MaxWeightKg { get; set; }
        public decimal AvailableWeightKg { get; set; }
    }
}
