namespace EWMS.Api.DTOs.Auth
{
    public class AuthUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<int> WarehouseIds { get; set; } = new List<int>();
    }

    public class LoginResponseDto
    {
        public AuthUserDto User { get; set; } = new AuthUserDto();
        public DateTime ExpiresAt { get; set; }
        // SDS AuthResponse trả JWT. Web dùng cookie HttpOnly, không lưu token vào localStorage.
        public string Token { get; set; } = string.Empty;
    }
}
