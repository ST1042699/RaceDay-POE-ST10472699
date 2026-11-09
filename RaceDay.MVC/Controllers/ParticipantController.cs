using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;

    public ParticipantController(RaceDayApiClient api)
    {
        _api = api;
    }

    private bool IsParticipant()
    {
        return HttpContext.Session.GetInt32("UserId") != null
            && HttpContext.Session.GetString("Role") == "Participant";
    }

    private IActionResult RequireParticipant()
    {
        return RedirectToAction("Login", "Account");
    }

    public async Task<IActionResult> MyEvents()
    {
        if (!IsParticipant())
            return RequireParticipant();

        var enrolments =
            await _api.GetAsync<List<MyEnrolmentViewModel>>(
                "api/enrolments/myenrolments");

        return View(enrolments ?? new List<MyEnrolmentViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(
        int eventId,
        EnrolFormViewModel model)
    {
        if (!IsParticipant())
            return RequireParticipant();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Select a valid event category.";
            return RedirectToAction("Details", "Home", new { id = eventId });
        }

        var success = await _api.PostAsync(
            "api/enrolments",
            new { model.CategoryId });

        TempData[success ? "Success" : "Error"] = success
            ? "You have successfully enrolled."
            : "Enrolment failed. You may already be enrolled.";

        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    public async Task<IActionResult> MyResults()
    {
        if (!IsParticipant())
            return RequireParticipant();

        var results = await _api.GetAsync<List<ResultViewModel>>(
            "api/results/my-results");

        return View(results ?? new List<ResultViewModel>());
    }
}