using FluentAssertions;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using Tw.Core.Extensions;
using Xunit;

namespace Tw.Core.Tests.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void AddIfNotContains_Adds_Only_New_Items()
    {
        var values = new List<int> { 1, 2 };

        var addedSingle = values.AddIfNotContains(3);
        var addedDuplicate = values.AddIfNotContains(2);
        var addedItems = values.AddIfNotContains(new[] { 2, 4, 5, 4 }).ToArray();

        addedSingle.Should().BeTrue();
        addedDuplicate.Should().BeFalse();
        addedItems.Should().Equal(4, 5);
        values.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void AddIfNotContains_Factory_Is_Only_Called_When_Predicate_Misses()
    {
        var values = new List<string> { "first" };
        var calls = 0;

        var existingAdded = values.AddIfNotContains(item => item.StartsWith("f", StringComparison.Ordinal), () =>
        {
            calls++;
            return "factory";
        });
        var missingAdded = values.AddIfNotContains(item => item.StartsWith("s", StringComparison.Ordinal), () =>
        {
            calls++;
            return "second";
        });

        existingAdded.Should().BeFalse();
        missingAdded.Should().BeTrue();
        calls.Should().Be(1);
        values.Should().Equal("first", "second");
    }

    [Fact]
    public void RemoveAll_Returns_Removed_Items_And_Removes_Expected_Values()
    {
        ICollection<int> values = new List<int> { 1, 2, 3, 4, 5 };

        var removed = values.RemoveAll(value => value % 2 == 0);

        removed.Should().Equal(2, 4);
        values.Should().Equal(1, 3, 5);
    }

    [Fact]
    public void RemoveAll_Items_Removes_Snapshot_Of_Provided_Items()
    {
        ICollection<int> values = new List<int> { 1, 2, 3, 4 };

        values.RemoveAll(values.Where(value => value is 2 or 4));

        values.Should().Equal(1, 3);
    }

    [Fact]
    public void Collection_Methods_Validate_Null_Arguments()
    {
        ICollection<int> source = null!;
        IEnumerable<int> items = null!;
        Func<int, bool> predicate = null!;
        Func<int> factory = null!;

        var nullSource = () => source.AddIfNotContains(1);
        var nullItems = () => new List<int>().AddIfNotContains(items).ToArray();
        var nullPredicate = () => new List<int>().AddIfNotContains(predicate, () => 1);
        var nullFactory = () => new List<int>().AddIfNotContains(_ => false, factory);

        nullSource.Should().Throw<ArgumentNullException>().WithParameterName(nameof(source));
        nullItems.Should().Throw<ArgumentNullException>().WithParameterName(nameof(items));
        nullPredicate.Should().Throw<ArgumentNullException>().WithParameterName(nameof(predicate));
        nullFactory.Should().Throw<ArgumentNullException>().WithParameterName("itemFactory");
    }

    [Fact]
    public void Dictionary_GetOrAdd_Uses_Factory_Once()
    {
        var values = new Dictionary<string, int>();
        var calls = 0;

        var first = values.GetOrAdd("one", key =>
        {
            calls++;
            return key.Length;
        });
        var second = values.GetOrAdd("one", _ =>
        {
            calls++;
            return 42;
        });

        first.Should().Be(3);
        second.Should().Be(3);
        calls.Should().Be(1);
    }

    [Fact]
    public void Dictionary_GetOrAdd_Factory_Is_Not_Called_When_Key_Exists()
    {
        var values = new Dictionary<string, int> { ["one"] = 1 };
        var calls = 0;

        var value = values.GetOrAdd("one", () =>
        {
            calls++;
            return 2;
        });

        value.Should().Be(1);
        calls.Should().Be(0);
    }

    [Fact]
    public void Dictionary_GetOrDefault_And_Dynamic_Conversion_Work()
    {
        var dictionary = new Dictionary<string, object>
        {
            ["name"] = "core",
            ["child"] = new Dictionary<string, object> { ["value"] = 42 },
        };
        IReadOnlyDictionary<string, object> readOnly = dictionary;
        var concurrent = new ConcurrentDictionary<string, int>();

        dictionary.GetOrDefault("missing").Should().BeNull();
        readOnly.GetOrDefault("name").Should().Be("core");
        concurrent.GetOrDefault("missing").Should().Be(0);
        concurrent.GetOrAdd("count", () => 7).Should().Be(7);

        dynamic dynamicObject = dictionary.ConvertToDynamicObject();

        ((object)dynamicObject).Should().BeAssignableTo<ExpandoObject>();
        ((string)dynamicObject.name).Should().Be("core");
        ((int)dynamicObject.child.value).Should().Be(42);
    }

    [Fact]
    public void Dictionary_Methods_Validate_Null_Arguments()
    {
        IDictionary<string, int> dictionary = null!;
        Func<string, int> keyedFactory = null!;
        Func<int> valueFactory = null!;

        var nullDictionary = () => dictionary.GetOrAdd("key", key => key.Length);
        var nullKeyedFactory = () => new Dictionary<string, int>().GetOrAdd("key", keyedFactory);
        var nullValueFactory = () => new Dictionary<string, int>().GetOrAdd("key", valueFactory);

        nullDictionary.Should().Throw<ArgumentNullException>().WithParameterName(nameof(dictionary));
        nullKeyedFactory.Should().Throw<ArgumentNullException>().WithParameterName("factory");
        nullValueFactory.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public async Task Enumerable_ForEachParallelAsync_Processes_All_Items()
    {
        var processed = new ConcurrentBag<int>();

        await Enumerable.Range(1, 20).ForEachParallelAsync(async item =>
        {
            await Task.Yield();
            processed.Add(item);
        }, maxDegreeOfParallelism: 4);

        processed.Should().BeEquivalentTo(Enumerable.Range(1, 20));
    }

    [Fact]
    public void Enumerable_Batch_WhereIf_PageBy_And_AsReadOnlyCollection_Work()
    {
        var values = Enumerable.Range(1, 7);

        values.Batch(3).Select(batch => batch.ToArray())
            .Should().BeEquivalentTo(new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 }, new[] { 7 } }, options => options.WithStrictOrdering());
        values.WhereIf(true, value => value % 2 == 0).Should().Equal(2, 4, 6);
        values.WhereIf(false, value => value % 2 == 0).Should().Equal(1, 2, 3, 4, 5, 6, 7);
        values.WhereIf(true, (value, index) => value + index > 7).Should().Equal(5, 6, 7);
        values.PageBy(pageNumber: 2, pageSize: 3).Should().Equal(4, 5, 6);
        values.AsReadOnlyCollection().Should().Equal(1, 2, 3, 4, 5, 6, 7)
            .And.BeAssignableTo<IReadOnlyCollection<int>>();
    }

    [Fact]
    public async Task Enumerable_Methods_Validate_Null_Arguments_And_Ranges()
    {
        IEnumerable<int> source = null!;
        Action<int> action = null!;
        Func<int, Task> asyncAction = null!;
        Func<int, bool> predicate = null!;

        var nullSource = () => source.ForEach(_ => { });
        var nullAction = () => new[] { 1 }.ForEach(action);
        var nullAsyncAction = () => new[] { 1 }.ForEachAsync(asyncAction);
        var nullPredicate = () => new[] { 1 }.WhereIf(true, predicate).ToArray();
        var invalidParallelism = () => new[] { 1 }.ForEachParallelAsync(_ => Task.CompletedTask, -1);
        var invalidBatch = () => new[] { 1 }.Batch(0).ToArray();
        var invalidPage = () => new[] { 1 }.PageBy(0, 10).ToArray();

        nullSource.Should().Throw<ArgumentNullException>().WithParameterName(nameof(source));
        nullAction.Should().Throw<ArgumentNullException>().WithParameterName(nameof(action));
        await nullAsyncAction.Should().ThrowAsync<ArgumentNullException>().WithParameterName("action");
        nullPredicate.Should().Throw<ArgumentNullException>().WithParameterName(nameof(predicate));
        await invalidParallelism.Should().ThrowAsync<ArgumentOutOfRangeException>();
        invalidBatch.Should().Throw<ArgumentOutOfRangeException>();
        invalidPage.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Enumerable_IsNullOrEmpty_And_JoinAsString_Work()
    {
        IEnumerable<int>? nullValues = null;

        nullValues.IsNullOrEmpty().Should().BeTrue();
        Array.Empty<int>().IsNullOrEmpty().Should().BeTrue();
        new[] { 1 }.IsNullOrEmpty().Should().BeFalse();
        new[] { "a", "b" }.JoinAsString(null).Should().Be("ab");
        new[] { 1, 2 }.JoinAsString(",").Should().Be("1,2");
    }

    [Fact]
    public void List_MoveItem_Moves_Matching_Item()
    {
        var values = new List<string> { "one", "two", "three" };

        values.MoveItem(item => item == "three", 0);

        values.Should().Equal("three", "one", "two");
    }

    [Fact]
    public void List_Insert_Replace_And_GetOrAdd_Behavior_Work()
    {
        IList<string> values = new List<string> { "b", "d" };

        values.AddFirst("a");
        values.AddLast("e");
        values.InsertAfter("b", "c");
        values.InsertBefore(item => item == "e", "d2");
        values.ReplaceOne(item => item == "d2", "d");
        values.ReplaceWhile(item => item == "d", item => item.ToUpperInvariant());
        var existing = values.GetOrAdd(item => item == "D", () => "factory");
        var added = values.GetOrAdd(item => item == "z", () => "z");

        existing.Should().Be("D");
        added.Should().Be("z");
        values.Should().Equal("a", "b", "c", "D", "D", "e", "z");
        values.FindIndex(item => item == "e").Should().Be(5);
        Tw.Core.Extensions.ListExtensions.AsReadOnly(values).Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    [Fact]
    public void List_Methods_Validate_Null_Arguments_And_Not_Found_Items()
    {
        IList<int> source = null!;
        Predicate<int> selector = null!;
        Func<int> factory = null!;
        IList<int> values = new List<int> { 1, 2 };
        var moveValues = new List<int> { 1, 2 };

        var nullSource = () => source.AddFirst(1);
        var nullSelector = () => values.FindIndex(selector);
        var nullFactory = () => values.GetOrAdd(_ => false, factory);
        var missingInsert = () => values.InsertAfter(9, 3);
        var missingMove = () => moveValues.MoveItem(_ => false, 0);
        var invalidMoveTarget = () => moveValues.MoveItem(_ => true, 3);

        nullSource.Should().Throw<ArgumentNullException>().WithParameterName(nameof(source));
        nullSelector.Should().Throw<ArgumentNullException>().WithParameterName(nameof(selector));
        nullFactory.Should().Throw<ArgumentNullException>().WithParameterName(nameof(factory));
        missingInsert.Should().Throw<InvalidOperationException>();
        missingMove.Should().Throw<InvalidOperationException>();
        invalidMoveTarget.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("targetIndex");
    }

    [Fact]
    public void Public_Api_Signatures_Match_Plan()
    {
        AssertPublicMethods(
            typeof(Tw.Core.Extensions.CollectionExtensions),
            (nameof(Tw.Core.Extensions.CollectionExtensions.AddIfNotContains), 3),
            (nameof(Tw.Core.Extensions.CollectionExtensions.RemoveAll), 2));
        AssertPublicMethods(
            typeof(Tw.Core.Extensions.DictionaryExtensions),
            (nameof(Tw.Core.Extensions.DictionaryExtensions.GetOrDefault), 4),
            (nameof(Tw.Core.Extensions.DictionaryExtensions.GetOrAdd), 3),
            (nameof(Tw.Core.Extensions.DictionaryExtensions.ConvertToDynamicObject), 1));
        AssertPublicMethods(
            typeof(Tw.Core.Extensions.EnumerableExtensions),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.IsNullOrEmpty), 1),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.JoinAsString), 2),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.ForEach), 2),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.ForEachAsync), 1),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.ForEachParallelAsync), 1),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.Batch), 1),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.WhereIf), 2),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.PageBy), 1),
            (nameof(Tw.Core.Extensions.EnumerableExtensions.AsReadOnlyCollection), 1));
        AssertPublicMethods(
            typeof(Tw.Core.Extensions.ListExtensions),
            (nameof(Tw.Core.Extensions.ListExtensions.AsReadOnly), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.InsertRange), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.FindIndex), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.AddFirst), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.AddLast), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.InsertAfter), 2),
            (nameof(Tw.Core.Extensions.ListExtensions.InsertBefore), 2),
            (nameof(Tw.Core.Extensions.ListExtensions.ReplaceWhile), 2),
            (nameof(Tw.Core.Extensions.ListExtensions.ReplaceOne), 3),
            (nameof(Tw.Core.Extensions.ListExtensions.GetOrAdd), 1),
            (nameof(Tw.Core.Extensions.ListExtensions.MoveItem), 1));
    }

    private static void AssertPublicMethods(Type type, params (string Name, int Count)[] expectedMethods)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.DeclaringType == type)
            .ToArray();

        methods.Should().HaveCount(expectedMethods.Sum(method => method.Count));

        foreach (var expectedMethod in expectedMethods)
        {
            methods.Where(method => method.Name == expectedMethod.Name).Should().HaveCount(expectedMethod.Count);
        }
    }
}
