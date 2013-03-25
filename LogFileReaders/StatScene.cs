using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using Logging;

namespace LogFileReaders
{
public class StatSceneRecord : Records
{
    public string category;
    public string container;
    public int rootAgents;
    public int childAgents;
    public float simFPS;
    public float physicsFPS;
    public int totalPrims;
    public int activePrims;
    public int activeScripts;
    public int scriptLines;
    public float frameTime;
    public float physicsTime;
    public float agentTime;
    public float imageTime;
    public float netTime;
    public float otherTime;
    public float simSpareMS;
    public float agentUpdates;
    public int slowFrames;
    public float timeDilation;
}

public static class StatScene
{
    private static string LogHeader = "[StatScene]";

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
                    StatSceneRecord aRec = new StatSceneRecord();
                    aRec.time = LongDate.ParseLongDate(pieces[0]);
                    aRec.bucket = 0;
                    aRec.category = pieces[1];
                    aRec.container = pieces[2];
                    aRec.rootAgents = Parser.GetInt(pieces[3]);
                    aRec.childAgents = Parser.GetInt(pieces[4]);
                    aRec.simFPS = Parser.GetFloat(pieces[5]);
                    aRec.physicsFPS = Parser.GetFloat(pieces[6]);
                    aRec.totalPrims = Parser.GetInt(pieces[7]);
                    aRec.activePrims = Parser.GetInt(pieces[8]);
                    aRec.activeScripts = Parser.GetInt(pieces[9]);
                    aRec.scriptLines = (int)Parser.GetFloat(pieces[10]);    // somtimes is recorded as a fraction
                    aRec.frameTime = Parser.GetFloat(pieces[11]);
                    aRec.physicsTime = Parser.GetFloat(pieces[12]);
                    aRec.agentTime = Parser.GetFloat(pieces[13]);
                    aRec.imageTime = Parser.GetFloat(pieces[14]);
                    aRec.netTime = Parser.GetFloat(pieces[15]);
                    aRec.otherTime = Parser.GetFloat(pieces[16]);
                    aRec.simSpareMS = Parser.GetFloat(pieces[17]);
                    aRec.agentUpdates = Parser.GetFloat(pieces[18]);
                    aRec.slowFrames = Parser.GetInt(pieces[19]);
                    aRec.timeDilation = Parser.GetFloat(pieces[20]);

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
