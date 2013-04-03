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

using LogFileReaders;
using Logging;
using ParameterParsing;

namespace HTTPLogSlicer
{
class HTTPLogSlicer
{
    Dictionary<string, string> m_Parameters;
    int m_ParamVerbose = 0;
    string m_inFiles = "";
    string m_outFile = "HTTPLogSlicer.csv";
    string m_cmHosts = "10.10.10.1,50.18.130.180,50.18.138.160,54.241.71.140";
    
    private bool IsVerbose { get { return m_ParamVerbose > 0; } }
    private bool IsVeryVerbose { get { return m_ParamVerbose > 1; } }

    private int m_bucketSeconds = 10;

    private string Invocation()
    {
        return @"Invocation:
INVOCATION:
HTTPLogSlicer 
        -p|--period sliceSeconds
        -h|--cmHosts commaListOfCMHosts
        -o|--output outputFilename
        --verbose
        inputFilenames
";
    }

    static void Main(string[] args)
    {
        HTTPLogSlicer prog = new HTTPLogSlicer();
        prog.Start(args);
        return;
    }

    private struct BucketLine
    {
        public int bucket;
        public double time;
        public int[] hostCount;
        public int logins;
    };

    public void Start(string[] args)
    {
        m_Parameters = ParameterParse.ParseArguments(args, false /* firstOpFlag */, true /* multipleFiles */);
        foreach (KeyValuePair<string, string> kvp in m_Parameters)
        {
            switch (kvp.Key)
            {
                case "-p":
                case "--period":
                    m_bucketSeconds = int.Parse(kvp.Value);
                    break;
                case "-o":
                case "--output":
                    m_outFile = kvp.Value;
                    break;
                case "-h":
                case "--cmHosts":
                    m_cmHosts = kvp.Value;
                    break;
                case "--verbose":
                    m_ParamVerbose++;
                    break;
                case ParameterParse.LAST_PARAM:
                    m_inFiles = kvp.Value;
                    break;
                case ParameterParse.ERROR_PARAM:
                    // if we get here, the parser found an error
                    Logger.Log("Parameter error: " + kvp.Value);
                    Logger.Log(Invocation());
                    return;
                default:
                    Logger.Log("ERROR: UNKNOWN PARAMETER: " + kvp.Key);
                    Logger.Log(Invocation());
                    return;
            }
        }

        List<HTTPRecord> records = new List<HTTPRecord>();

        // Read in the records
        if (!String.IsNullOrEmpty(m_inFiles))
        {
            string[] files = m_inFiles.Split(',');
            foreach (string fileName in files)
            {
                records.AddRange(HTTPRecord.Read(fileName));
            }
        }

        // Find high and low dates and compute the number of buckets
        double minDate = double.MaxValue;
        double maxDate = double.MinValue;
        foreach (HTTPRecord rec in records)
        {
            minDate = Math.Min(minDate, rec.time);
            maxDate = Math.Max(maxDate, rec.time);
        }

        double bucketBaseTime = minDate;
        int numBuckets = ((int)((maxDate - minDate) * LongDate.secondsPerDay)) / m_bucketSeconds;
        numBuckets += 1;    // add a last bucket for rounding error at the end.
        Logger.Log("Number of buckets = {0}", numBuckets);

        // Loop through all the records and assign each to a bucket
        foreach (HTTPRecord rec in records)
        {
            rec.bucket = ((int)((rec.time - bucketBaseTime) * LongDate.secondsPerDay)) / m_bucketSeconds;
        }

        // Specify individual hosts to count accesses with CSV list "host,host,host"
        List<string> cmHosts = m_cmHosts.Split(',').ToList<string>();
        int numHosts = cmHosts.Count;
        if (numHosts == 0)
        {
            Logger.Log("NUMBER OF Client Manager HOSTS MUST NOT BE ZERO!!");
            return;
        }

        // Initialize each bucket line with the variable sized structures
        BucketLine[] bucketLines = new BucketLine[numBuckets];
        for (int ii = 0; ii < numBuckets; ii++)
        {
            bucketLines[ii].bucket = ii;
            bucketLines[ii].time = bucketBaseTime + ((double)(ii * m_bucketSeconds) / LongDate.secondsPerDay);
            bucketLines[ii].hostCount = new int[numHosts];
            bucketLines[ii].logins = 0;
        }

        // Loop through all the records and fill the bucket info
        foreach (HTTPRecord rec in records)
        {
            int nHost = cmHosts.IndexOf(rec.source);
            if (nHost >= 0)
                bucketLines[rec.bucket].hostCount[nHost]++;
            if (rec.remain.Contains("/Grid/login/ "))
                bucketLines[rec.bucket].logins++;
        }

        // Print out all the buckets
        bool firstLine = true;
        TextWriter outWriter = new StreamWriter(File.Open(m_outFile, FileMode.Create));
        if (outWriter != null)
        {
            using (outWriter)
            {
                if (firstLine)
                {
                    StringBuilder buff = new StringBuilder();
                    buff.Append("bucket");
                    buff.Append(",");
                    buff.Append("time");
                    buff.Append(",");
                    buff.Append("logins");
                    buff.Append(",");
                    for (int ii = 0; ii < cmHosts.Count; ii++)
                    {
                        buff.Append(cmHosts[ii]);
                        buff.Append(",");
                    }
                    outWriter.WriteLine(buff.ToString());
                    firstLine = false;
                }

                foreach (BucketLine buck in bucketLines)
                {
                    StringBuilder buff = new StringBuilder();
                    buff.Append(buck.bucket.ToString());
                    buff.Append(",");
                    buff.Append(buck.time.ToString());
                    buff.Append(",");
                    buff.Append(buck.logins.ToString());
                    buff.Append(",");
                    for (int ii = 0; ii < buck.hostCount.Length; ii++)
                    {
                        buff.Append(buck.hostCount[ii].ToString());
                        buff.Append(",");
                    }
                    outWriter.WriteLine(buff.ToString());
                }
            }
        }
    }
}
}
