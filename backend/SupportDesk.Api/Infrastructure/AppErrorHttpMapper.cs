using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SupportDesk.Api.Application.Common;

namespace SupportDesk.Api.Infrastructure;

public static class AppErrorHttpMapper
{
    public static IActionResult ToActionResult(AppResult result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToProblemResult(result.Error!);
    }

    public static IActionResult ToActionResult<T>(AppResult<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return ToProblemResult(result.Error!);
    }

    public static ObjectResult ToProblemResult(AppError error)
    {
        var statusCode = MapStatusCode(error.Code);
        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["code"] = error.Code;

        if (error.Context is not null)
        {
            foreach (var pair in error.Context)
            {
                problem.Extensions[pair.Key] = pair.Value;
            }
        }

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Maps ASP.NET model-state / DataAnnotations failures to the same validation error contract.
    /// </summary>
    public static ObjectResult ToValidationProblemResult(ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Title = AppErrorCodes.ValidationError,
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://httpstatuses.com/400"
        };

        problem.Extensions["code"] = AppErrorCodes.ValidationError;

        return new BadRequestObjectResult(problem);
    }

    public static int MapStatusCode(string code) => code switch
    {
        AppErrorCodes.TicketNotFound => StatusCodes.Status404NotFound,
        AppErrorCodes.AgentNotFound => StatusCodes.Status404NotFound,
        AppErrorCodes.TicketClosed => StatusCodes.Status409Conflict,
        AppErrorCodes.TicketNotEditable => StatusCodes.Status409Conflict,
        AppErrorCodes.InvalidStatusTransition => StatusCodes.Status409Conflict,
        AppErrorCodes.AssignmentRequired => StatusCodes.Status409Conflict,
        AppErrorCodes.AgentInactive => StatusCodes.Status409Conflict,
        AppErrorCodes.AgentHasTickets => StatusCodes.Status409Conflict,
        AppErrorCodes.Conflict => StatusCodes.Status409Conflict,
        AppErrorCodes.ValidationError => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status400BadRequest
    };
}

/// <summary>
/// Maps unexpected exceptions to safe ProblemDetails without leaking internals.
/// </summary>
public sealed class SafeExceptionFilter : IExceptionFilter
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SafeExceptionFilter> _logger;

    public SafeExceptionFilter(IHostEnvironment environment, ILogger<SafeExceptionFilter> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.ExceptionHandled)
        {
            return;
        }

        _logger.LogError(context.Exception, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Detail = "An unexpected error occurred while processing the request.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://httpstatuses.com/500"
        };

        problem.Extensions["code"] = "UNEXPECTED_ERROR";

        // Never expose stack traces or SQL details to clients.
        if (_environment.IsDevelopment())
        {
            problem.Extensions["debugMessage"] = context.Exception.GetType().Name;
        }

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };
        context.ExceptionHandled = true;
    }
}
