using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace Alphora.Dataphor.DAE.Service.ConfigurationUtility
{
    public class Program
    {

        public const string FCSilentMode = @"/s";

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
                if ((AArgs.Length > 0) && (AArgs[0] == FCSilentMode))
                    LAppForm = new ApplicationForm(true);
                else
                    LAppForm = new ApplicationForm(false);
                Application.Run();
            }
        }

        protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
        {
            MessageBox.Show(AArgs.Exception.ToString());
        }
    }
}
