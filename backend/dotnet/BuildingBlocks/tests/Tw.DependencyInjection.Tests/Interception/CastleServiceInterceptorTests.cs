using Autofac;
using Castle.DynamicProxy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Core.Exceptions;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Invocation;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Interception;

/// <summary>
/// 验证 Autofac/Castle Service AOP 适配器的注册与调用契约
/// </summary>
public sealed class CastleServiceInterceptorTests
{
    [Fact]
    public async Task Module_Proxies_Interface_Service_And_Intercepts_Common_Return_Shapes()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<ICalculator, CalculatorService>()],
            [new InterceptorRegistration(typeof(IncrementingInterceptor), InterceptorScope.Service)],
            builder =>
            {
                builder.RegisterType<CallRecorder>().SingleInstance();
                builder.RegisterType<IncrementingInterceptor>().AsSelf();
            });

        await using var scope = container.BeginLifetimeScope();
        var calculator = scope.Resolve<ICalculator>();

        ProxyUtil.IsProxy(calculator).Should().BeTrue();
        calculator.Add(1).Should().Be(12);
        await calculator.TouchAsync();
        (await calculator.AddAsync(2)).Should().Be(13);
        await calculator.TouchValueTaskAsync();
        (await calculator.AddValueTaskAsync(3)).Should().Be(14);

        scope.Resolve<CallRecorder>().Calls.Should().ContainInOrder(
            "before:Add",
            "after:Add",
            "before:TouchAsync",
            "after:TouchAsync",
            "before:AddAsync",
            "after:AddAsync",
            "before:TouchValueTaskAsync",
            "after:TouchValueTaskAsync",
            "before:AddValueTaskAsync",
            "after:AddValueTaskAsync");
    }

    [Fact]
    public async Task Adapter_Propagates_Target_Exception_Without_Wrapping()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<IThrowingService, ThrowingService>()],
            [new InterceptorRegistration(typeof(PassthroughInterceptor), InterceptorScope.Service)],
            builder => builder.RegisterType<PassthroughInterceptor>().AsSelf());

        await using var scope = container.BeginLifetimeScope();
        var service = scope.Resolve<IThrowingService>();

        var act = () => service.ThrowAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("target failed");
    }

    [Fact]
    public void Adapter_Allows_ShortCircuit_Return_Value()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<ISecretService, SecretService>()],
            [new InterceptorRegistration(typeof(ShortCircuitInterceptor), InterceptorScope.Service)],
            builder =>
            {
                builder.RegisterType<CallRecorder>().SingleInstance();
                builder.RegisterType<ShortCircuitInterceptor>().AsSelf();
            });

        using var scope = container.BeginLifetimeScope();
        var service = scope.Resolve<ISecretService>();

        service.GetSecret("secret-argument").Should().Be(42);
        scope.Resolve<CallRecorder>().Calls.Should().NotContain("target:GetSecret");
    }

    [Fact]
    public void Adapter_Throws_When_Interceptor_Calls_Proceed_Twice()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<ISecretService, SecretService>()],
            [new InterceptorRegistration(typeof(DoubleProceedInterceptor), InterceptorScope.Service)],
            builder =>
            {
                builder.RegisterType<CallRecorder>().SingleInstance();
                builder.RegisterType<DoubleProceedInterceptor>().AsSelf();
            });

        using var scope = container.BeginLifetimeScope();
        var service = scope.Resolve<ISecretService>();

        var act = () => service.GetSecret("secret-argument");

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*GetSecret*");
    }

    [Fact]
    public void Adapter_Throws_When_ShortCircuit_Return_Value_Has_Wrong_Type()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<ISecretService, SecretService>()],
            [new InterceptorRegistration(typeof(WrongReturnInterceptor), InterceptorScope.Service)],
            builder =>
            {
                builder.RegisterType<CallRecorder>().SingleInstance();
                builder.RegisterType<WrongReturnInterceptor>().AsSelf();
            });

        using var scope = container.BeginLifetimeScope();
        var service = scope.Resolve<ISecretService>();

        var act = () => service.GetSecret("secret-argument");

        act.Should().Throw<TwConfigurationException>()
            .WithMessage("*GetSecret*System.Int32*")
            .Which.Message.Should().NotContain("secret-argument");
    }

    [Fact]
    public void Module_Does_Not_Proxy_Interface_Service_When_Service_Chain_Is_Empty()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<ICalculator, CalculatorService>()],
            [],
            _ => { });

        using var scope = container.BeginLifetimeScope();
        var calculator = scope.Resolve<ICalculator>();

        ProxyUtil.IsProxy(calculator).Should().BeFalse();
    }

    [Fact]
    public void Module_Does_Not_Proxy_Concrete_Only_Service_By_Default_And_Records_Warning()
    {
        var (container, module) = BuildContainer(
            [Descriptor.For<ConcreteOnlyService, ConcreteOnlyService>()],
            [new InterceptorRegistration(typeof(PassthroughInterceptor), InterceptorScope.Service)],
            builder => builder.RegisterType<PassthroughInterceptor>().AsSelf());

        using var scope = container.BeginLifetimeScope();
        var service = scope.Resolve<ConcreteOnlyService>();

        ProxyUtil.IsProxy(service).Should().BeFalse();
        module.Warnings.Should().ContainSingle()
            .Which.Should().Contain(nameof(ConcreteOnlyService));
    }

    [Fact]
    public void Module_ClassProxy_Mode_Records_Warning_For_NonVirtual_Methods()
    {
        var plan = ServiceRegistrationPlanner.Plan(
            [Descriptor.For<ClassProxyCandidate, ClassProxyCandidate>()],
            EmptyGraph);
        var builder = new ContainerBuilder();
        builder.RegisterType<PassthroughInterceptor>().AsSelf();
        var module = new AutoRegistrationModule(
            plan,
            [new InterceptorRegistration(typeof(PassthroughInterceptor), InterceptorScope.Service)],
            [],
            enableClassInterceptors: true);

        builder.RegisterModule(module);
        using var container = builder.Build();

        module.Warnings.Should().Contain(w => w.Contains(nameof(ClassProxyCandidate)) && w.Contains(nameof(ClassProxyCandidate.NonVirtual)));
    }

    [Fact]
    public void Module_Preserves_Scoped_Lifetime_For_Proxied_Service()
    {
        var (container, _) = BuildContainer(
            [Descriptor.For<IScopedCounter, ScopedCounter>(ServiceLifetime.Scoped)],
            [new InterceptorRegistration(typeof(PassthroughInterceptor), InterceptorScope.Service)],
            builder => builder.RegisterType<PassthroughInterceptor>().AsSelf());

        using var firstScope = container.BeginLifetimeScope();
        using var secondScope = container.BeginLifetimeScope();

        firstScope.Resolve<IScopedCounter>().Should().BeSameAs(firstScope.Resolve<IScopedCounter>());
        firstScope.Resolve<IScopedCounter>().Should().NotBeSameAs(secondScope.Resolve<IScopedCounter>());
    }

    [Fact]
    public void Module_Preserves_Collection_Service_Registrations()
    {
        var (container, _) = BuildContainer(
            [
                Descriptor.For<IHandler, FirstHandler>(isCollection: true),
                Descriptor.For<IHandler, SecondHandler>(isCollection: true),
            ],
            [],
            _ => { });

        using var scope = container.BeginLifetimeScope();

        scope.Resolve<IEnumerable<IHandler>>().Select(h => h.Name).Should().Equal("first", "second");
    }

    [Fact]
    public void Module_Applies_Keyed_Service_Registrations()
    {
        var (container, _) = BuildContainer(
            [
                Descriptor.For<IPaymentGateway, StripeGateway>(key: "stripe"),
                Descriptor.For<IPaymentGateway, AlipayGateway>(key: "alipay"),
            ],
            [],
            _ => { });

        using var scope = container.BeginLifetimeScope();

        scope.ResolveKeyed<IPaymentGateway>("stripe").Name.Should().Be("stripe");
        scope.ResolveKeyed<IPaymentGateway>("alipay").Name.Should().Be("alipay");
    }

    private static (IContainer Container, AutoRegistrationModule Module) BuildContainer(
        IReadOnlyList<ServiceScanDescriptor> scans,
        IReadOnlyList<InterceptorRegistration> globalInterceptors,
        Action<ContainerBuilder> configure)
    {
        var plan = ServiceRegistrationPlanner.Plan(scans, EmptyGraph);
        var builder = new ContainerBuilder();
        configure(builder);
        var module = new AutoRegistrationModule(plan, globalInterceptors, []);
        builder.RegisterModule(module);
        return (builder.Build(), module);
    }

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> EmptyGraph =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

    public interface ICalculator
    {
        int Add(int value);

        Task TouchAsync();

        Task<int> AddAsync(int value);

        ValueTask TouchValueTaskAsync();

        ValueTask<int> AddValueTaskAsync(int value);
    }

    public sealed class CalculatorService : ICalculator, IScopedDependency
    {
        public int Add(int value) => value + 10;

        public Task TouchAsync() => Task.CompletedTask;

        public Task<int> AddAsync(int value) => Task.FromResult(value + 10);

        public ValueTask TouchValueTaskAsync() => ValueTask.CompletedTask;

        public ValueTask<int> AddValueTaskAsync(int value) => ValueTask.FromResult(value + 10);
    }

    public interface IThrowingService
    {
        Task ThrowAsync();
    }

    public sealed class ThrowingService : IThrowingService, IScopedDependency
    {
        public Task ThrowAsync() => throw new InvalidOperationException("target failed");
    }

    public interface ISecretService
    {
        int GetSecret(string argument);
    }

    public sealed class SecretService(CallRecorder recorder) : ISecretService, IScopedDependency
    {
        public int GetSecret(string argument)
        {
            recorder.Calls.Add("target:GetSecret");
            return argument.Length;
        }
    }

    public sealed class ConcreteOnlyService : IScopedDependency
    {
        public int GetValue() => 1;
    }

    public class ClassProxyCandidate : IScopedDependency
    {
        public int NonVirtual() => 1;

        public virtual int Virtual() => 2;
    }

    public interface IScopedCounter { }

    public sealed class ScopedCounter : IScopedCounter, IScopedDependency { }

    public interface IHandler
    {
        string Name { get; }
    }

    public sealed class FirstHandler : IHandler, IScopedDependency
    {
        public string Name => "first";
    }

    public sealed class SecondHandler : IHandler, IScopedDependency
    {
        public string Name => "second";
    }

    public interface IPaymentGateway
    {
        string Name { get; }
    }

    public sealed class StripeGateway : IPaymentGateway, IScopedDependency
    {
        public string Name => "stripe";
    }

    public sealed class AlipayGateway : IPaymentGateway, IScopedDependency
    {
        public string Name => "alipay";
    }

    public sealed class CallRecorder
    {
        public List<string> Calls { get; } = [];
    }

    public sealed class IncrementingInterceptor(CallRecorder recorder) : InterceptorBase
    {
        public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            recorder.Calls.Add($"before:{context.Method.Name}");
            await context.ProceedAsync();
            if (context.ReturnType == typeof(int))
            {
                context.ReturnValue = (int)context.ReturnValue! + 1;
            }
            recorder.Calls.Add($"after:{context.Method.Name}");
        }
    }

    public sealed class PassthroughInterceptor : InterceptorBase { }

    public sealed class ShortCircuitInterceptor : InterceptorBase
    {
        public override ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            context.ReturnValue = 42;
            return ValueTask.CompletedTask;
        }
    }

    public sealed class DoubleProceedInterceptor : InterceptorBase
    {
        public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            await context.ProceedAsync();
            await context.ProceedAsync();
        }
    }

    public sealed class WrongReturnInterceptor : InterceptorBase
    {
        public override ValueTask InterceptAsync(IUnaryInvocationContext context)
        {
            context.ReturnValue = "not an int";
            return ValueTask.CompletedTask;
        }
    }

    private static class Descriptor
    {
        public static ServiceScanDescriptor For<TService, TImplementation>(
            ServiceLifetime lifetime = ServiceLifetime.Scoped,
            object? key = null,
            bool isCollection = false)
            => new(
                typeof(TImplementation),
                [typeof(TService)],
                lifetime,
                key,
                isCollection,
                IsReplacement: false,
                Order: 0,
                IsOpenGenericDefinition: false,
                AssemblyName: typeof(TImplementation).Assembly.GetName().Name ?? string.Empty);
    }
}
