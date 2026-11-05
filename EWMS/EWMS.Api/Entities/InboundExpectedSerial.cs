namespace EWMS.Api.Entities
{
    // Serial nhà cung cấp dự kiến giao. Chỉ Goods Receipt được duyệt mới tạo SerialNumber.
    public class InboundExpectedSerial
    {
        public int Id { get; set; }
        public int InboundRequestId { get; set; }
        public int InboundRequestItemId { get; set; }
        public string SerialCode { get; set; } = string.Empty;
        public InboundRequestItem InboundRequestItem { get; set; } = null!;
    }
}
