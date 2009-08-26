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

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;	
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Schema = Alphora.Dataphor.DAE.Schema;

	// operator Adorn(table) : table()
	public class AdornNode : UnaryTableNode
	{
		private AdornColumnExpressions FExpressions;		
		public AdornColumnExpressions Expressions
		{
			get { return FExpressions; }
			set { FExpressions = value; }
		}
		
		private CreateConstraintDefinitions FConstraints;
		public CreateConstraintDefinitions Constraints
		{
			get { return FConstraints; }
			set { FConstraints = value; }
		}
		
		private OrderDefinitions FOrders;
		public OrderDefinitions Orders
		{
			get { return FOrders; }
			set { FOrders = value; }
		}
		
		private AlterOrderDefinitions FAlterOrders;
		public AlterOrderDefinitions AlterOrders
		{
			get { return FAlterOrders; }
			set { FAlterOrders = value; }
		}
		
		private DropOrderDefinitions FDropOrders;
		public DropOrderDefinitions DropOrders
		{
			get { return FDropOrders; }
			set { FDropOrders = value; }
		}
		
		private KeyDefinitions FKeys;
		public KeyDefinitions Keys
		{
			get { return FKeys; }
			set { FKeys = value; }
		}
		
		private AlterKeyDefinitions FAlterKeys;
		public AlterKeyDefinitions AlterKeys
		{
			get { return FAlterKeys; }
			set { FAlterKeys = value; }
		}
		
		private DropKeyDefinitions FDropKeys;
		public DropKeyDefinitions DropKeys
		{
			get { return FDropKeys; }
			set { FDropKeys = value; }
		}
		
		protected ReferenceDefinitions FReferences;
		public ReferenceDefinitions References 
		{ 
			get { return FReferences; } 
			set { FReferences = value; }
		}

		private AlterReferenceDefinitions FAlterReferences;
		public AlterReferenceDefinitions AlterReferences
		{
			get { return FAlterReferences; }
			set { FAlterReferences = value; }
		}
		
		private DropReferenceDefinitions FDropReferences;
		public DropReferenceDefinitions DropReferences
		{
			get { return FDropReferences; }
			set { FDropReferences = value; }
		}
		
		private MetaData FMetaData;
		public MetaData MetaData
		{
			get { return FMetaData; }
			set { FMetaData = value; }
		}
		
		private AlterMetaData FAlterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return FAlterMetaData; }
			set { FAlterMetaData = value; }
		}
		
		private bool FIsRestrict;
		public bool IsRestrict
		{
			get { return FIsRestrict; }
			set { FIsRestrict = value; }
		}
		
		private PlanNode ReplaceColumnReferences(Plan APlan, PlanNode ANode, string AIdentifier, int AColumnIndex)
		{
			if (ANode is StackReferenceNode)
			{
				#if USECOLUMNLOCATIONBINDING
				return new StackColumnReferenceNode(AIdentifier, ANode.DataType, ((StackReferenceNode)ANode).Location, AColumnIndex);
				#else
				return new StackColumnReferenceNode(AIdentifier, ANode.DataType, ((StackReferenceNode)ANode).Location);
				#endif
			}
			else
			{
				for (int LIndex = 0; LIndex < ANode.NodeCount; LIndex++)
					ANode.Nodes[LIndex] = ReplaceColumnReferences(APlan, ANode.Nodes[LIndex], AIdentifier, AColumnIndex);
				return ANode;
			}
		}
		
		public override void DetermineDataType(Plan APlan)
		{
			DetermineModifiers(APlan);
			FDataType = new Schema.TableType();
			FTableVar = new Schema.ResultTableVar(this);
			FTableVar.Owner = APlan.User;
			FTableVar.InheritMetaData(SourceTableVar.MetaData);
			FTableVar.MergeMetaData(MetaData);
			AlterNode.AlterMetaData(FTableVar, AlterMetaData, true);
			CopyTableVarColumns(SourceTableVar.Columns);
			int LSourceColumnIndex;
			Schema.TableVarColumn LSourceColumn;
			Schema.TableVarColumn LNewColumn;
			FIsRestrict = false;
			PlanNode LRestrictNode = null;
			PlanNode LConstraintNode;

			foreach (AdornColumnExpression LExpression in FExpressions)
			{
				LSourceColumnIndex = TableVar.Columns.IndexOf(LExpression.ColumnName);
				LSourceColumn = TableVar.Columns[LExpression.ColumnName];
				LNewColumn = CopyTableVarColumn(LSourceColumn);
				if (LExpression.ChangeNilable)
					LNewColumn.IsNilable = LExpression.IsNilable;
				LNewColumn.MergeMetaData(LExpression.MetaData);
				AlterNode.AlterMetaData(LNewColumn, LExpression.AlterMetaData, true);
				LNewColumn.ReadOnly = Convert.ToBoolean(MetaData.GetTag(LNewColumn.MetaData, "Frontend.ReadOnly", LNewColumn.ReadOnly.ToString()));

				APlan.Symbols.Push(new Symbol(Keywords.Value, LNewColumn.DataType));
				try
				{
					foreach (ConstraintDefinition LConstraint in LExpression.Constraints)
					{
						FIsRestrict = true;
						Schema.TableVarColumnConstraint LNewConstraint = new Schema.TableVarColumnConstraint(Schema.Object.GetObjectID(LConstraint.MetaData), LConstraint.ConstraintName);
						LNewConstraint.ConstraintType = Schema.ConstraintType.Column;
						LNewConstraint.MergeMetaData(LConstraint.MetaData);
						APlan.PushCreationObject(LNewConstraint);
						try
						{
							PlanNode LNode = Compiler.CompileBooleanExpression(APlan, LConstraint.Expression);
							LNewConstraint.Node = LNode;
							LNewConstraint.IsRemotable = true;
							if (LNewConstraint.HasDependencies())
								for (int LIndex = 0; LIndex < LNewConstraint.Dependencies.Count; LIndex++)
								{
									Schema.Object LObject = LNewConstraint.Dependencies.Objects[LIndex];
									if (LObject != null)
									{
										if (!LObject.IsRemotable)
										{
											LNewConstraint.IsRemotable = false;
											break;
										}
									}
									else
									{
										Schema.ObjectHeader LHeader = APlan.CatalogDeviceSession.SelectObjectHeader(LNewConstraint.Dependencies.IDs[LIndex]);
										if (!LHeader.IsRemotable)
										{
											LNewConstraint.IsRemotable = false;
											break;
										}
									}
								}

							LNewColumn.Constraints.Add(LNewConstraint);
							
							LConstraintNode = Compiler.CompileBooleanExpression(APlan, LConstraint.Expression);
							LConstraintNode = ReplaceColumnReferences(APlan, LConstraintNode, LSourceColumn.Name, LSourceColumnIndex);
							if (LRestrictNode == null)
								LRestrictNode = LConstraintNode;
							else
								LRestrictNode = Compiler.EmitBinaryNode(APlan, LRestrictNode, Instructions.And, LConstraintNode);
						}
						finally
						{
							APlan.PopCreationObject();
						}
						
						if (LNewConstraint.HasDependencies())
							APlan.AttachDependencies(LNewConstraint.Dependencies);
					}
				}
				finally
				{
					APlan.Symbols.Pop();
				}
				
				// TODO: verify that the default satisfies the constraints
				if (LExpression.Default != null)
				{
					LNewColumn.Default = Compiler.CompileTableVarColumnDefault(APlan, FTableVar, LNewColumn, LExpression.Default);
					if (LNewColumn.Default.HasDependencies())
						APlan.AttachDependencies(LNewColumn.Default.Dependencies);
				}
				
				if (LExpression.MetaData != null)
				{
					Tag LTag;
					LTag = LExpression.MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
					if (LTag != Tag.None)
						LNewColumn.IsDefaultRemotable = LNewColumn.IsDefaultRemotable && Convert.ToBoolean(LTag.Value);
						
					LTag = LExpression.MetaData.Tags.GetTag("DAE.IsChangeRemotable");
					if (LTag != Tag.None)
						LNewColumn.IsChangeRemotable = LNewColumn.IsChangeRemotable && Convert.ToBoolean(LTag.Value);
						
					LTag = LExpression.MetaData.Tags.GetTag("DAE.IsValidateRemotable");
					if (LTag != Tag.None)
						LNewColumn.IsValidateRemotable = LNewColumn.IsValidateRemotable && Convert.ToBoolean(LTag.Value);
				}

				DataType.Columns[LSourceColumnIndex] = LNewColumn.Column;
				TableVar.Columns[LSourceColumnIndex] = LNewColumn;
			}
			
			// Keys
			CopyKeys(SourceTableVar.Keys);
			foreach (DropKeyDefinition LKeyDefinition in FDropKeys)
			{
				Schema.Key LOldKey = Compiler.FindKey(APlan, TableVar, LKeyDefinition);

				TableVar.Keys.SafeRemove(LOldKey);
				TableVar.Constraints.SafeRemove(LOldKey.Constraint);
				TableVar.InsertConstraints.SafeRemove(LOldKey.Constraint);
				TableVar.UpdateConstraints.SafeRemove(LOldKey.Constraint);
			}

			foreach (AlterKeyDefinition LKeyDefinition in FAlterKeys)
			{
				Schema.Key LOldKey = Compiler.FindKey(APlan, TableVar, LKeyDefinition);
				AlterNode.AlterMetaData(LOldKey, LKeyDefinition.AlterMetaData);
			}

			Compiler.CompileTableVarKeys(APlan, FTableVar, FKeys, false);

			// Orders
			CopyOrders(SourceTableVar.Orders);
				
			foreach (DropOrderDefinition LOrderDefinition in FDropOrders)
			{
				Schema.Order LOldOrder = Compiler.FindOrder(APlan, TableVar, LOrderDefinition);

				TableVar.Orders.SafeRemove(LOldOrder);
			}

			foreach (AlterOrderDefinition LOrderDefinition in FAlterOrders)
				AlterNode.AlterMetaData(Compiler.FindOrder(APlan, TableVar, LOrderDefinition), LOrderDefinition.AlterMetaData);

			Compiler.CompileTableVarOrders(APlan, FTableVar, FOrders);

			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);

			// Constraints
			Compiler.CompileTableVarConstraints(APlan, FTableVar, FConstraints);

			foreach (Schema.TableVarConstraint LConstraint in FTableVar.Constraints)
			{
				if (LRestrictNode == null)
				{
					if (LConstraint is Schema.RowConstraint)
					{
						LRestrictNode = ((Schema.RowConstraint)LConstraint).Node;
						FIsRestrict = true;
					}
				}
				else
				{
					if (LConstraint is Schema.RowConstraint)
					{
						LRestrictNode = Compiler.EmitBinaryNode(APlan, LRestrictNode, Instructions.And, ((Schema.RowConstraint)LConstraint).Node);
						FIsRestrict = true;
					}
				}
				
				if (LConstraint.HasDependencies())			
					APlan.AttachDependencies(LConstraint.Dependencies);
			}
				
			if (FIsRestrict)
				Nodes[0] = Compiler.EmitRestrictNode(APlan, Nodes[0], LRestrictNode);
				
			DetermineRemotable(APlan);

			if (MetaData != null)
			{
				Tag LTag;
				Schema.ResultTableVar LTableVar = (Schema.ResultTableVar)TableVar;
				LTag = MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
				if (LTag != Tag.None)
					LTableVar.InferredIsDefaultRemotable = LTableVar.InferredIsDefaultRemotable && Convert.ToBoolean(LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DAE.IsChangeRemotable");
				if (LTag != Tag.None)
					LTableVar.InferredIsChangeRemotable = LTableVar.InferredIsChangeRemotable && Convert.ToBoolean(LTag.Value);
					
				LTag = MetaData.Tags.GetTag("DAE.IsValidateRemotable");
				if (LTag != Tag.None)
					LTableVar.InferredIsValidateRemotable = LTableVar.InferredIsValidateRemotable && Convert.ToBoolean(LTag.Value);
			}

			if (Order == null)
			{
				string LOrderName = MetaData.GetTag(MetaData, "DAE.DefaultOrder", String.Empty);
				if (LOrderName != String.Empty)
					Order = 
						Compiler.CompileOrderDefinition
						(
							APlan, 
							TableVar, 
							new Parser().ParseOrderDefinition
							(
								MetaData.GetTag
								(
									MetaData, 
									"DAE.DefaultOrder", 
									String.Empty
								)
							),
							false
						);
			}
			
			if ((Order != null) && !TableVar.Orders.Contains(Order))
				TableVar.Orders.Add(Order);
			
			#if UseReferenceDerivation
			CopySourceReferences(APlan, SourceNode.TableVar.SourceReferences);
			CopyTargetReferences(APlan, SourceNode.TableVar.TargetReferences);
			#endif
			
			foreach (ReferenceDefinition LReferenceDefinition in FReferences)
			{
				// Create a reference on the table var
				Schema.Reference LReference = new Schema.Reference(Schema.Object.GetObjectID(LReferenceDefinition.MetaData), LReferenceDefinition.ReferenceName, LReferenceDefinition.MetaData);
				LReference.Enforced = false;
				LReference.SourceTable = TableVar;
				
				foreach (ReferenceColumnDefinition LColumn in LReferenceDefinition.Columns)
					LReference.SourceKey.Columns.Add(LReference.SourceTable.Columns[LColumn.ColumnName]);
				foreach (Schema.Key LKey in LReference.SourceTable.Keys)
					if (LReference.SourceKey.Columns.IsSupersetOf(LKey.Columns))
					{
						LReference.SourceKey.IsUnique = true;
						break;
					}
				
				Schema.Object LSchemaObject = Compiler.ResolveCatalogIdentifier(APlan, LReferenceDefinition.ReferencesDefinition.TableVarName, true);
				if (!(LSchemaObject is Schema.TableVar))
					throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, LReferenceDefinition, LReferenceDefinition.ReferenceName, LReferenceDefinition.ReferencesDefinition.TableVarName);
				if (LSchemaObject.IsATObject)
					LReferenceDefinition.ReferencesDefinition.TableVarName = Schema.Object.EnsureRooted(((Schema.TableVar)LSchemaObject).SourceTableName);
				else
					LReferenceDefinition.ReferencesDefinition.TableVarName = Schema.Object.EnsureRooted(LSchemaObject.Name); // Set the TableVarName in the references expression to the resolved identifier so that subsequent compiles do not depend on current library context (This really only matters in remote contexts, but there it is imperative, or this could be an ambiguous identifier)
				APlan.AttachDependency(LSchemaObject);
				LReference.TargetTable = (Schema.TableVar)LSchemaObject;
				LReference.AddDependency(LSchemaObject);
				
				foreach (ReferenceColumnDefinition LColumn in LReferenceDefinition.ReferencesDefinition.Columns)
					LReference.TargetKey.Columns.Add(LReference.TargetTable.Columns[LColumn.ColumnName]);
				foreach (Schema.Key LKey in LReference.TargetTable.Keys)
					if (LReference.TargetKey.Columns.IsSupersetOf(LKey.Columns))
					{
						LReference.TargetKey.IsUnique = true;
						break;
					}
					
				if (!LReference.TargetKey.IsUnique)
					throw new CompilerException(CompilerException.Codes.ReferenceMustTargetKey, LReferenceDefinition, LReferenceDefinition.ReferenceName, LReferenceDefinition.ReferencesDefinition.TableVarName);
					
				if (LReference.SourceKey.Columns.Count != LReference.TargetKey.Columns.Count)
					throw new CompilerException(CompilerException.Codes.InvalidReferenceColumnCount, LReferenceDefinition, LReferenceDefinition.ReferenceName);

				TableVar.SourceReferences.Add(LReference);
				TableVar.DerivedReferences.Add(LReference);					
			}

			if (!APlan.IsRepository)
				foreach (AlterReferenceDefinition LAlterReference in FAlterReferences)
				{
					int LReferenceIndex = TableVar.DerivedReferences.IndexOf(LAlterReference.ReferenceName);
					if (LReferenceIndex < 0)
						LReferenceIndex = TableVar.DerivedReferences.IndexOfOriginatingReference(LAlterReference.ReferenceName);
						
					if 
					(
						(LReferenceIndex >= 0) || 
						(
							(APlan.ApplicationTransactionID == Guid.Empty) && 
							(!APlan.InLoadingContext()) && 
							!APlan.InATCreationContext // We will be in an A/T creation context if we are reinfering view references for an A/T view
						)
					)
					{
						Schema.Reference LReferenceToAlter;
						if (LReferenceIndex < 0)
							LReferenceToAlter = TableVar.DerivedReferences[LAlterReference.ReferenceName]; // This is just to throw the object not found error
						else
							LReferenceToAlter = TableVar.DerivedReferences[LReferenceIndex];
						AlterNode.AlterMetaData(LReferenceToAlter, LAlterReference.AlterMetaData);
						Schema.Object LOriginatingReference = Compiler.ResolveCatalogIdentifier(APlan, LReferenceToAlter.OriginatingReferenceName(), false);
						if (LOriginatingReference != null)
							APlan.AttachDependency(LOriginatingReference);
					}
				}
				
			foreach (DropReferenceDefinition LDropReference in FDropReferences)
			{
				int LReferenceIndex = TableVar.DerivedReferences.IndexOf(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.DerivedReferences.RemoveAt(LReferenceIndex);
					
				LReferenceIndex = TableVar.DerivedReferences.IndexOfOriginatingReference(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.DerivedReferences.RemoveAt(LReferenceIndex);
					
				LReferenceIndex = TableVar.SourceReferences.IndexOf(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.SourceReferences.RemoveAt(LReferenceIndex);

				LReferenceIndex = TableVar.SourceReferences.IndexOfOriginatingReference(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.SourceReferences.RemoveAt(LReferenceIndex);

				LReferenceIndex = TableVar.TargetReferences.IndexOf(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.TargetReferences.RemoveAt(LReferenceIndex);

				LReferenceIndex = TableVar.TargetReferences.IndexOfOriginatingReference(LDropReference.ReferenceName);
				if (LReferenceIndex >= 0)
					TableVar.TargetReferences.RemoveAt(LReferenceIndex);
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
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			AdornExpression LExpression = new AdornExpression();
			LExpression.Expression = (Expression)Nodes[0].EmitStatement(AMode);
			LExpression.MetaData = MetaData == null ? null : MetaData.Copy();
			LExpression.AlterMetaData = AlterMetaData;
			LExpression.Expressions.AddRange(FExpressions);
			LExpression.Constraints.AddRange(FConstraints);
			LExpression.Orders.AddRange(FOrders);
			LExpression.AlterOrders.AddRange(FAlterOrders);
			LExpression.DropOrders.AddRange(FDropOrders);
			LExpression.Keys.AddRange(FKeys);
			LExpression.AlterKeys.AddRange(FAlterKeys);
			LExpression.DropKeys.AddRange(FDropKeys);
			LExpression.References.AddRange(FReferences);
			LExpression.AlterReferences.AddRange(FAlterReferences);
			LExpression.DropReferences.AddRange(FDropReferences);
			LExpression.Modifiers = Modifiers;
			return LExpression;
		}
		
		public override object InternalExecute(Program AProgram)
		{
			AdornTable LTable = new AdornTable(this, AProgram);
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
		
		protected void InternalValidateColumnConstraints(Row ARow, Schema.TableVarColumn AColumn, Program AProgram)
		{
			AProgram.Stack.Push(ARow[AColumn.Name]);
			try
			{
				foreach (Schema.TableVarColumnConstraint LConstraint in AColumn.Constraints)
					LConstraint.Validate(AProgram, Schema.Transition.Insert);
			}
			finally
			{
				AProgram.Stack.Pop();
			}
		}
		
		// Validate
		protected override bool InternalValidate(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending, bool AIsProposable)
		{
			if (AColumnName == String.Empty)
			{
				PushNewRow(AProgram, ANewRow);
				try
				{
					foreach (Schema.TableVarColumn LColumn in TableVar.Columns)
					{
						if (LColumn.Constraints.Count > 0)
							InternalValidateColumnConstraints(ANewRow, LColumn, AProgram);
					}
				}
				finally
				{
					PopRow(AProgram);
				}
			}
			else
			{
				Schema.TableVarColumn LColumn = TableVar.Columns[TableVar.Columns.IndexOfName(AColumnName)];
				if ((LColumn.Constraints.Count > 0) && ANewRow.HasValue(LColumn.Name))
					InternalValidateColumnConstraints(ANewRow, LColumn, AProgram);
			}

			return base.InternalValidate(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending, AIsProposable);
		}
		
		protected bool InternalDefaultColumn(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			int LColumnIndex = TableVar.Columns.IndexOfName(AColumnName);
			for (int LIndex = 0; LIndex < FExpressions.Count; LIndex++)
				if (TableVar.Columns.IndexOf(FExpressions[LIndex].ColumnName) == LColumnIndex)
					if (FExpressions[LIndex].Default != null)
					{
						bool LChanged = DefaultColumn(AProgram, TableVar, ANewRow, AValueFlags, AColumnName);
						if (AIsDescending && LChanged)
							Change(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName);
						return LChanged;
					}
					else
						return false;
			return false;
		}
		
		// Default
		protected override bool InternalDefault(Program AProgram, Row AOldRow, Row ANewRow, BitArray AValueFlags, string AColumnName, bool AIsDescending)
		{
			bool LChanged = false;

			if (AColumnName != String.Empty)
				LChanged = InternalDefaultColumn(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending) || LChanged;
			else
				for (int LIndex = 0; LIndex < ANewRow.DataType.Columns.Count; LIndex++)
					LChanged = InternalDefaultColumn(AProgram, AOldRow, ANewRow, AValueFlags, ANewRow.DataType.Columns[LIndex].Name, AIsDescending) || LChanged;

			if (AIsDescending && PropagateDefault)
				LChanged = base.InternalDefault(AProgram, AOldRow, ANewRow, AValueFlags, AColumnName, AIsDescending) || LChanged;

			return LChanged;
		}

		public override void JoinApplicationTransaction(Program AProgram, Row ARow)
		{
			// Safe to pass null for AOldRow only if AIsDescending is false
			InternalDefault(AProgram, null, ARow, null, String.Empty, false);
			base.JoinApplicationTransaction(AProgram, ARow);
		}
	}
}