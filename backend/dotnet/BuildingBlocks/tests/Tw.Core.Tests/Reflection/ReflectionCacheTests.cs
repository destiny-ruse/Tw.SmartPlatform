using FluentAssertions;
using System.Reflection;
using Tw.Core.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
internal sealed class ReflectionTestAttribute : Attribute
{
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
            .WithMessage("*ReflectionTestAttribute*PlainMethod*");
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
}
