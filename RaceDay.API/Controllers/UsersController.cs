using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public UsersController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Not logged in");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return Unauthorized();

            return Ok(new
            {
                user.UserID,
                user.FullName,
                user.Email,
                Role = user.Role.Rolename,
                user.CreatedAt
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Not logged in");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized();

            // Check email uniqueness if changed
            if (user.Email != dto.Email && await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email already in use");

            user.FullName = dto.FullName;
            user.Email = dto.Email;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.UserID,
                user.FullName,
                user.Email,
                Message = "Profile updated successfully"
            });
        }
    }
}