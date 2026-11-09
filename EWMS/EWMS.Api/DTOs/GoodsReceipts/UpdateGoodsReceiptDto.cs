using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.GoodsReceipts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class UpdateGoodsReceiptDto : ReceiptVersionDto
    {
        [Required]
        public List<UpdateReceiptBinDto>? Items { get; set; }
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class UpdateReceiptBinDto
    {
        [Range(1, int.MaxValue)]
        public int ItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int? BinId { get; set; }
        // Không có ReceivedQty: backend tự đếm từ số scan hợp lệ.
    }
}
