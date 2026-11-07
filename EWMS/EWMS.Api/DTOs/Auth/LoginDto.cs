using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EWMS.Api.DTOs.Auth
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class LoginDto
    {
        [Required, StringLength(50)]
        public string UserName { get; set; } = string.Empty;
        [Required, StringLength(256)]
        public string Password { get; set; } = string.Empty;
    }

    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class ChangePasswordDto
    {
        [Required, StringLength(256)]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required, StringLength(128, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
        [Required, Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
