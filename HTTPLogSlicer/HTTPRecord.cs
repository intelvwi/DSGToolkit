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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Logging;
using LogFileReaders;

namespace HTTPLogSlicer
{
public class HTTPRecord
{
    private static string LogHeader = "[HTTPRecord]";

    public string line;
    public int bucket;
    public double time;
    public DateTime timeDateTime;
    public string source;
    public string op;
    public string remain;

    public HTTPRecord()
    {
    }

    public override string ToString()
    {
        StringBuilder buff = new StringBuilder();
        buff.Append("<b=");
        buff.Append(bucket.ToString());
        buff.Append(",t=");
        buff.Append(time.ToString());
        buff.Append(",d=");
        buff.Append(timeDateTime.ToString());
        buff.Append(",src=");
        buff.Append(source);
        buff.Append(",op=");
        buff.Append(op);
        buff.Append(">");

        return buff.ToString(); ;
    }

    static public List<HTTPRecord> Read(string filename)
    {
        List<HTTPRecord> ret = new List<HTTPRecord>();

        TextReader inReader = new StreamReader(File.Open(filename, FileMode.Open));
        if (inReader == null)
        {
            Logger.Log("{0} Read: Failed opening stat file '{1}'", LogHeader, filename);
            return null;
        }

        string inLine;

        Regex pattern = new Regex(@"^(\d+\.\d+\.\d+\.\d+) - - \[(\d+)/(...)/(\d+):(\d+):(\d+):(\d+) .*""(\w+) (.*)$");

        using (inReader)
        {
            while ((inLine = inReader.ReadLine()) != null)
            {
                try
                {
// SAMPLE
// 10.10.10.1 - - [17/Mar/2013:15:30:43 -0400] "POST /Grid/ HTTP/1.1" 200 4703 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:43 -0400] "POST /Grid/ HTTP/1.1" 200 213 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:43 -0400] "POST /Grid/ HTTP/1.1" 200 211 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:43 -0400] "POST /Grid/ HTTP/1.1" 200 211 "-" "-"
// 98.229.237.99 - - [17/Mar/2013:15:30:43 -0400] "POST /Grid/login/ HTTP/1.1" 200 2586 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:47 -0400] "POST /Grid/ HTTP/1.1" 200 548 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:47 -0400] "POST /Grid/?id=9f54505f-b5aa-4e16-a1ef-d3152a252e08 HTTP/1.1" 200 293 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:47 -0400] "POST /Grid/ HTTP/1.1" 200 290 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:30:48 -0400] "GET /Grid/?id=cce0f112-878f-4586-a2e2-a8f104bba271 HTTP/1.1" 404 234 "-" "-"
// 10.10.10.1 - - [17/Mar/2013:15:31:10 -0400] "POST /Grid/ HTTP/1.1" 200 223 "-" "-"
// 98.229.237.99 - - [17/Mar/2013:15:31:10 -0400] "GET /GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sourceid=&version=4.4.0%20(33429) HTTP/1.1" 200 1432 "-" "Mozilla/5.0 (Windows; U; Windows NT6.1; en-US) AppleWebKit/533.3 (KHTML, like Gecko) SecondLife/4.4.0.33429 (Firestorm-Moses; firestorm skin) Safari/533.3"
// 10.10.10.1 - - [17/Mar/2013:15:31:10 -0400] "POST /Grid/ HTTP/1.1" 200 223 "-" "-"
// 98.229.237.99 - - [17/Mar/2013:15:31:10 -0400] "GET /GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sourceid=&version=4.4.0%20(33429) HTTP/1.1" 200 1432 "-" "Mozilla/5.0 (Windows; U; Windows NT6.1; en-US) AppleWebKit/533.3 (KHTML, like Gecko) SecondLife/4.4.0.33429 (Firestorm-Moses; firestorm skin) Safari/533.3"
// 98.229.237.99 - - [17/Mar/2013:15:31:11 -0400] "GET /GridFrontend//static/styles/default/style.css HTTP/1.1" 200 897 "http://107.7.21.234/GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sourceid=&version=4.4.0%20(33429)" "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.3 (KHTML, like Gecko) SecondLife/4.4.0.33429 (Firestorm-Moses; firestorm skin) Safari/533.3"
// 98.229.237.99 - - [17/Mar/2013:15:31:11 -0400] "GET /GridFrontend//static/javascript/jquery.qtip.js HTTP/1.1" 200 10148 "http://107.7.21.234/GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sourceid=&version=4.4.0%20(33429)" "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.3 (KHTML, like Gecko) SecondLife/4.4.0.33429 (Firestorm-Moses; firestorm skin) Safari/533.3"
// 98.229.237.99 - - [17/Mar/2013:15:31:11 -0400] "GET /GridFrontend//static/styles/default/jquery-ui.css HTTP/1.1" 200 5985 "http://107.7.21.234/GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sourceid=&version=4.4.0%20(33429)" "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.3 (KHTML, like Gecko) SecondLife/4.4.0.33429 (Firestorm-Moses; firestorm skin) Safari/533.3"
// 98.229.237.99 - - [17/Mar/2013:15:31:11 -0400] "GET /GridFrontend//static/styles/default/jquery.qtip.css HTTP/1.1" 200 1695 "http://107.7.21.234/GridFrontend/index.php?channel=Firestorm-Moses&firstlogin=TRUE&grid=mosesdsg&lang=en&os=Microsoft%20Windows%207%2064-bit%20&sstring[] pieces = inLine.Split(',');

                    Match fields = pattern.Match(inLine);
                    if (!fields.Success)
                    {
                        Logger.Log("PARSING FAILURE: {0}", inLine);
                        continue;
                    }

                    HTTPRecord aRec = new HTTPRecord();
                    aRec.line = inLine;
                    aRec.bucket = 0;

                    int nMonth = "xxJanFebMarAprMayJunJulAugSepOctNovDec".IndexOf(fields.Groups[3].Value) / 3;
                    aRec.timeDateTime = new DateTime(
                        int.Parse(fields.Groups[4].Value),  // year
                        nMonth,                             // month
                        int.Parse(fields.Groups[2].Value),  // month day
                        int.Parse(fields.Groups[5].Value),  // hour
                        int.Parse(fields.Groups[6].Value),  // minute
                        int.Parse(fields.Groups[7].Value)   // second
                        );

                    aRec.time = LongDate.DateTimeToExcelDate(aRec.timeDateTime);
                    aRec.source = fields.Groups[1].Value;
                    aRec.op = fields.Groups[8].Value;
                    aRec.remain = fields.Groups[9].Value;

                    ret.Add(aRec);
                    // Logger.Log(aRec.ToString());
                }
                catch (Exception e)
                {
                    Logger.Log("{0} Exception parsing line: '{1}'", LogHeader, inLine);
                    Logger.Log("{0} Exception parsing line: e: {1} ", LogHeader, e);
                    continue;
                }
            }
        }

        return ret;
    }
}
}
