using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogFileReaders
{
static class Parser
{

    public static int GetInt(string fld)
    {
        int ret = 0;
        if (fld.Length > 0)
            ret = int.Parse(fld);
        return ret;
    }
    public static long GetLong(string fld)
    {
        long ret = 0;
        if (fld.Length > 0)
            ret = long.Parse(fld);
        return ret;
    }
    public static float GetFloat(string fld)
    {
        float ret = 0;
        if (fld.Length > 0)
            ret = float.Parse(fld);
        return ret;
    }
    public static double GetDouble(string fld)
    {
        double ret = 0;
        if (fld.Length > 0)
            ret = double.Parse(fld);
        return ret;
    }
}
}
