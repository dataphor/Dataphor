/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

namespace Alphora.Dataphor.DAE.Schema
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using D4 = Alphora.Dataphor.DAE.Language.D4;

	// TODO: Refactor these dependencies
	using Alphora.Dataphor.DAE.Runtime; // Program
	using Alphora.Dataphor.DAE.Runtime.Data; // DataValue
	using Alphora.Dataphor.DAE.Runtime.Instructions; // PlanNode

    public enum ConstraintType 
    { 
		/// <summary>A scalar type constraint is a truth valued expression which limits the set of values in a scalar type.</summary>
		ScalarType, 
		
		/// <summary>A Column constraint is a truth valued expression which limits the set of values on a column.  This constraint functions in addition to the type specification of the column, and is equivalent to a row constraint except that it is evaluable in terms of the column only.</summary>
		Column, 
		
		/// <summary>A Row constraint is a truth valued expression which limits the set of rows permissible in a table.</summary>
		Row, 
		
		/// <summary>A table constraint is a declarative construct which limits the set of rows permissible in a table such as a key.</summary>
		Table, 
		
		/// <summary>A database constraint is a truth valued expression which limits the set of table values permissible in the database.</summary>
		Database 
	}
	
	public enum Transition
	{
		Insert,
		Update,
		Delete
	}

	/// <remarks> Constraint </remarks>
	public abstract class Constraint : Object
    {
		// constructor
		public Constraint(string name) : base(name) {}
		public Constraint(int iD, string name) : base(iD, name) {}
		public Constraint(int iD, string name, MetaData metaData) : base(iD, name)
		{
			MetaData = metaData;
		}

		// ConstraintType
		private ConstraintType _constraintType;
		public ConstraintType ConstraintType
		{
			get { return _constraintType; }
			set { _constraintType = value; }
		}
		
		// IsDeferred
		/// <summary>Indicates whether or not the constraint check should be deferred to transaction commit time.</summary>
		/// <remarks>
		/// Only database level constraints can be deferred.  By default all database level constraints are deferred.
		/// To change this behavior, use the DAE.IsDeferred tag.
		/// </remarks>
		public bool IsDeferred
		{
			get { return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.IsDeferred", (ConstraintType == ConstraintType.Database).ToString())); }
			set
			{
				if (ConstraintType == ConstraintType.Database)
				{
					if (MetaData == null)
						MetaData = new MetaData();
					MetaData.Tags.AddOrUpdate("DAE.IsDeferred", value.ToString());
				}
			}
		}
		
		// Enforced
		private bool _enforced = true;
		/// <summary>Indicates whether or not the constraint is enforced.</summary>
		/// <remarks>Set by the DAE.Enforced tag when the constraint is created.</remarks>
		public bool Enforced
		{
			get { return _enforced; }
			set { _enforced = value; }
		}
		
		public virtual string GetCustomMessage(Transition transition)
		{
			string message = MetaData.GetTag(MetaData, "DAE.Message", String.Empty);
			if (message == String.Empty)
			{
				message = MetaData.GetTag(MetaData, "DAE.SimpleMessage", String.Empty);
				if (message != String.Empty)
					message = String.Format("\"{0}\"", message);
			}
			return message;
		}
		
		protected abstract PlanNode GetViolationMessageNode(Program program, Transition transition);
		public string GetViolationMessage(Program program, Transition transition)
		{
			try
			{
				PlanNode node = GetViolationMessageNode(program, transition);
				if (node != null)
				{
					string message = (string)node.Execute(program);
					if ((message != String.Empty) && (message[message.Length - 1] != '.'))
						message = message + '.';
					return message;
				}
				return String.Empty;
			}
			catch (Exception exception)
			{
				return String.Format("Errors occurred attempting to generate custom error message for constraint \"{0}\": {1}", Name, exception.Message);
			}
		}
		
		public abstract void Validate(Program program, Transition transition);
    }
    
    public abstract class SimpleConstraint : Constraint
    {
		public SimpleConstraint(int iD, string name) : base(iD, name) {}
		
		// Expression
		private PlanNode _node;
		public PlanNode Node
		{
			get { return _node; }
			set { _node = value; }
		}
		
		// Violation
		private PlanNode _violationMessageNode;
		public PlanNode ViolationMessageNode
		{
			get { return _violationMessageNode; }
			set { _violationMessageNode = value; }
		}

		protected override PlanNode GetViolationMessageNode(Program program, Transition transition)
		{
			return _violationMessageNode;
		}
    }

    public class ScalarTypeConstraint : SimpleConstraint
    {
		public ScalarTypeConstraint(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.ScalarTypeConstraint"), DisplayName, _scalarType.DisplayName); } }

		public override bool IsPersistent { get { return true; } }

		[Reference]
		internal ScalarType _scalarType;
		public ScalarType ScalarType
		{
			get { return _scalarType; }
			set
			{
				if (_scalarType != null)
					_scalarType.Constraints.Remove(this);
				if (value != null)
					value.Constraints.Add(this);
			}
		}

		public override int CatalogObjectID { get { return _scalarType == null ? -1 : _scalarType.ID; } }

		public override int ParentObjectID { get { return _scalarType == null ? -1 : _scalarType.ID; } }
		
		public ConstraintDefinition EmitDefinition(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
			{
				SaveObjectID();
				SaveIsGenerated();
				SaveGeneratorID();
			}
			else
			{
				RemoveObjectID();
				RemoveIsGenerated();
				RemoveGeneratorID();
			}
			try
			{
				ConstraintDefinition statement = new ConstraintDefinition();
				statement.ConstraintName = Name;
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				statement.Expression = (Expression)Node.EmitStatement(mode);
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
				{
					RemoveObjectID();
					RemoveIsGenerated();
					RemoveGeneratorID();
				}
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{
			AlterScalarTypeStatement statement = new AlterScalarTypeStatement();
			statement.ScalarTypeName = Schema.Object.EnsureRooted(_scalarType.Name);
			statement.CreateConstraints.Add(EmitDefinition(mode));
			return statement;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterScalarTypeStatement statement = new AlterScalarTypeStatement();
			statement.ScalarTypeName = Schema.Object.EnsureRooted(_scalarType.Name);
			statement.DropConstraints.Add(new DropConstraintDefinition(Name));
			return statement;
		}
		
		public override void Validate(Program program, Transition transition)
		{
			object objectValue;
			try
			{
				objectValue = Node.Execute(program);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingTypeConstraint, E, Name, _scalarType.Name);
			}
				
			if ((objectValue != null) && !(bool)objectValue)
			{
				string message = GetViolationMessage(program, transition);
				if (message != String.Empty)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
				else
					throw new RuntimeException(RuntimeException.Codes.TypeConstraintViolation, ErrorSeverity.User, Name, _scalarType.Name);
			}
		}
    }
    
    public class TableVarColumnConstraint : SimpleConstraint
    {
		public TableVarColumnConstraint(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumnConstraint"), DisplayName, _tableVarColumn.DisplayName, _tableVarColumn.TableVar.DisplayName); } }

		[Reference]
		internal TableVarColumn _tableVarColumn;
		public TableVarColumn TableVarColumn
		{
			get { return _tableVarColumn; }
			set
			{
				if (_tableVarColumn != null)
					_tableVarColumn.Constraints.Remove(this);
				if (value != null)
					value.Constraints.Add(this);
			}
		}

		public override int CatalogObjectID { get { return _tableVarColumn == null ? -1 : _tableVarColumn.CatalogObjectID; } }

		public override int ParentObjectID { get { return _tableVarColumn == null ? -1 : _tableVarColumn.ID; } }
		
		public override bool IsATObject { get { return _tableVarColumn == null ? false : _tableVarColumn.IsATObject; } }
		
		public ConstraintDefinition EmitDefinition(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				ConstraintDefinition statement = new ConstraintDefinition();
				statement.ConstraintName = Name;
				statement.MetaData = MetaData == null ? null : MetaData.Copy();
				statement.Expression = (Expression)Node.EmitStatement(mode);
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}

		public override Statement EmitStatement(EmitMode mode)
		{	
			AlterTableStatement statement = new AlterTableStatement();
			statement.TableVarName = Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name);
			AlterColumnDefinition definition = new AlterColumnDefinition();
			definition.ColumnName = _tableVarColumn.Name;
			definition.CreateConstraints.Add(EmitDefinition(mode));
			statement.AlterColumns.Add(definition);
			return statement;
		}

		public override Statement EmitDropStatement(EmitMode mode)
		{
			if (_tableVarColumn.TableVar is BaseTableVar)
			{
				AlterTableStatement statement = new AlterTableStatement();
				statement.TableVarName = Schema.Object.EnsureRooted(_tableVarColumn.TableVar.Name);
				AlterColumnDefinition definition = new D4.AlterColumnDefinition();
				definition.ColumnName = _tableVarColumn.Name;
				definition.DropConstraints.Add(new DropConstraintDefinition(Name));
				statement.AlterColumns.Add(definition);
				return statement;
			}
			else
				return new Block();
		}

		public override void Validate(Program program, Transition transition)
		{
			object objectValue;
			try
			{
				objectValue = Node.Execute(program);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingColumnConstraint, E, Name, TableVarColumn.Name, TableVarColumn.TableVar.DisplayName);
			}
			
			if ((objectValue != null) && !(bool)objectValue)
			{
				string message = GetViolationMessage(program, transition);
				if (message != String.Empty)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
				else
					throw new RuntimeException(RuntimeException.Codes.ColumnConstraintViolation, ErrorSeverity.User, Name, TableVarColumn.Name, TableVarColumn.TableVar.DisplayName);
			}
		}
    }
    
    public abstract class TableVarConstraint : Constraint
    {
		public TableVarConstraint(string name) : base(name) {}
		public TableVarConstraint(int iD, string name) : base(iD, name) {}
		
		[Reference]
		internal TableVar _tableVar;
		public TableVar TableVar
		{
			get { return _tableVar; }
			set
			{
				if (_tableVar != null)
					_tableVar.Constraints.Remove(this);
				if (value != null)
					value.Constraints.Add(this);
			}
		}

		/// <summary>Table var constraints are always persistent.</summary>
		public override bool IsPersistent { get { return true; } }

		public override int CatalogObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }

		public override int ParentObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }
		
		public override bool IsATObject { get { return _tableVar == null ? false : _tableVar.IsATObject; } }
		
		public abstract Statement EmitDefinition(EmitMode mode);

		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				AlterTableVarStatement statement = (TableVar is BaseTableVar) ? (AlterTableVarStatement)new AlterTableStatement() : (AlterTableVarStatement)new AlterViewStatement();
				statement.TableVarName = TableVar.Name;
				statement.CreateConstraints.Add(EmitDefinition(mode));
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}

		/// <summary>Returns whether or not the constraint needs to be validated for the specified transition given the specified value flags.</summary>
		public abstract bool ShouldValidate(BitArray valueFlags, Transition transition);
	}
    
    public class RowConstraint : TableVarConstraint
    {
		public RowConstraint(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.RowConstraint"), DisplayName, TableVar.DisplayName); } }

		// Node
		private PlanNode _node;
		public PlanNode Node
		{
			get { return _node; }
			set { _node = value; }
		}
		
		// ViolationMessageNode
		private PlanNode _violationMessageNode;
		public PlanNode ViolationMessageNode
		{
			get { return _violationMessageNode; }
			set { _violationMessageNode = value; }
		}
		
		// ColumnFlags
		private BitArray _columnFlags;
		/// <summary>If specified, indicates which columns are referenced by the constraint</summary>
		public BitArray ColumnFlags
		{
			get { return _columnFlags; }
			set { _columnFlags = value; }
		}
		
		public override Statement EmitDefinition(EmitMode mode)
		{
			ConstraintDefinition statement = new ConstraintDefinition();
			statement.ConstraintName = Name;
			statement.MetaData = MetaData == null ? null : MetaData.Copy();
			statement.Expression = (Expression)Node.EmitStatement(mode);
			return statement;
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterTableVarStatement statement = _tableVar is Schema.BaseTableVar ? (AlterTableVarStatement)new AlterTableStatement() : new AlterViewStatement();
			statement.TableVarName = Schema.Object.EnsureRooted(_tableVar.Name);
			DropConstraintDefinition definition = new DropConstraintDefinition(Name);
			statement.DropConstraints.Add(definition);
			return statement;
		}
		
		/// <summary>Returns whether or not the constraint needs to be validated given the specified value flags.</summary>
		public override bool ShouldValidate(BitArray valueFlags, Schema.Transition transition)
		{
			if ((_columnFlags != null) && (valueFlags != null))
			{
				for (int index = 0; index < _columnFlags.Length; index++)
					if (_columnFlags[index] && valueFlags[index])
						return true;
				return false;
			}

			return true;
		}

		protected override PlanNode GetViolationMessageNode(Program program, Transition transition)
		{
			return _violationMessageNode;
		}

		public override void Validate(Program program, Transition transition)
		{
			object objectValue;
			try
			{
				objectValue = Node.Execute(program);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingRowConstraint, E, Name, TableVar.DisplayName);
			}

			if ((objectValue != null) && !(bool)objectValue)
			{
				string message = GetViolationMessage(program, transition);
				if (message != String.Empty)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
				else
					throw new RuntimeException(RuntimeException.Codes.RowConstraintViolation, ErrorSeverity.User, Name, TableVar.DisplayName);
			}
		}
	}

    /// <remarks> RowConstraints </remarks>
	public class RowConstraints : Objects<RowConstraint>
    {		
    }
    
    public class TransitionConstraint : TableVarConstraint
    {
		public TransitionConstraint(string name) : base(name) {}
		public TransitionConstraint(int iD, string name) : base(iD, name) {}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TransitionConstraint"), DisplayName, TableVar.DisplayName); } }

		// OnInsertNode
		private PlanNode _onInsertNode;
		public PlanNode OnInsertNode
		{
			get { return _onInsertNode; }
			set { _onInsertNode = value; }
		}
		
		// OnInsertViolationMessageNode
		private PlanNode _onInsertViolationMessageNode;
		public PlanNode OnInsertViolationMessageNode
		{
			get { return _onInsertViolationMessageNode; }
			set { _onInsertViolationMessageNode = value; }
		}
		
		// InsertColumnFlags
		private BitArray _insertColumnFlags;
		/// <summary>If specified, indicates which columns are referenced by the insert constraint</summary>
		public BitArray InsertColumnFlags
		{
			get { return _insertColumnFlags; }
			set { _insertColumnFlags = value; }
		}
		
		// OnUpdateNode
		private PlanNode _onUpdateNode;
		public PlanNode OnUpdateNode
		{
			get { return _onUpdateNode; }
			set { _onUpdateNode = value; }
		}
		
		// OnUpdateViolationMessageNode
		private PlanNode _onUpdateViolationMessageNode;
		public PlanNode OnUpdateViolationMessageNode
		{
			get { return _onUpdateViolationMessageNode; }
			set { _onUpdateViolationMessageNode = value; }
		}
		
		// UpdateColumnFlags
		private BitArray _updateColumnFlags;
		/// <summary>If specified, indicates which columns are referenced by the update constraint</summary>
		public BitArray UpdateColumnFlags
		{
			get { return _updateColumnFlags; }
			set { _updateColumnFlags = value; }
		}
		
		// OnDeleteNode
		private PlanNode _onDeleteNode;
		public PlanNode OnDeleteNode
		{
			get { return _onDeleteNode; }
			set { _onDeleteNode = value; }
		}
		
		// OnDeleteViolationMessageNode
		private PlanNode _onDeleteViolationMessageNode;
		public PlanNode OnDeleteViolationMessageNode
		{
			get { return _onDeleteViolationMessageNode; }
			set { _onDeleteViolationMessageNode = value; }
		}
		
		// DeleteColumnFlags
		private BitArray _deleteColumnFlags;
		/// <summary>If specified, indicates which columns are referenced by the delete constraint</summary>
		public BitArray DeleteColumnFlags
		{
			get { return _deleteColumnFlags; }
			set { _deleteColumnFlags = value; }
		}
		
		public override Statement EmitDefinition(EmitMode mode)
		{
			TransitionConstraintDefinition statement = new TransitionConstraintDefinition();
			statement.ConstraintName = Name;
			statement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (_onInsertNode != null)
				statement.OnInsertExpression = (Expression)_onInsertNode.EmitStatement(mode);
			if (_onUpdateNode != null)
				statement.OnUpdateExpression = (Expression)_onUpdateNode.EmitStatement(mode);
			if (_onDeleteNode != null)
				statement.OnDeleteExpression = (Expression)_onDeleteNode.EmitStatement(mode);
			return statement;
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			AlterTableVarStatement statement = _tableVar is Schema.BaseTableVar ? (AlterTableVarStatement)new AlterTableStatement() : new AlterViewStatement();
			statement.TableVarName = Schema.Object.EnsureRooted(_tableVar.Name);
			DropConstraintDefinition definition = new DropConstraintDefinition(Name);
			definition.IsTransition = true;
			statement.DropConstraints.Add(definition);
			return statement;
		}
		
		public override string GetCustomMessage(Transition transition)
		{
			string message = MetaData.GetTag(MetaData, String.Format("DAE.{0}.Message", transition.ToString()), MetaData.GetTag(MetaData, "DAE.Message", String.Empty));
			if (message == String.Empty)
			{
				message = MetaData.GetTag(MetaData, String.Format("DAE.{0}.SimpleMessage", transition.ToString()), MetaData.GetTag(MetaData, "DAE.SimpleMessage", String.Empty));
				if (message != String.Empty)
					message = String.Format("\"{0}\"", message);
			}
			return message;
		}

		/// <summary>Returns whether or not the constraint needs to be validated given the specified value flags.</summary>
		public override bool ShouldValidate(BitArray valueFlags, Schema.Transition transition)
		{
			switch (transition)
			{
				case Transition.Insert :
					if ((_insertColumnFlags != null) && (valueFlags != null))
					{
						for (int index = 0; index < _insertColumnFlags.Length; index++)
							if (_insertColumnFlags[index] && valueFlags[index])
								return true;
						return false;
					}
					return true;
				
				case Transition.Update :
					if ((_updateColumnFlags != null) && (valueFlags != null))
					{
						for (int index = 0; index < _updateColumnFlags.Length; index++)
							if (_updateColumnFlags[index] && valueFlags[index])
								return true;
						return false;
					}
					return true;
				
				case Transition.Delete :
					if ((_deleteColumnFlags != null) && (valueFlags != null))
					{
						for (int index = 0; index < _deleteColumnFlags.Length; index++)
							if (_deleteColumnFlags[index] && valueFlags[index])
								return true;
						return false;
					}
					return true;
				
				default : return true;
			}
		}

		protected override PlanNode GetViolationMessageNode(Program program, Transition transition)
		{
			switch (transition)
			{
				case Transition.Insert: return _onInsertViolationMessageNode;
				case Transition.Update: return _onUpdateViolationMessageNode;
				case Transition.Delete: return _onDeleteViolationMessageNode;
			}
			return null;
		}

		public override void Validate(Program program, Transition transition)
		{
			object objectValue;
			switch (transition)
			{
				case Transition.Insert :
					try
					{
						objectValue = OnInsertNode.Execute(program);
					}
					catch (Exception E)
					{
						throw new RuntimeException(RuntimeException.Codes.ErrorValidatingInsertConstraint, E, Name, TableVar.DisplayName);
					}
					
					if ((objectValue != null) && !(bool)objectValue)
					{
						string message = GetViolationMessage(program, transition);
						if (message != String.Empty)
							throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
						else
							throw new RuntimeException(RuntimeException.Codes.InsertConstraintViolation, ErrorSeverity.User, Name, TableVar.DisplayName);
					}
				break;
				
				case Transition.Update :
					try
					{
						objectValue = OnUpdateNode.Execute(program);
					}
					catch (Exception E)
					{
						throw new RuntimeException(RuntimeException.Codes.ErrorValidatingUpdateConstraint, E, Name, TableVar.DisplayName);
					}

					if ((objectValue != null) && !(bool)objectValue)
					{
						string message = GetViolationMessage(program, transition);
						if (message != String.Empty)
							throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
						else
							throw new RuntimeException(RuntimeException.Codes.UpdateConstraintViolation, ErrorSeverity.User, Name, TableVar.DisplayName);
					}
				break;
				
				case Transition.Delete :
					try
					{
						objectValue = OnDeleteNode.Execute(program);
					}
					catch (Exception E)
					{
						throw new RuntimeException(RuntimeException.Codes.ErrorValidatingDeleteConstraint, E, Name, TableVar.DisplayName);
					}
					
					if ((objectValue != null) && !(bool)objectValue)
					{
						string message = GetViolationMessage(program, transition);
						if (message != String.Empty)
							throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
						else
							throw new RuntimeException(RuntimeException.Codes.DeleteConstraintViolation, ErrorSeverity.User, Name, TableVar.DisplayName);
					}
				break;
			}
		}
    }
    
    /// <remarks> TransitionConstraints </remarks>
	public class TransitionConstraints : Objects<TransitionConstraint>
    {		
    }

    /// <remarks> Constraints </remarks>
	public class Constraints<T> : Objects<T> where T : Constraint
    {		
    }

	public class Constraints : Constraints<Constraint>
	{
	}
    
    public class ScalarTypeConstraints : Constraints<ScalarTypeConstraint>
    {
		public ScalarTypeConstraints(ScalarType scalarType) : base()
		{
			_scalarType = scalarType;
		}
		
		[Reference]
		private ScalarType _scalarType;
		public ScalarType ScalarType { get { return _scalarType; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(ScalarTypeConstraint item)
		{
			base.Validate(item);
			_scalarType.ValidateChildObjectName(item.Name);
		}
		#endif
		
		protected override void Adding(ScalarTypeConstraint item, int index)
		{
			base.Adding(item, index);
			item._scalarType = _scalarType;
		}
		
		protected override void Removing(ScalarTypeConstraint item, int index)
		{
			item._scalarType = null;
			base.Removing(item, index);
		}
    }

    public class TableVarColumnConstraints : Constraints<TableVarColumnConstraint>
    {
		public TableVarColumnConstraints(TableVarColumn tableVarColumn) : base()
		{
			_tableVarColumn = tableVarColumn;
		}
		
		[Reference]
		private TableVarColumn _tableVarColumn;
		public TableVarColumn TableVarColumn { get { return _tableVarColumn; } }
		
		protected override void Adding(TableVarColumnConstraint item, int index)
		{
			base.Adding(item, index);
			item._tableVarColumn = _tableVarColumn;
			_tableVarColumn.ConstraintsAdding(this, item);
		}
		
		protected override void Removing(TableVarColumnConstraint item, int index)
		{
			_tableVarColumn.ConstraintsRemoving(this, item);
			item._tableVarColumn = null;
			base.Removing(item, index);
		}
    }

    public class TableVarConstraints : Constraints<TableVarConstraint>
    {
		public TableVarConstraints(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
		
		[Reference]
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }
		
		#if USEOBJECTVALIDATE
		protected override void Validate(TableVarConstraint item)
		{
			base.Validate(item);
			_tableVar.ValidateChildObjectName(item.Name);
		}
		#endif
		
		protected override void Adding(TableVarConstraint item, int index)
		{
			base.Adding(item, index);
			item._tableVar = _tableVar;
			_tableVar.ResetHasDeferredConstraintsComputed();
		}
		
		protected override void Removing(TableVarConstraint item, int index)
		{
			item._tableVar = null;
			base.Removing(item, index);
			_tableVar.ResetHasDeferredConstraintsComputed();
		}
    }

    public class CatalogConstraint : CatalogObject
    {
		// constructor
		public CatalogConstraint(string name) : base(name) {}
		public CatalogConstraint(int iD, string name) : base(iD, name) {}
		public CatalogConstraint(int iD, string name, PlanNode node) : base(iD, name)
		{
			_node = node;
		}
		
		public CatalogConstraint(int iD, string name, MetaData metaData, PlanNode node) : base(iD, name)
		{
			MetaData = metaData;
			_node = node;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.CatalogConstraint"), DisplayName); } }

		// Expression
		private PlanNode _node;
		public PlanNode Node
		{
			get { return _node; }
			set { _node = value; }
		}
		
		// ViolationMessage
		private PlanNode _violationMessageNode;
		public PlanNode ViolationMessageNode 
		{ 
			get { return _violationMessageNode; } 
			set { _violationMessageNode = value; } 
		}
		
		// ConstraintType
		public ConstraintType ConstraintType
		{
			get { return ConstraintType.Database; }
			set { }
		}

		// Enforced
		private bool _enforced = true;
		/// <summary>Indicates whether or not the constraint is enforced.</summary>
		/// <remarks>Set by the DAE.Enforced tag when the constraint is created.</remarks>
		public bool Enforced
		{
			get { return _enforced; }
			set { _enforced = value; }
		}
		
		// IsDeferred
		/// <summary>Indicates whether or not the constraint check should be deferred to transaction commit time.</summary>
		/// <remarks>
		/// Only database level constraints can be deferred.  By default all database level constraints are deferred.
		/// To change this behavior, use the DAE.IsDeferred tag.
		/// </remarks>
		public bool IsDeferred
		{
			get { return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.IsDeferred", (ConstraintType == ConstraintType.Database).ToString())); }
			set
			{
				if (ConstraintType == ConstraintType.Database)
				{
					if (MetaData == null)
						MetaData = new MetaData();
					MetaData.Tags.AddOrUpdate("DAE.IsDeferred", value.ToString());
				}
			}
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				CreateConstraintStatement statement = new CreateConstraintStatement();
				if (SessionObjectName != null)
				{
					statement.IsSession = true;
					statement.ConstraintName = Schema.Object.EnsureRooted(SessionObjectName);
				}
				else
					statement.ConstraintName = Schema.Object.EnsureRooted(Name);
				statement.MetaData = MetaData == null ? new MetaData() : MetaData.Copy();
				if (SessionObjectName != null)
					statement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", Name, true);
				statement.Expression = (Expression)Node.EmitStatement(mode);
				return statement;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropConstraintStatement statement = new DropConstraintStatement();
			statement.ConstraintName = Schema.Object.EnsureRooted(Name);
			return statement;
		}

		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop
			};
		}
		
		public string GetCustomMessage()
		{
			string message = MetaData.GetTag(MetaData, "DAE.Message", String.Empty);
			if (message == String.Empty)
			{
				message = MetaData.GetTag(MetaData, "DAE.SimpleMessage", String.Empty);
				if (message != String.Empty)
					message = String.Format("\"{0}\"", message);
			}
			return message;
		}
		
		public string GetViolationMessage(Program program)
		{
			try
			{
				if (_violationMessageNode != null)
				{
					string message = (string)_violationMessageNode.Execute(program);
					if ((message != String.Empty) && (message[message.Length - 1] != '.'))
						message = message + '.';
					return message;
				}
				return String.Empty;
			}
			catch (Exception exception)
			{
				return String.Format("Errors occurred attempting to generate custom error message for constraint \"{0}\": {1}", Name, exception.Message);
			}
		}
		
		public void Validate(Program program)
		{
			object objectValue;
			try
			{
				objectValue = Node.Execute(program);
			}
			catch (Exception E)
			{
				throw new RuntimeException(RuntimeException.Codes.ErrorValidatingCatalogConstraint, E, Name);
			}
			
			if ((objectValue != null) && !(bool)objectValue)
			{
				string message = GetViolationMessage(program);
				if (message != String.Empty)
					throw new RuntimeException(RuntimeException.Codes.GeneralConstraintViolation, ErrorSeverity.User, message);
				else
					throw new RuntimeException(RuntimeException.Codes.CatalogConstraintViolation, ErrorSeverity.User, DisplayName);
			}
		}		
    }
    
    /// <remarks> CatalogConstraints </remarks>
	public class CatalogConstraints : Objects<CatalogConstraint>
    {		
    }
}