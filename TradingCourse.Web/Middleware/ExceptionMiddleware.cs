using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingCourse.Shared;

namespace TradingCourse.Web.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Allow EF Core tools or Host builder to abort gracefully
            if (ex.GetType().Name == "HostAbortedException")
            {
                throw;
            }

            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                await HandleApiExceptionAsync(context, ex);
            }
            else
            {
                // Rethrow and let the MVC UseExceptionHandler handle it for HTML pages
                throw;
            }
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var message = _env.IsDevelopment() ? exception.Message : "An unexpected server error occurred.";
        
        List<ApiError>? errors = null;
        if (_env.IsDevelopment())
        {
            errors = new List<ApiError>
            {
                new ApiError("ExceptionType", exception.GetType().FullName ?? "Unknown"),
                new ApiError("StackTrace", exception.StackTrace ?? string.Empty)
            };
        }

        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = context.Response.StatusCode,
            Message = message,
            Errors = errors,
            TraceId = traceId
        };

        var options = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}
