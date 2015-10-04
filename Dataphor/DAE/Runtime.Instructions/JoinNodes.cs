/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define UseElaborable
#define USEUNIFIEDJOINKEYINFERENCE // Unified join key inference uses the same algorithm for all joins. See InferJoinKeys for more information.
//#define DISALLOWMANYTOMANYOUTERJOINS
#define USENAMEDROWVARIABLES
	
using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;

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

	// Root node for all join and semi operators
	public abstract class ConditionedTableNode : BinaryTableNode
	{
		protected Schema.JoinKey _leftKey = new Schema.JoinKey();
		public Schema.JoinKey LeftKey
		{
			get { return _leftKey; }
			set { _leftKey = value; }
		}
		
		protected Schema.JoinKey _rightKey = new Schema.JoinKey();
		public Schema.JoinKey RightKey
		{
			get { return _rightKey; }
			set { _rightKey = value; }
		}
		
		protected Schema.Order _joinOrder;
		public Schema.Order JoinOrder
		{
			get { return _joinOrder; }
			set { _joinOrder = value; }
		}
		
		protected bool _isNatural;
		public bool IsNatural
		{
			get { return _isNatural; }
			set { _isNatural = value; }
		}
		
		private bool _enforcePredicate = false;
		public bool EnforcePredicate
		{
			get { return _enforcePredicate; }
			set { _enforcePredicate = value; }
		}
		
		protected Expression CollapseQualifierExpression(QualifierExpression expression)
		{
			if (expression.LeftExpression is QualifierExpression)
				expression.LeftExpression = CollapseQualifierExpression((QualifierExpression)expression.LeftExpression);
			if (expression.RightExpression is QualifierExpression)
				expression.RightExpression = CollapseQualifierExpression((QualifierExpression)expression.RightExpression);
			if ((expression.LeftExpression is IdentifierExpression) && (expression.RightExpression is IdentifierExpression))
				return new IdentifierExpression(String.Format("{0}{1}{2}", ((IdentifierExpression)expression.LeftExpression).Identifier, Keywords.Qualifier, ((IdentifierExpression)expression.RightExpression).Identifier));
			else
				throw new CompilerException(CompilerException.Codes.UnableToCollapseQualifierExpression, expression);
		}
		
		protected void CollapseJoinExpression(Expression expression)
		{
			if (expression is BinaryExpression)
			{
				BinaryExpression binaryExpression = (BinaryExpression)expression;
				if (binaryExpression.LeftExpression is BinaryExpression)
					CollapseJoinExpression(binaryExpression.LeftExpression);
				else if (binaryExpression.LeftExpression is QualifierExpression)
					binaryExpression.LeftExpression = CollapseQualifierExpression((QualifierExpression)binaryExpression.LeftExpression);
				
				if (binaryExpression.RightExpression is BinaryExpression)
					CollapseJoinExpression(binaryExpression.RightExpression);
				else if (binaryExpression.RightExpression is QualifierExpression)
					binaryExpression.RightExpression = CollapseQualifierExpression((QualifierExpression)binaryExpression.RightExpression);
			}
		}
		
		protected bool IsEquiJoin(Expression expression)
		{
			// TODO: IsEquiJoin should be a semantic determination, not a syntactic one...
			CollapseJoinExpression(expression);
			
			if (expression is BinaryExpression)
			{
				BinaryExpression localExpression = (BinaryExpression)expression;
				if (String.Compare(localExpression.Instruction, Instructions.And) == 0)
					return IsEquiJoin(localExpression.LeftExpression) && IsEquiJoin(localExpression.RightExpression);
				
				if (String.Compare(localExpression.Instruction, Instructions.Equal) == 0)
				{
					if ((localExpression.LeftExpression is IdentifierExpression) && (localExpression.RightExpression is IdentifierExpression))
					{
						string leftIdentifier = ((IdentifierExpression)localExpression.LeftExpression).Identifier;
						string rightIdentifier = ((IdentifierExpression)localExpression.RightExpression).Identifier;
						if (leftIdentifier.IndexOf(Keywords.Left) == 0)
							leftIdentifier = Schema.Object.Dequalify(leftIdentifier);
						if (rightIdentifier.IndexOf(Keywords.Right) == 0)
							rightIdentifier = Schema.Object.Dequalify(rightIdentifier);
							
						if (!LeftTableType.Columns.Contains(leftIdentifier) && !RightTableType.Columns.Contains(leftIdentifier))
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, localExpression.LeftExpression, leftIdentifier);
							
						if (LeftTableType.Columns.Contains(leftIdentifier) && RightTableType.Columns.Contains(leftIdentifier))
							throw new CompilerException
							(
								CompilerException.Codes.AmbiguousIdentifier, 
								leftIdentifier, 
								ExceptionUtility.StringsToCommaList
								(
									new string[]
									{
										LeftTableType.Columns[leftIdentifier].Name,
										RightTableType.Columns[leftIdentifier].Name
									}
								)
							);
						
						if (!LeftTableType.Columns.Contains(rightIdentifier) && !RightTableType.Columns.Contains(rightIdentifier))
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, localExpression.RightExpression, rightIdentifier);
							
						if (LeftTableType.Columns.Contains(rightIdentifier) && RightTableType.Columns.Contains(rightIdentifier))
							throw new CompilerException
							(
								CompilerException.Codes.AmbiguousIdentifier,
								rightIdentifier,
								ExceptionUtility.StringsToCommaList
								(
									new string[]
									{
										LeftTableType.Columns[rightIdentifier].Name,
										RightTableType.Columns[rightIdentifier].Name
									}
								)
							);
							
						return
							(LeftTableType.Columns.Contains(leftIdentifier) && RightTableType.Columns.Contains(rightIdentifier)) ||
							(LeftTableType.Columns.Contains(rightIdentifier) && RightTableType.Columns.Contains(leftIdentifier));
					}
				}
			}
			
			return false;
		}
		
		protected void DetermineJoinKeys(Expression expression)
		{
            // !!! Assumption: IsEquiJoin == true;
            BinaryExpression localExpression = (BinaryExpression)expression;
            if (String.Compare(localExpression.Instruction, Instructions.And) == 0)
            {
                DetermineJoinKeys(localExpression.LeftExpression);
                DetermineJoinKeys(localExpression.RightExpression);
            }
            else if (String.Compare(localExpression.Instruction, Instructions.Equal) == 0)
            {
				string leftIdentifier = ((IdentifierExpression)localExpression.LeftExpression).Identifier;
				string rightIdentifier = ((IdentifierExpression)localExpression.RightExpression).Identifier;
				if (leftIdentifier.IndexOf(Keywords.Left) == 0)
					leftIdentifier = Schema.Object.Dequalify(leftIdentifier);
				if (rightIdentifier.IndexOf(Keywords.Right) == 0)
					rightIdentifier = Schema.Object.Dequalify(rightIdentifier);

				Schema.TableVarColumn column;
                if (LeftTableVar.Columns.Contains(leftIdentifier))
                {
					column = LeftTableVar.Columns[leftIdentifier];
                    _leftKey.Columns.Add(column);
                    column = RightTableVar.Columns[rightIdentifier];
                    _rightKey.Columns.Add(column);
                }
                else
                {
					column = RightTableVar.Columns[leftIdentifier];
                    _rightKey.Columns.Add(column);
                    column = LeftTableVar.Columns[rightIdentifier];
                    _leftKey.Columns.Add(column);
                }
            }
		}

		protected bool IsJoinOrder(Plan plan, Schema.JoinKey joinKey, TableNode node)
		{
			return (node.Supports(CursorCapability.Searchable) && (node.Order != null) && (Compiler.OrderFromKey(plan, joinKey.Columns).Equivalent(node.Order)));
		}

		protected Expression _expression;
		public Expression Expression
		{
			get { return _expression; }
			set { _expression = value; }
		}
		
		protected virtual Expression GenerateJoinExpression()
		{
			// foreach common column name
			//   generate an expression of the form left.column = right.column [and...]
			Expression expression = null;
			BinaryExpression equalExpression;
			int columnIndex;
			Schema.TableVarColumn leftColumn;
			Schema.TableVarColumn rightColumn;
			for (int index = 0; index < LeftTableVar.Columns.Count; index++)
			{
				leftColumn = LeftTableVar.Columns[index];
				columnIndex = RightTableVar.Columns.IndexOfName(leftColumn.Name);
				if (columnIndex >= 0)
				{
					rightColumn = RightTableVar.Columns[columnIndex];
					equalExpression = 
						new BinaryExpression
						(
							#if USENAMEDROWVARIABLES
							new QualifierExpression(new IdentifierExpression(Keywords.Left), new IdentifierExpression(leftColumn.Name)),
							#else
							new IdentifierExpression(Schema.Object.Qualify(leftColumn.Name, Keywords.Left)), 
							#endif
							Instructions.Equal, 
							#if USENAMEDROWVARIABLES
							new QualifierExpression(new IdentifierExpression(Keywords.Right), new IdentifierExpression(rightColumn.Name))
							#else
							new IdentifierExpression(Schema.Object.Qualify(LRightColumn.Name, Keywords.Right))
							#endif
						);
						
					_leftKey.Columns.Add(leftColumn);
					_rightKey.Columns.Add(rightColumn);
						
					if (expression == null)
						expression = equalExpression;
					else
						expression = 
							new BinaryExpression
							(
								expression,
								Instructions.And,
								equalExpression
							);
				}
			}
			return expression;
		}
				
		protected virtual void InternalCreateDataType(Plan plan)
		{
			_dataType = new Schema.TableType();
		}

		// LeftExistsNode
		protected PlanNode _leftExistsNode;
		public PlanNode LeftExistsNode
		{
			get { return _leftExistsNode; }
			set { _leftExistsNode = value; }
		}
		
		// RightExistsNode
		protected PlanNode _rightExistsNode;
		public PlanNode RightExistsNode
		{
			get { return _rightExistsNode; }
			set { _rightExistsNode = value; }
		}
		
		protected bool LeftExists(Program program, IRow row)
		{
			EnsureLeftExistsNode(program.Plan);
			PushNewRow(program, row);
			try
			{
				return (bool)_leftExistsNode.Execute(program);
			}
			finally
			{
				PopRow(program);
			}
		}
		
		protected bool RightExists(Program program, IRow row)
		{
			EnsureRightExistsNode(program.Plan);
			PushNewRow(program, row);
			try
			{
				return (bool)_rightExistsNode.Execute(program);
			}
			finally
			{
				PopRow(program);
			}
		}
		
		protected void EnsureLeftExistsNode(Plan plan)
		{
			if (_leftExistsNode == null)
			{
				ApplicationTransaction transaction = null;
				if (plan.ApplicationTransactionID != Guid.Empty)
					transaction = plan.GetApplicationTransaction();
				try
				{
					if (transaction != null)
						transaction.PushLookup();
					try
					{
						plan.PushATCreationContext();
						try
						{
							plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(plan, _symbols);
								try
								{
									plan.EnterRowContext();
									try
									{
										#if USENAMEDROWVARIABLES
										plan.Symbols.Push(new Symbol(Keywords.New, DataType.RowType));
										#else
										APlan.Symbols.Push(new Symbol(String.Empty, DataType.NewRowType));
										#endif
										try
										{
											_leftExistsNode =
												Compiler.Compile
												(
													plan,
													new UnaryExpression
													(
														Instructions.Exists,
														new RestrictExpression
														(
															(Expression)LeftNode.EmitStatement(EmitMode.ForCopy),
															#if USENAMEDROWVARIABLES
															Compiler.BuildKeyEqualExpression
															(
																plan,
																String.Empty,
																Keywords.New,
																_leftKey.Columns,
																_leftKey.Columns
															)
															#else
															Compiler.BuildKeyEqualExpression
															(
																APlan,
																new Schema.RowType(FLeftKey.Columns).Columns,
																new Schema.RowType(FLeftKey.Columns, Keywords.New).Columns
															)
															#endif
														)
													)
												);
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
									PopSymbols(plan, _symbols);
								}
							}
							finally
							{
								plan.PopCursorContext();
							}
						}
						finally
						{
							plan.PopATCreationContext();
						}
					}
					finally
					{
						if (transaction != null)
							transaction.PopLookup();
					}
				}
				finally
				{
					if (transaction != null)
						Monitor.Exit(transaction);
				}
			}
		}
		
		protected void EnsureRightExistsNode(Plan plan)
		{
			if (_rightExistsNode == null)
			{
				ApplicationTransaction transaction = null;
				if (plan.ApplicationTransactionID != Guid.Empty)
					transaction = plan.GetApplicationTransaction();
				try
				{
					if (transaction != null)
						transaction.PushLookup();
					try
					{
						plan.PushATCreationContext();
						try
						{
							plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(plan, _symbols);
								try
								{
									plan.EnterRowContext();
									try
									{
										#if USENAMEDROWVARIABLES
										plan.Symbols.Push(new Symbol(Keywords.New, DataType.RowType));
										#else
										APlan.Symbols.Push(new Symbol(String.Empty, DataType.NewRowType));
										#endif
										try
										{
											_rightExistsNode =
												Compiler.Compile
												(
													plan,
													new UnaryExpression
													(
														Instructions.Exists,
														new RestrictExpression
														(
															(Expression)RightNode.EmitStatement(EmitMode.ForCopy),
															#if USENAMEDROWVARIABLES
															Compiler.BuildKeyEqualExpression
															(
																plan,
																String.Empty,
																Keywords.New,
																_rightKey.Columns,
																_rightKey.Columns
															)
															#else
															Compiler.BuildKeyEqualExpression
															(
																APlan,
																new Schema.RowType(FRightKey.Columns).Columns,
																new Schema.RowType(FRightKey.Columns, Keywords.New).Columns
															)
															#endif
														)
													)
												);
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
									PopSymbols(plan, _symbols);
								}
							}
							finally
							{
								plan.PopCursorContext();
							}
						}
						finally
						{
							plan.PopATCreationContext();
						}
					}
					finally
					{
						if (transaction != null)
							transaction.PopLookup();
					}
				}
				finally
				{
					if (transaction != null)
						Monitor.Exit(transaction);
				}
			}
		}
	}
	
	public abstract class SemiTableNode : ConditionedTableNode
	{
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			Row leftRow = new Row(program.ValueManager, LeftNode.DataType.RowType);
			try
			{
				row.CopyTo(leftRow);
				LeftNode.JoinApplicationTransaction(program, leftRow);
			}
			finally
			{
				leftRow.Dispose();
			}
		}

		protected virtual void DetermineSemiTableModifiers(Plan plan)
		{
			PropagateInsertRight = PropagateAction.False;
			PropagateUpdateRight = false;
			PropagateDeleteRight = false;
			PropagateDefaultRight = false;
			PropagateValidateRight = false;
			PropagateChangeRight = false;
			ShouldTranslateRight = false;
			
			if (Modifiers != null)
			{
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
				PropagateInsertLeft = (PropagateAction)Enum.Parse(typeof(PropagateAction), LanguageModifiers.GetModifier(Modifiers, "Left.PropagateInsert", PropagateInsertLeft.ToString()), true);
				PropagateInsertRight = (PropagateAction)Enum.Parse(typeof(PropagateAction), LanguageModifiers.GetModifier(Modifiers, "Right.PropagateInsert", PropagateInsertRight.ToString()), true);
				PropagateUpdateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateUpdate", PropagateUpdateLeft.ToString()));
				PropagateUpdateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateUpdate", PropagateUpdateRight.ToString()));
				PropagateDeleteLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateDelete", PropagateDeleteLeft.ToString()));
				PropagateDeleteRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateDelete", PropagateDeleteRight.ToString()));
				PropagateDefaultLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateDefault", PropagateDefaultLeft.ToString()));
				PropagateDefaultRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateDefault", PropagateDefaultRight.ToString()));
				PropagateValidateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateValidate", PropagateValidateLeft.ToString()));
				PropagateValidateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateValidate", PropagateValidateRight.ToString()));
				PropagateChangeLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateChange", PropagateChangeLeft.ToString()));
				PropagateChangeRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateChange", PropagateChangeRight.ToString()));
				ShouldTranslateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.ShouldTranslate", ShouldTranslateLeft.ToString()));
				ShouldTranslateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.ShouldTranslate", ShouldTranslateRight.ToString()));
			}
			
			// Set IsNilable for each column in the left and right tables based on PropagateXXXLeft and Right
			bool isLeftNilable = (PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft;
			bool isRightNilable = (PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
			
			int rightColumnIndex = RightTableVar.Columns.Count == 0 ? LeftTableVar.Columns.Count : TableVar.Columns.IndexOfName(RightTableVar.Columns[0].Name);
			
			for (int index = 0; index < TableVar.Columns.Count; index++)
			{
				if (index < rightColumnIndex)
				{
					if (isLeftNilable)
						TableVar.Columns[index].IsNilable = isLeftNilable;
				}
				else
				{
					if (isRightNilable)
						TableVar.Columns[index].IsNilable = isRightNilable;
				}
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			InternalCreateDataType(plan);

			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(LeftTableVar.MetaData);
			
			if (_isNatural)
			{
				_expression = GenerateJoinExpression();

				if ((_expression == null) && !plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.PossiblyIncorrectSemiTableExpression, CompilerErrorLevel.Warning, plan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString()));
			}
			else
			{
				if (!IsEquiJoin(_expression))
					throw new CompilerException(CompilerException.Codes.JoinMustBeEquiJoin, _expression);
					
				DetermineJoinKeys(_expression);
			}
			
			foreach (Schema.Key key in LeftTableVar.Keys)
				if (_leftKey.Columns.IsSupersetOf(key.Columns))
				{
					_leftKey.IsUnique = true;
					break;
				}
				
			foreach (Schema.Key key in RightTableVar.Keys)
				if (_rightKey.Columns.IsSupersetOf(key.Columns))
				{
					_rightKey.IsUnique = true;
					break;
				}
				
			// Determine columns
			CopyTableVarColumns(LeftTableVar.Columns);
			
			DetermineRemotable(plan);

			// Determine the keys
			CopyPreservedKeys(LeftTableVar.Keys);
			
			// Determine modifiers
			DetermineSemiTableModifiers(plan);
			
			// Determine orders			
			CopyPreservedOrders(LeftTableVar.Orders);
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);
			
			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, LeftTableVar);
			#endif
			
			plan.EnterRowContext();
			try
			{			
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftTableType.Columns, Keywords.Left)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, RightTableType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightTableType.Columns, Keywords.Right)));
					#endif
					try
					{
						if (_expression != null)
						{
							PlanNode planNode = Compiler.CompileExpression(plan, _expression);
							if (!(planNode.DataType.Is(plan.DataTypes.SystemBoolean)))
								throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, _expression);
							Nodes.Add(planNode);
						}
						else
							Nodes.Add(new ValueNode(plan.DataTypes.SystemBoolean, true));
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
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			plan.PushCursorContext(new CursorContext(plan.CursorContext.CursorType, plan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, plan.CursorContext.CursorIsolation));
			try
			{
				#if USEVISIT
				Nodes[1] = visitor.Visit(plan, Nodes[1]);
				#else
				Nodes[1].BindingTraversal(plan, visitor);
				#endif
			}
			finally
			{
				plan.PopCursorContext();
			}
			
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftTableType.Columns, Keywords.Left)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, RightTableType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightTableType.Columns, Keywords.Right)));
					#endif
					try
					{
						#if USEVISIT
						Nodes[2] = visitor.Visit(plan, Nodes[2]);
						#else
						Nodes[2].BindingTraversal(plan, visitor);
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
		
		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if (!DeviceSupported)
				DetermineSemiTableAlgorithm(plan);
		}
		
		protected abstract void DetermineSemiTableAlgorithm(Plan plan);
		
		public virtual void DetermineCursorCapabilities(Plan plan)
		{
			_cursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					plan.CursorContext.CursorCapabilities &
					LeftNode.CursorCapabilities &
					CursorCapability.Updateable
				) |
				(
					plan.CursorContext.CursorCapabilities &
					(LeftNode.CursorCapabilities | RightNode.CursorCapabilities) &
					CursorCapability.Elaborable
				) |
				(
					plan.CursorContext.CursorCapabilities &
					(LeftNode.CursorCapabilities | RightNode.CursorCapabilities) &
					CursorCapability.Elaborable
				);
		}

		public override void DetermineCursorBehavior(Plan plan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				_cursorType = CursorType.Dynamic;
			else
				_cursorType = CursorType.Static;

			_requestedCursorType = plan.CursorContext.CursorType;

			DetermineCursorCapabilities(plan);

			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}

		protected Type _semiTableAlgorithm;

		public override void DetermineRemotable(Plan plan)
		{
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldChange = PropagateChangeLeft && (_tableVar.ShouldChange || LeftTableVar.ShouldChange);
			_tableVar.ShouldDefault = PropagateDefaultLeft && (_tableVar.ShouldDefault || LeftTableVar.ShouldDefault);
			_tableVar.ShouldValidate = PropagateValidateLeft && (_tableVar.ShouldValidate || LeftTableVar.ShouldValidate);
			
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				Schema.TableVarColumn sourceColumn = LeftTableVar.Columns[LeftTableVar.Columns.IndexOfName(column.Name)];

				column.ShouldChange = PropagateChangeLeft && (column.ShouldChange || sourceColumn.ShouldChange);
				_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;

				column.ShouldDefault = PropagateDefaultLeft && (column.ShouldDefault || sourceColumn.ShouldDefault);
				_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;

				column.ShouldValidate = PropagateValidateLeft && (column.ShouldValidate || sourceColumn.ShouldValidate);
				_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;
			}
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if (isDescending && PropagateDefaultLeft)
				return LeftNode.Default(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			if (PropagateChangeLeft)
				return LeftNode.Change(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (isDescending && PropagateValidateLeft)
				return LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName);
			return false;
		}
		
		protected abstract void ValidatePredicate(Program program, IRow row);
		
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True :
					if (!uncheckedValue)
						ValidatePredicate(program, newRow);
					LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					if (!uncheckedValue)
						ValidatePredicate(program, newRow);
					
					using (Row leftRow = new Row(program.ValueManager, LeftNode.DataType.RowType))
					{
						newRow.CopyTo(leftRow);
						using (IRow currentRow = LeftNode.Select(program, leftRow))
						{
							if (currentRow != null)
							{
								if (PropagateInsertLeft == PropagateAction.Ensure)
									LeftNode.Update(program, currentRow, newRow, valueFlags, false, uncheckedValue);
							}
							else
								LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
					}
				break;
			}
		}
		
