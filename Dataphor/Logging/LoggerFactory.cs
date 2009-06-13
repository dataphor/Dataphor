using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.Logging;

namespace Alphora.Dataphor.Logging
{
    public class LoggerFactory
    {
        public ILogger CreateLogger()
        {
            return new Logger();
        }
    }
}
