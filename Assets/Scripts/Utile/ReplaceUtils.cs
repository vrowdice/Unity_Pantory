using System.Collections.Generic;

public static class ReplaceUtils
{
    public static string FormatNumber(long argNumber)
    {
        if (argNumber >= 1_000_000_000)
            return (argNumber / 1_000_000_000f).ToString("0.###") + "B";
        else if (argNumber >= 1_000_000)
            return (argNumber / 1_000_000f).ToString("0.###") + "M";
        else if (argNumber >= 1_000)
            return (argNumber / 1_000f).ToString("0.###") + "K";
        else
            return argNumber.ToString();
    }
}
