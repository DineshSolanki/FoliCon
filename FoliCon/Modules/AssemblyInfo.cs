using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace FoliCon.Modules
{
    static class AssemblyInfo
    {
        public static string GetVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }
}
