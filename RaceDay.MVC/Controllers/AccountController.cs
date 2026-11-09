using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;
using System.Net.Http.Json;

namespace RaceDay.MVC.Controllers;

public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;
    private readonly IHttpClientFactory _factory;

    public AccountController(
        RaceDayApiClient api,
        IHttpClientFactory factory)
    {
        _api = api;
        _factory = factory;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.LoginAsync(
            model.Email,
            model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        var role = HttpContext.Session.GetString("Role");

        return RedirectToAction(
            role == "Organiser" ? "Index" : "Index",
            role == "Organiser" ? "Organiser" : "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (model.RoleId != 1 && model.RoleId != 2)
        {
            ModelState.AddModelError(
                nameof(model.RoleId),
                "Choose a valid account type.");

            return View(model);
        }

        using var client = _factory.CreateClient("RaceDayAPI");

        var response = await client.PostAsJsonAsync(
            "api/auth/register",
            new
            {
                model.FullName,
                model.Email,
                model.Password,
                RoleId = model.RoleId
            });

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(
                "",
                "Registration failed. This email may already be registered.");

            return View(model);
        }

        TempData["Success"] =
            "Registration successful. Please log in.";

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction(nameof(Login));

        var profile =
            await _api.GetAsync<ProfileViewModel>("api/users/profile");

        if (profile == null)
        {
            TempData["Error"] = "Unable to load your profile.";
            return RedirectToAction("Index", "Home");
        }

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileViewModel model)
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            var existing =
                await _api.GetAsync<ProfileViewModel>("api/users/profile");

            if (existing != null)
            {
                existing.FullName = model.FullName;
                existing.Email = model.Email;
                return View(existing);
            }

            return RedirectToAction(nameof(Profile));
        }

        var success = await _api.PutAsync(
            "api/users/profile",
            model);

        TempData[success ? "Success" : "Error"] = success
            ? "Profile updated successfully."
            : "The profile could not be updated.";

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction(nameof(Login));

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an image.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.PostFileAsync(
            "api/images/profile-picture",
            file);

        TempData[result.Success ? "Success" : "Error"] =
            result.Success
                ? "Profile picture uploaded."
                : "Upload failed: " + result.Message;

        return RedirectToAction(nameof(Profile));
    }
}