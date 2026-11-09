
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using BCrypt.Net;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            RaceDayDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.RoleId != 1 && dto.RoleId != 2)
                return BadRequest(
                    "RoleId must be 1 (Organiser) or 2 (Participant)");

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("This email already exists");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleID = dto.RoleId,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new
            {
                user.UserID,
                user.FullName,
                user.Email,
                Role = user.RoleID == 1 ? "Organiser" : "Participant"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }

            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("Role", user.Role.Rolename);
            HttpContext.Session.SetString("FullName", user.FullName);

            return Ok(new
            {
                user.UserID,
                user.FullName,
                user.Email,
                Role = user.Role.Rolename,
                Message = "Logged in successfully"
            });
        }

        // TEMPORARY LOCAL DEVELOPMENT PASSWORD RESET
        // This endpoint must never be deployed to production.

        [HttpPost("reset-password-local")]
        public async Task<IActionResult> ResetPasswordLocal(
            [FromBody] LocalPasswordResetRequest request)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Email and new password are required.");
            }

            if (request.NewPassword.Length < 8)
                return BadRequest(
                    "The new password must contain at least 8 characters.");

            var email = request.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return NotFound("No account was found with that email.");

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Password successfully reset.",
                user.Email,
                user.FullName,
                user.UserID
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return Ok(new
            {
                Message = "Logged out successfully"
            });
        }

        public class LocalPasswordResetRequest
        {
            public string Email { get; set; } = string.Empty;

            public string NewPassword { get; set; } = string.Empty;
        }
    }
}
