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
	public class ValueInListNode : InstructionNode
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			ListValue LList = (ListValue)AArguments[1].Value;
			#if NILPROPOGATION
			if ((LList == null) || LList.IsNil || (AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			
			AProcess.Context.Push(AArguments[0]);
			try
			{
				DataVar LDataVar = new DataVar(((Schema.ListType)LList.DataType).ElementType);
				AProcess.Context.Push(LDataVar);
				try
				{
					DataValue LResult = new Scalar(AProcess, (Schema.ScalarType)FDataType, false);
					for (int LIndex = 0; LIndex < LList.Count(); LIndex++)
					{
						LDataVar.Value = LList[LIndex];
						DataValue LValue = FEqualNode.Execute(AProcess).Value;
						#if NILPROPOGATION
						if ((LValue == null) || LValue.IsNil)
						{
							LResult = LValue;
							continue;
						}
						#endif
						if (LValue.AsBoolean)
							return new DataVar(DataType, LValue);
					}
					return new DataVar(DataType, LResult);
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
	public class InTableNode : InstructionNode
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
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			Table LTable = (Table)AArguments[1].Value;
			#if NILPROPOGATION
			if ((LTable == null) || LTable.IsNil || (AArguments[0].Value == null) || AArguments[0].Value.IsNil)
				return new DataVar(FDataType, null);
			#endif
			AProcess.Context.Push(AArguments[0]);
			try
			{
				Row LRow = new Row(AProcess, LTable.DataType.RowType);
				try
				{
					DataVar LDataVar = new DataVar(LRow.DataType, LRow);
					AProcess.Context.Push(LDataVar);
					try
					{
						DataValue LResult = new Scalar(AProcess, (Schema.ScalarType)FDataType, false);
						while (LTable.Next())
						{
							LTable.Select(LRow);
							DataValue LValue = FEqualNode.Execute(AProcess).Value;
							#if NILPROPOGATION
							if ((LValue == null) || LValue.IsNil)
							{
								LResult = LValue;
								continue;
							}
							#endif
							if (LValue.AsBoolean)
								return new DataVar(DataType, LValue);
						}
						return new DataVar(DataType, LResult);
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
