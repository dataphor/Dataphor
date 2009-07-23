/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

// TODO: don't use exceptions for flow control at run-time
// TODO: don't push frames unless it is necessary
// TODO: optimize application transaction calls in the CallNode

using System; 
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	public class TestNode : NilaryInstructionNode
	{
		public TestNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Ldstr, "IL Generated exception");
			AGenerator.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new Type[] { typeof(String) }));
			AGenerator.Emit(OpCodes.Throw);
		}

		public override object NilaryInternalExecute(ServerProcess AProcess)
		{
			throw new NotImplementedException();
		}
	}

	public class BlockNode : PlanNode
	{
		public BlockNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{	
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
				EmitExecute(APlan, AGenerator, LExecutePath, LIndex);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			foreach (PlanNode LNode in Nodes)	
				LNode.Execute(AProcess);
			return null;
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			Block LBlock = new Block();
			Statement LStatement;
			foreach (PlanNode LNode in Nodes)
			{
				LStatement = LNode.EmitStatement(AMode);
				if (!(LStatement is EmptyStatement))
					LBlock.Statements.Add(LStatement);
			}
			switch (LBlock.Statements.Count)
			{
				case 0: return new EmptyStatement();
				case 1: return LBlock.Statements[0];
				default: return LBlock;
			}
		}
	}
	
	public class DelimitedBlockNode : PlanNode
	{
		public DelimitedBlockNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			foreach (PlanNode LNode in Nodes)
				LNode.Execute(AProcess);
			return null;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{	
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			for (int LIndex = 0; LIndex < Nodes.Count; LIndex++)
				EmitExecute(APlan, AGenerator, LExecutePath, LIndex);
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			DelimitedBlock LBlock = new DelimitedBlock();
			Statement LStatement;
			foreach (PlanNode LNode in Nodes)
			{
				LStatement = LNode.EmitStatement(AMode);
				if (!(LStatement is EmptyStatement))
					LBlock.Statements.Add(LStatement);
			}
			return LBlock;
		}
	}
	
	public class FrameNode : PlanNode
	{
		public FrameNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.PushFrame();
			try
			{
				base.InternalDetermineBinding(APlan);
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}
		
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{	
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			// Call AProcess.Context.PushFrame();
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PushFrame", new Type[] { }));
			AGenerator.BeginExceptionBlock();
			
			EmitExecute(APlan, AGenerator, LExecutePath, 0);

			// Call AProcess.Context.PopFrame()				
			AGenerator.BeginFinallyBlock();
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PopFrame"));
				
			AGenerator.EndExceptionBlock();
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context.PushFrame();
			try
			{
				Nodes[0].Execute(AProcess);
				return null;
			}
			finally
			{
				AProcess.Context.PopFrame();
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return Nodes[0].EmitStatement(AMode);
		}
	}
	
	public class ExpressionStatementNode : PlanNode
	{
		public ExpressionStatementNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public ExpressionStatementNode(PlanNode ANode) : base()
		{
			ShouldEmitIL = true;
			Nodes.Add(ANode);
		}
		
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{	
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			EmitExecute(APlan, AGenerator, LExecutePath, 0);			
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			object LObject = Nodes[0].Execute(AProcess);
			DataValue.DisposeValue(AProcess, LObject);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new ExpressionStatement((Expression)Nodes[0].EmitStatement(AMode));
		}
	}
	
	[Serializable]
	public class ControlError : Exception
	{
		public ControlError() : base(){} 
		public ControlError(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext){}
	}

	[Serializable]	
	public class ExitError : ControlError
	{
		public ExitError() : base(){}
		public ExitError(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext){}
	}
	
	public class ExitNode : PlanNode
	{
		public ExitNode() : base()
		{
			ShouldEmitIL = true;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Newobj, typeof(ExitError).GetConstructor(new Type[] { }));
			AGenerator.Emit(OpCodes.Throw);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			throw new ExitError();
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new ExitStatement();
		}
	}
	
	public class WhileNode : PlanNode
	{
		public WhileNode() : base()
		{
			ShouldEmitIL = true;
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			AGenerator.BeginExceptionBlock();
			
			Label LLoop = AGenerator.DefineLabel();
			Label LAfterLoop = AGenerator.DefineLabel();
			LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataValue));

			AGenerator.MarkLabel(LLoop);
			
			AGenerator.BeginExceptionBlock();
			
			// Test the iteration condition
			EmitEvaluate(APlan, AGenerator, LExecutePath, 0);
			
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stloc, LResult);

			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Brfalse, LAfterLoop);

			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("IsNil").GetGetMethod());
			AGenerator.Emit(OpCodes.Brtrue, LAfterLoop);
			
			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("AsBoolean").GetGetMethod());
			AGenerator.Emit(OpCodes.Brfalse, LAfterLoop);

			// Execute the iterated statement				
			EmitExecute(APlan, AGenerator, LExecutePath, 1);
				
			AGenerator.BeginCatchBlock(typeof(ContinueError));
			AGenerator.EndExceptionBlock();
			
			AGenerator.Emit(OpCodes.Br, LLoop);
				
			AGenerator.BeginCatchBlock(typeof(BreakError));
			AGenerator.EndExceptionBlock();
			
			AGenerator.MarkLabel(LAfterLoop);
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			try
			{
				while (true)
				{
					try
					{
						object LObject = Nodes[0].Execute(AProcess);
						if ((LObject == null) || !(bool)LObject)
							break;
							
						Nodes[1].Execute(AProcess);
					}
					catch (ContinueError) { }
				}
			}
			catch (BreakError) { }
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			WhileStatement LStatement = new WhileStatement();
			LStatement.Condition = (Expression)Nodes[0].EmitStatement(AMode);
			LStatement.Statement = Nodes[1].EmitStatement(AMode);
			return LStatement;
		}
	}
	
	public class DoWhileNode : PlanNode
	{
		public DoWhileNode() : base()
		{
			ShouldEmitIL = true;
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			AGenerator.BeginExceptionBlock();
			
			Label LLoop = AGenerator.DefineLabel();
			Label LAfterLoop = AGenerator.DefineLabel();
			LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataValue));

			AGenerator.MarkLabel(LLoop);
			
			AGenerator.BeginExceptionBlock();
			
			// Execute the iterated statement				
			EmitExecute(APlan, AGenerator, LExecutePath, 0);
				
			// Test the iteration condition
			EmitEvaluate(APlan, AGenerator, LExecutePath, 1);
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stloc, LResult);

			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Brfalse, LAfterLoop);
			
			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("IsNil").GetGetMethod());
			AGenerator.Emit(OpCodes.Brtrue, LAfterLoop);
			
			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("AsBoolean").GetGetMethod());
			AGenerator.Emit(OpCodes.Brfalse, LAfterLoop);

			AGenerator.BeginCatchBlock(typeof(ContinueError));
			AGenerator.EndExceptionBlock();
			
			AGenerator.Emit(OpCodes.Br, LLoop);
				
			AGenerator.BeginCatchBlock(typeof(BreakError));
			AGenerator.EndExceptionBlock();
			
			AGenerator.MarkLabel(LAfterLoop);
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			try
			{
				while (true)
				{
					try
					{
						Nodes[0].Execute(AProcess);
						object LObject = Nodes[1].Execute(AProcess);
						if ((LObject == null) || !(bool)LObject)
							break;
					}
					catch (ContinueError){}
				}
			}
			catch (BreakError){}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			DoWhileStatement LStatement = new DoWhileStatement();
			LStatement.Statement = Nodes[0].EmitStatement(AMode);
			LStatement.Condition = (Expression)Nodes[1].EmitStatement(AMode);
			return LStatement;
		}
	}

	// ForEachNode
	//	Nodes[0] - Iteration Expression
	//	Nodes[1] - Iteration Statement
	public class ForEachNode : PlanNode
	{
		private ForEachStatement FStatement;
		public ForEachStatement Statement
		{
			get { return FStatement; }
			set { FStatement = value; }
		}
		
		private Schema.IDataType FVariableType;
		public Schema.IDataType VariableType
		{
			get { return FVariableType; }
			set { FVariableType = value; }
		}
		
		private int FLocation;
		public int Location
		{
			get { return FLocation; }
			set { FLocation = value; }
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			if (FStatement.VariableName == String.Empty)
				APlan.EnterRowContext();
			try
			{
				if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
					APlan.Symbols.Push(new Symbol(FStatement.VariableName, FVariableType));
				try
				{
					if ((FStatement.VariableName != String.Empty) && !FStatement.IsAllocation)
					{
						int LColumnIndex;
						Location = Compiler.ResolveVariableIdentifier(APlan, FStatement.VariableName, out LColumnIndex);
						if (Location < 0)
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, FStatement.VariableName);
							
						if (LColumnIndex >= 0)
							throw new CompilerException(CompilerException.Codes.InvalidColumnBinding, FStatement.VariableName);
					}

					Nodes[1].DetermineBinding(APlan);
				}
				finally
				{
					if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
						APlan.Symbols.Pop();
				}
			}
			finally
			{
				if (FStatement.VariableName == String.Empty)
					APlan.ExitRowContext();
			}
		}
		
		private bool CursorNext(ServerProcess AProcess, Cursor ACursor)
		{
			ACursor.SwitchContext(AProcess);
			try
			{
				return ACursor.Table.Next();
			}
			finally
			{
				ACursor.SwitchContext(AProcess);
			}
		}
		
		private void CursorSelect(ServerProcess AProcess, Cursor ACursor, Row ARow)
		{
			ACursor.SwitchContext(AProcess);
			try
			{
				ACursor.Table.Select(ARow);
			}
			finally
			{
				ACursor.SwitchContext(AProcess);
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (Nodes[0].DataType is Schema.ICursorType)
			{
				CursorValue LCursorValue = (CursorValue)Nodes[0].Execute(AProcess);
				Cursor LCursor = AProcess.Plan.CursorManager.GetCursor(LCursorValue.ID);
				try
				{
					int LStackIndex = 0;
					if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
						AProcess.Context.Push(null);
					else
						LStackIndex = Location;
					try
					{
						using (Row LRow = new Row(AProcess, (Schema.IRowType)FVariableType))
						{
							AProcess.Context.Poke(LStackIndex, LRow);
							try
							{
								while (CursorNext(AProcess, LCursor))
								{
									try
									{
										// Select row...
										CursorSelect(AProcess, LCursor, LRow);
										Nodes[1].Execute(AProcess);
									}
									catch (ContinueError) {}
								}
							}
							finally
							{
								AProcess.Context.Poke(LStackIndex, null); // TODO: Stack imbalance if the iteration statement allocates a variable
							}
						}
					}
					finally
					{
						if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
							AProcess.Context.Pop();
					}
				}
				catch (BreakError) {}
				finally
				{
					AProcess.Plan.CursorManager.CloseCursor(LCursorValue.ID);
				}
			}
			else
			{
				ListValue LValue = (ListValue)Nodes[0].Execute(AProcess);
				if (LValue != null)
				{
					try
					{
						int LStackIndex = 0;
						if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
							AProcess.Context.Push(null);
						else
							LStackIndex = Location;
						
						try
						{
							for (int LIndex = 0; LIndex < LValue.Count(); LIndex++)
							{
								try
								{
									// Select iteration value
									AProcess.Context.Poke(LStackIndex, LValue[LIndex]);
									Nodes[1].Execute(AProcess);
								}
								catch (ContinueError) {}
							}
						}
						finally
						{
							if ((FStatement.VariableName == String.Empty) || FStatement.IsAllocation)
								AProcess.Context.Pop();
						}
					}
					catch (BreakError) {}
				}
			}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			ForEachStatement LStatement = new ForEachStatement();
			LStatement.IsAllocation = FStatement.IsAllocation;
			LStatement.VariableName = FStatement.VariableName;
			if (Nodes[0] is CursorNode)
			{
				CursorSelectorExpression LCursorSelectorExpression = (CursorSelectorExpression)Nodes[0].EmitStatement(AMode);
				LStatement.Expression = LCursorSelectorExpression.CursorDefinition;
			}
			else
				LStatement.Expression = new CursorDefinition((Expression)Nodes[0].EmitStatement(AMode));
			
			LStatement.Statement = Nodes[1].EmitStatement(AMode);	
			
			return LStatement;
		}
	}

	[Serializable]	
	public class BreakError : ControlError
	{
		public BreakError() : base(){}
		public BreakError(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext){}
	}
	
	public class BreakNode : PlanNode
	{
		public BreakNode() : base()
		{
			ShouldEmitIL = true;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Newobj, typeof(BreakError).GetConstructor(new Type[] { }));
			AGenerator.Emit(OpCodes.Throw);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			throw new BreakError();
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new BreakStatement();
		}
	}

	[Serializable]	
	public class ContinueError : ControlError
	{
		public ContinueError() : base(){}
		public ContinueError(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext){}
	}
	
	public class ContinueNode : PlanNode
	{
		public ContinueNode() : base()
		{
			ShouldEmitIL = true;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Newobj, typeof(ContinueError).GetConstructor(new Type[] { }));
			AGenerator.Emit(OpCodes.Throw);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			throw new ContinueError();
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new ContinueStatement();
		}
	}

	public class RaiseNode : PlanNode
	{
		public RaiseNode() : base()
		{
			ShouldEmitIL = true;
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			if (Nodes.Count > 0)
			{
				int[] LExecutePath = new int[AExecutePath.Length + 1];
				AExecutePath.CopyTo(LExecutePath, 0);
				
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());

				// Evaluate the raise expression
				LExecutePath[LExecutePath.Length - 1] = 0;
				Nodes[0].EmitIL(APlan, AGenerator, LExecutePath);
				
				AGenerator.Emit(OpCodes.Call, typeof(Context).GetProperty("ErrorVar").GetSetMethod());
			}
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetProperty("ErrorVar").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("AsException").GetGetMethod());
			AGenerator.Emit(OpCodes.Throw);
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (Nodes.Count > 0)
				AProcess.Context.ErrorVar = Nodes[0].Execute(AProcess);
			if (AProcess.Context.ErrorVar == null)
				throw new RuntimeException(RuntimeException.Codes.NilEncountered, this);
			throw (Exception)AProcess.Context.ErrorVar;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			RaiseStatement LStatement = new RaiseStatement();
			if (Nodes.Count > 0)
				LStatement.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			return LStatement;
		}
	}
	
	public class TryFinallyNode : PlanNode
	{
		public TryFinallyNode() : base()
		{
			ShouldEmitIL = true;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			AGenerator.BeginExceptionBlock();

			// Execute the protected statement
			EmitExecute(APlan, AGenerator, LExecutePath, 0);
				
			AGenerator.BeginFinallyBlock();

			// Execute the finally statement
			EmitExecute(APlan, AGenerator, LExecutePath, 1);
				
			AGenerator.EndExceptionBlock();
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			try
			{
				Nodes[0].Execute(AProcess);
				return null;
			}
			finally
			{
				Nodes[1].Execute(AProcess);
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			TryFinallyStatement LStatement = new TryFinallyStatement();
			LStatement.TryStatement = Nodes[0].EmitStatement(AMode);
			LStatement.FinallyStatement = Nodes[1].EmitStatement(AMode);
			return LStatement;
		}
	}
	
	public class ErrorHandlerNode : PlanNode
	{
		//public ErrorHandlerNode() : base()
		//{
		//    ShouldEmitIL = true;
		//}
		
		protected bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
		}
		
		protected Schema.IDataType FErrorType;
		public Schema.IDataType ErrorType
		{
			get { return FErrorType; }
			set { FErrorType = value; }
		}
		
		protected string FVariableName = String.Empty;
		public string VariableName
		{
			get { return FVariableName; }
			set { FVariableName = value == null ? String.Empty : value; }
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.PushFrame();
			try
			{
				if (FVariableName != String.Empty)
					APlan.Symbols.Push(new Symbol(FVariableName, FErrorType));
				base.InternalDetermineBinding(APlan);
			}
			finally
			{
				APlan.Symbols.PopFrame();
			}
		}

		//public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		//{
		//    int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
		//    AGenerator.Emit(OpCodes.Ldarg_1);
		//    AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
		//    AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PushFrame", new Type[] { }));
		//    AGenerator.BeginExceptionBlock();
		//    if (FVariableName != String.Empty)
		//    {
		//        LocalBuilder LThis = EmitThis(APlan, AGeneratr, AExecutePath);
		//        AGenerator.Emit(OpCodes.Ldarg_1);
		//        AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());

		//        AGenerator.Emit(OpCodes.Ldstr, FVariableName);

		//        AGenerator.Emit(OpCodes.Ldloc, LThis);
		//        AGenerator.Emit(OpCodes.Ldfld, typeof(ErrorHandlerNode).GetField("FErrorType"));

		//        AGenerator.Emit(OpCodes.Ldarg_1);
		//        AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
		//        AGenerator.Emit(OpCodes.Call, typeof(Context).GetProperty("ErrorVar").GetGetMethod());
		//        AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
		//        AGenerator.Emit(OpCodes.Castclass, typeof(Scalar));
		//        AGenerator.Emit(OpCodes.Call, typeof(DataValue).GetMethod("Copy", new Type[] { }));

		//        AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(String), typeof(Schema.IDataType), typeof(DataValue) }));
				
		//        AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Push", new Type[] { typeof(DataVar) }));
		//    }
			
		//    EmitExecute(APlan, AGenerator, LExecutePath, 0);

		//    AGenerator.BeginFinallyBlock();
		//    AGenerator.Emit(OpCodes.Ldarg_1);
		//    AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
		//    AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PopFrame", new Type[] { }));

		//    AGenerator.EndExceptionBlock();
		//}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context.PushFrame();
			try
			{
				if (FVariableName != String.Empty)
					AProcess.Context.Push(DataValue.CopyValue(AProcess, AProcess.Context.ErrorVar));
				Nodes[0].Execute(AProcess);	   
				return null;
			}
			finally
			{
				AProcess.Context.PopFrame();
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (IsGeneric)
			{
				GenericErrorHandler LHandler = new GenericErrorHandler();
				LHandler.Statement = Nodes[0].EmitStatement(AMode);
				return LHandler;
			}
			else
			{
				if (VariableName != String.Empty)
				{
					ParameterizedErrorHandler LHandler = new ParameterizedErrorHandler(ErrorType.Name, VariableName);
					LHandler.Statement = Nodes[0].EmitStatement(AMode);
					return LHandler;
				}
				else
				{
					SpecificErrorHandler LHandler = new SpecificErrorHandler(ErrorType.Name);
					LHandler.Statement = Nodes[0].EmitStatement(AMode);
					return LHandler;
				}
			}
		}
	}
	
	public class TryExceptNode : PlanNode
	{
		public static ErrorHandlerNode GetErrorHandlerNode(PlanNode ANode)
		{
			ErrorHandlerNode LResult = ANode as ErrorHandlerNode;
			if (LResult != null)
				return LResult;
			return GetErrorHandlerNode(ANode.Nodes[0]);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			try
			{
				Nodes[0].Execute(AProcess);
			}
			catch (Exception LException)
			{
				// if this is a host exception, set the error variable
				if (AProcess.Context.ErrorVar == null)
					AProcess.Context.ErrorVar = LException;
					
				ErrorHandlerNode LNode;
				object LErrorVar = AProcess.Context.ErrorVar;
				for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
				{
					LNode = GetErrorHandlerNode(Nodes[LIndex]);
					if (AProcess.DataTypes.SystemError.Is(LNode.ErrorType)) // TODO: No RTTI on the error
					{
						LNode.Execute(AProcess);
						break;
					}
				}
				AProcess.Context.ErrorVar = null;
			}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			TryExceptStatement LStatement = new TryExceptStatement();
			LStatement.TryStatement = Nodes[0].EmitStatement(AMode);
			for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
				LStatement.ErrorHandlers.Add((GenericErrorHandler)GetErrorHandlerNode(Nodes[LIndex]).EmitStatement(AMode));
			return LStatement;
		}
	}
	
	public class IfNode : PlanNode
	{
		public IfNode() : base()
		{
			ShouldEmitIL = true;
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			Label LFalse = AGenerator.DefineLabel();
			Label LEnd = AGenerator.DefineLabel();
			LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataValue));
			
			// Evaluate the condition expression
			EmitEvaluate(APlan, AGenerator, LExecutePath, 0);
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stloc, LResult);

			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Brfalse, LFalse);
			
			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("IsNil").GetGetMethod());
			AGenerator.Emit(OpCodes.Brtrue, LFalse);
			
			AGenerator.Emit(OpCodes.Ldloc, LResult);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("AsBoolean").GetGetMethod());
			AGenerator.Emit(OpCodes.Brfalse, LFalse);

			// Execute the true statement
			EmitExecute(APlan, AGenerator, LExecutePath, 1);
				
			AGenerator.Emit(OpCodes.Br, LEnd);
				
			AGenerator.MarkLabel(LFalse);
			
			// Execute the false statement
			if (Nodes.Count > 2)
				EmitExecute(APlan, AGenerator, LExecutePath, 2);

			AGenerator.MarkLabel(LEnd);			
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			object LObject = Nodes[0].Execute(AProcess);
			bool LValue = false;
			#if NILPROPOGATION
			if ((LObject == null))
				LValue = false;
			else
			#endif
				LValue = (bool)LObject;
			if (LValue)
				Nodes[1].Execute(AProcess);
			else
				if (Nodes.Count > 2)
					Nodes[2].Execute(AProcess);

			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			IfStatement LStatement = new IfStatement();
			LStatement.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			LStatement.TrueStatement = Nodes[1].EmitStatement(AMode);
			if (Nodes.Count > 2)
				LStatement.FalseStatement = Nodes[2].EmitStatement(AMode);
			return LStatement;
		}
	}
	
	public class CaseNode : PlanNode
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			foreach (CaseItemNode LNode in Nodes)
			{
				if (LNode.Nodes.Count == 2)
				{
					bool LValue = false;
					object LObject = LNode.Nodes[0].Execute(AProcess);
					#if NILPROPOGATION
					if ((LObject == null))
						LValue = false;
					else
					#endif
						LValue = (bool)LObject;
					
					if (LValue)
					{
						LNode.Nodes[1].Execute(AProcess);
						break;
					}
				}
				else
				{
					LNode.Nodes[0].Execute(AProcess);
					break;
				}
			}
			
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			CaseStatement LCaseStatement = new CaseStatement();
			foreach (CaseItemNode LNode in Nodes)
			{
				if (LNode.Nodes.Count == 2)
					LCaseStatement.CaseItems.Add(new CaseItemStatement((Expression)LNode.Nodes[0].EmitStatement(AMode), LNode.Nodes[1].EmitStatement(AMode)));
				else
					LCaseStatement.ElseStatement = LNode.Nodes[0].EmitStatement(AMode);
			}
			return LCaseStatement;
		}
	}
	
	// Nodes[0] -> Selector expression
	// Nodes[1] -> Selector equality node
	// Nodes[2..N] -> case item nodes
		// Nodes[0] -> case item when expression
		// Nodes[1] -> case item then statement
	public class SelectedCaseNode : PlanNode
	{
		public override void DetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			// Do not bind node 1, it will fail (it contains nameless stack references)
			APlan.Symbols.Push(new Symbol(Nodes[0].DataType));
			try
			{
				for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
					Nodes[LIndex].DetermineBinding(APlan);
			}
			finally
			{
				APlan.Symbols.Pop();
			}
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			object LSelector = Nodes[0].Execute(AProcess);
			try
			{
				AProcess.Context.Push(LSelector);
				try
				{
					for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
					{
						CaseItemNode LNode = (CaseItemNode)Nodes[LIndex];
						if (LNode.Nodes.Count == 2)
						{
							bool LValue = false;
							object LWhenVar = LNode.Nodes[0].Execute(AProcess);
							try
							{
								AProcess.Context.Push(LWhenVar);
								try
								{
									object LObject = Nodes[1].Execute(AProcess);
									#if NILPROPOGATION
									if (LObject == null)
										LValue = false;
									else
									#endif
										LValue = (bool)LObject;
								}
								finally
								{
									AProcess.Context.Pop();
								}
							}
							finally
							{
								DataValue.DisposeValue(AProcess, LWhenVar);
							}

							if (LValue)
							{
								LNode.Nodes[1].Execute(AProcess);
								break;
							}
						}
						else
						{
							LNode.Nodes[0].Execute(AProcess);
							break;
						}
					}
				}
				finally
				{	
					AProcess.Context.Pop();
				}
			}
			finally
			{
				DataValue.DisposeValue(AProcess, LSelector);
			}
			
			return null;
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			CaseStatement LCaseStatement = new CaseStatement();
			LCaseStatement.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
			{
				CaseItemNode LNode = (CaseItemNode)Nodes[LIndex];
				if (LNode.Nodes.Count == 2)
					LCaseStatement.CaseItems.Add(new CaseItemStatement((Expression)LNode.Nodes[0].EmitStatement(AMode), LNode.Nodes[1].EmitStatement(AMode)));
				else
					LCaseStatement.ElseStatement = LNode.Nodes[0].EmitStatement(AMode);
			}
			return LCaseStatement;
		}
	}
	
	public class CaseItemNode : PlanNode
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			return null;
		}
	}

	public class ValueNode : PlanNode
    {		
		// constructor
		public ValueNode() : base() 
		{
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = false;
			ShouldEmitIL = true;
		}

		public ValueNode(Schema.IDataType ADataType, object AValue) : base()
		{
			FDataType = ADataType;
			FValue = AValue;
			FIsLiteral = true;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsNilable = FValue == null;
			ShouldEmitIL = true;
		}
		
		protected object FValue;
		public object Value
		{
			get { return FValue; }
			set 
			{ 
				FValue = value; 
				FIsNilable = FValue == null;
			}
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("FDataType", BindingFlags.Instance | BindingFlags.NonPublic));
			
			if (FValue == null)
				AGenerator.Emit(OpCodes.Ldnull);
			else
			{
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Ldloc, LThis);
				AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("FDataType", BindingFlags.Instance | BindingFlags.NonPublic));
				AGenerator.Emit(OpCodes.Ldloc, LThis);
				AGenerator.Emit(OpCodes.Ldfld, typeof(ValueNode).GetField("FValue", BindingFlags.Instance | BindingFlags.NonPublic));
				AGenerator.Emit(OpCodes.Call, typeof(DataValue).GetMethod("FromNative", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(IServerProcess), typeof(Schema.IDataType), typeof(object) }, null));
			}	
			
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
		}
