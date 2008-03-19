/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
	
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
	
	// operator iRedefine(table{}, object) : table{}
	public class RedefineNode : UnaryTableNode
	{
		protected int[] FRedefineColumnOffsets;		
		public int[] RedefineColumnOffsets
		{
			get { return FRedefineColumnOffsets; }
			set { FRedefineColumnOffsets = value; }
		}
		
		private NamedColumnExpressions FExpressions;
		public NamedColumnExpressions Expressions
		{
			get { return FExpressions; }
			set { FExpressions = value; }
		}
		
		private bool FDistinctRequired;
		public bool DistinctRequired
		{
			get { return FDistinctRequired; }
			set { FDistinctRequired = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);

			int LIndex = 0;			
			FRedefineColumnOffsets = new int[FExpressions.Count];
			ApplicationTransaction LTransaction = null;
			if (APlan.ServerProcess.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.ServerProcess.GetApplicationTransaction();
			try
			{
				if (LTransaction != null)
					LTransaction.PushLookup();
				try
				{
					APlan.PushCursorContext(new CursorContext(CursorType.Dynamic, CursorCapability.Navigable, CursorIsolation.None));
					try
					{
						APlan.EnterRowContext();
						try
						{
							APlan.Symbols.Push(new DataVar(SourceTableType.RowType));
							try
							{
								// Add a column for each expression
								PlanNode LPlanNode;
								Schema.TableVarColumn LSourceColumn;
								Schema.TableVarColumn LTempColumn;
								Schema.TableVarColumn LNewColumn;
								foreach (NamedColumnExpression LColumn in FExpressions)
								{
									int LSourceColumnIndex = TableVar.Columns.IndexOf(LColumn.ColumnAlias);
									if (LSourceColumnIndex < 0)
										throw new CompilerException(CompilerException.Codes.UnknownIdentifier, LColumn, LColumn.ColumnAlias);
										
									LSourceColumn = TableVar.Columns[LSourceColumnIndex];
									LTempColumn = CopyTableVarColumn(LSourceColumn);

									APlan.PushCreationObject(LTempColumn);
									try
									{
										LPlanNode = Compiler.CompileExpression(APlan, LColumn.Expression);
									}
									finally
									{
										APlan.PopCreationObject();
									}
									
									LNewColumn = CopyTableVarColumn(LSourceColumn);
									LNewColumn.Column.DataType = LPlanNode.DataType;
									if (LTempColumn.HasDependencies())
										LNewColumn.AddDependencies(LTempColumn.Dependencies);
									Schema.Object LObject;
									if (LNewColumn.HasDependencies())
										for (int LDependencyIndex = 0; LIndex < LNewColumn.Dependencies.Count; LIndex++)
										{
											LObject = LNewColumn.Dependencies.ResolveObject(APlan.ServerProcess, LDependencyIndex);
											APlan.AttachDependency(LObject);
											LNewColumn.IsNilable = LPlanNode.IsNilable;
											LNewColumn.IsChangeRemotable = LNewColumn.IsChangeRemotable && LObject.IsRemotable;
											LNewColumn.IsDefaultRemotable = LNewColumn.IsDefaultRemotable && LObject.IsRemotable;
										}

									DataType.Columns[LSourceColumnIndex] = LNewColumn.Column;
									TableVar.Columns[LSourceColumnIndex] = LNewColumn;
									FRedefineColumnOffsets[LIndex] = LSourceColumnIndex;
									Nodes.Add(LPlanNode);
									LIndex++;
								}

								DetermineRemotable(APlan);
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
						APlan.PopCursorContext();
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
			
			foreach (Schema.Key LKey in SourceTableVar.Keys)
			{
				bool LAdd = true;
				foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					if (((IList)FRedefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(LColumn.Name)))
					{
						LAdd = false;
						break;
					}
					
				if (LAdd)
					TableVar.Keys.Add(CopyKey(LKey));
			}
			
			FDistinctRequired = TableVar.Keys.Count == 0;
			if (FDistinctRequired)
			{
				Schema.Key LNewKey = new Schema.Key();
				foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
					LNewKey.Columns.Add(LColumn);
				LNewKey.IsInherited = true;
				TableVar.Keys.Add(LNewKey);
			}
			
			foreach (Schema.Order LOrder in SourceTableVar.Orders)
			{
				bool LAdd = true;
				for (int LColumnIndex = 0; LColumnIndex < LOrder.Columns.Count; LColumnIndex++)
					if (((IList)FRedefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(LOrder.Columns[LColumnIndex].Column.Name)))
					{
						LAdd = false;
						break;
					}
					
				if (LAdd)
					TableVar.Orders.Add(CopyOrder(LOrder));
			}
			
			DetermineOrder(APlan);
			
			#if UseReferenceDerivation
			// TODO: Reference derivation on a redefine should exclude affected references
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
		}
		
		public override void JoinApplicationTransaction(ServerProcess AProcess, Row ARow)
		{
			// Exclude any columns from AKey which were included by this node
			Schema.RowType LRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				if (!((IList)FRedefineColumnOffsets).Contains(FTableVar.DataType.Columns.IndexOfName(LColumn.Name)))
					LRowType.Columns.Add(LColumn.Copy());

			Row LRow = new Row(AProcess, LRowType);
			try
			{
				ARow.CopyTo(LRow);
				SourceNode.JoinApplicationTransaction(AProcess, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
		}

		public void DetermineOrder(Plan APlan)
		{
			Order = null;
			if (SourceNode.Order != null)
			{
				bool LAdd = true;
				for (int LIndex = 0; LIndex < SourceNode.Order.Columns.Count; LIndex++)
					if (((IList)FRedefineColumnOffsets).Contains(TableVar.Columns.IndexOfName(SourceNode.Order.Columns[LIndex].Column.Name)))
					{
						LAdd = false;
						break;
					}
				
				if (LAdd)
					Order = CopyOrder(SourceNode.Order);
			}
		}
		
		public override void DetermineCursorBehavior(Plan APlan)
		{
			FCursorType = SourceNode.CursorType;
			FRequestedCursorType = APlan.CursorContext.CursorType;
			FCursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(APlan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				);
			FCursorIsolation = APlan.CursorContext.CursorIsolation;
			
			DetermineOrder(APlan);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			RedefineExpression LExpression = new RedefineExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			for (int LIndex = 0; LIndex < FRedefineColumnOffsets.Length; LIndex++)
				LExpression.Expressions.Add(new NamedColumnExpression((Expression)Nodes[LIndex + 1].EmitStatement(AMode), DataType.Columns[FRedefineColumnOffsets[LIndex]].Name));
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new DataVar(SourceTableType.RowType));
				try
				{
					for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
						Nodes[LIndex].DetermineBinding(APlan);
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
		
		public override DataVar InternalExecute(ServerProcess AProcess)
		{
			RedefineTable LTable = new RedefineTable(this, AProcess);
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
			
			FTableVar.ShouldChange = true;
			FTableVar.ShouldDefault = true;
			foreach (Schema.TableVarColumn LColumn in FTableVar.Columns)
			{
				LColumn.ShouldChange = true;
				LColumn.ShouldDefault = true;
			}
		}
		
		protected override bool InternalDefault(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			bool LChanged = false;
			if ((AColumnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(AColumnName)))
				LChanged = base.InternalDefault(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending);
			return InternalInternalChange(ANewRow, AColumnName, AProcess, true) || LChanged;
		}
		
		protected bool InternalInternalChange(Row ARow, string AColumnName, ServerProcess AProcess, bool AIsDefault)
		{
			bool LChanged = false;
			PushRow(AProcess, ARow);
			try
			{
				// Evaluate the Redefined columns
				// TODO: This change code should only run if the column changing can be determined to affect the extended columns...
				int LColumnIndex;
				for (int LIndex = 0; LIndex < FRedefineColumnOffsets.Length; LIndex++)
				{
					Schema.TableVarColumn LColumn = TableVar.Columns[FRedefineColumnOffsets[LIndex]];
					if ((AIsDefault || LColumn.IsComputed) && (!AProcess.ServerSession.Server.IsRepository || LColumn.IsChangeRemotable))
					{
						LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Name);
						if (LColumnIndex >= 0)
						{
							if (!AIsDefault || !ARow.HasValue(LColumnIndex))
							{
								ARow[LColumnIndex] = Nodes[LIndex + 1].Execute(AProcess).Value;
								LChanged = true;
							}
						}
					}
				}
				return LChanged;
			}
			finally
			{
				PopRow(AProcess);
			}
		}
		
		protected override bool InternalChange(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			bool LChanged = false;
			if ((AColumnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(AColumnName)))
				LChanged = base.InternalChange(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName);
			return InternalInternalChange(ANewRow, AColumnName, AProcess, false) || LChanged;
		}
		
		protected override bool InternalValidate(ServerProcess AProcess, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if ((AColumnName == String.Empty) || SourceTableVar.Columns.ContainsName(AColumnName))
				return base.InternalValidate(AProcess, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			return false;
		}

		public override bool IsContextLiteral(int ALocation)
		{
			if (!Nodes[0].IsContextLiteral(ALocation))
				return false;
			for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
				if (!Nodes[LIndex].IsContextLiteral(ALocation + 1))
					return false;
			return true;
		}
	}
}