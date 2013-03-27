/* -----------------------------------------------------------------
 * Copyright (c) 2013 Intel Corporation
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *
 *     * Redistributions in binary form must reproduce the above
 *       copyright notice, this list of conditions and the following
 *       disclaimer in the documentation and/or other materials provided
 *       with the distribution.
 *
 *     * Neither the name of the Intel Corporation nor the names of its
 *       contributors may be used to endorse or promote products derived
 *       from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE INTEL OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * EXPORT LAWS: THIS LICENSE ADDS NO RESTRICTIONS TO THE EXPORT LAWS OF
 * YOUR JURISDICTION. It is licensee's responsibility to comply with any
 * export regulations applicable in licensee's jurisdiction. Under
 * CURRENT (May 2000) U.S. export regulations this software is eligible
 * for export from the U.S. and can be downloaded by or otherwise
 * exported or reexported worldwide EXCEPT to U.S. embargoed destinations
 * which include Cuba, Iraq, Libya, North Korea, Iran, Syria, Sudan,
 * Afghanistan and any other country to which the U.S. has embargoed
 * goods and services.
 * -----------------------------------------------------------------
 */
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
