/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define UseReferenceDerivation
#define UseElaborable
	
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
		private AdornColumnExpressions _expressions;		
		public AdornColumnExpressions Expressions
		{
			get { return _expressions; }
			set { _expressions = value; }
		}
		
		private CreateConstraintDefinitions _constraints;
		public CreateConstraintDefinitions Constraints
		{
			get { return _constraints; }
			set { _constraints = value; }
		}
		
		private OrderDefinitions _orders;
		public OrderDefinitions Orders
		{
			get { return _orders; }
			set { _orders = value; }
		}
		
		private AlterOrderDefinitions _alterOrders;
		public AlterOrderDefinitions AlterOrders
		{
			get { return _alterOrders; }
			set { _alterOrders = value; }
		}
		
		private DropOrderDefinitions _dropOrders;
		public DropOrderDefinitions DropOrders
		{
			get { return _dropOrders; }
			set { _dropOrders = value; }
		}
		
		private KeyDefinitions _keys;
		public KeyDefinitions Keys
		{
			get { return _keys; }
			set { _keys = value; }
		}
		
		private AlterKeyDefinitions _alterKeys;
		public AlterKeyDefinitions AlterKeys
		{
			get { return _alterKeys; }
			set { _alterKeys = value; }
		}
		
		private DropKeyDefinitions _dropKeys;
		public DropKeyDefinitions DropKeys
		{
			get { return _dropKeys; }
			set { _dropKeys = value; }
		}
		
		protected ReferenceDefinitions _references;
		public ReferenceDefinitions References 
		{ 
			get { return _references; } 
			set { _references = value; }
		}

		private AlterReferenceDefinitions _alterReferences;
		public AlterReferenceDefinitions AlterReferences
		{
			get { return _alterReferences; }
			set { _alterReferences = value; }
		}
		
		private DropReferenceDefinitions _dropReferences;
		public DropReferenceDefinitions DropReferences
		{
			get { return _dropReferences; }
			set { _dropReferences = value; }
		}
		
		private MetaData _metaData;
		public MetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}
		
		private AlterMetaData _alterMetaData;
		public AlterMetaData AlterMetaData
		{
			get { return _alterMetaData; }
			set { _alterMetaData = value; }
		}
		
		private bool _isRestrict;
		public bool IsRestrict
		{
			get { return _isRestrict; }
			set { _isRestrict = value; }
		}
		
		private PlanNode ReplaceColumnReferences(Plan plan, PlanNode node, string identifier, int columnIndex)
		{
			if (node is StackReferenceNode)
			{
				#if USECOLUMNLOCATIONBINDING
				return new StackColumnReferenceNode(AIdentifier, ANode.DataType, ((StackReferenceNode)ANode).Location, AColumnIndex);
				#else
				return new StackColumnReferenceNode(identifier, node.DataType, ((StackReferenceNode)node).Location);
				#endif
			}
			else
			{
				for (int index = 0; index < node.NodeCount; index++)
					node.Nodes[index] = ReplaceColumnReferences(plan, node.Nodes[index], identifier, columnIndex);
				return node;
			}
		}
		
		public override void DetermineDataType(Plan plan)
		{
			DetermineModifiers(plan);
			_dataType = new Schema.TableType();
			_tableVar = new Schema.ResultTableVar(this);
			_tableVar.Owner = plan.User;
			_tableVar.InheritMetaData(SourceTableVar.MetaData);
			_tableVar.MergeMetaData(MetaData);
			AlterNode.AlterMetaData(_tableVar, AlterMetaData, true);
			CopyTableVarColumns(SourceTableVar.Columns);
			int sourceColumnIndex;
			Schema.TableVarColumn sourceColumn;
			Schema.TableVarColumn newColumn;
			_isRestrict = false;
			PlanNode restrictNode = null;
			PlanNode constraintNode;

			foreach (AdornColumnExpression expression in _expressions)
			{
				sourceColumnIndex = TableVar.Columns.IndexOf(expression.ColumnName);
				sourceColumn = TableVar.Columns[expression.ColumnName];
				newColumn = CopyTableVarColumn(sourceColumn);
				if (expression.ChangeNilable)
					newColumn.IsNilable = expression.IsNilable;
				newColumn.MergeMetaData(expression.MetaData);
				AlterNode.AlterMetaData(newColumn, expression.AlterMetaData, true);
				newColumn.ReadOnly = Convert.ToBoolean(MetaData.GetTag(newColumn.MetaData, "Frontend.ReadOnly", newColumn.ReadOnly.ToString()));

				foreach (ConstraintDefinition constraint in expression.Constraints)
				{
					_isRestrict = true;
					Schema.TableVarColumnConstraint newConstraint = Compiler.CompileTableVarColumnConstraint(plan, TableVar, newColumn, constraint);

					//Schema.TableVarColumnConstraint newConstraint = new Schema.TableVarColumnConstraint(Schema.Object.GetObjectID(constraint.MetaData), constraint.ConstraintName);
					//newConstraint.ConstraintType = Schema.ConstraintType.Column;
					//newConstraint.MergeMetaData(constraint.MetaData);
					plan.PushCreationObject(newConstraint);
					try
					{
						plan.Symbols.Push(new Symbol(Keywords.Value, newColumn.DataType));
						try
						{
							//PlanNode node = Compiler.CompileBooleanExpression(plan, constraint.Expression);
							//newConstraint.Node = node;
							//newConstraint.IsRemotable = true;
							//if (newConstraint.HasDependencies())
							//	for (int index = 0; index < newConstraint.Dependencies.Count; index++)
							//	{
							//		Schema.Object objectValue = newConstraint.Dependencies.Objects[index];
							//		if (objectValue != null)
							//		{
							//			if (!objectValue.IsRemotable)
							//			{
							//				newConstraint.IsRemotable = false;
							//				break;
							//			}
							//		}
							//		else
							//		{
							//			Error.Fail("Missing object dependency in AdornNode.");
							//			//Schema.ObjectHeader LHeader = APlan.CatalogDeviceSession.SelectObjectHeader(LNewConstraint.Dependencies.IDs[LIndex]);
							//			//if (!LHeader.IsRemotable)
							//			//{
							//			//    LNewConstraint.IsRemotable = false;
							//			//    break;
							//			//}
							//		}
							//	}

							newColumn.Constraints.Add(newConstraint);
							
							constraintNode = Compiler.CompileBooleanExpression(plan, constraint.Expression);
							constraintNode = ReplaceColumnReferences(plan, constraintNode, sourceColumn.Name, sourceColumnIndex);
							if (restrictNode == null)
								restrictNode = constraintNode;
							else
								restrictNode = Compiler.EmitBinaryNode(plan, restrictNode, Instructions.And, constraintNode);
						}
						finally
						{
							plan.Symbols.Pop();
						}
					}
					finally
					{
						plan.PopCreationObject();
					}
						
					if (newConstraint.HasDependencies())
						plan.AttachDependencies(newConstraint.Dependencies);
				}
				
				// TODO: verify that the default satisfies the constraints
				if (expression.Default != null)
				{
					newColumn.Default = Compiler.CompileTableVarColumnDefault(plan, _tableVar, newColumn, expression.Default);
					if (newColumn.Default.HasDependencies())
						plan.AttachDependencies(newColumn.Default.Dependencies);
				}
				
				if (expression.MetaData != null)
				{
					Tag tag;
					tag = expression.MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
					if (tag != Tag.None)
						newColumn.IsDefaultRemotable = newColumn.IsDefaultRemotable && Convert.ToBoolean(tag.Value);
						
					tag = expression.MetaData.Tags.GetTag("DAE.IsChangeRemotable");
					if (tag != Tag.None)
						newColumn.IsChangeRemotable = newColumn.IsChangeRemotable && Convert.ToBoolean(tag.Value);
						
					tag = expression.MetaData.Tags.GetTag("DAE.IsValidateRemotable");
					if (tag != Tag.None)
						newColumn.IsValidateRemotable = newColumn.IsValidateRemotable && Convert.ToBoolean(tag.Value);
				}

				DataType.Columns[sourceColumnIndex] = newColumn.Column;
				TableVar.Columns[sourceColumnIndex] = newColumn;
			}
			
			// Keys
			CopyKeys(SourceTableVar.Keys);
			foreach (DropKeyDefinition keyDefinition in _dropKeys)
			{
				Schema.Key oldKey = Compiler.FindKey(plan, TableVar, keyDefinition);

				TableVar.Keys.SafeRemove(oldKey);
				TableVar.Constraints.SafeRemove(oldKey.Constraint);
				TableVar.InsertConstraints.SafeRemove(oldKey.Constraint);
				TableVar.UpdateConstraints.SafeRemove(oldKey.Constraint);
			}

			foreach (AlterKeyDefinition keyDefinition in _alterKeys)
			{
				Schema.Key oldKey = Compiler.FindKey(plan, TableVar, keyDefinition);
				AlterNode.AlterMetaData(oldKey, keyDefinition.AlterMetaData);
			}

			Compiler.CompileTableVarKeys(plan, _tableVar, _keys, false);

			// Orders
			CopyOrders(SourceTableVar.Orders);
				
			foreach (DropOrderDefinition orderDefinition in _dropOrders)
			{
				Schema.Order oldOrder = Compiler.FindOrder(plan, TableVar, orderDefinition);

				TableVar.Orders.SafeRemove(oldOrder);
			}

			foreach (AlterOrderDefinition orderDefinition in _alterOrders)
				AlterNode.AlterMetaData(Compiler.FindOrder(plan, TableVar, orderDefinition), orderDefinition.AlterMetaData);

			Compiler.CompileTableVarOrders(plan, _tableVar, _orders);

			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);

			// Constraints
			Compiler.CompileTableVarConstraints(plan, _tableVar, _constraints);

			if (_tableVar.HasConstraints())
			{
				foreach (Schema.TableVarConstraint constraint in _tableVar.Constraints)
				{
					if (restrictNode == null)
					{
						if (constraint is Schema.RowConstraint)
						{
							restrictNode = ((Schema.RowConstraint)constraint).Node;
							_isRestrict = true;
						}
					}
					else
					{
						if (constraint is Schema.RowConstraint)
						{
							restrictNode = Compiler.EmitBinaryNode(plan, restrictNode, Instructions.And, ((Schema.RowConstraint)constraint).Node);
							_isRestrict = true;
						}
					}
				
					if (constraint.HasDependencies())			
						plan.AttachDependencies(constraint.Dependencies);
				}
			}
				
			if (_isRestrict)
				Nodes[0] = Compiler.EmitRestrictNode(plan, Nodes[0], restrictNode);
				
			DetermineRemotable(plan);

			if (MetaData != null)
			{
				Tag tag;
				Schema.ResultTableVar tableVar = (Schema.ResultTableVar)TableVar;
				tag = MetaData.Tags.GetTag("DAE.IsDefaultRemotable");
				if (tag != Tag.None)
					tableVar.InferredIsDefaultRemotable = tableVar.InferredIsDefaultRemotable && Convert.ToBoolean(tag.Value);
					
				tag = MetaData.Tags.GetTag("DAE.IsChangeRemotable");
				if (tag != Tag.None)
					tableVar.InferredIsChangeRemotable = tableVar.InferredIsChangeRemotable && Convert.ToBoolean(tag.Value);
					
				tag = MetaData.Tags.GetTag("DAE.IsValidateRemotable");
				if (tag != Tag.None)
					tableVar.InferredIsValidateRemotable = tableVar.InferredIsValidateRemotable && Convert.ToBoolean(tag.Value);
			}

			if (Order == null)
			{
				string orderName = MetaData.GetTag(MetaData, "DAE.DefaultOrder", String.Empty);
				if (orderName != String.Empty)
					Order = 
						Compiler.CompileOrderDefinition
						(
							plan, 
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
			#if UseElaborable
			if (plan.CursorContext.CursorCapabilities.HasFlag(CursorCapability.Elaborable))
			#endif
				CopyReferences(plan, SourceTableVar);
			#endif
			
			foreach (ReferenceDefinition referenceDefinition in _references)
			{
				// Create a reference on the table var
				Schema.Reference reference = new Schema.Reference(Schema.Object.GetObjectID(referenceDefinition.MetaData), referenceDefinition.ReferenceName, referenceDefinition.MetaData);
				reference.Enforced = false;
				reference.SourceTable = TableVar;
				
				foreach (ReferenceColumnDefinition column in referenceDefinition.Columns)
					reference.SourceKey.Columns.Add(reference.SourceTable.Columns[column.ColumnName]);
				foreach (Schema.Key key in reference.SourceTable.Keys)
					if (reference.SourceKey.Columns.IsSupersetOf(key.Columns))
					{
						reference.SourceKey.IsUnique = true;
						break;
					}
				
				Schema.Object schemaObject = Compiler.ResolveCatalogIdentifier(plan, referenceDefinition.ReferencesDefinition.TableVarName, true);
				if (!(schemaObject is Schema.TableVar))
					throw new CompilerException(CompilerException.Codes.InvalidReferenceObject, referenceDefinition, referenceDefinition.ReferenceName, referenceDefinition.ReferencesDefinition.TableVarName);
				if (schemaObject.IsATObject)
					referenceDefinition.ReferencesDefinition.TableVarName = Schema.Object.EnsureRooted(((Schema.TableVar)schemaObject).SourceTableName);
				else
					referenceDefinition.ReferencesDefinition.TableVarName = Schema.Object.EnsureRooted(schemaObject.Name); // Set the TableVarName in the references expression to the resolved identifier so that subsequent compiles do not depend on current library context (This really only matters in remote contexts, but there it is imperative, or this could be an ambiguous identifier)
				plan.AttachDependency(schemaObject);
				reference.TargetTable = (Schema.TableVar)schemaObject;
				reference.AddDependency(schemaObject);
				
				foreach (ReferenceColumnDefinition column in referenceDefinition.ReferencesDefinition.Columns)
					reference.TargetKey.Columns.Add(reference.TargetTable.Columns[column.ColumnName]);
				foreach (Schema.Key key in reference.TargetTable.Keys)
					if (reference.TargetKey.Columns.IsSupersetOf(key.Columns))
					{
						reference.TargetKey.IsUnique = true;
						break;
					}
					
				if (!reference.TargetKey.IsUnique)
					throw new CompilerException(CompilerException.Codes.ReferenceMustTargetKey, referenceDefinition, referenceDefinition.ReferenceName, referenceDefinition.ReferencesDefinition.TableVarName);
					
				if (reference.SourceKey.Columns.Count != reference.TargetKey.Columns.Count)
					throw new CompilerException(CompilerException.Codes.InvalidReferenceColumnCount, referenceDefinition, referenceDefinition.ReferenceName);

				TableVar.References.Add(reference);
			}

			if (!plan.IsEngine)
				foreach (AlterReferenceDefinition alterReference in _alterReferences)
				{
					int referenceIndex = TableVar.References.IndexOf(alterReference.ReferenceName);
					if (referenceIndex < 0)
						referenceIndex = TableVar.References.IndexOfOriginatingReference(alterReference.ReferenceName);
						
					if 
					(
						(referenceIndex >= 0) || 
						(
							(plan.ApplicationTransactionID == Guid.Empty) && 
							(!plan.InLoadingContext()) && 
							!plan.InATCreationContext // We will be in an A/T creation context if we are reinfering view references for an A/T view
						)
					)
					{
						Schema.ReferenceBase referenceToAlter;
						if (referenceIndex < 0)
							referenceToAlter = TableVar.References[alterReference.ReferenceName]; // This is just to throw the object not found error
						else
							referenceToAlter = TableVar.References[referenceIndex];
						AlterNode.AlterMetaData(referenceToAlter, alterReference.AlterMetaData);
						Schema.Object originatingReference = Compiler.ResolveCatalogIdentifier(plan, referenceToAlter.OriginatingReferenceName(), false);
						if (originatingReference != null)
							plan.AttachDependency(originatingReference);
					}
				}
				
			foreach (DropReferenceDefinition dropReference in _dropReferences)
			{
				//if (TableVar.HasDerivedReferences())
				//{
				//	int referenceIndex = TableVar.DerivedReferences.IndexOf(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.DerivedReferences.RemoveAt(referenceIndex);
					
				//	referenceIndex = TableVar.DerivedReferences.IndexOfOriginatingReference(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.DerivedReferences.RemoveAt(referenceIndex);
				//}
					
				//if (TableVar.HasSourceReferences())
				//{
				//	int referenceIndex = TableVar.SourceReferences.IndexOf(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.SourceReferences.RemoveAt(referenceIndex);

				//	referenceIndex = TableVar.SourceReferences.IndexOfOriginatingReference(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.SourceReferences.RemoveAt(referenceIndex);
				//}

				//if (TableVar.HasTargetReferences())
				//{
				//	int referenceIndex = TableVar.TargetReferences.IndexOf(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.TargetReferences.RemoveAt(referenceIndex);

				//	referenceIndex = TableVar.TargetReferences.IndexOfOriginatingReference(dropReference.ReferenceName);
				//	if (referenceIndex >= 0)
				//		TableVar.TargetReferences.RemoveAt(referenceIndex);
				//}

				if (TableVar.HasReferences())
				{
					int referenceIndex = TableVar.References.IndexOf(dropReference.ReferenceName);
					if (referenceIndex >= 0)
						TableVar.References.RemoveAt(referenceIndex);

					referenceIndex = TableVar.References.IndexOfOriginatingReference(dropReference.ReferenceName);
					if (referenceIndex >= 0)
						TableVar.References.RemoveAt(referenceIndex);
				}
			}
		}
		
		public override void DetermineCursorBehavior(Plan plan)
		{
			_cursorType = SourceNode.CursorType;
			_requestedCursorType = plan.CursorContext.CursorType;
			_cursorCapabilities = 
				CursorCapability.Navigable | 
				(
					(plan.CursorContext.CursorCapabilities & CursorCapability.Updateable) & 
					(SourceNode.CursorCapabilities & CursorCapability.Updateable)
				) |
				(
					plan.CursorContext.CursorCapabilities & SourceNode.CursorCapabilities & CursorCapability.Elaborable
				);
			_cursorIsolation = plan.CursorContext.CursorIsolation;
			if (SourceNode.Order != null)
				Order = CopyOrder(SourceNode.Order);
			else
				Order = null;
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			AdornExpression expression = new AdornExpression();
			expression.Expression = (Expression)Nodes[0].EmitStatement(mode);
			expression.MetaData = MetaData == null ? null : MetaData.Copy();
			expression.AlterMetaData = AlterMetaData;
			expression.Expressions.AddRange(_expressions);
			expression.Constraints.AddRange(_constraints);
			expression.Orders.AddRange(_orders);
			expression.AlterOrders.AddRange(_alterOrders);
			expression.DropOrders.AddRange(_dropOrders);
			expression.Keys.AddRange(_keys);
			expression.AlterKeys.AddRange(_alterKeys);
			expression.DropKeys.AddRange(_dropKeys);
			expression.References.AddRange(_references);
			expression.AlterReferences.AddRange(_alterReferences);
			expression.DropReferences.AddRange(_dropReferences);
			expression.Modifiers = Modifiers;
			return expression;
		}
		
		public override object InternalExecute(Program program)
		{
			AdornTable table = new AdornTable(this, program);
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
		
		protected void InternalValidateColumnConstraints(IRow row, Schema.TableVarColumn column, Program program)
		{
			program.Stack.Push(row[column.Name]);
			try
			{
				foreach (Schema.TableVarColumnConstraint constraint in column.Constraints)
					constraint.Validate(program, Schema.Transition.Insert);
			}
			finally
			{
				program.Stack.Pop();
			}
		}
		
		// Validate
		protected override bool InternalValidate(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending, bool isProposable)
		{
			if (columnName == String.Empty)
			{
				PushNewRow(program, newRow);
				try
				{
					foreach (Schema.TableVarColumn column in TableVar.Columns)
					{
						if (column.Constraints.Count > 0)
							InternalValidateColumnConstraints(newRow, column, program);
					}
				}
				finally
				{
					PopRow(program);
				}
			}
			else
			{
				Schema.TableVarColumn column = TableVar.Columns[TableVar.Columns.IndexOfName(columnName)];
				if ((column.Constraints.Count > 0) && newRow.HasValue(column.Name))
					InternalValidateColumnConstraints(newRow, column, program);
			}

			return base.InternalValidate(program, oldRow, newRow, valueFlags, columnName, isDescending, isProposable);
		}
		
		protected bool InternalDefaultColumn(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			int columnIndex = TableVar.Columns.IndexOfName(columnName);
			for (int index = 0; index < _expressions.Count; index++)
				if (TableVar.Columns.IndexOf(_expressions[index].ColumnName) == columnIndex)
					if (_expressions[index].Default != null)
					{
						bool changed = DefaultColumn(program, TableVar, newRow, valueFlags, columnName);
						if (isDescending && changed)
							Change(program, oldRow, newRow, valueFlags, columnName);
						return changed;
					}
					else
						return false;
			return false;
		}
		
		// Default
		protected override bool InternalDefault(Program program, IRow oldRow, IRow newRow, BitArray valueFlags, string columnName, bool isDescending)
		{
			bool changed = false;

			if (columnName != String.Empty)
				changed = InternalDefaultColumn(program, oldRow, newRow, valueFlags, columnName, isDescending) || changed;
			else
				for (int index = 0; index < newRow.DataType.Columns.Count; index++)
					changed = InternalDefaultColumn(program, oldRow, newRow, valueFlags, newRow.DataType.Columns[index].Name, isDescending) || changed;

			if (isDescending && PropagateDefault)
				changed = base.InternalDefault(program, oldRow, newRow, valueFlags, columnName, isDescending) || changed;

			return changed;
		}

		public override void JoinApplicationTransaction(Program program, IRow row)
		{
			// Safe to pass null for AOldRow only if AIsDescending is false
			InternalDefault(program, null, row, null, String.Empty, false);
			base.JoinApplicationTransaction(program, row);
		}
	}
}