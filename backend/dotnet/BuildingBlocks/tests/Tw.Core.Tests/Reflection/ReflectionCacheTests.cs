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
}
