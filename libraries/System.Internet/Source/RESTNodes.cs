/*
	Dataphor
	© Copyright 2000-2015 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Libraries.System.Internet
{
	public static class RESTUtilities
	{
		public static Schema.TableVar ResolveTableVarName(Plan plan, string tableVarName)
		{
			var tableVar = Compiler.ResolveCatalogIdentifier(plan, tableVarName, true) as Schema.TableVar;
			if (tableVar == null)
			{
				throw new CompilerException(CompilerException.Codes.TableVarIdentifierExpected);
			}

			return tableVar;
		}

		public static Schema.TableVar ResolveTableVarName(Program program, string tableVarName)
		{
			return ResolveTableVarName(program.Plan, tableVarName);
		}

		public static Schema.Key FindIdentifyingKey(Program program, Schema.TableVar tableVar)
		{
			foreach (var key in tableVar.Keys)
				if (Convert.ToBoolean(MetaData.GetTag(key.MetaData, "REST.IsIdentity", "false")))
					return key;

			return program.FindClusteringKey(tableVar);
		}

		public static string[] SplitKeyValues(string keyValue)
		{
			// TODO: Handle escaping of $ character...
			return keyValue.Split('$');
		}

		public static Row GetKeyValues(Program program, Schema.Key key, string keyValue)
		{
			var row = new Row(program.ValueManager, new Schema.RowType(key.Columns));
			try
			{
				var keyValues = SplitKeyValues(keyValue);
				for (var i = 0; i < keyValues.Length; i++)
				{
					var keyScalar = new Scalar(program.ValueManager, key.Columns[i].DataType as Schema.IScalarType, null);
					try
					{
						keyScalar.AsString = keyValues[i];
						row[i] = keyScalar;
					}
					finally
					{
						keyScalar.Dispose();
					}
				}

				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
		}

		public static object Evaluate(Program program, string expression, Schema.TableVar tableVar, DataParams dataParams)
		{
			IServerExpressionPlan plan = ((IServerProcess)program.ServerProcess).PrepareExpression(expression, dataParams);
			try
			{
				plan.CheckCompiled();

				IServerCursor cursor = plan.Open(dataParams);
				try
				{
					NativeTable nativeTable = new NativeTable(program.ValueManager, tableVar);
					while (cursor.Next())
					{
						using (IRow row = cursor.Select())
						{	
							nativeTable.Insert(program.ValueManager, row);
						}
					}
					return new TableValue(program.ValueManager, nativeTable.TableType, nativeTable);
				}
				finally
				{
					plan.Close(cursor);
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareExpression(plan);
			}
		}

		public static object EvaluateSingleton(Program program, string expression, DataParams dataParams)
		{
			IServerExpressionPlan plan = ((IServerProcess)program.ServerProcess).PrepareExpression(expression, dataParams);
			try
			{
				plan.CheckCompiled();
				IServerCursor cursor = plan.Open(dataParams);
				try
				{
					if (cursor.Next())
					{
						return cursor.Select();
					}
					else
					{
						return null;
					}
				}
				finally
				{
					plan.Close(cursor);
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareExpression(plan);
			}
		}

		public static void Execute(Program program, string statement, DataParams dataParams)
		{
			IServerStatementPlan plan = ((IServerProcess)program.ServerProcess).PrepareStatement(statement, dataParams);
			try
			{
				plan.CheckCompiled();
				plan.Execute(dataParams);
			}
			finally
			{
				((IServerProcess)program.ServerProcess).UnprepareStatement(plan);
			}
		}
	}

	// operator GetByKey(const ATableName : Name, const AKeyValue : String) : row
	public class GetByKeyNode : InstructionNode
	{
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);

			if (!Nodes[0].IsLiteral)
				throw new CompilerException(CompilerException.Codes.LiteralArgumentRequired, "ATableName");

			var tableVarName = (string)plan.ExecuteNode(Nodes[0]);

			var tableVar = RESTUtilities.ResolveTableVarName(plan, tableVarName);
			DataType = tableVar.DataType.CreateRowType();
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);
			var identifyingKey = RESTUtilities.FindIdentifyingKey(program, tableVar);
			var keyValues = RESTUtilities.GetKeyValues(program, identifyingKey, (string)arguments[1]);
			var keyEqualExpression = Compiler.BuildKeyEqualExpression(program.Plan, String.Empty, "AKey", new Schema.RowType(identifyingKey.Columns).Columns, keyValues.DataType.Columns);
			var tableVarExpression = new IdentifierExpression(tableVar.Name);
			var restrictExpression = new RestrictExpression(tableVarExpression, keyEqualExpression);
			var expression = new D4TextEmitter().Emit(restrictExpression);
			var dataParams = new DataParams();
			dataParams.Add(new DataParam("AKey", keyValues.DataType, Modifier.In, keyValues));

			return RESTUtilities.EvaluateSingleton(program, expression, dataParams);
		}
	}

	// operator Get(const ATableName : Name) : table
	public class GetNode : InstructionNode
	{
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);

			if (!Nodes[0].IsLiteral)
				throw new CompilerException(CompilerException.Codes.LiteralArgumentRequired, "ATableName");

			var tableVarName = (string)plan.ExecuteNode(Nodes[0]);

			var tableVar = RESTUtilities.ResolveTableVarName(plan, tableVarName);

			DataType = tableVar.DataType;
		}

		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);
			var expression = String.Format("select {0}", tableVar.Name);

			return RESTUtilities.Evaluate(program, expression, tableVar, null);
		}
	}

	// operator Post(const ATableName : Name, const ARow : row)
	public class PostNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);

			var insertStatement = new InsertStatement(new IdentifierExpression("ARow"), new IdentifierExpression(tableVar.Name));
			var statement = new D4TextEmitter().Emit(insertStatement);
			var dataParams = new DataParams();
			var row = (IRow)arguments[1];
			dataParams.Add(new DataParam("ARow", row.DataType, Modifier.In, row));

			RESTUtilities.Execute(program, statement, dataParams);

			return null;
		}
	}

	// operator Put(const ATableName : Name, const AKey : String, const ARow : row)
	public class PutNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);
			var identifyingKey = RESTUtilities.FindIdentifyingKey(program, tableVar);
			var keyValues = RESTUtilities.GetKeyValues(program, identifyingKey, (string)arguments[1]);
			var keyEqualExpression = Compiler.BuildKeyEqualExpression(program.Plan, String.Empty, "AKey", new Schema.RowType(identifyingKey.Columns).Columns, keyValues.DataType.Columns);

			var updateStatement = new UpdateStatement();
			updateStatement.Target = new IdentifierExpression(tableVar.Name);

			var row = (IRow)arguments[2];
			for (int i = 0; i < row.DataType.Columns.Count; i++)
				updateStatement.Columns.Add(new UpdateColumnExpression(new IdentifierExpression(row.DataType.Columns[i].Name), new IdentifierExpression(Schema.Object.Qualify(row.DataType.Columns[i].Name, "ARow"))));

			updateStatement.Condition = keyEqualExpression;

			var statement = new D4TextEmitter().Emit(updateStatement);
			var dataParams = new DataParams();
			dataParams.Add(new DataParam("AKey", keyValues.DataType, Modifier.In, keyValues));
			dataParams.Add(new DataParam("ARow", row.DataType, Modifier.In, row));

			RESTUtilities.Execute(program, statement, dataParams);

			return null;
		}
	}

	// operator Patch(const ATableName : Name, const AKey : String, const ARow : row)
	public class PatchNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);
			var identifyingKey = RESTUtilities.FindIdentifyingKey(program, tableVar);
			var keyValues = RESTUtilities.GetKeyValues(program, identifyingKey, (string)arguments[1]);
			var keyEqualExpression = Compiler.BuildKeyEqualExpression(program.Plan, String.Empty, "AKey", new Schema.RowType(identifyingKey.Columns).Columns, keyValues.DataType.Columns);

			var updateStatement = new UpdateStatement();
			updateStatement.Target = new IdentifierExpression(tableVar.Name);

			var row = (IRow)arguments[2];
			for (int i = 0; i < row.DataType.Columns.Count; i++)
				updateStatement.Columns.Add(new UpdateColumnExpression(new IdentifierExpression(row.DataType.Columns[i].Name), new IdentifierExpression(Schema.Object.Qualify(row.DataType.Columns[i].Name, "ARow"))));

			updateStatement.Condition = keyEqualExpression;

			var statement = new D4TextEmitter().Emit(updateStatement);
			var dataParams = new DataParams();
			dataParams.Add(new DataParam("AKey", keyValues.DataType, Modifier.In, keyValues));
			dataParams.Add(new DataParam("ARow", row.DataType, Modifier.In, row));

			RESTUtilities.Execute(program, statement, dataParams);

			return null;
		}
	}

	// operator Delete(const ATableName : Name, const AKey : row)
	public class DeleteNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			var tableVar = RESTUtilities.ResolveTableVarName(program, (string)arguments[0]);
			var identifyingKey = RESTUtilities.FindIdentifyingKey(program, tableVar);
			var keyValues = RESTUtilities.GetKeyValues(program, identifyingKey, (string)arguments[1]);
			var keyEqualExpression = Compiler.BuildKeyEqualExpression(program.Plan, String.Empty, "AKey", new Schema.RowType(identifyingKey.Columns).Columns, keyValues.DataType.Columns);

			var deleteStatement = new DeleteStatement(new RestrictExpression(new IdentifierExpression(tableVar.Name), keyEqualExpression));
			var statement = new D4TextEmitter().Emit(deleteStatement);
			var dataParams = new DataParams();
			dataParams.Add(new DataParam("AKey", keyValues.DataType, Modifier.In, keyValues));

			RESTUtilities.Execute(program, statement, dataParams);

			return null;
		}
	}
}
