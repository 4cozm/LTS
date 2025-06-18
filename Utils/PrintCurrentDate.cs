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

    public static string PrintDate(DateTime dateTime)
    {
        CultureInfo culture = new CultureInfo("ko-KR");
        return dateTime.ToString("yyyy-MM-dd tt hh시 mm분 ss초", culture);
    }

}