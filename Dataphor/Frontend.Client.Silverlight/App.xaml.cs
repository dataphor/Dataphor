using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	public partial class App : Application
	{

		public App()
		{
			this.Startup += this.Application_Startup;
			this.Exit += this.Application_Exit;
			this.UnhandledException += this.Application_UnhandledException;
			
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			RootVisual = new Main();
			
			// Ensure that the long assembly names are registered
			ReflectionUtility.EnsureAssemblyByName();
		}

		private void Application_Exit(object sender, EventArgs e)
		{

		}
		
		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			e.Handled = true;
			
			// If the app is running outside of the debugger then report the exception using
			// the browser's exception mechanism. On IE this will display it a yellow alert 
			// icon in the status bar and Firefox will display a script error.
			if (System.Diagnostics.Debugger.IsAttached)
				System.Diagnostics.Debugger.Break();
			else
				Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
		}
		
		private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
		{
			try
			{
				string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

				System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
			}
			catch (Exception)
			{
			}
		}
	}
}
