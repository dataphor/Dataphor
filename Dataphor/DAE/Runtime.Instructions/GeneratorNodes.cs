/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System; 
using System.Text;
using System.Collections;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator EnsureGenerators();
	// operator EnsureGenerators(ADeviceName : System.Name);
	// EnsureGenerators ensures that a generator table exists and is linked
	// A new one will be created with the name System.Generators if it does not already exist
	// if a device is not passed the default device will be used.
	public class SystemEnsureGeneratorsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			if (AProcess.ServerSession.Server.Catalog.Generators == null) // if not set already
			{
				// check for existing table named Datphor.Generators
				Schema.TableVar LTableVar = (Schema.TableVar)Compiler.ResolveCatalogIdentifier(AProcess.Plan, "System.Generators", false);

				if (LTableVar == null) // if system.generators doesn't already exist
				{
					// get device
					Schema.Device LDevice = 
						AArguments.Length > 0 
							? (Schema.Device)Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true)
							: Language.D4.Compiler.GetDefaultDevice(AProcess.Plan, true);

					// make sure the device is started so that DeviceScalarTypes is filled
					AProcess.EnsureDeviceStarted(LDevice);

					// create table type
					Schema.TableType LTableType = new Schema.TableType();
					// use System.String if available else System.IString
					if (LDevice.DeviceScalarTypes.Contains(AProcess.Plan.Catalog.DataTypes.SystemIString))
						LTableType.Columns.Add(new Schema.Column("ID", AProcess.Plan.Catalog.DataTypes.SystemIString));
					else
						LTableType.Columns.Add(new Schema.Column("ID", AProcess.Plan.Catalog.DataTypes.SystemString));
					LTableType.Columns.Add(new Schema.Column("NextKey", AProcess.Plan.Catalog.DataTypes.SystemInteger));
					
					// create table
					LTableVar = new Schema.BaseTableVar("System.Generators", LTableType, LDevice);
					MetaData LMetaData = new MetaData();
					LMetaData.Tags.Add(new Tag("Storage.Length", "200", false, true));
					LTableVar.Columns.Add(new Schema.TableVarColumn(LTableType.Columns["ID"], LMetaData));
					LTableVar.Columns.Add(new Schema.TableVarColumn(LTableType.Columns["NextKey"]));
					LTableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[]{LTableVar.Columns["ID"]}));
					LTableVar.Owner = AProcess.Plan.User;
					LTableVar.Library = AProcess.Plan.CurrentLibrary;
					LTableVar.AddDependency(LDevice);
					LTableVar.AddDependency((Schema.ScalarType)LTableVar.Columns[0].DataType);
					LTableVar.AddDependency((Schema.ScalarType)LTableVar.Columns[1].DataType);
					
					Compiler.Bind(AProcess.Plan, new CreateTableNode((Schema.BaseTableVar)LTableVar)).Execute(AProcess);

					if (AProcess.Plan.User.ID == Server.Server.CAdminUserID)
					{
						Schema.Group LGroup = AProcess.Plan.Catalog.Groups[Server.Server.CUserGroupName];
						LGroup.GrantRight(LTableVar.Rights.Select, true);
						LGroup.GrantRight(LTableVar.Rights.Insert, true);
						LGroup.GrantRight(LTableVar.Rights.Update, true);
					}
				}

				// set generator table
				SystemSetGeneratorsNode.SetGenerator(AProcess, LTableVar);
			}
			return null;
		}
	}
	
	// operator SetGenerators(ATableName : System.Name);
	// SetGenerators takes the name of a table that is of the type table { ID : string, NextKey : integer, key { ID } };
	public class SystemSetGeneratorsNode : InstructionNode
	{
		public static void SetGenerator(ServerProcess AProcess, Schema.TableVar ATableVar) 
		{
			if 
				(
					!(
						(ATableVar.DataType.Columns.Count == 2) && 
						(
							(ATableVar.DataType.Columns[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemString)) || 
							(ATableVar.DataType.Columns[0].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemIString)) 
						) &&
						(Schema.Object.NamesEqual(ATableVar.DataType.Columns[0].Name, "ID")) && 
						(ATableVar.DataType.Columns[1].DataType.Is(AProcess.Plan.Catalog.DataTypes.SystemInteger)) &&
						(Schema.Object.NamesEqual(ATableVar.DataType.Columns[1].Name, "NextKey"))
					)
				)
				throw new RuntimeException(RuntimeException.Codes.InvalidGeneratorsTable);
			AProcess.ServerSession.Server.Catalog.Generators = ATableVar;
		}

		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			SetGenerator
			(
				AProcess,
				(Schema.TableVar)Compiler.ResolveCatalogIdentifier(AProcess.Plan, AArguments[0].Value.AsString, true)
			);
			return null;
		}
	}
	
	// operator ClearGenerators()
	public class SystemClearGeneratorsNode : InstructionNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.ServerSession.Server.Catalog.Generators = null;
			return null;
		}
	}
	
	// operator GetNextGenerator(AID : String) : integer;
	// operator GetNextGenerator(AID : IString) : integer;
	public class SystemGetNextGeneratorNode : InstructionNode
	{
		private PlanNode FNode;
		
		public override void DetermineDataType(Plan APlan)
		{
			APlan.PushCursorContext(new CursorContext(CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.None));
			try
			{
				if (APlan.Catalog.Generators == null)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.NoGeneratorTable);
				FNode = Compiler.EmitBaseTableVarNode(APlan, APlan.Catalog.Generators);
				
				APlan.Symbols.PushWindow(0);
				try
				{
					// push a string or IString depending on what the id column is
					if (((Schema.Column)APlan.Catalog.Generators.DataType.Columns["ID"]).DataType == APlan.Catalog.DataTypes.SystemString)
						APlan.Symbols.Push(new DataVar("AID", APlan.Catalog.DataTypes.SystemString));
					else
						APlan.Symbols.Push(new DataVar("AID", APlan.Catalog.DataTypes.SystemIString));

					try
					{
						FNode = Compiler.EmitRestrictNode(APlan, FNode, new BinaryExpression(new IdentifierExpression("ID"), Language.D4.Instructions.Equal, new IdentifierExpression("AID")));
						FNode = Compiler.Bind(APlan, FNode);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.PopWindow();
				}
			}
			finally
			{
				APlan.PopCursorContext();
			}
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			AProcess.Context.Push(AArguments[0]);
			try
			{
				using (Table LTable = (Table)FNode.Execute(AProcess).Value)
				{
					int LValue = 0;
					if (LTable.Next())
					{
						Row LRow = LTable.Select();
						try
						{
							LValue = LRow[1].AsInt32;
							LRow[1] = new Scalar(AProcess, AProcess.DataTypes.SystemInteger, LValue + 1);
							LTable.Update(LRow);
						}
						finally
						{
							LRow.Dispose();
						}
					}
					else
					{
						LValue = 1;
						Row LRow = new Row(AProcess, LTable.DataType.CreateRowType());
						try
						{
							LRow[0] = (Scalar)AArguments[0].Value;
							LRow[1] = new Scalar(AProcess, AProcess.DataTypes.SystemInteger, LValue + 1);
							LTable.Insert(LRow);
						}
						finally
						{
							LRow.Dispose();
						}
					}
					return new DataVar(AProcess.Plan.Catalog.DataTypes.SystemInteger, new Scalar(AProcess, AProcess.DataTypes.SystemInteger, LValue));
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}
		}
	}
}