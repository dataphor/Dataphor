/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define NILPROPOGATION
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

	/*
		operator iIn(AValue : generic, AList : list) : boolean
		begin
			Result := false;
			for LInteger : integer := 0 to AList.Count() - 1 do
				if (AScalar = AList[LInteger])
				begin
					Result := true;
					break;
				end;
		end;
		
		operator iIn(AValue : generic, AList : list) : boolean
	*/
	public class ValueInListNode : BinaryInstructionNode
	{
		private PlanNode _equalNode;
		public PlanNode EqualNode
		{
			get { return _equalNode; }
			set { _equalNode = value; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			_equalNode = Compiler.EmitBinaryNode(plan, new StackReferenceNode(Nodes[0].DataType, 1, true), Instructions.Equal, new StackReferenceNode(((Schema.ListType)Nodes[1].DataType).ElementType, 0, true));
		}
		
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			ListValue list = (ListValue)argument2;
			#if NILPROPOGATION
			if ((list == null) || (argument1 == null))
				return null;
			#endif
			
			program.Stack.Push(argument1);
			try
			{
				program.Stack.Push(null);
				try
				{
					object result = false;
					for (int index = 0; index < list.Count(); index++)
					{
						program.Stack.Poke(0, list[index]);
						object tempValue = _equalNode.Execute(program);
						#if NILPROPOGATION
						if (tempValue == null)
						{
							result = null;
							continue;
						}
						#endif
						if ((bool)tempValue)
							return tempValue;
					}
					return result;
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				program.Stack.Pop();
			}
		}
	}
	
	// operator iIn(AScalar : scalar, ATable : table) : boolean;
	// operator iIn(ARow : row, ATable : table) : boolean;
	// operator iIn(AScalar : scalar, APresentation : presentation) : boolean;
	// operator iIn(AEntry : entry, APresentation : presentation) : boolean;
	public class InTableNode : BinaryInstructionNode
	{
		public InTableNode()
		{
			ExpectsTableValues = false;
		}

		private PlanNode _equalNode;
		public PlanNode EqualNode
		{
			get { return _equalNode; }
			set { _equalNode = value; }
		}
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			if (Nodes[0].DataType is Schema.ScalarType)
			{
				Schema.TableType tableType = (Schema.TableType)Nodes[1].DataType;
				if (tableType.Columns.Count != 1)
					throw new CompilerException(CompilerException.Codes.InvalidMembershipOperand, plan.CurrentStatement(), Nodes[0].DataType.Name, Nodes[1].DataType.Name);
				#if USECOLUMNLOCATIONBINDING
				FEqualNode = 
					Compiler.EmitBinaryNode
					(
						APlan, 
						new StackReferenceNode(Nodes[0].DataType, 1, true), 
						Instructions.Equal, 
						new StackColumnReferenceNode(tableType.Columns[0].DataType, 0, 0)
					);
				#else
				_equalNode = 
					Compiler.EmitBinaryNode
					(
						plan, 
						new StackReferenceNode(Nodes[0].DataType, 1, true), 
						Instructions.Equal, 
						new StackColumnReferenceNode(tableType.Columns[0].Name, tableType.Columns[0].DataType, 0)
					);
				#endif
			}
			else
				_equalNode = Compiler.EmitBinaryNode(plan, new StackReferenceNode(Nodes[0].DataType, 1, true), Instructions.Equal, new StackReferenceNode(((Schema.TableType)Nodes[1].DataType).RowType, 0, true));
		}
		
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			ITable table = (ITable)argument2;
			#if NILPROPOGATION
			if ((table == null) || (argument1 == null))
				return null;
			#endif
			program.Stack.Push(argument1);
			try
			{
				Row row = new Row(program.ValueManager, table.DataType.RowType);
				try
				{
					program.Stack.Push(row);
					try
					{
						object result = false;
						while (table.Next())
						{
							table.Select(row);
							object tempValue = _equalNode.Execute(program);
							#if NILPROPOGATION
							if (tempValue == null)
							{
								result = tempValue;
								continue;
							}
							#endif
							if ((bool)tempValue)
								return tempValue;
						}
						return result;
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					row.Dispose();
				}
			}
			finally
			{
				program.Stack.Pop();
			}
		}
	}
}
