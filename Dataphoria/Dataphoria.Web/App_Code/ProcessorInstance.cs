using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Alphora.Dataphor.Dataphoria.Processing;

namespace Alphora.Dataphor.Dataphoria.Web
{
    public static class ProcessorInstance
    {
        private static Processor _instance;
        public static Processor Instance { get { return _instance; } }

        public static void Initialize()
        {
            // TODO: Need to get the instance name configured here...
            _instance = new Processor();
        }

        public static void Uninitialize()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
            }
        }
    }
}