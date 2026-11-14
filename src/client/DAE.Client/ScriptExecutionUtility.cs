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
using System.Text;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Compiling;

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
		public static void ExecuteScript(IServerSession session, string script, ScriptExecuteOption options, out ErrorList errors, out TimeSpan timeElapsed, ReportScriptProgressHandler reportScriptProgress, DebugLocator locator)
		{
			IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
			try
			{
				ExecuteScript(process, script, options, out errors, out timeElapsed, reportScriptProgress, locator);
			}
			finally
			{
				session.StopProcess(process);
			}
		}

		public static void ExecuteScript(IServerProcess process, string script, ScriptExecuteOption options, out ErrorList errors, out TimeSpan timeElapsed, ReportScriptProgressHandler reportScriptProgress, DebugLocator locator)
		{
			StringBuilder result = new StringBuilder();
			errors = new ErrorList();
			timeElapsed = TimeSpan.Zero;

			bool attemptExecute = true;
			DateTime startTime = DateTime.Now;
			try
			{
				IServerScript localScript = process.PrepareScript(script, locator);
				try
				{
					if (ConvertParserErrors(localScript.Messages, errors))
					{
						foreach (IServerBatch batch in localScript.Batches)
						{
							PlanStatistics statistics = null;
							try
							{
								if (batch.IsExpression())
								{
									IServerExpressionPlan plan = batch.PrepareExpression(null);
									try
									{
										attemptExecute &= ConvertCompilerErrors(plan.Messages, errors);
										if (attemptExecute)
										{
											int rowCount = ReadResult(result, plan);

											AppendStatistics(result, options, plan.PlanStatistics, plan.ProgramStatistics, rowCount);
											statistics = plan.PlanStatistics;
										}
									}
									finally
									{
										batch.UnprepareExpression(plan);
									}
								}
								else
								{
									IServerStatementPlan plan = batch.PrepareStatement(null);
									try
									{
										attemptExecute &= ConvertCompilerErrors(plan.Messages, errors);
										if (attemptExecute)
										{
											plan.Execute(null);

											AppendStatistics(result, options, plan.PlanStatistics, plan.ProgramStatistics, -1);
											statistics = plan.PlanStatistics;
										}
									}
									finally
									{
										batch.UnprepareStatement(plan);
									}
								}
							}
							finally
							{
								if (reportScriptProgress != null)
									reportScriptProgress(statistics, result.ToString());
								result.Length = 0;
							}
						}	// foreach batch...
					}	// if (no parser errors)...
				}
				finally
				{
					process.UnprepareScript(localScript);
				}
			}
			catch (Exception exception)
			{
				errors.Add(exception);
			}
			timeElapsed = DateTime.Now - startTime;
		}

		private static int ReadResult(StringBuilder LResult, IServerExpressionPlan LPlan)
		{
			int rowCount;
			if (LPlan.DataType is DAE.Schema.ITableType)
			{
				IServerCursor cursor = LPlan.Open(null);
				try
				{
					rowCount = ResultsFromCursor(cursor, LResult);
					LResult.Append("\r\n");
				}
				finally
				{
					LPlan.Close(cursor);
				}
			}
			else
			{
				IDataValue value = LPlan.Evaluate(null);
				rowCount = -1; // row count not applicable
				if ((value == null) || value.IsNil)
					LResult.Append(Strings.Get("NoValue"));
				else
					if (LPlan.DataType is DAE.Schema.IRowType)
						ResultsFromRow((IRow)value, LResult);
					else if (LPlan.DataType is DAE.Schema.IListType)
						ResultsFromList((ListValue)value, LResult);
					else if (LPlan.DataType is DAE.Schema.IScalarType)
						LResult.Append(((IScalar)value).AsDisplayString);
					else
						LResult.Append(String.Format("<Unknown Result Type: {0}>", LPlan.DataType.Name));
				LResult.Append("\r\n");
			}
			return rowCount;
		}

		private static void AppendStatistics(StringBuilder result, ScriptExecuteOption options, PlanStatistics statistics, ProgramStatistics programStatistics, int rowCount)
		{
			if ((options & ScriptExecuteOption.ShowSuccessStatus) != 0)
			{
				result.Append(Strings.Get("ExecuteSuccess"));
				if (rowCount != -1)
				{
					if (rowCount == 1)
						result.Append(Strings.Get("ExecuteSuccessRowCountSingular"));
					else
						result.AppendFormat(Strings.Get("ExecuteSuccessRowCountPlural"), rowCount.ToString("#,##0"));
				}
				result.Append("\r\n");
			}
			if ((options & ScriptExecuteOption.ShowPrepareTime) != 0)
			{
				result.AppendFormat(Strings.Get("PrepareTime"), statistics.PrepareTime.ToString(), statistics.CompileTime.ToString(), statistics.OptimizeTime.ToString(), statistics.BindingTime.ToString());
				result.Append("\r\n");
			}
			if ((options & ScriptExecuteOption.ShowExecutionTime) != 0)
			{
				result.AppendFormat(Strings.Get("ExecuteTime"), programStatistics.ExecuteTime.ToString(), programStatistics.DeviceExecuteTime.ToString(), programStatistics.ExecuteTime == TimeSpan.Zero ? "N/A" : String.Format("{0}%", ((Convert.ToDecimal(programStatistics.DeviceExecuteTime.Ticks) / programStatistics.ExecuteTime.Ticks) * 100).ToString("F6")));
				result.Append("\r\n");
			}
		}

		/// <returns> True if no errors.  Continue processing... </returns>
		public static bool ConvertParserErrors(DAE.Language.ParserMessages messages, ErrorList errors)
		{
			bool error = false;
			foreach (Exception exception in messages)
			{
				errors.Add(exception);
				error = true;
			}
			return !error;
		}

		/// <returns> True if no errors.  Continue processing... </returns>
		public static bool ConvertCompilerErrors(CompilerMessages messages, ErrorList errors)
		{
			bool anyErrors = false;
			CompilerException compilerException;
			foreach (Exception exception in messages)
			{
				compilerException = exception as CompilerException;
				anyErrors = anyErrors || (compilerException == null) || (compilerException.ErrorLevel != CompilerErrorLevel.Warning);
				errors.Add(exception);
			}
			return !anyErrors;
		}
		
		public static bool ContainsError(ErrorList errors)
		{
			CompilerException compilerException;
			foreach (Exception exception in errors)
			{
				compilerException = exception as CompilerException;
				if ((compilerException == null) || (compilerException.ErrorLevel != CompilerErrorLevel.Warning))
					return true;
			}
			return false;
		}

		#region Results generation

		private static ResultColumn[] BuildResultColumns(DAE.Schema.IRowType rowType)
		{
			ResultColumn[] result = new ResultColumn[rowType.Columns.Count];
			for (int i = 0; i < rowType.Columns.Count; i++)
				result[i] = new ResultColumn(rowType.Columns[i].Name);
			return result;
		}

		private static void ReadRow(DAE.Runtime.Data.IRow row, ResultColumn[] LTarget)
		{
			for (int columnIndex = 0; columnIndex < row.DataType.Columns.Count; columnIndex++)
			{
				if (!row.HasValue(columnIndex))
					LTarget[columnIndex].Add(Strings.Get("NoValue"));
				else		
				{
					string value;
					try
					{
						value = row.GetValue(columnIndex).ToString();
					}
					catch (Exception exception)
					{
						value = "<error retrieving value: " + exception.Message.Replace("\r\n", "↵") + ">";
					}
					
					LTarget[columnIndex].Add(value);
				}
			}
		}

		private static int BuildResults(ResultColumn[] columns, StringBuilder results)
		{
			int rowCount = (columns.Length == 0 ? 0 : columns[0].Rows.Count);

			// Write the column header
			foreach (ResultColumn column in columns)
			{
				results.Append(((string)column.Rows[0]).PadRight(column.MaxLength).Substring(0, column.MaxLength));
				results.Append(' ');
			}
			results.Append("\r\n");

			// Write the line
			foreach (ResultColumn column in columns)
			{
				results.Append(String.Empty.PadRight(column.MaxLength, '-'));
				results.Append(' ');
			}
			results.Append("\r\n");

			// Write the rows
			for (int rowIndex = 1; rowIndex < rowCount; rowIndex++)
			{
				foreach (ResultColumn column in columns)
				{
					results.Append(((string)column.Rows[rowIndex]).PadRight(column.MaxLength).Substring(0, column.MaxLength));
					results.Append(' ');
				}
				results.Append("\r\n");
			}
			return (rowCount > 1 ? rowCount-1 : 0);
		}

		public static int ResultsFromCursor(IServerCursor cursor, StringBuilder results)
		{
			DAE.Schema.IRowType rowType = ((DAE.Schema.TableType)cursor.Plan.DataType).RowType;

			ResultColumn[] resultColumns = BuildResultColumns(rowType);

			int rowCount;
			try
			{
				using (DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(cursor.Plan.Process.ValueManager, rowType))
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						ReadRow(row, resultColumns);
					}
				}
			}
			finally
			{
				// Even if something fails (or aborts) build what we can
				rowCount = BuildResults(resultColumns, results);
			}
			return rowCount;
		}

		private static void ResultsFromRow(IRow row, StringBuilder results)
		{
			ResultColumn[] resultColumns = BuildResultColumns(row.DataType);
			ReadRow(row, resultColumns);
			BuildResults(resultColumns, results);
		}
		
		private static void ResultsFromList(ListValue list, StringBuilder results)
		{
			ResultColumn[] resultColumns = new ResultColumn[] { new ResultColumn("Elements") };
			for (int index = 0; index < list.Count(); index++)
				resultColumns[0].Add(list.GetValue(index).ToString());
			BuildResults(resultColumns, results);
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