*/

		// Execute
		public override object InternalExecute(ServerProcess AProcess)
		{
			return FValue;		 
		}
		
		// EmitStatement
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (FValue == null)
				return new ValueExpression(null, TokenType.Nil);
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemBoolean))
				return new ValueExpression(FValue, TokenType.Boolean);
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemLong))
				return new ValueExpression(FValue, TokenType.Integer);
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemInteger))
				return new ValueExpression(FValue, TokenType.Integer);
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemDecimal))
				return new ValueExpression(FValue, TokenType.Decimal);
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemString))
				return new ValueExpression(FValue, TokenType.String);
			#if USEISTRING
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemIString))
				return new ValueExpression(FValue, LexerToken.IString);
			#endif
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemMoney))
				return new ValueExpression(FValue, TokenType.Money);
			else
				throw new RuntimeException(RuntimeException.Codes.UnsupportedValueType, FDataType.Name);
		}
    }																			
    
	public class ParameterNode : PlanNode
	{
		public ParameterNode() : base()
		{
			ShouldEmitIL = true;
		}

		public ParameterNode(PlanNode ANode, Modifier AModifier)
		{
			Nodes.Add(ANode);
			FModifier = AModifier;
			ShouldEmitIL = true;
		}
		
		private Modifier FModifier;
		public Modifier Modifier
		{
			get { return FModifier; }
			set { FModifier = value; }
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new ParameterExpression(FModifier, (Expression)Nodes[0].EmitStatement(AMode));
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = Nodes[0].DataType;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			if (Modifier == Modifier.Var)
				APlan.PushCursorContext(new CursorContext(CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{
				base.InternalDetermineBinding(APlan);
			}
			finally
			{
				if (Modifier == Modifier.Var)
					APlan.PopCursorContext();
			}
		}
		
		public override void BindToProcess(Plan APlan)
		{
			if (Modifier == Modifier.Var)
				APlan.PushCursorContext(new CursorContext(CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{	
				base.BindToProcess(APlan);
			}
			finally
			{
				if (Modifier == Modifier.Var)
					APlan.PopCursorContext();
			}
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			EmitEvaluate(APlan, AGenerator, LExecutePath, 0);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			return Nodes[0].Execute(AProcess);
		}
	}
	
	// The CallNode is responsible for preparing the stack with the given arguments.
	// The CallNode is also responsible for the Result variable, if the call is a function.
	public class CallNode : InstructionNode
	{
		public CallNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		private PlanNode FAllocateResultNode;
		
		public override void DetermineDataType(Plan APlan)
		{
			base.DetermineDataType(APlan);

			if (FDataType != null)
				FAllocateResultNode = new VariableNode(Keywords.Result, FDataType);
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			bool LSaveIsInsert = APlan.ServerProcess.IsInsert;
			APlan.ServerProcess.IsInsert = false;
			try
			{
				base.InternalDetermineBinding(APlan);
			}
			finally
			{
				APlan.ServerProcess.IsInsert = LSaveIsInsert;
			}

			for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
				APlan.Symbols.Push(new Symbol(Operator.Operands[LIndex].Name, Nodes[LIndex].DataType));
				
			APlan.Symbols.PushWindow(Operator.Operands.Count);
			try
			{
				if (FAllocateResultNode != null)
					FAllocateResultNode.DetermineBinding(APlan);
			}
			finally
			{
				APlan.Symbols.PopWindow();
			}
		}

/*
		protected override void EmitInstructionIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath, LocalBuilder AArguments)
		{
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetMethod("CheckAborted", new Type[] { }));
			
			for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
			{
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
				AGenerator.Emit(OpCodes.Ldloc, AArguments);
				AGenerator.Emit(OpCodes.Ldc_I4, LIndex);
				AGenerator.Emit(OpCodes.Ldelem_Ref);
				AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Push", new Type[] { typeof(DataVar) }));
			}
			
			LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataVar));
			
			AGenerator.Emit(OpCodes.Ldnull);
			AGenerator.Emit(OpCodes.Stloc, LResult);

			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldc_I4, Operator.Operands.Count);
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PushWindow", new Type[] { typeof(int) }));
			
			AGenerator.BeginExceptionBlock();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Plan").GetGetMethod());
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(InstructionNodeBase).GetField("FOperator", BindingFlags.Instance | BindingFlags.NonPublic));
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Operator).GetProperty("Owner").GetGetMethod());
			AGenerator.Emit(OpCodes.Newobj, typeof(SecurityContext).GetConstructor(new Type[] { typeof(Schema.User) }));
			
			AGenerator.Emit(OpCodes.Call, typeof(Plan).GetMethod("PushSecurityContext", new Type[] { typeof(SecurityContext) }));
			
			AGenerator.BeginExceptionBlock();
			
			LocalBuilder LSaveIsInsert = AGenerator.DeclareLocal(typeof(bool));
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("IsInsert").GetGetMethod());
			AGenerator.Emit(OpCodes.Stloc, LSaveIsInsert);
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Ldc_I4_0);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("IsInsert").GetSetMethod());
			
			AGenerator.BeginExceptionBlock();
			
			LocalBuilder LTransaction = AGenerator.DeclareLocal(typeof(ApplicationTransaction));
			
			AGenerator.Emit(OpCodes.Ldnull);
			AGenerator.Emit(OpCodes.Stloc, LTransaction);
			
			Label LNoTransaction = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("ApplicationTransactionID").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldfld, typeof(Guid).GetField("Empty", BindingFlags.Static | BindingFlags.Public));
			AGenerator.Emit(OpCodes.Beq, LNoTransaction);
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(InstructionNodeBase).GetField("FOperator", BindingFlags.Instance | BindingFlags.NonPublic));
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Operator).GetProperty("ShouldTranslate").GetGetMethod());
			AGenerator.Emit(OpCodes.Brtrue, LNoTransaction);
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetMethod("GetApplicationTransaction", new Type[] { }));
			AGenerator.Emit(OpCodes.Stloc, LTransaction);
			
			AGenerator.MarkLabel(LNoTransaction);
			
			AGenerator.BeginExceptionBlock();
			
			Label LNoTransactionGlobal = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Brfalse, LNoTransactionGlobal);
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Call, typeof(ApplicationTransaction).GetMethod("PushGlobalContext", new Type[] { }));
			
			AGenerator.MarkLabel(LNoTransactionGlobal);
			
			AGenerator.BeginExceptionBlock();

			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("IncCallDepth", new Type[] { }));
			
			AGenerator.BeginExceptionBlock();
			
			if (FAllocateResultNode != null)
			{
				if (FAllocateResultNode.ShouldEmitIL)
				{
					FAllocateResultNode.EmitIL(APlan, AGenerator, AExecutePath);
				}
				else
				{
					AGenerator.Emit(OpCodes.Ldloc, LThis);
					AGenerator.Emit(OpCodes.Ldfld, typeof(CallNode).GetField("FAllocateResultNode", BindingFlags.Instance | BindingFlags.NonPublic));
					AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("Execute"));
					AGenerator.Emit(OpCodes.Ldarg_1);
					AGenerator.Emit(OpCodes.Callvirt, typeof(ExecuteDelegate).GetMethod("Invoke", new Type[] { typeof(ServerProcess) }));
				}
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
				AGenerator.Emit(OpCodes.Ldc_I4_0);
				AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Peek", new Type[] { typeof(int) }));
				AGenerator.Emit(OpCodes.Stloc, LResult);
			}
			else
			{
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("DataTypes").GetGetMethod());
				AGenerator.Emit(OpCodes.Call, typeof(Schema.DataTypes).GetProperty("SystemScalar").GetGetMethod());
				AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType) }));
				AGenerator.Emit(OpCodes.Stloc, LResult);
			}
			
			AGenerator.BeginExceptionBlock();
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(InstructionNodeBase).GetField("FOperator", BindingFlags.Instance | BindingFlags.NonPublic));
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Operator).GetProperty("Block").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Schema.OperatorBlock).GetProperty("BlockNode").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("Execute"));
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Callvirt, typeof(ExecuteDelegate).GetMethod("Invoke", new Type[] { typeof(ServerProcess) }));
			AGenerator.Emit(OpCodes.Pop);
			
			AGenerator.BeginCatchBlock(typeof(ExitError));
			
			AGenerator.EndExceptionBlock();
			
			if (DataType != null)
				AGenerator.Emit(OpCodes.Ldloc, LResult);
				
			AGenerator.BeginFinallyBlock();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("DecCallDepth", new Type[] { }));
			
			AGenerator.EndExceptionBlock();

			AGenerator.BeginFinallyBlock();
			
			Label LNoTransactionGlobalFinally = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Brfalse, LNoTransactionGlobalFinally);
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Call, typeof(ApplicationTransaction).GetMethod("PopGlobalContext", new Type[] { }));
			
			AGenerator.MarkLabel(LNoTransactionGlobalFinally);
			
			AGenerator.EndExceptionBlock();
			
			AGenerator.BeginFinallyBlock();
			
			Label LNoTransactionFinally = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Brfalse, LNoTransactionFinally);
			
			AGenerator.Emit(OpCodes.Ldloc, LTransaction);
			AGenerator.Emit(OpCodes.Call, typeof(Monitor).GetMethod("Exit", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object) }, null));
			
			AGenerator.MarkLabel(LNoTransactionFinally);
			
			AGenerator.EndExceptionBlock();
			
			AGenerator.BeginFinallyBlock();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Ldloc, LSaveIsInsert);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("IsInsert").GetSetMethod());

			AGenerator.EndExceptionBlock();
			
			AGenerator.BeginFinallyBlock();

			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Plan").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Plan).GetMethod("PopSecurityContext", new Type[] { }));
			
			AGenerator.EndExceptionBlock();

			AGenerator.BeginFinallyBlock();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("PopWindow", new Type[] { }));

			AGenerator.EndExceptionBlock();
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess, object[] AArguments)
		{
			for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
				AProcess.Context.Push(AArguments[LIndex]);
				
			AProcess.Context.PushWindow(Operator.Operands.Count);
			try
			{
				AProcess.Plan.PushSecurityContext(new SecurityContext(Operator.Owner));
				try
				{
					bool LSaveIsInsert = AProcess.IsInsert;
					AProcess.IsInsert = false;
					try
					{
						ApplicationTransaction LTransaction = null;
						if ((AProcess.ApplicationTransactionID != Guid.Empty) && !Operator.ShouldTranslate)
							LTransaction = AProcess.GetApplicationTransaction();
						try
						{
							if (LTransaction != null)
								LTransaction.PushGlobalContext();
							try
							{
								// Prepare the result
								if (FAllocateResultNode != null)
									FAllocateResultNode.Execute(AProcess);

								// Record the stack depth
								int LStackDepth = AProcess.Context.Count;

								try
								{
									Operator.Block.BlockNode.Execute(AProcess);
								}
								catch (ExitError){}
								
								// Pass any var arguments back out to the instruction
								for (int LIndex = 0; LIndex < Operator.Operands.Count; LIndex++)
									if (Operator.Operands[LIndex].Modifier == Modifier.Var)
										AArguments[LIndex] = AProcess.Context[AProcess.Context.Count - LStackDepth + (Operator.Operands.Count + (FAllocateResultNode != null ? 1 : 0) - 1 - LIndex)];
								
								// Return the result
								if (FAllocateResultNode != null)
									return AProcess.Context[AProcess.Context.Count - LStackDepth];

								return null;
							}
							finally
							{
								if (LTransaction != null)
									LTransaction.PopGlobalContext();
							}
						}
						finally
						{
							if (LTransaction != null)
								Monitor.Exit(LTransaction);
						}
					}
					finally
					{
						AProcess.IsInsert = LSaveIsInsert;
					}
				}
				finally
				{
					AProcess.Plan.PopSecurityContext();
				}
			}
			finally
			{
				AProcess.Context.PopWindow();
			}
		}
	}
	
	public class VariableNode : PlanNode
	{
		public VariableNode() : base(){}
		public VariableNode(string AVariableName, Schema.IDataType AVariableType) : base()
		{
			FVariableName = AVariableName;
			FVariableType = AVariableType;
		}
		
		protected string FVariableName = String.Empty;
		public string VariableName
		{
			get { return FVariableName; }
			set { FVariableName = value == null ? String.Empty : value; }
		}
		
		protected Schema.IDataType FVariableType;
		public Schema.IDataType VariableType
		{
			get { return FVariableType; }
			set { FVariableType = value; }
		}
		
		private bool FHasDefault;
		public bool HasDefault 
		{ 
			get { return FHasDefault; } 
			set { FHasDefault = value; } 
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			if (Nodes.Count > 0)
				APlan.Symbols.Push(new Symbol(String.Empty, APlan.Catalog.DataTypes.SystemGeneric));
			try
			{
				base.InternalDetermineBinding(APlan);
			}
			finally
			{
				if (Nodes.Count > 0)
					APlan.Symbols.Pop();
			}
			APlan.Symbols.Push(new Symbol(FVariableName, FVariableType));
			Schema.ScalarType LScalarType = FVariableType as Schema.ScalarType;
			if (LScalarType != null)
				FHasDefault = ((LScalarType.Default != null) || (LScalarType.HasHandlers(APlan.ServerProcess, EventType.Default)));
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			LocalBuilder LLocal = AGenerator.DeclareLocal(typeof(DataVar));
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			
			// Create the new variable			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(VariableNode).GetField("FVariableName", BindingFlags.Instance | BindingFlags.NonPublic));
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(VariableNode).GetField("FVariableType", BindingFlags.Instance | BindingFlags.NonPublic));
			
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(String), typeof(Schema.IDataType) }));
			AGenerator.Emit(OpCodes.Stloc, LLocal);
			
			// Push the new variable on to the stack
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldloc, LLocal);
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Push", new Type[] { typeof(DataVar) }));
			
			if (Nodes.Count > 0)
			{
				LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataVar));
				EmitEvaluate(APlan, AGenerator, LExecutePath, 0);
				AGenerator.Emit(OpCodes.Stloc, LResult);
				
				if (FVariableType is Schema.IScalarType)
				{
					AGenerator.Emit(OpCodes.Ldloc, LResult);
					AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("DataType"));
					AGenerator.Emit(OpCodes.Ldarg_1);
					AGenerator.Emit(OpCodes.Ldloc, LResult);
					AGenerator.Emit(OpCodes.Call, typeof(Schema.ScalarType).GetMethod("ValidateValue", new Type[] { typeof(ServerProcess), typeof(DataVar) }));
				}
				
				AGenerator.Emit(OpCodes.Ldloc, LLocal);
				AGenerator.Emit(OpCodes.Ldloc, LResult);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
				AGenerator.Emit(OpCodes.Stfld, typeof(DataVar).GetField("Value"));
			}
			else
			{
				if (FHasDefault && (FVariableType is Schema.IScalarType))
				{
					LocalBuilder LResult = AGenerator.DeclareLocal(typeof(DataVar));
					AGenerator.Emit(OpCodes.Ldloc, LLocal);
					AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("DataType"));
					AGenerator.Emit(OpCodes.Ldarg_1);
					AGenerator.Emit(OpCodes.Call, typeof(Schema.ScalarType).GetMethod("DefaultValue", new Type[] { typeof(ServerProcess) }));
					AGenerator.Emit(OpCodes.Stloc, LResult);
					
					Label LDone = AGenerator.DefineLabel();
					
					AGenerator.Emit(OpCodes.Ldloc, LResult);
					AGenerator.Emit(OpCodes.Brfalse, LDone);
					
					AGenerator.Emit(OpCodes.Ldloc, LLocal);
					AGenerator.Emit(OpCodes.Ldloc, LResult);
					AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
					AGenerator.Emit(OpCodes.Stfld, typeof(DataVar).GetField("Value"));
					
					AGenerator.MarkLabel(LDone);
				}
			}
		}
*/
		
		// Note that initialization is more efficient than the equivalent declaration / assignment construct 
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context.Push(null);
			int LStackDepth = AProcess.Context.Count;

			if (Nodes.Count > 0)
			{
				object LValue = Nodes[0].Execute(AProcess);
				if (Nodes[0].DataType is Schema.ScalarType)
					LValue = ((Schema.ScalarType)Nodes[0].DataType).ValidateValue(AProcess, LValue);
				AProcess.Context.Poke(AProcess.Context.Count - LStackDepth, LValue);
			}
			else
			{
				if (FHasDefault && (VariableType is Schema.ScalarType))
					AProcess.Context.Poke(0, ((Schema.ScalarType)VariableType).DefaultValue(AProcess));
			}


			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			VariableStatement LStatement = new VariableStatement();
			LStatement.VariableName = new IdentifierExpression(VariableName);
			LStatement.TypeSpecifier = VariableType.EmitSpecifier(AMode);
			if (Nodes.Count > 0)
				LStatement.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			return LStatement;
		}
	}
	
	public class DropVariableNode : PlanNode
	{
		public DropVariableNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			APlan.Symbols.Pop();
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Pop", new Type[] { }));
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			AProcess.Context.Pop();
			return null;
		}
	}
	
	public class DeallocateVariableNode : PlanNode
	{
		public DeallocateVariableNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public DeallocateVariableNode(int ALocation) : base()
		{
			Location = ALocation;
			ShouldEmitIL = true;
		}
		
		public int Location;

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			LocalBuilder LValue = AGenerator.DeclareLocal(typeof(DataValue));
			Label LDone = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(DeallocateVariableNode).GetField("Location"));
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("get_Item", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stloc, LValue);
			
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Brfalse, LDone);
			
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Call, typeof(IDisposable).GetMethod("Dispose", new Type[] { }));
			
			AGenerator.MarkLabel(LDone);
		}
