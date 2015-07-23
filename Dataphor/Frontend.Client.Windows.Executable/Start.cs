/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using WinForms = System.Windows.Forms;

using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Windows.Executable
{
	public class Start
	{
		public const string AliasParameter = @"-alias";
		public const string ApplicationParameter = @"-application";
		public const string UserParameter = @"-user";
		public const string ApplicationListExpression = @"Applications over {ID, Description}";
		public const string ApplicationListOrder = @"order {Description}";

		/// <summary> The main entry point for the application. </summary>
		[STAThread]
		public static void Main(string[] args)
		{
			WinForms.Application.SetUnhandledExceptionMode(WinForms.UnhandledExceptionMode.CatchException, true);
			WinForms.Application.EnableVisualStyles();

			WinForms.Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
			try
			{
				// Parse the command line
				string alias = String.Empty;
				string applicationID = String.Empty;
				string userID = String.Empty;
				int i = 0;
				while (i < args.Length)
				{
					switch (args[i].ToLower())
					{
						case AliasParameter :
							alias = args[i + 1];
							i++;
							break;
						case ApplicationParameter :
							applicationID = args[i + 1];
							i++;
							break;
						case UserParameter :
							userID = args[i + 1];
							i++;
							break;
						default :
							throw new ClientException(ClientException.Codes.InvalidCommandLine, args[i]);
					}
					i++;
				}

				AliasConfiguration configuration;
				if (alias == String.Empty)
					configuration = ServerConnectForm.Execute();
				else
				{
					configuration = AliasManager.LoadConfiguration();
					configuration.DefaultAliasName = alias;
				}

				if (userID != String.Empty)
					configuration.Aliases[configuration.DefaultAliasName].SessionInfo.UserID = userID;

				using (DataSession dataSession = new DataSession())
				{
					dataSession.Alias = configuration.Aliases[configuration.DefaultAliasName];
					dataSession.SessionInfo.Environment = "WindowsClient";
					dataSession.Active = true;
					
					if (applicationID == String.Empty)
					{
						using (DAE.Client.DataView view = new DAE.Client.DataView())
						{
							view.Session = dataSession;
							view.Expression = ApplicationListExpression;
							view.OrderString = ApplicationListOrder;
							view.IsReadOnly = true;
							view.Open();
							
							// Count the number of applications
							System.Collections.IEnumerator enumValue = view.GetEnumerator();	 // use explicit enumerator to avoid foreach unused var warning
							int count = 0;
							while (enumValue.MoveNext())
								count++;

							// Prompt the user for the application if there is not exactly one row
							if (count != 1)
							{
								view.First();
								ApplicationListForm.Execute(view);
							}

							applicationID = view.Fields["ID"].AsString;
						}
					}

					Session session = new Session(dataSession, false); // Pass false because the DataSession will be disposed by the using block
					session.Start(session.SetApplication(applicationID)); // This call will dispose the session.
				}
			}
			catch (AbortException)
			{
				// Do nothing (ignore abort)
			}
			catch (Exception exception)
			{
				Windows.Session.HandleException(exception);
			}
		}

		protected static void ThreadException(object sender, ThreadExceptionEventArgs args)
		{
			Windows.Session.HandleException(args.Exception);
		}
	}
}