namespace TradingCourse.Shared;

public class ApiError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public ApiError()
    {
    }

    public ApiError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
