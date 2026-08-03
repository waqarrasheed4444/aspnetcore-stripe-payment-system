using System.Net;
using CleanArchitecture.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebApi.Middleware;

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
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var details = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Title = "Validation Error",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = validationEx.Message,
                Instance = context.Request.Path,
                Extensions = { ["errors"] = validationEx.Errors }
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Title = "Resource Not Found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = notFoundEx.Message,
                Instance = context.Request.Path
            },
            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An error occurred while processing your request.",
                Instance = context.Request.Path
            }
        };

        context.Response.StatusCode = details.Status ?? (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsJsonAsync(details);
    }
}
