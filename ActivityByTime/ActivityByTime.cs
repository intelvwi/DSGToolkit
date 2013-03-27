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

namespace ActivityByNumAgents
{
public class ActivityByTime
{
    Dictionary<string, string> m_Parameters;
    int m_ParamVerbose = 0;
    string m_inFiles = "";
    string m_outFile = "ActivityByTime.csv";
    
    private bool IsVerbose { get { return m_ParamVerbose > 0; } }
    private bool IsVeryVerbose { get { return m_ParamVerbose > 1; } }

    private const string connName = "conn-";
    private const string sceneName = "scene-";
    private const string serverName = "server-";

    private string Invocation()
    {
        return @"Invocation:
INVOCATION:
ActivityByTime 
        -o|--output outputFilename
        --verbose
        listOfInputFiles
";
    }

    static void Main(string[] args)
    {
        ActivityByTime prog = new ActivityByTime();
        prog.Start(args);
        return;
    }
    
    public ActivityByTime()
    {
    }

    public void Start(string[] args)
    {
        m_Parameters = ParameterParse.ParseArguments(args, false /* firstOpFlag */, true /* multipleFiles */);
        foreach (KeyValuePair<string, string> kvp in m_Parameters)
        {
            switch (kvp.Key)
            {
                case "-o":
                case "--output":
                    m_outFile = kvp.Value;
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

        GlobalRecordCollection globalCollection = new GlobalRecordCollection();

        // Read in all log records into approriate structure type for each log line
        if (!String.IsNullOrEmpty(m_inFiles))
        {
            string[] files = m_inFiles.Split(',');
            foreach (string fileName in files)
            {
                if (fileName.Substring(0, connName.Length) == connName)
                {
                    List<Records> syncConn = StatSyncConnector.Read(fileName);
                    globalCollection.Add(syncConn);
                }
                if (fileName.Substring(0, sceneName.Length) == sceneName)
                {
                    List<Records> syncConn = StatScene.Read(fileName);
                    globalCollection.Add(syncConn);
                }
                if (fileName.Substring(0, serverName.Length) == serverName)
                {
                    List<Records> syncConn = StatServer.Read(fileName);
                    globalCollection.Add(syncConn);
                }
            }
        }

        // Compute the number of buckets based on the log records. Compute min and max.
        double[] bucketTimes = ComputeBuckets(globalCollection, 10 /* bucket size in seconds */);
        long numBuckets = bucketTimes.Length;

        if (numBuckets > 0)
        {
            List<Records>[] buckets = DistributeToBucketArray(globalCollection, numBuckets);

            GenerateOutput(globalCollection, buckets, bucketTimes);
        }
    }

    // A collection class for all the log records.
    private class GlobalRecordCollection
    {
        private List<List<Records>> collections;
        public GlobalRecordCollection()
        {
            collections = new List<List<Records>>();
        }
        public void Add(List<Records> newCollection)
        {
            collections.Add(newCollection);
        }
        public void ForEveryRecord(Action<Records> act)
        {
            foreach (List<Records> oneCollection in collections)
            {
                foreach (Records rec in oneCollection)
                {
                    act(rec);
                }
            }
        }
    }

    // Loop through all the log records, compute min and max time and number of buckets.
    // Also assign each record to a bucket and store the bucket number into the record.
    private double[] ComputeBuckets(GlobalRecordCollection gRecords, int resolutionSec)
    {
        double[] bucketTimes;

        double secondsInDay = 24d * 60d * 60d;
        double minDate = double.MaxValue;
        double maxDate = double.MinValue;

        // compute the high and low times
        gRecords.ForEveryRecord((r) => {
            if (r.time < minDate) minDate = r.time;
            if (r.time > maxDate) maxDate = r.time;
        } );

        // add the bucket number to each of the records
        gRecords.ForEveryRecord((r) => {
            r.bucket = (long)((r.time - minDate) * secondsInDay / (double)resolutionSec);
        } );

        long numBuckets = (long)((maxDate - minDate) * secondsInDay / (double)resolutionSec);
        // Add an extra bucket for rounding at the end
        numBuckets += 1;

        // Compute a base time for each bucket
        bucketTimes = new double[numBuckets];
        for (int ii = 0; ii < numBuckets; ii++)
        {
            bucketTimes[ii] = minDate + (ii * resolutionSec) / secondsInDay;
        }

        Logger.Log("DEBUG: ComputeBuckets: numBuckets={0}", numBuckets);

        return bucketTimes;
    }

    // Loop through all the records and build the list of records for each bucket.
    // Returns an array of lists. Each element of the array is a bucket and each bucket has
    //    a list of the records assigned to the bucket.
    private List<Records>[] DistributeToBucketArray(GlobalRecordCollection gRecords, long numBuckets)
    {
        List<Records>[] buckets = new List<Records>[numBuckets];

        for (int ii = 0; ii < numBuckets; ii++)
        {
            buckets[ii] = new List<Records>();
        }

        gRecords.ForEveryRecord((r) =>
        {
            try
            {
                buckets[r.bucket].Add(r);
            }
            catch (Exception e)
            {
                Logger.Log("ERROR: adding record to bucket failure. r.bucket={0}, numBuckets={1}, e:{2}", r.bucket, numBuckets, e);
            }
        });

        return buckets;
    }

    // Output a line for each bucket. Loop through the records in the bucket and figure out the
    //    logged fields.
    // The records are first passed through to get the names of all the logged fields so
    //    each line of the output file will have the same number of fields.
    // Modify GetRecordFileNames() to change the columns that are actually output.
    private void GenerateOutput(GlobalRecordCollection gRecords, List<Records>[] buckets, double[] bucketTimes)
    {
        List<string> fieldNames = new List<string>();
        bool firstLine = true;

        gRecords.ForEveryRecord((r) =>
        {
            GetRecordFieldNames(r, ref fieldNames);
        });

        TextWriter outWriter = new StreamWriter(File.Open(m_outFile, FileMode.Create));
        if (outWriter != null)
        {
            using (outWriter)
            {
                for (int ii=0; ii < buckets.Length; ii++)
                {
                    double bucketTime = 0d;
                    Dictionary<string, string> outFields = new Dictionary<string, string>();
                    foreach (Records rec in buckets[ii])
                    {
                        bucketTime = bucketTimes[ii];

                        // Add the output fields (generated in GetRecordFileNames) to this output line.
                        foreach (string fldname in rec.outputFields.Keys)
                        {
                            if (!outFields.ContainsKey(fldname))
                                outFields.Add(fldname, rec.outputFields[fldname]);

                        }
                    }

                    // If the first line, output the field names
                    if (firstLine)
                    {
                        StringBuilder tbuff = new StringBuilder();
                        tbuff.Append("bucket");
                        tbuff.Append(",");
                        tbuff.Append("time");
                        foreach (string fName in fieldNames)
                        {
                            tbuff.Append(",");
                            tbuff.Append(fName);
                        }
                        outWriter.WriteLine(tbuff.ToString());
                        firstLine = false;
                    }

                    StringBuilder buff = new StringBuilder();
                    buff.Append(ii.ToString());         // bucket number
                    buff.Append(",");
                    buff.Append(bucketTime.ToString()); // bucket time

                    foreach (string fName in fieldNames)
                    {
                        buff.Append(",");
                        if (outFields.ContainsKey(fName))
                            buff.Append(outFields[fName]);
                    }
                    outWriter.WriteLine(buff.ToString());
                }
            }
        }
        else
        {
            Logger.Log("COULD NOT OPEN OUTPUT FILE '{0}", m_outFile);
        }
    }

    // Scan through all the records and add the output fields to same while also
    //    creating a list of all the output fields. The latter is for the title line.
    // This is where you define which fields to output.
    private void GetRecordFieldNames(Records rec, ref List<string> fieldNames)
    {
        string fldName;

        rec.outputFields = new Dictionary<string, string>();

        StatSceneRecord sceneRec = rec as StatSceneRecord;
        if (sceneRec != null)
        {
            // Logger.Log("DEBUG: GenerateOutput: sceneRec: bkt={0}, container={1}", ii, sceneRec.container);
            fldName = sceneRec.container + " agents";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, sceneRec.rootAgents.ToString());

            fldName = sceneRec.container + " simFPS";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, sceneRec.simFPS.ToString());
            
            fldName = sceneRec.container + " physicsTime";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, sceneRec.physicsTime.ToString());

            fldName = sceneRec.container + " totalPrims";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, sceneRec.totalPrims.ToString());
        }