*/
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			DataValue.DisposeValue(AProcess, AProcess.Context[Location]);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new EmptyStatement();
		}
	}
	
	public class NoOpNode : PlanNode
	{
		public NoOpNode() : base()
		{
			ShouldEmitIL = true;
		}

		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			AGenerator.Emit(OpCodes.Nop);
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			return null;
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			return new EmptyStatement();
		}
	}
	
	public class AssignmentNode : PlanNode
	{
		public AssignmentNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public AssignmentNode(PlanNode ATargetNode, PlanNode AValueNode) : base()
		{
			Nodes.Add(ATargetNode);
			Nodes.Add(AValueNode);
			ShouldEmitIL = true;
		}

		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			int LLocation = ((StackReferenceNode)Nodes[0]).Location;
			Symbol LSymbol = APlan.Symbols[LLocation];
			if (LSymbol.IsConstant)
				throw new CompilerException(CompilerException.Codes.ConstantObjectCannotBeAssigned, APlan.CurrentStatement(), LSymbol.Name);
			APlan.Symbols.SetIsModified(LLocation);
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			int[] LExecutePath = PrepareExecutePath(APlan, AExecutePath);
			LocalBuilder LValue = AGenerator.DeclareLocal(typeof(DataVar));
			
			EmitEvaluate(APlan, AGenerator, LExecutePath, 1);
			AGenerator.Emit(OpCodes.Stloc, LValue);
			
			if (Nodes[1].DataType is Schema.ScalarType)
			{
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("DataType"));
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Call, typeof(Schema.ScalarType).GetMethod("ValidateValue", new Type[] { typeof(ServerProcess), typeof(DataVar) }));
			}
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldc_I4, ((StackReferenceNode)Nodes[0]).Location);
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("Peek", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Ldloc, LValue);
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Stfld, typeof(DataVar).GetField("Value"));
		}
