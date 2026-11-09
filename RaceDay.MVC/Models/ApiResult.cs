namespace RaceDay.MVC.Models;

public class ApiResult<T>
{
    public bool Success { get; set; }

    public T? Data { get; set; }

    public string Message { get; set; } = string.Empty;
}