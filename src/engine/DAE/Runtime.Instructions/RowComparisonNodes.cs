/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define UseReferenceDerivation
#define NILPROPOGATION
#define USENAMEDROWVARIABLES
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Compiling.Visitors;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator =(row, row) : bool
	// operator =(entry, entry) : bool
	// A row is equal to another row if it has the same number of columns and all columns by name have equal values
	public class RowEqualNode : InstructionNode
	{
		protected PlanNode _comparisonNode;
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			
			if (!Nodes[0].DataType.IsGeneric && !Nodes[1].DataType.IsGeneric) // Generic row comparison must be done at run-time
			{
				if (Nodes[0].DataType.Is(Nodes[1].DataType))
					Nodes[0] = Compiler.Upcast(plan, Nodes[0], Nodes[1].DataType);
				else if (Nodes[1].DataType.Is(Nodes[0].DataType))
					Nodes[1] = Compiler.Upcast(plan, Nodes[1], Nodes[0].DataType);
				else
				{
					ConversionContext context = Compiler.FindConversionPath(plan, Nodes[0].DataType, Nodes[1].DataType);
					if (context.CanConvert)
						Nodes[0] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[0], context), Nodes[1].DataType);
					else
					{
						context = Compiler.FindConversionPath(plan, Nodes[1].DataType, Nodes[0].DataType);
						Compiler.CheckConversionContext(plan, context);
						Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[0].DataType);
					}
				}

				plan.Symbols.PushWindow(0);
				try
				{
					plan.EnterRowContext();
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Left, (Schema.RowType)Nodes[0].DataType));
						#else
						Schema.RowType leftRowType = new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left);
						APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.Right, (Schema.RowType)Nodes[1].DataType));
							#else
							Schema.RowType rightRowType = new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right);
							APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
							#endif
							try
							{
								_comparisonNode = 
									#if USENAMEDROWVARIABLES
									Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, ((Schema.RowType)Nodes[0].DataType).Columns, ((Schema.RowType)Nodes[1].DataType).Columns));
									#else
									Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, leftRowType.Columns, rightRowType.Columns));
									#endif
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.ExitRowContext();
					}
				}
				finally
				{
					plan.Symbols.PopWindow();
				}
			}

			// base.DetermineDataType rather than DetermineModifiers alone, and last rather than first. The const branch of
			// InstructionNodeBase.DetermineDataType is the only place that asks an argument node to lend the value it
			// already holds rather than copy it, and iEqual declares both of its operands const
			// (src/engine/DAE/Schema/SystemCatalog.d4). Asking is half the const-operand rule in
			// docs/architecture/operand-ownership.md and the release in InternalExecute below is the other half; missing
			// both held one stream per stream-backed column per side for the life of the session, on every row comparison
			// (bug-row-equality-leaks-the-rows-it-compares). It is asked here rather than before the rewrite above because
			// the lend is granted to whichever node sits in the operand slot at the time: ask first, and a conversion
			// interposed afterwards leaves the grant on the node underneath it, whose innermost consumer - RowExtendNode,
			// which Compiler.ConvertNode reaches through EmitRedefineNode - disposes the source row it is handed, so
			// comparing two rows whose columns differ by a convertible type destroyed the row in the variable on the side the
			// conversion wraps (bug-row-equality-change-breaks-implicitconversions). base guards its own body on Operator != null,
			// which is what the generic-row branch of InternalCompare below relies on, and sets _dataType from
			// Operator.ReturnDataType, which is why the assignment follows rather than precedes it.
			base.DetermineDataType(plan);
			_dataType = plan.DataTypes.SystemBoolean;
		}

		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			Nodes[1].BindingTraversal(plan, visitor);
			#endif
			if (_comparisonNode != null)
			{
				plan.Symbols.PushWindow(0);
				try
				{
					plan.EnterRowContext();
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Left, (Schema.RowType)Nodes[0].DataType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.RowType)Nodes[0].DataType).Columns, Keywords.Left)));
						#endif
						try
						{
							#if USENAMEDROWVARIABLES
							plan.Symbols.Push(new Symbol(Keywords.Right, (Schema.RowType)Nodes[1].DataType));
							#else
							APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.RowType)Nodes[1].DataType).Columns, Keywords.Right)));
							#endif
							try
							{
								#if USEVISIT
								_comparisonNode = visitor.Visit(plan, _comparisonNode);
								#else
								_comparisonNode.BindingTraversal(plan, visitor);
								#endif
							}
							finally
							{
								plan.Symbols.Pop();
							}
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.ExitRowContext();
					}
				}
				finally
				{
					plan.Symbols.PopWindow();
				}
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments) { return null; }

		/// <remarks>
		/// Overriding this shape of InternalExecute puts the node outside InstructionNodeBase.CleanupOperand, so the
		/// const-operand rule in docs/architecture/operand-ownership.md is applied here by hand: an argument node that was
		/// asked to lend answers ReturnsByReference true and the value it handed over belongs to something else, where a
		/// node that computed one - a literal, a conversion, another call - answers false and the value is this node's to
		/// release. A row's value is a managed stream per stream-backed column, so releasing none of them held that many
		/// streams per side for the life of the session, on every comparison.
		/// </remarks>
		public override object InternalExecute(Program program)
		{
			object leftValue = Nodes[0].Execute(program);
			object rightValue;
			try
			{
				rightValue = Nodes[1].Execute(program);
			}
			catch
			{
				// The second argument raised, so the comparison never happens, and the rule for the argument already
				// evaluated is the one InstructionNodeBase.ReleaseUncalledOperand applies on the shapes' own failure path:
				// a value this node owns is still this node's to release.
				if (!Nodes[0].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, leftValue);
				throw;
			}

			try
			{
				return InternalCompare(program, leftValue, rightValue);
			}
			finally
			{
				// The answer is read off the node left in Nodes[index] at run time rather than remembered from what
				// base.DetermineDataType asked at compile time, because the DetermineDataType override above may replace
				// either argument node afterwards. Compiler.Upcast returns the node unchanged today, but Compiler.ConvertNode
				// genuinely wraps it - correctly, and for the reason the rule exists: a conversion computes a new value from
				// the one it was lent, so it answers false and what arrives here is a value this node owns.
				if (!Nodes[0].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, leftValue);
				if (!Nodes[1].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, rightValue);
			}
		}

		/// <summary>Compares the two row values, whose ownership InternalExecute above has already settled.</summary>
		private object InternalCompare(Program program, object leftValue, object rightValue)
		{
			#if NILPROPOGATION
			if ((leftValue == null) || (rightValue == null))
				return null;
			#endif

			if (_comparisonNode != null)
			{
				program.Stack.PushWindow(0);
				try
				{
					program.Stack.Push(leftValue);
					try
					{
						program.Stack.Push(rightValue);
						try
						{
							return _comparisonNode.Execute(program);
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
					program.Stack.PopWindow();
				}
			}
			else
			{
				// A generic row operand - what an operator declared operator F(const A : row, const B : row) hands its body -
				// has no column list at plan time, so the comparison is planned here over the two values just evaluated. The
				// inner node applies the release rule to its own arguments, and these two values are still this node's, so
				// the argument nodes handed to it have to say they are lending rather than giving.
				RowEqualNode node = new RowEqualNode();
				node.Nodes.Add(new BorrowedValueNode(((IRow)leftValue).DataType, leftValue));
				node.Nodes.Add(new BorrowedValueNode(((IRow)rightValue).DataType, rightValue));
				node.DetermineDataType(program.Plan);
				return node.Execute(program);
			}
		}

		/// <summary>A ValueNode over a value the node that built it still owns, so the consumer must leave it alone.</summary>
		/// <remarks>
		/// The wider answer - giving ValueNode itself a SetByReference, so that no consumer has to know not to release
		/// what a literal lent it - is deliberately not taken inside a leak fix, because its blast radius is every call
		/// in the engine with a literal argument. The NOTE at ValueNode (ImperativeNodes.cs) records that, and what to
		/// check before relying on a plain ValueNode over a value with a stream behind it.
		/// </remarks>
		private class BorrowedValueNode : ValueNode
		{
			public BorrowedValueNode(Schema.IDataType dataType, object tempValue) : base(dataType, tempValue) { }

			public override bool ReturnsByReference { get { return true; } }

			public override bool SetByReference() { return true; }
		}
	}

	public class CompoundScalarEqualNode : InstructionNode
	{
		protected PlanNode _comparisonNode;
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			
			if (Nodes[0].DataType.Is(Nodes[1].DataType))
				Nodes[0] = Compiler.Upcast(plan, Nodes[0], Nodes[1].DataType);
			else if (Nodes[1].DataType.Is(Nodes[0].DataType))
				Nodes[1] = Compiler.Upcast(plan, Nodes[1], Nodes[0].DataType);
			else
			{
				ConversionContext context = Compiler.FindConversionPath(plan, Nodes[0].DataType, Nodes[1].DataType);
				if (context.CanConvert)
					Nodes[0] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[0], context), Nodes[1].DataType);
				else
				{
					context = Compiler.FindConversionPath(plan, Nodes[1].DataType, Nodes[0].DataType);
					Compiler.CheckConversionContext(plan, context);
					Nodes[1] = Compiler.Upcast(plan, Compiler.ConvertNode(plan, Nodes[1], context), Nodes[0].DataType);
				}
			}

			plan.Symbols.PushWindow(0);
			try
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType));
					#else
					Schema.RowType leftRowType = new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left);
					APlan.Symbols.Push(new Symbol(String.Empty, leftRowType));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType));
						#else
						Schema.RowType rightRowType = new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right);
						APlan.Symbols.Push(new Symbol(String.Empty, rightRowType));
						#endif
						try
						{
							_comparisonNode = 
								#if USENAMEDROWVARIABLES
								Compiler.CompileExpression(plan, Compiler.BuildRowEqualExpression(plan, Keywords.Left, Keywords.Right, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns));
								#else
								Compiler.CompileExpression(APlan, Compiler.BuildRowEqualExpression(APlan, leftRowType.Columns, rightRowType.Columns));
								#endif
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}

			// base.DetermineDataType rather than DetermineModifiers alone, and last rather than first, for the reasons
			// given in RowEqualNode above: only the const branch asks an argument node to lend the value it already holds,
			// the equality generated for a compound scalar type (Compiler.CompileComparisonOperator) declares both of its
			// operands const, and the rewrite above may replace either argument node with a conversion, so the question has
			// to reach whichever node ends up in the operand slot rather than the one underneath it.
			base.DetermineDataType(plan);
			_dataType = plan.DataTypes.SystemBoolean;
		}

		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			Nodes[1].BindingTraversal(plan, visitor);
			#endif
			plan.Symbols.PushWindow(0);
			try
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Left, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.ScalarType)Nodes[0].DataType).CompoundRowType.Columns, Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						plan.Symbols.Push(new Symbol(Keywords.Right, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType));
						#else
						APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(((Schema.ScalarType)Nodes[1].DataType).CompoundRowType.Columns, Keywords.Right)));
						#endif
						try
						{
							#if USEVISIT
							_comparisonNode = visitor.Visit(plan, _comparisonNode);
							#else
							_comparisonNode.BindingTraversal(plan, visitor);
							#endif
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.Symbols.Pop();
					}
				}
				finally
				{
					plan.ExitRowContext();
				}
			}
			finally
			{
				plan.Symbols.PopWindow();
			}
		}
		
		public override object InternalExecute(Program program, object[] arguments) { return null; }

		/// <remarks>
		/// The same const-operand rule by hand as RowEqualNode above, and for the same reason: this shape of
		/// InternalExecute is outside InstructionNodeBase.CleanupOperand, and the equality generated for a compound
		/// scalar type declares both of its operands const. See docs/architecture/operand-ownership.md.
		/// </remarks>
		public override object InternalExecute(Program program)
		{
			object leftVar = Nodes[0].Execute(program);
			object rightVar;
			try
			{
				rightVar = Nodes[1].Execute(program);
			}
			catch
			{
				if (!Nodes[0].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, leftVar);
				throw;
			}

			try
			{
				// Inside the try, so that a nil on one side still releases the value evaluated for the other.
				#if NILPROPOGATION
				if ((leftVar == null) || (rightVar == null))
					return null;
				#endif

				IDataValue leftValue = null;
				IDataValue rightValue = null;
				try
				{
					// ValuesOwned is cleared rather than left as it comes: the wrapper is only a host view of a native value
					// whose one release belongs to the argument node above, and a wrapper that owned its values would release
					// the columns of a row this node may only have been lent. Row's by-reference constructor clears the flag
					// itself today, but it is the flag the dispose below reads, so this says so rather than relying on it.
					leftValue = DataValue.FromNative(program.ValueManager, ((Schema.ScalarType)Nodes[0].DataType).CompoundRowType, leftVar);
					leftValue.ValuesOwned = false;
					rightValue = DataValue.FromNative(program.ValueManager, ((Schema.ScalarType)Nodes[1].DataType).CompoundRowType, rightVar);
					rightValue.ValuesOwned = false;

					program.Stack.PushWindow(0);
					try
					{
						program.Stack.Push(leftValue);
						try
						{
							program.Stack.Push(rightValue);
							try
							{
								return _comparisonNode.Execute(program);
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
						program.Stack.PopWindow();
					}
				}
				finally
				{
					if (rightValue != null)
						rightValue.Dispose();
					if (leftValue != null)
						leftValue.Dispose();
				}
			}
			finally
			{
				// A compound scalar's native value is a NativeRow, which DataValue.DisposeValue releases by recursing into
				// its column values. As in RowEqualNode, the lending answer is read off the node left in Nodes[index] at run
				// time, because the DetermineDataType override above may have replaced it with a conversion.
				if (!Nodes[0].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, leftVar);
				if (!Nodes[1].ReturnsByReference)
					DataValue.DisposeValue(program.ValueManager, rightVar);
			}
		}
	}
}
