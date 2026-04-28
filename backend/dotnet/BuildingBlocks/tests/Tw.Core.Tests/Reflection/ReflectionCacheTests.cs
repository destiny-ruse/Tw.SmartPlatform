using FluentAssertions;
using System.Reflection;
using Tw.Core.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal sealed class ReflectionTestAttribute : Attribute
{
    public int Order { get; set; }
}

public class ReflectionCacheTests
{
    [Fact]
    public void HasAttribute_Uses_Member_Metadata()
    {
        var method = GetMethod(nameof(AsyncMethod));

        method.HasAttribute<ReflectionTestAttribute>().Should().BeTrue();
    }

    [Fact]
    public void GetAsyncResultType_Returns_Task_Result_Type()
    {
        var method = GetMethod(nameof(AsyncMethod));

        method.GetAsyncResultType().Should().Be(typeof(int));
    }

    [Fact]
    public void GetSingleAttribute_Returns_Inherited_Attribute()
    {
        var method = typeof(DerivedReflectionSubject)
            .GetMethod(nameof(DerivedReflectionSubject.InheritedMethod))!;

        method.GetSingleAttribute<ReflectionTestAttribute>().Should().NotBeNull();
        method.GetAttributes<ReflectionTestAttribute>().Should().ContainSingle();
    }

    [Fact]
    public void GetSingleAttribute_Throws_When_Attribute_Is_Missing()
    {
        var method = GetMethod(nameof(PlainMethod));

        var act = () => method.GetSingleAttribute<ReflectionTestAttribute>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*未在成员*PlainMethod*找到特性*ReflectionTestAttribute*");
    }

    [Fact]
    public void GetAsyncResultType_Returns_Void_For_Non_Generic_Task()
    {
        var method = GetMethod(nameof(TaskMethod));

        method.GetAsyncResultType().Should().Be(typeof(void));
    }

    [Fact]
    public void GetAsyncResultType_Returns_Method_Return_Type_For_Non_Task_Methods()
    {
        var method = GetMethod(nameof(PlainMethod));

        method.GetAsyncResultType().Should().Be(typeof(string));
    }

    [Fact]
    public void IsAsyncReturnType_Detects_Task_And_ValueTask_Return_Types()
    {
        typeof(Task).IsAsyncReturnType().Should().BeTrue();
        typeof(Task<int>).IsAsyncReturnType().Should().BeTrue();
        typeof(ValueTask).IsAsyncReturnType().Should().BeTrue();
        typeof(ValueTask<int>).IsAsyncReturnType().Should().BeTrue();
        typeof(string).IsAsyncReturnType().Should().BeFalse();
    }

    [Fact]
    public void IsAsyncMethod_Uses_Method_Return_Type()
    {
        GetMethod(nameof(AsyncMethod)).IsAsyncMethod().Should().BeTrue();
        GetMethod(nameof(PlainMethod)).IsAsyncMethod().Should().BeFalse();
    }

    [Fact]
    public void GetAsyncResultType_Returns_ValueTask_Result_Type()
    {
        var method = GetMethod(nameof(ValueTaskMethod));

        method.GetAsyncResultType().Should().Be(typeof(int));
    }

    [Fact]
    public void GetAsyncResultType_Returns_Void_For_Non_Generic_ValueTask()
    {
        var method = GetMethod(nameof(NonGenericValueTaskMethod));

        method.GetAsyncResultType().Should().Be(typeof(void));
    }

