/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Drawing;

namespace Alphora.Dataphor.Frontend.Client
{
	public sealed class TitleUtility
	{
		public static string RemoveAccellerators(string ASource)
		{
			System.Text.StringBuilder LResult = new System.Text.StringBuilder(ASource.Length);
			for (int i = 0; i < ASource.Length; i++)
			{
				if (ASource[i] == '&') 
				{
					if ((i < (ASource.Length - 1)) && (ASource[i + 1] == '&'))
						i++;
					else
						continue;
				}
				LResult.Append(ASource[i]);
			}
			return LResult.ToString();
		}
	}
	
	public sealed class SequenceColumnUtility
	{
		public static void SequenceChange(Client.Session ASession, ISource ASource, bool AShouldEnlist, DAE.Runtime.Data.Row AFromRow, DAE.Runtime.Data.Row AToRow, bool AAbove, string AScript)
		{
			if (!String.IsNullOrEmpty(AScript) && ASource != null)
			{
				Guid LEnlistWithATID = Guid.Empty;

				if (AShouldEnlist && ASource.DataView.Active && ASource.DataView.ApplicationTransactionServer != null)
					LEnlistWithATID = ASource.DataView.ApplicationTransactionServer.ApplicationTransactionID;

				DAE.IServerProcess LProcess = ASession.DataSession.ServerSession.StartProcess(new DAE.ProcessInfo(ASession.DataSession.ServerSession.SessionInfo));
				try
				{
					if (LEnlistWithATID != Guid.Empty)
						LProcess.JoinApplicationTransaction(LEnlistWithATID, false);

					// Prepare arguments
					DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
					foreach (DAE.Schema.Column LColumn in AFromRow.DataType.Columns)
					{
						LParams.Add(new DAE.Runtime.DataParam("AFromRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AFromRow[LColumn.Name]));
						LParams.Add(new DAE.Runtime.DataParam("AToRow." + LColumn.Name, LColumn.DataType, DAE.Language.Modifier.In, AToRow[LColumn.Name]));
					}
					LParams.Add(new DAE.Runtime.DataParam("AAbove", ASource.DataView.Process.DataTypes.SystemBoolean, DAE.Language.Modifier.In, AAbove));

					ASession.ExecuteScript(LProcess, AScript, LParams);
				}
				finally
				{
					ASession.DataSession.ServerSession.StopProcess(LProcess);
				}

				ASource.DataView.Refresh();
			}
		}

	}
}
