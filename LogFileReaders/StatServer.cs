using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Logging;

namespace LogFileReaders
{
public class StatServerRecord : Records
{
    public string category;
    public string regionName;
    public float cPUPercent;
    public float totalProcessorTime;
    public float userProcessorTime;
    public float privilegedProcessorTime;
    public int threads;
    public float averageMemoryChurn;
    public float lastMemoryChurn;
    public float objectMemory;
    public float processMemory;
    public double bytesRcvd;
    public double bytesSent;
    public double totalBytes;
}

public static class StatServer
{
    private static string LogHeader = "[StatServer]";

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
                if (inLine.Contains(",Category,"))
                    continue;

                try
                {
                    string[] pieces = inLine.Split(',');
                    StatServerRecord aRec = new StatServerRecord();
                    aRec.time = LongDate.ParseLongDate(pieces[0]);
                    aRec.bucket = 0;
                    aRec.category = pieces[1];
                    aRec.regionName = pieces[2];
                    aRec.cPUPercent = Parser.GetFloat(pieces[3]);
                    aRec.totalProcessorTime = Parser.GetFloat(pieces[4]);
                    aRec.userProcessorTime = Parser.GetFloat(pieces[5]);
                    aRec.privilegedProcessorTime = Parser.GetFloat(pieces[6]);
                    aRec.threads = Parser.GetInt(pieces[7]);
                    aRec.averageMemoryChurn = Parser.GetFloat(pieces[8]);
                    aRec.lastMemoryChurn = Parser.GetFloat(pieces[9]);
                    aRec.objectMemory = Parser.GetFloat(pieces[10]);
                    aRec.processMemory = Parser.GetFloat(pieces[11]);
                    aRec.bytesRcvd = Parser.GetDouble(pieces[12]);
                    aRec.bytesSent = Parser.GetDouble(pieces[13]);
                    aRec.totalBytes = Parser.GetDouble(pieces[14]);

                    records.Add(aRec);
                }
                catch (Exception e)
                {
                    Logger.Log("{0} Exception parsing line: '{1}'", LogHeader, inLine);
                    Logger.Log("{0} Exception parsing line: e: {1}", LogHeader, e);
                }
            }
        }
        return records;
    }
}
}
