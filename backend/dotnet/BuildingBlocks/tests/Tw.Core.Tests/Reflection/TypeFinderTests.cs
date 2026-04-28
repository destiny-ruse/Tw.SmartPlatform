using FluentAssertions;
using Tw.Core.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

internal interface IReflectionService
{
}

internal sealed class ReflectionService : IReflectionService
{
}

internal abstract class AbstractReflectionService : IReflectionService
{
}

public class TypeFinderTests
{
    [Fact]
    public void FindTypes_Returns_Non_Abstract_Assignable_Types()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);

        var types = finder.FindTypes<IReflectionService>();

        types.Should().Contain(typeof(ReflectionService));
        types.Should().NotContain(typeof(AbstractReflectionService));
        types.Should().NotContain(typeof(IReflectionService));
    }

    [Fact]
    public void TypeFinderExtensions_Do_Not_Require_DependencyInjection()
    {
        var references = typeof(TypeFinderExtensions).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        references.Should().NotContain("Microsoft.Extensions.DependencyInjection.Abstractions");
    }

    [Fact]
    public void FindTypes_Rejects_Null_BaseType()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);
        Type baseType = null!;

        var act = () => finder.FindTypes(baseType);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(baseType));
    }

    [Fact]
    public void TypeFinder_Stores_Distinct_Assemblies_In_Stable_Order()
    {
        var testAssembly = typeof(ReflectionService).Assembly;
        var coreAssembly = typeof(TypeFinder).Assembly;

        var finder = new TypeFinder([testAssembly, coreAssembly, testAssembly]);

        finder.Assemblies.Should().Equal(testAssembly, coreAssembly);
    }

    [Fact]
    public void FindTypes_Handles_Duplicate_Assembly_Input_Deterministically()
    {
        var assembly = typeof(ReflectionService).Assembly;
        var finder = new TypeFinder([assembly, assembly]);

        var types = finder.FindTypes<IReflectionService>();

        types.Should().ContainSingle(type => type == typeof(ReflectionService));
    }

    [Fact]
    public void FindConcreteTypes_Returns_Non_Abstract_Non_Interface_Types()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);

        var types = finder.FindConcreteTypes();

        types.Should().Contain(typeof(ReflectionService));
        types.Should().NotContain(typeof(AbstractReflectionService));
        types.Should().NotContain(typeof(IReflectionService));
    }

    [Fact]
    public void FindConcreteTypesAssignableTo_Generic_Delegates_To_TypeFinder()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);

        var types = finder.FindConcreteTypesAssignableTo<IReflectionService>();

        types.Should().ContainSingle(type => type == typeof(ReflectionService));
    }

    [Fact]
    public void FindConcreteTypesAssignableTo_Type_Delegates_To_TypeFinder()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);

        var types = finder.FindConcreteTypesAssignableTo(typeof(IReflectionService));

        types.Should().ContainSingle(type => type == typeof(ReflectionService));
    }
}
