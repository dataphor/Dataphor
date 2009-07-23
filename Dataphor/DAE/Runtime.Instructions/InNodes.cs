/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define NILPROPOGATION
	
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.Text;
	using System.Threading;
	using System.Collections;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
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
		private PlanNode FEqualNode;
		public PlanNode EqualNode
		{
			get { return FEqualNode; }
			set { FEqualNode = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			FEqualNode = Compiler.EmitBinaryNode(APlan, new StackReferenceNode(Nodes[0].DataType, 1, true), Instructions.Equal, new StackReferenceNode(((Schema.ListType)Nodes[1].DataType).ElementType, 0, true));
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			ListValue LList = (ListValue)AArgument2;
			#if NILPROPOGATION
			if ((LList == null) || (AArgument1 == null))
				return null;
			#endif
			
			AProcess.Context.Push(AArgument1);
			try
			{
				AProcess.Context.Push(null);
				try
				{
					object LResult = false;
					for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					{
						AProcess.Context.Poke(0, LList[LIndex]);
						object LValue = FEqualNode.Execute(AProcess);
						#if NILPROPOGATION
						if (LValue == null)
						{
							LResult = null;
							continue;
						}
						#endif
						if ((bool)LValue)
							return LValue;
					}
					return LResult;
				}
				finally
				{
					AProcess.Context.Pop();
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}
		}
	}
	
	// operator iIn(AScalar : scalar, ATable : table) : boolean;
	// operator iIn(ARow : row, ATable : table) : boolean;
	// operator iIn(AScalar : scalar, APresentation : presentation) : boolean;
	// operator iIn(AEntry : entry, APresentation : presentation) : boolean;
	public class InTableNode : BinaryInstructionNode
	{
		private PlanNode FEqualNode;
		public PlanNode EqualNode
		{
			get { return FEqualNode; }
			set { FEqualNode = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);
			if (Nodes[0].DataType is Schema.ScalarType)
			{
				Schema.TableType LTableType = (Schema.TableType)Nodes[1].DataType;
				if (LTableType.Columns.Count != 1)
					throw new CompilerException(CompilerException.Codes.InvalidMembershipOperand, APlan.CurrentStatement(), Nodes[0].DataType.Name, Nodes[1].DataType.Name);
				#if USECOLUMNLOCATIONBINDING
				FEqualNode = 
					Compiler.EmitBinaryNode
					(
						APlan, 
						new StackReferenceNode(Nodes[0].DataType, 1, true), 
						Instructions.Equal, 
						new StackColumnReferenceNode(LTableType.Columns[0].DataType, 0, 0)
					);
				#else
				FEqualNode = 
					Compiler.EmitBinaryNode
					(
						APlan, 
						new StackReferenceNode(Nodes[0].DataType, 1, true), 
						Instructions.Equal, 
						new StackColumnReferenceNode(LTableType.Columns[0].Name, LTableType.Columns[0].DataType, 0)
					);
				#endif
			}
			else
				FEqualNode = Compiler.EmitBinaryNode(APlan, new StackReferenceNode(Nodes[0].DataType, 1, true), Instructions.Equal, new StackReferenceNode(((Schema.TableType)Nodes[1].DataType).RowType, 0, true));
		}
		
		public override object InternalExecute(ServerProcess AProcess, object AArgument1, object AArgument2)
		{
			Table LTable = (Table)AArgument2;
			#if NILPROPOGATION
			if ((LTable == null) || (AArgument1 == null))
				return null;
			#endif
			AProcess.Context.Push(AArgument1);
			try
			{
				Row LRow = new Row(AProcess, LTable.DataType.RowType);
				try
				{
					AProcess.Context.Push(LRow);
					try
					{
						object LResult = false;
						while (LTable.Next())
						{
							LTable.Select(LRow);
							object LValue = FEqualNode.Execute(AProcess);
							#if NILPROPOGATION
							if (LValue == null)
							{
								LResult = LValue;
								continue;
							}
							#endif
							if ((bool)LValue)
								return LValue;
						}
						return LResult;
					}
					finally
					{
						AProcess.Context.Pop();
					}
				}
				finally
				{
					LRow.Dispose();
				}
			}
			finally
			{
				AProcess.Context.Pop();
			}
		}
	}
}
