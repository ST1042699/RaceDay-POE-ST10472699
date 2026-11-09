using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api)
    {
        _api = api;
    }

    private bool IsOrganiser()
    {
        return HttpContext.Session.GetInt32("UserId") != null
            && HttpContext.Session.GetString("Role") == "Organiser";
    }

    private IActionResult RequireOrganiser()
    {
        return RedirectToAction("Login", "Account");
    }

    public async Task<IActionResult> Index()
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        var events = await _api.GetAsync<List<EventViewModel>>(
            "api/events") ?? new List<EventViewModel>();

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Upcoming = events.Count(e => e.EventDate >= DateTime.Today);

        return View(events);
    }

    public async Task<IActionResult> Events()
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        var events = await _api.GetAsync<List<EventViewModel>>(
            "api/events") ?? new List<EventViewModel>();

        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        return View(new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        if (!ModelState.IsValid)
            return View(model);

        var created = await _api.PostAsync<EventViewModel>(
            "api/events",
            model);

        if (created == null)
        {
            ModelState.AddModelError(
                "",
                "The event could not be created.");

            return View(model);
        }

        TempData["Success"] =
            "Event created. You can now upload its banner.";

        return RedirectToAction(
            nameof(EditEvent),
            new { id = created.EventID });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        var ev = await _api.GetAsync<EventViewModel>(
            $"api/events/{id}");

        if (ev == null)
            return NotFound();

        return View(new EventFormViewModel
        {
            EventID = ev.EventID,
            Title = ev.Title,
            Description = ev.Description,
            EventType = ev.EventType,
            Distance = ev.Distance,
            EventDate = ev.EventDate,
            Location = ev.Location
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(
        int id,
        EventFormViewModel model)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        if (!ModelState.IsValid)
            return View(model);

        var success = await _api.PutAsync(
            $"api/events/{id}",
            model);

        TempData[success ? "Success" : "Error"] = success
            ? "Event updated."
            : "The event could not be updated.";

        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        var success = await _api.DeleteAsync($"api/events/{id}");

        TempData[success ? "Success" : "Error"] = success
            ? "Event deleted."
            : "The event could not be deleted.";

        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadBanner(
        int id,
        IFormFile file)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Select an image first.";
            return RedirectToAction(nameof(EditEvent), new { id });
        }

        var result = await _api.PostFileAsync(
            $"api/images/event-banner/{id}",
            file);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Event banner uploaded successfully."
                : "Banner upload failed: " + result.Message;

        return RedirectToAction(nameof(EditEvent), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int id)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        ViewBag.EventId = id;

        var categories = await _api.GetAsync<List<CategoryViewModel>>(
            $"api/events/{id}/categories");

        if (categories == null)
            return NotFound("The event or its categories could not be loaded.");

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(
        int id,
        CategoryFormViewModel model)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Check the category details.";
            return RedirectToAction(nameof(Categories), new { id });
        }

        var success = await _api.PostAsync(
            $"api/events/{id}/categories",
            new
            {
                model.CategoryName,
                model.DistanceKM,
                model.EntryFee
            });

        TempData[success ? "Success" : "Error"] = success
            ? "Category created."
            : "Category could not be created.";

        return RedirectToAction(nameof(Categories), new { id });
    }

    public async Task<IActionResult> Enrolments(int id)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        var data = await _api.GetAsync<List<EnrolmentViewModel>>(
            $"api/events/{id}/enrolments");

        if (data == null)
        {
            TempData["Error"] =
                "Enrolments could not be loaded.";

            return RedirectToAction(nameof(Events));
        }

        ViewBag.EventId = id;

        return View(data);
    }

    [HttpGet]
    public IActionResult AddResult(int enrolmentId)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        return View(new ResultFormViewModel
        {
            EnrolmentId = enrolmentId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddResult(ResultFormViewModel model)
    {
        if (!IsOrganiser())
            return RequireOrganiser();

        if (!ModelState.IsValid)
            return View(model);

        var success = await _api.PostAsync(
            "api/results",
            new
            {
                model.EnrolmentId,
                model.CompletionTime,
                model.RankPosition
            });

        TempData[success ? "Success" : "Error"] = success
            ? "Result recorded."
            : "Result could not be recorded. Check the enrolment and whether a result already exists.";

        return RedirectToAction(nameof(Events));
    }
}