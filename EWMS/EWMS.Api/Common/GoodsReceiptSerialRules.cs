using System.Text.RegularExpressions;

namespace EWMS.Api.Common
{
    // Dùng chung khi Scan, Submit và Approve, tránh mỗi bước kiểm tra một kiểu.
    public static class GoodsReceiptSerialRules
    {
        public static void Validate(string code, string? configuredPattern)
        {
            string pattern = configuredPattern ?? @"\A[A-Za-z0-9._-]{1,100}\z";
            bool valid = !string.IsNullOrWhiteSpace(code) && code.Length <= 100
                && code.All(c => c >= 33 && c <= 126);
            if (valid)
            {
                try
                {
                    var match = Regex.Match(code, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                    valid = match.Success && match.Index == 0 && match.Length == code.Length;
                }
                catch (RegexMatchTimeoutException)
                {
                    valid = false;
                }
            }
            if (!valid)
            {
                throw new GoodsReceiptException(400, "GR_SERIAL_FORMAT", "Invalid Serial/IMEI format.");
            }
        }
    }
}
