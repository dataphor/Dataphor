using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client.Windows;


namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
    public class Program
    {

        private static string SFSilentMode = @"/s";

        public static string SilentMode
        {
            get
            {
                return SFSilentMode;
            }

        }

        [STAThread]
        static void Main(string[] AArgs)
        {
            Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
            System.Diagnostics.Process[] LProcesses;
            // See if this is already running
            LProcesses = System.Diagnostics.Process.GetProcessesByName("DAEConfigUtil");
            // There will be 1 running...this one!  But any more, and we just exit.
            if (LProcesses.Length <= 1)
            {
                ApplicationForm LAppForm;
                if ((AArgs.Length > 0) && (AArgs[0] == SFSilentMode))
                    LAppForm = new ApplicationForm(true);
                else
                    LAppForm = new ApplicationForm(false);
                Application.Run();
            }
        }

        protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
        {
            HandleException(AArgs.Exception);
        }

        public static void HandleException(Exception AException)
        {
            if (AException is ThreadAbortException)
                Thread.ResetAbort();
            Session.HandleException(AException);
        }
    }
}
