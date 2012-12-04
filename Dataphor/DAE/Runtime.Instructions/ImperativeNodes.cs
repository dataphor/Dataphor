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

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
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
		
		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Ldstr, "IL Generated exception");
			generator.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new Type[] { typeof(String) }));
			generator.Emit(OpCodes.Throw);
		}

		public override object NilaryInternalExecute(Program program)
		{
			throw new NotImplementedException();
		}
	}

	public class BlockNode : PlanNode
	{
		public BlockNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{	
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			
			for (int index = 0; index < NodeCount; index++)
				EmitExecute(plan, generator, localExecutePath, index);
		}

		public override object InternalExecute(Program program)
		{
			for (int index = 0; index < NodeCount; index++)
				Nodes[index].Execute(program);
			return null;
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			Block block = new Block();
			Statement statement;
			for (int index = 0; index < NodeCount; index++)
			{
				statement = Nodes[index].EmitStatement(mode);
				if (!(statement is EmptyStatement))
					block.Statements.Add(statement);
			}
			switch (block.Statements.Count)
			{
				case 0: return new EmptyStatement();
				case 1: return block.Statements[0];
				default: return block;
			}
		}
	}
	
	public class DelimitedBlockNode : PlanNode
	{
		public DelimitedBlockNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		public override object InternalExecute(Program program)
		{
			for (int index = 0; index < NodeCount; index++)
				Nodes[index].Execute(program);
			return null;
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{	
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			
			for (int index = 0; index < NodeCount; index++)
				EmitExecute(plan, generator, localExecutePath, index);
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			DelimitedBlock block = new DelimitedBlock();
			Statement statement;
			for (int index = 0; index < NodeCount; index++)
			{
				statement = Nodes[index].EmitStatement(mode);
				if (!(statement is EmptyStatement))
					block.Statements.Add(statement);
			}
			return block;
		}
	}
	
	public class FrameNode : PlanNode
	{
		public FrameNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.PushFrame();
			try
			{
				base.InternalBindingTraversal(plan, visitor);
			}
			finally
			{
				plan.Symbols.PopFrame();
			}
		}
		
		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{	
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			
			// Call AProcess.Context.PushFrame();
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			generator.Emit(OpCodes.Call, typeof(Stack).GetMethod("PushFrame", new Type[] { }));
			generator.BeginExceptionBlock();
			
			EmitExecute(plan, generator, localExecutePath, 0);

			// Call AProcess.Context.PopFrame()				
			generator.BeginFinallyBlock();
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			generator.Emit(OpCodes.Call, typeof(Stack).GetMethod("PopFrame"));
				
			generator.EndExceptionBlock();
		}

		public override object InternalExecute(Program program)
		{
			program.Stack.PushFrame();
			try
			{
				Nodes[0].Execute(program);
				return null;
			}
			finally
			{
				program.Stack.PopFrame();
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return Nodes[0].EmitStatement(mode);
		}
	}
	
	public class ExpressionStatementNode : PlanNode
	{
		public ExpressionStatementNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		public ExpressionStatementNode(PlanNode node) : base()
		{
			ShouldEmitIL = true;
			Nodes.Add(node);
		}
		
		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{	
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			EmitExecute(plan, generator, localExecutePath, 0);			
		}

		public override object InternalExecute(Program program)
		{
			object objectValue = Nodes[0].Execute(program);
			DataValue.DisposeValue(program.ValueManager, objectValue);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new ExpressionStatement((Expression)Nodes[0].EmitStatement(mode));
		}
	}
	
	public class ControlError : Exception
	{
		public ControlError() : base(){} 
	}

	public class ExitError : ControlError
	{
		public ExitError() : base(){}
	}
	
	public class ExitNode : PlanNode
	{
		public ExitNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Newobj, typeof(ExitError).GetConstructor(new Type[] { }));
			generator.Emit(OpCodes.Throw);
		}

		public override object InternalExecute(Program program)
		{
			throw new ExitError();
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new ExitStatement();
		}
	}
	
	public class WhileNode : PlanNode
	{
		public WhileNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
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
		
		public override object InternalExecute(Program program)
		{
			try
			{
				while (true)
				{
					try
					{
						object objectValue = Nodes[0].Execute(program);
						if ((objectValue == null) || !(bool)objectValue)
							break;
							
						Nodes[1].Execute(program);
					}
					catch (ContinueError) { }
				}
			}
			catch (BreakError) { }
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			WhileStatement statement = new WhileStatement();
			statement.Condition = (Expression)Nodes[0].EmitStatement(mode);
			statement.Statement = Nodes[1].EmitStatement(mode);
			return statement;
		}
	}
	
	public class DoWhileNode : PlanNode
	{
		public DoWhileNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
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
		
		public override object InternalExecute(Program program)
		{
			try
			{
				while (true)
				{
					try
					{
						Nodes[0].Execute(program);
						object objectValue = Nodes[1].Execute(program);
						if ((objectValue == null) || !(bool)objectValue)
							break;
					}
					catch (ContinueError){}
				}
			}
			catch (BreakError){}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			DoWhileStatement statement = new DoWhileStatement();
			statement.Statement = Nodes[0].EmitStatement(mode);
			statement.Condition = (Expression)Nodes[1].EmitStatement(mode);
			return statement;
		}
	}

	// ForEachNode
	//	Nodes[0] - Iteration Expression
	//	Nodes[1] - Iteration Statement
	public class ForEachNode : PlanNode
	{
		public ForEachNode() : base()
		{
			IsBreakable = true;
		}
		
		private ForEachStatement _statement;
		public ForEachStatement Statement
		{
			get { return _statement; }
			set { _statement = value; }
		}
		
		private Schema.IDataType _variableType;
		public Schema.IDataType VariableType
		{
			get { return _variableType; }
			set { _variableType = value; }
		}
		
		private int _location;
		public int Location
		{
			get { return _location; }
			set { _location = value; }
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			Nodes[0].BindingTraversal(plan, visitor);
			if (_statement.VariableName == String.Empty)
				plan.EnterRowContext();
			try
			{
				if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
					plan.Symbols.Push(new Symbol(_statement.VariableName, _variableType));
				try
				{
					if ((_statement.VariableName != String.Empty) && !_statement.IsAllocation)
					{
						int columnIndex;
						Location = Compiler.ResolveVariableIdentifier(plan, _statement.VariableName, out columnIndex);
						if (Location < 0)
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, _statement.VariableName);
							
						if (columnIndex >= 0)
							throw new CompilerException(CompilerException.Codes.InvalidColumnBinding, _statement.VariableName);
					}

					Nodes[1].BindingTraversal(plan, visitor);
				}
				finally
				{
					if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
						plan.Symbols.Pop();
				}
			}
			finally
			{
				if (_statement.VariableName == String.Empty)
					plan.ExitRowContext();
			}
		}
		
		private bool CursorNext(Program program, Cursor cursor)
		{
			cursor.SwitchContext(program);
			try
			{
				return cursor.Table.Next();
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
		
		private void CursorSelect(Program program, Cursor cursor, Row row)
		{
			cursor.SwitchContext(program);
			try
			{
				cursor.Table.Select(row);
			}
			finally
			{
				cursor.SwitchContext(program);
			}
		}
		
		public override object InternalExecute(Program program)
		{
			if (Nodes[0].DataType is Schema.ICursorType)
			{
				CursorValue cursorValue = (CursorValue)Nodes[0].Execute(program);
				Cursor cursor = program.CursorManager.GetCursor(cursorValue.ID);
				try
				{
					int stackIndex = 0;
					if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
						program.Stack.Push(null);
					else
						stackIndex = Location;
					try
					{
						using (Row row = new Row(program.ValueManager, (Schema.IRowType)_variableType))
						{
							program.Stack.Poke(stackIndex, row);
							try
							{
								while (CursorNext(program, cursor))
								{
									try
									{
										// Select row...
										CursorSelect(program, cursor, row);
										Nodes[1].Execute(program);
									}
									catch (ContinueError) {}
								}
							}
							finally
							{
								program.Stack.Poke(stackIndex, null); // TODO: Stack imbalance if the iteration statement allocates a variable
							}
						}
					}
					finally
					{
						if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
							program.Stack.Pop();
					}
				}
				catch (BreakError) {}
				finally
				{
					program.CursorManager.CloseCursor(cursorValue.ID);
				}
			}
			else
			{
				ListValue tempValue = (ListValue)Nodes[0].Execute(program);
				if (tempValue != null)
				{
					try
					{
						int stackIndex = 0;
						if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
							program.Stack.Push(null);
						else
							stackIndex = Location;
						
						try
						{
							for (int index = 0; index < tempValue.Count(); index++)
							{
								try
								{
									// Select iteration value
									program.Stack.Poke(stackIndex, tempValue[index]);
									Nodes[1].Execute(program);
								}
								catch (ContinueError) {}
							}
						}
						finally
						{
							if ((_statement.VariableName == String.Empty) || _statement.IsAllocation)
								program.Stack.Pop();
						}
					}
					catch (BreakError) {}
				}
			}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			ForEachStatement statement = new ForEachStatement();
			statement.IsAllocation = _statement.IsAllocation;
			statement.VariableName = _statement.VariableName;
			if (Nodes[0] is CursorNode)
			{
				CursorSelectorExpression cursorSelectorExpression = (CursorSelectorExpression)Nodes[0].EmitStatement(mode);
				statement.Expression = cursorSelectorExpression.CursorDefinition;
			}
			else
				statement.Expression = new CursorDefinition((Expression)Nodes[0].EmitStatement(mode));
			
			statement.Statement = Nodes[1].EmitStatement(mode);	
			
			return statement;
		}
	}

	public class BreakError : ControlError
	{
		public BreakError() : base(){}
	}
	
	public class BreakNode : PlanNode
	{
		public BreakNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Newobj, typeof(BreakError).GetConstructor(new Type[] { }));
			generator.Emit(OpCodes.Throw);
		}
		
		public override object InternalExecute(Program program)
		{
			throw new BreakError();
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new BreakStatement();
		}
	}

	public class ContinueError : ControlError
	{
		public ContinueError() : base(){}
	}
	
	public class ContinueNode : PlanNode
	{
		public ContinueNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Newobj, typeof(ContinueError).GetConstructor(new Type[] { }));
			generator.Emit(OpCodes.Throw);
		}
		
		public override object InternalExecute(Program program)
		{
			throw new ContinueError();
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new ContinueStatement();
		}
	}

	public class RaiseNode : PlanNode
	{
		public RaiseNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}

