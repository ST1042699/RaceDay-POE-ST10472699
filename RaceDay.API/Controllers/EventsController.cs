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

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetRole() => HttpContext.Session.GetString("Role");

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
                    Organiser = e.Organiser.FullName
                })
                .ToListAsync();

            return Ok(events);
        }

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

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EventType != "Run" && dto.EventType != "Walk" && dto.EventType != "Cycle")
                return BadRequest("EventType must be Run, Walk or Cycle");

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

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound("Event was not found");

            if (ev.OrganiserID != userId)
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            ev.Title = dto.Title;
            ev.Description = dto.Description;
            ev.EventType = dto.EventType;
            ev.Distance = dto.Distance;
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location;

            await _context.SaveChangesAsync();
            return Ok(ev);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
                return NotFound("Event was not found");

            if (ev.OrganiserID != userId)
                return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Event deleted successfully" });
        }
    }
}