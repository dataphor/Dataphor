/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define USEUNIFIEDJOINKEYINFERENCE // Unified join key inference uses the same algorithm for all joins. See InferJoinKeys for more information.
//#define DISALLOWMANYTOMANYOUTERJOINS
	
namespace Alphora.Dataphor.DAE.Runtime.Instructions
{											  
	using System;
	using System.Text;
	using System.Threading;
	using System.Collections;
	using System.Collections.Specialized;

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

	// Root node for all join and semi operators
	public class ConditionedTableNode : BinaryTableNode
	{
		protected Schema.JoinKey FLeftKey = new Schema.JoinKey();
		public Schema.JoinKey LeftKey
		{
			get { return FLeftKey; }
			set { FLeftKey = value; }
		}
		
		protected Schema.JoinKey FRightKey = new Schema.JoinKey();
		public Schema.JoinKey RightKey
		{
			get { return FRightKey; }
			set { FRightKey = value; }
		}
		
		protected Schema.Order FJoinOrder;
		public Schema.Order JoinOrder
		{
			get { return FJoinOrder; }
			set { FJoinOrder = value; }
		}
		
		protected bool FIsNatural;
		public bool IsNatural
		{
			get { return FIsNatural; }
			set { FIsNatural = value; }
		}
		
		private bool FEnforcePredicate = false;
		public bool EnforcePredicate
		{
			get { return FEnforcePredicate; }
			set { FEnforcePredicate = value; }
		}
		
		protected Expression CollapseQualifierExpression(QualifierExpression AExpression)
		{
			if (AExpression.LeftExpression is QualifierExpression)
				AExpression.LeftExpression = CollapseQualifierExpression((QualifierExpression)AExpression.LeftExpression);
			if (AExpression.RightExpression is QualifierExpression)
				AExpression.RightExpression = CollapseQualifierExpression((QualifierExpression)AExpression.RightExpression);
			if ((AExpression.LeftExpression is IdentifierExpression) && (AExpression.RightExpression is IdentifierExpression))
				return new IdentifierExpression(String.Format("{0}{1}{2}", ((IdentifierExpression)AExpression.LeftExpression).Identifier, Keywords.Qualifier, ((IdentifierExpression)AExpression.RightExpression).Identifier));
			else
				throw new CompilerException(CompilerException.Codes.UnableToCollapseQualifierExpression, AExpression);
		}
		
		protected void CollapseJoinExpression(Expression AExpression)
		{
			if (AExpression is BinaryExpression)
			{
				BinaryExpression LBinaryExpression = (BinaryExpression)AExpression;
				if (LBinaryExpression.LeftExpression is BinaryExpression)
					CollapseJoinExpression(LBinaryExpression.LeftExpression);
				else if (LBinaryExpression.LeftExpression is QualifierExpression)
					LBinaryExpression.LeftExpression = CollapseQualifierExpression((QualifierExpression)LBinaryExpression.LeftExpression);
				
				if (LBinaryExpression.RightExpression is BinaryExpression)
					CollapseJoinExpression(LBinaryExpression.RightExpression);
				else if (LBinaryExpression.RightExpression is QualifierExpression)
					LBinaryExpression.RightExpression = CollapseQualifierExpression((QualifierExpression)LBinaryExpression.RightExpression);
			}
		}
		
		protected bool IsEquiJoin(Expression AExpression)
		{
			// TODO: IsEquiJoin should be a semantic determination, not a syntactic one...
			CollapseJoinExpression(AExpression);
			
			if (AExpression is BinaryExpression)
			{
				BinaryExpression LExpression = (BinaryExpression)AExpression;
				if (String.Compare(LExpression.Instruction, Instructions.And) == 0)
					return IsEquiJoin(LExpression.LeftExpression) && IsEquiJoin(LExpression.RightExpression);
				
				if (String.Compare(LExpression.Instruction, Instructions.Equal) == 0)
				{
					if ((LExpression.LeftExpression is IdentifierExpression) && (LExpression.RightExpression is IdentifierExpression))
					{
						string LLeftIdentifier = ((IdentifierExpression)LExpression.LeftExpression).Identifier;
						string LRightIdentifier = ((IdentifierExpression)LExpression.RightExpression).Identifier;
						if (LLeftIdentifier.IndexOf(Keywords.Left) == 0)
							LLeftIdentifier = Schema.Object.Dequalify(LLeftIdentifier);
						if (LRightIdentifier.IndexOf(Keywords.Right) == 0)
							LRightIdentifier = Schema.Object.Dequalify(LRightIdentifier);
							
						if (!LeftTableType.Columns.Contains(LLeftIdentifier) && !RightTableType.Columns.Contains(LLeftIdentifier))
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, LExpression.LeftExpression, LLeftIdentifier);
							
						if (LeftTableType.Columns.Contains(LLeftIdentifier) && RightTableType.Columns.Contains(LLeftIdentifier))
							throw new CompilerException
							(
								CompilerException.Codes.AmbiguousIdentifier, 
								LLeftIdentifier, 
								ExceptionUtility.StringsToCommaList
								(
									new string[]
									{
										LeftTableType.Columns[LLeftIdentifier].Name,
										RightTableType.Columns[LLeftIdentifier].Name
									}
								)
							);
						
						if (!LeftTableType.Columns.Contains(LRightIdentifier) && !RightTableType.Columns.Contains(LRightIdentifier))
							throw new CompilerException(CompilerException.Codes.UnknownIdentifier, LExpression.RightExpression, LRightIdentifier);
							
						if (LeftTableType.Columns.Contains(LRightIdentifier) && RightTableType.Columns.Contains(LRightIdentifier))
							throw new CompilerException
							(
								CompilerException.Codes.AmbiguousIdentifier,
								LRightIdentifier,
								ExceptionUtility.StringsToCommaList
								(
									new string[]
									{
										LeftTableType.Columns[LRightIdentifier].Name,
										RightTableType.Columns[LRightIdentifier].Name
									}
								)
							);
							
						return
							(LeftTableType.Columns.Contains(LLeftIdentifier) && RightTableType.Columns.Contains(LRightIdentifier)) ||
							(LeftTableType.Columns.Contains(LRightIdentifier) && RightTableType.Columns.Contains(LLeftIdentifier));
					}
				}
			}
			
			return false;
		}
		
		protected void DetermineJoinKeys(Expression AExpression)
		{
            // !!! Assumption: IsEquiJoin == true;
            BinaryExpression LExpression = (BinaryExpression)AExpression;
            if (String.Compare(LExpression.Instruction, Instructions.And) == 0)
            {
                DetermineJoinKeys(LExpression.LeftExpression);
                DetermineJoinKeys(LExpression.RightExpression);
            }
            else if (String.Compare(LExpression.Instruction, Instructions.Equal) == 0)
            {
				string LLeftIdentifier = ((IdentifierExpression)LExpression.LeftExpression).Identifier;
				string LRightIdentifier = ((IdentifierExpression)LExpression.RightExpression).Identifier;
				if (LLeftIdentifier.IndexOf(Keywords.Left) == 0)
					LLeftIdentifier = Schema.Object.Dequalify(LLeftIdentifier);
				if (LRightIdentifier.IndexOf(Keywords.Right) == 0)
					LRightIdentifier = Schema.Object.Dequalify(LRightIdentifier);

				Schema.TableVarColumn LColumn;
                if (LeftTableVar.Columns.Contains(LLeftIdentifier))
                {
					LColumn = LeftTableVar.Columns[LLeftIdentifier];
                    FLeftKey.Columns.Add(LColumn);
                    LColumn = RightTableVar.Columns[LRightIdentifier];
                    FRightKey.Columns.Add(LColumn);
                }
                else
                {
					LColumn = RightTableVar.Columns[LLeftIdentifier];
                    FRightKey.Columns.Add(LColumn);
                    LColumn = LeftTableVar.Columns[LRightIdentifier];
                    FLeftKey.Columns.Add(LColumn);
                }
            }
		}

		protected bool IsJoinOrder(Plan APlan, Schema.JoinKey AJoinKey, TableNode ANode)
		{
			return (ANode.Supports(CursorCapability.Searchable) && (ANode.Order != null) && (new Schema.Order(AJoinKey, APlan).Equivalent(ANode.Order)));
		}

		protected Expression FExpression;
		public Expression Expression
		{
			get { return FExpression; }
			set { FExpression = value; }
		}
		
		protected virtual Expression GenerateJoinExpression()
		{
			// foreach common column name
			//   generate an expression of the form left.column = right.column [and...]
			Expression LExpression = null;
			BinaryExpression LEqualExpression;
			int LColumnIndex;
			Schema.TableVarColumn LLeftColumn;
			Schema.TableVarColumn LRightColumn;
			for (int LIndex = 0; LIndex < LeftTableVar.Columns.Count; LIndex++)
			{
				LLeftColumn = LeftTableVar.Columns[LIndex];
				LColumnIndex = RightTableVar.Columns.IndexOfName(LLeftColumn.Name);
				if (LColumnIndex >= 0)
				{
					LRightColumn = RightTableVar.Columns[LColumnIndex];
					LEqualExpression = 
						new BinaryExpression
						(
							new IdentifierExpression(Schema.Object.Qualify(LLeftColumn.Name, Keywords.Left)), 
							Instructions.Equal, 
							new IdentifierExpression(Schema.Object.Qualify(LRightColumn.Name, Keywords.Right))
						);
						
					FLeftKey.Columns.Add(LLeftColumn);
					FRightKey.Columns.Add(LRightColumn);
						
					if (LExpression == null)
						LExpression = LEqualExpression;
					else
						LExpression = 
							new BinaryExpression
							(
								LExpression,
								Instructions.And,
								LEqualExpression
							);
				}
			}
			return LExpression;
		}
				
		protected virtual void InternalCreateDataType(Plan APlan)
		{
			FDataType = new Schema.TableType();
		}

		// LeftExistsNode
		protected PlanNode FLeftExistsNode;
		public PlanNode LeftExistsNode
		{
			get { return FLeftExistsNode; }
			set { FLeftExistsNode = value; }
		}
		
		// RightExistsNode
		protected PlanNode FRightExistsNode;
		public PlanNode RightExistsNode
		{
			get { return FRightExistsNode; }
			set { FRightExistsNode = value; }
		}
		
