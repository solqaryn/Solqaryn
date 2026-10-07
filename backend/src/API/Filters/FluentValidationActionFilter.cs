using System.Collections;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Solqaryn.API.Filters;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    public FluentValidationActionFilter(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
    {
        _apiBehaviorOptions = apiBehaviorOptions.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>())
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument) || argument is null)
                continue;

            var modelType = parameter.ParameterInfo.ParameterType;
            if (!modelType.IsInstanceOfType(argument))
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(validatorType);
            if (context.HttpContext.RequestServices.GetService(enumerableType) is not IEnumerable validators)
                continue;

            foreach (var candidate in validators)
            {
                if (candidate is not IValidator validator)
                    continue;

                var validationContextType = typeof(ValidationContext<>).MakeGenericType(modelType);
                var validationContext = Activator.CreateInstance(validationContextType, argument) as IValidationContext
                    ?? throw new InvalidOperationException($"No se pudo crear ValidationContext para {modelType.FullName}.");

                var result = await validator
                    .ValidateAsync(validationContext, context.HttpContext.RequestAborted)
                    .ConfigureAwait(false);

                foreach (var failure in result.Errors)
                    context.ModelState.AddModelError(failure.PropertyName ?? string.Empty, failure.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
            return;
        }

        await next();
    }
}
