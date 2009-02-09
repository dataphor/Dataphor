using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using Alphora.Dataphor.Frontend.Client.Windows;
using System.IO;

namespace Alphora.Dataphor.Dataphoria
{
    class Program
    {

        static void AppDomainException(object sender, UnhandledExceptionEventArgs e)
        {
        }

        public static string DocumentTypeFromFileName(string AFileName)
        {
            string LResult = Path.GetExtension(AFileName).ToLower();
            if (LResult.Length > 0)
                LResult = LResult.Substring(1);
            return LResult;
        }

        /// <summary> Gets a new document expression for a given expression string. </summary>
        public static DocumentExpression GetDocumentExpression(string AExpression)
        {
            DocumentExpression LExpression = new DocumentExpression(AExpression);
            if (LExpression.Type != DocumentType.Document)
                throw new DataphoriaException(DataphoriaException.Codes.CanOnlyLiveEditDocuments);
            return LExpression;
        }

        /// <summary> The main entry point for the application. </summary>
        [STAThread]
        static void Main(string[] AArgs)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomainException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, true);
            Application.EnableVisualStyles();

            //Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
            try
            {
                try
                {
                    FDataphoriaInstance = new FreeDataphoria();
                    try
                    {
                        FDataphoriaInstance.OpenFiles(AArgs);
                        Application.Run((Form)FDataphoriaInstance);
                    }
                    finally
                    {
                        FDataphoriaInstance.Dispose();
                        FDataphoriaInstance = null;
                    }
                }
                catch (Exception LException)
                {
                    HandleException(LException);
                }
            }
            finally
            {
                Application.ThreadException -= new ThreadExceptionEventHandler(ThreadException);
            }
        }

        private static IDataphoria FDataphoriaInstance;
        public static IDataphoria DataphoriaInstance
        {
            get { return FDataphoriaInstance; }
        }

        protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
        {
            HandleException(AArgs.Exception);
        }

        public static void HandleException(System.Exception AException)
        {
            /*if (AException is ThreadAbortException)
                Thread.ResetAbort();
            Frontend.Client.Windows.Session.HandleException(AException);*/
            throw new Exception("Handled Exception" + AException, AException);
        }

        
    }
}
