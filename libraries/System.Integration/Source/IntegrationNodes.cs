/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;

namespace Alphora.Dataphor.Libraries.System.Integration
{
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator Insert(ASource : table, ATarget : table) : String
	public class InsertNode : InstructionNodeBase
	{
		public InsertNode()
		{
			// Prevent arguments from being converted to values
			ExpectsTableValues = false;
		}

		public override object InternalExecute(Program program)
		{
			return
				IntegrationHelper.Copy
				(
					program,
					(TableNode)Nodes[0],
					(TableNode)Nodes[1],
					new GenerateStatementHandler(IntegrationHelper.GenerateInsertStatement)
				);
		}
	}

	// operator Copy(ASourceExpression : String, ATargetExpression : String, AKeyColumnNames : String, AInsertOnly : Boolean) : String
	public class CopyNode : InstructionNodeBase
	{
		public override object InternalExecute(Program program)
		{
			string AKeyColumnNames = Nodes.Count > 2 ? (string)Nodes[2].Execute(program) : null;
			bool AInsertOnly = Nodes.Count > 3 ? (bool)Nodes[3].Execute(program) : false;
			
			IServerExpressionPlan ASourcePlan = ((IServerProcess)program.ServerProcess).PrepareExpression((string)Nodes[0].Execute(program), null);
			try
			{
				IServerExpressionPlan ATargetPlan = ((IServerProcess)program.ServerProcess).PrepareExpression((string)Nodes[1].Execute(program), null);
				try
				{
					TableNode ASourceTable = ((CursorNode)((ServerPlan)ASourcePlan).Program.Code).SourceNode;
					TableNode ATargetTable = ((CursorNode)((ServerPlan)ATargetPlan).Program.Code).SourceNode;

					return CopyTablesNode.InternalCopy(program, _dataType, ASourceTable, ATargetTable, AKeyColumnNames, AInsertOnly);
				}
				finally
				{
					((IServerProcess)program.ServerProcess).UnprepareExpression(ATargetPlan);
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareExpression(ASourcePlan);
			}
		}
	}

	// operator Copy(ASource : table, ATarget : table, AKeyColumnNames : String, AInsertOnly : Boolean) : String
	// operator Copy(ASource : table, ATarget : table, AKeyColumnNames : String) : String
	// operator Copy(ASource : table, ATarget : table) : String
	public class CopyTablesNode : InstructionNodeBase
	{
		public CopyTablesNode()
		{
			// Prevent arguments from being converted to values
			ExpectsTableValues = false;
		}

		public override object InternalExecute(Program program)
		{
			string AKeyColumnNames = Nodes.Count > 2 ? (string)Nodes[2].Execute(program) : null;
			bool AInsertOnly = Nodes.Count > 3 ? (bool)Nodes[3].Execute(program) : false;

			TableNode ASourceTable = (TableNode)Nodes[0];
			TableNode ATargetTable = (TableNode)Nodes[1];

			return InternalCopy(program, _dataType, ASourceTable, ATargetTable, AKeyColumnNames, AInsertOnly);
		}

		internal static object InternalCopy(Program program, Schema.IDataType dataType, TableNode sourceTable, TableNode targetTable, string keyColumnNames, bool insertOnly)
		{
			return
				IntegrationHelper.Copy
				(
					program,
					sourceTable,
					targetTable,
					delegate(TableNode ASourceTableInner, TableNode ATargetTableInner, Expression ATargetExpression)
					{
						return
							IntegrationHelper.GenerateConditionedInsertStatement
							(
								program,
								ASourceTableInner,
								ATargetTableInner,
								ATargetExpression,
								insertOnly,
								keyColumnNames
							);
					}
				);
		}
	}

