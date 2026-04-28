using FluentAssertions;
using System.Reflection;
using System.Text;
using Tw.Core.Extensions;
using Xunit;

namespace Tw.Core.Tests.Extensions;

public class StreamExtensionsTests
{
    [Fact]
    public async Task ReadAndWriteTextAsync_RoundTrips_Text()
    {
        await using var stream = new MemoryStream();

        await stream.WriteTextAsync("hello core", Encoding.UTF8);
        stream.Position = 0;
        var text = await stream.ReadAllTextAsync(Encoding.UTF8);

        text.Should().Be("hello core");
    }

    [Fact]
    public async Task GetAllBytes_And_Async_Read_From_Current_Position()
    {
        await using var stream = new MemoryStream([1, 2, 3, 4]);
        stream.Position = 1;

        stream.GetAllBytes().Should().Equal(2, 3, 4);
        stream.Position = 2;
        var bytes = await stream.GetAllBytesAsync();

        bytes.Should().Equal(3, 4);
    }

    [Fact]
    public async Task CopyToAsyncFromBeginning_Resets_Source_To_Beginning()
    {
        await using var source = new MemoryStream([1, 2, 3]);
        await using var destination = new MemoryStream();
        source.Position = 2;

        await source.CopyToAsyncFromBeginning(destination);

        destination.ToArray().Should().Equal(1, 2, 3);
        source.Position.Should().Be(source.Length);
    }

    [Fact]
    public void ResetPosition_Returns_Same_Stream_At_Start()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        stream.Position = 2;

        var result = stream.ResetPosition();

        result.Should().BeSameAs(stream);
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void CreateMemoryStream_Reads_From_Current_Position()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);
        stream.Position = 2;

        using var copy = stream.CreateMemoryStream();

        copy.ToArray().Should().Equal(3, 4);
        copy.Position.Should().Be(0);
    }

    [Fact]
    public async Task Stream_Methods_Validate_Null_Arguments()
    {
        Stream stream = null!;
        Stream destination = null!;
        string text = null!;

        var nullStream = () => stream.GetAllBytes();
        Func<Task> nullDestination = () => new MemoryStream().CopyToAsyncFromBeginning(destination);
        var nullText = () => new MemoryStream().WriteText(text);
        Func<Task> nullAsyncText = () => new MemoryStream().WriteTextAsync(text);

        nullStream.Should().Throw<ArgumentNullException>().WithParameterName(nameof(stream));
        await nullDestination.Should().ThrowAsync<ArgumentNullException>().WithParameterName(nameof(destination));
        nullText.Should().Throw<ArgumentNullException>().WithParameterName(nameof(text));
        await nullAsyncText.Should().ThrowAsync<ArgumentNullException>().WithParameterName(nameof(text));
    }

    [Fact]
    public void ResetPosition_Throws_Clear_Exception_For_NonSeekable_Stream()
    {
        using var stream = new NonSeekableStream();

        var act = () => stream.ResetPosition();

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Public_Api_Signatures_Match_Plan()
    {
        var methods = typeof(Tw.Core.Extensions.StreamExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.DeclaringType == typeof(Tw.Core.Extensions.StreamExtensions))
            .ToArray();

        methods.Should().HaveCount(8);
        methods.Select(method => method.Name).Should().BeEquivalentTo(
        [
            nameof(Tw.Core.Extensions.StreamExtensions.GetAllBytes),
            nameof(Tw.Core.Extensions.StreamExtensions.GetAllBytesAsync),
            nameof(Tw.Core.Extensions.StreamExtensions.CopyToAsyncFromBeginning),
            nameof(Tw.Core.Extensions.StreamExtensions.CreateMemoryStream),
            nameof(Tw.Core.Extensions.StreamExtensions.ReadAllTextAsync),
            nameof(Tw.Core.Extensions.StreamExtensions.WriteText),
            nameof(Tw.Core.Extensions.StreamExtensions.WriteTextAsync),
            nameof(Tw.Core.Extensions.StreamExtensions.ResetPosition),
        ]);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}
