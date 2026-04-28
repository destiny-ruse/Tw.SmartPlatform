using FluentAssertions;
using System.Reflection;
using Tw.Core.Collections;
using Xunit;

namespace Tw.Core.Tests;

public class TypeListTests
{
    [Fact]
    public void ITypeList_Generic_Parameter_Is_Invariant()
    {
        var genericParameter = typeof(ITypeList<>).GetGenericArguments()[0];

        genericParameter.GenericParameterAttributes.Should()
            .NotHaveFlag(GenericParameterAttributes.Contravariant);
    }

    [Fact]
    public void TypeList_Accepts_Assignable_Types()
    {
        var types = new TypeList<IDisposable>();

        types.Add(typeof(MemoryStream));

        types.Should().Contain(typeof(MemoryStream));
    }

    [Fact]
    public void TypeList_Rejects_Unassignable_Types()
    {
        var types = new TypeList<IDisposable>();

        var act = () => types.Add(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("type");
    }

    [Fact]
    public void TryAdd_Returns_False_For_Duplicate_Type()
    {
        var types = new TypeList<IDisposable>();

        var first = types.TryAdd<MemoryStream>();
        var second = types.TryAdd<MemoryStream>();

        first.Should().BeTrue();
        second.Should().BeFalse();
        types.Should().ContainSingle(type => type == typeof(MemoryStream));
    }

    [Fact]
    public void Generic_Type_Operations_Use_Exact_Type_Matching()
    {
        var types = new TypeList<IDisposable>();

        types.Add<MemoryStream>();

        types.Contains<MemoryStream>().Should().BeTrue();
        types.Remove<MemoryStream>().Should().BeTrue();
        types.Contains<MemoryStream>().Should().BeFalse();
        types.Remove<MemoryStream>().Should().BeFalse();
    }

    [Fact]
    public void Add_Rejects_Null_Type()
    {
        var types = new TypeList<IDisposable>();
        Type type = null!;

        var act = () => types.Add(type);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(type));
    }

    [Fact]
    public void Insert_Enforces_Assignability()
    {
        var types = new TypeList<IDisposable>();

        var act = () => types.Insert(0, typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("item");
    }

    [Fact]
    public void Insert_Rejects_Null_Type()
    {
        var types = new TypeList<IDisposable>();
        Type item = null!;

        var act = () => types.Insert(0, item);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(item));
    }

    [Fact]
    public void Index_Setter_Enforces_Assignability()
    {
        var types = new TypeList<IDisposable> { typeof(MemoryStream) };

        var act = () => types[0] = typeof(string);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Index_Setter_Rejects_Null_Type()
    {
        var types = new TypeList<IDisposable> { typeof(MemoryStream) };
        Type value = null!;

        var act = () => types[0] = value;

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void TypeList_Add_Allows_Duplicate_Types()
    {
        var types = new TypeList<IDisposable>();

        types.Add(typeof(MemoryStream));
        types.Add(typeof(MemoryStream));

        types.Should().HaveCount(2);
    }
}
