/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.Libraries.System.Integration
{
	// operator Insert(ASource : table, ATarget : table) : String
	public class InsertNode : InstructionNodeBase
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			return
				IntegrationHelper.Copy
				(
					AProcess,
					(TableNode)Nodes[0],
					(TableNode)Nodes[1],
					new GenerateStatementHandler(IntegrationHelper.GenerateInsertStatement)
				);
		}
	}

	// operator Copy(ASourceExpression : String, ATargetExpression : String, AKeyColumnNames : String, AInsertOnly : Boolean) : String
	public class CopyNode : InstructionNodeBase
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			string AKeyColumnNames = Nodes.Count > 2 ? (string)Nodes[2].Execute(AProcess) : null;
			bool AInsertOnly = Nodes.Count > 3 ? (bool)Nodes[3].Execute(AProcess) : false;
			
			IServerExpressionPlan ASourcePlan = ((IServerProcess)AProcess).PrepareExpression((string)Nodes[0].Execute(AProcess), null);
			try
			{
				IServerExpressionPlan ATargetPlan = ((IServerProcess)AProcess).PrepareExpression((string)Nodes[1].Execute(AProcess), null);
				try
				{
					TableNode ASourceTable = ((CursorNode)((ServerPlan)ASourcePlan).Code).SourceNode;
					TableNode ATargetTable = ((CursorNode)((ServerPlan)ATargetPlan).Code).SourceNode;

					return CopyTablesNode.InternalCopy(AProcess, FDataType, ASourceTable, ATargetTable, AKeyColumnNames, AInsertOnly);
				}
				finally
				{
					((IServerProcess)AProcess).UnprepareExpression(ATargetPlan);
				}
			}
			finally
			{
				((IServerProcess)AProcess).UnprepareExpression(ASourcePlan);
			}
		}
	}

	// operator Copy(ASource : table, ATarget : table, AKeyColumnNames : String, AInsertOnly : Boolean) : String
	// operator Copy(ASource : table, ATarget : table, AKeyColumnNames : String) : String
	// operator Copy(ASource : table, ATarget : table) : String
	public class CopyTablesNode : InstructionNodeBase
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			string AKeyColumnNames = Nodes.Count > 2 ? (string)Nodes[2].Execute(AProcess) : null;
			bool AInsertOnly = Nodes.Count > 3 ? (bool)Nodes[3].Execute(AProcess) : false;

			TableNode ASourceTable = (TableNode)Nodes[0];
			TableNode ATargetTable = (TableNode)Nodes[1];

			return InternalCopy(AProcess, FDataType, ASourceTable, ATargetTable, AKeyColumnNames, AInsertOnly);
		}

		internal static object InternalCopy(ServerProcess AProcess, Schema.IDataType ADataType, TableNode ASourceTable, TableNode ATargetTable, string AKeyColumnNames, bool AInsertOnly)
		{
			return
				IntegrationHelper.Copy
				(
					AProcess,
					ASourceTable,
					ATargetTable,
					delegate(TableNode ASourceTableInner, TableNode ATargetTableInner, Expression ATargetExpression)
					{
						return
							IntegrationHelper.GenerateConditionedInsertStatement
							(
								ASourceTableInner,
								ATargetTableInner,
								ATargetExpression,
								AInsertOnly,
								AKeyColumnNames
							);
					}
				);
		}
	}

	public sealed class IntegrationHelper
	{
		public static string Copy(ServerProcess AProcess, TableNode ASourceTable, TableNode ATargetTable, GenerateStatementHandler AGenerateStatement)
		{
			Statement LTargetStatement =
				AGenerateStatement
				(
					ASourceTable,
					ATargetTable,
					(Expression)ATargetTable.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy)
				);
			Schema.Key LDisplaySourceKey = ASourceTable.TableVar.FindClusteringKey();

			StringBuilder LResult = new StringBuilder();
			long LSucceededUpdateCount = 0;
			long LFailedUpdateCount = 0;
			bool LMaxResultLengthMessageWritten = false;

			// Start a new process so that we don't mess with the transaction context of this one
			ProcessInfo LInfo = new ProcessInfo(AProcess.ServerSession.SessionInfo);
			LInfo.UseImplicitTransactions = false;
			IServerProcess LTargetProcess = ((IServerSession)AProcess.ServerSession).StartProcess(LInfo);
			try
			{
				// Have the target process use the main process' context
				Context LOldTargetContext = ((ServerProcess)LTargetProcess).SwitchContext(AProcess.Context);
				try
				{
					LInfo.DefaultIsolationLevel = IsolationLevel.Browse;
					IServerProcess LSourceProcess = ((IServerSession)AProcess.ServerSession).StartProcess(LInfo);
					try
					{
						// Have the source process use the main process' context
						Context LOldSourceContext = ((ServerProcess)LSourceProcess).SwitchContext(AProcess.Context);
						try
						{
							Table LSource = (Table)ASourceTable.Execute((ServerProcess)LSourceProcess);
							try
							{
								LSource.Open();

								// TODO: IBAS Project #26790 - allow cross-process row copies for streamed types
								// There is a MarshalRow call in the LocalProcess, would that solve this problem?
								using (Row LRow = new Row(LTargetProcess, ASourceTable.DataType.CreateRowType()))
								{
									DataParams LParams = new DataParams();
									LParams.Add(new DataParam("ASourceRow", LRow.DataType, DAE.Language.Modifier.Const, LRow));

									IServerStatementPlan LTarget = LTargetProcess.PrepareStatement(new D4TextEmitter().Emit(LTargetStatement), LParams);
									try
									{
										LTarget.CheckCompiled();

										while (LSource.Next())
										{
											LRow.ClearValues();
											LTargetProcess.BeginTransaction(IsolationLevel.Isolated);
											try
											{
												LSource.Select(LRow);
												LTarget.Execute(LParams);
												LTargetProcess.CommitTransaction();
												LSucceededUpdateCount++;
											}
											catch (Exception LException)
											{
												LFailedUpdateCount++;
												LTargetProcess.RollbackTransaction();
												if (LResult.Length < 100000)
													LResult.Append(KeyValuesToString(LDisplaySourceKey, LRow) + " - " + LException.Message + "\r\n");
												else
												{
													if (!LMaxResultLengthMessageWritten)
													{
														LResult.Append(Strings.Get("MaxResultLengthExceeded"));
														LMaxResultLengthMessageWritten = true;
													}
												}
											}

											// Yield in case our process is aborted.
											AProcess.CheckAborted();
										}
									}
									finally
									{
										LTargetProcess.UnprepareStatement(LTarget);
									}
								}
							}
							finally
							{
								LSource.Close();
							}
						}
						finally
						{
							((ServerProcess)LSourceProcess).SwitchContext(LOldSourceContext);	// Don't let the source process cleanup the main context
						}
					}
					finally
					{
						((IServerSession)AProcess.ServerSession).StopProcess(LSourceProcess);
					}

					LResult.AppendFormat(Strings.Get("Results"), LSucceededUpdateCount, LFailedUpdateCount);
					return LResult.ToString();
				}
				finally
				{
					((ServerProcess)LTargetProcess).SwitchContext(LOldTargetContext);	// Don't let the target process cleanup the main context
				}
			}
			finally
			{
				((IServerSession)AProcess.ServerSession).StopProcess(LTargetProcess);
			}
		}

		public static Statement GenerateInsertStatement(TableNode ASourceTable, TableNode ATargetTable, Expression ATargetExpression)
		{
			return new InsertStatement(new IdentifierExpression("ASourceRow"), ATargetExpression);
		}

		public static Statement GenerateConditionedInsertStatement(TableNode ASourceTable, TableNode ATargetTable, Expression ATargetExpression, bool AInsertOnly, string AKeyColumns)
		{
			// Produced statement looks similar to this:
			//	if not exists (<target> rename Target where <key column restriction>) then
			//		insert ASourceRow into <target>
			//	else
			//		update <target> rename Target set { <assignment list> } where <key column restriction>;

			string[] LKeyColumnNames = null;
			if (AKeyColumns == null)
			{
				// If no key columns are specified, default to a key from the target
				Schema.Key LKey = ATargetTable.TableVar.FindClusteringKey();
				LKeyColumnNames = new string[LKey.Columns.Count];
				for (int i = 0; i < LKey.Columns.Count; i++)
					LKeyColumnNames[i] = LKey.Columns[i].Name;
			}
			else
			{
				// Parse and trim the key column names
				LKeyColumnNames = (AKeyColumns == "" ? new String[] { } : AKeyColumns.Split(new char[] { ',', ';' }));
				for (int i = 0; i < LKeyColumnNames.Length; i++)
					LKeyColumnNames[i] = LKeyColumnNames[i].Trim();
			}

			// Compute the target restriction
			Expression LTargetCondition = null;
			foreach (string LKeyColumnName in LKeyColumnNames)
			{
				BinaryExpression LComparison = new BinaryExpression(new IdentifierExpression(".Target." + LKeyColumnName), Instructions.Equal, new IdentifierExpression(".ASourceRow." + LKeyColumnName));
				if (LTargetCondition != null)
					LTargetCondition = new BinaryExpression(LTargetCondition, Instructions.And, LComparison);
				else
					LTargetCondition = LComparison;
			}

			// Construct the if ... then insert ... portion
			IfStatement LIfStatement =
				new IfStatement
				(
					new UnaryExpression
					(
						Instructions.Not,
						new UnaryExpression
						(
							Instructions.Exists,
							(LTargetCondition != null)
								? new RestrictExpression(new RenameAllExpression(ATargetExpression, "Target"), LTargetCondition)
								: ATargetExpression
						)
					),
					GenerateInsertStatement(ASourceTable, ATargetTable, ATargetExpression),
					null
				);

			// If also doing updates, construct the else update ... portion
			if (!AInsertOnly)
			{
				UpdateStatement LUpdate = new UpdateStatement(new RenameAllExpression(ATargetExpression, "Target"));

				// Compute the assignments based on the target's columns
				foreach (Schema.Column LColumn in ATargetTable.DataType.Columns)
					// If not a key column and it is in the source then add it as a restriction column
					if ((Array.IndexOf(LKeyColumnNames, LColumn.Name) < 0) && ASourceTable.DataType.Columns.ContainsName(LColumn.Name))
						LUpdate.Columns.Add
						(
							new UpdateColumnExpression
							(
								new IdentifierExpression(".Target." + LColumn.Name),
								new IdentifierExpression(".ASourceRow." + LColumn.Name)
							)
						);

				if (LUpdate.Columns.Count > 0)
				{
					LUpdate.Condition = LTargetCondition;
					LIfStatement.FalseStatement = LUpdate;
				}
			}

			return LIfStatement;
		}

		private static string KeyValuesToString(Schema.Key AKey, Row ARow)
		{
			StringBuilder LResult = new StringBuilder();
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				if (LResult.Length > 0)
					LResult.Append(", ");
				LResult.Append(LColumn.Column.Name + " := " + (ARow.HasValue(LColumn.Column.Name) ? (string)ARow[LColumn.Column.Name] : "<nil>"));
			}
			return "{ " + LResult.ToString() + " }";
		}
	}
	
	public delegate Statement GenerateStatementHandler(TableNode ASourceTable, TableNode ATargetTable, Expression ATargetExpression);
}
