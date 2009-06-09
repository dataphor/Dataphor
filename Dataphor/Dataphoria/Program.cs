using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria
{
    internal class Program
    {
        private static IDataphoria SDataphoriaInstance;

        public static IDataphoria DataphoriaInstance
        {
            get { return SDataphoriaInstance; }
        }

        private static void AppDomainException(object ASender, UnhandledExceptionEventArgs AArgs)
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
            var LExpression = new DocumentExpression(AExpression);
            if (LExpression.Type != DocumentType.Document)
                throw new DataphoriaException(DataphoriaException.Codes.CanOnlyLiveEditDocuments);
            return LExpression;
        }

        /// <summary> The main entry point for the application. </summary>
        [STAThread]
        private static void Main(string[] AArgs)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomainException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, true);
            Application.EnableVisualStyles();

            Application.ThreadException += ThreadException;
            try
            {
                try
                {
                    SDataphoriaInstance = new Dataphoria();
                    try
                    {
                        SDataphoriaInstance.OpenFiles(AArgs);
                        Application.Run((Form) SDataphoriaInstance);
                    }
                    finally
                    {
                        SDataphoriaInstance.Dispose();
                        SDataphoriaInstance = null;
                    }
                }
                catch (Exception LException)
                {
                    HandleException(LException);
                }
            }
            finally
            {
                Application.ThreadException -= ThreadException;
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
            //throw new Exception("Handled Exception" + AException, AException);
        }
    }
}