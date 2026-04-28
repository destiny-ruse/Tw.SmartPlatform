namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for GUID values.</summary>
public static class GuidExtensions
{
    /// <summary>Returns whether a nullable GUID is <see langword="null"/> or empty.</summary>
    /// <param name="value">The nullable GUID value.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is <see langword="null"/> or <see cref="Guid.Empty"/>.</returns>
    public static bool IsNullOrEmpty(this Guid? value)
    {
        return value is null || value.Value == Guid.Empty;
    }

    /// <summary>Formats a GUID using the compact N format.</summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>The GUID without separators.</returns>
    public static string ToNString(this Guid value)
    {
        return value.ToString("N");
    }
}
