using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PersonalDiaryApp.API.Dtos;
using PersonalDiaryApp.Entities;
using Microsoft.AspNetCore.Authorization;

namespace PersonalDiaryApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        public AuthController(UserManager<ApplicationUser> userManager,
                         IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        // ── REGISTER ─────────────────────────────────────────────────────────────
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Sadece Email + Password, ConfirmPassword yok
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Kayıt başarılı. Lütfen giriş yapın." });
        }

        // ── LOGIN ────────────────────────────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized("Geçersiz kullanıcı adı veya şifre");

            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256);

            // Burada artık JwtRegisteredClaimNames değil, ClaimTypes.Name kullanıyoruz.
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email,          user.Email),
        // Uygulamanızda FullName yoksa UserName kullanın
        new Claim(ClaimTypes.Name,           user.UserName!)

    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(tokenDescriptor);
            var token = handler.WriteToken(securityToken);

            return Ok(new { token });
        }


        // DELETE: /api/Auth/delete-account
        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            // 1) Kullanıcının kimliğini al
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return BadRequest("Token geçersiz.");

            // 2) Mevcut kullanıcıyı bulun
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // 3) Kullanıcıyı silin
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            return Ok("Hesabınız başarıyla silindi.");
        }

    }
}

