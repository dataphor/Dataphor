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
        static void Main(string[] args)
        {
            Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
            System.Diagnostics.Process[] processes;
            // See if this is already running
            processes = System.Diagnostics.Process.GetProcessesByName("DAEConfigUtil");
            // There will be 1 running...this one!  But any more, and we just exit.
            if (processes.Length <= 1)
            {
                ApplicationForm appForm;
                if ((args.Length > 0) && (args[0] == SFSilentMode))
                    appForm = new ApplicationForm(true);
                else
                    appForm = new ApplicationForm(false);
                Application.Run();
            }
        }

        protected static void ThreadException(object sender, ThreadExceptionEventArgs args)
        {
            HandleException(args.Exception);
        }

        public static void HandleException(Exception exception)
        {
            if (exception is ThreadAbortException)
                Thread.ResetAbort();
            Session.HandleException(exception);
        }
    }
}
