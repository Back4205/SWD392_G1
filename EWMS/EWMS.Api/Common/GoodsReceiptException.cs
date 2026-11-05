namespace EWMS.Api.Common
{
    // Service báo lỗi; Controller dùng StatusCode/Code để trả HTTP cho frontend.
    public class GoodsReceiptException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }

        public GoodsReceiptException(int statusCode, string code, string message) : base(message)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }

    public static class GoodsReceiptStatus
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}