*/

		public override object InternalExecute(ServerProcess AProcess)
		{
			object LObject = Nodes[1].Execute(AProcess);
			if (Nodes[1].DataType is Schema.ScalarType)
				LObject = ((Schema.ScalarType)Nodes[1].DataType).ValidateValue(AProcess, LObject);
			AProcess.Context.Poke(((StackReferenceNode)Nodes[0]).Location, LObject);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AssignmentStatement LStatement = new AssignmentStatement();
			LStatement.Target = (Expression)Nodes[0].EmitStatement(AMode);
			LStatement.Expression = (Expression)Nodes[1].EmitStatement(AMode);
			return LStatement;
		}
	}
	
    // Nodes[0] = If condition (must be boolean)
    // Nodes[1] = True expression
    // Nodes[2] = False expression (must be the same type as the true expression)
    public class ConditionNode : PlanNode
    {
		// DetermineDataType
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			if (!Nodes[0].DataType.Is(APlan.Catalog.DataTypes.SystemBoolean))
				throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, APlan.CurrentStatement());
				
			if (Nodes[2].DataType.Is(Nodes[1].DataType))
			{
				FDataType = Nodes[1].DataType;
				Nodes[2] = Compiler.Upcast(APlan, Nodes[2], FDataType);
			}
			else if (Nodes[1].DataType.Is(Nodes[2].DataType))
			{
				FDataType = Nodes[2].DataType;
				Nodes[1] = Compiler.Upcast(APlan, Nodes[1], FDataType);
			}
			else
			{
				ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[2].DataType, Nodes[1].DataType);
				if (LContext.CanConvert)
				{
					FDataType = Nodes[1].DataType;
					Nodes[2] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[2], LContext), Nodes[1].DataType);
				}
				else
				{
					LContext = Compiler.FindConversionPath(APlan, Nodes[1].DataType, Nodes[2].DataType);
					if (LContext.CanConvert)
					{
						FDataType = Nodes[2].DataType;
						Nodes[1] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[1], LContext), Nodes[2].DataType);
					}
					else
						Compiler.CheckConversionContext(APlan, LContext);
				}
			}
		}
		
		// Execute
		public override object InternalExecute(ServerProcess AProcess)
		{
			bool LValue = false;
			object LObject = Nodes[0].Execute(AProcess);
			#if NILPROPOGATION
			if ((LObject == null))
				LValue = false;
			else
			#endif
				LValue = (bool)LObject;

			if (LValue)
				return Nodes[1].Execute(AProcess);
			else
				return Nodes[2].Execute(AProcess);
		}
		
		// EmitStatement
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new IfExpression((Expression)Nodes[0].EmitStatement(AMode), (Expression)Nodes[1].EmitStatement(AMode), (Expression)Nodes[2].EmitStatement(AMode));
		}
    }

	public class ConditionedCaseNode : PlanNode
	{
		// DetermineDataType
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);

			FDataType = Nodes[0].Nodes[1].DataType;
			for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
			{
				int LNodeIndex = Nodes[LIndex].Nodes.Count - 1;
				if (Nodes[LIndex].Nodes[LNodeIndex].DataType.Is(Nodes[0].Nodes[1].DataType))
					Nodes[LIndex].Nodes[LNodeIndex] = Compiler.Upcast(APlan, Nodes[LIndex].Nodes[LNodeIndex], FDataType);
				else
				{	
					ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[LIndex].Nodes[LNodeIndex].DataType, FDataType);
					if (LContext.CanConvert)
						Nodes[LIndex].Nodes[LNodeIndex] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[LIndex].Nodes[LNodeIndex], LContext), FDataType);
					else
						Compiler.CheckConversionContext(APlan, LContext);
				}
			}
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			foreach (ConditionedCaseItemNode LNode in Nodes)
			{
				if (LNode.Nodes.Count == 2)
				{
					bool LValue = false;
					object LObject = LNode.Nodes[0].Execute(AProcess);
					#if NILPROPOGATION
					if ((LObject == null))
						LValue = false;
					else
					#endif
						LValue = (bool)LObject;
					
					if (LValue)
						return LNode.Nodes[1].Execute(AProcess);
				}
				else
					return LNode.Nodes[0].Execute(AProcess);
			}
			
			return null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			CaseExpression LCaseExpression = new CaseExpression();
			foreach (ConditionedCaseItemNode LNode in Nodes)
			{
				if (LNode.Nodes.Count == 2)
					LCaseExpression.CaseItems.Add(new CaseItemExpression((Expression)LNode.Nodes[0].EmitStatement(AMode), (Expression)LNode.Nodes[1].EmitStatement(AMode)));
				else
					LCaseExpression.ElseExpression = new CaseElseExpression((Expression)LNode.Nodes[0].EmitStatement(AMode));
			}
			return LCaseExpression;
		}
	}
	
	// Nodes[0] -> Selector expression
	// Nodes[1] -> Selector equality node
	// Nodes[2..N] -> case item nodes
		// Nodes[0] -> case item when expression
		// Nodes[1] -> case item then expression
	public class SelectedConditionedCaseNode : PlanNode
	{
		// DetermineDataType
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = Nodes[2].Nodes[1].DataType;
			for (int LIndex = 3; LIndex < Nodes.Count; LIndex++)
			{
				int LNodeIndex = Nodes[LIndex].Nodes.Count - 1;
				if (Nodes[LIndex].Nodes[LNodeIndex].DataType.Is(FDataType))
					Nodes[LIndex].Nodes[LNodeIndex] = Compiler.Upcast(APlan, Nodes[LIndex].Nodes[LNodeIndex], FDataType);
				else
				{	
					ConversionContext LContext = Compiler.FindConversionPath(APlan, Nodes[LIndex].Nodes[LNodeIndex].DataType, FDataType);
					if (LContext.CanConvert)
						Nodes[LIndex].Nodes[LNodeIndex] = Compiler.Upcast(APlan, Compiler.ConvertNode(APlan, Nodes[LIndex].Nodes[LNodeIndex], LContext), FDataType);
					else
						Compiler.CheckConversionContext(APlan, LContext);
				}
			}
		}
		
		public override void DetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			// Do not bind node 1, it will fail (it contains nameless stack references)
			for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
				Nodes[LIndex].DetermineBinding(APlan);
		}

		public override object InternalExecute(ServerProcess AProcess)
		{
			object LSelector = Nodes[0].Execute(AProcess);
			try
			{
				for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
				{
					ConditionedCaseItemNode LNode = (ConditionedCaseItemNode)Nodes[LIndex];
					if (LNode.Nodes.Count == 2)
					{
						bool LValue = false;
						object LWhenVar = LNode.Nodes[0].Execute(AProcess);
						try
						{
							AProcess.Context.Push(LSelector);
							try
							{
								AProcess.Context.Push(LWhenVar);
								try
								{
									object LObject = Nodes[1].Execute(AProcess);
									#if NILPROPOGATION
									if ((LObject == null))
										LValue = false;
									else
									#endif
										LValue = (bool)LObject;
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
						finally
						{
							DataValue.DisposeValue(AProcess, LWhenVar);
						}

						if (LValue)
							return LNode.Nodes[1].Execute(AProcess);
					}
					else
						return LNode.Nodes[0].Execute(AProcess);
				}
			}
			finally
			{
				DataValue.DisposeValue(AProcess, LSelector);
			}
			
			return null;
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			CaseExpression LCaseExpression = new CaseExpression();
			LCaseExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			for (int LIndex = 2; LIndex < Nodes.Count; LIndex++)
			{
				ConditionedCaseItemNode LNode = (ConditionedCaseItemNode)Nodes[LIndex];
				if (LNode.Nodes.Count == 2)
					LCaseExpression.CaseItems.Add(new CaseItemExpression((Expression)LNode.Nodes[0].EmitStatement(AMode), (Expression)LNode.Nodes[1].EmitStatement(AMode)));
				else
					LCaseExpression.ElseExpression = new CaseElseExpression((Expression)LNode.Nodes[0].EmitStatement(AMode));
			}
			return LCaseExpression;
		}
	}
	
	public class ConditionedCaseItemNode : PlanNode
	{
		public override object InternalExecute(ServerProcess AProcess)
		{
			return null;
		}
	}

	public abstract class VarReferenceNode : PlanNode
	{
		public VarReferenceNode() : base()
		{
			FIsLiteral = false;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsOrderPreserving = true;
		}
		
		public VarReferenceNode(Schema.IDataType ADataType) : base()
		{
			FDataType = ADataType;
			FIsLiteral = false;
			FIsFunctional = true;
			FIsDeterministic = true;
			FIsRepeatable = true;
			FIsOrderPreserving = true;
		}
	}
	
    public class StackReferenceNode : VarReferenceNode
    {
		// constructor
		public StackReferenceNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(Schema.IDataType ADataType, int ALocation) : base(ADataType)
		{
			Location = ALocation;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(Schema.IDataType ADataType, int ALocation, bool AByReference) : base(ADataType)
		{
			Location = ALocation;
			ByReference = AByReference;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(string AIdentifier, Schema.IDataType ADataType, int ALocation) : base(ADataType)
		{
			Identifier = AIdentifier;
			Location = ALocation;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(string AIdentifier, Schema.IDataType ADataType, int ALocation, bool AByReference) : base(ADataType)
		{
			Identifier = AIdentifier;
			Location = ALocation;
			ByReference = AByReference;
			ShouldEmitIL = true;
		}
		
		// Identifier
		protected string FIdentifier = String.Empty;
		public string Identifier
		{
			get { return FIdentifier; }
			set { FIdentifier = value == null ? String.Empty : value; }
		}
		
		// Location
		public int Location = -1;
		
		// ByReference
		public bool ByReference;
		
		// Statement
		public override Statement EmitStatement(EmitMode AMode)
		{
			return new VariableIdentifierExpression(FIdentifier);
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			int LColumnIndex;
			Location = Compiler.ResolveVariableIdentifier(APlan, FIdentifier, out LColumnIndex);
			if (Location < 0)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, FIdentifier);
				
			if (LColumnIndex >= 0)
				throw new CompilerException(CompilerException.Codes.InvalidColumnBinding, FIdentifier);
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			if (ByReference)
			{
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
				AGenerator.Emit(OpCodes.Ldc_I4, Location);
				AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("get_Item", new Type[] { typeof(int) }));
			}
			else
			{
				LocalBuilder LValue = AGenerator.DeclareLocal(typeof(DataVar));
				
				AGenerator.Emit(OpCodes.Ldarg_1);
				AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
				AGenerator.Emit(OpCodes.Ldc_I4, Location);
				AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("get_Item", new Type[] { typeof(int) }));
				AGenerator.Emit(OpCodes.Stloc, LValue);
				
				Label LNull = AGenerator.DefineLabel();
				Label LEnd = AGenerator.DefineLabel();
				
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
				AGenerator.Emit(OpCodes.Brfalse, LNull);
				
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("DataType"));
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
				AGenerator.Emit(OpCodes.Call, typeof(DataValue).GetMethod("Copy", new Type[] { }));
				AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
				
				AGenerator.Emit(OpCodes.Br, LEnd);
				
				AGenerator.MarkLabel(LNull);
				
				AGenerator.Emit(OpCodes.Ldloc, LValue);
				AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("DataType"));
				AGenerator.Emit(OpCodes.Ldnull);
				AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
				
				AGenerator.MarkLabel(LEnd);
			}
		}
*/
		
		// Execute
		public override object InternalExecute(ServerProcess AProcess)
		{
			if (ByReference)
				return AProcess.Context[Location];
			else
				return DataValue.CopyValue(AProcess, AProcess.Context[Location]);
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			AWriter.WriteAttributeString("Identifier", Identifier);
			AWriter.WriteAttributeString("StackIndex", Location.ToString());
			AWriter.WriteAttributeString("ByReference", Convert.ToString(ByReference));
		}

		public override bool IsContextLiteral(int ALocation)
		{
			if (Location == ALocation)
				return false;
			return true;
		}
    }
    
    public class StackColumnReferenceNode : VarReferenceNode
    {
		// constructor
		#if USECOLUMNLOCATIONBINDING
		public StackColumnReferenceNode() : base(){}
		public StackColumnReferenceNode(Schema.IDataType ADataType, int ALocation, int AColumnLocation) : base(ADataType)
		{
			Location = ALocation;
			ColumnLocation = AColumnLocation;
		}
		
		public StackColumnReferenceNode(string AIdentifier, Schema.IDataType ADataType, int ALocation, int AColumnLocation) : base(ADataType)
		{
			Identifier = AIdentifier;
			Location = ALocation;
			ColumnLocation = AColumnLocation;
		}
		#else
		public StackColumnReferenceNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public StackColumnReferenceNode(string AIdentifier, Schema.IDataType ADataType, int ALocation) : base(ADataType)
		{
			Identifier = AIdentifier;
			Location = ALocation;
			ShouldEmitIL = true;
		}
		#endif
		
		// Identifier
		private string FIdentifier;
		public string Identifier
		{
			get { return FIdentifier; }
			set 
			{ 
				FIdentifier = value;
				#if !USECOLUMNLOCATIONBINDING
				SetResolvingIdentifier();
				#endif
			}
		}

		#if !USECOLUMNLOCATIONBINDING		
		private string FResolvingIdentifier;
		private void SetResolvingIdentifier()
		{
			switch (Schema.Object.Qualifier(Schema.Object.EnsureUnrooted(Identifier)))
			{
				case Keywords.Parent :
				case Keywords.Left :
				case Keywords.Right :
				case Keywords.Source :
					FResolvingIdentifier = Schema.Object.Dequalify(Schema.Object.EnsureUnrooted(Identifier)); break;
				
				default : FResolvingIdentifier = Identifier; break;
			}
		}
		#endif
		
		// Location
		public int Location = -1;

		// ColumnLocation
		#if USECOLUMNLOCATIONBINDING
		public int ColumnLocation;
		#endif

		// ByReference
		public bool ByReference;
		
		// Statement
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (Schema.Object.Qualifier(Schema.Object.EnsureUnrooted(Identifier)) == Keywords.Parent)
				return new ExplodeColumnExpression(FResolvingIdentifier);
			else
				return new ColumnIdentifierExpression(FResolvingIdentifier);
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			#if USECOLUMNLOCATIONBINDING
			Location = Compiler.ResolveVariableIdentifier(APlan, Identifier, out ColumnLocation);
			if ((Location < 0) || (ColumnLocation < 0))
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, Identifier);
			#else
			int LColumnIndex;
			Location = Compiler.ResolveVariableIdentifier(APlan, Identifier, out LColumnIndex);
			if ((Location < 0) || (LColumnIndex < 0))
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, Identifier);
			#endif
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			LocalBuilder LThis = EmitThis(APlan, AGenerator, AExecutePath);
			LocalBuilder LRow = AGenerator.DeclareLocal(typeof(Row));
			
			AGenerator.Emit(OpCodes.Ldarg_1);
			AGenerator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			AGenerator.Emit(OpCodes.Ldc_I4, Location);
			AGenerator.Emit(OpCodes.Call, typeof(Context).GetMethod("get_Item", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Ldfld, typeof(DataVar).GetField("Value"));
			AGenerator.Emit(OpCodes.Castclass, typeof(Row));
			AGenerator.Emit(OpCodes.Stloc, LRow);
			
			Label LNull = AGenerator.DefineLabel();
			Label LNotNull = AGenerator.DefineLabel();
			Label LNoValue = AGenerator.DefineLabel();
			Label LEnd = AGenerator.DefineLabel();
			
			AGenerator.Emit(OpCodes.Ldloc, LRow);
			AGenerator.Emit(OpCodes.Brfalse, LNull);
			
			AGenerator.Emit(OpCodes.Ldloc, LRow);
			AGenerator.Emit(OpCodes.Callvirt, typeof(DataValue).GetProperty("IsNil").GetGetMethod());
			AGenerator.Emit(OpCodes.Brtrue, LNull);
			
			AGenerator.Emit(OpCodes.Br, LNotNull);
			
			AGenerator.MarkLabel(LNull);
			
			AGenerator.Emit(OpCodes.Ldloc, LThis);
			AGenerator.Emit(OpCodes.Ldfld, typeof(PlanNode).GetField("FDataType", BindingFlags.NonPublic | BindingFlags.Instance));
			AGenerator.Emit(OpCodes.Ldnull);
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
			
			AGenerator.Emit(OpCodes.Br, LEnd);
			
			AGenerator.MarkLabel(LNotNull);
			
			LocalBuilder LColumnIndex = AGenerator.DeclareLocal(typeof(int));
			LocalBuilder LColumns = AGenerator.DeclareLocal(typeof(Schema.Columns));
			
			AGenerator.Emit(OpCodes.Ldloc, LRow);
			AGenerator.Emit(OpCodes.Call, typeof(Row).GetProperty("DataType", BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).GetGetMethod());
			AGenerator.Emit(OpCodes.Call, typeof(Schema.RowType).GetProperty("Columns").GetGetMethod());
			AGenerator.Emit(OpCodes.Stloc, LColumns);
			AGenerator.Emit(OpCodes.Ldloc, LColumns);
			AGenerator.Emit(OpCodes.Ldstr, FResolvingIdentifier);
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Columns).GetMethod("IndexOf", new Type[] { typeof(string) }));
			AGenerator.Emit(OpCodes.Stloc, LColumnIndex);

			AGenerator.Emit(OpCodes.Ldloc, LRow);
			AGenerator.Emit(OpCodes.Ldloc, LColumnIndex);
			AGenerator.Emit(OpCodes.Call, typeof(Row).GetMethod("HasValue", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Brfalse, LNoValue);
			
			AGenerator.Emit(OpCodes.Ldloc, LColumns);
			AGenerator.Emit(OpCodes.Ldloc, LColumnIndex);
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Columns).GetMethod("get_Item", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Column).GetProperty("DataType").GetGetMethod());
			
			AGenerator.Emit(OpCodes.Ldloc, LRow);
			AGenerator.Emit(OpCodes.Ldloc, LColumnIndex);
			AGenerator.Emit(OpCodes.Call, typeof(Row).GetMethod("get_Item", new Type[] { typeof(int) } ));
			if (!ByReference)
				AGenerator.Emit(OpCodes.Call, typeof(DataValue).GetMethod("Copy", new Type[] { }));
				
			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));
			AGenerator.Emit(OpCodes.Br, LEnd);
			
			AGenerator.MarkLabel(LNoValue);
			
			AGenerator.Emit(OpCodes.Ldloc, LColumns);
			AGenerator.Emit(OpCodes.Ldloc, LColumnIndex);
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Columns).GetMethod("get_Item", new Type[] { typeof(int) }));
			AGenerator.Emit(OpCodes.Call, typeof(Schema.Column).GetProperty("DataType").GetGetMethod());

			AGenerator.Emit(OpCodes.Ldnull);

			AGenerator.Emit(OpCodes.Newobj, typeof(DataVar).GetConstructor(new Type[] { typeof(Schema.IDataType), typeof(DataValue) }));

			AGenerator.MarkLabel(LEnd);
		}
