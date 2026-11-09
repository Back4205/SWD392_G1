namespace EWMS.Api.Entities
{
    // Lưu phiên trên SQL để Logout/đổi mật khẩu thu hồi JWT ngay, không chờ token hết hạn.
    public class AuthSession
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public User User { get; set; } = null!;
    }
}
