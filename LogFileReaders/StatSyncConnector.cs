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
using System.IO;
using System.Text;

using Logging;

namespace LogFileReaders
{
public class StatSyncConnectorRecord : Records
{
    public string region;
    public int connectorNum;
    public string actorID;
    public string otherSideActorID;
    public string otherSideRegionName;
    public long msgs_sent; 
    public long msgs_rcvd; 
    public long bytes_sent; 
    public long bytes_rcvd; 
    public float msgs_sent_per_sec; 
    public float msgs_rcvd_per_sec; 
    public float bytes_sent_per_sec; 
    public float bytes_rcvd_per_sec;
    public int queued_msgs;
    public int updatedProperties_sent;
    public int updatedProperties_rcvd;
    public int newObject_sent;
    public int newObject_rcvd;
    public int newPresence_sent;
    public int newPresence_rcvd;
}

public static class StatSyncConnector
{
    private static string LogHeader = "[StatSyncConnector]";

    public static List<Records> Read(string filename)
    {
        TextReader inReader = new StreamReader(File.Open(filename, FileMode.Open));
        if (inReader == null)
        {
            Logger.Log("{0} Read: Failed opening stat file '{1}'", LogHeader, filename);
            return null;
        }

        List<Records> records = new List<Records>();
        string inLine;

        using (inReader)
        {
            while ((inLine = inReader.ReadLine()) != null)
            {
                // Cheap way of checking for the optional title line
                if (inLine.Contains("SyncConnNum"))
                    continue;

                try
                {
                    string[] pieces = inLine.Split(',');
                    StatSyncConnectorRecord aRec = new StatSyncConnectorRecord();
                    aRec.time = LongDate.ParseLongDate(pieces[0]);
                    aRec.bucket = 0;
                    aRec.region = pieces[1];
                    aRec.connectorNum = Parser.GetInt(pieces[2]);
                    aRec.actorID = pieces[3];
                    aRec.otherSideActorID = pieces[4];
                    aRec.otherSideRegionName = pieces[5];
                    aRec.msgs_sent = Parser.GetInt(pieces[6]);
                    aRec.msgs_rcvd = Parser.GetInt(pieces[7]);
                    aRec.bytes_sent = Parser.GetLong(pieces[8]);
                    aRec.bytes_rcvd = Parser.GetLong(pieces[9]);
                    aRec.msgs_sent_per_sec = Parser.GetFloat(pieces[10]);
                    aRec.msgs_rcvd_per_sec = Parser.GetFloat(pieces[11]);
                    aRec.bytes_sent_per_sec = Parser.GetFloat(pieces[12]);
                    aRec.bytes_rcvd_per_sec = Parser.GetFloat(pieces[13]);
                    aRec.queued_msgs = Parser.GetInt(pieces[14]);
                    aRec.updatedProperties_sent = Parser.GetInt(pieces[15]);
                    aRec.updatedProperties_rcvd = Parser.GetInt(pieces[16]);
                    aRec.newObject_sent = Parser.GetInt(pieces[17]);
                    aRec.newObject_rcvd = Parser.GetInt(pieces[18]);
                    aRec.newPresence_sent = Parser.GetInt(pieces[19]);
                    aRec.newPresence_rcvd = Parser.GetInt(pieces[20]);

                    records.Add(aRec);
                }
                catch (Exception e)
                {
                    Logger.Log("{0} Exception parsing line: '{1}'", LogHeader, inLine);
                    Logger.Log("{0} Exception parsing line: e: {1} ", LogHeader, e);
                }
            }
        }
        return records;
    }
}
}
