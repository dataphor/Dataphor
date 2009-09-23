

using System;

namespace Alphora.Dataphor.Logging
{
    public class LoggerFactory : ILoggerFactory
    {
		private static readonly ILoggerFactory SRFInstance = new LoggerFactory();

		private LoggerFactory() { }

		public static ILoggerFactory Instance
		{
			get { return SRFInstance; }
		}

        public ILogger CreateLogger(string ADisplayName, string ADescription)
        {
            return new Logger(ADisplayName, ADescription);
        }

        public ILogger CreateLogger(string ADisplayName)
        {
            return new Logger(ADisplayName);
        }

        public ILogger CreateLogger(Type AType)
        {
            return new Logger(AType);
        }
    }
}
