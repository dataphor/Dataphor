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
		public static void ExecuteScript(string script, IServer server, SessionInfo sessionInfo) 
		{
			IServerSession session = server.Connect(sessionInfo);
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(sessionInfo));
				try
				{
					IServerScript localScript = process.PrepareScript(script);
					try
					{
						localScript.Execute(null);
					}
					finally
					{
						process.UnprepareScript(localScript);
					}
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				server.Disconnect(session);
			}
		}
	}

}
