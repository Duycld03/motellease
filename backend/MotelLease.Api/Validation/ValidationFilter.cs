using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Api.Validation;

/// <summary>
/// Runs the FluentValidation validator for every bound action argument that has one, before
/// the action body. Arguments without a registered validator pass through untouched, which is
/// how <c>IFormFile</c> uploads and route ids stay out of the way.
/// </summary>
public sealed class ValidationFilter(
    IServiceProvider services,
    ILocalizer localizer,
    IRequestContext requestContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        Dictionary<string, List<string>>? errors = null;

        foreach (var (name, argument) in context.ActionArguments)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(
                validationContext,
                context.HttpContext.RequestAborted);

            if (result.IsValid)
            {
                continue;
            }

            errors ??= [];

            foreach (var failure in result.Errors)
            {
                // Property paths are reported as the client sent them; the message is a
                // resource key resolved in the request language.
                var key = string.IsNullOrEmpty(failure.PropertyName) ? name : failure.PropertyName;

                if (!errors.TryGetValue(key, out var messages))
                {
                    messages = [];
                    errors[key] = messages;
                }

                messages.Add(localizer.Get(failure.ErrorMessage, requestContext.Language));
            }
        }

        if (errors is null)
        {
            await next();
            return;
        }

        var problem = new ValidationProblemDetails(
            errors.ToDictionary(e => e.Key, e => e.Value.ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = localizer.Get(MessageKeys.General.ValidationFailed, requestContext.Language),
            Instance = context.HttpContext.Request.Path
        };

        problem.Extensions["code"] = MessageKeys.General.ValidationFailed;
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }
}
