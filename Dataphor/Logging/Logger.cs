using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Logging
{
    public class Logger : ILogger
    {
        private static TraceSwitch FTraceSwitch;

        public Logger(string ADisplayName):this(ADisplayName,"TraceSwitch for "+ADisplayName)
        {
            
        }

        public Logger(string ADisplayName, string ADescription)
        {
            FTraceSwitch=new TraceSwitch(ADisplayName,ADescription);
        }


        public void WriteLine(TraceLevel ATraceLevel, string AFormat)
        {
            Debug.WriteLineIf(FTraceSwitch.Level == ATraceLevel,AFormat);
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs)
        {
            Debug.WriteLineIf(FTraceSwitch.Level == ATraceLevel, string.Format(AFormat, AArgs));
        }
    }
}
