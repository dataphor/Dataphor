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
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
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

    public class RowNode : PlanNode
    {
		// constructor
		public RowNode() : base(){}
		
		public new Schema.IRowType DataType
		{
			get { return (Schema.IRowType)_dataType; }
			set { _dataType = value; }
		}
		
		private Schema.IRowType _specifiedRowType;		
		/// <summary>If this is a specified row selector, this is the specified subset of the row selector.</summary>
		public Schema.IRowType SpecifiedRowType
		{
			get { return _specifiedRowType; }
			set { _specifiedRowType = value; }
		}

		public override void DetermineCharacteristics(Plan plan)
		{
			IsLiteral = true;
			IsFunctional = true;
			IsDeterministic = true;
			IsRepeatable = true;
			IsNilable = false;
			for (int index = 0; index < NodeCount; index++)
			{
				IsLiteral = IsLiteral && Nodes[index].IsLiteral;
				IsFunctional = IsFunctional && Nodes[index].IsFunctional;
				IsDeterministic = IsDeterministic && Nodes[index].IsDeterministic;
				IsRepeatable = IsRepeatable && Nodes[index].IsRepeatable;
			} 
		}
		
		// Evaluate
		public override object InternalExecute(Program program)
		{
			Row row = new Row(program.ValueManager, DataType);
			try
			{
				if (_specifiedRowType != null)
				{
					for (int index = 0; index < _specifiedRowType.Columns.Count; index++)
					{
						object objectValue = Nodes[index].Execute(program);
						if (objectValue != null)
						{
							try
							{
								if (_specifiedRowType.Columns[index].DataType is Schema.ScalarType)
									objectValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)_specifiedRowType.Columns[index].DataType, objectValue);
								row[DataType.Columns.IndexOfName(_specifiedRowType.Columns[index].Name)] = objectValue;
							}
							finally
							{
								DataValue.DisposeValue(program.ValueManager, objectValue);
							}
						}
					}
				}
				else
				{
					for (int index = 0; index < DataType.Columns.Count; index++)
					{
						object objectValue = Nodes[index].Execute(program);
						if (objectValue != null)
						{
							try
							{
								if (DataType.Columns[index].DataType is Schema.ScalarType)
									objectValue = ValueUtility.ValidateValue(program, (Schema.ScalarType)DataType.Columns[index].DataType, objectValue);
								row[index] = objectValue;
							}
							finally
							{
								DataValue.DisposeValue(program.ValueManager, objectValue);
							}
						}
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
		
		// Statement
		public override Statement EmitStatement(EmitMode mode)
		{
			if (_dataType is Schema.RowType)
			{
				RowSelectorExpression expression = new RowSelectorExpression();
				if (_specifiedRowType != null)
					expression.TypeSpecifier = DataType.EmitSpecifier(mode);
				Schema.IRowType rowType = _specifiedRowType != null ? _specifiedRowType : DataType;
				for (int index = 0; index < rowType.Columns.Count; index++)
					expression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[index].EmitStatement(mode), rowType.Columns[index].Name));
				expression.Modifiers = Modifiers;
				return expression;
			}
			else
			{
				EntrySelectorExpression expression = new EntrySelectorExpression();
				for (int index = 0; index < DataType.Columns.Count; index++)
					expression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[index].EmitStatement(mode), DataType.Columns[index].Name));
				expression.Modifiers = Modifiers;
				return expression;
			}
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newRowNode = (RowNode)newNode;
			newRowNode.SpecifiedRowType = _specifiedRowType;
		}
	}

    public class RowSelectorNode : RowNode
    {
		public RowSelectorNode(Schema.IRowType dataType) : base()
		{
			_dataType = dataType;
		}
		
		public override void DetermineDataType(Plan plan)
		{
			Error.Fail("Unimplemented RowSelectorNode.DetermineDataType");
		}
    }
    
    public class RowExtendNode : UnaryRowNode
    {
		protected int _extendColumnOffset;		
		public int ExtendColumnOffset { get { return _extendColumnOffset; } }
		
		private NamedColumnExpressions _expressions;
		public NamedColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newRowExtendNode = (RowExtendNode)newNode;
			newRowExtendNode._extendColumnOffset = _extendColumnOffset;
			newRowExtendNode._expressions = _expressions;
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.RowType();

			foreach (Schema.Column column in SourceRowType.Columns)
				DataType.Columns.Add(column.Copy());
			_extendColumnOffset = DataType.Columns.Count;

			plan.EnterRowContext();
			try
			{			
				plan.Symbols.Push(new Symbol(String.Empty, SourceRowType));
				try
				{
					// Add a column for each expression
					PlanNode planNode;
					Schema.Column newColumn;
					foreach (NamedColumnExpression column in _expressions)
					{
						planNode = Compiler.CompileExpression(plan, column.Expression);

						newColumn = 
							new Schema.Column
							(
								column.ColumnAlias, 
								planNode.DataType
							);
							
						DataType.Columns.Add(newColumn);
						Nodes.Add(planNode);
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
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, SourceRowType));
				try
				{
					for (int index = 1; index < Nodes.Count; index++)
						#if USEVISIT
						Nodes[index] = visitor.Visit(plan, Nodes[index]);
						#else
						Nodes[index].BindingTraversal(plan, visitor);
						#endif
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
		
		public override Statement EmitStatement(EmitMode mode)
		{
			ExtendExpression expression = new ExtendExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = ExtendColumnOffset; index < DataType.Columns.Count; index++)
				expression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[index - ExtendColumnOffset + 1].EmitStatement(mode), DataType.Columns[index].Name));
			expression.Modifiers = Modifiers;
			return expression;
		}

		public override object InternalExecute(Program program, object argument1)
		{
			return null; // Not called, required override due to abstract
		}
		
		public override object InternalExecute(Program program)
		{
			object result;
			IRow sourceRow = (IRow)Nodes[0].Execute(program);
			#if NILPROPOGATION
			if ((sourceRow == null) || sourceRow.IsNil)
				return null;
			#endif
			try
			{
				Row newRow = new Row(program.ValueManager, DataType);
				try
				{
					program.Stack.Push(sourceRow);
					try
					{
						sourceRow.CopyTo(newRow);
						
						for (int index = _extendColumnOffset; index < newRow.DataType.Columns.Count; index++)
						{
							result = Nodes[index - _extendColumnOffset + 1].Execute(program);
							if (result != null)
							{
								try
								{
									newRow[index] = result;
								}
								finally
								{
									DataValue.DisposeValue(program.ValueManager, result);
								}
							}
						}
					}
					finally
					{
						program.Stack.Pop();
					}
					return newRow;
				}
				catch
				{
					newRow.Dispose();
					throw;
				}
			}
			finally
			{
				sourceRow.Dispose();
			}
		}

		public override bool IsContextLiteral(int location, IList<string> columnReferences)
		{
			if (!Nodes[0].IsContextLiteral(location, columnReferences))
				return false;
			for (int index = 1; index < Nodes.Count; index++)
				if (!Nodes[index].IsContextLiteral(location + 1, columnReferences))
					return false;
			return true;
		}
    }
    
	public class RowRedefineNode : UnaryRowNode
	{
		protected int[] _redefineColumnOffsets;		
		public int[] RedefineColumnOffsets
		{
			get { return _redefineColumnOffsets; }
			set { _redefineColumnOffsets = value; }
		}
		
		private NamedColumnExpressions _expressions;
		public NamedColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}
		
		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newRowRedefineNode = (RowRedefineNode)newNode;
			newRowRedefineNode._redefineColumnOffsets = (int[])_redefineColumnOffsets.Clone();
			newRowRedefineNode._expressions = _expressions;
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.RowType();

			foreach (Schema.Column column in SourceRowType.Columns)
				DataType.Columns.Add(column.Copy());

			int index = 0;			
			_redefineColumnOffsets = new int[_expressions.Count];
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, SourceRowType));
				try
				{
					// Add a column for each expression
					PlanNode planNode;
					Schema.Column sourceColumn;
					Schema.Column newColumn;
					foreach (NamedColumnExpression column in _expressions)
					{
						int sourceColumnIndex = DataType.Columns.IndexOf(column.ColumnAlias);
						if (sourceColumnIndex < 0)
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, column, column.ColumnAlias);
							
						sourceColumn = DataType.Columns[sourceColumnIndex];
						planNode = Compiler.CompileExpression(plan, column.Expression);
						newColumn = new Schema.Column(sourceColumn.Name, planNode.DataType);
						DataType.Columns[sourceColumnIndex] = newColumn;
						_redefineColumnOffsets[index] = sourceColumnIndex;
						Nodes.Add(planNode);
						index++;
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
		
		public override Statement EmitStatement(EmitMode mode)
		{
			RedefineExpression expression = new RedefineExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			for (int index = 0; index < _redefineColumnOffsets.Length; index++)
				expression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[index + 1].EmitStatement(mode), DataType.Columns[_redefineColumnOffsets[index]].Name));
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			plan.EnterRowContext();
			try
			{
				plan.Symbols.Push(new Symbol(String.Empty, SourceRowType));
				try
				{
					for (int index = 1; index < Nodes.Count; index++)
						#if USEVISIT
						Nodes[index] = visitor.Visit(plan, Nodes[index]);
						#else
						Nodes[index].BindingTraversal(plan, visitor);
						#endif
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
		
		public override object InternalExecute(Program program, object argument1)
		{
			return null; // Not called, required override due to abstract
		}
		
		public override object InternalExecute(Program program)
		{
			IRow sourceRow = (IRow)Nodes[0].Execute(program);
			#if NILPROPOGATION
			if ((sourceRow == null) || sourceRow.IsNil)
				return null;
			#endif
			try
			{
				program.Stack.Push(sourceRow);
				try
				{
					Row newRow = new Row(program.ValueManager, DataType);
					try
					{
						int columnIndex;
						for (int index = 0; index < DataType.Columns.Count; index++)
						{
							if (!((IList)_redefineColumnOffsets).Contains(index))
							{
								columnIndex = sourceRow.DataType.Columns.IndexOfName(DataType.Columns[index].Name);
								if (sourceRow.HasValue(columnIndex))
									newRow[index] = sourceRow[columnIndex];
								else
									newRow.ClearValue(index);
							}
						}
						
						for (int index = 0; index < _redefineColumnOffsets.Length; index++)
						{
							object result = Nodes[index + 1].Execute(program);
							if ((result != null))
							{
								try
								{
									newRow[_redefineColumnOffsets[index]] = result;
								}
								finally
								{
									DataValue.DisposeValue(program.ValueManager, result);
								}
							}
							else
								newRow.ClearValue(_redefineColumnOffsets[index]);
						}
						return newRow;
					}
					catch
					{
						newRow.Dispose();
						throw;
					}
				}
				finally
				{
					program.Stack.Pop();
				}
			}
			finally
			{
				sourceRow.Dispose();
			}
		}
		
		public override bool IsContextLiteral(int location, IList<string> columnReferences)
		{
			if (!Nodes[0].IsContextLiteral(location, columnReferences))
				return false;
			for (int index = 1; index < Nodes.Count; index++)
				if (!Nodes[index].IsContextLiteral(location + 1, columnReferences))
					return false;
			return true;
		}
	}
	
	// operator Rename(row) : row
	public class RowRenameNode : UnaryRowNode
	{
		private string _rowAlias;
		public string RowAlias
		{
			get { return _rowAlias; }
			set { _rowAlias = value; }
		}
		
		private MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}

		private RenameColumnExpressions _expressions;		
		public RenameColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}

		// Indicates whether this row rename should be emitted
		// This is used by the table-indexer compilation process to
		// ensure that the row rename portion is not re-emitted
		// as part of the expression
		private bool _shouldEmit = true;
		public bool ShouldEmit
		{
			get { return _shouldEmit; }
			set { _shouldEmit = value; }
		}
		
		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newRowRenameNode = (RowRenameNode)newNode;
			newRowRenameNode._rowAlias = _rowAlias;
			newRowRenameNode._metaData = _metaData;
			newRowRenameNode._expressions = _expressions;
			newRowRenameNode._shouldEmit = _shouldEmit;
		}

		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.RowType();

			if (_expressions == null)
			{
				// Inherit columns
				foreach (Schema.Column column in SourceRowType.Columns)
					DataType.Columns.Add(new Schema.Column(Schema.Object.Qualify(column.Name, _rowAlias), column.DataType));
			}
			else
			{
				bool columnAdded;
				Schema.Column column;
				int renameColumnIndex;
				for (int index = 0; index < SourceRowType.Columns.Count; index++)
				{
					columnAdded = false;
					foreach (RenameColumnExpression renameColumn in _expressions)
					{
						renameColumnIndex = SourceRowType.Columns.IndexOf(renameColumn.ColumnName);
						if (renameColumnIndex < 0)
							throw new Schema.SchemaException(Schema.SchemaException.Codes.ObjectNotFound, renameColumn.ColumnName);
						else if (renameColumnIndex == index)
						{
							if (columnAdded)
								throw new CompilerException(CompilerException.Codes.DuplicateRenameColumn, renameColumn.ColumnName);

							column = new Schema.Column(renameColumn.ColumnAlias, SourceRowType.Columns[index].DataType);
							DataType.Columns.Add(column);
							columnAdded = true;
						}
					}
					if (!columnAdded)
						DataType.Columns.Add(SourceRowType.Columns[index].Copy());
				}
			}
		}
		
		public override object InternalExecute(Program program, object argument)
		{
			IRow sourceRow = (IRow)argument;
			#if NILPROPOGATION
			if ((sourceRow == null) || sourceRow.IsNil)
				return null;
			#endif
			try
			{
				Row newRow = new Row(program.ValueManager, DataType);
				try
				{
					for (int index = 0; index < sourceRow.DataType.Columns.Count; index++)
						if (sourceRow.HasValue(index) && (index < newRow.DataType.Columns.Count))
							newRow[index] = sourceRow[index];
					
					return newRow;
				}
				catch
				{
					newRow.Dispose();
					throw;
				}
			}
			finally
			{
				sourceRow.Dispose();
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			if (ShouldEmit && (DataType.Columns.Count > 0))
			{
				RenameExpression expression = new RenameExpression();
				expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
				for (int index = 0; index < DataType.Columns.Count; index++)
					expression.Expressions.Add(new RenameColumnExpression(Schema.Object.EnsureRooted(SourceRowType.Columns[index].Name), DataType.Columns[index].Name));
				expression.Modifiers = Modifiers;
				return expression;
			}
			else
				return Nodes[0].EmitStatement(mode);
		}
	}
	
	public class RowJoinNode : BinaryInstructionNode
	{
		private PlanNode _equalNode;

		public new Schema.RowType DataType { get { return (Schema.RowType)_dataType; } }
		public Schema.RowType LeftRowType { get { return (Schema.RowType)Nodes[0].DataType; } }
		public Schema.RowType RightRowType { get { return (Schema.RowType)Nodes[1].DataType; } }

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			if (_equalNode != null)
			{
				var newRowJoinNode = (RowJoinNode)newNode;
				newRowJoinNode._equalNode = _equalNode.Clone();
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			base.DetermineDataType(plan);
			_dataType = new Schema.RowType();
			Expression expression = null;
			int rightIndex;
			for (int leftIndex = 0; leftIndex < LeftRowType.Columns.Count; leftIndex++)
			{
				rightIndex = RightRowType.Columns.IndexOfName(LeftRowType.Columns[leftIndex].Name);
				if (rightIndex >= 0)
				{
					Expression equalExpression = 
						new BinaryExpression
						(
							new IdentifierExpression(Schema.Object.Qualify(LeftRowType.Columns[leftIndex].Name, Keywords.Left)), 
							Instructions.Equal, 
							new IdentifierExpression(Schema.Object.Qualify(RightRowType.Columns[rightIndex].Name, Keywords.Right))
						);
						
					if (expression != null)
						expression = new BinaryExpression(expression, Instructions.And, equalExpression);
					else
						expression = equalExpression;
				}
				DataType.Columns.Add(LeftRowType.Columns[leftIndex].Copy());
			}
			
			for (int index = 0; index < RightRowType.Columns.Count; index++)
				if (!LeftRowType.Columns.ContainsName(RightRowType.Columns[index].Name))
					DataType.Columns.Add(RightRowType.Columns[index].Copy());
			
			if (expression != null)
			{
				plan.EnterRowContext();
				try
				{
					#if USENAMEDROWVARIABLES
					APlan.Symbols.Push(new Symbol(Keywords.Left, LeftRowType));
					#else
					plan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftRowType.Columns, Keywords.Left)));
					#endif
					try
					{
						#if USENAMEDROWVARIABLES
						APlan.Symbols.Push(new Symbol(Keywords.Right, RightRowType));
						#else
						plan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightRowType.Columns, Keywords.Right)));
						#endif
						try
						{
							_equalNode = Compiler.CompileExpression(plan, expression);
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
			else
				_equalNode = null;
		}
		
		public override object InternalExecute(Program program, object argument1, object argument2)
		{
			#if NILPROPOGATION
			if (argument1 == null || argument2 == null)
				return null;
			#endif
			if (_equalNode != null)
			{
				program.Stack.Push(argument1);
				try
				{
					program.Stack.Push(argument2);
					try
					{
						object tempValue = _equalNode.Execute(program);
						if ((tempValue == null) || !(bool)tempValue)
							throw new RuntimeException(RuntimeException.Codes.InvalidRowJoin);
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
			Row result = new Row(program.ValueManager, DataType);
			try
			{
				((IRow)argument1).CopyTo(result);
				((IRow)argument2).CopyTo(result);
				return result;
			}
			catch
			{	
				result.Dispose();
				throw;
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			InnerJoinExpression expression = new InnerJoinExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			expression.Modifiers = Modifiers;
			return expression;
		}
	}
	
	public abstract class UnaryRowNode : UnaryInstructionNode
	{
		public new Schema.RowType DataType { get { return (Schema.RowType)_dataType; } }
		public Schema.RowType SourceRowType { get { return (Schema.RowType)Nodes[0].DataType; } }
	}
	
	public abstract class RowProjectNodeBase : UnaryRowNode
	{
		// ColumnNames
		#if USETYPEDLIST
		protected TypedList FColumnNames = new TypedList(typeof(string), false);
		public TypedList ColumnNames { get { return FColumnNames; } }
		#else
		protected BaseList<string> _columnNames = new BaseList<string>();
		public BaseList<string> ColumnNames { get { return _columnNames; } }
		#endif

		protected override void InternalClone(PlanNode newNode)
		{
			base.InternalClone(newNode);

			var newRowProjectNodeBase = (RowProjectNodeBase)newNode;
			newRowProjectNodeBase._columnNames = _columnNames;
		}
		
		public override object InternalExecute(Program program, object argument)
		{
			#if NILPROPOGATION
			if (argument == null)
				return null;
			#endif
			
			Row row = new Row(program.ValueManager, DataType);
			try
			{
				((IRow)argument).CopyTo(row);
				return row;
			}
			catch
			{
				row.Dispose();
				throw;
			}
		}
	}
	
	public class RowProjectNode : RowProjectNodeBase
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.RowType();
			foreach (string columnName in _columnNames)
				DataType.Columns.Add(SourceRowType.Columns[columnName].Copy());
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			ProjectExpression expression = new ProjectExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			foreach (Schema.Column column in DataType.Columns)
				expression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(column.Name)));
			expression.Modifiers = Modifiers;
			return expression;
		}
	}
	
	public class RowRemoveNode : RowProjectNodeBase
	{
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.RowType();
			
			foreach (Schema.Column column in SourceRowType.Columns)
				DataType.Columns.Add(column.Copy());
				
			foreach (string columnName in _columnNames)
				DataType.Columns.Remove(DataType.Columns[columnName]);
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			RemoveExpression expression = new RemoveExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			foreach (Schema.Column column in SourceRowType.Columns)
				if (!DataType.Columns.ContainsName(column.Name))
					expression.Columns.Add(new ColumnExpression(Schema.Object.EnsureRooted(column.Name)));
			expression.Modifiers = Modifiers;
			return expression;
		}
	}
}
