using System.Globalization;

namespace LTS.Utils;

public static class PrintCurrentDate
{
    public static string PrintDate()
    {
        DateTime now = DateTime.Now;
        CultureInfo culture = new CultureInfo("ko-KR");

        return now.ToString("yyyy-MM-dd tt hh시 mm분 ss초", culture);
    }
}