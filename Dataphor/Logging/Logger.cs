
using System.Diagnostics;


namespace Alphora.Dataphor.Logging
{
    internal class Logger : ILogger
    {
        private static TraceSwitch SFTraceSwitch;

        public Logger(string ADisplayName):this(ADisplayName,"TraceSwitch for "+ADisplayName)
        {
            
        }

        public Logger(string ADisplayName, string ADescription)
        {
            SFTraceSwitch=new TraceSwitch(ADisplayName,ADescription);
        }


        public void WriteLine(TraceLevel ATraceLevel, string AFormat)
        {
            bool LWillWriteLine = SFTraceSwitch.Level >= ATraceLevel;
            if (LWillWriteLine)
                Debug.WriteLine(AFormat, SFTraceSwitch.DisplayName);
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs)
        {
            bool LWillWriteLine = SFTraceSwitch.Level >= ATraceLevel;
            if(LWillWriteLine)
                Debug.WriteLine(string.Format(AFormat, AArgs), SFTraceSwitch.DisplayName);
        }
    }
}
