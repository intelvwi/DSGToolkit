using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogFileReaders
{
public static class LongDate
{
    public const double ticksPerSecond = 10000000.0d;
    public const double ticksPerHour = ticksPerSecond * 60d * 60d;
    public const double ticksPerDay = ticksPerSecond * 60d * 60d * 24d;

    // Excel time is based on this year.
    public static DateTime ExcelBaseTime = new DateTime(1900, 1, 1, 0, 0, 0);

    /// <summary>
    /// Given a date of the form YYYYMMDD[HH[MM[SS]]], return the Excel date
    /// </summary>
    /// <param name="dat"></param>
    /// <returns></returns>
    public static double ParseLongDate(string dat)
    {
        if (dat.Length < 8) return 0d;
        int year = Int32.Parse(dat.Substring(0, 4));
        int month = Int32.Parse(dat.Substring(4, 2));
        int day = Int32.Parse(dat.Substring(6, 2));
        int hour = 0;
        int min = 0;
        int sec = 0;
        if (dat.Length > 9) hour = Int32.Parse(dat.Substring(8,2));
        if (dat.Length > 11) min = Int32.Parse(dat.Substring(10,2));
        if (dat.Length > 13) sec = Int32.Parse(dat.Substring(12,2));
        DateTime thisTime = new DateTime(year, month, day, hour, min, sec);
        if (dat.Length == 17)
        {
            int millisecs = Int32.Parse(dat.Substring(14, 3));
            thisTime.AddMilliseconds(millisecs);
        }

        // First day is numbered "1" (not zero) and it traditionally forgets that it is a century leap day.
        return ((double)thisTime.Ticks - (double)ExcelBaseTime.Ticks) / ticksPerDay + 2d;
    }

    // The date is in days
    public static string LongDateToString(double dat)
    {
        DateTime thisDate = new DateTime((long)(dat * ticksPerDay) + ExcelBaseTime.Ticks);

        // TODO: this might be off by a day or two. See note above about odd base of ExcelBaseTime.
        return thisDate.ToString("yyyyMMddHHmmssfff");
    }


}
}
