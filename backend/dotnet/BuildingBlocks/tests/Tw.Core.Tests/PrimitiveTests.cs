using FluentAssertions;
using Tw.Core.Primitives;
using Xunit;

namespace Tw.Core.Tests;

public class PrimitiveTests
{
    [Fact]
    public void NamedObject_Rejects_Blank_Name()
    {
        var act = () => new NamedObject(" ");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void NamedAction_Stores_Action()
    {
        var called = false;
        var action = new NamedAction<object>("run", _ => called = true);

        action.Action(new object());

        action.Name.Should().Be("run");
        called.Should().BeTrue();
    }

    [Fact]
    public void NamedAction_Rejects_Null_Action()
    {
        Action<object> action = null!;

        var act = () => new NamedAction<object>("run", action);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(action));
    }

    [Fact]
    public void NamedValue_Stores_Name_And_Value()
    {
        var value = new NamedValue<int>("answer", 42);

        value.Name.Should().Be("answer");
        value.Value.Should().Be(42);
    }

    [Fact]
    public void NamedValue_Allows_Null_Generic_Value()
    {
        var value = new NamedValue<object?>("optional", null);

        value.Value.Should().BeNull();
    }

    [Fact]
    public void NamedTypeSelector_Rejects_Null_Predicate()
    {
        Func<Type, bool> predicate = null!;

        var act = () => new NamedTypeSelector("selector", predicate);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(predicate));
    }

    [Fact]
    public void NamedTypeSelectorList_Adds_Exact_Type_Matcher()
    {
        var selectors = new List<NamedTypeSelector>();

        selectors.Add("strings", typeof(string));

        selectors.Should().ContainSingle();
        selectors[0].Name.Should().Be("strings");
        selectors[0].Predicate(typeof(string)).Should().BeTrue();
        selectors[0].Predicate(typeof(int)).Should().BeFalse();
    }

    [Fact]
    public void NamedTypeSelectorList_Add_Rejects_Null_Selector_List()
    {
        ICollection<NamedTypeSelector> selectors = null!;

        var act = () => selectors.Add("strings", typeof(string));

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(selectors));
    }

    [Fact]
    public void NamedTypeSelectorList_Add_Rejects_Null_Type()
    {
        var selectors = new List<NamedTypeSelector>();
        Type type = null!;

        var act = () => selectors.Add("strings", type);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(type));
    }

    [Fact]
    public void NamedTypeSelectorList_Add_Rejects_Null_Predicate()
    {
        var selectors = new List<NamedTypeSelector>();
        Func<Type, bool> predicate = null!;

        var act = () => selectors.Add("strings", predicate);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(predicate));
    }
}