*/
		
		// Execute
		public override object InternalExecute(ServerProcess AProcess)
		{
			Row LRow = (Row)AProcess.Context[Location];
			#if NILPROPOGATION
			if ((LRow == null) || LRow.IsNil)
				return null;
			#endif

			#if USECOLUMNLOCATIONBINDING
			if (LRow.HasValue(ColumnLocation))
				return ByReference ? LRow[ColumnLocation] : DataValue.CopyValue(AProcess, LRow[ColumnLocation]);
			else
				return null;
			#else
			int LColumnIndex = LRow.DataType.Columns.IndexOf(FResolvingIdentifier);
			if (LColumnIndex < 0)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, FResolvingIdentifier);
			if (LRow.HasValue(LColumnIndex))
				return ByReference ? LRow[LColumnIndex] : DataValue.CopyValue(AProcess, LRow[LColumnIndex]);
			else
				return null;
			#endif
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			AWriter.WriteAttributeString("ColumnName", Identifier);
			AWriter.WriteAttributeString("StackIndex", Location.ToString());
		}

		public override bool IsContextLiteral(int ALocation)
		{
			if (Location == ALocation)
				return false;
			return true;
		}
    }

    public class PropertyReferenceNode : VarReferenceNode
    {
		public PropertyReferenceNode() : base(){}
		public PropertyReferenceNode(Schema.IDataType ADataType, Schema.ScalarType AScalarType, PlanNode AValueNode, int ARepresentationIndex, int APropertyIndex) : base(((Schema.ScalarType)ADataType).Representations[ARepresentationIndex].Properties[APropertyIndex].DataType)
		{
			Nodes.Add(AValueNode);
			FScalarType = AScalarType;
			FRepresentationIndex = ARepresentationIndex;
			FPropertyIndex = APropertyIndex;
		}
		
		private Schema.ScalarType FScalarType;
		public Schema.ScalarType ScalarType
		{
			get { return FScalarType; }
			set { FScalarType = value; }
		}
		
		private int FRepresentationIndex;
		public int RepresentationIndex
		{
			get { return FRepresentationIndex; }
			set { FRepresentationIndex = value; }
		}
		
		private int FPropertyIndex;
		public int PropertyIndex
		{
			get { return FPropertyIndex; }
			set { FPropertyIndex = value; }
		}
		
		public override object InternalExecute(ServerProcess AProcess)
		{
			throw new RuntimeException(RuntimeException.Codes.PropertyRefNodeExecuted);
		}
    }
}
