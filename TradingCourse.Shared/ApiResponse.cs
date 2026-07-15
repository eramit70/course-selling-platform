using System;
using System.Collections.Generic;

namespace TradingCourse.Shared;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<ApiError>? Errors { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Metadata { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, int statusCode, string message, T? data = default, List<ApiError>? errors = null, string traceId = "", object? metadata = null)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
        Data = data;
        Errors = errors;
        TraceId = traceId;
        Timestamp = DateTime.UtcNow;
        Metadata = metadata;
    }
}
