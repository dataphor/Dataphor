using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Alphora.Dataphor.Frontend.Client.Windows;

namespace Alphora.Dataphor.Dataphoria
{
    internal class Program
    {
        private static IDataphoria SDataphoriaInstance;
        public static IDataphoria DataphoriaInstance { get { return SDataphoriaInstance; } }

        private static void AppDomainException(object sender, UnhandledExceptionEventArgs args) { }

        public static string DocumentTypeFromFileName(string fileName)
        {
            string result = Path.GetExtension(fileName).ToLower();
            if (result.Length > 0)
                result = result.Substring(1);
            return result;
        }

        /// <summary> Gets a new document expression for a given expression string. </summary>
        public static DocumentExpression GetDocumentExpression(string expression)
        {
            var localExpression = new DocumentExpression(expression);
            if (localExpression.Type != DocumentType.Document)
                throw new DataphoriaException(DataphoriaException.Codes.CanOnlyLiveEditDocuments);
            return localExpression;
        }

        /// <summary> The main entry point for the application. </summary>
        [STAThread]
        private static void Main(string[] args)
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
                        SDataphoriaInstance.OpenFiles(args);
                        Application.Run((Form)SDataphoriaInstance);
                    }
                    finally
                    {
                        SDataphoriaInstance.Dispose();
                        SDataphoriaInstance = null;
                    }
                }
                catch (Exception exception)
                {
                    HandleException(exception);
                }
            }
            finally
            {
                Application.ThreadException -= ThreadException;
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