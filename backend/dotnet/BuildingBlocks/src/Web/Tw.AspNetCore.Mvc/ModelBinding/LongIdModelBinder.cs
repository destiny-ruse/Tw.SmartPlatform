using System.Globalization;

namespace Tw.AspNetCore.Mvc.ModelBinding;

public sealed class LongIdModelBinder
{
    public static bool TryParse(string? value, out long id)
    {
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }
}
