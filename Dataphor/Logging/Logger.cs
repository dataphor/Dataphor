
using System.Diagnostics;


namespace Alphora.Dataphor.Logging
{
    internal class Logger : ILogger
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
            bool LWillWriteLine = FTraceSwitch.Level >= ATraceLevel;
            Debug.WriteLineIf(LWillWriteLine, AFormat, FTraceSwitch.DisplayName);
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs)
        {
            bool LWillWriteLine = FTraceSwitch.Level >= ATraceLevel;
            Debug.WriteLineIf(LWillWriteLine, string.Format(AFormat, AArgs), FTraceSwitch.DisplayName);
        }
    }
}
