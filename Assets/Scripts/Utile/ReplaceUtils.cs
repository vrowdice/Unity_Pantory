using System.Collections.Generic;
using System.Globalization;

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

    /// <summary>
    /// 숫자에 3자리마다 쉼표를 추가하여 포맷합니다.
    /// </summary>
    /// <param name="number">포맷할 숫자</param>
    /// <returns>쉼표가 추가된 문자열 (예: 1234567 -> "1,234,567")</returns>
    public static string FormatNumberWithCommas(long number)
    {
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }
}
