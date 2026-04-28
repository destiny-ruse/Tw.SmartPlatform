using System.Text;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for streams.</summary>
public static class StreamExtensions
{
    /// <summary>Reads all bytes from the stream's current position without disposing the stream.</summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The bytes read from the current position to the end.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static byte[] GetAllBytes(this Stream stream)
    {
        var source = Check.NotNull(stream);
        using var memoryStream = new MemoryStream();
        source.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>Reads all bytes from the stream's current position without disposing the stream.</summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="cancellationToken">The token that cancels the asynchronous copy.</param>
    /// <returns>The bytes read from the current position to the end.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static async Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var source = Check.NotNull(stream);
        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>Copies the source stream to a destination after resetting a seekable source to the beginning.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="destination">The destination stream.</param>
    /// <param name="cancellationToken">The token that cancels the asynchronous copy.</param>
    /// <returns>A task that completes when the copy is finished.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
    public static async Task CopyToAsyncFromBeginning(this Stream stream, Stream destination, CancellationToken cancellationToken = default)
    {
        var source = Check.NotNull(stream);
        Check.NotNull(destination);

        if (source.CanSeek)
        {
            source.Position = 0;
        }

        await source.CopyToAsync(destination, cancellationToken);
    }

    /// <summary>Creates a memory stream containing bytes from the stream's current position.</summary>
    /// <param name="stream">The stream to copy.</param>
    /// <returns>A memory stream positioned at the beginning of the copied bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static MemoryStream CreateMemoryStream(this Stream stream)
    {
        return new MemoryStream(stream.GetAllBytes());
    }

    /// <summary>Reads all text from the stream's current position using UTF-8 by default.</summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="encoding">The text encoding, or UTF-8 when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the asynchronous read.</param>
    /// <returns>The text read from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    public static async Task<string> ReadAllTextAsync(this Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Check.NotNull(stream), encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>Writes text at the stream's current position using UTF-8 by default without resetting the position.</summary>
    /// <param name="stream">The stream to write.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="encoding">The text encoding, or UTF-8 when omitted.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    public static void WriteText(this Stream stream, string text, Encoding? encoding = null)
    {
        var writer = new StreamWriter(Check.NotNull(stream), encoding ?? Encoding.UTF8, leaveOpen: true);
        using (writer)
        {
            writer.Write(Check.NotNull(text));
        }
    }

    /// <summary>Writes text at the stream's current position using UTF-8 by default without resetting the position.</summary>
    /// <param name="stream">The stream to write.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="encoding">The text encoding, or UTF-8 when omitted.</param>
    /// <param name="cancellationToken">The token that cancels the asynchronous write.</param>
    /// <returns>A task that completes when the text has been flushed to the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    public static async Task WriteTextAsync(this Stream stream, string text, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        var writer = new StreamWriter(Check.NotNull(stream), encoding ?? Encoding.UTF8, leaveOpen: true);
        await using (writer)
        {
            await writer.WriteAsync(Check.NotNull(text).AsMemory(), cancellationToken);
        }
    }

    /// <summary>Resets a seekable stream to position zero and returns the same stream.</summary>
    /// <param name="stream">The stream to reset.</param>
    /// <returns>The same stream instance at position zero.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="stream"/> is not seekable.</exception>
    public static Stream ResetPosition(this Stream stream)
    {
        var source = Check.NotNull(stream);
        if (!source.CanSeek)
        {
            throw new NotSupportedException("The stream does not support seeking.");
        }

        source.Position = 0;
        return source;
    }
}