/*
		public override void EmitIL(Plan APlan, ILGenerator AGenerator, int[] AExecutePath)
		{
			if (NodeCount > 0)
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
		
		public override object InternalExecute(Program program)
		{
			if (NodeCount > 0)
				program.Stack.ErrorVar = Nodes[0].Execute(program);
			if (program.Stack.ErrorVar == null)
				throw new RuntimeException(RuntimeException.Codes.NilEncountered);
			program.ReportThrow();
			throw (Exception)program.Stack.ErrorVar;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			RaiseStatement statement = new RaiseStatement();
			if (NodeCount > 0)
				statement.Expression = (Expression)Nodes[0].EmitStatement(mode);
			return statement;
		}
	}
	
	public class TryFinallyNode : PlanNode
	{
		public TryFinallyNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			
			generator.BeginExceptionBlock();

			// Execute the protected statement
			EmitExecute(plan, generator, localExecutePath, 0);
				
			generator.BeginFinallyBlock();

			// Execute the finally statement
			EmitExecute(plan, generator, localExecutePath, 1);
				
			generator.EndExceptionBlock();
		}
		
		public override object InternalExecute(Program program)
		{
			try
			{
				Nodes[0].Execute(program);
				return null;
			}
			finally
			{
				Nodes[1].Execute(program);
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			TryFinallyStatement statement = new TryFinallyStatement();
			statement.TryStatement = Nodes[0].EmitStatement(mode);
			statement.FinallyStatement = Nodes[1].EmitStatement(mode);
			return statement;
		}
	}
	
	public class ErrorHandlerNode : PlanNode
	{
		public ErrorHandlerNode() : base()
		{
		    ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		protected bool _isGeneric;
		public bool IsGeneric
		{
			get { return _isGeneric; }
			set { _isGeneric = value; }
		}
		
		protected Schema.IDataType _errorType;
		public Schema.IDataType ErrorType
		{
			get { return _errorType; }
			set { _errorType = value; }
		}
		
		protected string _variableName = String.Empty;
		public string VariableName
		{
			get { return _variableName; }
			set { _variableName = value == null ? String.Empty : value; }
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.PushFrame();
			try
			{
				if (_variableName != String.Empty)
					plan.Symbols.Push(new Symbol(_variableName, _errorType));
				base.InternalBindingTraversal(plan, visitor);
			}
			finally
			{
				plan.Symbols.PopFrame();
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
		
		public override object InternalExecute(Program program)
		{
			program.Stack.PushFrame();
			try
			{
				if (_variableName != String.Empty)
					program.Stack.Push(DataValue.CopyValue(program.ValueManager, program.Stack.ErrorVar));
				Nodes[0].Execute(program);	   
				return null;
			}
			finally
			{
				program.Stack.PopFrame();
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (IsGeneric)
			{
				GenericErrorHandler handler = new GenericErrorHandler();
				handler.Statement = Nodes[0].EmitStatement(mode);
				return handler;
			}
			else
			{
				if (VariableName != String.Empty)
				{
					ParameterizedErrorHandler handler = new ParameterizedErrorHandler(ErrorType.Name, VariableName);
					handler.Statement = Nodes[0].EmitStatement(mode);
					return handler;
				}
				else
				{
					SpecificErrorHandler handler = new SpecificErrorHandler(ErrorType.Name);
					handler.Statement = Nodes[0].EmitStatement(mode);
					return handler;
				}
			}
		}
	}
	
	public class TryExceptNode : PlanNode
	{
		public TryExceptNode() : base()
		{
			IsBreakable = true;
		}
		
		public static ErrorHandlerNode GetErrorHandlerNode(PlanNode node)
		{
			ErrorHandlerNode result = node as ErrorHandlerNode;
			if (result != null)
				return result;
			return GetErrorHandlerNode(node.Nodes[0]);
		}
		
		public override object InternalExecute(Program program)
		{
			try
			{
				Nodes[0].Execute(program);
			}
			catch (Exception exception)
			{
				// if this is a host exception, set the error variable
				if (program.Stack.ErrorVar == null)
					program.Stack.ErrorVar = exception;
					
				ErrorHandlerNode node;
				object errorVar = program.Stack.ErrorVar;
				for (int index = 1; index < NodeCount; index++)
				{
					node = GetErrorHandlerNode(Nodes[index]);
					if (program.DataTypes.SystemError.Is(node.ErrorType)) // TODO: No RTTI on the error
					{
						node.Execute(program);
						break;
					}
				}
				program.Stack.ErrorVar = null;
			}
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			TryExceptStatement statement = new TryExceptStatement();
			statement.TryStatement = Nodes[0].EmitStatement(mode);
			for (int index = 1; index < Nodes.Count; index++)
				statement.ErrorHandlers.Add((GenericErrorHandler)GetErrorHandlerNode(Nodes[index]).EmitStatement(mode));
			return statement;
		}
	}
	
	public class IfNode : PlanNode
	{
		public IfNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
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
		
		public override object InternalExecute(Program program)
		{
			object objectValue = Nodes[0].Execute(program);
			bool tempValue = false;
			#if NILPROPOGATION
			if ((objectValue == null))
				tempValue = false;
			else
			#endif
				tempValue = (bool)objectValue;
			if (tempValue)
				Nodes[1].Execute(program);
			else
				if (Nodes.Count > 2)
					Nodes[2].Execute(program);

			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			IfStatement statement = new IfStatement();
			statement.Expression = (Expression)Nodes[0].EmitStatement(mode);
			statement.TrueStatement = Nodes[1].EmitStatement(mode);
			if (Nodes.Count > 2)
				statement.FalseStatement = Nodes[2].EmitStatement(mode);
			return statement;
		}
	}
	
	public class CaseNode : PlanNode
	{
		public CaseNode() : base()
		{
			IsBreakable = true;
		}
		
		public override object InternalExecute(Program program)
		{
			foreach (CaseItemNode node in Nodes)
			{
				if (node.Nodes.Count == 2)
				{
					bool tempValue = false;
					object objectValue = node.Nodes[0].Execute(program);
					#if NILPROPOGATION
					if ((objectValue == null))
						tempValue = false;
					else
					#endif
						tempValue = (bool)objectValue;
					
					if (tempValue)
					{
						node.Nodes[1].Execute(program);
						break;
					}
				}
				else
				{
					node.Nodes[0].Execute(program);
					break;
				}
			}
			
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			CaseStatement caseStatement = new CaseStatement();
			foreach (CaseItemNode node in Nodes)
			{
				if (node.Nodes.Count == 2)
					caseStatement.CaseItems.Add(new CaseItemStatement((Expression)node.Nodes[0].EmitStatement(mode), node.Nodes[1].EmitStatement(mode)));
				else
					caseStatement.ElseStatement = node.Nodes[0].EmitStatement(mode);
			}
			return caseStatement;
		}
	}
	
	// Nodes[0] -> Selector expression
	// Nodes[1] -> Selector equality node
	// Nodes[2..N] -> case item nodes
		// Nodes[0] -> case item when expression
		// Nodes[1] -> case item then statement
	public class SelectedCaseNode : PlanNode
	{
		public SelectedCaseNode() : base()
		{
			IsBreakable = true;
		}

		// TODO: Change the way this compiles so that it doesn't use nameless stack references so that this binding override can be removed
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			Nodes[0].BindingTraversal(plan, visitor);
			// Do not bind node 1, it will fail (it contains nameless stack references)
			plan.Symbols.Push(new Symbol(String.Empty, Nodes[0].DataType));
			try
			{
				for (int index = 2; index < Nodes.Count; index++)
					Nodes[index].BindingTraversal(plan, visitor);
			}
			finally
			{
				plan.Symbols.Pop();
			}
		}

		public override object InternalExecute(Program program)
		{
			object selector = Nodes[0].Execute(program);
			try
			{
				program.Stack.Push(selector);
				try
				{
					for (int index = 2; index < Nodes.Count; index++)
					{
						CaseItemNode node = (CaseItemNode)Nodes[index];
						if (node.Nodes.Count == 2)
						{
							bool tempValue = false;
							object whenVar = node.Nodes[0].Execute(program);
							try
							{
								program.Stack.Push(whenVar);
								try
								{
									object objectValue = Nodes[1].Execute(program);
									#if NILPROPOGATION
									if (objectValue == null)
										tempValue = false;
									else
									#endif
										tempValue = (bool)objectValue;
								}
								finally
								{
									program.Stack.Pop();
								}
							}
							finally
							{
								DataValue.DisposeValue(program.ValueManager, whenVar);
							}

							if (tempValue)
							{
								node.Nodes[1].Execute(program);
								break;
							}
						}
						else
						{
							node.Nodes[0].Execute(program);
							break;
						}
					}
				}
				finally
				{	
					program.Stack.Pop();
				}
			}
			finally
			{
				DataValue.DisposeValue(program.ValueManager, selector);
			}
			
			return null;
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			CaseStatement caseStatement = new CaseStatement();
			caseStatement.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = 2; index < Nodes.Count; index++)
			{
				CaseItemNode node = (CaseItemNode)Nodes[index];
				if (node.Nodes.Count == 2)
					caseStatement.CaseItems.Add(new CaseItemStatement((Expression)node.Nodes[0].EmitStatement(mode), node.Nodes[1].EmitStatement(mode)));
				else
					caseStatement.ElseStatement = node.Nodes[0].EmitStatement(mode);
			}
			return caseStatement;
		}
	}
	
	public class CaseItemNode : PlanNode
	{
		public CaseItemNode() : base()
		{
			IsBreakable = true;
		}
		
		public override object InternalExecute(Program program)
		{
			return null;
		}
	}

	public class ValueNode : PlanNode
    {		
		// constructor
		public ValueNode() : base() 
		{
			_isLiteral = true;
			_isFunctional = true;
			_isDeterministic = true;
			_isRepeatable = true;
			_isNilable = false;
			ShouldEmitIL = true;
		}

		public ValueNode(Schema.IDataType dataType, object tempValue) : base()
		{
			_dataType = dataType;
			_value = tempValue;
			_isLiteral = true;
			_isFunctional = true;
			_isDeterministic = true;
			_isRepeatable = true;
			_isNilable = _value == null;
			ShouldEmitIL = true;
		}
		
		protected object _value;
		public object Value
		{
			get { return _value; }
			set 
			{ 
				_value = value; 
				_isNilable = _value == null;
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
		public override object InternalExecute(Program program)
		{
			return _value;		 
		}
		
		// EmitStatement
		public override Statement EmitStatement(EmitMode mode)
		{
			if (_value == null)
				return new ValueExpression(null, TokenType.Nil);
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemBooleanName))
				return new ValueExpression(_value, TokenType.Boolean);
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemLongName))
				return new ValueExpression(_value, TokenType.Integer);
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemIntegerName))
				return new ValueExpression(_value, TokenType.Integer);
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemDecimalName))
				return new ValueExpression(_value, TokenType.Decimal);
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemStringName))
				return new ValueExpression(_value, TokenType.String);
			#if USEISTRING
			else if (Schema.Object.NamesEqual(FDataType.Name, Schema.DataTypes.CSystemIString))
				return new ValueExpression(FValue, LexerToken.IString);
			#endif
			else if (Schema.Object.NamesEqual(_dataType.Name, Schema.DataTypes.SystemMoneyName))
				return new ValueExpression(_value, TokenType.Money);
			else
				throw new RuntimeException(RuntimeException.Codes.UnsupportedValueType, _dataType.Name);
		}
    }																			
    
	public class ParameterNode : PlanNode
	{
		public ParameterNode() : base()
		{
			ShouldEmitIL = true;
		}

		public ParameterNode(PlanNode node, Modifier modifier)
		{
			Nodes.Add(node);
			_modifier = modifier;
			ShouldEmitIL = true;
		}
		
		private Modifier _modifier;
		public Modifier Modifier
		{
			get { return _modifier; }
			set { _modifier = value; }
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			return new ParameterExpression(_modifier, (Expression)Nodes[0].EmitStatement(mode));
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = Nodes[0].DataType;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			if (Modifier == Modifier.Var)
				plan.PushCursorContext(new CursorContext(CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{
				base.InternalBindingTraversal(plan, visitor);
			}
			finally
			{
				if (Modifier == Modifier.Var)
					plan.PopCursorContext();
			}
		}
		
		public override void BindToProcess(Plan plan)
		{
			if (Modifier == Modifier.Var)
				plan.PushCursorContext(new CursorContext(CursorType.Static, CursorCapability.Navigable | CursorCapability.Updateable, CursorIsolation.Isolated));
			try
			{	
				base.BindToProcess(plan);
			}
			finally
			{
				if (Modifier == Modifier.Var)
					plan.PopCursorContext();
			}
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			int[] localExecutePath = PrepareExecutePath(plan, executePath);
			EmitEvaluate(plan, generator, localExecutePath, 0);
		}
		
		public override object InternalExecute(Program program)
		{
			return Nodes[0].Execute(program);
		}
	}
	
	// The CallNode is responsible for preparing the stack with the given arguments.
	// The CallNode is also responsible for the Result variable, if the call is a function.
	public class CallNode : InstructionNode
	{
		public CallNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		private PlanNode _allocateResultNode;
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);

			if (_dataType != null)
			{
				_allocateResultNode = new VariableNode(Keywords.Result, _dataType);
				_allocateResultNode.IsBreakable = false;
			}

			if (plan.IsEngine && (Modifiers != null))
			{
				var tableType = LanguageModifiers.GetModifier(Modifiers, "TableType", String.Empty);

				if (!String.IsNullOrEmpty(tableType))
				{
					_dataType = Compiler.CompileTypeSpecifier(plan, new Parser().ParseTypeSpecifier(tableType)) as Schema.TableType;
					if (_dataType == null)
						throw new CompilerException(CompilerException.Codes.TableTypeExpected);
				}
			}
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			bool saveIsInsert = plan.IsInsert;
			plan.IsInsert = false;
			try
			{
				base.InternalBindingTraversal(plan, visitor);
			}
			finally
			{
				plan.IsInsert = saveIsInsert;
			}

			for (int index = 0; index < Operator.Operands.Count; index++)
				plan.Symbols.Push(new Symbol(Operator.Operands[index].Name, Nodes[index].DataType));
				
			plan.Symbols.PushWindow(Operator.Operands.Count);
			try
			{
				if (_allocateResultNode != null)
					_allocateResultNode.BindingTraversal(plan, visitor);
			}
			finally
			{
				plan.Symbols.PopWindow();
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
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			for (int index = 0; index < Operator.Operands.Count; index++)
				program.Stack.Push(arguments[index]);

			// TODO: I am not sure this is necessary, the derived table var does not do this
			// All rights checking should be being performed at compile-time, so there should
			// be no reason to impersonate the operator owner at this point...				
			program.Plan.PushSecurityContext(new SecurityContext(Operator.Owner));
			try
			{
				bool saveIsInsert = program.ServerProcess.IsInsert;
				program.ServerProcess.IsInsert = false;
				try
				{
					ApplicationTransaction transaction = null;
					if ((program.ServerProcess.ApplicationTransactionID != Guid.Empty) && !Operator.ShouldTranslate)
						transaction = program.ServerProcess.GetApplicationTransaction();
					try
					{
						if (transaction != null)
							transaction.PushGlobalContext();
						try
						{
							// Prepare the result
							if (_allocateResultNode != null)
								_allocateResultNode.Execute(program);

							// Record the stack depth
							int stackDepth = program.Stack.Count;

							try
							{
								Operator.Block.BlockNode.Execute(program);
							}
							catch (ExitError){}
							
							// Pass any var arguments back out to the instruction
							for (int index = 0; index < Operator.Operands.Count; index++)
								if (Operator.Operands[index].Modifier == Modifier.Var)
									arguments[index] = program.Stack[program.Stack.Count - stackDepth + (Operator.Operands.Count + (_allocateResultNode != null ? 1 : 0) - 1 - index)];
							
							// Return the result
							if (_allocateResultNode != null)
								return program.Stack[program.Stack.Count - stackDepth];

							return null;
						}
						finally
						{
							if (transaction != null)
								transaction.PopGlobalContext();
						}
					}
					finally
					{
						if (transaction != null)
							Monitor.Exit(transaction);
					}
				}
				finally
				{
					program.ServerProcess.IsInsert = saveIsInsert;
				}
			}
			finally
			{
				program.Plan.PopSecurityContext();
			}
		}
	}
	
	public class VariableNode : PlanNode
	{
		public VariableNode() : base()
		{
			IsBreakable = true;
		}

		public VariableNode(string variableName, Schema.IDataType variableType) : base()
		{
			IsBreakable = true;
			_variableName = variableName;
			_variableType = variableType;
		}
		
		protected string _variableName = String.Empty;
		public string VariableName
		{
			get { return _variableName; }
			set { _variableName = value == null ? String.Empty : value; }
		}
		
		protected Schema.IDataType _variableType;
		public Schema.IDataType VariableType
		{
			get { return _variableType; }
			set { _variableType = value; }
		}
		
		private bool _hasDefault;
		public bool HasDefault 
		{ 
			get { return _hasDefault; } 
			set { _hasDefault = value; } 
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			if (NodeCount > 0)
				plan.Symbols.Push(new Symbol(String.Empty, plan.DataTypes.SystemGeneric));
			try
			{
				base.InternalBindingTraversal(plan, visitor);
			}
			finally
			{
				if (NodeCount > 0)
					plan.Symbols.Pop();
			}
			plan.Symbols.Push(new Symbol(_variableName, _variableType));

			// TODO: This is more of a DetermineBehavior type call
			Schema.ScalarType scalarType = _variableType as Schema.ScalarType;
			if (scalarType != null)
				_hasDefault = ((scalarType.Default != null) || (scalarType.HasHandlers(EventType.Default)));
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
			
			if (NodeCount > 0)
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
		public override object InternalExecute(Program program)
		{
			program.Stack.Push(null);
			int stackDepth = program.Stack.Count;

			if (NodeCount > 0)
			{
				object tempValue = Nodes[0].Execute(program);
				if (Nodes[0].DataType is Schema.ScalarType)
					tempValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)Nodes[0].DataType, tempValue);
				program.Stack.Poke(program.Stack.Count - stackDepth, tempValue);
			}
			else
			{
				if (_hasDefault && (VariableType is Schema.ScalarType))
					program.Stack.Poke(0, ValueUtility.DefaultValue(program, (Schema.ScalarType)VariableType));
			}


			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			VariableStatement statement = new VariableStatement();
			statement.VariableName = new IdentifierExpression(VariableName);
			statement.TypeSpecifier = VariableType.EmitSpecifier(mode);
			if (NodeCount > 0)
				statement.Expression = (Expression)Nodes[0].EmitStatement(mode);
			return statement;
		}
	}
	
	public class DropVariableNode : PlanNode
	{
		public DropVariableNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			plan.Symbols.Pop();
		}

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Call, typeof(ServerProcess).GetProperty("Context").GetGetMethod());
			generator.Emit(OpCodes.Call, typeof(Stack).GetMethod("Pop", new Type[] { }));
		}
		
		public override object InternalExecute(Program program)
		{
			program.Stack.Pop();
			return null;
		}
	}
	
	public class DeallocateVariableNode : PlanNode
	{
		public DeallocateVariableNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public DeallocateVariableNode(int location) : base()
		{
			Location = location;
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
		
		public override object InternalExecute(Program program)
		{
			DataValue.DisposeValue(program.ValueManager, program.Stack[Location]);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
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

		public override void EmitIL(Plan plan, ILGenerator generator, int[] executePath)
		{
			generator.Emit(OpCodes.Nop);
		}
		
		public override object InternalExecute(Program program)
		{
			return null;
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			return new EmptyStatement();
		}
	}
	
	public class AssignmentNode : PlanNode
	{
		public AssignmentNode() : base()
		{
			ShouldEmitIL = true;
			IsBreakable = true;
		}
		
		public AssignmentNode(PlanNode targetNode, PlanNode valueNode) : base()
		{
			Nodes.Add(targetNode);
			Nodes.Add(valueNode);
			ShouldEmitIL = true;
			IsBreakable = true;
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			int location = ((StackReferenceNode)Nodes[0]).Location;
			Symbol symbol = plan.Symbols[location];
			if (symbol.IsConstant)
				throw new CompilerException(CompilerException.Codes.ConstantObjectCannotBeAssigned, plan.CurrentStatement(), symbol.Name);
			plan.Symbols.SetIsModified(location);
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

		public override object InternalExecute(Program program)
		{
			object objectValue = Nodes[1].Execute(program);
			if (Nodes[1].DataType is Schema.ScalarType)
				objectValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)Nodes[1].DataType, objectValue);
			program.Stack.Poke(((StackReferenceNode)Nodes[0]).Location, objectValue);
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			AssignmentStatement statement = new AssignmentStatement();
			statement.Target = (Expression)Nodes[0].EmitStatement(mode);
			statement.Expression = (Expression)Nodes[1].EmitStatement(mode);
			return statement;
		}
	}
	
    // Nodes[0] = If condition (must be boolean)
    // Nodes[1] = True expression
    // Nodes[2] = False expression (must be the same type as the true expression)
    public class ConditionNode : PlanNode
    {
		// DetermineDataType
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			if (!Nodes[0].DataType.Is(plan.DataTypes.SystemBoolean))
				throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, plan.CurrentStatement());
				
			if (Nodes[2].DataType.Is(Nodes[1].DataType))
			{
				_dataType = Nodes[1].DataType;
				Nodes[2] = Compiler.Upcast(plan, Nodes[2], _dataType);
			}
			else if (Nodes[1].DataType.Is(Nodes[2].DataType))
			{
				_dataType = Nodes[2].DataType;
				Nodes[1] = Compiler.Upcast(plan, Nodes[1], _dataType);
			}
			else
			{
				ConversionContext context = Compiler.FindConversionPath(plan, Nodes[2].DataType, Nodes[1].DataType);
				if (context.CanConvert)
				{
					_dataType = Nodes[1].DataType;
					Nodes[2] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[2], context), Nodes[1].DataType);
				}
				else
				{
					context = Compiler.FindConversionPath(plan, Nodes[1].DataType, Nodes[2].DataType);
					if (context.CanConvert)
					{
						_dataType = Nodes[2].DataType;
						Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[2].DataType);
					}
					else
						Compiler.CheckConversionContext(plan, context);
				}
			}
		}
		
		// Execute
		public override object InternalExecute(Program program)
		{
			bool tempValue = false;
			object objectValue = Nodes[0].Execute(program);
			#if NILPROPOGATION
			if ((objectValue == null))
				tempValue = false;
			else
			#endif
				tempValue = (bool)objectValue;

			if (tempValue)
				return Nodes[1].Execute(program);
			else
				return Nodes[2].Execute(program);
		}
		
		// EmitStatement
		public override Statement EmitStatement(EmitMode mode)
		{
			return new IfExpression((Expression)Nodes[0].EmitStatement(mode), (Expression)Nodes[1].EmitStatement(mode), (Expression)Nodes[2].EmitStatement(mode));
		}
    }

	public class ConditionedCaseNode : PlanNode
	{
		// DetermineDataType
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);

			_dataType = Nodes[0].Nodes[1].DataType;
			for (int index = 1; index < Nodes.Count; index++)
			{
				int nodeIndex = Nodes[index].Nodes.Count - 1;
				if (Nodes[index].Nodes[nodeIndex].DataType.Is(Nodes[0].Nodes[1].DataType))
					Nodes[index].Nodes[nodeIndex] = Compiler.Upcast(plan, Nodes[index].Nodes[nodeIndex], _dataType);
				else
				{	
					ConversionContext context = Compiler.FindConversionPath(plan, Nodes[index].Nodes[nodeIndex].DataType, _dataType);
					if (context.CanConvert)
						Nodes[index].Nodes[nodeIndex] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[index].Nodes[nodeIndex], context), _dataType);
					else
						Compiler.CheckConversionContext(plan, context);
				}
			}
		}
		
		public override object InternalExecute(Program program)
		{
			foreach (ConditionedCaseItemNode node in Nodes)
			{
				if (node.Nodes.Count == 2)
				{
					bool tempValue = false;
					object objectValue = node.Nodes[0].Execute(program);
					#if NILPROPOGATION
					if ((objectValue == null))
						tempValue = false;
					else
					#endif
						tempValue = (bool)objectValue;
					
					if (tempValue)
						return node.Nodes[1].Execute(program);
				}
				else
					return node.Nodes[0].Execute(program);
			}
			
			return null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			CaseExpression caseExpression = new CaseExpression();
			foreach (ConditionedCaseItemNode node in Nodes)
			{
				if (node.Nodes.Count == 2)
					caseExpression.CaseItems.Add(new CaseItemExpression((Expression)node.Nodes[0].EmitStatement(mode), (Expression)node.Nodes[1].EmitStatement(mode)));
				else
					caseExpression.ElseExpression = new CaseElseExpression((Expression)node.Nodes[0].EmitStatement(mode));
			}
			return caseExpression;
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
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = Nodes[2].Nodes[1].DataType;
			for (int index = 3; index < Nodes.Count; index++)
			{
				int nodeIndex = Nodes[index].Nodes.Count - 1;
				if (Nodes[index].Nodes[nodeIndex].DataType.Is(_dataType))
					Nodes[index].Nodes[nodeIndex] = Compiler.Upcast(plan, Nodes[index].Nodes[nodeIndex], _dataType);
				else
				{	
					ConversionContext context = Compiler.FindConversionPath(plan, Nodes[index].Nodes[nodeIndex].DataType, _dataType);
					if (context.CanConvert)
						Nodes[index].Nodes[nodeIndex] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[index].Nodes[nodeIndex], context), _dataType);
					else
						Compiler.CheckConversionContext(plan, context);
				}
			}
		}
		
		// TODO: Change the way this compiles so that it doesn't use nameless stack references so that this binding override can be removed
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			Nodes[0].BindingTraversal(plan, visitor);
			// Do not bind node 1, it will fail (it contains nameless stack references)
			for (int index = 2; index < Nodes.Count; index++)
				Nodes[index].BindingTraversal(plan, visitor);
		}

		public override object InternalExecute(Program program)
		{
			object selector = Nodes[0].Execute(program);
			try
			{
				for (int index = 2; index < Nodes.Count; index++)
				{
					ConditionedCaseItemNode node = (ConditionedCaseItemNode)Nodes[index];
					if (node.Nodes.Count == 2)
					{
						bool tempValue = false;
						object whenVar = node.Nodes[0].Execute(program);
						try
						{
							program.Stack.Push(selector);
							try
							{
								program.Stack.Push(whenVar);
								try
								{
									object objectValue = Nodes[1].Execute(program);
									#if NILPROPOGATION
									if ((objectValue == null))
										tempValue = false;
									else
									#endif
										tempValue = (bool)objectValue;
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
						finally
						{
							DataValue.DisposeValue(program.ValueManager, whenVar);
						}

						if (tempValue)
							return node.Nodes[1].Execute(program);
					}
					else
						return node.Nodes[0].Execute(program);
				}
			}
			finally
			{
				DataValue.DisposeValue(program.ValueManager, selector);
			}
			
			return null;
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			CaseExpression caseExpression = new CaseExpression();
			caseExpression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = 2; index < Nodes.Count; index++)
			{
				ConditionedCaseItemNode node = (ConditionedCaseItemNode)Nodes[index];
				if (node.Nodes.Count == 2)
					caseExpression.CaseItems.Add(new CaseItemExpression((Expression)node.Nodes[0].EmitStatement(mode), (Expression)node.Nodes[1].EmitStatement(mode)));
				else
					caseExpression.ElseExpression = new CaseElseExpression((Expression)node.Nodes[0].EmitStatement(mode));
			}
			return caseExpression;
		}
	}
	
	public class ConditionedCaseItemNode : PlanNode
	{
		public override object InternalExecute(Program program)
		{
			return null;
		}
	}

	public abstract class VarReferenceNode : PlanNode
	{
		public VarReferenceNode() : base()
		{
			_isLiteral = false;
			_isFunctional = true;
			_isDeterministic = true;
			_isRepeatable = true;
			_isOrderPreserving = true;
		}
		
		public VarReferenceNode(Schema.IDataType dataType) : base()
		{
			_dataType = dataType;
			_isLiteral = false;
			_isFunctional = true;
			_isDeterministic = true;
			_isRepeatable = true;
			_isOrderPreserving = true;
		}
	}
	
    public class StackReferenceNode : VarReferenceNode
    {
		// constructor
		public StackReferenceNode() : base()
		{
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(Schema.IDataType dataType, int location) : base(dataType)
		{
			Location = location;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(Schema.IDataType dataType, int location, bool byReference) : base(dataType)
		{
			Location = location;
			ByReference = byReference;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(string identifier, Schema.IDataType dataType, int location) : base(dataType)
		{
			Identifier = identifier;
			Location = location;
			ShouldEmitIL = true;
		}
		
		public StackReferenceNode(string identifier, Schema.IDataType dataType, int location, bool byReference) : base(dataType)
		{
			Identifier = identifier;
			Location = location;
			ByReference = byReference;
			ShouldEmitIL = true;
		}
		
		// Identifier
		protected string _identifier = String.Empty;
		public string Identifier
		{
			get { return _identifier; }
			set { _identifier = value == null ? String.Empty : value; }
		}
		
		// Location
		public int Location = -1;
		
		// ByReference
		public bool ByReference;
		
		// Statement
		public override Statement EmitStatement(EmitMode mode)
		{
			return new VariableIdentifierExpression(_identifier);
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			int columnIndex;
			Location = Compiler.ResolveVariableIdentifier(plan, _identifier, out columnIndex);
			if (Location < 0)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, _identifier);
				
			if (columnIndex >= 0)
				throw new CompilerException(CompilerException.Codes.InvalidColumnBinding, _identifier);
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
		public override object InternalExecute(Program program)
		{
			if (ByReference)
				return program.Stack[Location];
			else
				return DataValue.CopyValue(program.ValueManager, program.Stack[Location]);
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			writer.WriteAttributeString("Identifier", Identifier);
			writer.WriteAttributeString("StackIndex", Location.ToString());
			writer.WriteAttributeString("ByReference", Convert.ToString(ByReference));
		}

		public override bool IsContextLiteral(int location)
		{
			if (Location == location)
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
		
		public StackColumnReferenceNode(string identifier, Schema.IDataType dataType, int location) : base(dataType)
		{
			Identifier = identifier;
			Location = location;
			ShouldEmitIL = true;
		}
		#endif
		
		// Identifier
		private string _identifier;
		public string Identifier
		{
			get { return _identifier; }
			set 
			{ 
				_identifier = value;
				#if !USECOLUMNLOCATIONBINDING
				SetResolvingIdentifier();
				#endif
			}
		}

		#if !USECOLUMNLOCATIONBINDING		
		private string _resolvingIdentifier;
		private void SetResolvingIdentifier()
		{
			switch (Schema.Object.Qualifier(Schema.Object.EnsureUnrooted(Identifier)))
			{
				case Keywords.Parent :
				case Keywords.Left :
				case Keywords.Right :
				case Keywords.Source :
					_resolvingIdentifier = Schema.Object.Dequalify(Schema.Object.EnsureUnrooted(Identifier)); break;
				
				default : _resolvingIdentifier = Identifier; break;
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
		public override Statement EmitStatement(EmitMode mode)
		{
			if (Schema.Object.Qualifier(Schema.Object.EnsureUnrooted(Identifier)) == Keywords.Parent)
				return new ExplodeColumnExpression(_resolvingIdentifier);
			else
				return new ColumnIdentifierExpression(_resolvingIdentifier);
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USECOLUMNLOCATIONBINDING
			Location = Compiler.ResolveVariableIdentifier(APlan, Identifier, out ColumnLocation);
			if ((Location < 0) || (ColumnLocation < 0))
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, Identifier);
			#else
			int columnIndex;
			Location = Compiler.ResolveVariableIdentifier(plan, Identifier, out columnIndex);
			if ((Location < 0) || (columnIndex < 0))
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
		public override object InternalExecute(Program program)
		{
			Row row = (Row)program.Stack[Location];
			#if NILPROPOGATION
			if ((row == null) || row.IsNil)
				return null;
			#endif

			#if USECOLUMNLOCATIONBINDING
			if (row.HasValue(ColumnLocation))
				return ByReference ? row[ColumnLocation] : DataValue.CopyValue(AProgram.ServerProcess, row[ColumnLocation]);
			else
				return null;
			#else
			int columnIndex = row.DataType.Columns.IndexOf(_resolvingIdentifier);
			if (columnIndex < 0)
				throw new CompilerException(CompilerException.Codes.UnknownIdentifier, _resolvingIdentifier);
			if (row.HasValue(columnIndex))
				return ByReference ? row[columnIndex] : DataValue.CopyValue(program.ValueManager, row[columnIndex]);
			else
				return null;
			#endif
		}

		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			writer.WriteAttributeString("ColumnName", Identifier);
			writer.WriteAttributeString("StackIndex", Location.ToString());
		}

		public override bool IsContextLiteral(int location)
		{
			if (Location == location)
				return false;
			return true;
		}

		public override void DetermineCharacteristics(Plan plan)
		{
			var column =
			IsLiteral = false;
			IsFunctional = true;
			IsDeterministic = true;
			IsRepeatable = true;
			// TODO: introduce infrastructure (possible by merging tablevar with tabletype) to infer nilability through columns
			IsNilable = false;  //((Schema.RowType)APlan.Symbols[Location].DataType).Columns[FResolvingIdentifier].IsNilable;
			base.DetermineCharacteristics(plan);
		}
    }

    public class PropertyReferenceNode : VarReferenceNode
    {
		public PropertyReferenceNode() : base(){}
		public PropertyReferenceNode(Schema.IDataType dataType, Schema.ScalarType scalarType, PlanNode valueNode, int representationIndex, int propertyIndex) : base(((Schema.ScalarType)dataType).Representations[representationIndex].Properties[propertyIndex].DataType)
		{
			Nodes.Add(valueNode);
			_scalarType = scalarType;
			_representationIndex = representationIndex;
			_propertyIndex = propertyIndex;
		}
		
		private Schema.ScalarType _scalarType;
		public Schema.ScalarType ScalarType
		{
			get { return _scalarType; }
			set { _scalarType = value; }
		}
		
		private int _representationIndex;
		public int RepresentationIndex
		{
			get { return _representationIndex; }
			set { _representationIndex = value; }
		}
		
		private int _propertyIndex;
		public int PropertyIndex
		{
			get { return _propertyIndex; }
			set { _propertyIndex = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			throw new RuntimeException(RuntimeException.Codes.PropertyRefNodeExecuted);
		}
    }
}
