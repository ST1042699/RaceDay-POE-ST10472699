
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private readonly RaceDayDbContext _context;
        private readonly IBlobStorageService _blobStorage;

        private const long MaxFileSize = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        public ImagesController(
            RaceDayDbContext context,
            IBlobStorageService blobStorage)
        {
            _context = context;
            _blobStorage = blobStorage;
        }

        private int? GetUserId() =>
            HttpContext.Session.GetInt32("UserId");

        private string? GetRole() =>
            HttpContext.Session.GetString("Role");

        // ==========================================
        // UPLOAD EVENT BANNER
        // ==========================================

        [HttpPost("event-banner/{eventId:int}")]
        [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
        public async Task<IActionResult> UploadEventBanner(
            int eventId,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized("Please log in.");

            if (GetRole() != "Organiser")
                return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("Select an image.");

            if (file.Length > MaxFileSize)
                return BadRequest("The image must be 5 MB or smaller.");

            if (!AllowedTypes.Contains(file.ContentType))
                return BadRequest("Use a JPEG, PNG or WebP image.");

            var ev = await _context.Events
                .FirstOrDefaultAsync(
                    e => e.EventID == eventId,
                    cancellationToken);

            if (ev == null)
                return NotFound("Event not found.");

            if (ev.OrganiserID != userId.Value)
                return Forbid();

            await using var stream = file.OpenReadStream();

            var imageUrl = await _blobStorage.UploadImageAsync(
                stream,
                Path.GetFileName(file.FileName),
                file.ContentType,
                cancellationToken);

            ev.BannerImageUrl = imageUrl;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                ev.EventID,
                ev.BannerImageUrl,
                Message = "Event banner uploaded successfully."
            });
        }

        // ==========================================
        // UPLOAD PROFILE PICTURE
        // ==========================================

        [HttpPost("profile-picture")]
        [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
        public async Task<IActionResult> UploadProfilePicture(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized("Please log in.");

            if (file == null || file.Length == 0)
                return BadRequest("Select an image.");

            if (file.Length > MaxFileSize)
                return BadRequest("The image must be 5 MB or smaller.");

            if (!AllowedTypes.Contains(file.ContentType))
                return BadRequest("Use a JPEG, PNG or WebP image.");

            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.UserID == userId.Value,
                    cancellationToken);

            if (user == null)
                return NotFound("User not found.");

            await using var stream = file.OpenReadStream();

            var imageUrl = await _blobStorage.UploadImageAsync(
                stream,
                Path.GetFileName(file.FileName),
                file.ContentType,
                cancellationToken);

            user.ProfilePictureUrl = imageUrl;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                user.UserID,
                user.ProfilePictureUrl,
                Message = "Profile picture uploaded successfully."
            });
        }

        // ==========================================
        // DOWNLOAD / DISPLAY EVENT BANNER
        // ==========================================

        [HttpGet("event-banner/{eventId:int}")]
        public async Task<IActionResult> GetEventBanner(
            int eventId,
            CancellationToken cancellationToken)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(
                    e => e.EventID == eventId,
                    cancellationToken);

            if (ev == null)
                return NotFound("Event not found.");

            if (string.IsNullOrWhiteSpace(ev.BannerImageUrl))
                return NotFound("This event has no banner.");

            try
            {
                var blobUri = new Uri(ev.BannerImageUrl);

                var blobName = Uri.UnescapeDataString(
                    blobUri.Segments.Last());

                var image = await _blobStorage.DownloadImageAsync(
                    blobName,
                    cancellationToken);

                return File(
                    image.Stream,
                    image.ContentType);
            }
            catch (Azure.RequestFailedException)
            {
                return NotFound(
                    "The event banner could not be found in Azure Storage.");
            }
            catch (UriFormatException)
            {
                return BadRequest("The saved banner URL is invalid.");
            }
        }
    }
}
