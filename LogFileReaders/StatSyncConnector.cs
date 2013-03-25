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
