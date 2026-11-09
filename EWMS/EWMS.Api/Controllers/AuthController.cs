using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EWMS.Api.Common;
using EWMS.Api.DTOs.Auth;
using EWMS.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EWMS.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly IAntiforgery _antiforgery;
        private readonly IWebHostEnvironment _environment;

        public AuthController(AuthService service, IAntiforgery antiforgery, IWebHostEnvironment environment)
        {
            _service = service;
            _antiforgery = antiforgery;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpGet("csrf")]
        public IActionResult GetCsrf()
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(new { requestToken = _antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _service.LoginAsync(dto);
                Response.Cookies.Append(JwtSessionEvents.CookieName, result.Token, CookieOptions(result.ExpiresAt));
                Response.Headers.CacheControl = "no-store";
                return Ok(result);
            }
            catch (GoodsReceiptException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(await _service.GetUserAsync(GetUserId()));
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _service.LogoutAsync(Guid.ParseExact(User.FindFirstValue(JwtRegisteredClaimNames.Jti)!, "N"));
            Response.Cookies.Delete(JwtSessionEvents.CookieName, CookieOptions(null));
            return NoContent();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            try
            {
                await _service.ChangePasswordAsync(GetUserId(), dto);
                Response.Cookies.Delete(JwtSessionEvents.CookieName, CookieOptions(null));
                return NoContent();
            }
            catch (GoodsReceiptException ex)
            {
                return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
            }
        }

        private int GetUserId() { return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); }

        private CookieOptions CookieOptions(DateTime? expiresAt)
        {
            return new CookieOptions
            {
                HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = !_environment.IsDevelopment(),
                Path = "/", Expires = expiresAt, IsEssential = true
            };
        }
    }
}