		protected bool LeftExists(ServerProcess AProcess, Row ARow)
		{
			EnsureLeftExistsNode(AProcess.Plan);
			PushNewRow(AProcess, ARow);
			try
			{
				DataVar LObject = FLeftExistsNode.Execute(AProcess);
				return LObject.Value.AsBoolean;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		protected bool RightExists(ServerProcess AProcess, Row ARow)
		{
			EnsureRightExistsNode(AProcess.Plan);
			PushNewRow(AProcess, ARow);
			try
			{
				DataVar LObject = FRightExistsNode.Execute(AProcess);
				return LObject.Value.AsBoolean;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		protected void EnsureLeftExistsNode(Plan APlan)
		{
			if (FLeftExistsNode == null)
			{
				ApplicationTransaction LTransaction = null;
				if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.ServerProcess.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushLookup();
					try
					{
						APlan.PushATCreationContext();
						try
						{
							APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(APlan, FSymbols);
								try
								{
									APlan.EnterRowContext();
									try
									{
										APlan.Symbols.Push(new DataVar(DataType.NewRowType));
										try
										{
											FLeftExistsNode =
												Compiler.Bind
												(
													APlan,
													Compiler.EmitUnaryNode
													(
														APlan,
														Instructions.Exists,
														Compiler.EmitRestrictNode
														(
															APlan,
															Compiler.CompileExpression(APlan, (Expression)LeftNode.EmitStatement(EmitMode.ForCopy)),
															Compiler.BuildKeyEqualExpression
															(
																APlan,
																new Schema.RowType(FLeftKey.Columns).Columns,
																new Schema.RowType(FLeftKey.Columns, Keywords.New).Columns
															)
														)
													)
												);
										}
										finally
										{
											APlan.Symbols.Pop();
										}
									}
									finally
									{
										APlan.ExitRowContext();
									}
								}
								finally
								{
									PopSymbols(APlan, FSymbols);
								}
							}
							finally
							{
								APlan.PopCursorContext();
							}
						}
						finally
						{
							APlan.PopATCreationContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopLookup();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
		
		protected void EnsureRightExistsNode(Plan APlan)
		{
			if (FRightExistsNode == null)
			{
				ApplicationTransaction LTransaction = null;
				if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.ServerProcess.GetApplicationTransaction();
				try
				{
					if (LTransaction != null)
						LTransaction.PushLookup();
					try
					{
						APlan.PushATCreationContext();
						try
						{
							APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(APlan, FSymbols);
								try
								{
									APlan.EnterRowContext();
									try
									{
										APlan.Symbols.Push(new DataVar(DataType.NewRowType));
										try
										{
											FRightExistsNode =
												Compiler.Bind
												(
													APlan,
													Compiler.EmitUnaryNode
													(
														APlan,
														Instructions.Exists,
														Compiler.EmitRestrictNode
														(
															APlan,
															Compiler.CompileExpression(APlan, (Expression)RightNode.EmitStatement(EmitMode.ForCopy)),
															Compiler.BuildKeyEqualExpression
															(
																APlan,
																new Schema.RowType(FRightKey.Columns).Columns,
																new Schema.RowType(FRightKey.Columns, Keywords.New).Columns
															)
														)
													)
												);
										}
										finally
										{
											APlan.Symbols.Pop();
										}
									}
									finally
									{
										APlan.ExitRowContext();
									}
								}
								finally
								{
									PopSymbols(APlan, FSymbols);
								}
							}
							finally
							{
								APlan.PopCursorContext();
							}
						}
						finally
						{
							APlan.PopATCreationContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopLookup();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
	}
	
	public abstract class SemiTableNode : ConditionedTableNode
	{
		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			Row LLeftRow = new Row(AProcess, LeftNode.DataType.RowType);
			try
			{
				ARow.CopyTo(LLeftRow);
				LeftNode.JoinApplicationTransaction(AProcess, LLeftRow);
			}
			finally
			{
				LLeftRow.Dispose();
			}
		}

		protected virtual void DetermineSemiTableModifiers(Plan APlan)
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
			bool LIsLeftNilable = (PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft;
			bool LIsRightNilable = (PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
			
			int LRightColumnIndex = RightTableVar.Columns.Count == 0 ? LeftTableVar.Columns.Count : TableVar.Columns.IndexOfName(RightTableVar.Columns[0].Name);
			
			for (int LIndex = 0; LIndex < TableVar.Columns.Count; LIndex++)
			{
				if (LIndex < LRightColumnIndex)
				{
					if (LIsLeftNilable)
						TableVar.Columns[LIndex].IsNilable = LIsLeftNilable;
				}
				else
				{
					if (LIsRightNilable)
						TableVar.Columns[LIndex].IsNilable = LIsRightNilable;
				}
			}
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			InternalCreateDataType(APlan);

			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(LeftTableVar.MetaData);
			
			if (FIsNatural)
			{
				FExpression = GenerateJoinExpression();

				if ((FExpression == null) && !APlan.ServerProcess.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.PossiblyIncorrectSemiTableExpression, CompilerErrorLevel.Warning, APlan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString()));
			}
			else
			{
				if (!IsEquiJoin(FExpression))
					throw new CompilerException(CompilerException.Codes.JoinMustBeEquiJoin, FExpression);
					
				DetermineJoinKeys(FExpression);
			}
			
			foreach (Schema.Key LKey in LeftTableVar.Keys)
				if (FLeftKey.Columns.IsSupersetOf(LKey.Columns))
				{
					FLeftKey.IsUnique = true;
					break;
				}
				
			foreach (Schema.Key LKey in RightTableVar.Keys)
				if (FRightKey.Columns.IsSupersetOf(LKey.Columns))
				{
					FRightKey.IsUnique = true;
					break;
				}
				
			// Determine columns
			CopyTableVarColumns(LeftTableVar.Columns);
			
			DetermineRemotable(APlan);

			// Determine the keys
			CopyPreservedKeys(LeftTableVar.Keys);
			
			// Determine modifiers
			DetermineSemiTableModifiers(APlan);
			
			// Determine orders			
			CopyPreservedOrders(LeftTableVar.Orders);
			if (LeftNode.Order != null)
				Order = CopyOrder(LeftNode.Order);
			
			#if UseReferenceDerivation
			CopySourceReferences(APlan, LeftTableVar.SourceReferences);
			CopyTargetReferences(APlan, LeftTableVar.TargetReferences);
			#endif
			
			APlan.EnterRowContext();
			try
			{			
				APlan.Symbols.Push(new DataVar(new Schema.RowType(LeftTableType.Columns, Keywords.Left)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(RightTableType.Columns, Keywords.Right)));
					try
					{
						if (FExpression != null)
						{
							PlanNode LPlanNode = Compiler.CompileExpression(APlan, FExpression);
							if (!(LPlanNode.DataType.Is(APlan.Catalog.DataTypes.SystemBoolean)))
								throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, FExpression);
							Nodes.Add(LPlanNode);
						}
						else
							Nodes.Add(new ValueNode(new Scalar(APlan.ServerProcess, APlan.ServerProcess.DataTypes.SystemBoolean, true)));
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			APlan.PushCursorContext(new CursorContext(APlan.CursorContext.CursorType, APlan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, APlan.CursorContext.CursorIsolation));
			try
			{
				Nodes[1].DetermineBinding(APlan);
			}
			finally
			{
				APlan.PopCursorContext();
			}
			
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new DataVar(new Schema.RowType(LeftTableType.Columns, Keywords.Left)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(RightTableType.Columns, Keywords.Right)));
					try
					{
						Nodes[2].DetermineBinding(APlan);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			base.DetermineDevice(APlan);
			if (!FDeviceSupported)
				DetermineSemiTableAlgorithm(APlan);
		}
		
		protected abstract void DetermineSemiTableAlgorithm(Plan APlan);
		
		public virtual void DetermineCursorCapabilities(Plan APlan)
		{
			FCursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					APlan.CursorContext.CursorCapabilities &
					LeftNode.CursorCapabilities &
					CursorCapability.Updateable
				);
		}

		public override void DetermineCursorBehavior(Plan APlan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				FCursorType = CursorType.Dynamic;
			else
				FCursorType = CursorType.Static;

			FRequestedCursorType = APlan.CursorContext.CursorType;

			DetermineCursorCapabilities(APlan);

			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}

		protected Type FSemiTableAlgorithm;

		public override void DetermineRemotable(Plan APlan)
		{
			base.DetermineRemotable(APlan);
			
			FTableVar.ShouldChange = PropagateChangeLeft && (FTableVar.ShouldChange || LeftTableVar.ShouldChange);
			FTableVar.ShouldDefault = PropagateDefaultLeft && (FTableVar.ShouldDefault || LeftTableVar.ShouldDefault);
			FTableVar.ShouldValidate = PropagateValidateLeft && (FTableVar.ShouldValidate || LeftTableVar.ShouldValidate);
			
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				Schema.TableVarColumn LSourceColumn = LeftTableVar.Columns[LeftTableVar.Columns.IndexOfName(LColumn.Name)];

				LColumn.ShouldChange = PropagateChangeLeft && (LColumn.ShouldChange || LSourceColumn.ShouldChange);
				FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;

				LColumn.ShouldDefault = PropagateDefaultLeft && (LColumn.ShouldDefault || LSourceColumn.ShouldDefault);
				FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;

				LColumn.ShouldValidate = PropagateValidateLeft && (LColumn.ShouldValidate || LSourceColumn.ShouldValidate);
				FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;
			}
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if (AIsDescending && PropagateDefaultLeft)
				return LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			if (PropagateChangeLeft)
				return LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AIsDescending && PropagateValidateLeft)
				return LeftNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
			return false;
		}
		
		protected abstract void ValidatePredicate(ServerProcess AProcess, Row ARow);
		
		protected override void InternalExecuteInsert(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True :
					if (!AUnchecked)
						ValidatePredicate(AProcess, ANewRow);
					LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					if (!AUnchecked)
						ValidatePredicate(AProcess, ANewRow);
					
					using (Row LLeftRow = new Row(AProcess, LeftNode.DataType.RowType))
					{
						ANewRow.CopyTo(LLeftRow);
						using (Row LCurrentRow = LeftNode.Select(AProcess, LLeftRow))
						{
							if (LCurrentRow != null)
							{
								if (PropagateInsertLeft == PropagateAction.Ensure)
									LeftNode.Update(AProcess, LCurrentRow, ANewRow, AValueFlags, false, AUnchecked);
							}
							else
								LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
						}
					}
				break;
			}
		}
		
/*
		protected override void InternalBeforeDeviceInsert(Row ARow, ServerProcess AProcess)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.BeforeDeviceInsert(ARow, AProcess);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			
			LeftNode.BeforeDeviceInsert(ARow, AProcess);
		}
		
		protected override void InternalAfterDeviceInsert(Row ARow, ServerProcess AProcess)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.AfterDeviceInsert(ARow, AProcess);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
			
			LeftNode.AfterDeviceInsert(ARow, AProcess);
		}
*/
		
		protected override void InternalExecuteUpdate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateUpdateLeft)
			{
				if (!AUnchecked)
					ValidatePredicate(AProcess, ANewRow);
				LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
		}
		
/*
		protected override void InternalBeforeDeviceUpdate(Row AOldRow, Row ANewRow, ServerProcess AProcess)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.BeforeDeviceInsert(ANewRow, AProcess);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
				
			LeftNode.BeforeDeviceUpdate(AOldRow, ANewRow, AProcess);
		}
		
		protected override void InternalAfterDeviceUpdate(Row AOldRow, Row ANewRow, ServerProcess AProcess)
		{
			bool LRightNodeValid = false;
			try
			{
				RightNode.AfterDeviceInsert(ANewRow, AProcess);
				LRightNodeValid = true;
			}
			catch (DataphorException LException)
			{
				if ((LException.Severity != ErrorSeverity.User) && (LException.Severity != ErrorSeverity.Application))
					throw;
			}
			
			if (LRightNodeValid)
				throw new RuntimeException(RuntimeException.Codes.RowViolatesDifferencePredicate, ErrorSeverity.User);
				
			LeftNode.AfterDeviceUpdate(AOldRow, ANewRow, AProcess);
		}
*/
		
		protected override void InternalExecuteDelete(ServerProcess AProcess, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (PropagateDeleteLeft)
				LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
		}
		
/*
		protected override void InternalBeforeDeviceDelete(Row ARow, ServerProcess AProcess)
		{
			LeftNode.BeforeDeviceDelete(ARow, AProcess);
		}
		
		protected override void InternalAfterDeviceDelete(Row ARow, ServerProcess AProcess)
		{
			LeftNode.AfterDeviceDelete(ARow, AProcess);
		}
*/

		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			AWriter.WriteAttributeString("Condition", Nodes[2].SafeEmitStatementAsString());
		}
	}
	
	public class HavingNode : SemiTableNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			HavingTable LTable = (HavingTable)Activator.CreateInstance(FSemiTableAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, AProcess}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				LTable.Open();
				return new DataVar(String.Empty, FDataType, LTable);
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			HavingExpression LExpression = new HavingExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			if (!IsNatural)
				LExpression.Condition = FExpression;
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}

		protected override void ValidatePredicate(ServerProcess AProcess, Row ARow)
		{
			if (EnforcePredicate && !RightExists(AProcess, ARow))
				throw new RuntimeException(RuntimeException.Codes.RowViolatesHavingPredicate, ErrorSeverity.User);
		}
		
		protected override void DetermineSemiTableAlgorithm(Plan APlan)
		{
			FSemiTableAlgorithm = typeof(SearchedHavingTable);

			// ensure that the right side is a searchable node
			Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, RightKey);
		}
	}
	
	public class WithoutNode : SemiTableNode
	{
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			WithoutTable LTable = (WithoutTable)Activator.CreateInstance(FSemiTableAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, AProcess}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				LTable.Open();
				return new DataVar(String.Empty, FDataType, LTable);
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			WithoutExpression LExpression = new WithoutExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			if (!IsNatural)
				LExpression.Condition = FExpression;
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}

		protected override void ValidatePredicate(ServerProcess AProcess, Row ARow)
		{
			if (EnforcePredicate && RightExists(AProcess, ARow))
				throw new RuntimeException(RuntimeException.Codes.RowViolatesWithoutPredicate, ErrorSeverity.User);
		}

		protected override void DetermineSemiTableAlgorithm(Plan APlan)
		{
			FSemiTableAlgorithm = typeof(SearchedWithoutTable);

			// ensure that the right side is a searchable node
			Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, RightKey);
		}
	}
	
	// operator iJoin(table{}, table{}, bool) : table{}	
	public class JoinNode : ConditionedTableNode
	{		
		protected virtual void DetermineLeftColumns(Plan APlan, bool AIsNilable)
		{
			CopyTableVarColumns(LeftTableVar.Columns, AIsNilable);
			
			Schema.TableVarColumn LNewColumn;
			foreach (Schema.TableVarColumn LColumn in LeftKey.Columns)
			{
				LNewColumn = TableVar.Columns[LColumn];
				LNewColumn.IsChangeRemotable = false;
				LNewColumn.IsDefaultRemotable = false;
			}
		}
		
		protected virtual void DetermineRightColumns(Plan APlan, bool AIsNilable)
		{
			Schema.TableVarColumn LNewColumn;

			if (!FIsNatural)
				CopyTableVarColumns(RightTableVar.Columns, AIsNilable);
			else
			{
				foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
				{
					if (!FRightKey.Columns.ContainsName(LColumn.Name))
					{
						LNewColumn = CopyTableVarColumn(LColumn);
						LNewColumn.IsNilable = LNewColumn.IsNilable || AIsNilable;
						DataType.Columns.Add(LNewColumn.Column);
						TableVar.Columns.Add(LNewColumn);
					}
					else
					{	
						Schema.TableVarColumn LLeftColumn = TableVar.Columns[LColumn];
						LLeftColumn.IsDefaultRemotable = LLeftColumn.IsDefaultRemotable && LColumn.IsDefaultRemotable;
						LLeftColumn.IsChangeRemotable = LLeftColumn.IsChangeRemotable && LColumn.IsChangeRemotable;
						LLeftColumn.IsValidateRemotable = LLeftColumn.IsValidateRemotable && LColumn.IsValidateRemotable;
						LLeftColumn.IsNilable = LLeftColumn.IsNilable || LColumn.IsNilable || AIsNilable;
						LLeftColumn.JoinInheritMetaData(LColumn.MetaData);
					}
				}
			}

			foreach (Schema.TableVarColumn LColumn in LeftKey.Columns)
			{
				LNewColumn = TableVar.Columns[LColumn];
				LNewColumn.IsChangeRemotable = false;
				LNewColumn.IsDefaultRemotable = false;
			}
		}
		
		protected JoinCardinality FCardinality;
		public JoinCardinality Cardinality { get { return FCardinality; } set { FCardinality = value; } }
		
		protected virtual void DetermineCardinality(Plan APlan)
		{
			if (FLeftKey.IsUnique)
				if (FRightKey.IsUnique)
					FCardinality = JoinCardinality.OneToOne;
				else
					FCardinality = JoinCardinality.OneToMany;
			else
				if (FRightKey.IsUnique)
					FCardinality = JoinCardinality.ManyToOne;
				else
					FCardinality = JoinCardinality.ManyToMany;
		}
		
		protected Type FJoinAlgorithm;
		public Type JoinAlgorithm { get { return FJoinAlgorithm; } }
		
		protected override void WritePlanAttributes(System.Xml.XmlWriter AWriter)
		{
			base.WritePlanAttributes(AWriter);
			AWriter.WriteAttributeString("Cardinality", FCardinality.ToString());
			if (FJoinAlgorithm != null)
				AWriter.WriteAttributeString("Algorithm", FJoinAlgorithm.ToString());
			AWriter.WriteAttributeString("Condition", Nodes[2].SafeEmitStatementAsString());
		}
		
		protected bool JoinOrdersAscendingCompatible(Schema.Order ALeftOrder, Schema.Order ARightOrder)
		{
			for (int LIndex = 0; LIndex < FLeftKey.Columns.Count; LIndex++)
				if (ALeftOrder.Columns[LIndex].Ascending != ARightOrder.Columns[LIndex].Ascending)
					return false;
			return true;
		}
		
		protected virtual void DetermineJoinAlgorithm(Plan APlan)
		{
			if (FExpression == null)
				FJoinAlgorithm = typeof(TimesTable);
			else
			{
				FJoinOrder = new Schema.Order(FLeftKey, APlan);
				Schema.OrderColumn LColumn;
				for (int LIndex = 0; LIndex < FJoinOrder.Columns.Count; LIndex++)
				{
					LColumn = FJoinOrder.Columns[LIndex];
					LColumn.Sort = Compiler.GetSort(APlan, LColumn.Column.DataType);
					if (LColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LColumn.Sort.Dependencies);
					LColumn.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to one side
				if (!IsJoinOrder(APlan, FLeftKey, LeftNode) && !IsJoinOrder(APlan, FRightKey, RightNode))
				{
					// if the right side is unique, order it
					// else if the left side is unique, order it
					// else order the right side (should be whichever side has the least cardinality)
					if (FRightKey.IsUnique)
						Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, FRightKey);
					else if (FLeftKey.IsUnique)
						Nodes[0] = Compiler.EnsureSearchableNode(APlan, LeftNode, FLeftKey);
					else
						Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, FRightKey);
				}

				if (IsJoinOrder(APlan, FLeftKey, LeftNode) && IsJoinOrder(APlan, FRightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (FLeftKey.IsUnique || FRightKey.IsUnique))
				{
					// if both inputs are ordered by join keys and one or both join keys are unique
						// use a merge join algorithm
					if (FLeftKey.IsUnique && FRightKey.IsUnique)
					{
						FJoinAlgorithm = typeof(UniqueMergeJoinTable);
						Order = CopyOrder(LeftNode.Order);
					}
					else if (FLeftKey.IsUnique)
					{
						FJoinAlgorithm = typeof(LeftUniqueMergeJoinTable);
						Order = CopyOrder(RightNode.Order);
					}
					else
					{
						FJoinAlgorithm = typeof(RightUniqueMergeJoinTable);
						Order = CopyOrder(LeftNode.Order);
					}
				}
				else if (IsJoinOrder(APlan, FLeftKey, LeftNode))
				{
					// else if one input is ordered by join keys
						// use a searched join algorithm
					if (FLeftKey.IsUnique)
						FJoinAlgorithm = typeof(LeftUniqueSearchedJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueLeftSearchedJoinTable);

					if (RightNode.Order != null)
						Order = CopyOrder(RightNode.Order);
				}
				else if (IsJoinOrder(APlan, FRightKey, RightNode))
				{
					if (FRightKey.IsUnique)
						FJoinAlgorithm = typeof(RightUniqueSearchedJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueRightSearchedJoinTable);
						
					if (LeftNode.Order != null)
						Order = CopyOrder(LeftNode.Order);
				}
				else
				{
					// NOTE - this code is not being hit right now because all joins are ordered by the code above
					// use a nested loop join
					// note that if both left and right sides are unique this selection is arbitrary (possible cost based optimization)
					if (FLeftKey.IsUnique)
						FJoinAlgorithm = typeof(LeftUniqueNestedLoopJoinTable);
					else if (FRightKey.IsUnique)
						FJoinAlgorithm = typeof(RightUniqueNestedLoopJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueNestedLoopJoinTable);
				}
			}
		}
		
		protected virtual void JoinLeftApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			Row LLeftRow = new Row(AProcess, LeftNode.DataType.RowType);
			try
			{
				ARow.CopyTo(LLeftRow);
				LeftNode.JoinApplicationTransaction(AProcess, LLeftRow);
			}
			finally
			{
				LLeftRow.Dispose();
			}
		}
		
		protected virtual void JoinRightApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			Row LRightRow = new Row(AProcess, RightNode.DataType.RowType);
			try
			{
				ARow.CopyTo(LRightRow);
				RightNode.JoinApplicationTransaction(AProcess, LRightRow);
			}
			finally
			{
				LRightRow.Dispose();
			}
		}
		
		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			JoinLeftApplicationTransaction(AProcess, ARow);
			if (!FIsLookup || FIsDetailLookup)
				JoinRightApplicationTransaction(AProcess, ARow);
		}
		
		protected virtual void DetermineDetailLookup(Plan APlan)
		{
			if ((Modifiers != null) && (Modifiers.Contains("IsDetailLookup")))
				FIsDetailLookup = Boolean.Parse(Modifiers["IsDetailLookup"].Value);
			else
			{
				FIsDetailLookup = false;
				
				// If this is a lookup and the left join columns are a non-trivial proper superset of any key of the left side, this is a detail lookup
				if (FIsLookup)
					foreach (Schema.Key LKey in LeftTableVar.Keys)
						if ((LKey.Columns.Count > 0) && FLeftKey.Columns.IsProperSupersetOf(LKey.Columns))
						{
							FIsDetailLookup = true;
							break;
						}
				
				#if USEDETAILLOOKUP
				// If this is a lookup, the left key is not unique, and any key of the left table is a subset of the left key, this is a detail lookup
				if (FIsLookup && !FLeftKey.IsUnique)
					foreach (Schema.Key LKey in LeftTableVar.Keys)
						if (LKey.Columns.IsSubsetOf(FLeftKey.Columns))
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
					Schema.Key LInvariant = LeftNode.TableVar.CalculateInvariant();
					foreach (Schema.Column LColumn in FLeftKey.Columns)
						if (LInvariant.Columns.Contains(LColumn.Name))
						{
							FIsDetailLookup = true;
							break;
						}
				}
				#endif
			}
		}
		
		protected virtual void DetermineColumns(Plan APlan)
		{
			DetermineLeftColumns(APlan, false);
			DetermineRightColumns(APlan, false);
		}
		
		protected bool FUpdateLeftToRight;
		public bool UpdateLeftToRight
		{
			get { return FUpdateLeftToRight; }
			set { FUpdateLeftToRight = value; }
		}
		
		protected bool FIsLookup;
		public bool IsLookup
		{
			get { return FIsLookup; }
			set { FIsLookup = value; }
		}
		
		protected bool FIsDetailLookup;
		public bool IsDetailLookup
		{
			get { return FIsDetailLookup; }
			set { FIsDetailLookup = value; }
		}
		
		protected bool FIsIntersect;
		public bool IsIntersect
		{
			get { return FIsIntersect; }
			set { FIsIntersect = value; }
		}
		
		protected bool FIsTimes;
		public bool IsTimes
		{
			get { return FIsTimes; }
			set { FIsTimes = value; }
		}
		
		protected virtual void DetermineManyToManyKeys(Plan APlan)
		{
			// This algorithm is also used to infer keys for any cardinality changing outer (one-to-many or many-to-many)
			// If neither join key is unique
				// for each key of the left input
					// for each key of the right input
						// create a key based on all columns of the left key, excluding join columns that are not key columns of the right input,
							// extended with all columns of the right key, excluding join columns that are not key columns of the left input or are the correlated column of an existing column in the key
						// create a key based on all columns of the right key, excluding join columns that are not key columns of the left input,
							// extended with all columns of the left key, excluding join columns that are not key columns of the right input or are the correlated column of an existing column in the key
			Schema.Key LKey;
			foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					LKey = new Schema.Key();
					LKey.IsInherited = true;
					LKey.IsSparse = LLeftKey.IsSparse || LRightKey.IsSparse;
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
					{
						int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
						if ((LLeftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name)))
							LKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						Schema.TableVarColumn LTableVarColumn = TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)];
						if 
						(
							(
								(LRightKeyIndex < 0) || 
								(
									LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name) && 
									!LKey.Columns.ContainsName(LeftKey.Columns[LRightKeyIndex].Name)
								)
							) && 
							!LKey.Columns.Contains(LTableVarColumn)
						)
							LKey.Columns.Add(LTableVarColumn);
					}

					if (!TableVar.Keys.Contains(LKey))
						TableVar.Keys.Add(LKey);
						
					LKey = new Schema.Key();
					LKey.IsInherited = true;
					LKey.IsSparse = LLeftKey.IsSparse || LRightKey.IsSparse;
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						if ((LRightKeyIndex < 0) || (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name)))
							LKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}
						
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
					{
						int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
						Schema.TableVarColumn LTableVarColumn = TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)];
						if
						(
							(
								(LLeftKeyIndex < 0) ||
								(
									RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name) &&
									!LKey.Columns.ContainsName(RightKey.Columns[LLeftKeyIndex].Name)
								)
							) &&
							!LKey.Columns.Contains(LTableVarColumn)
						)
							LKey.Columns.Add(LTableVarColumn);
					}
					
					if (!TableVar.Keys.Contains(LKey))
						TableVar.Keys.Add(LKey);
				}
				
			RemoveSuperKeys();
		}
		
		#if USEUNIFIEDJOINKEYINFERENCE
		// BTR -> The USEUNIFIEDJOINKEYINFERENCE define is introduced to make reversion easier if it becomes necessary, and to allow for easy reference of
		// the way the old algorithms worked. I am confident that this is now the correct algorithm for all join types.
		protected void InferJoinKeys(Plan APlan, JoinCardinality ACardinality, bool AIsLeftOuter, bool AIsRightOuter)
		{
			// foreach key of the left table var
				// foreach key of the right table var
					// copy the left key, adding all the columns of the right key that are not join columns
						// if the left key is sparse, or the join is a cardinality preserving right outer, the resulting key is sparse
					// copy the right key, adding all the columns of the left key that are not join columns
						// if the right key is sparse, or the join is a cardinality preserving left outer, the resulting key is sparse
			Schema.Key LNewKey;
			foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse = 
						LLeftKey.IsSparse 
							|| (AIsRightOuter && !IsNatural && ((ACardinality == JoinCardinality.OneToOne) || (ACardinality == JoinCardinality.OneToMany)));
							
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						if (!RightKey.Columns.ContainsName(LColumn.Name) && !LNewKey.Columns.ContainsName(LColumn.Name)) // if this is not a right join column, and it is not already part of the key
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
							
					if (LNewKey.Columns.Count == 0)
						LNewKey.IsSparse = false;
							
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);
							
					LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse =
						LRightKey.IsSparse
							|| (AIsLeftOuter && !IsNatural && ((ACardinality == JoinCardinality.OneToOne) || (ACardinality == JoinCardinality.ManyToOne)));
							
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						if (!LeftKey.Columns.ContainsName(LColumn.Name) ) // if this is not a left join column
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);

					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						if (!LNewKey.Columns.ContainsName(LColumn.Name)) // if this column is not already part of the key
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
							
					if (LNewKey.Columns.Count == 0)
						LNewKey.IsSparse = false;
						
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);
				}
				
			RemoveSuperKeys();
		}
		#endif
		
		protected virtual void DetermineJoinKey(Plan APlan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(APlan, Cardinality, false, false);
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
				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					Schema.Key LNewKey = CopyKey(LLeftKey);
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LLeftKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						{
							int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
							if (LLeftKeyIndex >= 0)
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}

						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
					}
				}
				
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					Schema.Key LNewKey = CopyKey(LRightKey);
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LRightKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						{
							int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
							if (LRightKeyIndex >= 0)
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
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
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse = LRightKey.IsSparse;
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						if ((LRightKeyIndex < 0) || LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}
					
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LRightKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						{
							int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
							if (LRightKeyIndex >= 0)
							{
								if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name))
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
							}
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
					}
				}
			}
			else if (FRightKey.IsUnique)
			{
				// If only the right join key is unique,
					// for each key of the left table var
						// copy the key, excluding join columns that are not key columns in the right
						// copy the key, excluding join columns that are not key columns in the right, with all correlated columns replaced with the right join key columns (a no-op for natural joins)
				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse = LLeftKey.IsSparse;
					
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
					{
						int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
						if ((LLeftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name)))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LLeftKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						{
							int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
							if (LLeftKeyIndex >= 0)
							{
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name))
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
							}
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
					}
				}
			}
			else
			{
				DetermineManyToManyKeys(APlan);
			}
			#endif
		}
		
		protected virtual void DetermineOrders(Plan APlan)
		{
			if (FLeftKey.IsUnique && FRightKey.IsUnique)
			{
				CopyPreservedOrders(LeftTableVar.Orders);
				CopyPreservedOrders(RightTableVar.Orders);
			}
			else if (FLeftKey.IsUnique)
				CopyPreservedOrders(RightTableVar.Orders);
			else if (FRightKey.IsUnique)
				CopyPreservedOrders(LeftTableVar.Orders);
		}
		
		protected override void DetermineModifiers(Plan APlan)
		{
			base.DetermineModifiers(APlan);
			
			if (Modifiers != null)
				IsTimes = Boolean.Parse(LanguageModifiers.GetModifier(Modifiers, "IsTimes", IsTimes.ToString()));
		}
		
		protected virtual void DetermineJoinModifiers(Plan APlan)
		{
			bool LIsLeft = !(this is RightOuterJoinNode);
			if (FIsLookup)
			{
				if (LIsLeft)
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
					if (LIsLeft || !FIsLookup)
						PropagateInsertLeft = PropagateAction.Ignore;
				break;
					
				case JoinCardinality.ManyToOne :
					if (!LIsLeft || !FIsLookup)
						PropagateInsertRight = PropagateAction.Ignore;
				break;
					
				case JoinCardinality.ManyToMany :
					if (LIsLeft)
					{
						PropagateInsertLeft = PropagateAction.Ignore;
						if (!FIsLookup)
							PropagateInsertRight = PropagateAction.Ignore;
					}
					else
					{
						if (!FIsLookup)
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
			bool LIsLeftNilable = (PropagateInsertLeft == PropagateAction.False) || !PropagateUpdateLeft;
			bool LIsRightNilable = (PropagateInsertRight == PropagateAction.False) || !PropagateUpdateRight;
			
			int LRightColumnIndex = RightTableVar.Columns.Count == 0 ? LeftTableVar.Columns.Count : TableVar.Columns.IndexOfName(RightTableVar.Columns[0].Name);
			
			for (int LIndex = 0; LIndex < TableVar.Columns.Count; LIndex++)
			{
				if (LIndex < LRightColumnIndex)
				{
					if (LIsLeftNilable)
						TableVar.Columns[LIndex].IsNilable = LIsLeftNilable;
				}
				else
				{
					if (LIsRightNilable)
						TableVar.Columns[LIndex].IsNilable = LIsRightNilable;
				}
			}
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			InternalCreateDataType(APlan);

			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(LeftTableVar.MetaData);
			FTableVar.JoinInheritMetaData(RightTableVar.MetaData);
			
			if (FIsNatural)
			{
				if (FIsIntersect && (!RightTableType.Compatible(LeftTableType)))
					throw new CompilerException(CompilerException.Codes.TableExpressionsNotCompatible, APlan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString());
					
				FExpression = GenerateJoinExpression();
				
				if (FIsTimes)
				{
					if (Modifiers == null)
						Modifiers = new LanguageModifiers();
					if (!Modifiers.Contains("IsTimes"))
						Modifiers.Add(new LanguageModifier("IsTimes", "true"));
				}
				
				if (FIsTimes && (FExpression != null))
					throw new CompilerException(CompilerException.Codes.TableExpressionsNotProductCompatible, APlan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString());

				if (!FIsTimes && (FExpression == null) && !APlan.ServerProcess.SuppressWarnings)
					APlan.Messages.Add(new CompilerException(CompilerException.Codes.PossiblyIncorrectProductExpression, CompilerErrorLevel.Warning, APlan.CurrentStatement(), LeftTableType.ToString(), RightTableType.ToString()));
			}
			else
			{
				if (!IsEquiJoin(FExpression))
					throw new CompilerException(CompilerException.Codes.JoinMustBeEquiJoin, FExpression);
					
				DetermineJoinKeys(FExpression);
			}
			
			foreach (Schema.Key LKey in LeftTableVar.Keys)
				if (FLeftKey.Columns.IsSupersetOf(LKey.Columns))
				{
					FLeftKey.IsUnique = true;
					break;
				}
				
			foreach (Schema.Key LKey in RightTableVar.Keys)
				if (FRightKey.Columns.IsSupersetOf(LKey.Columns))
				{
					FRightKey.IsUnique = true;
					break;
				}
				
			DetermineCardinality(APlan);
			DetermineDetailLookup(APlan);
				
			// Determine columns
			DetermineColumns(APlan);				
			
			// Determine the keys
			DetermineJoinKey(APlan);
			
			FUpdateLeftToRight = (FLeftKey.IsUnique && FRightKey.IsUnique) || (!FLeftKey.IsUnique && !FRightKey.IsUnique) || FLeftKey.IsUnique;

			// Determine modifiers
			DetermineJoinModifiers(APlan);
			
			// DetermineRemotable
			DetermineRemotable(APlan);

			// Determine orders			
			DetermineOrders(APlan);

			#if UseReferenceDerivation
			foreach (Schema.Reference LReference in LeftTableVar.SourceReferences)
				CopySourceReference(APlan, LReference, LReference.IsExcluded || RightTableVar.TargetReferences.ContainsOriginatingReference(LReference.OriginatingReferenceName()) && LeftKey.Columns.Equals(LReference.SourceKey.Columns));
					
			foreach (Schema.Reference LReference in LeftTableVar.TargetReferences)
				CopyTargetReference(APlan, LReference, LReference.IsExcluded || RightTableVar.SourceReferences.ContainsOriginatingReference(LReference.OriginatingReferenceName()) && LeftKey.Columns.Equals(LReference.TargetKey.Columns));
					
			foreach (Schema.Reference LReference in RightTableVar.SourceReferences)
				CopySourceReference(APlan, LReference, LReference.IsExcluded || LeftTableVar.TargetReferences.ContainsOriginatingReference(LReference.OriginatingReferenceName()) && RightKey.Columns.Equals(LReference.SourceKey.Columns));
			
			foreach (Schema.Reference LReference in RightTableVar.TargetReferences)
				CopyTargetReference(APlan, LReference, LReference.IsExcluded || LeftTableVar.SourceReferences.ContainsOriginatingReference(LReference.OriginatingReferenceName()) && RightKey.Columns.Equals(LReference.TargetKey.Columns));
			#endif
			
			APlan.EnterRowContext();
			try
			{			
				APlan.Symbols.Push(new DataVar(new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					try
					{
						if (FExpression != null)
						{
							PlanNode LPlanNode = Compiler.CompileExpression(APlan, FExpression);
							if (!(LPlanNode.DataType.Is(APlan.Catalog.DataTypes.SystemBoolean)))
								throw new CompilerException(CompilerException.Codes.BooleanExpressionExpected, FExpression);
							Nodes.Add(LPlanNode);
						}
						else
							Nodes.Add(new ValueNode(new Scalar(APlan.ServerProcess, APlan.ServerProcess.DataTypes.SystemBoolean, true)));
						
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			if (IsLookup && !IsDetailLookup)
				APlan.PushCursorContext(new CursorContext(APlan.CursorContext.CursorType, APlan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, APlan.CursorContext.CursorIsolation));
			try
			{
				Nodes[1].DetermineBinding(APlan);
			}
			finally
			{
				if (IsLookup && !IsDetailLookup)
					APlan.PopCursorContext();
			}
			
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new DataVar(new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					try
					{
						Nodes[2].DetermineBinding(APlan);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public override void DetermineDevice(Plan APlan)
		{
			base.DetermineDevice(APlan);
			if (!FDeviceSupported)
				DetermineJoinAlgorithm(APlan);
		}
		
		public virtual void DetermineCursorCapabilities(Plan APlan)
		{
			FCursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					APlan.CursorContext.CursorCapabilities &
					LeftNode.CursorCapabilities &
					((IsLookup && !IsDetailLookup) ? CursorCapability.Updateable : RightNode.CursorCapabilities) & 
					CursorCapability.Updateable
				);
		}

		public override void DetermineCursorBehavior(Plan APlan)
		{
			if ((LeftNode.CursorType == CursorType.Dynamic) || (RightNode.CursorType == CursorType.Dynamic))
				FCursorType = CursorType.Dynamic;
			else
				FCursorType = CursorType.Static;
			FRequestedCursorType = APlan.CursorContext.CursorType;

			DetermineCursorCapabilities(APlan);

			FCursorIsolation = APlan.CursorContext.CursorIsolation;
		}
		
		protected PlanNode FLeftSelectNode;
		public PlanNode LeftSelectNode
		{
			get { return FLeftSelectNode; }
			set { FLeftSelectNode = value; }
		}
		
		protected PlanNode FRightSelectNode;
		public PlanNode RightSelectNode
		{
			get { return FRightSelectNode; }
			set { FRightSelectNode = value; }
		}
		
		protected Row SelectLeftRow(ServerProcess AProcess)
		{
			EnsureLeftSelectNode(AProcess.Plan);
			DataVar LObject = FLeftSelectNode.Execute(AProcess);
			using (Table LTable = (Table)LObject.Value)
			{
				LTable.Open();
				if (LTable.Next())
					return LTable.Select();
				else
					return null;
			}
		}
		
		protected bool SelectLeftRow(ServerProcess AProcess, Row ARow)
		{
			EnsureLeftSelectNode(AProcess.Plan);
			DataVar LObject = FLeftSelectNode.Execute(AProcess);
			using (Table LTable = (Table)LObject.Value)
			{
				LTable.Open();
				if (LTable.Next())
				{
					LTable.Select(ARow);
					return true;
				}
				return false;
			}
		}
		
		protected Row SelectRightRow(ServerProcess AProcess)
		{
			EnsureRightSelectNode(AProcess.Plan);
			DataVar LObject = FRightSelectNode.Execute(AProcess);
			using (Table LTable = (Table)LObject.Value)
			{
				LTable.Open();
				if (LTable.Next())
					return LTable.Select();
				else
					return null;
			}
		}
		
		protected bool SelectRightRow(ServerProcess AProcess, Row ARow)
		{
			EnsureRightSelectNode(AProcess.Plan);
			DataVar LObject = FRightSelectNode.Execute(AProcess);
			using (Table LTable = (Table)LObject.Value)
			{
				LTable.Open();
				if (LTable.Next())
				{
					LTable.Select(ARow);
					return true;
				}
				return false;
			}
		}
		
		protected void EnsureLeftSelectNode(Plan APlan)
		{
			if (FLeftSelectNode == null)
			{
				ApplicationTransaction LTransaction = null;
				if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.ServerProcess.GetApplicationTransaction();
				try
				{
					if ((LTransaction != null) && (!ShouldTranslateLeft || (IsLookup && !IsDetailLookup)))
						LTransaction.PushLookup();
					try
					{
						APlan.PushATCreationContext();
						try
						{
							APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(APlan, FSymbols);
								try
								{
									APlan.EnterRowContext();
									try
									{
										APlan.Symbols.Push(new DataVar(DataType.NewRowType));
										try
										{
											FLeftSelectNode =
												Compiler.Bind
												(
													APlan,
													Compiler.EmitRestrictNode
													(
														APlan,
														Compiler.CompileExpression(APlan, (Expression)LeftNode.EmitStatement(EmitMode.ForCopy)),
														Compiler.BuildKeyEqualExpression
														(
															APlan,
															new Schema.RowType(FLeftKey.Columns).Columns,
															new Schema.RowType(FRightKey.Columns, Keywords.New).Columns
														)
													)
												);
										}
										finally
										{
											APlan.Symbols.Pop();
										}
									}
									finally
									{
										APlan.ExitRowContext();
									}
								}
								finally
								{
									PopSymbols(APlan, FSymbols);
								}
							}
							finally
							{
								APlan.PopCursorContext();
							}
						}
						finally
						{
							APlan.PopATCreationContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopLookup();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
		
		protected void EnsureRightSelectNode(Plan APlan)
		{
			if (FRightSelectNode == null)
			{
				ApplicationTransaction LTransaction = null;
				if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
					LTransaction = APlan.ServerProcess.GetApplicationTransaction();
				try
				{
					if ((LTransaction != null) && (!ShouldTranslateRight || (IsLookup && !IsDetailLookup)))
						LTransaction.PushLookup();
					try
					{
						APlan.PushATCreationContext();
						try
						{
							APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.Isolated));
							try
							{
								PushSymbols(APlan, FSymbols);
								try
								{
									APlan.EnterRowContext();
									try
									{
										APlan.Symbols.Push(new DataVar(DataType.NewRowType));
										try
										{
											FRightSelectNode =
												Compiler.Bind
												(	
													APlan,
													Compiler.EmitRestrictNode
													(
														APlan,
														Compiler.CompileExpression(APlan, (Expression)RightNode.EmitStatement(EmitMode.ForCopy)),
														Compiler.BuildKeyEqualExpression
														(
															APlan,
															new Schema.RowType(FRightKey.Columns).Columns,
															new Schema.RowType(FLeftKey.Columns, Keywords.New).Columns
														)
													)
												);
										}
										finally
										{
											APlan.Symbols.Pop();
										}
									}
									finally
									{
										APlan.ExitRowContext();
									}
								}
								finally
								{
									PopSymbols(APlan, FSymbols);
								}
							}
							finally
							{
								APlan.PopCursorContext();
							}
						}
						finally
						{
							APlan.PopATCreationContext();
						}
					}
					finally
					{
						if (LTransaction != null)
							LTransaction.PopLookup();
					}
				}
				finally
				{
					if (LTransaction != null)
						Monitor.Exit(LTransaction);
				}
			}
		}
		
		private bool FCoordinateLeft = true;
		public bool CoordinateLeft
		{
			get { return FCoordinateLeft; }
			set { FCoordinateLeft = value; }
		}
		
		private bool FCoordinateRight = true;
		public bool CoordinateRight
		{
			get { return FCoordinateRight; }
			set { FCoordinateRight = value; }
		}
		
		private bool FRetrieveLeft = false;
		public bool RetrieveLeft
		{
			get { return FRetrieveLeft; }
			set { FRetrieveLeft = value; }
		}
		
		private bool FRetrieveRight = false;
		public bool RetrieveRight
		{
			get { return FRetrieveRight; }
			set { FRetrieveRight = value; }
		}
		
		private bool FClearLeft = false;
		public bool ClearLeft
		{
			get { return FClearLeft; }
			set { FClearLeft = value; }
		}
		
		private bool FClearRight = false;
		public bool ClearRight
		{
			get { return FClearRight; }
			set { FClearRight = value; }
		}
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			JoinTable LTable = (JoinTable)Activator.CreateInstance(FJoinAlgorithm, System.Reflection.BindingFlags.Default, null, new object[]{this, AProcess}, System.Globalization.CultureInfo.CurrentCulture);
			try
			{
				LTable.Open();
				return new DataVar(String.Empty, FDataType, LTable);
			}
			catch
			{
				LTable.Dispose();
				throw;
			}
		}
		
		public override void DetermineRemotable(Plan APlan)
		{
			base.DetermineRemotable(APlan);
			
			FTableVar.ShouldValidate = FTableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate || (EnforcePredicate && !FIsNatural);
			FTableVar.ShouldDefault = FTableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			FTableVar.ShouldChange = FTableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange;
			
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				int LColumnIndex;
				Schema.TableVarColumn LSourceColumn;
				
				LColumnIndex = LeftTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = LeftTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = FLeftKey.Columns.ContainsName(LColumn.Name) || LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}

				LColumnIndex = RightTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = RightTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = FRightKey.Columns.ContainsName(LColumn.Name) || LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}
			}
		}
		
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (EnforcePredicate && !FIsNatural && (AColumnName == String.Empty))
			{
				Row LLeftRow = new Row(AProcess, LeftTableType.RowType);
				try
				{
					ANewRow.CopyTo(LLeftRow);
					AProcess.Context.Push(new DataVar(String.Empty, LLeftRow.DataType, LLeftRow));
					try
					{
						Row LRightRow = new Row(AProcess, RightTableType.RowType);
						try
						{
							ANewRow.CopyTo(LRightRow);
							AProcess.Context.Push(new DataVar(String.Empty, LRightRow.DataType, LRightRow));
							try
							{
								DataVar LObject = Nodes[2].Execute(AProcess);
								if ((LObject.Value != null) && !LObject.Value.IsNil && !LObject.Value.AsBoolean)
									throw new RuntimeException(RuntimeException.Codes.RowViolatesJoinPredicate);
							}
							finally
							{
								AProcess.Context.Pop();
							}
						}
						finally
						{
							LRightRow.Dispose();
						}
					}
					finally
					{
						AProcess.Context.Pop();
					}
				}
				finally
				{
					LLeftRow.Dispose();
				}
			}

			if (AIsDescending)
			{
				bool LChanged = false;
				if (PropagateValidateLeft && ((AColumnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(AColumnName))))
					LChanged = LeftNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
				if (PropagateValidateRight && ((AColumnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(AColumnName))))
					LChanged = RightNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
				return LChanged;
			}
			return false;
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			if (AIsDescending)
			{
				bool LChanged = false;

				if (PropagateDefaultLeft && ((AColumnName == String.Empty) || LeftTableType.Columns.ContainsName(AColumnName)))
				{
					BitArray LValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(LeftKey.Columns[LIndex].Name);
					LChanged = LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(LeftKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, LeftKey.Columns[LIndex].Name);
				}

				if (PropagateDefaultRight && ((AColumnName == String.Empty) || RightTableType.Columns.ContainsName(AColumnName)))
				{
					BitArray LValueFlags = new BitArray(RightKey.Columns.Count);
					for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(RightKey.Columns[LIndex].Name);
					LChanged = RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(RightKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, RightKey.Columns[LIndex].Name);
				}

				return LChanged;
			}
			return false;
		}
		
		///<summary>Coordinates the value of the left join-key column with the value of the right join-key column given by AColumnName.</summary>		
		protected void CoordinateLeftJoinKey(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			int LRightIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
			int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(FLeftKey.Columns[FRightKey.Columns.IndexOfName(AColumnName)].Name);
			if (ANewRow.HasValue(LRightIndex))
				ANewRow[LLeftIndex] = ANewRow[LRightIndex];
			else
				ANewRow.ClearValue(LLeftIndex);
				
			if (PropagateChangeLeft)
				LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LLeftIndex].Name);
		}

		///<summary>Coordinates the value of the right join-key column with the value of the left join-key column given by AColumnName.</summary>		
		protected void CoordinateRightJoinKey(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
			int LRightIndex = ANewRow.DataType.Columns.IndexOfName(FRightKey.Columns[FLeftKey.Columns.IndexOfName(AColumnName)].Name);
			if (ANewRow.HasValue(LLeftIndex))
				ANewRow[LRightIndex] = ANewRow[LLeftIndex];
			else
				ANewRow.ClearValue(LRightIndex);

			if (PropagateChangeRight)
				RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LRightIndex].Name);
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
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			PushNewRow(AProcess, ANewRow);
			try
			{
				bool LChanged = false;
				bool LChangePropagated = false;
				
				if (AColumnName != String.Empty)
				{
					if (LeftKey.Columns.ContainsName(AColumnName))
					{
						if (!AProcess.ServerSession.Server.IsRepository && RetrieveRight)
						{
							LChanged = true;
							if (IsNatural)
								LChangePropagated = true;
							if (SelectRightRow(AProcess, ANewRow))
							{
								foreach (Schema.Column LColumn in RightTableType.Columns)
									if (PropagateChangeRight)
										RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
							}
							else
							{
								foreach (Schema.Column LColumn in RightTableType.Columns)
								{
									if (RightKey.Columns.ContainsName(LColumn.Name) && CoordinateRight)
									{
										int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(FLeftKey.Columns[FRightKey.Columns.IndexOfName(LColumn.Name)].Name);
										int LRightIndex = ANewRow.DataType.Columns.IndexOfName(LColumn.Name);
										if (ANewRow.HasValue(LLeftIndex))
											ANewRow[LRightIndex] = ANewRow[LLeftIndex];
										else
											ANewRow.ClearValue(LRightIndex);
									}
									else
									{
										if (ClearRight)
											ANewRow.ClearValue(LColumn.Name);

										if (!ANewRow.HasValue(LColumn.Name) && PropagateDefaultRight)
											RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}

									if (PropagateChangeRight)
										RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
							}
						}
						else
						{
							if (CoordinateRight)
							{
								LChanged = true;
								int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
								int LRightIndex = ANewRow.DataType.Columns.IndexOfName(FRightKey.Columns[FLeftKey.Columns.IndexOfName(AColumnName)].Name);
								if (ANewRow.HasValue(LLeftIndex))
									ANewRow[LRightIndex] = ANewRow[LLeftIndex];
								else
									ANewRow.ClearValue(LRightIndex);

								if (PropagateChangeRight)
									RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LRightIndex].Name);
							}
						}
					}
					else if (LeftTableVar.Columns.ContainsName(AColumnName))
					{
						if (PropagateChangeLeft)
						{
							LChangePropagated = true;
							LChanged = LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
						}
					}
					else if (RightKey.Columns.ContainsName(AColumnName))
					{
						if (!AProcess.ServerSession.Server.IsRepository && RetrieveLeft)
						{
							LChanged = true;
							if (IsNatural)
								LChangePropagated = true;
							if (SelectLeftRow(AProcess, ANewRow))
							{
								foreach (Schema.Column LColumn in LeftTableType.Columns)
									if (PropagateChangeLeft)
										LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
							}
							else
							{
								foreach (Schema.Column LColumn in LeftTableType.Columns)
								{
									if (LeftKey.Columns.ContainsName(LColumn.Name) && CoordinateLeft)
									{
										int LRightIndex = ANewRow.DataType.Columns.IndexOfName(FRightKey.Columns[FLeftKey.Columns.IndexOfName(LColumn.Name)].Name);
										int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(LColumn.Name);
										if (ANewRow.HasValue(LRightIndex))
											ANewRow[LLeftIndex] = ANewRow[LRightIndex];
										else
											ANewRow.ClearValue(LLeftIndex);
									}
									else
									{
										if (ClearLeft)
											ANewRow.ClearValue(LColumn.Name);

										if (!ANewRow.HasValue(LColumn.Name) && PropagateDefaultLeft)
											LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}

									if (PropagateChangeLeft)
										LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
							}
						}
						else
						{
							if (CoordinateLeft)
							{
								LChanged = true;
								int LRightIndex = ANewRow.DataType.Columns.IndexOfName(AColumnName);
								int LLeftIndex = ANewRow.DataType.Columns.IndexOfName(FLeftKey.Columns[FRightKey.Columns.IndexOfName(AColumnName)].Name);
								if (ANewRow.HasValue(LRightIndex))
									ANewRow[LLeftIndex] = ANewRow[LRightIndex];
								else
									ANewRow.ClearValue(LLeftIndex);

								if (PropagateChangeLeft)
									LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LLeftIndex].Name);
							}
						}
					}
					else if (RightTableVar.Columns.ContainsName(AColumnName))
					{
						if (PropagateChangeRight)
						{
							LChangePropagated = true;
							LChanged = RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
						}
					}
				}

				if (!LChangePropagated && PropagateChangeLeft && ((AColumnName == String.Empty) || LeftTableVar.Columns.ContainsName(AColumnName)))
					LChanged = LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
						
				if (!LChangePropagated && PropagateChangeRight && ((AColumnName == String.Empty) || RightTableVar.Columns.ContainsName(AColumnName)))
					LChanged = RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;

				return LChanged;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		private void InternalExecuteInsertLeft(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;

				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LLeftRow = LeftNode.Select(AProcess, ANewRow))
					{
						if (LLeftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(AProcess, LLeftRow, ANewRow, AValueFlags, false, AUnchecked);
						}
						else
							LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				break;
			}
		}
		
		private void InternalExecuteInsertRight(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True : 
					RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LRightRow = RightNode.Select(AProcess, ANewRow))
					{
						if (LRightRow != null)
						{
							if (PropagateInsertRight == PropagateAction.Ensure)
								RightNode.Update(AProcess, LRightRow, ANewRow, AValueFlags, false, AUnchecked);
						}
						else
							RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				break;
			}
		}
		
		protected override void InternalExecuteInsert(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				InternalExecuteInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalExecuteInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
			else
			{
				InternalExecuteInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalExecuteInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
		}
		
		protected override void InternalExecuteUpdate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			DataVar LOldRow = new DataVar(String.Empty, AOldRow.DataType, AOldRow);
			if (FUpdateLeftToRight)
			{	
				if (PropagateUpdateLeft)
					LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
					
				if (PropagateUpdateRight)
					RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
			else
			{
				if (PropagateUpdateRight)
					RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);

				if (PropagateUpdateLeft)
					LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
		}
		
		protected override void InternalExecuteDelete(ServerProcess AProcess, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				if (PropagateDeleteRight)
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
				if (PropagateDeleteLeft)
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
			else
			{
				if (PropagateDeleteLeft)
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
				if (PropagateDeleteRight)
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
		}

		public override Statement EmitStatement(EmitMode AMode)
		{
			InnerJoinExpression LExpression = new InnerJoinExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			if (!IsNatural)
				LExpression.Condition = FExpression; //(Expression)Nodes[2].EmitStatement(AMode); // use expression instead of EmitStatement because the statement may be of the form A ?= B = 0;
			LExpression.IsLookup = FIsLookup;
			LExpression.Cardinality = FCardinality;
			//LExpression.IsDetailLookup = FIsDetailLookup;
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
	}
	
	public class OuterJoinNode : JoinNode
	{
        // RowExistsColumnIndex
        protected int FRowExistsColumnIndex = -1;
        public int RowExistsColumnIndex
        {
            get { return FRowExistsColumnIndex; }
        }
        
        protected IncludeColumnExpression FRowExistsColumn;
        public IncludeColumnExpression RowExistsColumn
        {
			get { return FRowExistsColumn; }
			set { FRowExistsColumn = value; }
        }
        
        // AnyOf
        // Together with AllOf, determines what columns must have values to consititute row existence
        // HasRow = if hasrowexists then rowexists else HasAllOf and (AnyOf.Count == 0 or HasAnyOf)
        // Setting this property will have no effect after the DetermineJoinModifiers call
        // By default, this property is set to the set of non-join key  columns
        private string FAnyOf;
        public string AnyOf
        {
			get { return FAnyOf; }
			set { FAnyOf = value; }
		}
		
		// AllOf
		// Together with AnyOf, determines what columns must have values to constitute row existence
		// HasRow = if hasrowexists then rowexists else HasAllOf and HasAnyOf
		// Setting this property will have no effect after the DetermineJoinModifiers call
		// By default, this property is set to the set of  join key columns
		private string FAllOf;
		public string AllOf
		{
			get { return FAllOf; }
			set { FAllOf = value; }
		}
		
		protected Schema.JoinKey FAllOfKey = new Schema.JoinKey();
		public Schema.JoinKey AllOfKey
		{
			get { return FAllOfKey; }
			set { FAllOfKey = value; }
		}
		
		protected Schema.JoinKey FAnyOfKey = new Schema.JoinKey();
		public Schema.JoinKey AnyOfKey
		{
			get { return FAnyOfKey; }
			set { FAnyOfKey = value; }
		}
		
		protected void DetermineKeyColumns(string AColumnNames, Schema.Key AKey)
		{
			if (AColumnNames != String.Empty)
			{
				string[] LColumnNames = AColumnNames.Split(';');
				foreach (string LColumnName in LColumnNames)
					AKey.Columns.Add(TableVar.Columns[LColumnName]);
			}
		}
        
        protected override void InternalCreateDataType(Plan APlan)
        {
			FDataType = new Schema.TableType();
        }
        
		protected bool IsRowExistsColumn(string AColumnName)
		{
			return (FRowExistsColumnIndex >= 0) && (Schema.Object.NamesEqual(TableVar.Columns[FRowExistsColumnIndex].Name, AColumnName));
		}
		
        protected bool HasAllValues(Row ARow, Schema.Key AKey)
        {
			int LColumnIndex;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
				if ((LColumnIndex >= 0) && !ARow.HasValue(LColumnIndex))
					return false;
			}
			return true;
        }
        
        protected bool HasAnyValues(Row ARow, Schema.Key AKey)
        {
			int LColumnIndex;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
				if ((LColumnIndex >= 0) && ARow.HasValue(LColumnIndex))
					return true;
			}
			return false;
        }
        
		protected bool HasRowExistsColumn()
		{
			return FRowExistsColumnIndex >= 0;
		}
		
		protected bool HasValues(Row ARow)
		{
			return HasAllValues(ARow, FAllOfKey) && ((FAnyOfKey.Columns.Count == 0) || HasAnyValues(ARow, FAnyOfKey));
		}
		
        protected bool HasRow(Row ARow)
        {
			if (FRowExistsColumnIndex >= 0)
				return ARow.HasValue(TableVar.Columns[FRowExistsColumnIndex].Name) && ARow[TableVar.Columns[FRowExistsColumnIndex].Name].AsBoolean;
			else
				return HasValues(ARow);
        }
        
        protected bool HasAllJoinValues(Row ARow, Schema.Key AKey)
        {
			int LColumnIndex;
			foreach (Schema.TableVarColumn LColumn in AKey.Columns)
			{
				LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
				if ((LColumnIndex >= 0) && !ARow.HasValue(LColumnIndex))
					return false;
			}
			return true;
        }

		protected bool HasAnyNonJoinRightValues(Row ARow)
		{
			foreach (Schema.Column LColumn in RightTableType.Columns)
			{
				if (!FRightKey.Columns.ContainsName(LColumn.Name) && ARow.HasValue(LColumn.Name))
					return true;
			}
			return false;
		}
        
		protected bool HasAnyNonJoinLeftValues(Row ARow)
		{
			foreach (Schema.Column LColumn in LeftTableType.Columns)
			{
				if (!FLeftKey.Columns.ContainsName(LColumn.Name) && ARow.HasValue(LColumn.Name))
					return true;
			}
			return false;
		}

		public override void DetermineRemotable(Plan APlan)
		{
			Schema.ResultTableVar LTableVar = (Schema.ResultTableVar)TableVar;
			LTableVar.InferredIsDefaultRemotable = (!PropagateDefaultLeft || LeftTableVar.IsDefaultRemotable) && (!PropagateDefaultRight || RightTableVar.IsDefaultRemotable);
			LTableVar.InferredIsChangeRemotable = (!PropagateChangeLeft || LeftTableVar.IsChangeRemotable) && (!PropagateChangeRight || RightTableVar.IsChangeRemotable);
			LTableVar.InferredIsValidateRemotable = (!PropagateValidateLeft || LeftTableVar.IsValidateRemotable) && (!PropagateValidateRight || RightTableVar.IsValidateRemotable);

			FTableVar.DetermineRemotable(APlan.ServerProcess);
			FTableVar.ShouldValidate = FTableVar.ShouldValidate || LeftTableVar.ShouldValidate || RightTableVar.ShouldValidate || (EnforcePredicate && !FIsNatural);
			FTableVar.ShouldDefault = FTableVar.ShouldDefault || LeftTableVar.ShouldDefault || RightTableVar.ShouldDefault;
			FTableVar.ShouldChange = FTableVar.ShouldChange || LeftTableVar.ShouldChange || RightTableVar.ShouldChange || HasRowExistsColumn();
			
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				int LColumnIndex;
				Schema.TableVarColumn LSourceColumn;
				
				if (IsRowExistsColumn(LColumn.Name))
				{
					LColumn.ShouldDefault = true;
					LColumn.ShouldChange = true;
				}
				
				if (FAnyOfKey.Columns.ContainsName(LColumn.Name) || FAllOfKey.Columns.ContainsName(LColumn.Name))
					LColumn.ShouldChange = true;
				
				LColumnIndex = LeftTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = LeftTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = FLeftKey.Columns.ContainsName(LColumn.Name) || LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}

				LColumnIndex = RightTableVar.Columns.IndexOfName(LColumn.Name);
				if (LColumnIndex >= 0)
				{
					LSourceColumn = RightTableVar.Columns[LColumnIndex];
					LColumn.ShouldDefault = LColumn.ShouldDefault || LSourceColumn.ShouldDefault;
					FTableVar.ShouldDefault = FTableVar.ShouldDefault || LColumn.ShouldDefault;
					
					LColumn.ShouldValidate = LColumn.ShouldValidate || LSourceColumn.ShouldValidate;
					FTableVar.ShouldValidate = FTableVar.ShouldValidate || LColumn.ShouldValidate;

					LColumn.ShouldChange = FRightKey.Columns.ContainsName(LColumn.Name) || LColumn.ShouldChange || LSourceColumn.ShouldChange;
					FTableVar.ShouldChange = FTableVar.ShouldChange || LColumn.ShouldChange;
				}
			}
		}
	}
	
	// operator LeftJoin(table{}, table{}, object, object) : table{}	
	public class LeftOuterJoinNode : OuterJoinNode
	{
        protected override void DetermineColumns(Plan APlan)
        {
			DetermineLeftColumns(APlan, false);
			
			if (FRowExistsColumn != null)
			{
				Schema.TableVarColumn LRowExistsColumn =
					Compiler.CompileIncludeColumnExpression
					(
						APlan,
						FRowExistsColumn, 
						Keywords.RowExists, 
						APlan.Catalog.DataTypes.SystemBoolean, 
						Schema.TableVarColumnType.RowExists
					);
				DataType.Columns.Add(LRowExistsColumn.Column);
				TableVar.Columns.Add(LRowExistsColumn);
				FRowExistsColumnIndex = TableVar.Columns.Count - 1;
				LRowExistsColumn.IsChangeRemotable = false;
				LRowExistsColumn.IsDefaultRemotable = false;
			}

			DetermineRightColumns(APlan, true);
        }
        
		protected override void DetermineJoinModifiers(Plan APlan)
		{
			base.DetermineJoinModifiers(APlan);
			
			AnyOf = String.Empty;
			AllOf = String.Empty;
			foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
				if (!RightKey.Columns.ContainsName(LColumn.Name))
					AnyOf = (AnyOf == String.Empty) ? LColumn.Name : String.Format("{0};{1}", AnyOf, Schema.Object.EnsureRooted(LColumn.Name));
					
			AnyOf = LanguageModifiers.GetModifier(Modifiers, "AnyOf", AnyOf);
			AllOf = LanguageModifiers.GetModifier(Modifiers, "AllOf", AllOf);
			
			DetermineKeyColumns(AnyOf, AnyOfKey);
			DetermineKeyColumns(AllOf, AllOfKey);

			
			// Set change remotable to false for all AnyOf and AllOf columns
			foreach (Schema.TableVarColumn LColumn in AnyOfKey.Columns)
				LColumn.IsChangeRemotable = false;
				
			foreach (Schema.TableVarColumn LColumn in AllOfKey.Columns)
				LColumn.IsChangeRemotable = false;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			LeftOuterJoinExpression LExpression = new LeftOuterJoinExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			if (!IsNatural)
				LExpression.Condition = FExpression;
			LExpression.IsLookup = FIsLookup;
			LExpression.Cardinality = FCardinality;
			if (FRowExistsColumnIndex >= 0)
			{
				Schema.TableVarColumn LColumn = TableVar.Columns[FRowExistsColumnIndex];
				LExpression.RowExistsColumn = new IncludeColumnExpression(LColumn.Name);
				if (LColumn.MetaData != null)
				{
					LExpression.RowExistsColumn.MetaData = new MetaData();
					#if USEHASHTABLEFORTAGS
					foreach (Tag LTag in LColumn.MetaData.Tags)
						if (!LColumn.MetaData.Tags.IsReference(LTag.Name))
							LExpression.RowExistsColumn.MetaData.Tags.Add(LTag.Copy());
					#else
					for (int LIndex = 0; LIndex < LColumn.MetaData.Tags.Count; LIndex++)
						if (!LColumn.MetaData.Tags.IsReference(LIndex))
							LExpression.RowExistsColumn.MetaData.Tags.Add(LColumn.MetaData.Tags[LIndex].Copy());
					#endif
				}
			}
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		protected override void DetermineJoinKey(Plan APlan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(APlan, Cardinality, true, false);
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
				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					Schema.Key LNewKey = CopyKey(LLeftKey);
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = true;
						foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						{
							int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
							if (LLeftKeyIndex >= 0)
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}

						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
					}
				}
				
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					if (!LRightKey.Columns.IsProperSubsetOf(RightKey.Columns))
					{
						Schema.Key LNewKey = CopyKey(LRightKey, !LeftKey.Columns.IsSupersetOf(LRightKey.Columns));
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);

						if (!IsNatural)
						{
							LNewKey = new Schema.Key();
							LNewKey.IsInherited = true;
							LNewKey.IsSparse = !RightKey.Columns.IsSupersetOf(LRightKey.Columns);
							foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
							{
								int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
								if (LRightKeyIndex >= 0)
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
								else
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
							}
							
							if (!TableVar.Keys.Contains(LNewKey))
								TableVar.Keys.Add(LNewKey);
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
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						if ((LRightKeyIndex < 0) || (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name)))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					LNewKey.IsSparse = !LeftKey.Columns.IsSupersetOf(LNewKey.Columns);

					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LRightKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						{
							int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
							if (LRightKeyIndex >= 0)
							{
								if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name))
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
							}
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
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
				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse = LLeftKey.IsSparse;
					
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
					{
						int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
						if ((LLeftKeyIndex < 0) || (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name)))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						{
							int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
							if (LLeftKeyIndex >= 0)
							{
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name))
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
							}
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						LNewKey.IsSparse = !LeftKey.Columns.IsSupersetOf(LNewKey.Columns);

						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
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
		
		protected override void DetermineJoinAlgorithm(Plan APlan)
		{
			if (FExpression == null)
				FJoinAlgorithm = typeof(TimesTable);
			else
			{
				FJoinOrder = new Schema.Order(FLeftKey, APlan);
				Schema.OrderColumn LColumn;
				for (int LIndex = 0; LIndex < FJoinOrder.Columns.Count; LIndex++)
				{
					LColumn = FJoinOrder.Columns[LIndex];
					LColumn.Sort = Compiler.GetSort(APlan, LColumn.Column.DataType);
					if (LColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LColumn.Sort.Dependencies);
					LColumn.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to the right side
				if (!IsJoinOrder(APlan, FRightKey, RightNode))
				{
					Nodes[1] = Compiler.EnsureSearchableNode(APlan, RightNode, FRightKey);
					Nodes[1].DetermineDevice(APlan);
				}

				if (IsJoinOrder(APlan, FLeftKey, LeftNode) && IsJoinOrder(APlan, FRightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (FLeftKey.IsUnique || FRightKey.IsUnique))
				{
					// if both inputs are ordered by join keys and one or both join keys are unique
						// use a merge join algorithm
					FJoinAlgorithm = typeof(MergeLeftJoinTable);
					
					Order = CopyOrder(LeftNode.Order);
				}
				else if (IsJoinOrder(APlan, FRightKey, RightNode))
				{
					// else if the right input is ordered by join keys
						// use a searched join algorithm
					if (FRightKey.IsUnique)
						FJoinAlgorithm = typeof(RightUniqueSearchedLeftJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueSearchedLeftJoinTable);
						
					if (LeftNode.Order != null)
						Order = CopyOrder(LeftNode.Order);
				}
				else
				{
					// use a nested loop join
					// NOTE: this code will not be hit because of the join accelerator step above
					if (FRightKey.IsUnique)
						FJoinAlgorithm = typeof(RightUniqueNestedLoopLeftJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueNestedLoopLeftJoinTable);
				}
			}
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			bool LChanged = false;
			
			if ((HasRowExistsColumn() && (AColumnName == String.Empty)) || IsRowExistsColumn(AColumnName))
			{
				LChanged = DefaultColumn(AProcess, TableVar, ANewRow, AValueFlags, TableVar.Columns[FRowExistsColumnIndex].Name) || LChanged;

				int LRowIndex = ANewRow.DataType.Columns.IndexOfName(TableVar.Columns[FRowExistsColumnIndex].Name);
				if (!ANewRow.HasValue(LRowIndex))
				{
					LChanged = true;
					ANewRow[LRowIndex].AsBoolean = false;
				}
			}
			
			if (AIsDescending)
			{
				if (PropagateDefaultLeft && ((AColumnName == String.Empty) || LeftTableType.Columns.ContainsName(AColumnName)))
				{
					BitArray LValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(LeftKey.Columns[LIndex].Name);
					LChanged = LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(LeftKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, LeftKey.Columns[LIndex].Name);
				}

				if (PropagateDefaultRight && ((AColumnName == String.Empty) || RightTableType.Columns.ContainsName(AColumnName)) && HasRow(ANewRow))
				{
					BitArray LValueFlags = new BitArray(RightKey.Columns.Count);
					for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(RightKey.Columns[LIndex].Name);
					LChanged = RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(RightKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, RightKey.Columns[LIndex].Name);
				}
			}

			return LChanged;
		}
		
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (!FIsNatural && (AColumnName == String.Empty) && HasRow(ANewRow))
				return base.InternalValidate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			else
			{
				if (AIsDescending)
				{
					bool LChanged = false;
					if (PropagateValidateLeft && ((AColumnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(AColumnName))))
						LChanged = LeftNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;

					if (PropagateValidateRight && (((AColumnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(AColumnName))) && HasRow(ANewRow)))
						LChanged = RightNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					return LChanged;
				}
			}
			return false;
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
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			PushNewRow(AProcess, ANewRow);
			try
			{
				bool LChanged = false;
				bool LChangePropagated = false;
				
				if (AColumnName != String.Empty)
				{
					bool LIsRowExistsColumn = IsRowExistsColumn(AColumnName);
					if (FLeftKey.Columns.ContainsName(AColumnName) || LIsRowExistsColumn)
					{
						LChanged = true;
						if (IsNatural)
							LChangePropagated = true;
						if (!LIsRowExistsColumn || HasRow(ANewRow))
						{
							if (!AProcess.ServerSession.Server.IsRepository && RetrieveRight)
							{
								EnsureRightSelectNode(AProcess.Plan);
								using (Table LTable = FRightSelectNode.Execute(AProcess).Value as Table)
								{
									LTable.Open();
									if (LTable.Next())
									{
										LTable.Select(ANewRow);
										if (HasRowExistsColumn() && !LIsRowExistsColumn)
											ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = true;

										if (PropagateChangeRight)
											foreach (Schema.Column LColumn in RightTableType.Columns)
												RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}
									else
									{
										if (!LIsRowExistsColumn)
											if (HasRowExistsColumn())
												ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = false;
											else
											{
												// Clear any of/all of columns
												// TODO: This should be looked at, if any of/all of are both subsets of the right side, nothing needs to happen here
												// I don't want to run through them all here because it will end up firing changes twice in the default case
											}
											
										foreach (Schema.Column LColumn in RightTableType.Columns)
										{
											int LRightKeyIndex = FRightKey.Columns.IndexOfName(LColumn.Name);
											if (!HasRowExistsColumn() && (LRightKeyIndex >= 0) && CoordinateRight)
											{
												int LLeftRowIndex = ANewRow.DataType.Columns.IndexOfName(FLeftKey.Columns[LRightKeyIndex].Name);
												if (ANewRow.HasValue(LLeftRowIndex))
													ANewRow[LColumn.Name] = ANewRow[LLeftRowIndex];
												else
													ANewRow.ClearValue(LColumn.Name);
											}
											else
											{
												if (ClearRight)
													ANewRow.ClearValue(LColumn.Name);

												if (LIsRowExistsColumn && PropagateDefaultRight)
													RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
											}
												
											if (PropagateChangeRight)
												RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
										}
									}
								}
							}
							else
							{
								if (!LIsRowExistsColumn)
								{
									if (HasRow(ANewRow) && CoordinateRight)
										CoordinateRightJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
								}
								else // rowexists was set
								{
									foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
									{
										int LRightKeyIndex = FRightKey.Columns.IndexOfName(LColumn.Name);
										if ((LRightKeyIndex >= 0) && CoordinateRight)
										{
											int LLeftRowIndex = ANewRow.DataType.Columns.IndexOfName(FLeftKey.Columns[LRightKeyIndex].Name);
											if (ANewRow.HasValue(LLeftRowIndex))
												ANewRow[LColumn.Name] = ANewRow[LLeftRowIndex];
											else
												ANewRow.ClearValue(LColumn.Name);
										}
										else
										{
											if (ClearRight)
												ANewRow.ClearValue(LColumn.Name);

											if (!ANewRow.HasValue(LColumn.Name) && PropagateDefaultRight)
												RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
										}
										
										if (PropagateChangeRight)
											RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}
								}
							}
						}
						else // rowexists was cleared
						{
							if (ClearRight)
								foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
								{
									ANewRow.ClearValue(LColumn.Name);
									if (PropagateChangeRight)
										RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
						}
					}
					
					if (FRightKey.Columns.ContainsName(AColumnName) && HasRow(ANewRow) && CoordinateLeft)
					{
						LChanged = true;
						if (IsNatural)
							LChangePropagated = true;
						CoordinateLeftJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
					}
						
					if (FAnyOfKey.Columns.ContainsName(AColumnName) || FAllOfKey.Columns.ContainsName(AColumnName))
					{
						LChanged = true;
						if (HasValues(ANewRow))
						{
							if (HasRowExistsColumn())
								ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = true;
								
							foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
							{
								int LRightKeyIndex = FRightKey.Columns.IndexOfName(LColumn.Name);
								if ((LRightKeyIndex >= 0) && CoordinateRight)
									CoordinateRightJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, FLeftKey.Columns[LRightKeyIndex].Name);
								else
								{
									if (PropagateDefaultRight && RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name) && PropagateChangeRight)
										RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
							}
						}
						else
						{
							if (HasRowExistsColumn())
								ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = false;
								
							if (ClearRight)
								foreach (Schema.TableVarColumn LColumn in RightTableVar.Columns)
								{
									ANewRow.ClearValue(LColumn.Name);
									if (PropagateChangeRight)
										RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
						}
					}
				}
					
				if (!LChangePropagated && PropagateChangeLeft && ((AColumnName == String.Empty) || LeftTableType.Columns.ContainsName(AColumnName)))
					LChanged = LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					
				if (!LChangePropagated && PropagateChangeRight && ((AColumnName == String.Empty) || RightTableType.Columns.ContainsName(AColumnName)))
					LChanged = RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;

				return LChanged;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		protected void InternalInsertLeft(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertLeft)
			{
				case PropagateAction.True : 
					LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;
					
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LLeftRow = LeftNode.Select(AProcess, ANewRow))
					{
						if (LLeftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(AProcess, LLeftRow, ANewRow, AValueFlags, false, AUnchecked);
						}
						else
							LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				break;
			}
		}
		
		protected void InternalInsertRight(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			if (HasRow(ANewRow))				
			{
				switch (PropagateInsertRight)
				{
					case PropagateAction.True :
						RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); 
					break;
					
					case PropagateAction.Ensure :
					case PropagateAction.Ignore :
						using (Row LRightRow = RightNode.Select(AProcess, ANewRow))
						{
							if (LRightRow != null)
							{
								if (PropagateInsertRight == PropagateAction.Ensure)
									RightNode.Update(AProcess, LRightRow, ANewRow, AValueFlags, false, AUnchecked);
							}
							else
								RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
						}
					break;
				}
			}
		}

		protected override void InternalExecuteInsert(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				InternalInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
			else
			{
				InternalInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
		}
		
		protected override void InternalExecuteUpdate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			DataVar LOldRow = new DataVar(String.Empty, AOldRow.DataType, AOldRow);
			if (FUpdateLeftToRight)
			{
				if (PropagateUpdateLeft)
					LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
					
				if (PropagateUpdateRight)
				{
					if (HasRow(AOldRow))
					{
						if (HasRow(ANewRow))
							RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
						else
							RightNode.Delete(AProcess, AOldRow, ACheckConcurrency, AUnchecked);
					}
					else if (HasRow(ANewRow))
						RightNode.Insert(AProcess, null, ANewRow, AValueFlags, AUnchecked);
				}
			}
			else
			{
				if (PropagateUpdateRight)
				{
					if (HasRow(AOldRow))
					{
						if (HasRow(ANewRow))
							RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
						else
							RightNode.Delete(AProcess, AOldRow, ACheckConcurrency, AUnchecked);
					}
					else if (HasRow(ANewRow))
						RightNode.Insert(AProcess, null, ANewRow, AValueFlags, AUnchecked);
				}

				if (PropagateUpdateLeft)
					LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
		}

		protected override void InternalExecuteDelete(ServerProcess AProcess, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				if (PropagateDeleteRight && HasRow(ARow))
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
				
				if (PropagateDeleteLeft)
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
			else
			{
				if (PropagateDeleteLeft)
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);

				if (PropagateDeleteRight && HasRow(ARow))
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
		}

		public override void DetermineCursorCapabilities(Plan APlan)
		{
			FCursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					(
						APlan.CursorContext.CursorCapabilities & 
						LeftNode.CursorCapabilities & 
						CursorCapability.Updateable
					) &
					((FIsLookup && !FIsDetailLookup) ? CursorCapability.Updateable : (RightNode.CursorCapabilities & CursorCapability.Updateable))
				);
		}

		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			JoinLeftApplicationTransaction(AProcess, ARow);
			if (!FIsLookup || FIsDetailLookup)
				JoinRightApplicationTransaction(AProcess, ARow);
		}
	}
    
	// operator RightJoin(table{}, table{}, object, object) : table{}	
	public class RightOuterJoinNode : OuterJoinNode
	{
        protected override void DetermineColumns(Plan APlan)
        {
			DetermineLeftColumns(APlan, true);
			
			if (FRowExistsColumn != null)
			{
				Schema.TableVarColumn LRowExistsColumn =
					Compiler.CompileIncludeColumnExpression
					(
						APlan,
						FRowExistsColumn, 
						Keywords.RowExists, 
						APlan.Catalog.DataTypes.SystemBoolean, 
						Schema.TableVarColumnType.RowExists
					);
				DataType.Columns.Add(LRowExistsColumn.Column);
				TableVar.Columns.Add(LRowExistsColumn);
				FRowExistsColumnIndex = TableVar.Columns.Count - 1;
				LRowExistsColumn.IsChangeRemotable = false;
				LRowExistsColumn.IsDefaultRemotable = false;
			}

			DetermineRightColumns(APlan, false);
        }
        
		protected override void DetermineJoinModifiers(Plan APlan)
		{
			base.DetermineJoinModifiers(APlan);
			
			AnyOf = String.Empty;
			AllOf = String.Empty;
			foreach (Schema.TableVarColumn LColumn in LeftTableVar.Columns)
				if (!LeftKey.Columns.ContainsName(LColumn.Name))
					AnyOf = (AnyOf == String.Empty) ? LColumn.Name : String.Format("{0};{1}", AnyOf, Schema.Object.EnsureRooted(LColumn.Name));
					
			AnyOf = LanguageModifiers.GetModifier(Modifiers, "AnyOf", AnyOf);
			AllOf = LanguageModifiers.GetModifier(Modifiers, "AllOf", AllOf);
			
			DetermineKeyColumns(AnyOf, AnyOfKey);
			DetermineKeyColumns(AllOf, AllOfKey);

			// Set change remotable to false for all AnyOf and AllOf columns
			foreach (Schema.TableVarColumn LColumn in AnyOfKey.Columns)
				LColumn.IsChangeRemotable = false;
				
			foreach (Schema.TableVarColumn LColumn in AllOfKey.Columns)
				LColumn.IsChangeRemotable = false;
		}
		
		protected override void DetermineDetailLookup(Plan APlan)
		{
			if ((Modifiers != null) && (Modifiers.Contains("IsDetailLookup")))
				FIsDetailLookup = Convert.ToBoolean(Modifiers["IsDetailLookup"]);
			else
			{
				FIsDetailLookup = false;

				// If this is a lookup and the right join columns are a non-trivial proper superset of any key of the right side, this is a detail lookup
				if (FIsLookup)
					foreach (Schema.Key LKey in RightTableVar.Keys)
						if ((FRightKey.Columns.Count > 0) && FRightKey.Columns.IsProperSupersetOf(LKey.Columns))
						{
							FIsDetailLookup = true;
							break;
						}
				
				#if USEDETAILLOOKUP
				// if this is a lookup, the right key is not unique, and any key of the right table is a subset of the right key, this is a detail lookup.
				if (FIsLookup && !FRightKey.IsUnique)
					foreach (Schema.Key LKey in RightTableVar.Keys)
						if (LKey.Columns.IsSubsetOf(FRightKey.Columns))
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
					Schema.Key LInvariant = RightNode.TableVar.CalculateInvariant();
					foreach (Schema.Column LColumn in FRightKey.Columns)
						if (LInvariant.Columns.Contains(LColumn.Name))
						{
							FIsDetailLookup = true;
							break;
						}
				}
				#endif
			}
		}

		protected override void DetermineJoinKey(Plan APlan)
		{
			#if USEUNIFIEDJOINKEYINFERENCE
			InferJoinKeys(APlan, Cardinality, false, true);
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
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					Schema.Key LNewKey = CopyKey(LRightKey);
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = true;
						foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
						{
							int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
							if (LRightKeyIndex >= 0)
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
							else
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
					}
				}

				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					if (!LLeftKey.Columns.IsProperSubsetOf(LeftKey.Columns))
					{
						Schema.Key LNewKey = CopyKey(LLeftKey, !RightKey.Columns.IsSupersetOf(LLeftKey.Columns));
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);

						if (!IsNatural)
						{
							LNewKey = new Schema.Key();
							LNewKey.IsInherited = true;
							LNewKey.IsSparse = !LeftKey.Columns.IsSupersetOf(LLeftKey.Columns);
							foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
							{
								int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
								if (LLeftKeyIndex >= 0)
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
								else
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
							}

							if (!TableVar.Keys.Contains(LNewKey))
								TableVar.Keys.Add(LNewKey);
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
				foreach (Schema.Key LLeftKey in LeftTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
					{
						int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
						if ((LLeftKeyIndex < 0) || RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					LNewKey.IsSparse = !RightKey.Columns.IsSupersetOf(LLeftKey.Columns);
					
					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);

					if (!IsNatural)
					{
						LNewKey = new Schema.Key();
						LNewKey.IsInherited = true;
						LNewKey.IsSparse = LLeftKey.IsSparse;
						foreach (Schema.TableVarColumn LColumn in LLeftKey.Columns)
						{
							int LLeftKeyIndex = LeftKey.Columns.IndexOfName(LColumn.Name);
							if (LLeftKeyIndex >= 0)
							{	
								if (RightTableVar.Keys.IsKeyColumnName(RightKey.Columns[LLeftKeyIndex].Name))
									LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(RightKey.Columns[LLeftKeyIndex].Name)]);
							}
							else
								LNewKey.Columns.Add(LColumn);
						}
						
						if (!TableVar.Keys.Contains(LNewKey))
							TableVar.Keys.Add(LNewKey);
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
				foreach (Schema.Key LRightKey in RightTableVar.Keys)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					LNewKey.IsSparse = LRightKey.IsSparse;
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						if ((LRightKeyIndex < 0) || LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name))
							LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LColumn.Name)]);
					}

					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);
	
					LNewKey = new Schema.Key();
					LNewKey.IsInherited = true;
					foreach (Schema.TableVarColumn LColumn in LRightKey.Columns)
					{
						int LRightKeyIndex = RightKey.Columns.IndexOfName(LColumn.Name);
						if (LRightKeyIndex >= 0)
						{
							if (LeftTableVar.Keys.IsKeyColumnName(LeftKey.Columns[LRightKeyIndex].Name))
								LNewKey.Columns.Add(TableVar.Columns[TableVar.Columns.IndexOfName(LeftKey.Columns[LRightKeyIndex].Name)]);
						}
						else
							LNewKey.Columns.Add(LColumn);
					}
					
					LNewKey.IsSparse = !RightKey.Columns.IsSupersetOf(LNewKey.Columns);

					if (!TableVar.Keys.Contains(LNewKey))
						TableVar.Keys.Add(LNewKey);
				}
				
				DetermineManyToManyKeys(APlan);
			}
			else
			{
				DetermineManyToManyKeys(APlan);
			}
			#endif
		}
		
		protected override void DetermineJoinAlgorithm(Plan APlan)
		{
			if (FExpression == null)
				FJoinAlgorithm = typeof(TimesTable);
			else
			{
				FJoinOrder = new Schema.Order(FLeftKey, APlan);
				Schema.OrderColumn LColumn;
				for (int LIndex = 0; LIndex < FJoinOrder.Columns.Count; LIndex++)
				{
					LColumn = FJoinOrder.Columns[LIndex];
					LColumn.Sort = Compiler.GetSort(APlan, LColumn.Column.DataType);
					if (LColumn.Sort.HasDependencies())
						APlan.AttachDependencies(LColumn.Sort.Dependencies);
					LColumn.IsDefaultSort = true;
				}

				// if no inputs are ordered by join keys, add an order node to the left side
				if (!IsJoinOrder(APlan, FLeftKey, LeftNode))
				{
					Nodes[0] = Compiler.EnsureSearchableNode(APlan, LeftNode, FLeftKey);
					Nodes[0].DetermineDevice(APlan);
				}

				// if both inputs are ordered by join keys and one or both join keys are unique
				if (IsJoinOrder(APlan, FLeftKey, LeftNode) && IsJoinOrder(APlan, FRightKey, RightNode) && JoinOrdersAscendingCompatible(LeftNode.Order, RightNode.Order) && (FLeftKey.IsUnique || FRightKey.IsUnique))
				{
					// use a merge join algorithm
					FJoinAlgorithm = typeof(MergeRightJoinTable);
					
					Order = CopyOrder(RightNode.Order);
				}
				// else if the left input is ordered by join keys
					// use a searched join algorithm
				else if (IsJoinOrder(APlan, FLeftKey, LeftNode))
				{
					if (FLeftKey.IsUnique)
						FJoinAlgorithm = typeof(LeftUniqueSearchedRightJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueSearchedRightJoinTable);
						
					if (RightNode.Order != null)
						Order = CopyOrder(RightNode.Order);
				}
				else
				{
					// use a nested loop join
					// NOTE: this code will not be hit because of the join accelerator step above
					if (FLeftKey.IsUnique)
						FJoinAlgorithm = typeof(LeftUniqueNestedLoopRightJoinTable);
					else
						FJoinAlgorithm = typeof(NonUniqueNestedLoopRightJoinTable);
				}
			}
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			if (IsLookup && !IsDetailLookup)
				APlan.PushCursorContext(new CursorContext(APlan.CursorContext.CursorType, APlan.CursorContext.CursorCapabilities & ~CursorCapability.Updateable, APlan.CursorContext.CursorIsolation));
			try
			{
				Nodes[0].DetermineBinding(APlan);
			}
			finally
			{
				if (IsLookup && !IsDetailLookup)
					APlan.PopCursorContext();
			}

			Nodes[1].DetermineBinding(APlan);
			
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new DataVar(new Schema.RowType(LeftTableType.Columns, FIsNatural ? Keywords.Left : String.Empty)));
				try
				{
					APlan.Symbols.Push(new DataVar(new Schema.RowType(RightTableType.Columns, FIsNatural ? Keywords.Right : String.Empty)));
					try
					{
						Nodes[2].DetermineBinding(APlan);
					}
					finally
					{
						APlan.Symbols.Pop();
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
			}
			finally
			{
				APlan.ExitRowContext();
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			RightOuterJoinExpression LExpression = new RightOuterJoinExpression();
			LExpression.LeftExpression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.RightExpression = (Expression)Nodes[1].EmitStatement(AMode);
			if (!IsNatural)
				LExpression.Condition = FExpression; //(Expression)Nodes[2].EmitStatement(AMode);
			LExpression.IsLookup = FIsLookup;
			LExpression.Cardinality = FCardinality;
			//LExpression.IsDetailLookup = FIsDetailLookup;
			if (FRowExistsColumnIndex >= 0)
			LExpression.RowExistsColumn = new IncludeColumnExpression(DataType.Columns[FRowExistsColumnIndex].Name);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			bool LChanged = false;

			if ((HasRowExistsColumn() && (AColumnName == String.Empty)) || IsRowExistsColumn(AColumnName))
			{
				LChanged = DefaultColumn(AProcess, TableVar, ANewRow, AValueFlags, TableVar.Columns[FRowExistsColumnIndex].Name) || LChanged;

				int LRowIndex = ANewRow.DataType.Columns.IndexOfName(TableVar.Columns[FRowExistsColumnIndex].Name);
				if (!ANewRow.HasValue(LRowIndex))
				{
					LChanged = true;
					ANewRow[LRowIndex].AsBoolean = false;
				}
			}

			if (AIsDescending)
			{
				if (PropagateDefaultLeft && ((AColumnName == String.Empty) || LeftTableType.Columns.ContainsName(AColumnName)) && HasRow(ANewRow))
				{
					BitArray LValueFlags = new BitArray(LeftKey.Columns.Count);
					for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(LeftKey.Columns[LIndex].Name);
					LChanged = LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < LeftKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(LeftKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, LeftKey.Columns[LIndex].Name);
				}

				if (PropagateDefaultRight && ((AColumnName == String.Empty) || RightTableType.Columns.ContainsName(AColumnName)))
				{
					BitArray LValueFlags = new BitArray(RightKey.Columns.Count);
					for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
						LValueFlags[LIndex] = ANewRow.HasValue(RightKey.Columns[LIndex].Name);
					LChanged = RightNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					if (LChanged)
						for (int LIndex = 0; LIndex < RightKey.Columns.Count; LIndex++)
							if (!LValueFlags[LIndex] && ANewRow.HasValue(RightKey.Columns[LIndex].Name))
								Change(AProcess, AOldRow, ANewRow, AValueFlags, RightKey.Columns[LIndex].Name);
				}
			}

			return LChanged;
		}
		
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			// The new row only needs to satisfy the join predicate if the row contains values for the left side.
			if (!FIsNatural && (AColumnName == String.Empty) && HasRow(ANewRow))
				return base.InternalValidate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			else
			{
				if (AIsDescending)
				{
					bool LChanged = false;
					if (((AColumnName == String.Empty) || (LeftNode.TableVar.Columns.ContainsName(AColumnName))) && HasRow(ANewRow))
						LChanged = LeftNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;

					if ((AColumnName == String.Empty) || (RightNode.TableVar.Columns.ContainsName(AColumnName)))
						LChanged = RightNode.Validate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					return LChanged;
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
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			PushNewRow(AProcess, ANewRow);
			try
			{
				bool LChanged = false;
				bool LChangePropagated = false;
				
				if (AColumnName != String.Empty)
				{
					bool LIsRowExistsColumn = IsRowExistsColumn(AColumnName);
					if (FRightKey.Columns.ContainsName(AColumnName) || LIsRowExistsColumn)
					{
						LChanged = true;
						if (IsNatural)
							LChangePropagated = true;
						if (!LIsRowExistsColumn || HasRow(ANewRow))
						{
							if (!AProcess.ServerSession.Server.IsRepository && RetrieveLeft)
							{
								EnsureLeftSelectNode(AProcess.Plan);
								using (Table LTable = FLeftSelectNode.Execute(AProcess).Value as Table)
								{
									LTable.Open();
									if (LTable.Next())
									{
										LTable.Select(ANewRow);
										if (HasRowExistsColumn() && !LIsRowExistsColumn)
											ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = true;

										if (PropagateChangeLeft)
											foreach (Schema.Column LColumn in LeftTableType.Columns)
												LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}
									else
									{
										if (!LIsRowExistsColumn)
											if (HasRowExistsColumn())
												ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = false;
											else
											{
												// Clear any of/all of columns
												// TODO: This should be looked at, if any of/all of are both subsets of the Left side, nothing needs to happen here
												// I don't want to run through them all here because it will end up firing changes twice in the default case
											}
											
										foreach (Schema.Column LColumn in LeftTableType.Columns)
										{
											int LLeftKeyIndex = FLeftKey.Columns.IndexOfName(LColumn.Name);
											if (!HasRowExistsColumn() && (LLeftKeyIndex >= 0) && CoordinateLeft)
											{
												int LRightRowIndex = ANewRow.DataType.Columns.IndexOfName(FRightKey.Columns[LLeftKeyIndex].Name);
												if (ANewRow.HasValue(LRightRowIndex))
													ANewRow[LColumn.Name] = ANewRow[LRightRowIndex];
												else
													ANewRow.ClearValue(LColumn.Name);
											}
											else
											{
												if (ClearLeft)
													ANewRow.ClearValue(LColumn.Name);

												if (LIsRowExistsColumn && PropagateDefaultLeft)
													LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
											}
												
											if (PropagateChangeLeft)
												LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
										}
									}
								}
							}
							else
							{
								if (!LIsRowExistsColumn)
								{
									if (HasRow(ANewRow) && CoordinateLeft)
										CoordinateLeftJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
								}
								else // rowexists was set
								{
									foreach (Schema.TableVarColumn LColumn in LeftTableVar.Columns)
									{
										int LLeftKeyIndex = FLeftKey.Columns.IndexOfName(LColumn.Name);
										if ((LLeftKeyIndex >= 0) && CoordinateLeft)
										{
											int LRightRowIndex = ANewRow.DataType.Columns.IndexOfName(FRightKey.Columns[LLeftKeyIndex].Name);
											if (ANewRow.HasValue(LRightRowIndex))
												ANewRow[LColumn.Name] = ANewRow[LRightRowIndex];
											else
												ANewRow.ClearValue(LColumn.Name);
										}
										else
										{
											if (ClearLeft)
												ANewRow.ClearValue(LColumn.Name);

											if (!ANewRow.HasValue(LColumn.Name) && PropagateDefaultLeft)
												LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
										}
										
										if (PropagateChangeLeft)
											LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
									}
								}
							}
						}
						else // rowexists was cleared
						{
							if (ClearLeft)
								foreach (Schema.TableVarColumn LColumn in LeftTableVar.Columns)
								{
									ANewRow.ClearValue(LColumn.Name);
									if (PropagateChangeLeft)
										LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
						}
					}
					
					if (FLeftKey.Columns.ContainsName(AColumnName) && HasRow(ANewRow) && CoordinateRight)
					{
						LChanged = true;
						if (IsNatural)
							LChangePropagated = true;
						CoordinateRightJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
					}
						
					if (FAnyOfKey.Columns.ContainsName(AColumnName) || FAllOfKey.Columns.ContainsName(AColumnName))
					{
						LChanged = true;
						if (HasValues(ANewRow))
						{
							if (HasRowExistsColumn())
								ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = true;
								
							foreach (Schema.TableVarColumn LColumn in LeftTableVar.Columns)
							{
								int LLeftKeyIndex = FLeftKey.Columns.IndexOfName(LColumn.Name);
								if ((LLeftKeyIndex >= 0) && CoordinateLeft)
									CoordinateLeftJoinKey(AProcess, AOldRow, ANewRow, AValueFlags, FRightKey.Columns[LLeftKeyIndex].Name);
								else
								{
									if (PropagateDefaultLeft && LeftNode.Default(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name) && PropagateChangeLeft)
										LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
							}
						}
						else
						{
							if (HasRowExistsColumn())
								ANewRow[DataType.Columns[FRowExistsColumnIndex].Name].AsBoolean = false;
								
							if (ClearLeft)
								foreach (Schema.TableVarColumn LColumn in LeftTableVar.Columns)
								{
									ANewRow.ClearValue(LColumn.Name);
									if (PropagateChangeLeft)
										LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, LColumn.Name);
								}
						}
					}
				}
					
				if (!LChangePropagated && PropagateChangeRight && ((AColumnName == String.Empty) || RightTableType.Columns.ContainsName(AColumnName)))
					LChanged = RightNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;
					
				if (!LChangePropagated && PropagateChangeLeft && ((AColumnName == String.Empty) || LeftTableType.Columns.ContainsName(AColumnName)))
					LChanged = LeftNode.Change(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName) || LChanged;

				return LChanged;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		protected void InternalInsertLeft(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			if (HasRow(ANewRow))
			{
				switch (PropagateInsertLeft)
				{
					case PropagateAction.True : 
						LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); break;
						
					case PropagateAction.Ensure :
					case PropagateAction.Ignore :
					using (Row LLeftRow = LeftNode.Select(AProcess, ANewRow))
					{
						if (LLeftRow != null)
						{
							if (PropagateInsertLeft == PropagateAction.Ensure)
								LeftNode.Update(AProcess, LLeftRow, ANewRow, AValueFlags, false, AUnchecked);
						}
						else
							LeftNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
					break;
				}
			}
		}
		
		protected void InternalInsertRight(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			switch (PropagateInsertRight)
			{
				case PropagateAction.True :
					RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked); 
				break;
				
				case PropagateAction.Ensure :
				case PropagateAction.Ignore :
					using (Row LRightRow = RightNode.Select(AProcess, ANewRow))
					{
						if (LRightRow != null)
						{
							if (PropagateInsertRight == PropagateAction.Ensure)
								RightNode.Update(AProcess, LRightRow, ANewRow, AValueFlags, false, AUnchecked);
						}
						else
							RightNode.Insert(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
					}
				break;
			}
		}

		protected override void InternalExecuteInsert(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				InternalInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
			else
			{
				InternalInsertRight(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
				InternalInsertLeft(AProcess, AOldRow, ANewRow, AValueFlags, AUnchecked);
			}
		}
		
		protected override void InternalExecuteUpdate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, bool ACheckConcurrency, bool AUnchecked)
		{
			DataVar LOldRow = new DataVar(String.Empty, AOldRow.DataType, AOldRow);
			if (FUpdateLeftToRight)
			{
				if (PropagateUpdateLeft)
				{
					if (HasRow(AOldRow))
					{
						if (HasRow(ANewRow))
							LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
						else
							LeftNode.Delete(AProcess, AOldRow, ACheckConcurrency, AUnchecked);
					}
					else
						if (HasRow(ANewRow))
							LeftNode.Insert(AProcess, null, ANewRow, AValueFlags, AUnchecked);
				}

				if (PropagateUpdateRight)
					RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
			}
			else
			{
				if (PropagateUpdateRight)
					RightNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
					
				if (PropagateUpdateLeft)
				{
					if (HasRow(AOldRow))
					{
						if (HasRow(ANewRow))
							LeftNode.Update(AProcess, AOldRow, ANewRow, AValueFlags, ACheckConcurrency, AUnchecked);
						else
							LeftNode.Delete(AProcess, AOldRow, ACheckConcurrency, AUnchecked);
					}
					else
						if (HasRow(ANewRow))
							LeftNode.Insert(AProcess, null, ANewRow, AValueFlags, AUnchecked);
				}
			}
		}

		protected override void InternalExecuteDelete(ServerProcess AProcess, Row ARow, bool ACheckConcurrency, bool AUnchecked)
		{
			if (FUpdateLeftToRight)
			{
				if (PropagateDeleteRight)
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
				
				if (PropagateDeleteLeft && HasRow(ARow))
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
			else
			{
				if (PropagateDeleteLeft && HasRow(ARow))
					LeftNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
				
				if (PropagateDeleteRight)
					RightNode.Delete(AProcess, ARow, ACheckConcurrency, AUnchecked);
			}
		}

		public override void DetermineCursorCapabilities(Plan APlan)
		{
			FCursorCapabilities = 
				CursorCapability.Navigable |
				(
					LeftNode.CursorCapabilities & 
					RightNode.CursorCapabilities & 
					CursorCapability.BackwardsNavigable
				) |
				(
					(
						APlan.CursorContext.CursorCapabilities & 
						RightNode.CursorCapabilities & 
						CursorCapability.Updateable
					) &
					((FIsLookup && !FIsDetailLookup) ? CursorCapability.Updateable : (LeftNode.CursorCapabilities & CursorCapability.Updateable))
				);
		}

		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			if (!FIsLookup || FIsDetailLookup)
				JoinLeftApplicationTransaction(AProcess, ARow);
			JoinRightApplicationTransaction(AProcess, ARow);
		}
	}
}