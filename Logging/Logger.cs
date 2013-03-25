using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging
{
public static class Logger
{
    public static void Log(string msg, params Object[] args)
    {
        System.Console.WriteLine(msg, args);
    }
}
}
