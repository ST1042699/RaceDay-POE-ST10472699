using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetRole() => HttpContext.Session.GetString("Role");

        [HttpPost("enrolments")]
        public async Task<IActionResult> Enrol([FromBody] EnrolDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Participant")
                return Forbid();

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null)
                return BadRequest("Invalid category");

            var alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e => e.ParticipantID == userId && e.CategoryID == dto.CategoryId);

            if (alreadyEnrolled)
                return Conflict("Participant already enrolled");

            var enrolment = new Enrolment
            {
                ParticipantID = userId.Value,
                CategoryID = dto.CategoryId,
                EnrolledAt = DateTime.Now
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyEnrolments), null, enrolment);
        }

        [HttpGet("enrolments/myenrolments")]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized("Not logged in");

            var enrolments = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .Where(e => e.ParticipantID == userId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.EnrolledAt,
                    Category = e.Category.CategoryName,
                    Event = e.Category.Event.Title,
                    EventDate = e.Category.Event.EventDate
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("events/{eventId}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null)
                return NotFound();

            if (ev.OrganiserID != userId)
                return Forbid();

            var enrolments = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Where(e => e.Category.EventID == eventId)
                .Select(e => new
                {
                    e.EnrolmentID,
                    Participant = e.Participant.FullName,
                    e.Participant.Email,
                    Category = e.Category.CategoryName,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolments);
        }

        [HttpGet("enrolments/{id}")]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Participant)
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound("Record was not found");

            return Ok(new
            {
                enrolment.EnrolmentID,
                Participant = enrolment.Participant.FullName,
                Category = enrolment.Category.CategoryName,
                Event = enrolment.Category.Event.Title,
                enrolment.EnrolledAt
            });
        }
    }
}