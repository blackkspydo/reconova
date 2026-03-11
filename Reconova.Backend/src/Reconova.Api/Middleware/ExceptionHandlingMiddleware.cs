using System.Text.Json;
using FluentValidation;
using Reconova.Application.Common.Models;
using Reconova.Domain.Common.Exceptions;

namespace Reconova.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (400, new ApiResponse
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "VALIDATION_ERROR",
                    Message = "One or more validation errors occurred.",
                    Details = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            }),

            NotFoundException notFoundEx => (404, ApiResponse.Fail(notFoundEx.Code, notFoundEx.Message)),
            ConflictException conflictEx => (409, ApiResponse.Fail(conflictEx.Code, conflictEx.Message)),
            ForbiddenException forbiddenEx => (403, ApiResponse.Fail(forbiddenEx.Code, forbiddenEx.Message)),
            BusinessRuleException brEx => (400, ApiResponse.Fail(brEx.Code, brEx.Message)),
            DomainException domainEx => (400, ApiResponse.Fail(domainEx.Code, domainEx.Message)),
            UnauthorizedAccessException => (401, ApiResponse.Fail("UNAUTHORIZED", "Authentication required.")),

            _ => (500, ApiResponse.Fail("INTERNAL_ERROR", "An unexpected error occurred."))
        };

        if (statusCode == 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
