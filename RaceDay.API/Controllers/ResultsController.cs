using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        private int? GetUserId() => HttpContext.Session.GetInt32("UserId");
        private string? GetRole() => HttpContext.Session.GetString("Role");

        [HttpPost("results")]
        public async Task<IActionResult> CreateResult([FromBody] CreateResultDto dto)
        {
            var userId = GetUserId();
            var role = GetRole();

            if (userId == null)
                return Unauthorized();

            if (role != "Organiser")
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TimeSpan.TryParse(dto.CompletionTime, out var time))
                return BadRequest("Invalid time format. Use HH:mm:ss");

            var enrolment = await _context.Enrolments
                .Include(e => e.Category)
                    .ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentId);

            if (enrolment == null)
                return BadRequest("Invalid enrolment");

            if (enrolment.Category.Event.OrganiserID != userId)
                return Forbid();

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == dto.EnrolmentId))
                return Conflict("Result already exists for this enrolment");

            var result = new Result
            {
                EnrolmentID = dto.EnrolmentId,
                CompletionTime = time,
                RankPosition = dto.RankPosition,
                RecordedAt = DateTime.Now
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyResults), null, result);
        }

        [HttpGet("results/my-results")]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Category)
                        .ThenInclude(c => c.Event)
                .Where(r => r.Enrolment.ParticipantID == userId)
                .Select(r => new
                {
                    r.ResultID,
                    Event = r.Enrolment.Category.Event.Title,
                    Category = r.Enrolment.Category.CategoryName,
                    CompletionTime = r.CompletionTime.ToString(@"hh\:mm\:ss"),
                    r.RankPosition,
                    r.RecordedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("categories/{categoryId}/results")]
        public async Task<IActionResult> GetCategoryResults(int categoryId)
        {
            var exists = await _context.Categories.AnyAsync(c => c.CategoryID == categoryId);
            if (!exists)
                return NotFound("Category was not found");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e.Participant)
                .Where(r => r.Enrolment.CategoryID == categoryId)
                .OrderBy(r => r.RankPosition)
                .Select(r => new
                {
                    r.RankPosition,
                    Participant = r.Enrolment.Participant.FullName,
                    CompletionTime = r.CompletionTime.ToString(@"hh\:mm\:ss")
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}