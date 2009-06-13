using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Logging
{
    public interface ILogger
    {
        
        void WriteLine(TraceLevel ATraceLevel,string AFormat);
        void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs);
    }
}
