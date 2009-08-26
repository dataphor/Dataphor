/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
	
using System;
using System.Text;
using System.Threading;
using System.Collections;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Server;	
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	// operator iExtend(table{}, object) : table{}
	public class ExtendNode : UnaryTableNode
	{
		protected int FExtendColumnOffset;		
		public int ExtendColumnOffset
		{
			get { return FExtendColumnOffset; }
		}
		
		private NamedColumnExpressions FExpressions;
		public NamedColumnExpressions Expressions
		{
			get { return FExpressions; }
			set { FExpressions = value; }
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			CopyTableVarColumns(SourceTableVar.Columns);
			FExtendColumnOffset = TableVar.Columns.Count;

			// This structure will track key columns as a set of sets, and any extended columns that are equivalent to them
			Hashtable LKeyColumns = new Hashtable();
			foreach (Schema.TableVarColumn LTableVarColumn in TableVar.Columns)
				if (SourceTableVar.Keys.IsKeyColumnName(LTableVarColumn.Name) && !LKeyColumns.Contains(LTableVarColumn.Name))
					LKeyColumns.Add(LTableVarColumn.Name, new Schema.Key(new Schema.TableVarColumn[]{LTableVarColumn}));
			
			ApplicationTransaction LTransaction = null;
			if (APlan.ApplicationTransactionID != Guid.Empty)
				LTransaction = APlan.GetApplicationTransaction();
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
							APlan.Symbols.Push(new Symbol(SourceTableType.RowType));
							try
							{
								// Add a column for each expression
								PlanNode LPlanNode;
								Schema.TableVarColumn LNewColumn;
								foreach (NamedColumnExpression LColumn in FExpressions)
								{
									LNewColumn = new Schema.TableVarColumn(new Schema.Column(LColumn.ColumnAlias, APlan.DataTypes.SystemScalar));
									APlan.PushCreationObject(LNewColumn);
									try
									{
										LPlanNode = Compiler.CompileExpression(APlan, LColumn.Expression);
									}
									finally
									{
										APlan.PopCreationObject();
									}
									
									bool LIsChangeRemotable = true;
									if (LNewColumn.HasDependencies())
										for (int LIndex = 0; LIndex < LNewColumn.Dependencies.Count; LIndex++)
										{
											Schema.Object LObject = LNewColumn.Dependencies.ResolveObject(APlan.CatalogDeviceSession, LIndex);
											LIsChangeRemotable = LIsChangeRemotable && LObject.IsRemotable;
											APlan.AttachDependency(LObject);
										}

									LNewColumn = 
										new Schema.TableVarColumn
										(
											new Schema.Column(LColumn.ColumnAlias, LPlanNode.DataType),
											LColumn.MetaData, 
											Schema.TableVarColumnType.Virtual
										);

									LNewColumn.IsNilable = LPlanNode.IsNilable;
									LNewColumn.IsChangeRemotable = LIsChangeRemotable;
									LNewColumn.IsDefaultRemotable = LIsChangeRemotable;

									DataType.Columns.Add(LNewColumn.Column);
									TableVar.Columns.Add(LNewColumn);
									
									string LColumnName = String.Empty;
									if (IsColumnReferencing(LPlanNode, ref LColumnName))
									{
										Schema.TableVarColumn LReferencedColumn = TableVar.Columns[LColumnName];
										if (SourceTableVar.Keys.IsKeyColumnName(LReferencedColumn.Name))
										{
											if (LKeyColumns.Contains(LReferencedColumn.Name))
												((Schema.Key)LKeyColumns[LReferencedColumn.Name]).Columns.Add(LNewColumn);
											else
												LKeyColumns.Add(LReferencedColumn.Name, new Schema.Key(new Schema.TableVarColumn[]{LNewColumn}));
										}
									}
									
									Nodes.Add(LPlanNode);
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
				// Seed the result key set with the empty set
				Schema.Keys LResultKeys = new Schema.Keys();
				LResultKeys.Add(new Schema.Key());
				
				foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					LResultKeys = KeyProduct(LResultKeys, (Schema.Key)LKeyColumns[LColumn.Name]);
					
				foreach (Schema.Key LResultKey in LResultKeys)
				{
					LResultKey.IsSparse = LKey.IsSparse;
					LResultKey.IsInherited = true;
					LResultKey.MergeMetaData(LKey.MetaData);
					TableVar.Keys.Add(LResultKey);
				}
			}
			
			CopyOrders(SourceTableVar.Orders);
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);

			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceTableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceTableVar.TargetReferences);
			#endif
		}
		
		protected Schema.Keys KeyProduct(Schema.Keys AKeys, Schema.Key AKey)
		{
			Schema.Keys LResult = new Schema.Keys();
			foreach (Schema.Key LKey in AKeys)
				foreach (Schema.TableVarColumn LNewColumn in AKey.Columns)
				{
					Schema.Key LNewKey = new Schema.Key();
					LNewKey.Columns.AddRange(LKey.Columns);
					LNewKey.Columns.Add(LNewColumn);
					LResult.Add(LNewKey);
				}
			return LResult;
		}
		
		protected bool IsColumnReferencing(PlanNode ANode, ref string AColumnName)
		{
			StackColumnReferenceNode LNode = ANode as StackColumnReferenceNode;
			if ((LNode != null) && (LNode.Location == 0))
			{
				AColumnName = LNode.Identifier;
				return true;
			}
			else if (ANode.IsOrderPreserving && (ANode.NodeCount == 1))
				return IsColumnReferencing(ANode.Nodes[0], ref AColumnName);
			else
				return false;
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

			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			ExtendExpression LExpression = new ExtendExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			for (int LIndex = ExtendColumnOffset; LIndex < TableVar.Columns.Count; LIndex++)
				LExpression.Expressions.Add
				(
					new NamedColumnExpression
					(
						(Expression)Nodes[LIndex - ExtendColumnOffset + 1].EmitStatement(AMode), 
						DataType.Columns[LIndex].Name, 
						(MetaData)(TableVar.Columns[LIndex].MetaData == null ? 
							null : 
							TableVar.Columns[LIndex].MetaData.Copy()
						)
					)
				);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override void InternalDetermineBinding(Plan APlan)
		{
			Nodes[0].DetermineBinding(APlan);
			APlan.EnterRowContext();
			try
			{
				APlan.Symbols.Push(new Symbol(SourceTableType.RowType));
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
		
		public override object InternalExecute(Program AProgram)
		{
			ExtendTable LTable = new ExtendTable(this, AProgram);
			try
			{
				LTable.Open();
				return LTable;
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
		
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			bool LChanged = false;
			if ((AColumnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(AColumnName)))
				LChanged = base.InternalDefault(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending);
			return InternalInternalChange(ANewRow, AColumnName, AProgram, true) || LChanged;
		}
		
		protected bool InternalInternalChange(Row ARow, string AColumnName, Program AProgram, bool AIsDefault)
		{
			bool LChanged = false;
			PushRow(AProgram, ARow);
			try
			{
				// Evaluate the Extended columns
				// TODO: This change code should only run if the column changing can be determined to affect the extended columns...
				int LColumnIndex;
				for (int LIndex = 1; LIndex < Nodes.Count; LIndex++)
				{
					Schema.TableVarColumn LColumn = TableVar.Columns[FExtendColumnOffset + LIndex - 1];
					if ((AIsDefault || LColumn.IsComputed) && (!AProgram.ServerProcess.ServerSession.Server.IsRepository || LColumn.IsChangeRemotable))
					{
						LColumnIndex = ARow.DataType.Columns.IndexOfName(LColumn.Column.Name);
						if (LColumnIndex >= 0)
						{
							if (!AIsDefault || !ARow.HasValue(LColumnIndex))
							{
								ARow[LColumnIndex] = Nodes[LIndex].Execute(AProgram);
								LChanged = true;
							}
						}
					}
				}
				return LChanged;
			}
			finally
			{
				PopRow(AProgram);
			}
		}
		
		protected override bool InternalChange(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName)
		{
			bool LChanged = false;
			if ((AColumnName == String.Empty) || (SourceNode.DataType.Columns.ContainsName(AColumnName)))
				LChanged = base.InternalChange(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
			return InternalInternalChange(ANewRow, AColumnName, AProgram, false) || LChanged;
		}
		
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if ((AColumnName == String.Empty) || SourceNode.DataType.Columns.ContainsName(AColumnName))
				return base.InternalValidate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
			return false;
		}
		
		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			// Exclude any columns from AKey which were included by this node
			Schema.RowType LRowType = new Schema.RowType();
			foreach (Schema.Column LColumn in ARow.DataType.Columns)
				if (SourceNode.DataType.Columns.ContainsName(LColumn.Name))
					LRowType.Columns.Add(LColumn.Copy());

			Row LRow = new Row(AProgram.ValueManager, LRowType);
			try
			{
				ARow.CopyTo(LRow);
				SourceNode.JoinApplicationTransaction(AProgram, LRow);
			}
			finally
			{
				LRow.Dispose();
			}
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