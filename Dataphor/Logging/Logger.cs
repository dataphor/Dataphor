
using System;
using System.Diagnostics;
using System.Reflection;


namespace Alphora.Dataphor.Logging
{
    internal class Logger : ILogger
    {
        private static TraceSwitch SFTraceSwitch;
        private Type FType;

        public Logger(string ADisplayName)
			: this(ADisplayName, "TraceSwitch for " + ADisplayName)
        {
            
        }

        public Logger(string ADisplayName, string ADescription)
        {
            SFTraceSwitch = new TraceSwitch(ADisplayName,ADescription);
        }

        public Logger(Type AType) : this(AType.FullName)
        {
            FType = AType;
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat)
        {
            bool LWillWriteLine = SFTraceSwitch.Level >= ATraceLevel;
            if (LWillWriteLine)
            {
                String LCategoryName;
                if (FType == null)
                {
                    LCategoryName = SFTraceSwitch.DisplayName;
                }
                else
                {
                    StackTrace LStackTrace = new StackTrace();
                    StackFrame LStackFrame = LStackTrace.GetFrame(1);
                    MethodBase LMethodBase = LStackFrame.GetMethod();
                    LCategoryName = LMethodBase.ReflectedType + "." + LMethodBase.Name;
                }
                Debug.WriteLine(AFormat, LCategoryName);
            }
        }

        public void WriteLine(TraceLevel ATraceLevel, string AFormat, params object[] AArgs)
        {
            bool LWillWriteLine = SFTraceSwitch.Level >= ATraceLevel;
            if (LWillWriteLine)
            {
                String LCategoryName;
                if (FType == null)
                {
                    LCategoryName = SFTraceSwitch.DisplayName;
                }
                else
                {
                    StackTrace LStackTrace = new StackTrace();
                    StackFrame LStackFrame = LStackTrace.GetFrame(1);
                    MethodBase LMethodBase = LStackFrame.GetMethod();
                    LCategoryName = LMethodBase.ReflectedType + "." + LMethodBase.Name;
                }
                Debug.WriteLine(String.Format(AFormat, AArgs), LCategoryName);
            }
        }
    }
}
