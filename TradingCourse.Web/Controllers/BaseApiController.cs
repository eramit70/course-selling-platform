using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TradingCourse.Shared;

namespace TradingCourse.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseApiController : ControllerBase
{
    protected IActionResult SuccessResponse<T>(T data, string message = "Success", object? metadata = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = data,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Metadata = metadata
        };
        return Ok(response);
    }

    protected IActionResult CreatedResponse<T>(T data, string message = "Resource created successfully", object? metadata = null)
    {
        var response = new ApiResponse<T>
        {
            Success = true,
            StatusCode = 201,
            Message = message,
            Data = data,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Metadata = metadata
        };
        return StatusCode(201, response);
    }

    protected IActionResult ErrorResponse(string message, int statusCode = 400, List<ApiError>? errors = null)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        return StatusCode(statusCode, response);
    }

    protected IActionResult NotFoundResponse(string message = "Resource not found")
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            StatusCode = 404,
            Message = message,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };
        return NotFound(response);
    }
}
