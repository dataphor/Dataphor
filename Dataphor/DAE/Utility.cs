/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE
{
	/// <nodoc/>
	/// <summary> Various DAE static utilitary functions. </summary>
	public sealed class DAEUtility 
	{
		/// <summary> Executes a D4 script. </summary>
		public static void ExecuteScript(string AScript, IServer AServer, SessionInfo ASessionInfo) 
		{
			IServerSession LSession = AServer.Connect(ASessionInfo);
			try
			{
				IServerProcess LProcess = LSession.StartProcess(new ProcessInfo(ASessionInfo));
				try
				{
					IServerScript LScript = LProcess.PrepareScript(AScript);
					try
					{
						LScript.Execute(null);
					}
					finally
					{
						LProcess.UnprepareScript(LScript);
					}
				}
				finally
				{
					LSession.StopProcess(LProcess);
				}
			}
			finally
			{
				AServer.Disconnect(LSession);
			}
		}
	}

}
