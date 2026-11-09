using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Services;

public class RaceDayApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _accessor;
    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(
        IHttpClientFactory factory,
        IHttpContextAccessor accessor)
    {
        _factory = factory;
        _accessor = accessor;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient("RaceDayAPI");

        var context = _accessor.HttpContext;

        var cookie = context?.Session.GetString("ApiSessionCookie");

        if (!string.IsNullOrWhiteSpace(cookie))
        {
            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Cookie",
                $".RaceDay.Session={cookie}");
        }

        return client;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return default;

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<T?> PostAsync<T>(
        string url,
        object data)
    {
        using var client = CreateClient();

        var json = JsonSerializer.Serialize(data, _jsonOptions);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
            return default;

        var result = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(result))
            return default;

        return JsonSerializer.Deserialize<T>(result, _jsonOptions);
    }

    public async Task<bool> PostAsync(
        string url,
        object data,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();

        var json = JsonSerializer.Serialize(data, _jsonOptions);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(
            url,
            content,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PutAsync(
        string url,
        object data)
    {
        using var client = CreateClient();

        var json = JsonSerializer.Serialize(data, _jsonOptions);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await client.PutAsync(url, content);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(string url)
    {
        using var client = CreateClient();
        using var response = await client.DeleteAsync(url);

        return response.IsSuccessStatusCode;
    }

    public async Task<(bool Success, string Message)> PostFileAsync(
        string url,
        IFormFile file)
    {
        using var client = CreateClient();
        using var multipart = new MultipartFormDataContent();

        await using var stream = file.OpenReadStream();

        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(file.ContentType);

        multipart.Add(fileContent, "file", file.FileName);

        using var response = await client.PostAsync(url, multipart);

        var message = await response.Content.ReadAsStringAsync();

        return (
            response.IsSuccessStatusCode,
            string.IsNullOrWhiteSpace(message)
                ? response.ReasonPhrase ?? "Request completed."
                : message
        );
    }

    public async Task<(bool Success, string Message)> LoginAsync(
        string email,
        string password)
    {
        using var client = _factory.CreateClient("RaceDayAPI");

        using var response = await client.PostAsJsonAsync(
            "api/auth/login",
            new { Email = email, Password = password });

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return (false, "Login failed. Check your email and password.");

        string? cookieHeader = null;

        if (response.Headers.TryGetValues(
                "Set-Cookie", out var cookieHeaders))
        {
            cookieHeader = cookieHeaders.FirstOrDefault(value =>
                value.StartsWith(
                    ".RaceDay.Session=",
                    StringComparison.OrdinalIgnoreCase));
        }

        var cookieValue = cookieHeader?
            .Split(';', 2)[0]
            .Split('=', 2)
            .ElementAtOrDefault(1);

        var context = _accessor.HttpContext;

        if (context == null || string.IsNullOrWhiteSpace(cookieValue))
        {
            return (
                false,
                "The API login succeeded, but its session cookie was not received."
            );
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        context.Session.SetString("ApiSessionCookie", cookieValue);
        context.Session.SetInt32(
            "UserId",
            root.GetProperty("userID").GetInt32());

        context.Session.SetString(
            "FullName",
            root.GetProperty("fullName").GetString() ?? "");

        context.Session.SetString(
            "Role",
            root.GetProperty("role").GetString() ?? "");

        return (true, "Login successful.");
    }

    public async Task LogoutAsync()
    {
        try
        {
            using var client = CreateClient();
            using var response =
                await client.PostAsync("api/auth/logout", null);
        }
        finally
        {
            _accessor.HttpContext?.Session.Clear();
        }
    }
}