	public sealed class IntegrationHelper
	{
		public static string Copy(Program program, TableNode sourceTable, TableNode targetTable, GenerateStatementHandler generateStatement)
		{
			Statement targetStatement =
				generateStatement
				(
					sourceTable,
					targetTable,
					(Expression)targetTable.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy)
				);
			Schema.Key displaySourceKey = program.FindClusteringKey(sourceTable.TableVar);

			StringBuilder result = new StringBuilder();
			long succeededUpdateCount = 0;
			long failedUpdateCount = 0;
			bool maxResultLengthMessageWritten = false;

			// Start a new process so that we don't mess with the transaction context of this one
			ProcessInfo info = new ProcessInfo(program.ServerProcess.ServerSession.SessionInfo);
			info.UseImplicitTransactions = false;
			IServerProcess targetProcess = ((IServerSession)program.ServerProcess.ServerSession).StartProcess(info);
			try
			{
				Program targetProgram = new Program((ServerProcess)targetProcess);
				targetProgram.Code = targetTable;
				targetProgram.Start(null);
				try
				{
					// Have the target program use the main program's context
					Stack oldTargetContext = targetProgram.SwitchContext(program.Stack);
					try
					{
						info.DefaultIsolationLevel = IsolationLevel.Browse;
						IServerProcess sourceProcess = ((IServerSession)program.ServerProcess.ServerSession).StartProcess(info);
						try
						{
							Program sourceProgram = new Program((ServerProcess)sourceProcess);
							sourceProgram.Code = sourceTable;
							sourceProgram.Start(null);
							try
							{
								// Have the source program use the main program's context
								Stack oldSourceContext = sourceProgram.SwitchContext(program.Stack);
								try
								{
									ITable source = (ITable)sourceTable.Execute(sourceProgram);
									try
									{
										source.Open();

										// TODO: IBAS Project #26790 - allow cross-process row copies for streamed types
										// There is a MarshalRow call in the LocalProcess, would that solve this problem?
										using (Row row = new Row(targetProcess.ValueManager, sourceTable.DataType.CreateRowType()))
										{
											DataParams paramsValue = new DataParams();
											paramsValue.Add(new DataParam("ASourceRow", row.DataType, DAE.Language.Modifier.Const, row));

											IServerStatementPlan target = targetProcess.PrepareStatement(new D4TextEmitter().Emit(targetStatement), paramsValue);
											try
											{
												target.CheckCompiled();

												while (source.Next())
												{
													row.ClearValues();
													targetProcess.BeginTransaction(IsolationLevel.Isolated);
													try
													{
														source.Select(row);
														target.Execute(paramsValue);
														targetProcess.CommitTransaction();
														succeededUpdateCount++;
													}
													catch (Exception exception)
													{
														failedUpdateCount++;
														targetProcess.RollbackTransaction();
														if (result.Length < 100000)
															result.Append(KeyValuesToString(displaySourceKey, row) + " - " + exception.Message + "\r\n");
														else
														{
															if (!maxResultLengthMessageWritten)
															{
																result.Append(Strings.Get("MaxResultLengthExceeded"));
																maxResultLengthMessageWritten = true;
															}
														}
													}

													// Yield in case our process is aborted.
													program.CheckAborted();
												}
											}
											finally
											{
												targetProcess.UnprepareStatement(target);
											}
										}
									}
									finally
									{
										source.Close();
									}
								}
								finally
								{
									sourceProgram.SwitchContext(oldSourceContext);	// Don't let the source program cleanup the main context
								}
							}
							finally
							{
								sourceProgram.Stop(null);
							}
						}
						finally
						{
							((IServerSession)program.ServerProcess.ServerSession).StopProcess(sourceProcess);
						}

						result.AppendFormat(Strings.Get("Results"), succeededUpdateCount, failedUpdateCount);
						return result.ToString();
					}
					finally
					{
						targetProgram.SwitchContext(oldTargetContext);	// Don't let the target program cleanup the main context
					}
				}
				finally
				{
					targetProgram.Stop(null);
				}
			}
			finally
			{
				((IServerSession)program.ServerProcess.ServerSession).StopProcess(targetProcess);
			}
		}