/*
		protected override void InternalBeforeDeviceInsert(Row ARow, Program AProgram)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.BeforeDeviceInsert(ARow, AProgram);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			
			LeftNode.BeforeDeviceInsert(ARow, AProgram);
		}
		
		protected override void InternalAfterDeviceInsert(Row ARow, Program AProgram)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.AfterDeviceInsert(ARow, AProgram);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			
			LeftNode.AfterDeviceInsert(ARow, AProgram);
		}
*/
		
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateUpdateLeft)
			{
				if (!uncheckedValue)
					ValidatePredicate(program, newRow);
				LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
		}
		
/*
		protected override void InternalBeforeDeviceUpdate(Row AOldRow, Row ANewRow, Program AProgram)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.BeforeDeviceInsert(ANewRow, AProgram);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
				
			LeftNode.BeforeDeviceUpdate(AOldRow, ANewRow, AProgram);
		}
		
		protected override void InternalAfterDeviceUpdate(Row AOldRow, Row ANewRow, Program AProgram)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.AfterDeviceInsert(ANewRow, AProgram);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
				
			LeftNode.AfterDeviceUpdate(AOldRow, ANewRow, AProgram);
		}
*/
		
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (PropagateDeleteLeft)
				LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
		}
		
/*
		protected override void InternalBeforeDeviceDelete(Row ARow, Program AProgram)
		{
			LeftNode.BeforeDeviceDelete(ARow, AProgram);
		}
		
		protected override void InternalAfterDeviceDelete(Row ARow, Program AProgram)
		{
			LeftNode.AfterDeviceDelete(ARow, AProgram);
		}
*/

		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			writer.WriteAttributeString("Condition", Nodes[2].SafeEmitStatementAsString());
		}
	}
	
	public class HavingNode : SemiTableNode
	{
		public override object InternalExecute(Program program)
		{
			HavingTable table = (HavingTable)Activator.CreateInstance(_semiTableAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, program}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			HavingExpression expression = new HavingExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			if (!IsNatural)
				expression.Condition = _expression;
			expression.Modifiers = Modifiers;
			return expression;
		}

		protected override void ValidatePredicate(Program program, IRow row)
		{
			if (EnforcePredicate && !RightExists(program, row))
				throw new RuntimeException(RuntimeException.Codes.RowViolatesHavingPredicate, ErrorSeverity.User);
		}
		
		protected override void DetermineSemiTableAlgorithm(Plan plan)
		{
			_semiTableAlgorithm = typeof(SearchedHavingTable);

			// ensure that the right side is a searchable node
			Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, RightKey.Columns);
		}
	}
	
	public class WithoutNode : SemiTableNode
	{
		public override object InternalExecute(Program program)
		{
			WithoutTable table = (WithoutTable)Activator.CreateInstance(_semiTableAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, program}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			WithoutExpression expression = new WithoutExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			if (!IsNatural)
				expression.Condition = _expression;
			expression.Modifiers = Modifiers;
			return expression;
		}

		protected override void ValidatePredicate(Program program, IRow row)
		{
			if (EnforcePredicate && RightExists(program, row))
				throw new RuntimeException(RuntimeException.Codes.RowViolatesWithoutPredicate, ErrorSeverity.User);
		}

		protected override void DetermineSemiTableAlgorithm(Plan plan)
		{
			_semiTableAlgorithm = typeof(SearchedWithoutTable);

			// ensure that the right side is a searchable node
			Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, RightKey.Columns);
		}
	}
	
	// operator iJoin(table{}, table{}, bool) : table{}	
	public class JoinNode : ConditionedTableNode
	{		
		protected virtual void DetermineLeftColumns(Plan plan, bool isNilable)
		{
			CopyTableVarColumns(LeftTableVar.Columns, isNilable);
			
			Schema.TableVarColumn newColumn;
			foreach (Schema.TableVarColumn column in LeftKey.Columns)
			{
				newColumn = TableVar.Columns[column];
				newColumn.IsChangeRemotable = false;
				newColumn.IsDefaultRemotable = false;
			}
		}
		
		protected virtual void DetermineRightColumns(Plan plan, bool isNilable)
		{
			Schema.TableVarColumn newColumn;

			if (!_isNatural)
				CopyTableVarColumns(RightTableVar.Columns, isNilable);
			else
			{
				foreach (Schema.TableVarColumn column in RightTableVar.Columns)
				{
					if (!_rightKey.Columns.ContainsName(column.Name))
					{
						newColumn = CopyTableVarColumn(column);
						newColumn.IsNilable = newColumn.IsNilable || isNilable;
						DataType.Columns.Add(newColumn.Column);
						TableVar.Columns.Add(newColumn);
					}
					else
					{	
						Schema.TableVarColumn leftColumn = TableVar.Columns[column];
						leftColumn.IsDefaultRemotable = leftColumn.IsDefaultRemotable && column.IsDefaultRemotable;
						leftColumn.IsChangeRemotable = leftColumn.IsChangeRemotable && column.IsChangeRemotable;
						leftColumn.IsValidateRemotable = leftColumn.IsValidateRemotable && column.IsValidateRemotable;
						leftColumn.IsNilable = leftColumn.IsNilable || column.IsNilable || isNilable;
						leftColumn.JoinInheritMetaData(column.MetaData);
					}
				}
			}

			foreach (Schema.TableVarColumn column in LeftKey.Columns)
			{
				newColumn = TableVar.Columns[column];
				newColumn.IsChangeRemotable = false;
				newColumn.IsDefaultRemotable = false;
			}
		}
		
		protected JoinCardinality _cardinality;
		public JoinCardinality Cardinality { get { return _cardinality; } set { _cardinality = value; } }
		
		protected virtual void DetermineCardinality(Plan plan)
		{
			if (_leftKey.IsUnique)
				if (_rightKey.IsUnique)
					_cardinality = JoinCardinality.OneToOne;
				else
					_cardinality = JoinCardinality.OneToMany;
			else
				if (_rightKey.IsUnique)
					_cardinality = JoinCardinality.ManyToOne;
				else
					_cardinality = JoinCardinality.ManyToMany;
		}
		
		protected Type _joinAlgorithm;
		public Type JoinAlgorithm { get { return _joinAlgorithm; } }
		
		protected override void WritePlanAttributes(System.Xml.XmlWriter writer)
		{
			base.WritePlanAttributes(writer);
			writer.WriteAttributeString("Cardinality", _cardinality.ToString());
			if (_joinAlgorithm != null)
				writer.WriteAttributeString("Algorithm", _joinAlgorithm.ToString());
			writer.WriteAttributeString("Condition", Nodes[2].SafeEmitStatementAsString());
		}
		
		protected bool JoinOrdersAscendingCompatible(Schema.Order leftOrder, Schema.Order rightOrder)
		{
			for (int index = 0; index < _leftKey.Columns.Count; index++)
				if (leftOrder.Columns[index].Ascending != rightOrder.Columns[index].Ascending)
					return false;
			return true;
		}
		
		protected virtual void DetermineJoinAlgorithm(Plan plan)
		{
			if (_expression == null)
				_joinAlgorithm = typeof(TimesTable);
			else
			{
				_joinOrder = Compiler.OrderFromKey(plan, _leftKey.Columns);
				Schema.OrderColumn column;
				for (int index = 0; index < _joinOrder.Columns.Count; index++)
				{
					column = _joinOrder.Columns[index];
					column.Sort = Compiler.GetSort(plan, column.Column.DataType);
					plan.AttachDependency(column.Sort);
					column.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to one side
				if (!IsJoinOrder(plan, _leftKey, LeftNode) && !IsJoinOrder(plan, _rightKey, RightNode))
				{
					// if the right side is unique, order it
					// else if the left side is unique, order it
					// else order the right side (should be whichever side has the least cardinality)
					if (_rightKey.IsUnique)
						Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, _rightKey.Columns);
					else if (_leftKey.IsUnique)
						Nodes[0] = Compiler.EnsureSearchableNode(plan, LeftNode, _leftKey.Columns);
					else
						Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, _rightKey.Columns);
				}

				if (IsJoinOrder(plan, _leftKey, LeftNode) && IsJoinOrder(plan, _rightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (_leftKey.IsUnique || _rightKey.IsUnique))
				{
					// if both inputs are ordered by join keys and one or both join keys are unique
						// use a merge join algorithm
					if (_leftKey.IsUnique && _rightKey.IsUnique)
					{
						_joinAlgorithm = typeof(UniqueMergeJoinTable);
						Order = CopyOrder(LeftNode.Order);
					}
					else if (_leftKey.IsUnique)
					{
						_joinAlgorithm = typeof(LeftUniqueMergeJoinTable);
						Order = CopyOrder(RightNode.Order);
					}
					else
					{
						_joinAlgorithm = typeof(RightUniqueMergeJoinTable);
						Order = CopyOrder(LeftNode.Order);
					}
				}
				else if (IsJoinOrder(plan, _leftKey, LeftNode))
				{
					// else if one input is ordered by join keys
						// use a searched join algorithm
					if (_leftKey.IsUnique)
						_joinAlgorithm = typeof(LeftUniqueSearchedJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueLeftSearchedJoinTable);

					if (RightNode.Order != null)
						Order = CopyOrder(RightNode.Order);
				}
				else if (IsJoinOrder(plan, _rightKey, RightNode))
				{
					if (_rightKey.IsUnique)
						_joinAlgorithm = typeof(RightUniqueSearchedJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueRightSearchedJoinTable);
						
					if (LeftNode.Order != null)
						Order = CopyOrder(LeftNode.Order);
				}
				else
				{
					// NOTE - this code is not being hit right now because all joins are ordered by the code above
					// use a nested loop join
					// note that if both left and right sides are unique this selection is arbitrary (possible cost based optimization)
					if (_leftKey.IsUnique)
						_joinAlgorithm = typeof(LeftUniqueNestedLoopJoinTable);
					else if (_rightKey.IsUnique)
						_joinAlgorithm = typeof(RightUniqueNestedLoopJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueNestedLoopJoinTable);
				}
			}
		}
		
		protected virtual void JoinLeftApplicationTransaction(Program program, IRow row)
		{
			Row leftRow = new Row(program.ValueManager, LeftNode.DataType.RowType);
			try
			{
				row.CopyTo(leftRow);
				LeftNode.JoinApplicationTransaction(program, leftRow);
			}
			finally
			{
				leftRow.Dispose();
			}
		}
		
		protected virtual void JoinRightApplicationTransaction(Program program, IRow row)
		{
			Row rightRow = new Row(program.ValueManager, RightNode.DataType.RowType);
			try
			{
				row.CopyTo(rightRow);
				RightNode.JoinApplicationTransaction(program, rightRow);
			}
			finally
			{
				rightRow.Dispose();
			}
		}
		
		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			JoinLeftApplicationTransaction(program, row);
			if (!_isLookup || _isDetailLookup)
				JoinRightApplicationTransaction(program, row);
		}
		
		protected virtual void DetermineDetailLookup(Plan plan)
		{
			if ((Modifiers != null) && (Modifiers.Contains("IsDetailLookup")))
				_isDetailLookup = Boolean.Parse(Modifiers["IsDetailLookup"].Value);
			else
			{
				_isDetailLookup = false;
				
				// If this is a lookup and the left join columns are a non-trivial proper superset of any key of the left side, this is a detail lookup
				if (_isLookup)
					foreach (Schema.Key key in LeftTableVar.Keys)
						if ((key.Columns.Count > 0) && _leftKey.Columns.IsProperSupersetOf(key.Columns))
						{
							_isDetailLookup = true;
							break;
						}
				
				#if USEDETAILLOOKUP
				// If this is a lookup, the left key is not unique, and any key of the left table is a subset of the left key, this is a detail lookup
				if (FIsLookup && !FLeftKey.IsUnique)
					foreach (Schema.Key key in LeftTableVar.Keys)
						if (key.Columns.IsSubsetOf(FLeftKey.Columns))
						{
							FIsDetailLookup = true;
							break;
						}
				#endif
						
				#if USEINVARIANT
				// The invariant of any given tablevar is the intersection of the source columns of all source references that involve a key column.
				// If this is a lookup, the left key is not unique and the intersection of the invariant of the left table with the left join key is non-empty, this is a detail lookup.
				FIsDetailLookup = false;
				if (FIsLookup && !FLeftKey.IsUnique)
				{
					Schema.Key invariant = LeftNode.TableVar.CalculateInvariant();
					foreach (Schema.Column column in FLeftKey.Columns)
						if (invariant.Columns.Contains(column.Name))
						{
							FIsDetailLookup = true;
							break;
						}
				}
				#endif
			}
		}
		
		protected virtual void DetermineColumns(Plan plan)
		{
			DetermineLeftColumns(plan, false);
			DetermineRightColumns(plan, false);
		}
		
		protected bool _updateLeftToRight;
		public bool UpdateLeftToRight
		{
			get { return _updateLeftToRight; }
			set { _updateLeftToRight = value; }
		}
		
		protected bool _isLookup;
		public bool IsLookup
		{
			get { return _isLookup; }
			set { _isLookup = value; }
		}
		
		protected bool _isDetailLookup;
		public bool IsDetailLookup
		{
			get { return _isDetailLookup; }
			set { _isDetailLookup = value; }
		}
		
		protected bool _isIntersect;
		public bool IsIntersect
		{
			get { return _isIntersect; }
			set { _isIntersect = value; }
		}
		
		protected bool _isTimes;
		public bool IsTimes
		{
			get { return _isTimes; }
			set { _isTimes = value; }
		}
		
		protected virtual void DetermineManyToManyKeys(Plan plan)
		{
			// This algorithm is also used to infer keys for any cardinality changing outer (one-to-many or many-to-many)
			// If neither join key is unique
				// for each key of the left input
					// for each key of the right input
						// create a key based on all columns of the left key, excluding join columns that are not key columns of the right input,
							// extended with all columns of the right key, excluding join columns that are not key columns of the left input or are the correlated column of an existing column in the key
						// create a key based on all columns of the right key, excluding join columns that are not key columns of the left input,
							// extended with all columns of the left key, excluding join columns that are not key columns of the right input or are the correlated column of an existing column in the key
			Schema.Key key;
			foreach (Schema.Key leftKey in LeftTableVar.Keys)
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					key = new Schema.Key();
					key.IsInherited = true;
					key.IsSparse = leftKey.IsSparse || rightKey.IsSparse;
					foreach (Schema.TableVarColumn column in leftKey.Columns)
					{
						int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
						if ((leftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name)))
							key.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						Schema.TableVarColumn tableVarColumn = TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)];
						if 
						(
							(
								(rightKeyIndex < 0) || 
								(
									LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name) && 
									!key.Columns.ContainsName(LeftKey.Columns[rightKeyIndex].Name)
								)
							) && 
							!key.Columns.Contains(tableVarColumn)
						)
							key.Columns.Add(tableVarColumn);
					}

					if (!TableVar.Keys.Contains(key))
						TableVar.Keys.Add(key);
						
					key = new Schema.Key();
					key.IsInherited = true;
					key.IsSparse = leftKey.IsSparse || rightKey.IsSparse;
					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						if ((rightKeyIndex < 0) || (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name)))
							key.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}
						
					foreach (Schema.TableVarColumn column in leftKey.Columns)
					{
						int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
						Schema.TableVarColumn tableVarColumn = TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)];
						if
						(
							(
								(leftKeyIndex < 0) ||
								(
									RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name) &&
									!key.Columns.ContainsName(RightKey.Columns[leftKeyIndex].Name)
								)
							) &&
							!key.Columns.Contains(tableVarColumn)
						)
							key.Columns.Add(tableVarColumn);
					}
					
					if (!TableVar.Keys.Contains(key))
						TableVar.Keys.Add(key);
				}
				
			RemoveSuperKeys();
		}
		
		#if USEUNIFIEDJOINKEYINFERENCE
		// BTR -> The USEUNIFIEDJOINKEYINFERENCE define is introduced to make reversion easier if it becomes necessary, and to allow for easy reference of
		// the way the old algorithms worked. I am confident that this is now the correct algorithm for all join types.
		protected void InferJoinKeys(Plan plan, JoinCardinality cardinality, bool isLeftOuter, bool isRightOuter)
		{
			// foreach key of the left table var
				// foreach key of the right table var
					// copy the left key, adding all the columns of the right key that are not join columns
						// if the left key is sparse, or the join is a cardinality preserving right outer, the resulting key is sparse
					// copy the right key, adding all the columns of the left key that are not join columns
						// if the right key is sparse, or the join is a cardinality preserving left outer, the resulting key is sparse
			Schema.Key newKey;
			foreach (Schema.Key leftKey in LeftTableVar.Keys)
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse = 
						leftKey.IsSparse 
							|| 
							(
								isRightOuter 
									&& (!IsNatural || (IsNatural && !leftKey.Columns.IsSubsetOf(RightTableVar.Columns))) 
									&& ((cardinality == JoinCardinality.OneToOne) || (cardinality == JoinCardinality.OneToMany))
							);
							
					foreach (Schema.TableVarColumn column in leftKey.Columns)
						newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						
					foreach (Schema.TableVarColumn column in rightKey.Columns)
						if (!RightKey.Columns.ContainsName(column.Name) && !newKey.Columns.ContainsName(column.Name)) // if this is not a right join column, and it is not already part of the key
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
							
					if (newKey.Columns.Count == 0)
						newKey.IsSparse = false;
							
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);
							
					newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse =
						rightKey.IsSparse
							|| 
							(
								isLeftOuter 
									&& (!IsNatural || (IsNatural && !rightKey.Columns.IsSubsetOf(LeftTableVar.Columns))) 
									&& ((cardinality == JoinCardinality.OneToOne) || (cardinality == JoinCardinality.ManyToOne))
							);
							
					foreach (Schema.TableVarColumn column in leftKey.Columns)
						if (!LeftKey.Columns.ContainsName(column.Name) ) // if this is not a left join column
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);

					foreach (Schema.TableVarColumn column in rightKey.Columns)
						if (!newKey.Columns.ContainsName(column.Name)) // if this column is not already part of the key
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
							
					if (newKey.Columns.Count == 0)
						newKey.IsSparse = false;
						
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);
				}
				
			RemoveSuperKeys();
		}
		#endif
		
		protected virtual void DetermineJoinKey(Plan plan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(plan, Cardinality, false, false);
			#else
			if (FLeftKey.IsUnique && FRightKey.IsUnique)
			{
				// If both the left and right join keys are unique
					// foreach key of the left table var
						// copy the key
						// copy the key with all correlated columns replaced with the right join key columns (a no-op for natural joins)
					// foreach key of the right table var
						// copy the key
						// copy the key with all correlated columns replaced with the left join key columns (a no-op for natural joins)
				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					Schema.Key newKey = CopyKey(leftKey);
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = leftKey.IsSparse;
						foreach (Schema.TableVarColumn column in leftKey.Columns)
						{
							int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
							if (leftKeyIndex >= 0)
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}

						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
				
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					Schema.Key newKey = CopyKey(rightKey);
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = rightKey.IsSparse;
						foreach (Schema.TableVarColumn column in rightKey.Columns)
						{
							int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
							if (rightKeyIndex >= 0)
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
				
				RemoveSuperKeys();
			}
			else if (FLeftKey.IsUnique)
			{
				// If only the left join key is unique,
					// for each key of the right table var
						// copy the key, excluding join columns that are not key columns in the left
						// copy the key, excluding join columns that are not key columns in the left, with all correlated columns replaced with the left join key columns (a no-op for natural joins)
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse = rightKey.IsSparse;
					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						if ((rightKeyIndex < 0) || LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}
					
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = rightKey.IsSparse;
						foreach (Schema.TableVarColumn column in rightKey.Columns)
						{
							int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
							if (rightKeyIndex >= 0)
							{
								if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name))
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
							}
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
			}
			else if (FRightKey.IsUnique)
			{
				// If only the right join key is unique,
					// for each key of the left table var
						// copy the key, excluding join columns that are not key columns in the right
						// copy the key, excluding join columns that are not key columns in the right, with all correlated columns replaced with the right join key columns (a no-op for natural joins)
				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse = leftKey.IsSparse;
					
					foreach (Schema.TableVarColumn column in leftKey.Columns)
					{
						int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
						if ((leftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name)))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = leftKey.IsSparse;
						foreach (Schema.TableVarColumn column in leftKey.Columns)
						{
							int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
							if (leftKeyIndex >= 0)
							{
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name))
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
							}
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
			}
			else
			{
				DetermineManyToManyKeys(APlan);
			}
			#endif
		}
		
		protected virtual void DetermineOrders(Plan plan)
		{
			if (_leftKey.IsUnique && _rightKey.IsUnique)
			{
				CopyPreservedOrders(LeftTableVar.Orders);
				CopyPreservedOrders(RightTableVar.Orders);
			}
			else if (_leftKey.IsUnique)
				CopyPreservedOrders(RightTableVar.Orders);
			else if (_rightKey.IsUnique)
				CopyPreservedOrders(LeftTableVar.Orders);
		}
		
		protected override void DetermineModifiers(Plan plan)
		{
			base.DetermineModifiers(plan);
			
			if (Modifiers != null)
				IsTimes = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "IsTimes", IsTimes.ToString()));
		}
		
		protected virtual void DetermineJoinModifiers(Plan plan)
		{
			bool isLeft = !(this is RightOuterJoinNode);
			if (_isLookup)
			{
				if (isLeft)
				{
					PropagateInsertRight = PropagateAction.False;
					PropagateUpdateRight = false;
					PropagateDeleteRight = false;
					PropagateDefaultRight = false;
					PropagateValidateRight = false;
					PropagateChangeRight = false;
					ShouldTranslateRight = false;
					RetrieveRight = true;
					ClearRight = true;
				}
				else
				{
					PropagateInsertLeft = PropagateAction.False;
					PropagateUpdateLeft = false;
					PropagateDeleteLeft = false;
					PropagateDefaultLeft = false;
					PropagateValidateLeft = false;
					PropagateChangeLeft = false;
					ShouldTranslateLeft = false;
					RetrieveLeft = true;
					ClearLeft = true;
				}
			}
			
			switch (Cardinality)
			{
				case JoinCardinality.OneToMany :
					if (isLeft || !_isLookup)
						PropagateInsertLeft = PropagateAction.Ignore;
				break;
					
				case JoinCardinality.ManyToOne :
					if (!isLeft || !_isLookup)
						PropagateInsertRight = PropagateAction.Ignore;
				break;
					
				case JoinCardinality.ManyToMany :
					if (isLeft)
					{
						PropagateInsertLeft = PropagateAction.Ignore;
						if (!_isLookup)
							PropagateInsertRight = PropagateAction.Ignore;
					}
					else
					{
						if (!_isLookup)
							PropagateInsertLeft = PropagateAction.Ignore;
						PropagateInsertRight = PropagateAction.Ignore;
					}
				break;
			}
			
			if (Modifiers != null)
			{
				EnforcePredicate = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "EnforcePredicate", EnforcePredicate.ToString()));
				PropagateInsertLeft = (PropagateAction)Enum.Parse(typeof(PropagateAction), LanguageModifiers.GetModifier(Modifiers, "Left.PropagateInsert", PropagateInsertLeft.ToString()), true);
				PropagateInsertRight = (PropagateAction)Enum.Parse(typeof(PropagateAction), LanguageModifiers.GetModifier(Modifiers, "Right.PropagateInsert", PropagateInsertRight.ToString()), true);
				PropagateUpdateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateUpdate", PropagateUpdateLeft.ToString()));
				PropagateUpdateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateUpdate", PropagateUpdateRight.ToString()));
				PropagateDeleteLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateDelete", PropagateDeleteLeft.ToString()));
				PropagateDeleteRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateDelete", PropagateDeleteRight.ToString()));
				PropagateDefaultLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateDefault", PropagateDefaultLeft.ToString()));
				PropagateDefaultRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateDefault", PropagateDefaultRight.ToString()));
				PropagateValidateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateValidate", PropagateValidateLeft.ToString()));
				PropagateValidateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateValidate", PropagateValidateRight.ToString()));
				PropagateChangeLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.PropagateChange", PropagateChangeLeft.ToString()));
				PropagateChangeRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.PropagateChange", PropagateChangeRight.ToString()));
				ShouldTranslateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.ShouldTranslate", ShouldTranslateLeft.ToString()));
				ShouldTranslateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.ShouldTranslate", ShouldTranslateRight.ToString()));
				RetrieveLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.Retrieve", RetrieveLeft.ToString()));
				RetrieveRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.Retrieve", RetrieveRight.ToString()));
				ClearLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Left.Clear", ClearLeft.ToString()));
				ClearRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "Right.Clear", ClearRight.ToString()));
				CoordinateLeft = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "CoordinateLeft", CoordinateLeft.ToString()));
				CoordinateRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "CoordinateRight", CoordinateRight.ToString()));
				UpdateLeftToRight = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "UpdateLeftToRight", UpdateLeftToRight.ToString()));
			}
			
			// Set IsNilable for each column in the left and right tables based on PropagateXXXLeft and Right
			bool isLeftNilable = (PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft;
			bool isRightNilable = (PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
			
			int rightColumnIndex = RightTableVar.Columns.Count == 0 ? LeftTableVar.Columns.Count : TableVar.Columns.IndexOfName(RightTableVar.Columns[0].Name);
			
			for (int index = 0; index < TableVar.Columns.Count; index++)
			{
				if (index < rightColumnIndex)
				{
					if (isLeftNilable)
						TableVar.Columns[index].IsNilable = isLeftNilable;
				}
				else
				{
					if (isRightNilable)
						TableVar.Columns[index].IsNilable = isRightNilable;
				}
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			InternalCreateDataType(plan);

			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(LeftTableVar.MetaData);
			_tableVar.JoinInheritMetaData(RightTableVar.MetaData);
			
			if (_isNatural)
			{
				if (_isIntersect && (!RightTableType.Compatible(LeftTableType)))
					throw new CompilerException(CompilerException.Codes.TableExpressionsNotCompatible, plan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString());
					
				_expression = GenerateJoinExpression();
				
				if (_isTimes)
				{
					if (Modifiers == null)
						Modifiers = new LanguageModifiers();
					if (!Modifiers.Contains("IsTimes"))
						Modifiers.Add(new LanguageModifier("IsTimes", "true"));
				}
				
				if (_isTimes && (_expression != null))
					throw new CompilerException(CompilerException.Codes.TableExpressionsNotProductCompatible, plan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString());

				if (!_isTimes && (_expression == null) && !plan.SuppressWarnings)
					plan.Messages.Add(new CompilerException(CompilerException.Codes.PossiblyIncorrectProductExpression, CompilerErrorLevel.Warning, plan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString()));
			}
			else
			{
				if (!IsEquiJoin(_expression))
					throw new CompilerException(CompilerException.Codes.JoinMustBeEquiJoin, _expression);
					
				DetermineJoinKeys(_expression);
			}
			
			foreach (Schema.Key key in LeftTableVar.Keys)
				if (_leftKey.Columns.IsSupersetOf(key.Columns))
				{
					_leftKey.IsUnique = true;
					break;
				}
				
			foreach (Schema.Key key in RightTableVar.Keys)
				if (_rightKey.Columns.IsSupersetOf(key.Columns))
				{
					_rightKey.IsUnique = true;
					break;
				}
				
			DetermineCardinality(plan);
			DetermineDetailLookup(plan);
				
			// Determine columns
			DetermineColumns(plan);				
			
			// Determine the keys
			DetermineJoinKey(plan);
			
			_updateLeftToRight = (_leftKey.IsUnique && _rightKey.IsUnique) || (!_leftKey.IsUnique && !_rightKey.IsUnique) || _leftKey.IsUnique;

			// Determine modifiers
			DetermineJoinModifiers(plan);
			
			// DetermineRemotable
			DetermineRemotable(plan);

			// Determine orders			
			DetermineOrders(plan);

			#if UseReferenceDerivation
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
			{
				if (LeftTableVar.HasReferences())
				{
					foreach (Schema.ReferenceBase reference in LeftTableVar.References)
					{
						if (reference.SourceTable.Equals(LeftTableVar))
						{
							CopySourceReference(plan, reference, reference.IsExcluded 
								|| (RightTableVar.HasReferences() && RightTableVar.References.ContainsOriginatingReference(reference.OriginatingReferenceName()) && LeftKey.Columns.Equals(reference.SourceKey.Columns)));
						}
						else if (reference.TargetTable.Equals(LeftTableVar))
						{
							CopyTargetReference(plan, reference, reference.IsExcluded 
								|| (RightTableVar.HasReferences() && RightTableVar.References.ContainsOriginatingReference(reference.OriginatingReferenceName()) && LeftKey.Columns.Equals(reference.TargetKey.Columns)));
						}
					}
				}

				if (RightTableVar.HasReferences())
				{
					foreach (Schema.ReferenceBase reference in RightTableVar.References)
					{
						if (reference.SourceTable.Equals(RightTableVar))
						{
							CopySourceReference(plan, reference, reference.IsExcluded 
								|| (LeftTableVar.HasReferences() && LeftTableVar.References.ContainsOriginatingReference(reference.OriginatingReferenceName()) && RightKey.Columns.Equals(reference.SourceKey.Columns)));
						}
						else if (reference.TargetTable.Equals(RightTableVar))
						{
							CopyTargetReference(plan, reference, reference.IsExcluded 
								|| (LeftTableVar.HasReferences() && LeftTableVar.References.ContainsOriginatingReference(reference.OriginatingReferenceName()) && RightKey.Columns.Equals(reference.TargetKey.Columns)));
						}
					}
				}
			}
			#endif
					
			plan.EnterRowContext();
			try
			{			
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, RightTableType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					#endif
					try
					{
						if (_expression != null)
						{
							PlanNode planNode = Compiler.CompileExpression(plan, _expression);
							if (!(planNode.DataType.Is(plan.DataTypes.SystemBoolean)))
								throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, _expression);
							Nodes.Add(planNode);
						}
						else
							Nodes.Add(new ValueNode(plan.DataTypes.SystemBoolean, true));
						
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
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			#if USEVISIT
			Nodes[0] = visitor.Visit(plan, Nodes[0]);
			#else
			Nodes[0].BindingTraversal(plan, visitor);
			#endif
			if (IsLookup && !IsDetailLookup)
				plan.PushCursorContext(new CursorContext(plan.CursorContext.CursorType, plan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, plan.CursorContext.CursorIsolation));
			try
			{
				#if USEVISIT
				Nodes[1] = visitor.Visit(plan, Nodes[1]);
				#else
				Nodes[1].BindingTraversal(plan, visitor);
				#endif
			}
			finally
			{
				if (IsLookup && !IsDetailLookup)
					plan.PopCursorContext();
			}
			
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, RightTableType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					#endif
					try
					{
						#if USEVISIT
						Nodes[2] = visitor.Visit(plan, Nodes[2]);
						#else
						Nodes[2].BindingTraversal(plan, visitor);
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
		
		public override void DetermineAccessPath(Plan plan)
		{
			base.DetermineAccessPath(plan);
			if (!DeviceSupported)
				DetermineJoinAlgorithm(plan);
		}
		
		public virtual void DetermineCursorCapabilities(Plan plan)
		{
			_cursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					plan.CursorContext.CursorCapabilities &
					LeftNode.CursorCapabilities &
					((IsLookup && !IsDetailLookup) ? CursorCapability.Updateable : RightNode.CursorCapabilities) & 
					CursorCapability.Updateable
				) |
				(
					plan.CursorContext.CursorCapabilities &
					(LeftNode.CursorCapabilities | RightNode.CursorCapabilities) &
					CursorCapability.Elaborable
				);
		}

		public override void DetermineCursorBehavior(Plan plan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				_cursorType = CursorType.Dynamic;
			else
				_cursorType = CursorType.Static;
			_requestedCursorType = plan.CursorContext.CursorType;

			DetermineCursorCapabilities(plan);

			_cursorIsolation = plan.CursorContext.CursorIsolation;
		}
		
		protected PlanNode _leftSelectNode;
		public PlanNode LeftSelectNode
		{
			get { return _leftSelectNode; }
			set { _leftSelectNode = value; }
		}
		
		protected PlanNode _rightSelectNode;
		public PlanNode RightSelectNode
		{
			get { return _rightSelectNode; }
			set { _rightSelectNode = value; }
		}
		
		protected IRow SelectLeftRow(Program program)
		{
			EnsureLeftSelectNode(program.Plan);
			using (ITable table = (ITable)_leftSelectNode.Execute(program))
			{
				table.Open();
				if (table.Next())
					return table.Select();
				else
					return null;
			}
		}
		
		protected bool SelectLeftRow(Program program, IRow row)
		{
			EnsureLeftSelectNode(program.Plan);
			using (ITable table = (ITable)_leftSelectNode.Execute(program))
			{
				table.Open();
				if (table.Next())
				{
					table.Select(row);
					return true;
				}
				return false;
			}
		}
		
		protected IRow SelectRightRow(Program program)
		{
			EnsureRightSelectNode(program.Plan);
			using (ITable table = (ITable)_rightSelectNode.Execute(program))
			{
				table.Open();
				if (table.Next())
					return table.Select();
				else
					return null;
			}
		}
		
		protected bool SelectRightRow(Program program, IRow row)
		{
			EnsureRightSelectNode(program.Plan);
			using (ITable table = (ITable)_rightSelectNode.Execute(program))
			{
				table.Open();
				if (table.Next())
				{
					table.Select(row);
					return true;
				}
				return false;
			}
		}
		
		protected void EnsureLeftSelectNode(Plan plan)
		{
			if (_leftSelectNode == null)
			{
				ApplicationTransaction transaction = null;
				if (plan.ApplicationTransactionID != Guid.Empty)
					transaction = plan.GetApplicationTransaction();
				try
				{
					if ((transaction != null) && (!ShouldTranslateLeft || (IsLookup && !IsDetailLookup)))
						transaction.PushLookup();
					try
					{
						plan.PushATCreationContext();
						try
						{
							plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(plan, _symbols);
								try
								{
									plan.EnterRowContext();
									try
									{
										#if USENAMEDROWVARIABLES
										plan.Symbols.Push(new Symbol(Keywords.New, DataType.RowType));
										#else
										APlan.Symbols.Push(new Symbol(String.Empty, DataType.NewRowType));
										#endif
										try
										{
											_leftSelectNode =
												Compiler.Compile
												(
													plan,
													new RestrictExpression
													(
														(Expression)LeftNode.EmitStatement(EmitMode.ForCopy),
														#if USENAMEDROWVARIABLES
														Compiler.BuildKeyEqualExpression
														(
															plan,
															String.Empty,
															Keywords.New,
															_leftKey.Columns,
															_rightKey.Columns
														)
														#else
														Compiler.BuildKeyEqualExpression
														(
															APlan,
															new Schema.RowType(FLeftKey.Columns).Columns,
															new Schema.RowType(FRightKey.Columns, Keywords.New).Columns
														)
														#endif
													)
												);
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
									PopSymbols(plan, _symbols);
								}
							}
							finally
							{
								plan.PopCursorContext();
							}
						}
						finally
						{
							plan.PopATCreationContext();
						}
					}
					finally
					{
						if (transaction != null)
							transaction.PopLookup();
					}
				}
				finally
				{
					if (transaction != null)
						Monitor.Exit(transaction);
				}
			}
		}
		
		protected void EnsureRightSelectNode(Plan plan)
		{
			if (_rightSelectNode == null)
			{
				ApplicationTransaction transaction = null;
				if (plan.ApplicationTransactionID != Guid.Empty)
					transaction = plan.GetApplicationTransaction();
				try
				{
					if ((transaction != null) && (!ShouldTranslateRight || (IsLookup && !IsDetailLookup)))
						transaction.PushLookup();
					try
					{
						plan.PushATCreationContext();
						try
						{
							plan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(plan, _symbols);
								try
								{
									plan.EnterRowContext();
									try
									{
										#if USENAMEDROWVARIABLES
										plan.Symbols.Push(new Symbol(Keywords.New, DataType.RowType));
										#else
										APlan.Symbols.Push(new Symbol(String.Empty, DataType.NewRowType));
										#endif
										try
										{
											_rightSelectNode =
												Compiler.Compile
												(
													plan,
													new RestrictExpression
													(
														(Expression)RightNode.EmitStatement(EmitMode.ForCopy),
														#if USENAMEDROWVARIABLES
														Compiler.BuildKeyEqualExpression
														(
															plan,
															String.Empty,
															Keywords.New,
															_rightKey.Columns,
															_leftKey.Columns
														)
														#else
														Compiler.BuildKeyEqualExpression
														(
															APlan,
															new Schema.RowType(FRightKey.Columns).Columns,
															new Schema.RowType(FLeftKey.Columns, Keywords.New).Columns
														)
														#endif
													)
												);
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
									PopSymbols(plan, _symbols);
								}
							}
							finally
							{
								plan.PopCursorContext();
							}
						}
						finally
						{
							plan.PopATCreationContext();
						}
					}
					finally
					{
						if (transaction != null)
							transaction.PopLookup();
					}
				}
				finally
				{
					if (transaction != null)
						Monitor.Exit(transaction);
				}
			}
		}
		
		private bool _coordinateLeft = true;
		public bool CoordinateLeft
		{
			get { return _coordinateLeft; }
			set { _coordinateLeft = value; }
		}
		
		private bool _coordinateRight = true;
		public bool CoordinateRight
		{
			get { return _coordinateRight; }
			set { _coordinateRight = value; }
		}
		
		private bool _retrieveLeft = false;
		public bool RetrieveLeft
		{
			get { return _retrieveLeft; }
			set { _retrieveLeft = value; }
		}
		
		private bool _retrieveRight = false;
		public bool RetrieveRight
		{
			get { return _retrieveRight; }
			set { _retrieveRight = value; }
		}
		
		private bool _clearLeft = false;
		public bool ClearLeft
		{
			get { return _clearLeft; }
			set { _clearLeft = value; }
		}
		
		private bool _clearRight = false;
		public bool ClearRight
		{
			get { return _clearRight; }
			set { _clearRight = value; }
		}
		
		public override object InternalExecute(Program program)
		{
			JoinTable table = (JoinTable)Activator.CreateInstance(_joinAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, program}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				table.Open();
				return table;
			}
			catch
			{
				table.Dispose();
				throw;
			}
		}
		
		public override void DetermineRemotable(Plan plan)
		{
			base.DetermineRemotable(plan);
			
			_tableVar.ShouldValidate = _tableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate || (EnforcePredicate && !_isNatural);
			_tableVar.ShouldDefault = _tableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			_tableVar.ShouldChange = _tableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange;
			
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				int columnIndex;
				Schema.TableVarColumn sourceColumn;
				
				columnIndex = LeftTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = LeftTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = _leftKey.Columns.ContainsName(column.Name) || column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}

				columnIndex = RightTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = RightTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = _rightKey.Columns.ContainsName(column.Name) || column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}
			}
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (EnforcePredicate && !_isNatural && (columnName == String.Empty))
			{
				Row leftRow = new Row(program.ValueManager, LeftTableType.RowType);
				try
				{
					newRow.CopyTo(leftRow);
					program.Stack.Push(leftRow);
					try
					{
						Row rightRow = new Row(program.ValueManager, RightTableType.RowType);
						try
						{
							newRow.CopyTo(rightRow);
							program.Stack.Push(rightRow);
							try
							{
								object objectValue = Nodes[2].Execute(program);
								if ((objectValue != null) && !(bool)objectValue)
									throw new RuntimeException(RuntimeException.Codes.RowViolatesJoinPredicate);
							}
							finally
							{
								program.Stack.Pop();
							}
						}
						finally
						{
							rightRow.Dispose();
						}
					}
					finally
					{
						program.Stack.Pop();
					}
				}
				finally
				{
					leftRow.Dispose();
				}
			}

			if (isDescending)
			{
				bool changed = false;
				if (PropagateValidateLeft && ((columnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(columnName))))
					changed = LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;
				if (PropagateValidateRight && ((columnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(columnName))))
					changed = RightNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;
				return changed;
			}
			return false;
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			if (isDescending)
			{
				bool changed = false;

				if (PropagateDefaultLeft && ((columnName == String.Empty) || LeftTableType.Columns.ContainsName(columnName)))
				{
					BitArray localValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int index = 0; index < LeftKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(LeftKey.Columns[index].Name);
					changed = LeftNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < LeftKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(LeftKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, LeftKey.Columns[index].Name);
				}

				if (PropagateDefaultRight && ((columnName == String.Empty) || RightTableType.Columns.ContainsName(columnName)))
				{
					BitArray localValueFlags = new BitArray(RightKey.Columns.Count);
					for (int index = 0; index < RightKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(RightKey.Columns[index].Name);
					changed = RightNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < RightKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(RightKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, RightKey.Columns[index].Name);
				}

				return changed;
			}
			return false;
		}
		
		///<summary>Coordinates the value of the left join-key column with the value of the right join-key column given by AColumnName.</summary>		
		protected void CoordinateLeftJoinKey(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			int rightIndex = newRow.DataType.Columns.IndexOfName(columnName);
			int leftIndex = newRow.DataType.Columns.IndexOfName(_leftKey.Columns[_rightKey.Columns.IndexOfName(columnName)].Name);
			if (newRow.HasValue(rightIndex))
				newRow[leftIndex] = newRow[rightIndex];
			else
				newRow.ClearValue(leftIndex);
				
			if (PropagateChangeLeft)
				LeftNode.Change(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[leftIndex].Name);
		}

		///<summary>Coordinates the value of the right join-key column with the value of the left join-key column given by AColumnName.</summary>		
		protected void CoordinateRightJoinKey(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			int leftIndex = newRow.DataType.Columns.IndexOfName(columnName);
			int rightIndex = newRow.DataType.Columns.IndexOfName(_rightKey.Columns[_leftKey.Columns.IndexOfName(columnName)].Name);
			if (newRow.HasValue(leftIndex))
				newRow[rightIndex] = newRow[leftIndex];
			else
				newRow.ClearValue(rightIndex);

			if (PropagateChangeRight)
				RightNode.Change(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[rightIndex].Name);
		}
		
		/*
			if the change is to a specific column and the join is not natural
				if a change is made to a non-key column in the left side
					if PropagateChangeLeft
						propagate the change

				if a change is made to a column in the left key
					if !IsRepository and RetrieveRight
						Lookup the right side from the database
							if a row is found
								all right columns are set to the lookup row
								fire a change for each column of the right side
							else
								foreach column
									if IsKeyColumn && CoordinateRight
										set the column to the left value
										fire a change
									else
										if Clear
											Clear
											fire a default
											fire a change
					else
						if CoordinateRight
							Coordinate the corresponding right key column with the new value for the left key
							fire a change for the column
						
				if a change is made to a column in the right key
					if !IsRepository and RetrieveLeft
						Lookup the left side from the database
							if a row is found
								all right columns are set to the lookup row
								fire a change for each column of the right side
							else
								foreach column
									if IsKeyColumn && CoordinateRight
										set the column to the left value
										fire a change
									else
										if Clear
											Clear
											fire a default
											fire a change
					else
						if CoordinateLeft
							Coordinate the corresponding left key column with the new value for the right key
							fire a change for the column
							
				if a change is made to a non-key column in the right side
					if PropagateChangeRight
						propagate the change
			else
				if PropagateChangeLeft and change is not to a specific column, or is to a column in the left side
					propagate the change
				
				if PropagateChangeRight and change is not to a specific column, or is to a column in the right side
					propagate the change
		*/
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			PushNewRow(program, newRow);
			try
			{
				bool changed = false;
				bool changePropagated = false;
				
				if (columnName != String.Empty)
				{
					if (LeftKey.Columns.ContainsName(columnName))
					{
						if (!program.ServerProcess.ServerSession.Server.IsEngine && RetrieveRight)
						{
							changed = true;
							if (IsNatural)
								changePropagated = true;
							if (SelectRightRow(program, newRow))
							{
								foreach (Schema.Column column in RightTableType.Columns)
									if (PropagateChangeRight)
										RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
							}
							else
							{
								foreach (Schema.Column column in RightTableType.Columns)
								{
									if (RightKey.Columns.ContainsName(column.Name) && CoordinateRight)
									{
										int leftIndex = newRow.DataType.Columns.IndexOfName(_leftKey.Columns[_rightKey.Columns.IndexOfName(column.Name)].Name);
										int rightIndex = newRow.DataType.Columns.IndexOfName(column.Name);
										if (newRow.HasValue(leftIndex))
											newRow[rightIndex] = newRow[leftIndex];
										else
											newRow.ClearValue(rightIndex);
									}
									else
									{
										if (ClearRight)
											newRow.ClearValue(column.Name);

										if (!newRow.HasValue(column.Name) && PropagateDefaultRight)
											RightNode.Default(program, oldRow, newRow, valueFlags, column.Name);
									}

									if (PropagateChangeRight)
										RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
							}
						}
						else
						{
							if (CoordinateRight)
							{
								changed = true;
								int leftIndex = newRow.DataType.Columns.IndexOfName(columnName);
								int rightIndex = newRow.DataType.Columns.IndexOfName(_rightKey.Columns[_leftKey.Columns.IndexOfName(columnName)].Name);
								if (newRow.HasValue(leftIndex))
									newRow[rightIndex] = newRow[leftIndex];
								else
									newRow.ClearValue(rightIndex);

								if (PropagateChangeRight)
									RightNode.Change(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[rightIndex].Name);
							}
						}
					}
					else if (LeftTableVar.Columns.ContainsName(columnName))
					{
						if (PropagateChangeLeft)
						{
							changePropagated = true;
							changed = LeftNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;
						}
					}
					else if (RightKey.Columns.ContainsName(columnName))
					{
						if (!program.ServerProcess.ServerSession.Server.IsEngine && RetrieveLeft)
						{
							changed = true;
							if (IsNatural)
								changePropagated = true;
							if (SelectLeftRow(program, newRow))
							{
								foreach (Schema.Column column in LeftTableType.Columns)
									if (PropagateChangeLeft)
										LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
							}
							else
							{
								foreach (Schema.Column column in LeftTableType.Columns)
								{
									if (LeftKey.Columns.ContainsName(column.Name) && CoordinateLeft)
									{
										int rightIndex = newRow.DataType.Columns.IndexOfName(_rightKey.Columns[_leftKey.Columns.IndexOfName(column.Name)].Name);
										int leftIndex = newRow.DataType.Columns.IndexOfName(column.Name);
										if (newRow.HasValue(rightIndex))
											newRow[leftIndex] = newRow[rightIndex];
										else
											newRow.ClearValue(leftIndex);
									}
									else
									{
										if (ClearLeft)
											newRow.ClearValue(column.Name);

										if (!newRow.HasValue(column.Name) && PropagateDefaultLeft)
											LeftNode.Default(program, oldRow, newRow, valueFlags, column.Name);
									}

									if (PropagateChangeLeft)
										LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
							}
						}
						else
						{
							if (CoordinateLeft)
							{
								changed = true;
								int rightIndex = newRow.DataType.Columns.IndexOfName(columnName);
								int leftIndex = newRow.DataType.Columns.IndexOfName(_leftKey.Columns[_rightKey.Columns.IndexOfName(columnName)].Name);
								if (newRow.HasValue(rightIndex))
									newRow[leftIndex] = newRow[rightIndex];
								else
									newRow.ClearValue(leftIndex);

								if (PropagateChangeLeft)
									LeftNode.Change(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[leftIndex].Name);
							}
						}
					}
					else if (RightTableVar.Columns.ContainsName(columnName))
					{
						if (PropagateChangeRight)
						{
							changePropagated = true;
							changed = RightNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;
						}
					}
				}

				if (!changePropagated && PropagateChangeLeft && ((columnName == String.Empty) || LeftTableVar.Columns.ContainsName(columnName)))
					changed = LeftNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;
						
				if (!changePropagated && PropagateChangeRight && ((columnName == String.Empty) || RightTableVar.Columns.ContainsName(columnName)))
					changed = RightNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;

				return changed;
			}
			finally
			{
				PopRow(program);
			}
		}
		
		private void InternalExecuteInsertLeft(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (IRow leftRow = LeftNode.Select(program, newRow))
					{
						if (leftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(program, leftRow, newRow, valueFlags, false, uncheckedValue);
						}
						else
							LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				break;
			}
		}
		
		private void InternalExecuteInsertRight(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True : 
					RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (IRow rightRow = RightNode.Select(program, newRow))
					{
						if (rightRow != null)
						{
							if (PropagateInsertRight == PropagateAction.Ensure)
								RightNode.Update(program, rightRow, newRow, valueFlags, false, uncheckedValue);
						}
						else
							RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				break;
			}
		}
		
		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				InternalExecuteInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalExecuteInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
			else
			{
				InternalExecuteInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalExecuteInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{	
				if (PropagateUpdateLeft)
					LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
					
				if (PropagateUpdateRight)
					RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
			else
			{
				if (PropagateUpdateRight)
					RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);

				if (PropagateUpdateLeft)
					LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				if (PropagateDeleteRight)
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
				if (PropagateDeleteLeft)
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
			else
			{
				if (PropagateDeleteLeft)
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
				if (PropagateDeleteRight)
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			InnerJoinExpression expression = new InnerJoinExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			if (!IsNatural)
				expression.Condition = _expression; //(Expression)Nodes[2].EmitStatement(AMode); // use expression instead of EmitStatement because the statement may be of the form A ?= B = 0;
			expression.IsLookup = _isLookup;
			expression.Cardinality = _cardinality;
			//LExpression.IsDetailLookup = FIsDetailLookup;
			expression.Modifiers = Modifiers;
			return expression;
		}
	}
	
	public class OuterJoinNode : JoinNode
	{
        // RowExistsColumnIndex
        protected int _rowExistsColumnIndex = -1;
        public int RowExistsColumnIndex
        {
            get { return _rowExistsColumnIndex; }
        }
        
        protected IncludeColumnExpression _rowExistsColumn;
        public IncludeColumnExpression RowExistsColumn
        {
			get { return _rowExistsColumn; }
			set { _rowExistsColumn = value; }
        }
        
        // AnyOf
        // Together with AllOf, determines what columns must have values to consititute row existence
        // HasRow = if hasrowexists then rowexists else HasAllOf and (AnyOf.Count == 0 or HasAnyOf)
        // Setting this property will have no effect after the DetermineJoinModifiers call
        // By default, this property is set to the set of non-join key  columns
        private string _anyOf;
        public string AnyOf
        {
			get { return _anyOf; }
			set { _anyOf = value; }
		}
		
		// AllOf
		// Together with AnyOf, determines what columns must have values to constitute row existence
		// HasRow = if hasrowexists then rowexists else HasAllOf and HasAnyOf
		// Setting this property will have no effect after the DetermineJoinModifiers call
		// By default, this property is set to the set of  join key columns
		private string _allOf;
		public string AllOf
		{
			get { return _allOf; }
			set { _allOf = value; }
		}
		
		protected Schema.JoinKey _allOfKey = new Schema.JoinKey();
		public Schema.JoinKey AllOfKey
		{
			get { return _allOfKey; }
			set { _allOfKey = value; }
		}
		
		protected Schema.JoinKey _anyOfKey = new Schema.JoinKey();
		public Schema.JoinKey AnyOfKey
		{
			get { return _anyOfKey; }
			set { _anyOfKey = value; }
		}
		
		protected void DetermineKeyColumns(string columnNames, Schema.JoinKey key)
		{
			if (columnNames != String.Empty)
			{
				string[] localColumnNames = columnNames.Split(';');
				foreach (string columnName in localColumnNames)
					key.Columns.Add(TableVar.Columns[columnName]);
			}
		}
        
        protected override void InternalCreateDataType(Plan plan)
        {
			_dataType = new Schema.TableType();
        }
        
		protected bool IsRowExistsColumn(string columnName)
		{
			return (_rowExistsColumnIndex >= 0) && (Schema.Object.NamesEqual(TableVar.Columns[_rowExistsColumnIndex].Name, columnName));
		}
		
        protected bool HasAllValues(IRow row, Schema.TableVarColumnsBase columns)
        {
			int columnIndex;
			foreach (Schema.TableVarColumn column in columns)
			{
				columnIndex = row.DataType.Columns.IndexOfName(column.Name);
				if ((columnIndex >= 0) && !row.HasValue(columnIndex))
					return false;
			}
			return true;
        }
        
        protected bool HasAnyValues(IRow row, Schema.TableVarColumnsBase columns)
        {
			int columnIndex;
			foreach (Schema.TableVarColumn column in columns)
			{
				columnIndex = row.DataType.Columns.IndexOfName(column.Name);
				if ((columnIndex >= 0) && row.HasValue(columnIndex))
					return true;
			}
			return false;
        }
        
		protected bool HasRowExistsColumn()
		{
			return _rowExistsColumnIndex >= 0;
		}
		
		protected bool HasValues(IRow row)
		{
			return HasAllValues(row, _allOfKey.Columns) && ((_anyOfKey.Columns.Count == 0) || HasAnyValues(row, _anyOfKey.Columns));
		}
		
        protected bool HasRow(IRow row)
        {
			if (_rowExistsColumnIndex >= 0)
				return row.HasValue(TableVar.Columns[_rowExistsColumnIndex].Name) && (bool)row[TableVar.Columns[_rowExistsColumnIndex].Name];
			else
				return HasValues(row);
        }
        
        protected bool HasAllJoinValues(IRow row, Schema.Key key)
        {
			int columnIndex;
			foreach (Schema.TableVarColumn column in key.Columns)
			{
				columnIndex = row.DataType.Columns.IndexOfName(column.Name);
				if ((columnIndex >= 0) && !row.HasValue(columnIndex))
					return false;
			}
			return true;
        }

		protected bool HasAnyNonJoinRightValues(Row row)
		{
			foreach (Schema.Column column in RightTableType.Columns)
			{
				if (!_rightKey.Columns.ContainsName(column.Name) && row.HasValue(column.Name))
					return true;
			}
			return false;
		}
        
		protected bool HasAnyNonJoinLeftValues(Row row)
		{
			foreach (Schema.Column column in LeftTableType.Columns)
			{
				if (!_leftKey.Columns.ContainsName(column.Name) && row.HasValue(column.Name))
					return true;
			}
			return false;
		}

		public override void DetermineRemotable(Plan plan)
		{
			Schema.ResultTableVar tableVar = (Schema.ResultTableVar)TableVar;
			tableVar.InferredIsDefaultRemotable = (!PropagateDefaultLeft || LeftTableVar.IsDefaultRemotable) && (!PropagateDefaultRight || RightTableVar.IsDefaultRemotable);
			tableVar.InferredIsChangeRemotable = (!PropagateChangeLeft || LeftTableVar.IsChangeRemotable) && (!PropagateChangeRight || RightTableVar.IsChangeRemotable);
			tableVar.InferredIsValidateRemotable = (!PropagateValidateLeft || LeftTableVar.IsValidateRemotable) && (!PropagateValidateRight || RightTableVar.IsValidateRemotable);

			_tableVar.DetermineRemotable(plan.CatalogDeviceSession);
			_tableVar.ShouldValidate = _tableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate || (EnforcePredicate && !_isNatural);
			_tableVar.ShouldDefault = _tableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			_tableVar.ShouldChange = _tableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange || HasRowExistsColumn();
			
			foreach (Schema.TableVarColumn column in _tableVar.Columns)
			{
				int columnIndex;
				Schema.TableVarColumn sourceColumn;
				
				if (IsRowExistsColumn(column.Name))
				{
					column.ShouldDefault = true;
					column.ShouldChange = true;
				}
				
				if (_anyOfKey.Columns.ContainsName(column.Name) || _allOfKey.Columns.ContainsName(column.Name))
					column.ShouldChange = true;
				
				columnIndex = LeftTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = LeftTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = _leftKey.Columns.ContainsName(column.Name) || column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}

				columnIndex = RightTableVar.Columns.IndexOfName(column.Name);
				if (columnIndex >= 0)
				{
					sourceColumn = RightTableVar.Columns[columnIndex];
					column.ShouldDefault = column.ShouldDefault || sourceColumn.ShouldDefault;
					_tableVar.ShouldDefault = _tableVar.ShouldDefault || column.ShouldDefault;
					
					column.ShouldValidate = column.ShouldValidate || sourceColumn.ShouldValidate;
					_tableVar.ShouldValidate = _tableVar.ShouldValidate || column.ShouldValidate;

					column.ShouldChange = _rightKey.Columns.ContainsName(column.Name) || column.ShouldChange || sourceColumn.ShouldChange;
					_tableVar.ShouldChange = _tableVar.ShouldChange || column.ShouldChange;
				}
			}
		}
	}
	
	// operator LeftJoin(table{}, table{}, object, object) : table{}	
	public class LeftOuterJoinNode : OuterJoinNode
	{
        protected override void DetermineColumns(Plan plan)
        {
			DetermineLeftColumns(plan, false);
			
			if (_rowExistsColumn != null)
			{
				Schema.TableVarColumn rowExistsColumn =
					Compiler.CompileIncludeColumnExpression
					(
						plan,
						_rowExistsColumn, 
						Keywords.RowExists, 
						plan.DataTypes.SystemBoolean, 
						Schema.TableVarColumnType.RowExists
					);
				DataType.Columns.Add(rowExistsColumn.Column);
				TableVar.Columns.Add(rowExistsColumn);
				_rowExistsColumnIndex = TableVar.Columns.Count - 1;
				rowExistsColumn.IsChangeRemotable = false;
				rowExistsColumn.IsDefaultRemotable = false;
			}

			DetermineRightColumns(plan, true);
        }
        
		protected override void DetermineJoinModifiers(Plan plan)
		{
			base.DetermineJoinModifiers(plan);
			
			AnyOf = String.Empty;
			AllOf = String.Empty;
			foreach (Schema.TableVarColumn column in RightTableVar.Columns)
				if (!RightKey.Columns.ContainsName(column.Name))
					AnyOf = (AnyOf == String.Empty) ? column.Name : String.Format("{0};{1}", AnyOf, Schema.Object.EnsureRooted(column.Name));
					
			AnyOf = LanguageModifiers.GetModifier(Modifiers, "AnyOf", AnyOf);
			AllOf = LanguageModifiers.GetModifier(Modifiers, "AllOf", AllOf);
			
			DetermineKeyColumns(AnyOf, AnyOfKey);
			DetermineKeyColumns(AllOf, AllOfKey);

			
			// Set change remotable to false for all AnyOf and AllOf columns
			foreach (Schema.TableVarColumn column in AnyOfKey.Columns)
				column.IsChangeRemotable = false;
				
			foreach (Schema.TableVarColumn column in AllOfKey.Columns)
				column.IsChangeRemotable = false;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			LeftOuterJoinExpression expression = new LeftOuterJoinExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			if (!IsNatural)
				expression.Condition = _expression;
			expression.IsLookup = _isLookup;
			expression.Cardinality = _cardinality;
			if (_rowExistsColumnIndex >= 0)
			{
				Schema.TableVarColumn column = TableVar.Columns[_rowExistsColumnIndex];
				expression.RowExistsColumn = new IncludeColumnExpression(column.Name);
				if (column.MetaData != null)
				{
					expression.RowExistsColumn.MetaData = new MetaData();
					#if USEHASHTABLEFORTAGS
					foreach (Tag tag in column.MetaData.Tags)
						if (!column.MetaData.Tags.IsReference(tag.Name))
							expression.RowExistsColumn.MetaData.Tags.Add(tag.Copy());
					#else
					for (int index = 0; index < column.MetaData.Tags.Count; index++)
						if (!column.MetaData.Tags[index].IsInherited) //!LColumn.MetaData.Tags.IsReference(LIndex))
							expression.RowExistsColumn.MetaData.Tags.Add(column.MetaData.Tags[index].Copy());
					#endif
				}
			}
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		protected override void DetermineJoinKey(Plan plan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(plan, Cardinality, true, false);
			#else
			if (FLeftKey.IsUnique && FRightKey.IsUnique)
			{
				// If the left and right join keys are unique
					// for each key of the left table var
						// copy the key
						// copy the key with all correlated columns replaced with the right key columns (a no-op for natural joins), inferred as sparse
					// for each key of the right table var
						// copy the key, if it is not a proper subset of the join key, inferred as sparse if the left join key is not a superset of the key
						// copy the key, if it is not a proper subset of the join key, with all correlated columns replaced with the left key columns (a no-op for natural joins), inferred as sparse if the right join key is not a superset of the key
				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					Schema.Key newKey = CopyKey(leftKey);
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = true;
						foreach (Schema.TableVarColumn column in leftKey.Columns)
						{
							int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
							if (leftKeyIndex >= 0)
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}

						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
				
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					if (!rightKey.Columns.IsProperSubsetOf(RightKey.Columns))
					{
						Schema.Key newKey = CopyKey(rightKey, !LeftKey.Columns.IsSupersetOf(rightKey.Columns));
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);

						if (!IsNatural)
						{
							newKey = new Schema.Key();
							newKey.IsInherited = true;
							newKey.IsSparse = !RightKey.Columns.IsSupersetOf(rightKey.Columns);
							foreach (Schema.TableVarColumn column in rightKey.Columns)
							{
								int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
								if (rightKeyIndex >= 0)
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
								else
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
							}
							
							if (!TableVar.Keys.Contains(newKey))
								TableVar.Keys.Add(newKey);
						}
					}
				}
				
				RemoveSuperKeys();
			}
			else if (FLeftKey.IsUnique)
			{
				// If only the left join key is unique,
					// for each key of the right table var
						// copy the key, excluding join columns that are not key columns of the left, inferred as sparse if the left join key is not a superset of the resulting key's columns
						// copy the key, excluding join columns that are not key columns of the left, with all correlated columns replaced with the left key columns (a no-op for natural joins), non-sparse
					// In addition to these, the same keys inferred for many-to-many joins are inferred
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						if ((rightKeyIndex < 0) || (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name)))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					newKey.IsSparse = !LeftKey.Columns.IsSupersetOf(newKey.Columns);

					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = rightKey.IsSparse;
						foreach (Schema.TableVarColumn column in rightKey.Columns)
						{
							int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
							if (rightKeyIndex >= 0)
							{
								if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name))
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
							}
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
				
				DetermineManyToManyKeys(APlan);
			}
			else if (FRightKey.IsUnique)
			{
				// If only the right join key is unique,
					// for each key of the left table var
						// copy the key, non-sparse
						// copy the key with all correlated columns replaced with the right key columns (a no-op for natural joins), inferred as sparse if the left join key is not a superset of the key's columns
				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse = leftKey.IsSparse;
					
					foreach (Schema.TableVarColumn column in leftKey.Columns)
					{
						int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
						if ((leftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name)))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						foreach (Schema.TableVarColumn column in leftKey.Columns)
						{
							int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
							if (leftKeyIndex >= 0)
							{
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name))
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
							}
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						newKey.IsSparse = !LeftKey.Columns.IsSupersetOf(newKey.Columns);

						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
			}
			else
			{
				// BTR 9/14/2004 ->
				// Although this behavior is correct in terms of the key inference, the fact is that the result set of this query
				// could never be materialized by our engine, and there is no way to uniquely identify a row within the result set,
				// i.e., the result set is effectively unaddressable. This has major imiplications for query processing, as well
				// as browse technology, not least of which is that the browse technology should be able to address duplicate
				// index keys. It was decided that these issues will be addressed later, and for 2.0, this class of joins is outlawed.
				// See RightOuterJoinNode.DetermineJoinKey as well.
				//
				// BTR 9/20/2004 ->
				// On second thought, this join does not produce only sparse keys, on the contrary, it produces no sparse keys,
				// because the row from the left input with no matching rows in the right input will only appear once, resulting
				// in only one nil for each column of the right portion of the key for each key value of the left input.
				// Keys in many-to-many outers are sparse only if the key from the inner table is sparse
				//
				// BTR 4/27/2005 ->
				// Moved this code down to the JoinNode because it is the same for inner and left and right outer joins.
				DetermineManyToManyKeys(APlan);
			}
			#endif
		}
		
		protected override void DetermineJoinAlgorithm(Plan plan)
		{
			if (_expression == null)
				_joinAlgorithm = typeof(TimesTable);
			else
			{
				_joinOrder = Compiler.OrderFromKey(plan, _leftKey.Columns);
				Schema.OrderColumn column;
				for (int index = 0; index < _joinOrder.Columns.Count; index++)
				{
					column = _joinOrder.Columns[index];
					column.Sort = Compiler.GetSort(plan, column.Column.DataType);
					plan.AttachDependency(column.Sort);
					column.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to the right side
				if (!IsJoinOrder(plan, _rightKey, RightNode))
					Nodes[1] = Compiler.EnsureSearchableNode(plan, RightNode, _rightKey.Columns);

				if (IsJoinOrder(plan, _leftKey, LeftNode) && IsJoinOrder(plan, _rightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (_leftKey.IsUnique || _rightKey.IsUnique))
				{
					// if both inputs are ordered by join keys and one or both join keys are unique
						// use a merge join algorithm
					_joinAlgorithm = typeof(MergeLeftJoinTable);
					
					Order = CopyOrder(LeftNode.Order);
				}
				else if (IsJoinOrder(plan, _rightKey, RightNode))
				{
					// else if the right input is ordered by join keys
						// use a searched join algorithm
					if (_rightKey.IsUnique)
						_joinAlgorithm = typeof(RightUniqueSearchedLeftJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueSearchedLeftJoinTable);
						
					if (LeftNode.Order != null)
						Order = CopyOrder(LeftNode.Order);
				}
				else
				{
					// use a nested loop join
					// NOTE: this code will not be hit because of the join accelerator step above
					if (_rightKey.IsUnique)
						_joinAlgorithm = typeof(RightUniqueNestedLoopLeftJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueNestedLoopLeftJoinTable);
				}
			}
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			bool changed = false;
			
			if ((HasRowExistsColumn() && (columnName == String.Empty)) || IsRowExistsColumn(columnName))
			{
				changed = DefaultColumn(program, TableVar, newRow, valueFlags, TableVar.Columns[_rowExistsColumnIndex].Name) || changed;

				int rowIndex = newRow.DataType.Columns.IndexOfName(TableVar.Columns[_rowExistsColumnIndex].Name);
				if (!newRow.HasValue(rowIndex))
				{
					changed = true;
					newRow[rowIndex] = false;
				}
			}
			
			if (isDescending)
			{
				if (PropagateDefaultLeft && ((columnName == String.Empty) || LeftTableType.Columns.ContainsName(columnName)))
				{
					BitArray localValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int index = 0; index < LeftKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(LeftKey.Columns[index].Name);
					changed = LeftNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < LeftKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(LeftKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, LeftKey.Columns[index].Name);
				}

				if (PropagateDefaultRight && ((columnName == String.Empty) || RightTableType.Columns.ContainsName(columnName)) && HasRow(newRow))
				{
					BitArray localValueFlags = new BitArray(RightKey.Columns.Count);
					for (int index = 0; index < RightKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(RightKey.Columns[index].Name);
					changed = RightNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < RightKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(RightKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, RightKey.Columns[index].Name);
				}
			}

			return changed;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (!_isNatural && (columnName == String.Empty) && HasRow(newRow))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			else
			{
				if (isDescending)
				{
					bool changed = false;
					if (PropagateValidateLeft && ((columnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(columnName))))
						changed = LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;

					if (PropagateValidateRight && (((columnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(columnName))) && HasRow(newRow)))
						changed = RightNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;
					return changed;
				}
			}
			return false;
		}
		
		private bool PerformChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, ref bool changePropagated)
		{
			bool changed = false;

			if (columnName != String.Empty)
			{
				bool isRowExistsColumn = IsRowExistsColumn(columnName);
				if (_leftKey.Columns.ContainsName(columnName) || isRowExistsColumn)
				{
					changed = true;
					if (IsNatural)
						changePropagated = true;
					if (!isRowExistsColumn || HasRow(newRow))
					{
						if (!program.ServerProcess.ServerSession.Server.IsEngine && RetrieveRight)
						{
							EnsureRightSelectNode(program.Plan);
							using (ITable table = (ITable)_rightSelectNode.Execute(program))
							{
								table.Open();
								if (table.Next())
								{
									table.Select(newRow);
									if (HasRowExistsColumn() && !isRowExistsColumn)
										newRow[DataType.Columns[_rowExistsColumnIndex].Name] = true;

									if (PropagateChangeRight)
										foreach (Schema.Column column in RightTableType.Columns)
											RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
								else
								{
									if (!isRowExistsColumn)
										if (HasRowExistsColumn())
											newRow[DataType.Columns[_rowExistsColumnIndex].Name] = false;
										else
										{
											// Clear any of/all of columns
											// TODO: This should be looked at, if any of/all of are both subsets of the right side, nothing needs to happen here
											// I don't want to run through them all here because it will end up firing changes twice in the default case
										}
										
									foreach (Schema.Column column in RightTableType.Columns)
									{
										int rightKeyIndex = _rightKey.Columns.IndexOfName(column.Name);
										if (!HasRowExistsColumn() && (rightKeyIndex >= 0) && CoordinateRight)
										{
											int leftRowIndex = newRow.DataType.Columns.IndexOfName(_leftKey.Columns[rightKeyIndex].Name);
											if (newRow.HasValue(leftRowIndex))
												newRow[column.Name] = newRow[leftRowIndex];
											else
												newRow.ClearValue(column.Name);
										}
										else
										{
											if (ClearRight)
												newRow.ClearValue(column.Name);

											if (isRowExistsColumn && PropagateDefaultRight)
												RightNode.Default(program, oldRow, newRow, valueFlags, column.Name);
										}
											
										if (PropagateChangeRight)
											RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
									}
								}
							}
						}
						else
						{
							if (!isRowExistsColumn)
							{
								if (HasRow(newRow) && CoordinateRight)
									CoordinateRightJoinKey(program, oldRow, newRow, valueFlags, columnName);
							}
							else // rowexists was set
							{
								foreach (Schema.TableVarColumn column in RightTableVar.Columns)
								{
									int rightKeyIndex = _rightKey.Columns.IndexOfName(column.Name);
									if ((rightKeyIndex >= 0) && CoordinateRight)
									{
										int leftRowIndex = newRow.DataType.Columns.IndexOfName(_leftKey.Columns[rightKeyIndex].Name);
										if (newRow.HasValue(leftRowIndex))
											newRow[column.Name] = newRow[leftRowIndex];
										else
											newRow.ClearValue(column.Name);
									}
									else
									{
										if (ClearRight)
											newRow.ClearValue(column.Name);

										if (!newRow.HasValue(column.Name) && PropagateDefaultRight)
											RightNode.Default(program, oldRow, newRow, valueFlags, column.Name);
									}
									
									if (PropagateChangeRight)
										RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
							}
						}
					}
					else // rowexists was cleared
					{
						if (ClearRight)
							foreach (Schema.TableVarColumn column in RightTableVar.Columns)
							{
								newRow.ClearValue(column.Name);
								if (PropagateChangeRight)
									RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
							}
					}
				}
				
				if (_rightKey.Columns.ContainsName(columnName) && HasRow(newRow) && CoordinateLeft)
				{
					changed = true;
					if (IsNatural)
						changePropagated = true;
					CoordinateLeftJoinKey(program, oldRow, newRow, valueFlags, columnName);
				}
					
				if (_anyOfKey.Columns.ContainsName(columnName) || _allOfKey.Columns.ContainsName(columnName))
				{
					changed = true;
					if (HasValues(newRow))
					{
						if (HasRowExistsColumn())
							newRow[DataType.Columns[_rowExistsColumnIndex].Name] = true;
							
						foreach (Schema.TableVarColumn column in RightTableVar.Columns)
						{
							int rightKeyIndex = _rightKey.Columns.IndexOfName(column.Name);
							if ((rightKeyIndex >= 0) && CoordinateRight)
								CoordinateRightJoinKey(program, oldRow, newRow, valueFlags, _leftKey.Columns[rightKeyIndex].Name);
							else
							{
								if (PropagateDefaultRight && RightNode.Default(program, oldRow, newRow, valueFlags, column.Name) && PropagateChangeRight)
									RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
							}
						}
					}
					else
					{
						if (HasRowExistsColumn())
							newRow[DataType.Columns[_rowExistsColumnIndex].Name] = false;
							
						if (ClearRight)
							foreach (Schema.TableVarColumn column in RightTableVar.Columns)
							{
								newRow.ClearValue(column.Name);
								if (PropagateChangeRight)
									RightNode.Change(program, oldRow, newRow, valueFlags, column.Name);
							}
					}
				}
			}
			
			return changed;
		}
		
		/*
			if the change is made to a specific column and the join is not natural
				if a change is made to a left join-key column
					if !IsRepository and RetrieveRight
						Lookup the right side from the database
						if a row is found
							set the rowexists flag if there is one
							all right columns are set to the lookup row
							fire a change for each column of the right side
						else
							if HasRowExistsColumn
								clear it
							else
								clear any of/all of columns
							if Clear
								foreach right column
									Clear
									fire a change
					else
						if HasRow && CoordinateRight
							Coordinate the corresponding right key column with the new value for the left key
							fire a change for the column
							
				if a change is made to the rowexists column
					if it is set true
						if !IsRepository and RetrieveRight
							Lookup the right side from the database
							if a row is found
								all right columns are set to the lookup row
								fire a change for each column of the right side
							else
								foreach right column
									if IsKeyColumn && CoordinateRight
										set the column to the left value
										fire a change
									else
										if Clear
											Clear
											fire a default
											fire a change
						else
							foreach right column
								if IsKeyColumn && CoordinateRight
									set the column to the left value
									fire a change
								else
									if Clear
										Clear
										fire a default
										fire a change
					else (the rowexists column is cleared)
						if Clear
							foreach right column
								Clear
								fire a change
							
				if a change is made to a right join-key column
					if HasRow && CoordinateLeft
						set the left column to the right value
						fire a change
						
				if a change is made to an any of/all of column
					if HasValues
						if HasRowExistsColumn
							set it
						foreach right column
							if IsKeyColumn && CoordinateRight
								set the column to the left value
								fire a change
						else
							fire a default
							fire a change if necessary
					else
						if HasRowExistsColumn
							clear it
						if Clear
							foreach right column
								Clear
								fire a change
									
			if PropagateChangeLeft and change is not to a specific column, or is to a column in the left side
				propagate the change

			if PropagateChangeRight and change is not to a specific column, or is to a column in the right side
				propagate the change
		*/
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			PushNewRow(program, newRow);
			try
			{
				bool changed = false;
				bool changePropagated = false;
				
				changed = PerformChange(program, oldRow, newRow, valueFlags, columnName, ref changePropagated);
				
				if (!changePropagated && PropagateChangeLeft && ((columnName == String.Empty) || LeftTableType.Columns.ContainsName(columnName)))
				{
					// TODO: Need to generalize this solution and integrate it better with overall value-flags determination
					// For now, instead of passing the given AValueFlags, we create a new LValueFlags and use it to detect whether or not
					// the change affected any of the join columns. If it did, we need to refire the lookup logic.
					// This is safe at this point, because nothing is actually using the ValueFlags on change.
					BitArray localValueFlags = new BitArray(newRow.DataType.Columns.Count);
					localValueFlags.SetAll(false);
					changed = LeftNode.Change(program, oldRow, newRow, localValueFlags, columnName) || changed;
					for (int index = 0; index < localValueFlags.Count; index++)
					{
						if (localValueFlags[index])
						{
							string localColumnName = newRow.DataType.Columns[index].Name;
							if (LeftKey.Columns.ContainsName(localColumnName))
								changed = PerformChange(program, oldRow, newRow, valueFlags, localColumnName, ref changePropagated) || changed;
						}
					}
				}
					
				if (!changePropagated && PropagateChangeRight && ((columnName == String.Empty) || RightTableType.Columns.ContainsName(columnName)))
					changed = RightNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;

				return changed;
			}
			finally
			{
				PopRow(program);
			}
		}
		
		protected void InternalInsertLeft(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;
					
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (IRow leftRow = LeftNode.Select(program, newRow))
					{
						if (leftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(program, leftRow, newRow, valueFlags, false, uncheckedValue);
						}
						else
							LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				break;
			}
		}
		
		protected void InternalInsertRight(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			if (HasRow(newRow))				
			{
				switch (PropagateInsertRight)
				{
					case PropagateAction.True :
						RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
					break;
					
					case PropagateAction.Ensure :
					case PropagateAction.Ignore :
						using (IRow rightRow = RightNode.Select(program, newRow))
						{
							if (rightRow != null)
							{
								if (PropagateInsertRight == PropagateAction.Ensure)
									RightNode.Update(program, rightRow, newRow, valueFlags, false, uncheckedValue);
							}
							else
								RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
						}
					break;
				}
			}
		}

		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
			else
			{
				InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				if (PropagateUpdateLeft)
					LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
					
				if (PropagateUpdateRight)
				{
					if (HasRow(oldRow))
					{
						if (HasRow(newRow))
							RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
						else
							RightNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
					}
					else if (HasRow(newRow))
						RightNode.Insert(program, null, newRow, valueFlags, uncheckedValue);
				}
			}
			else
			{
				if (PropagateUpdateRight)
				{
					if (HasRow(oldRow))
					{
						if (HasRow(newRow))
							RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
						else
							RightNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
					}
					else if (HasRow(newRow))
						RightNode.Insert(program, null, newRow, valueFlags, uncheckedValue);
				}

				if (PropagateUpdateLeft)
					LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
		}

		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				if (PropagateDeleteRight && HasRow(row))
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
				
				if (PropagateDeleteLeft)
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
			else
			{
				if (PropagateDeleteLeft)
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);

				if (PropagateDeleteRight && HasRow(row))
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
		}

		public override void DetermineCursorCapabilities(Plan plan)
		{
			_cursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					(
						plan.CursorContext.CursorCapabilities & 
						LeftNode.CursorCapabilities & 
						CursorCapability.Updateable
					) &
					((_isLookup && !_isDetailLookup) ? CursorCapability.Updateable : (RightNode.CursorCapabilities & CursorCapability.Updateable))
				) |
				(
					plan.CursorContext.CursorCapabilities &
					(LeftNode.CursorCapabilities | RightNode.CursorCapabilities) &
					CursorCapability.Elaborable
				);
		}

		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			JoinLeftApplicationTransaction(program, row);
			if (!_isLookup || _isDetailLookup)
				JoinRightApplicationTransaction(program, row);
		}
	}
    
	// operator RightJoin(table{}, table{}, object, object) : table{}	
	public class RightOuterJoinNode : OuterJoinNode
	{
        protected override void DetermineColumns(Plan plan)
        {
			DetermineLeftColumns(plan, true);
			
			if (_rowExistsColumn != null)
			{
				Schema.TableVarColumn rowExistsColumn =
					Compiler.CompileIncludeColumnExpression
					(
						plan,
						_rowExistsColumn, 
						Keywords.RowExists, 
						plan.DataTypes.SystemBoolean, 
						Schema.TableVarColumnType.RowExists
					);
				DataType.Columns.Add(rowExistsColumn.Column);
				TableVar.Columns.Add(rowExistsColumn);
				_rowExistsColumnIndex = TableVar.Columns.Count - 1;
				rowExistsColumn.IsChangeRemotable = false;
				rowExistsColumn.IsDefaultRemotable = false;
			}

			DetermineRightColumns(plan, false);
        }
        
		protected override void DetermineJoinModifiers(Plan plan)
		{
			base.DetermineJoinModifiers(plan);
			
			AnyOf = String.Empty;
			AllOf = String.Empty;
			foreach (Schema.TableVarColumn column in LeftTableVar.Columns)
				if (!LeftKey.Columns.ContainsName(column.Name))
					AnyOf = (AnyOf == String.Empty) ? column.Name : String.Format("{0};{1}", AnyOf, Schema.Object.EnsureRooted(column.Name));
					
			AnyOf = LanguageModifiers.GetModifier(Modifiers, "AnyOf", AnyOf);
			AllOf = LanguageModifiers.GetModifier(Modifiers, "AllOf", AllOf);
			
			DetermineKeyColumns(AnyOf, AnyOfKey);
			DetermineKeyColumns(AllOf, AllOfKey);

			// Set change remotable to false for all AnyOf and AllOf columns
			foreach (Schema.TableVarColumn column in AnyOfKey.Columns)
				column.IsChangeRemotable = false;
				
			foreach (Schema.TableVarColumn column in AllOfKey.Columns)
				column.IsChangeRemotable = false;
		}
		
		protected override void DetermineDetailLookup(Plan plan)
		{
			if ((Modifiers != null) && (Modifiers.Contains("IsDetailLookup")))
				_isDetailLookup = Convert.ToBoolean(Modifiers["IsDetailLookup"]);
			else
			{
				_isDetailLookup = false;

				// If this is a lookup and the right join columns are a non-trivial proper superset of any key of the right side, this is a detail lookup
				if (_isLookup)
					foreach (Schema.Key key in RightTableVar.Keys)
						if ((_rightKey.Columns.Count > 0) && _rightKey.Columns.IsProperSupersetOf(key.Columns))
						{
							_isDetailLookup = true;
							break;
						}
				
				#if USEDETAILLOOKUP
				// if this is a lookup, the right key is not unique, and any key of the right table is a subset of the right key, this is a detail lookup.
				if (FIsLookup && !FRightKey.IsUnique)
					foreach (Schema.Key key in RightTableVar.Keys)
						if (key.Columns.IsSubsetOf(FRightKey.Columns))
						{
							FIsDetailLookup = true;
							break;
						}
				#endif

				#if USEINVARIANT
				// The invariant of any given tablevar is the intersection of the source columns of all source references that involve a key column.
				// If this is a lookup, the right key is not unique and the intersection of the invariant of the right table with the right join key is non-empty, this is a detail lookup.
				FIsDetailLookup = false;
				if (FIsLookup && !FLeftKey.IsUnique)
				{
					Schema.Key invariant = RightNode.TableVar.CalculateInvariant();
					foreach (Schema.Column column in FRightKey.Columns)
						if (invariant.Columns.Contains(column.Name))
						{
							FIsDetailLookup = true;
							break;
						}
				}
				#endif
			}
		}

		protected override void DetermineJoinKey(Plan plan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(plan, Cardinality, false, true);
			#else
			if (FRightKey.IsUnique && FLeftKey.IsUnique)
			{
				// If the left and right join keys are unique 
					// for each key of the right table var
						// copy the key
						// copy the key, with all correlated columns replaced with the left key columns (a no-op for natural joins), inferred as sparse if the right join key is not a superset of the key
					// for each key of the left table var
						// copy the key, if it is not a proper subset of the join key, inferred as sparse if the right join key is not a superset of the key
						// copy the key, if it is not a proper subset of the join key, with all correlated columns replaced with the right key columns (a no-op for natural joins), inferred as sparse if the left join key is not a superset of the key
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					Schema.Key newKey = CopyKey(rightKey);
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = true;
						foreach (Schema.TableVarColumn column in rightKey.Columns)
						{
							int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
							if (rightKeyIndex >= 0)
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
							else
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}

				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					if (!leftKey.Columns.IsProperSubsetOf(LeftKey.Columns))
					{
						Schema.Key newKey = CopyKey(leftKey, !RightKey.Columns.IsSupersetOf(leftKey.Columns));
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);

						if (!IsNatural)
						{
							newKey = new Schema.Key();
							newKey.IsInherited = true;
							newKey.IsSparse = !LeftKey.Columns.IsSupersetOf(leftKey.Columns);
							foreach (Schema.TableVarColumn column in leftKey.Columns)
							{
								int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
								if (leftKeyIndex >= 0)
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
								else
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
							}

							if (!TableVar.Keys.Contains(newKey))
								TableVar.Keys.Add(newKey);
						}
					}
				}
				
				RemoveSuperKeys();
			}
			else if (FRightKey.IsUnique)
			{
				// If only the right join key is unique,
					// for each key of the left table var
						// copy the key, excluding join columns that are not key columns of the right, inferred as sparse if the right join key is not a superset of the resulting key's columns
						// copy the key, excluding join columns that are not key columns of the right, with all correlated columns replaced with the right key columns (a no-op for natural joins), non-sparse
				foreach (Schema.Key leftKey in LeftTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					foreach (Schema.TableVarColumn column in leftKey.Columns)
					{
						int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
						if ((leftKeyIndex < 0) || RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					newKey.IsSparse = !RightKey.Columns.IsSupersetOf(leftKey.Columns);
					
					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);

					if (!IsNatural)
					{
						newKey = new Schema.Key();
						newKey.IsInherited = true;
						newKey.IsSparse = leftKey.IsSparse;
						foreach (Schema.TableVarColumn column in leftKey.Columns)
						{
							int leftKeyIndex = LeftKey.Columns.IndexOfName(column.Name);
							if (leftKeyIndex >= 0)
							{	
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[leftKeyIndex].Name))
									newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[leftKeyIndex].Name)]);
							}
							else
								newKey.Columns.Add(column);
						}
						
						if (!TableVar.Keys.Contains(newKey))
							TableVar.Keys.Add(newKey);
					}
				}
			}
			else if (FLeftKey.IsUnique)
			{
				// If only the left join key is unique,
					// for each key of the right table var
						// copy the key, excluding join columns that are not key columns of the left, non-sparse
						// copy the key, excluding join columns that are not key columns of the left, with all correlated columns replaced with the right key columns (a no-op for natural joins), inferred as sparse if the left join key is not a superset of the resulting key's columns
					// In addition to these, the same keys inferred for many-to-many joins are inferred
				foreach (Schema.Key rightKey in RightTableVar.Keys)
				{
					Schema.Key newKey = new Schema.Key();
					newKey.IsInherited = true;
					newKey.IsSparse = rightKey.IsSparse;
					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						if ((rightKeyIndex < 0) || LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name))
							newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(column.Name)]);
					}

					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);
	
					newKey = new Schema.Key();
					newKey.IsInherited = true;
					foreach (Schema.TableVarColumn column in rightKey.Columns)
					{
						int rightKeyIndex = RightKey.Columns.IndexOfName(column.Name);
						if (rightKeyIndex >= 0)
						{
							if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[rightKeyIndex].Name))
								newKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[rightKeyIndex].Name)]);
						}
						else
							newKey.Columns.Add(column);
					}
					
					newKey.IsSparse = !RightKey.Columns.IsSupersetOf(newKey.Columns);

					if (!TableVar.Keys.Contains(newKey))
						TableVar.Keys.Add(newKey);
				}
				
				DetermineManyToManyKeys(APlan);
			}
			else
			{
				DetermineManyToManyKeys(APlan);
			}
			#endif
		}
		
		protected override void DetermineJoinAlgorithm(Plan plan)
		{
			if (_expression == null)
				_joinAlgorithm = typeof(TimesTable);
			else
			{
				_joinOrder = Compiler.OrderFromKey(plan, _leftKey.Columns);
				Schema.OrderColumn column;
				for (int index = 0; index < _joinOrder.Columns.Count; index++)
				{
					column = _joinOrder.Columns[index];
					column.Sort = Compiler.GetSort(plan, column.Column.DataType);
					plan.AttachDependency(column.Sort);
					column.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to the left side
				if (!IsJoinOrder(plan, _leftKey, LeftNode))
					Nodes[0] = Compiler.EnsureSearchableNode(plan, LeftNode, _leftKey.Columns);

				// if both inputs are ordered by join keys and one or both join keys are unique
				if (IsJoinOrder(plan, _leftKey, LeftNode) && IsJoinOrder(plan, _rightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (_leftKey.IsUnique || _rightKey.IsUnique))
				{
					// use a merge join algorithm
					_joinAlgorithm = typeof(MergeRightJoinTable);
					
					Order = CopyOrder(RightNode.Order);
				}
				// else if the left input is ordered by join keys
					// use a searched join algorithm
				else if (IsJoinOrder(plan, _leftKey, LeftNode))
				{
					if (_leftKey.IsUnique)
						_joinAlgorithm = typeof(LeftUniqueSearchedRightJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueSearchedRightJoinTable);
						
					if (RightNode.Order != null)
						Order = CopyOrder(RightNode.Order);
				}
				else
				{
					// use a nested loop join
					// NOTE: this code will not be hit because of the join accelerator step above
					if (_leftKey.IsUnique)
						_joinAlgorithm = typeof(LeftUniqueNestedLoopRightJoinTable);
					else
						_joinAlgorithm = typeof(NonUniqueNestedLoopRightJoinTable);
				}
			}
		}
		
		protected override void InternalBindingTraversal(Plan plan, PlanNodeVisitor visitor)
		{
			if (IsLookup && !IsDetailLookup)
				plan.PushCursorContext(new CursorContext(plan.CursorContext.CursorType, plan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, plan.CursorContext.CursorIsolation));
			try
			{
				#if USEVISIT
				Nodes[0] = visitor.Visit(plan, Nodes[0]);
				#else
				Nodes[0].BindingTraversal(plan, visitor);
				#endif
			}
			finally
			{
				if (IsLookup && !IsDetailLookup)
					plan.PopCursorContext();
			}

			#if USEVISIT
			Nodes[1] = visitor.Visit(plan, Nodes[1]);
			#else
			Nodes[1].BindingTraversal(plan, visitor);
			#endif
			
			plan.EnterRowContext();
			try
			{
				#if USENAMEDROWVARIABLES
				plan.Symbols.Push(new Symbol(Keywords.Left, LeftTableType.RowType));
				#else
				APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				#endif
				try
				{
					#if USENAMEDROWVARIABLES
					plan.Symbols.Push(new Symbol(Keywords.Right, RightTableType.RowType));
					#else
					APlan.Symbols.Push(new Symbol(String.Empty, new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					#endif
					try
					{
						#if USEVISIT
						Nodes[2] = visitor.Visit(plan, Nodes[2]);
						#else
						Nodes[2].BindingTraversal(plan, visitor);
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
		
		public override Statement EmitStatement(EmitMode mode)
		{
			RightOuterJoinExpression expression = new RightOuterJoinExpression();
			expression.LeftExpression = (Expression)Nodes[0].EmitStatement(mode);
			expression.RightExpression = (Expression)Nodes[1].EmitStatement(mode);
			if (!IsNatural)
				expression.Condition = _expression; //(Expression)Nodes[2].EmitStatement(AMode);
			expression.IsLookup = _isLookup;
			expression.Cardinality = _cardinality;
			//LExpression.IsDetailLookup = FIsDetailLookup;
			if (_rowExistsColumnIndex >= 0)
			expression.RowExistsColumn = new IncludeColumnExpression(DataType.Columns[_rowExistsColumnIndex].Name);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			bool changed = false;

			if ((HasRowExistsColumn() && (columnName == String.Empty)) || IsRowExistsColumn(columnName))
			{
				changed = DefaultColumn(program, TableVar, newRow, valueFlags, TableVar.Columns[_rowExistsColumnIndex].Name) || changed;

				int rowIndex = newRow.DataType.Columns.IndexOfName(TableVar.Columns[_rowExistsColumnIndex].Name);
				if (!newRow.HasValue(rowIndex))
				{
					changed = true;
					newRow[rowIndex] = false;
				}
			}

			if (isDescending)
			{
				if (PropagateDefaultLeft && ((columnName == String.Empty) || LeftTableType.Columns.ContainsName(columnName)) && HasRow(newRow))
				{
					BitArray localValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int index = 0; index < LeftKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(LeftKey.Columns[index].Name);
					changed = LeftNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < LeftKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(LeftKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, LeftKey.Columns[index].Name);
				}

				if (PropagateDefaultRight && ((columnName == String.Empty) || RightTableType.Columns.ContainsName(columnName)))
				{
					BitArray localValueFlags = new BitArray(RightKey.Columns.Count);
					for (int index = 0; index < RightKey.Columns.Count; index++)
						localValueFlags[index] = newRow.HasValue(RightKey.Columns[index].Name);
					changed = RightNode.Default(program, oldRow, newRow, valueFlags, columnName) || changed;
					if (changed)
						for (int index = 0; index < RightKey.Columns.Count; index++)
							if (!localValueFlags[index] && newRow.HasValue(RightKey.Columns[index].Name))
								Change(program, oldRow, newRow, valueFlags, RightKey.Columns[index].Name);
				}
			}

			return changed;
		}
		
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			// The new row only needs to satisfy the join predicate if the row contains values for the left side.
			if (!_isNatural && (columnName == String.Empty) && HasRow(newRow))
				return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
			else
			{
				if (isDescending)
				{
					bool changed = false;
					if (((columnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(columnName))) && HasRow(newRow))
						changed = LeftNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;

					if ((columnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(columnName)))
						changed = RightNode.Validate(program, oldRow, newRow, valueFlags, columnName) || changed;
					return changed;
				}
			}
			return false;
		}

		/*
			if the change is made to a specific column and the join is not natural
				if a change is made to a right join-key column
					if !IsRepository and RetrieveLeft
						Lookup the left side from the database
						if a row is found
							set the rowexists flag if there is one
							all left columns are set to the lookup row
							fire a change for each column of the left side
						else
							if HasRowExistsColumn
								clear it
							else
								clear any of/all of columns
							if Clear
								foreach left column
									Clear
									fire a change
					else
						if HasRow && CoordinateLeft
							Coordinate the corresponding left key column with the new value for the right key
							fire a change for the column
							
				if a change is made to the rowexists column
					if it is set true
						if !IsRepository and RetrieveLeft
							Lookup the left side from the database
							if a row is found
								all right columns are set to the lookup row
								fire a change for each column of the left side
							else
								foreach left column
									if IsKeyColumn && CoordinateLeft
										set the column to the right value
										fire a change
									else
										if Clear
											Clear
											fire a default
											fire a change
						else
							foreach left column
								if IsKeyColumn && CoordinateLeft
									set the column to the right value
									fire a change
								else
									if Clear
										Clear
										fire a default
										fire a change
					else (the rowexists column is cleared)
						if Clear
							foreach left column
								Clear
								fire a change
							
				if a change is made to a left join-key column
					if HasRow && CoordinateRight
						set the right column to the left value
						fire a change
						
				if a change is made to an any of/all of column
					if HasValues
						if HasRowExistsColumn
							set it
						foreach left column
							if IsKeyColumn && CoordinateLeft
								set the column to the right value
								fire a change
						else
							fire a default
							fire a change if necessary
					else
						if HasRowExistsColumn
							clear it
						if Clear
							foreach left column
								Clear
								fire a change
									
			if PropagateChangeLeft and change is not to a specific column, or is to a column in the left side
				propagate the change

			if PropagateChangeRight and change is not to a specific column, or is to a column in the right side
				propagate the change
		*/
		protected override bool InternalChange(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName)
		{
			PushNewRow(program, newRow);
			try
			{
				bool changed = false;
				bool changePropagated = false;
				
				if (columnName != String.Empty)
				{
					bool isRowExistsColumn = IsRowExistsColumn(columnName);
					if (_rightKey.Columns.ContainsName(columnName) || isRowExistsColumn)
					{
						changed = true;
						if (IsNatural)
							changePropagated = true;
						if (!isRowExistsColumn || HasRow(newRow))
						{
							if (!program.ServerProcess.ServerSession.Server.IsEngine && RetrieveLeft)
							{
								EnsureLeftSelectNode(program.Plan);
								using (ITable table = (ITable)_leftSelectNode.Execute(program))
								{
									table.Open();
									if (table.Next())
									{
										table.Select(newRow);
										if (HasRowExistsColumn() && !isRowExistsColumn)
											newRow[DataType.Columns[_rowExistsColumnIndex].Name] = true;

										if (PropagateChangeLeft)
											foreach (Schema.Column column in LeftTableType.Columns)
												LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
									}
									else
									{
										if (!isRowExistsColumn)
											if (HasRowExistsColumn())
												newRow[DataType.Columns[_rowExistsColumnIndex].Name] = false;
											else
											{
												// Clear any of/all of columns
												// TODO: This should be looked at, if any of/all of are both subsets of the Left side, nothing needs to happen here
												// I don't want to run through them all here because it will end up firing changes twice in the default case
											}
											
										foreach (Schema.Column column in LeftTableType.Columns)
										{
											int leftKeyIndex = _leftKey.Columns.IndexOfName(column.Name);
											if (!HasRowExistsColumn() && (leftKeyIndex >= 0) && CoordinateLeft)
											{
												int rightRowIndex = newRow.DataType.Columns.IndexOfName(_rightKey.Columns[leftKeyIndex].Name);
												if (newRow.HasValue(rightRowIndex))
													newRow[column.Name] = newRow[rightRowIndex];
												else
													newRow.ClearValue(column.Name);
											}
											else
											{
												if (ClearLeft)
													newRow.ClearValue(column.Name);

												if (isRowExistsColumn && PropagateDefaultLeft)
													LeftNode.Default(program, oldRow, newRow, valueFlags, column.Name);
											}
												
											if (PropagateChangeLeft)
												LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
										}
									}
								}
							}
							else
							{
								if (!isRowExistsColumn)
								{
									if (HasRow(newRow) && CoordinateLeft)
										CoordinateLeftJoinKey(program, oldRow, newRow, valueFlags, columnName);
								}
								else // rowexists was set
								{
									foreach (Schema.TableVarColumn column in LeftTableVar.Columns)
									{
										int leftKeyIndex = _leftKey.Columns.IndexOfName(column.Name);
										if ((leftKeyIndex >= 0) && CoordinateLeft)
										{
											int rightRowIndex = newRow.DataType.Columns.IndexOfName(_rightKey.Columns[leftKeyIndex].Name);
											if (newRow.HasValue(rightRowIndex))
												newRow[column.Name] = newRow[rightRowIndex];
											else
												newRow.ClearValue(column.Name);
										}
										else
										{
											if (ClearLeft)
												newRow.ClearValue(column.Name);

											if (!newRow.HasValue(column.Name) && PropagateDefaultLeft)
												LeftNode.Default(program, oldRow, newRow, valueFlags, column.Name);
										}
										
										if (PropagateChangeLeft)
											LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
									}
								}
							}
						}
						else // rowexists was cleared
						{
							if (ClearLeft)
								foreach (Schema.TableVarColumn column in LeftTableVar.Columns)
								{
									newRow.ClearValue(column.Name);
									if (PropagateChangeLeft)
										LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
						}
					}
					
					if (_leftKey.Columns.ContainsName(columnName) && HasRow(newRow) && CoordinateRight)
					{
						changed = true;
						if (IsNatural)
							changePropagated = true;
						CoordinateRightJoinKey(program, oldRow, newRow, valueFlags, columnName);
					}
						
					if (_anyOfKey.Columns.ContainsName(columnName) || _allOfKey.Columns.ContainsName(columnName))
					{
						changed = true;
						if (HasValues(newRow))
						{
							if (HasRowExistsColumn())
								newRow[DataType.Columns[_rowExistsColumnIndex].Name] = true;
								
							foreach (Schema.TableVarColumn column in LeftTableVar.Columns)
							{
								int leftKeyIndex = _leftKey.Columns.IndexOfName(column.Name);
								if ((leftKeyIndex >= 0) && CoordinateLeft)
									CoordinateLeftJoinKey(program, oldRow, newRow, valueFlags, _rightKey.Columns[leftKeyIndex].Name);
								else
								{
									if (PropagateDefaultLeft && LeftNode.Default(program, oldRow, newRow, valueFlags, column.Name) && PropagateChangeLeft)
										LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
							}
						}
						else
						{
							if (HasRowExistsColumn())
								newRow[DataType.Columns[_rowExistsColumnIndex].Name] = false;
								
							if (ClearLeft)
								foreach (Schema.TableVarColumn column in LeftTableVar.Columns)
								{
									newRow.ClearValue(column.Name);
									if (PropagateChangeLeft)
										LeftNode.Change(program, oldRow, newRow, valueFlags, column.Name);
								}
						}
					}
				}
					
				if (!changePropagated && PropagateChangeRight && ((columnName == String.Empty) || RightTableType.Columns.ContainsName(columnName)))
					changed = RightNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;
					
				if (!changePropagated && PropagateChangeLeft && ((columnName == String.Empty) || LeftTableType.Columns.ContainsName(columnName)))
					changed = LeftNode.Change(program, oldRow, newRow, valueFlags, columnName) || changed;

				return changed;
			}
			finally
			{
				PopRow(program);
			}
		}
		
		protected void InternalInsertLeft(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			if (HasRow(newRow))
			{
				switch (PropagateInsertLeft)
				{
					case PropagateAction.True : 
						LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); break;
						
					case PropagateAction.Ensure :
					case PropagateAction.Ignore :
					using (IRow leftRow = LeftNode.Select(program, newRow))
					{
						if (leftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(program, leftRow, newRow, valueFlags, false, uncheckedValue);
						}
						else
							LeftNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
					break;
				}
			}
		}
		
		protected void InternalInsertRight(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True :
					RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue); 
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (IRow rightRow = RightNode.Select(program, newRow))
					{
						if (rightRow != null)
						{
							if (PropagateInsertRight == PropagateAction.Ensure)
								RightNode.Update(program, rightRow, newRow, valueFlags, false, uncheckedValue);
						}
						else
							RightNode.Insert(program, oldRow, newRow, valueFlags, uncheckedValue);
					}
				break;
			}
		}

		protected override void InternalExecuteInsert(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
			else
			{
				InternalInsertRight(program, oldRow, newRow, valueFlags, uncheckedValue);
				InternalInsertLeft(program, oldRow, newRow, valueFlags, uncheckedValue);
			}
		}
		
		protected override void InternalExecuteUpdate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				if (PropagateUpdateLeft)
				{
					if (HasRow(oldRow))
					{
						if (HasRow(newRow))
							LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
						else
							LeftNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
					}
					else
						if (HasRow(newRow))
							LeftNode.Insert(program, null, newRow, valueFlags, uncheckedValue);
				}

				if (PropagateUpdateRight)
					RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
			}
			else
			{
				if (PropagateUpdateRight)
					RightNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
					
				if (PropagateUpdateLeft)
				{
					if (HasRow(oldRow))
					{
						if (HasRow(newRow))
							LeftNode.Update(program, oldRow, newRow, valueFlags, checkConcurrency, uncheckedValue);
						else
							LeftNode.Delete(program, oldRow, checkConcurrency, uncheckedValue);
					}
					else
						if (HasRow(newRow))
							LeftNode.Insert(program, null, newRow, valueFlags, uncheckedValue);
				}
			}
		}

		protected override void InternalExecuteDelete(Program program, IRow row, bool checkConcurrency, bool uncheckedValue)
		{
			if (_updateLeftToRight)
			{
				if (PropagateDeleteRight)
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
				
				if (PropagateDeleteLeft && HasRow(row))
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
			else
			{
				if (PropagateDeleteLeft && HasRow(row))
					LeftNode.Delete(program, row, checkConcurrency, uncheckedValue);
				
				if (PropagateDeleteRight)
					RightNode.Delete(program, row, checkConcurrency, uncheckedValue);
			}
		}

		public override void DetermineCursorCapabilities(Plan plan)
		{
			_cursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					(
						plan.CursorContext.CursorCapabilities & 
						RightNode.CursorCapabilities & 
						CursorCapability.Updateable
					) &
					((_isLookup && !_isDetailLookup) ? CursorCapability.Updateable : (LeftNode.CursorCapabilities & CursorCapability.Updateable))
				) |
				(
					plan.CursorContext.CursorCapabilities &
					(LeftNode.CursorCapabilities | RightNode.CursorCapabilities) &
					CursorCapability.Elaborable
				);
		}

		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			if (!_isLookup || _isDetailLookup)
				JoinLeftApplicationTransaction(program, row);
			JoinRightApplicationTransaction(program, row);
		}
	}
}