
using Microsoft.AspNetCore.Identity;

namespace EWMS.Api.Entities
{
    public class User
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Status { get; set; } = string.Empty;

        public Role Role { get; set; } = null!;

        public bool VerifyPassword(string input, IPasswordHasher<User> passwordHasher)
        {
            try
            {
                return passwordHasher.VerifyHashedPassword(this, PasswordHash, input)
                    != PasswordVerificationResult.Failed;
            }
            catch (FormatException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public void SetPasswordHash(string newHash)
        {
            if (string.IsNullOrWhiteSpace(newHash)) throw new ArgumentException("Password hash is required.", nameof(newHash));
            PasswordHash = newHash;
        }
    }
}
