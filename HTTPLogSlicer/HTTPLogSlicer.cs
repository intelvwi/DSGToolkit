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
    
    private bool IsVerbose { get { return m_ParamVerbose > 0; } }
    private bool IsVeryVerbose { get { return m_ParamVerbose > 1; } }

    private int m_bucketSeconds = 10;

    private string Invocation()
    {
        return @"Invocation:
INVOCATION:
HTTPLogSlicer 
        -p|--period sliceSeconds
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
    }

}
}
