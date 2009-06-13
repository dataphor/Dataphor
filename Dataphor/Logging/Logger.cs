
using System.Diagnostics;


namespace Alphora.Dataphor.Logging
{
    internal class Logger : ILogger
    {
        private static TraceSwitch SRFTraceSwitch;

        public Logger(string ADisplayName):this(ADisplayName,"TraceSwitch for "+ADisplayName)
        {
            
        }

        public Logger(string ADisplayName, string ADescription)
        {
            SRFTraceSwitch=new TraceSwitch(ADisplayName,ADescription);
        }


        public void WriteLine(TraceLevel ATraceLevel, string AFormat)
        {
            bool LWillWriteLine = SRFTraceSwitch.Level >= ATraceLevel;
            if (LWillWriteLine)
                Debug.WriteLine(AFormat, SRFTraceSwitch.DisplayName);
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs)
        {
            bool LWillWriteLine = SRFTraceSwitch.Level >= ATraceLevel;
            if(LWillWriteLine)
                Debug.WriteLine(string.Format(AFormat, AArgs), SRFTraceSwitch.DisplayName);
        }
    }
}
