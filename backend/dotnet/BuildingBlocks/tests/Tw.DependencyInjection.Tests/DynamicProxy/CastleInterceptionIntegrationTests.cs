using System.Reflection;
using System.Reflection.Emit;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class CastleInterceptionIntegrationTests
{
    [Fact]
    public async Task AddServiceRegistration_WithInterceptedInterfaceService_UsesCastleInterfaceProxy()
    {
        var implementationType = DynamicAuditedOrderServiceBuilder.Build();
        var recorder = new AuditRecorder();
        var builder = new ContainerBuilder();
        var configuration = new ConfigurationBuilder().Build();

        builder.RegisterInstance(recorder).SingleInstance();
        builder.RegisterType<AuditInterceptor>().AsSelf().InstancePerLifetimeScope();
        builder.AddServiceRegistration(configuration, new SingleAssemblySource(implementationType.Assembly));
        using var container = builder.Build();
        await using var scope = container.BeginLifetimeScope();

        var service = scope.Resolve<IAuditedOrderService>();
        var result = await service.SubmitAsync("A");

        result.Should().Be("audited:B");
        recorder.OriginalArguments.Should().Equal("A");
        recorder.TargetReturnValues.Should().Equal("B");

        var report = container.Resolve<InterceptionReport>();
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == typeof(IAuditedOrderService).FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == nameof(IAuditedOrderService.SubmitAsync)
            && item.Carrier == "CastleInterfaceProxy"
            && item.Status == "enabled"
            && item.InterceptorTypeNames.Contains(typeof(AuditInterceptor).FullName!));
        report.Items.Should().Contain(item =>
            item.ServiceTypeName == implementationType.FullName
            && item.ImplementationTypeName == implementationType.FullName
            && item.MethodName == nameof(IAuditedOrderService.SubmitAsync)
            && item.Carrier == "CastleClassProxy"
            && item.Status == "skipped");
    }

    public interface IAuditedOrderService
    {
        Task<string> SubmitAsync(string id);
    }

    public sealed class AuditRecorder
    {
        public List<string> OriginalArguments { get; } = [];

        public List<string> TargetReturnValues { get; } = [];
    }

    public sealed class AuditInterceptor(AuditRecorder recorder) : IInterceptor
    {
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            recorder.OriginalArguments.Add((string)context.Arguments[0]!);
            context.Arguments[0] = "B";

            await context.ProceedAsync();

            recorder.TargetReturnValues.Add((string)context.ReturnValue!);
            context.ReturnValue = $"audited:{context.ReturnValue}";
        }
    }

    private sealed class SingleAssemblySource(Assembly assembly) : IAssemblySource
    {
        public IReadOnlyList<Assembly> GetCandidateAssemblies() => [assembly];
    }

    private static class DynamicAuditedOrderServiceBuilder
    {
        public static Type Build()
        {
            var assemblyName = new AssemblyName("Tw.DependencyInjection.Tests.DynamicInterceptionFixtures");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
            var typeBuilder = moduleBuilder.DefineType(
                "Tw.DependencyInjection.Tests.DynamicInterceptionFixtures.AuditedOrderService",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);

            typeBuilder.AddInterfaceImplementation(typeof(IAuditedOrderService));
            typeBuilder.AddInterfaceImplementation(typeof(IScopedDependency));
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(InterceptAttribute).GetConstructor([typeof(Type)])!,
                [typeof(AuditInterceptor)]));

            var methodBuilder = typeBuilder.DefineMethod(
                nameof(IAuditedOrderService.SubmitAsync),
                MethodAttributes.Public
                | MethodAttributes.Virtual
                | MethodAttributes.Final
                | MethodAttributes.HideBySig
                | MethodAttributes.NewSlot,
                typeof(Task<string>),
                [typeof(string)]);

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult)
                    && method.IsGenericMethodDefinition
                    && method.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(string));

            var il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(
                methodBuilder,
                typeof(IAuditedOrderService).GetMethod(nameof(IAuditedOrderService.SubmitAsync))!);

            return typeBuilder.CreateType();
        }
    }
}
