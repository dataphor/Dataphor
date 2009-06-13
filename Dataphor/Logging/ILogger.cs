
using System.Diagnostics;


namespace Alphora.Dataphor.Logging
{
    public interface ILogger
    {
        
        void WriteLine(TraceLevel ATraceLevel,string AFormat);
        void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs);
    }
}
