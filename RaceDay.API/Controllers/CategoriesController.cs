using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events/{eventId}/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetRole() => HttpContext.Session.GetString("Role");

        [HttpGet]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            var exists = await _context.Events.AnyAsync(e => e.EventID == eventId);
            if (!exists)
                return NotFound("Event not found");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKM,
                    c.EntryFee
                })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CreateCategoryDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
                return NotFound("Event not found");

            if (ev.OrganiserID != userId)
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = new Category
            {
                EventID = eventId,
                CategoryName = dto.CategoryName,
                DistanceKM = dto.DistanceKM,
                EntryFee = dto.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategories), new { eventId }, category);
        }
    }
}