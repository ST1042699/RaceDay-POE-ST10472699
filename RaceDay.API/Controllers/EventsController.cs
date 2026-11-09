
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetUserId() =>
            HttpContext.Session.GetInt32("UserId");

        private string? GetRole() =>
            HttpContext.Session.GetString("Role");

        // GET: api/events
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organiser)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventID,
                    e.Title,
                    e.Description,
                    e.EventType,
                    e.Distance,
                    e.EventDate,
                    e.Location,
                    e.BannerImageUrl,
                    Organiser = e.Organiser.FullName
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound("Event does not exist");

            return Ok(new
            {
                ev.EventID,
                ev.Title,
                ev.Description,
                ev.EventType,
                ev.Distance,
                ev.EventDate,
                ev.Location,
                ev.BannerImageUrl,
                Organiser = ev.Organiser.FullName,

                Categories = ev.Categories.Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.DistanceKM,
                    c.EntryFee
                })
            });
        }

        // POST: api/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent(
            [FromBody] CreateEventDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized("Please log in.");

            if (role != "Organiser")
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EventType != "Run" &&
                dto.EventType != "Walk" &&
                dto.EventType != "Cycle")
            {
                return BadRequest(
                    "EventType must be Run, Walk or Cycle.");
            }

            var ev = new Event
            {
                OrganiserID = userId.Value,
                Title = dto.Title,
                Description = dto.Description,
                EventType = dto.EventType,
                Distance = dto.Distance,
                EventDate = dto.EventDate,
                Location = dto.Location,
                CreatedAt = DateTime.Now
            };

            _context.Events.Add(ev);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEvent),
                new { id = ev.EventID },
                ev);
        }

        // PUT: api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            [FromBody] UpdateEventDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized("Please log in.");

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound("Event was not found.");

            if (ev.OrganiserID != userId.Value)
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EventType != "Run" &&
                dto.EventType != "Walk" &&
                dto.EventType != "Cycle")
            {
                return BadRequest(
                    "EventType must be Run, Walk or Cycle.");
            }

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.EventType = dto.EventType;
            ev.Distance = dto.Distance;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;

            await _context.SaveChangesAsync();

            return Ok(ev);
        }

        // DELETE: api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetUserId();
            var role = GetRole();

            // Check login
            if (userId == null)
                return Unauthorized("Please log in.");

            // Only organisers can delete events
            if (role != "Organiser")
                return Forbid();

            // Find event
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null)
                return NotFound("Event was not found.");

            // Check event ownership
            if (ev.OrganiserID != userId.Value)
                return Forbid();

            // Find categories belonging to this event
            var categories = await _context.Categories
                .Where(c => c.EventID == id)
                .ToListAsync();

            var categoryIds = categories
                .Select(c => c.CategoryID)
                .ToList();

            // Check whether participants have enrolled
            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => categoryIds.Contains(e.CategoryID));

            // Protect participant enrolments and results
            if (hasEnrolments)
            {
                return Conflict(
                    "This event cannot be deleted because participants " +
                    "have enrolled in its categories. Their enrolments " +
                    "and results must be preserved.");
            }

            // Delete categories first
            if (categories.Any())
            {
                _context.Categories.RemoveRange(categories);
            }

            // Delete the event
            _context.Events.Remove(ev);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Event and its categories deleted successfully."
            });
        }
    }
}
