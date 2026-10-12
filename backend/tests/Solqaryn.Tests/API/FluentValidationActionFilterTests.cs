using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Solqaryn.API.Filters;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class FluentValidationActionFilterTests
{
    [Fact]
    public async Task ValidRequest_ContinuesPipeline()
    {
        var (filter, context) = BuildContext(new TestRequest { Name = "ok", Code = "A" }, new TestRequestValidator());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(BuildExecutedContext(context));
        });

        Assert.True(nextCalled);
        Assert.Null(context.Result);
        Assert.True(context.ModelState.IsValid);
    }

    [Fact]
    public async Task InvalidRequest_PreservesMultipleErrorsAndConfiguredContract()
    {
        var (filter, context) = BuildContext(new TestRequest { Name = "", Code = "" }, new TestRequestValidator());
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(BuildExecutedContext(context));
        });

        Assert.False(nextCalled);
        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(TestRequest.Name), problem.Errors.Keys);
        Assert.Contains(nameof(TestRequest.Code), problem.Errors.Keys);
    }

    [Fact]
    public async Task AsyncRule_IsAwaited()
    {
        var (filter, context) = BuildContext(
            new TestRequest { Name = "blocked", Code = "A" },
            new AsyncTestRequestValidator());

        await filter.OnActionExecutionAsync(
            context,
            () => Task.FromResult(BuildExecutedContext(context)));

        var badRequest = Assert.IsType<BadRequestObjectResult>(context.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains(nameof(TestRequest.Name), problem.Errors.Keys);
    }

    [Fact]
    public async Task ExceptionFromAction_IsNotReclassifiedByValidationFilter()
    {
        var (filter, context) = BuildContext(new TestRequest { Name = "ok", Code = "A" }, new TestRequestValidator());

        await Assert.ThrowsAsync<ValidationException>(() =>
            filter.OnActionExecutionAsync(
                context,
                () => throw new ValidationException("service validation")));
    }

    private static (FluentValidationActionFilter Filter, ActionExecutingContext Context) BuildContext(
        TestRequest request,
        IValidator<TestRequest> validator)
    {
        var services = new ServiceCollection()
            .AddSingleton(validator)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        var method = typeof(TestController).GetMethod(nameof(TestController.Execute))!;
        var parameterInfo = method.GetParameters().Single();
        var descriptor = new ControllerActionDescriptor
        {
            ControllerName = nameof(TestController),
            ActionName = nameof(TestController.Execute),
            MethodInfo = method,
            Parameters =
            [
                new ControllerParameterDescriptor
                {
                    Name = parameterInfo.Name!,
                    ParameterInfo = parameterInfo
                }
            ]
        };
        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            descriptor,
            new ModelStateDictionary());
        var executing = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { [parameterInfo.Name!] = request },
            new TestController());

        var options = Options.Create(new ApiBehaviorOptions
        {
            InvalidModelStateResponseFactory = ctx =>
                new BadRequestObjectResult(new ValidationProblemDetails(ctx.ModelState))
        });

        return (new FluentValidationActionFilter(options), executing);
    }

    private static ActionExecutedContext BuildExecutedContext(ActionExecutingContext context) =>
        new(context, [], context.Controller);

    private sealed class TestController
    {
        public void Execute(TestRequest request) { }
    }

    private sealed class TestRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Code).NotEmpty();
        }
    }

    private sealed class AsyncTestRequestValidator : AbstractValidator<TestRequest>
    {
        public AsyncTestRequestValidator()
        {
            RuleFor(x => x.Name).MustAsync(async (value, cancellationToken) =>
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return value == "allowed";
            });
        }
    }
}
