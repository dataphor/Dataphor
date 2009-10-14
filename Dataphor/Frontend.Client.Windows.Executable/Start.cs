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
		public const string CAliasParameter = @"-alias";
		public const string CApplicationParameter = @"-application";
		public const string CUserParameter = @"-user";
		public const string CApplicationListExpression = @"Applications over {ID, Description}";
		public const string CApplicationListOrder = @"order {Description}";

		/// <summary> The main entry point for the application. </summary>
		[STAThread]
		public static void Main(string[] AArgs)
		{
			WinForms.Application.SetUnhandledExceptionMode(WinForms.UnhandledExceptionMode.CatchException, true);
			WinForms.Application.EnableVisualStyles();

			WinForms.Application.ThreadException += new ThreadExceptionEventHandler(ThreadException);
			try
			{
				// Parse the command line
				string LAlias = String.Empty;
				string LApplicationID = String.Empty;
				string LUserID = String.Empty;
				int i = 0;
				while (i < AArgs.Length)
				{
					switch (AArgs[i].ToLower())
					{
						case CAliasParameter :
							LAlias = AArgs[i + 1];
							i++;
							break;
						case CApplicationParameter :
							LApplicationID = AArgs[i + 1];
							i++;
							break;
						case CUserParameter :
							LUserID = AArgs[i + 1];
							i++;
							break;
						default :
							throw new ClientException(ClientException.Codes.InvalidCommandLine, AArgs[i]);
					}
					i++;
				}

				AliasConfiguration LConfiguration;
				if (LAlias == String.Empty)
					LConfiguration = ServerConnectForm.Execute();
				else
				{
					LConfiguration = AliasManager.LoadConfiguration();
					LConfiguration.DefaultAliasName = LAlias;
				}

				if (LUserID != String.Empty)
					LConfiguration.Aliases[LConfiguration.DefaultAliasName].SessionInfo.UserID = LUserID;

				using (DataSession LDataSession = new DataSession())
				{
					LDataSession.Alias = LConfiguration.Aliases[LConfiguration.DefaultAliasName];
					LDataSession.SessionInfo.Environment = "WindowsClient";
					LDataSession.Active = true;
					
					if (LApplicationID == String.Empty)
					{
						using (DAE.Client.DataView LView = new DAE.Client.DataView())
						{
							LView.Session = LDataSession;
							LView.Expression = CApplicationListExpression;
							LView.OrderString = CApplicationListOrder;
							LView.IsReadOnly = true;
							LView.Open();
							
							// Count the number of applications
							System.Collections.IEnumerator LEnum = LView.GetEnumerator();	 // use explicit enumerator to avoid foreach unused var warning
							int LCount = 0;
							while (LEnum.MoveNext())
								LCount++;

							// Prompt the user for the application if there is not exactly one row
							if (LCount != 1)
								ApplicationListForm.Execute(LView);

							LApplicationID = LView.Fields["ID"].AsString;
						}
					}

					Session LSession = new Session(LDataSession, false); // Pass false because the DataSession will be disposed by the using block
					LSession.Start(LSession.SetApplication(LApplicationID)); // This call will dispose the session.
				}
			}
			catch (AbortException)
			{
				// Do nothing (ignore abort)
			}
			catch (Exception LException)
			{
				Windows.Session.HandleException(LException);
			}
		}

		protected static void ThreadException(object ASender, ThreadExceptionEventArgs AArgs)
		{
			Windows.Session.HandleException(AArgs.Exception);
		}
	}
}