		public static Statement GenerateInsertStatement(TableNode sourceTable, TableNode targetTable, Expression targetExpression)
		{
			return new InsertStatement(new IdentifierExpression("ASourceRow"), targetExpression);
		}

		public static Statement GenerateConditionedInsertStatement(Program program, TableNode sourceTable, TableNode targetTable, Expression targetExpression, bool insertOnly, string keyColumns)
		{
			// Produced statement looks similar to this:
			//	if not exists (<target> rename Target where <key column restriction>) then
			//		insert ASourceRow into <target>
			//	else
			//		update <target> rename Target set { <assignment list> } where <key column restriction>;

			string[] keyColumnNames = null;
			if (keyColumns == null)
			{
				// If no key columns are specified, default to a key from the target
				Schema.Key key = program.FindClusteringKey(targetTable.TableVar);
				keyColumnNames = new string[key.Columns.Count];
				for (int i = 0; i < key.Columns.Count; i++)
					keyColumnNames[i] = key.Columns[i].Name;
			}
			else
			{
				// Parse and trim the key column names
				keyColumnNames = (keyColumns == "" ? new String[] { } : keyColumns.Split(new char[] { ',', ';' }));
				for (int i = 0; i < keyColumnNames.Length; i++)
					keyColumnNames[i] = keyColumnNames[i].Trim();
			}

			// Compute the target restriction
			Expression targetCondition = null;
			foreach (string keyColumnName in keyColumnNames)
			{
				BinaryExpression comparison = new BinaryExpression(new IdentifierExpression(".Target." + keyColumnName), Instructions.Equal, new IdentifierExpression(".ASourceRow." + keyColumnName));
				if (targetCondition != null)
					targetCondition = new BinaryExpression(targetCondition, Instructions.And, comparison);
				else
					targetCondition = comparison;
			}

			// Construct the if ... then insert ... portion
			IfStatement ifStatement =
				new IfStatement
				(
					new UnaryExpression
					(
						Instructions.Not,
						new UnaryExpression
						(
							Instructions.Exists,
							(targetCondition != null)
								? new RestrictExpression(new RenameAllExpression(targetExpression, "Target"), targetCondition)
								: targetExpression
						)
					),
					GenerateInsertStatement(sourceTable, targetTable, targetExpression),
					null
				);

			// If also doing updates, construct the else update ... portion
			if (!insertOnly)
			{
				UpdateStatement update = new UpdateStatement(new RenameAllExpression(targetExpression, "Target"));

				// Compute the assignments based on the target's columns
				foreach (Schema.Column column in targetTable.DataType.Columns)
					// If not a key column and it is in the source then add it as a restriction column
					if ((Array.IndexOf(keyColumnNames, column.Name) < 0) && sourceTable.DataType.Columns.ContainsName(column.Name))
						update.Columns.Add
						(
							new UpdateColumnExpression
							(
								new IdentifierExpression(".Target." + column.Name),
								new IdentifierExpression(".ASourceRow." + column.Name)
							)
						);

				if (update.Columns.Count > 0)
				{
					update.Condition = targetCondition;
					ifStatement.FalseStatement = update;
				}
			}

			return ifStatement;
		}

		private static string KeyValuesToString(Schema.Key key, Row row)
		{
			StringBuilder result = new StringBuilder();
			foreach (Schema.TableVarColumn column in key.Columns)
			{
				if (result.Length > 0)
					result.Append(", ");
				result.Append(column.Column.Name + " := " + (row.HasValue(column.Column.Name) ? (string)row[column.Column.Name] : "<nil>"));
			}
			return "{ " + result.ToString() + " }";
		}
	}
	
	public delegate Statement GenerateStatementHandler(TableNode ASourceTable, TableNode ATargetTable, Expression ATargetExpression);
}
