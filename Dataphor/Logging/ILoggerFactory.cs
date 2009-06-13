using System;

namespace Alphora.Dataphor.Logging
{
    public interface ILoggerFactory
    {
        ILogger CreateLogger(string ADisplayName, string ADescription);
        ILogger CreateLogger(string ADisplayName);
        ILogger CreateLogger(Type AType);
    }
}