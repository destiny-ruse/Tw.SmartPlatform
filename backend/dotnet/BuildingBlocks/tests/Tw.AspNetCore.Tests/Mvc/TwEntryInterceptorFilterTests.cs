using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Cancellation;
using Tw.AspNetCore.Features;
using Tw.AspNetCore.Mvc;
using Tw.DependencyInjection.Cancellation;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Invocation;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.AspNetCore.Tests.Mvc;

/// <summary>
/// 验证 MVC Entry 拦截器 Filter 与 ASP.NET Core 基础设施入口
/// </summary>
public sealed class TwEntryInterceptorFilterTests
{
    [Fact]
    public void MvcOptionsSetup_Adds_Exactly_One_Global_Entry_Filter()
    {
        var options = new MvcOptions();
        var setup = new TwMvcOptionsSetup();

        setup.Configure(options);
        setup.Configure(options);

        options.Filters
            .OfType<ServiceFilterAttribute>()
            .Count(f => f.ServiceType == typeof(TwEntryInterceptorFilter))
            .Should().Be(1);
    }

    [Fact]
    public async Task Filter_Adds_Http_Feature_And_Wraps_Action_Execution()
    {
        var recorder = new EntryRecorder();
        var filter = CreateFilter<FeatureRecordingInterceptor>(recorder, out var serviceProvider);
        var context = CreateExecutingContext(serviceProvider, requestAborted: CancellationToken.None);

        ActionExecutionDelegate next = () =>
        {
            recorder.Events.Add("action");
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller)
            {
                Result = new ObjectResult("ok")
            });
        };

        await filter.OnActionExecutionAsync(context, next);

        recorder.Events.Should().Equal("before", "action", "after");
        recorder.HttpFeature.Should().NotBeNull();
        recorder.HttpFeature!.HttpContext.Should().BeSameAs(context.HttpContext);
        recorder.HttpFeature.ActionContext.Should().BeSameAs(context);
    }

    [Fact]
    public async Task Filter_Uses_RequestAborted_As_Ambient_Cancellation_When_Action_Has_No_Token()
    {
        using var cts = new CancellationTokenSource();
        var recorder = new EntryRecorder();
        var filter = CreateFilter<CancellationRecordingInterceptor>(recorder, out var serviceProvider);
        var context = CreateExecutingContext(serviceProvider, cts.Token);

        await filter.OnActionExecutionAsync(
            context,
            () => Task.FromResult(new ActionExecutedContext(context, [], context.Controller)));

        recorder.ContextCancellationToken.Should().Be(cts.Token);
        recorder.ProviderCancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public void AddTwAspNetCoreInfrastructure_Registers_Mvc_And_Http_Cancellation_Infrastructure()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddTwAspNetCoreInfrastructure();

        builder.Services.Should().Contain(d => d.ServiceType == typeof(IHttpContextAccessor));
        builder.Services.Should().Contain(d => d.ServiceType == typeof(IConfigureOptions<MvcOptions>)
                                             && d.ImplementationType == typeof(TwMvcOptionsSetup));
        builder.Services.Last(d => d.ServiceType == typeof(ICancellationTokenProvider))
            .ImplementationType.Should().Be(typeof(HttpContextCancellationTokenProvider));
    }

    private static TwEntryInterceptorFilter CreateFilter<TInterceptor>(
        EntryRecorder recorder,
        out ServiceProvider serviceProvider)
        where TInterceptor : InterceptorBase
    {
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddSingleton<TInterceptor>();
        services.AddSingleton<CurrentCancellationTokenAccessor>();
        services.AddSingleton<ICurrentCancellationTokenAccessor>(
            provider => provider.GetRequiredService<CurrentCancellationTokenAccessor>());
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICancellationTokenProvider, HttpContextCancellationTokenProvider>();
        serviceProvider = services.BuildServiceProvider();

        var cache = new EntryChainCache(
            [new InterceptorRegistration(typeof(TInterceptor), InterceptorScope.Entry)],
            []);

        return new TwEntryInterceptorFilter(
            cache,
            serviceProvider.GetRequiredService<ICurrentCancellationTokenAccessor>());
    }

    private static ActionExecutingContext CreateExecutingContext(
        IServiceProvider serviceProvider,
        CancellationToken requestAborted)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            RequestAborted = requestAborted
        };

        var accessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = httpContext;

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.Get))!,
            Parameters = []
        };

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            actionDescriptor);

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            new TestController());
    }

    private sealed class TestController
    {
        public IActionResult Get() => new OkResult();
    }

    private sealed class EntryRecorder
    {
        public List<string> Events { get; } = [];

        public IHttpRequestFeature? HttpFeature { get; set; }

        public CancellationToken ContextCancellationToken { get; set; }

        public CancellationToken ProviderCancellationToken { get; set; }
    }

    private sealed class FeatureRecordingInterceptor(EntryRecorder recorder) : InterceptorBase
    {
        public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            recorder.Events.Add("before");
            recorder.HttpFeature = context.GetFeature<IHttpRequestFeature>();
            await context.ProceedAsync();
            recorder.Events.Add("after");
        }
    }

    private sealed class CancellationRecordingInterceptor(
        EntryRecorder recorder,
        ICancellationTokenProvider cancellationTokenProvider) : InterceptorBase
    {
        public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            recorder.ContextCancellationToken = context.CancellationToken;
            recorder.ProviderCancellationToken = cancellationTokenProvider.Token;
            await context.ProceedAsync();
        }
    }
}