        StatSyncConnectorRecord syncRec = rec as StatSyncConnectorRecord;
        if (syncRec != null)
        {
            // Logger.Log("DEBUG: GenerateOutput: syncRec: bkt={0}, thisID={1}, otherID={2}", ii, syncRec.actorID, syncRec.otherSideActorID);
            string thisActorID = syncRec.actorID;
            string otherActorID = syncRec.otherSideActorID;
            string fldNameBase = thisActorID + "/" + otherActorID;

            /*
            fldName = fldNameBase + " msgsSent";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.msgs_sent.ToString());

            fldName = fldNameBase + " msgsRcvd";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.msgs_rcvd.ToString());

            fldName = fldNameBase + " BytesSent";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.bytes_sent.ToString());

            fldName = fldNameBase + " BytesRcvd";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.bytes_rcvd.ToString());
            */

            fldName = fldNameBase + " msgsSent";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.msgs_sent.ToString());

            fldName = fldNameBase + " msgsRcvd";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.msgs_rcvd.ToString());

            fldName = fldNameBase + " BytesSent";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.bytes_sent.ToString());

            fldName = fldNameBase + " BytesRcvd";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.bytes_rcvd.ToString());

            fldName = fldNameBase + " QueueSize";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, syncRec.queued_msgs.ToString());
        }

        StatServerRecord serverRec = rec as StatServerRecord;
        if (serverRec != null)
        {
            // Logger.Log("DEBUG: GenerateOutput: serverRec: bkt={0}, region={1}", ii, serverRec.regionName);
            fldName = serverRec.regionName + "totalProcTime";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, serverRec.totalProcessorTime.ToString());

            fldName = serverRec.regionName + "bytesRcvd";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, serverRec.bytesRcvd.ToString());

            fldName = serverRec.regionName + "bytesSent";
            if (!fieldNames.Contains(fldName))
                fieldNames.Add(fldName);
            rec.outputFields.Add(fldName, serverRec.bytesSent.ToString());
        }
    }

}
}
