/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Data;
using System.Text;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Client
{
	[Flags]
	public enum ScriptExecuteOption
	{
		None = 0,
		ShowPrepareTime = 1,
		ShowExecutionTime = 2,
		ShowSuccessStatus = 4,
		All = ShowPrepareTime | ShowExecutionTime | ShowSuccessStatus
	}

	public static class ScriptExecutionUtility
	{
		public static void ExecuteScript(IServerSession ASession, string AScript, ScriptExecuteOption AOptions, out ErrorList AErrors, out TimeSpan ATimeElapsed, ReportScriptProgressHandler AReportScriptProgress)
		{
			IServerProcess LProcess = ASession.StartProcess(new ProcessInfo(ASession.SessionInfo));
			try
			{
				ExecuteScript(LProcess, AScript, AOptions, out AErrors, out ATimeElapsed, AReportScriptProgress);
			}
			finally
			{
				ASession.StopProcess(LProcess);
			}
		}

		public static void ExecuteScript(IServerProcess AProcess, string AScript, ScriptExecuteOption AOptions, out ErrorList AErrors, out TimeSpan ATimeElapsed, ReportScriptProgressHandler AReportScriptProgress)
		{
			StringBuilder LResult = new StringBuilder();
			AErrors = new ErrorList();
			ATimeElapsed = TimeSpan.Zero;

			bool LAttemptExecute = true;
			DateTime LStartTime = DateTime.Now;
			try
			{
				IServerScript LScript = AProcess.PrepareScript(AScript);
				try
				{
					if (ConvertParserErrors(LScript.Messages, AErrors))
					{
						foreach (IServerBatch LBatch in LScript.Batches)
						{
							PlanStatistics LStatistics = null;
							try
							{
								if (LBatch.IsExpression())
								{
									IServerExpressionPlan LPlan = LBatch.PrepareExpression(null);
									try
									{
										LAttemptExecute &= ConvertCompilerErrors(LPlan.Messages, AErrors);
										if (LAttemptExecute)
										{
											int LRowCount = ReadResult(LResult, LPlan);

											AppendStatistics(LResult, AOptions, LPlan.Statistics, LRowCount);
											LStatistics = LPlan.Statistics;
										}
									}
									finally
									{
										LBatch.UnprepareExpression(LPlan);
									}
								}
								else
								{
									IServerStatementPlan LPlan = LBatch.PrepareStatement(null);
									try
									{
										LAttemptExecute &= ConvertCompilerErrors(LPlan.Messages, AErrors);
										if (LAttemptExecute)
										{
											LPlan.Execute(null);

											AppendStatistics(LResult, AOptions, LPlan.Statistics, -1);
											LStatistics = LPlan.Statistics;
										}
									}
									finally
									{
										LBatch.UnprepareStatement(LPlan);
									}
								}
							}
							finally
							{
								if (AReportScriptProgress != null)
									AReportScriptProgress(LStatistics, LResult.ToString());
								LResult.Length = 0;
							}
						}	// foreach batch...
					}	// if (no parser errors)...
				}
				finally
				{
					AProcess.UnprepareScript(LScript);
				}
			}
			catch (Exception LException)
			{
				AErrors.Add(LException);
			}
			ATimeElapsed = DateTime.Now - LStartTime;
		}

		private static int ReadResult(StringBuilder LResult, IServerExpressionPlan LPlan)
		{
			int LRowCount;
			if (LPlan.DataType is DAE.Schema.ITableType)
			{
				IServerCursor LCursor = LPlan.Open(null);
				try
				{
					LRowCount = ResultsFromCursor(LCursor, LResult);
					LResult.Append("\r\n");
				}
				finally
				{
					LPlan.Close(LCursor);
				}
			}
			else
			{
				DataValue LValue = LPlan.Evaluate(null);
				LRowCount = -1; // row count not applicable
				if ((LValue == null) || LValue.IsNil)
					LResult.Append(Strings.Get("NoValue"));
				else
					if (LPlan.DataType is DAE.Schema.IRowType)
						ResultsFromRow((Row)LValue, LResult);
					else if (LPlan.DataType is DAE.Schema.IListType)
						ResultsFromList((ListValue)LValue, LResult);
					else if (LPlan.DataType is DAE.Schema.IScalarType)
						LResult.Append(((Scalar)LValue).AsDisplayString);
					else
						LResult.Append(String.Format("<Unknown Result Type: {0}>", LPlan.DataType.Name));
				LResult.Append("\r\n");
			}
			return LRowCount;
		}

		private static void AppendStatistics(StringBuilder AResult, ScriptExecuteOption AOptions, PlanStatistics AStatistics, int ARowCount)
		{
			if ((AOptions & ScriptExecuteOption.ShowSuccessStatus) != 0)
			{
				AResult.Append(Strings.Get("ExecuteSuccess"));
				if (ARowCount != -1)
				{
					if (ARowCount == 1)
						AResult.Append(Strings.Get("ExecuteSuccessRowCountSingular"));
					else
						AResult.AppendFormat(Strings.Get("ExecuteSuccessRowCountPlural"), ARowCount.ToString("#,##0"));
				}
				AResult.Append("\r\n");
			}
			if ((AOptions & ScriptExecuteOption.ShowPrepareTime) != 0)
			{
				AResult.AppendFormat(Strings.Get("PrepareTime"), AStatistics.PrepareTime.ToString(), AStatistics.CompileTime.ToString(), AStatistics.OptimizeTime.ToString(), AStatistics.BindingTime.ToString());
				AResult.Append("\r\n");
			}
			if ((AOptions & ScriptExecuteOption.ShowExecutionTime) != 0)
			{
				AResult.AppendFormat(Strings.Get("ExecuteTime"), AStatistics.ExecuteTime.ToString(), AStatistics.DeviceExecuteTime.ToString(), AStatistics.ExecuteTime == TimeSpan.Zero ? "N/A" : String.Format("{0}%", ((Convert.ToDecimal(AStatistics.DeviceExecuteTime.Ticks) / AStatistics.ExecuteTime.Ticks) * 100).ToString("F6")));
				AResult.Append("\r\n");
			}
		}

		/// <returns> True if no errors.  Continue processing... </returns>
		public static bool ConvertParserErrors(DAE.Language.ParserMessages AMessages, ErrorList AErrors)
		{
			bool LError = false;
			foreach (Exception LException in AMessages)
			{
				AErrors.Add(LException);
				LError = true;
			}
			return !LError;
		}

		/// <returns> True if no errors.  Continue processing... </returns>
		public static bool ConvertCompilerErrors(DAE.Language.D4.CompilerMessages AMessages, ErrorList AErrors)
		{
			bool LAnyErrors = false;
			DAE.Language.D4.CompilerException LCompilerException;
			foreach (Exception LException in AMessages)
			{
				LCompilerException = LException as DAE.Language.D4.CompilerException;
				LAnyErrors = LAnyErrors || (LCompilerException == null) || (LCompilerException.ErrorLevel != DAE.Language.D4.CompilerErrorLevel.Warning);
				AErrors.Add(LException);
			}
			return !LAnyErrors;
		}
		
		public static bool ContainsError(ErrorList AErrors)
		{
			Alphora.Dataphor.DAE.Language.D4.CompilerException LCompilerException;
			foreach (Exception LException in AErrors)
			{
				LCompilerException = LException as Alphora.Dataphor.DAE.Language.D4.CompilerException;
				if ((LCompilerException == null) || (LCompilerException.ErrorLevel != Alphora.Dataphor.DAE.Language.D4.CompilerErrorLevel.Warning))
					return true;
			}
			return false;
		}

		#region Results generation

		private static ResultColumn[] BuildResultColumns(DAE.Schema.IRowType ARowType)
		{
			ResultColumn[] LResult = new ResultColumn[ARowType.Columns.Count];
			for (int i = 0; i < ARowType.Columns.Count; i++)
				LResult[i] = new ResultColumn(ARowType.Columns[i].Name);
			return LResult;
		}

		private static void ReadRow(DAE.Runtime.Data.Row ARow, ResultColumn[] LTarget)
		{
			for (int LColumnIndex = 0; LColumnIndex < ARow.DataType.Columns.Count; LColumnIndex++)
			{
				if (!ARow.HasValue(LColumnIndex))
					LTarget[LColumnIndex].Add(Strings.Get("NoValue"));
				else		
				{
					string LValue;
					try
					{
						LValue = ARow.GetValue(LColumnIndex).ToString();
					}
					catch (Exception LException)
					{
						LValue = "<error retrieving value: " + LException.Message.Replace("\r\n", "↵") + ">";
					}
					
					LTarget[LColumnIndex].Add(LValue);
				}
			}
		}

		private static int BuildResults(ResultColumn[] AColumns, StringBuilder AResults)
		{
			int LRowCount = (AColumns.Length == 0 ? 0 : AColumns[0].Rows.Count);

			// Write the column header
			foreach (ResultColumn LColumn in AColumns)
			{
				AResults.Append(((string)LColumn.Rows[0]).PadRight(LColumn.MaxLength).Substring(0, LColumn.MaxLength));
				AResults.Append(' ');
			}
			AResults.Append("\r\n");

			// Write the line
			foreach (ResultColumn LColumn in AColumns)
			{
				AResults.Append(String.Empty.PadRight(LColumn.MaxLength, '-'));
				AResults.Append(' ');
			}
			AResults.Append("\r\n");

			// Write the rows
			for (int LRowIndex = 1; LRowIndex < LRowCount; LRowIndex++)
			{
				foreach (ResultColumn LColumn in AColumns)
				{
					AResults.Append(((string)LColumn.Rows[LRowIndex]).PadRight(LColumn.MaxLength).Substring(0, LColumn.MaxLength));
					AResults.Append(' ');
				}
				AResults.Append("\r\n");
			}
			return (LRowCount > 1 ? LRowCount-1 : 0);
		}

		public static int ResultsFromCursor(IServerCursor ACursor, StringBuilder AResults)
		{
			DAE.Schema.IRowType LRowType = ((DAE.Schema.TableType)ACursor.Plan.DataType).RowType;

			ResultColumn[] LResultColumns = BuildResultColumns(LRowType);

			int LRowCount;
			try
			{
				using (DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(ACursor.Plan.Process, LRowType))
				{
					while (ACursor.Next())
					{
						ACursor.Select(LRow);
						ReadRow(LRow, LResultColumns);
					}
				}
			}
			finally
			{
				// Even if something fails (or aborts) build what we can
				LRowCount = BuildResults(LResultColumns, AResults);
			}
			return LRowCount;
		}

		private static void ResultsFromRow(Row ARow, StringBuilder AResults)
		{
			ResultColumn[] LResultColumns = BuildResultColumns(ARow.DataType);
			ReadRow(ARow, LResultColumns);
			BuildResults(LResultColumns, AResults);
		}
		
		private static void ResultsFromList(ListValue AList, StringBuilder AResults)
		{
			ResultColumn[] LResultColumns = new ResultColumn[] { new ResultColumn("Elements") };
			for (int LIndex = 0; LIndex < AList.Count(); LIndex++)
				LResultColumns[0].Add(AList.GetValue(LIndex).ToString());
			BuildResults(LResultColumns, AResults);
		}
		
		/// <summary> Used internally by the TextExpressionWriter to buffer string data which has been read. </summary>
		private struct ResultColumn
		{
			public ResultColumn(string AValue)
			{
				MaxLength = 0;
				Rows = new List<string>();
				Add(AValue);
			}

			public int MaxLength;
			public List<string> Rows;
			
			public void Add(string AValue)
			{
				if (AValue.Length > MaxLength)
					MaxLength = AValue.Length;
				Rows.Add(AValue.Replace("\r\n", "↵"));
			}
		}

		#endregion
	}

	public delegate void ReportScriptProgressHandler(PlanStatistics AStatistics, string AResults);
}