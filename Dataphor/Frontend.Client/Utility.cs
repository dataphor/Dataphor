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
		public static string RemoveAccellerators(string source)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder(source.Length);
			for (int i = 0; i < source.Length; i++)
			{
				if (source[i] == '&') 
				{
					if ((i < (source.Length - 1)) && (source[i + 1] == '&'))
						i++;
					else
						continue;
				}
				result.Append(source[i]);
			}
			return result.ToString();
		}
	}
	
	public sealed class SequenceColumnUtility
	{
		public static void SequenceChange(Client.Session session, ISource source, bool shouldEnlist, DAE.Runtime.Data.IRow fromRow, DAE.Runtime.Data.IRow toRow, bool above, string script)
		{
			if (!String.IsNullOrEmpty(script) && source != null)
			{
				Guid enlistWithATID = Guid.Empty;

				if (shouldEnlist && source.DataView.Active && source.DataView.ApplicationTransactionServer != null)
					enlistWithATID = source.DataView.ApplicationTransactionServer.ApplicationTransactionID;

				DAE.IServerProcess process = session.DataSession.ServerSession.StartProcess(new DAE.ProcessInfo(session.DataSession.ServerSession.SessionInfo));
				try
				{
					if (enlistWithATID != Guid.Empty)
						process.JoinApplicationTransaction(enlistWithATID, false);

					// Prepare arguments
					DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
					foreach (DAE.Schema.Column column in fromRow.DataType.Columns)
					{
						paramsValue.Add(new DAE.Runtime.DataParam("AFromRow." + column.Name, column.DataType, DAE.Language.Modifier.In, fromRow[column.Name]));
						paramsValue.Add(new DAE.Runtime.DataParam("AToRow." + column.Name, column.DataType, DAE.Language.Modifier.In, toRow[column.Name]));
					}
					paramsValue.Add(new DAE.Runtime.DataParam("AAbove", source.DataView.Process.DataTypes.SystemBoolean, DAE.Language.Modifier.In, above));

					session.ExecuteScript(process, script, paramsValue);
				}
				finally
				{
					session.DataSession.ServerSession.StopProcess(process);
				}

				source.DataView.Refresh();
			}
		}

	}
}
