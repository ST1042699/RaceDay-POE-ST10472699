using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? eventType)
    {
        var events = await _api.GetAsync<List<EventViewModel>>(
            "api/events") ?? new List<EventViewModel>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.Title.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            events = events.Where(e =>
                e.EventType.Equals(
                    eventType,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.Search = search;
        ViewBag.EventType = eventType;

        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventViewModel>(
            $"api/events/{id}");

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    public IActionResult Error()
    {
        return View();
    }
}