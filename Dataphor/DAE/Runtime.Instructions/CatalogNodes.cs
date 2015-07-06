/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Schema;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator ObjectExists(const AName : Name) : Boolean
	// operator ObjectExists(const ASpecifier : String) : Boolean
	public class ObjectExistsNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				if (Operator.Operands[0].DataType.Is(program.DataTypes.SystemString))
					return program.ResolveCatalogObjectSpecifier((string)argument1, false) != null;
				else
					return program.ResolveCatalogIdentifier((string)argument1, false) != null;
		}
	}
	
	// operator System.NameFromGuid(const AID : System.Guid) : System.Name
	public class SystemNameFromGuidNode : UnaryInstructionNode
	{
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return Schema.Object.NameFromGuid((Guid)argument1);
		}
	}
	
    public class SystemNameSelectorNode : UnaryInstructionNode
    {
		public static void CheckValidName(string tempValue)
		{
			if (!Parser.IsValidQualifiedIdentifier(tempValue))
				throw new ParserException(ParserException.Codes.InvalidIdentifier, tempValue);
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
			{
				string argument = (string)argument1;
				CheckValidName(argument);
				return argument;
			}
		}
    }
    
    public class SystemNameReadAccessorNode : UnaryInstructionNode
    {
		public SystemNameReadAccessorNode() : base()
		{
			IsOrderPreserving = true;
		}
		
		public override object InternalExecute(Program program, object argument1)
		{
			#if NILPROPOGATION
			if (argument1 == null)
				return null;
			else
			#endif
				return argument1;
		}
		
		public override void DetermineCharacteristics(Plan plan)
		{
			base.DetermineCharacteristics(plan);
			IsOrderPreserving = true;
		}
    }
    
    public class SystemNameWriteAccessorNode : BinaryInstructionNode
    {
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument2 == null)
				return null;
			else
			#endif
			{
				string argument = (string)argument2;
				SystemNameSelectorNode.CheckValidName(argument);
				return argument;
			}
		}
    }

	public class RowColumnsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("DataType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Sequence"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var sequence = 1;

					foreach (Schema.Column column in ((Schema.IRowType)Nodes[0].DataType).Columns)
					{
						row[0] = sequence;
						sequence++;

						row[1] = -1; //column.ID;
						row[2] = -1; //column.ParentObjectID;
						row[3] = -1; //column.CatalogObjectID;
						row[4] = column.Name;
						row[5] = column.Name; //column.DisplayName;
						row[6] = column.Name; //column.Description;
						row[7] = column.DataType.Name;
						row[8] = false; //column.IsGenerated;
						row[9] = false; //column.IsSystem;
						row[10] = true; //column.IsRemotable;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class TableColumnsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("DataType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsNilable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsDefaultRemotable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsValidateRemotable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsChangeRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Sequence"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var sequence = 1;

					foreach (Schema.TableVarColumn column in ((TableNode)Nodes[0]).TableVar.Columns)
					{
						row[0] = sequence;
						sequence++;

						row[1] = column.ID;
						row[2] = column.ParentObjectID;
						row[3] = column.CatalogObjectID;
						row[4] = column.Name;
						row[5] = column.DisplayName;
						row[6] = column.Description;
						row[7] = column.DataType.Name;
						row[8] = column.IsNilable;
						row[9] = column.IsGenerated;
						row[10] = column.IsSystem;
						row[11] = column.IsRemotable;
						row[12] = column.IsDefaultRemotable;
						row[13] = column.IsValidateRemotable;
						row[14] = column.IsChangeRemotable;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ColumnDefaultsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Expression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.TableVarColumn column in ((TableNode)Nodes[0]).TableVar.Columns)
					{
						if (column.Default != null)
						{
							row[0] = column.Default.ID;
							row[1] = column.Default.ParentObjectID;
							row[2] = column.Default.CatalogObjectID;
							row[3] = column.Default.Name;
							row[4] = column.Default.DisplayName;
							row[5] = column.Default.Description;
							row[6] = column.Default.Node.SafeEmitStatementAsString(true);
							row[7] = column.Default.IsGenerated;
							row[8] = column.Default.IsSystem;
							row[9] = column.Default.IsRemotable;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ColumnConstraintsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("ConstraintType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Expression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Enforced", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsDeferred", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.TableVarColumn column in ((TableNode)Nodes[0]).TableVar.Columns)
					{
						foreach (Schema.TableVarColumnConstraint constraint in column.Constraints)
						{
							row[0] = constraint.ID;
							row[1] = constraint.ParentObjectID;
							row[2] = constraint.CatalogObjectID;
							row[3] = constraint.Name;
							row[4] = constraint.DisplayName;
							row[5] = constraint.Description;
							row[6] = constraint.ConstraintType.ToString();
							row[7] = constraint.Node.SafeEmitStatementAsString(true);
							row[8] = constraint.Enforced;
							row[9] = constraint.IsDeferred;
							row[10] = constraint.IsGenerated;
							row[11] = constraint.IsSystem;
							row[12] = constraint.IsRemotable;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ColumnEventHandlersNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("EventType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Operator", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsDeferred", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("ShouldTranslate", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.TableVarColumn column in ((TableNode)Nodes[0]).TableVar.Columns)
					{
						foreach (Schema.TableVarColumnEventHandler eventHandler in column.EventHandlers)
						{
							row[0] = eventHandler.ID;
							row[1] = eventHandler.ParentObjectID;
							row[2] = eventHandler.CatalogObjectID;
							row[3] = eventHandler.Name;
							row[4] = eventHandler.DisplayName;
							row[5] = eventHandler.Description;
							row[6] = eventHandler.EventType.ToString();
							row[7] = eventHandler.Operator.Name;
							row[8] = eventHandler.IsDeferred;
							row[9] = eventHandler.ShouldTranslate;
							row[10] = eventHandler.IsGenerated;
							row[11] = eventHandler.IsSystem;
							row[12] = eventHandler.IsRemotable;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class KeysNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Enforced", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsInherited", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsNilable", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSparse", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.Key key in ((TableNode)Nodes[0]).TableVar.Keys)
					{
						row[0] = key.ID;
						row[1] = key.ParentObjectID;
						row[2] = key.CatalogObjectID;
						row[3] = key.Name;
						row[4] = key.DisplayName;
						row[5] = key.Description;
						row[6] = key.Enforced;
						row[7] = key.IsInherited;
						row[8] = key.IsNilable;
						row[9] = key.IsSparse;
						row[10] = key.IsGenerated;
						row[11] = key.IsSystem;
						row[12] = key.IsRemotable;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class KeyColumnsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Key_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Column_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Column_Name", plan.DataTypes.SystemName));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Key_ID"], TableVar.Columns["Column_ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.Key key in ((TableNode)Nodes[0]).TableVar.Keys)
					{
						foreach (Schema.TableVarColumn column in key.Columns)
						{
							row[0] = key.ID;
							row[1] = column.ID;
							row[2] = column.Name;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class OrdersNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsClustered", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsInherited", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.Order order in ((TableNode)Nodes[0]).TableVar.Orders)
					{
						row[0] = order.ID;
						row[1] = order.ParentObjectID;
						row[2] = order.CatalogObjectID;
						row[3] = order.Name;
						row[4] = order.DisplayName;
						row[5] = order.Description;
						row[6] = (order == ((TableNode)Nodes[0]).Order);
						row[7] = order.IsInherited;
						row[8] = order.IsGenerated;
						row[9] = order.IsSystem;
						row[10] = order.IsRemotable;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class OrderColumnsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Order_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Column_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Column_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Ascending", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IncludeNils", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsDefaultSort", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Sort", plan.DataTypes.SystemString));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Order_ID"], TableVar.Columns["Sequence"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.Order order in ((TableNode)Nodes[0]).TableVar.Orders)
					{
						var sequence = 1;
						foreach (Schema.OrderColumn column in order.Columns)
						{
							row[0] = order.ID;
							row[1] = sequence;
							sequence++;

							row[2] = column.Column.ID;
							row[3] = column.Column.Name;
							row[4] = column.Ascending;
							row[5] = column.IncludeNils;
							row[6] = column.IsDefaultSort;
							row[7] = column.IsDefaultSort ? null : column.Sort.CompareNode.SafeEmitStatementAsString(true);
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ConstraintsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("ConstraintType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsTransition", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("Expression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("InsertExpression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("UpdateExpression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("DeleteExpression", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Enforced", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsDeferred", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					TableVar tableVar = ((TableNode)Nodes[0]).TableVar;
					if (tableVar.HasConstraints())
					{
						foreach (Schema.TableVarConstraint constraint in tableVar.Constraints)
						{
							row[0] = constraint.ID;
							row[1] = constraint.ParentObjectID;
							row[2] = constraint.CatalogObjectID;
							row[3] = constraint.Name;
							row[4] = constraint.DisplayName;
							row[5] = constraint.Description;
							row[6] = constraint.ConstraintType.ToString();

							var rowConstraint = constraint as Schema.RowConstraint;
							var transitionConstraint = constraint as Schema.TransitionConstraint;

							if (rowConstraint != null)
							{
								row[7] = false;
								row[8] = rowConstraint.Node.SafeEmitStatementAsString(true);
							}

							if (transitionConstraint != null)
							{
								row[7] = true;
								row[9] = transitionConstraint.OnInsertNode == null ? null : transitionConstraint.OnInsertNode.SafeEmitStatementAsString(true);
								row[10] = transitionConstraint.OnUpdateNode == null ? null : transitionConstraint.OnUpdateNode.SafeEmitStatementAsString(true);
								row[11] = transitionConstraint.OnDeleteNode == null ? null : transitionConstraint.OnDeleteNode.SafeEmitStatementAsString(true);
							}

							row[12] = constraint.Enforced;
							row[13] = constraint.IsDeferred;
							row[14] = constraint.IsGenerated;
							row[15] = constraint.IsSystem;
							row[16] = constraint.IsRemotable;
							result.Insert(row);
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class EventHandlersNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("EventType", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Operator", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsDeferred", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("ShouldTranslate", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					foreach (Schema.TableVarEventHandler eventHandler in ((TableNode)Nodes[0]).TableVar.EventHandlers)
					{
						row[0] = eventHandler.ID;
						row[1] = eventHandler.ParentObjectID;
						row[2] = eventHandler.CatalogObjectID;
						row[3] = eventHandler.Name;
						row[4] = eventHandler.DisplayName;
						row[5] = eventHandler.Description;
						row[6] = eventHandler.EventType.ToString();
						row[7] = eventHandler.Operator.Name;
						row[8] = eventHandler.IsDeferred;
						row[9] = eventHandler.ShouldTranslate;
						row[10] = eventHandler.IsGenerated;
						row[11] = eventHandler.IsSystem;
						row[12] = eventHandler.IsRemotable;
						result.Insert(row);
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ReferencesNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("ParentObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("CatalogObjectID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("DisplayName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Description", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("SourceTableName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("TargetTableName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("Enforced", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsDerived", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSourceReference", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("ParentReferenceName", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsExcluded", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("UpdateReferenceAction", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("DeleteReferenceAction", plan.DataTypes.SystemString));
			DataType.Columns.Add(new Schema.Column("IsGenerated", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsSystem", plan.DataTypes.SystemBoolean));
			DataType.Columns.Add(new Schema.Column("IsRemotable", plan.DataTypes.SystemBoolean));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["ID"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var tableVar = ((TableNode)Nodes[0]).TableVar;
					if (tableVar.HasReferences())
					{
						foreach (Schema.ReferenceBase referenceBase in tableVar.References)
						{
							if (referenceBase.SourceTable.Equals(tableVar))
							{
								Schema.Reference reference = referenceBase as Schema.Reference;
								Schema.DerivedReference derivedReference = referenceBase as Schema.DerivedReference;
								row[0] = referenceBase.ID;
								row[1] = referenceBase.ParentObjectID;
								row[2] = referenceBase.CatalogObjectID;
								row[3] = referenceBase.Name;
								row[4] = referenceBase.DisplayName;
								row[5] = referenceBase.Description;
								row[6] = referenceBase.SourceTable.Name;
								row[7] = referenceBase.TargetTable.Name;
								row[8] = referenceBase.Enforced;
								row[9] = referenceBase.IsDerived;
								row[10] = true;
								row[11] = derivedReference == null ? null : derivedReference.ParentReference.Name;
								row[12] = derivedReference == null ? false : derivedReference.IsExcluded;
								row[13] = reference == null ? null : reference.UpdateReferenceAction.ToString();
								row[14] = reference == null ? null : reference.DeleteReferenceAction.ToString();
								row[15] = referenceBase.IsGenerated;
								row[16] = referenceBase.IsSystem;
								row[17] = referenceBase.IsRemotable;
								result.Insert(row);
							}
							else if (referenceBase.TargetTable.Equals(tableVar))
							{
								Schema.Reference reference = referenceBase as Schema.Reference;
								Schema.DerivedReference derivedReference = referenceBase as Schema.DerivedReference;
								row[0] = referenceBase.ID;
								row[1] = referenceBase.ParentObjectID;
								row[2] = referenceBase.CatalogObjectID;
								row[3] = referenceBase.Name;
								row[4] = referenceBase.DisplayName;
								row[5] = referenceBase.Description;
								row[6] = referenceBase.SourceTable.Name;
								row[7] = referenceBase.TargetTable.Name;
								row[8] = referenceBase.Enforced;
								row[9] = referenceBase.IsDerived;
								row[10] = false;
								row[11] = derivedReference == null ? null : derivedReference.ParentReference.Name;
								row[12] = derivedReference == null ? false : derivedReference.IsExcluded;
								row[13] = reference == null ? null : reference.UpdateReferenceAction.ToString();
								row[14] = reference == null ? null : reference.DeleteReferenceAction.ToString();
								row[15] = referenceBase.IsGenerated;
								row[16] = referenceBase.IsSystem;
								row[17] = referenceBase.IsRemotable;
								result.Insert(row);
							}
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}

	public class ReferenceColumnsNode : TableNode
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;

			DataType.Columns.Add(new Schema.Column("Reference_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Sequence", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Source_Column_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Source_Column_Name", plan.DataTypes.SystemName));
			DataType.Columns.Add(new Schema.Column("Target_Column_ID", plan.DataTypes.SystemInteger));
			DataType.Columns.Add(new Schema.Column("Target_Column_Name", plan.DataTypes.SystemName));

			foreach (Schema.Column column in DataType.Columns)
				TableVar.Columns.Add(new Schema.TableVarColumn(column));
				
			TableVar.Keys.Add(new Schema.Key(new Schema.TableVarColumn[] { TableVar.Columns["Reference_ID"], TableVar.Columns["Sequence"] }));

			TableVar.DetermineRemotable(plan.CatalogDeviceSession);
			Order = Compiler.FindClusteringOrder(plan, TableVar);
			
			// Ensure the order exists in the orders list
			if (!TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
		}
		
		public override object InternalExecute(Program program)
		{
			LocalTable result = new LocalTable(this, program);
			try
			{
				result.Open();

				// Populate the result
				Row row = new Row(program.ValueManager, result.DataType.RowType);
				try
				{
					row.ValuesOwned = false;

					var tableVar = ((TableNode)Nodes[0]).TableVar;
					if (tableVar.HasReferences())
					{
						foreach (Schema.ReferenceBase reference in tableVar.References)
						{
							if (reference.SourceTable.Equals(tableVar))
							{
								for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
								{
									row[0] = reference.ID;
									row[1] = index;
									row[2] = reference.SourceKey.Columns[index].ID;
									row[3] = reference.SourceKey.Columns[index].Name;
									row[4] = reference.TargetKey.Columns[index].ID;
									row[5] = reference.TargetKey.Columns[index].Name;
								
									result.Insert(row);
								}
							}
							else if (reference.TargetTable.Equals(tableVar))
							{
								for (int index = 0; index < reference.SourceKey.Columns.Count; index++)
								{
									row[0] = reference.ID;
									row[1] = index;
									row[2] = reference.SourceKey.Columns[index].ID;
									row[3] = reference.SourceKey.Columns[index].Name;
									row[4] = reference.TargetKey.Columns[index].ID;
									row[5] = reference.TargetKey.Columns[index].Name;

									result.Insert(row);
								}
							}
						}
					}
				}
				finally
				{
					row.Dispose();
				}
				
				result.First();
				
				return result;
			}
			catch
			{
				result.Dispose();
				throw;
			}
		}
	}
}
