using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogFileReaders
{
public abstract class Records
{
    public double time;
    public long bucket;
    public Dictionary<string, string> outputFields;
}
}
