using System.Text;

namespace Tw.Core.Extensions;

/// <summary>提供流扩展方法</summary>
public static class StreamExtensions
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>从流当前位置读取所有字节，且不释放该流</summary>
    /// <param name="stream">要读取的流</param>
    /// <returns>从当前位置到末尾读取到的字节</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    public static byte[] GetAllBytes(this Stream stream)
    {
        var source = Check.NotNull(stream);
        using var memoryStream = new MemoryStream();
        source.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>从流当前位置异步读取所有字节，且不释放该流</summary>
    /// <param name="stream">要读取的流</param>
    /// <param name="cancellationToken">取消异步复制的令牌</param>
    /// <returns>从当前位置到末尾读取到的字节</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    public static async Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var source = Check.NotNull(stream);
        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    /// <summary>在可定位源流重置到开头后，将源流复制到目标流</summary>
    /// <param name="stream">源流</param>
    /// <param name="destination">目标流</param>
    /// <param name="cancellationToken">取消异步复制的令牌</param>
    /// <returns>复制完成时结束的任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 或 <paramref name="destination"/> 为 <see langword="null"/> 时抛出</exception>
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

    /// <summary>创建包含流当前位置后续字节的内存流</summary>
    /// <param name="stream">要复制的流</param>
    /// <returns>定位在复制字节开头的内存流</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    public static MemoryStream CreateMemoryStream(this Stream stream)
    {
        return new MemoryStream(stream.GetAllBytes());
    }

    /// <summary>从流当前位置读取所有文本，默认使用无字节顺序标记的 UTF-8</summary>
    /// <param name="stream">要读取的流</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消异步读取的令牌</param>
    /// <returns>从流中读取的文本</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    public static async Task<string> ReadAllTextAsync(this Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Check.NotNull(stream), encoding ?? DefaultEncoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>在流当前位置写入文本，默认使用无字节顺序标记的 UTF-8，且不重置位置</summary>
    /// <param name="stream">要写入的流</param>
    /// <param name="text">要写入的文本</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 或 <paramref name="text"/> 为 <see langword="null"/> 时抛出</exception>
    public static void WriteText(this Stream stream, string text, Encoding? encoding = null)
    {
        var writer = new StreamWriter(Check.NotNull(stream), encoding ?? DefaultEncoding, leaveOpen: true);
        using (writer)
        {
            writer.Write(Check.NotNull(text));
        }
    }

    /// <summary>在流当前位置异步写入文本，默认使用无字节顺序标记的 UTF-8，且不重置位置</summary>
    /// <param name="stream">要写入的流</param>
    /// <param name="text">要写入的文本</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <param name="cancellationToken">取消异步写入的令牌</param>
    /// <returns>文本已刷新到流时结束的任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 或 <paramref name="text"/> 为 <see langword="null"/> 时抛出</exception>
    public static async Task WriteTextAsync(this Stream stream, string text, Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        var writer = new StreamWriter(Check.NotNull(stream), encoding ?? DefaultEncoding, leaveOpen: true);
        await using (writer)
        {
            await writer.WriteAsync(Check.NotNull(text).AsMemory(), cancellationToken);
        }
    }

    /// <summary>将可定位流重置到零位置，并返回同一个流</summary>
    /// <param name="stream">要重置的流</param>
    /// <returns>位于零位置的同一个流实例</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="stream"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="NotSupportedException">当 <paramref name="stream"/> 不支持定位时抛出</exception>
    public static Stream ResetPosition(this Stream stream)
    {
        var source = Check.NotNull(stream);
        if (!source.CanSeek)
        {
            throw new NotSupportedException("流不支持定位。");
        }

        source.Position = 0;
        return source;
    }
}