    [Fact]
    public void GetAsyncResultType_Rejects_Null_Method()
    {
        MethodInfo method = null!;

        var act = () => method.GetAsyncResultType();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(method));
    }

    [Fact]
    public void GetSingleAttribute_Returns_First_Attribute_In_Reflection_Order()
    {
        var method = GetMethod(nameof(MultipleAttributesMethod));
        var attributes = method.GetAttributes<ReflectionTestAttribute>();

        method.GetSingleAttributeOrNull<ReflectionTestAttribute>().Should().BeSameAs(attributes[0]);
        method.GetSingleAttribute<ReflectionTestAttribute>().Should().BeSameAs(attributes[0]);
        attributes.Select(attribute => attribute.Order).Should().Equal(1, 2);
    }

    [Fact]
    public void GetCachedInterfaces_Returns_Type_Interfaces()
    {
        typeof(ConstructibleReflectionSubject)
            .GetCachedInterfaces()
            .Should()
            .Contain(typeof(IReflectionCacheSubject));
    }

    [Fact]
    public void GetCachedInterfaces_Returns_Copy_Of_Cached_Metadata()
    {
        var interfaces = typeof(ConstructibleReflectionSubject).GetCachedInterfaces();
        interfaces[0] = typeof(IDisposable);

        typeof(ConstructibleReflectionSubject)
            .GetCachedInterfaces()
            .Should()
            .Contain(typeof(IReflectionCacheSubject));
    }

    [Fact]
    public void GetCachedParameterlessCtor_Returns_Public_Parameterless_Constructor()
    {
        var constructor = typeof(ConstructibleReflectionSubject).GetCachedParameterlessCtor();

        constructor.Should().NotBeNull();
        constructor!.GetParameters().Should().BeEmpty();
    }

    [Fact]
    public void HasParameterlessCtor_Uses_Cached_Constructor_Metadata()
    {
        typeof(ConstructibleReflectionSubject).HasParameterlessCtor().Should().BeTrue();
        typeof(SubjectWithoutParameterlessConstructor).HasParameterlessCtor().Should().BeFalse();
    }

#if DEBUG
    [Fact]
    public void GetStatistics_Returns_Debug_Cache_Counts()
    {
        var method = GetMethod(nameof(AsyncMethod));
        method.GetAttributes<ReflectionTestAttribute>();
        method.GetAsyncResultType();
        typeof(Task<int>).IsAsyncReturnType();
        typeof(ConstructibleReflectionSubject).GetCachedInterfaces();
        typeof(ConstructibleReflectionSubject).GetCachedParameterlessCtor();
        method.IsAsyncMethod();

        var statistics = ReflectionCache.GetStatistics();

        statistics.AttributeCacheCount.Should().BeGreaterThan(0);
        statistics.AsyncResultTypeCacheCount.Should().BeGreaterThan(0);
        statistics.AsyncReturnTypeCacheCount.Should().BeGreaterThan(0);
        statistics.InterfacesCacheCount.Should().BeGreaterThan(0);
        statistics.ParameterlessConstructorCacheCount.Should().BeGreaterThan(0);
        statistics.MethodIsAsyncCacheCount.Should().BeGreaterThan(0);
    }
#endif

    [ReflectionTest]
    private static Task<int> AsyncMethod()
    {
        return Task.FromResult(1);
    }

    private static Task TaskMethod()
    {
        return Task.CompletedTask;
    }

    private static string PlainMethod()
    {
        return string.Empty;
    }

    private static ValueTask<int> ValueTaskMethod()
    {
        return ValueTask.FromResult(1);
    }

    private static ValueTask NonGenericValueTaskMethod()
    {
        return ValueTask.CompletedTask;
    }

    [ReflectionTest(Order = 1)]
    [ReflectionTest(Order = 2)]
    private static void MultipleAttributesMethod()
    {
    }

    private static MethodInfo GetMethod(string name)
    {
        return typeof(ReflectionCacheTests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private abstract class ReflectionSubject
    {
        [ReflectionTest]
        public virtual void InheritedMethod()
        {
        }
    }

    private sealed class DerivedReflectionSubject : ReflectionSubject
    {
        public override void InheritedMethod()
        {
        }
    }

    private interface IReflectionCacheSubject
    {
    }

    private sealed class ConstructibleReflectionSubject : IReflectionCacheSubject
    {
        public ConstructibleReflectionSubject()
        {
        }
    }

    private sealed class SubjectWithoutParameterlessConstructor
    {
        public SubjectWithoutParameterlessConstructor(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
