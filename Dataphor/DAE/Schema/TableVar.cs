/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Schema
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

	public enum TableVarColumnType { Stored, Virtual, RowExists, Level, Sequence, InternalID }
	
	/// <remarks> Provides the representation for a column header (Name:DataType) </remarks>
	public class TableVarColumn : Object
    {
		public const ushort IsNilableFlag = 0x0001;
		public const ushort NotIsNilableFlag = 0xFFFE;
		public const ushort IsComputedFlag = 0x0002;
		public const ushort NotIsComputedFlag = 0xFFFD;
		public const ushort ReadOnlyFlag = 0x0004;
		public const ushort NotReadOnlyFlag = 0xFFFB;
		public const ushort IsDefaultRemotableFlag = 0x0008;
		public const ushort NotIsDefaultRemotableFlag = 0xFFF7;
		public const ushort IsValidateRemotableFlag = 0x0010;
		public const ushort NotIsValidateRemotableFlag = 0xFFEF;
		public const ushort IsChangeRemotableFlag = 0x0020;
		public const ushort NotIsChangeRemotableFlag = 0xFFDF;
		public const ushort ShouldDefaultFlag = 0x0040;
		public const ushort NotShouldDefaultFlag = 0xFFBF;
		public const ushort ShouldValidateFlag = 0x0080;
		public const ushort NotShouldValidateFlag = 0xFF7F;
		public const ushort ShouldChangeFlag = 0x0100;
		public const ushort NotShouldChangeFlag = 0xFEFF;

		public TableVarColumn(Column column) : base(column.Name)
		{
			SetColumn(column);
		}
		
		public TableVarColumn(Column column, TableVarColumnType columnType) : base(column.Name)
		{
			SetColumn(column);
			_columnType = columnType;
			ReadOnly = !((_columnType == TableVarColumnType.Stored) || (_columnType == TableVarColumnType.RowExists));
		}
		
		public TableVarColumn(Column column, MetaData metaData) : base(column.Name)
		{
			SetColumn(column);
			MergeMetaData(metaData);
			IsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (_columnType != TableVarColumnType.Stored).ToString()));
			ReadOnly = !((_columnType == TableVarColumnType.Stored) || (_columnType == TableVarColumnType.RowExists) || !IsComputed);
		}
		
		public TableVarColumn(Column column, MetaData metaData, TableVarColumnType columnType) : base(column.Name)
		{
			SetColumn(column);
			MergeMetaData(metaData);
			_columnType = columnType;
			IsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (_columnType != TableVarColumnType.Stored).ToString()));
			ReadOnly = !((_columnType == TableVarColumnType.Stored) || (_columnType == TableVarColumnType.RowExists) || !IsComputed);
		}
		
		public TableVarColumn(int iD, Column column, MetaData metaData, TableVarColumnType columnType) : base(iD, column.Name)
		{
			SetColumn(column);
			MergeMetaData(metaData);
			_columnType = columnType;
			IsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (_columnType != TableVarColumnType.Stored).ToString()));
			ReadOnly = !((_columnType == TableVarColumnType.Stored) || (_columnType == TableVarColumnType.RowExists) || !IsComputed);
		}

		private ushort _characteristics = IsDefaultRemotableFlag | IsValidateRemotableFlag | IsChangeRemotableFlag | ShouldDefaultFlag | ShouldValidateFlag | ShouldChangeFlag;
		
        // IsNilable
        public bool IsNilable
        {
			get { return (_characteristics & IsNilableFlag) == IsNilableFlag; }
			set { if (value) _characteristics |= IsNilableFlag; else _characteristics &= NotIsNilableFlag; }
		}
        
        // IsComputed
        public bool IsComputed
        {
			get { return (_characteristics & IsComputedFlag) == IsComputedFlag; }
			set { if (value) _characteristics |= IsComputedFlag; else _characteristics &= NotIsComputedFlag; }
		}
        
		public void DetermineShouldCallProposables(bool reset)
		{
			if (reset)
			{
				ShouldChange = false;
				ShouldDefault = false;
				ShouldValidate = false;
			}

			if (HasHandlers(EventType.Change))
				ShouldChange = true;
				
			if (HasHandlers(EventType.Default) || (_default != null))
				ShouldDefault = true;
				
			if (HasHandlers(EventType.Validate) || HasConstraints())
				ShouldValidate = true;
				
			Schema.ScalarType scalarType = DataType as Schema.ScalarType;
			if (scalarType != null) 
			{
				if (scalarType.HasHandlers(EventType.Change))
					ShouldChange = true;
					
				if (scalarType.HasHandlers(EventType.Default) || (scalarType.Default != null))
					ShouldDefault = true;
					
				if (scalarType.HasHandlers(EventType.Validate) || (scalarType.Constraints.Count > 0))
					ShouldValidate = true;
			}
		}
		
		private void SetIsValidateRemotable(ScalarType dataType)
		{	
			foreach (Constraint constraint in dataType.Constraints)
			{
				IsValidateRemotable = IsValidateRemotable && constraint.IsRemotable;
				if (!IsValidateRemotable)
					break;
			}

			if (IsValidateRemotable)
			{
				foreach (EventHandler handler in dataType.EventHandlers)
					if ((handler.EventType & EventType.Validate) != 0)
					{
						IsValidateRemotable = IsValidateRemotable && handler.IsRemotable;
						if (!IsValidateRemotable)
							break;
					}

				#if USETYPEINHERITANCE
				if (FIsValidateRemotable)			
					foreach (ScalarType parentType in ADataType.ParentTypes)
						if (FIsValidateRemotable)
							SetIsValidateRemotable(parentType);
				#endif
			}
		}
		
		private void SetIsValidateRemotable()
		{
			IsValidateRemotable = true;
			if (_constraints != null)
				foreach (Constraint constraint in Constraints)
				{
					IsValidateRemotable = IsValidateRemotable && constraint.IsRemotable;
					if (!IsValidateRemotable)
						break;
				}
			
			if (IsValidateRemotable)
			{
				if (_eventHandlers != null)
					foreach (EventHandler handler in _eventHandlers)
						if ((handler.EventType & EventType.Validate) != 0)
						{
							IsValidateRemotable = IsValidateRemotable && handler.IsRemotable;
							if (!IsValidateRemotable)
								break;
						}
					
				if (IsValidateRemotable && (DataType is ScalarType))
					SetIsValidateRemotable((ScalarType)DataType);
			}
		}
		
		private void SetIsChangeRemotable(ScalarType dataType)
		{
			if (_eventHandlers != null)
				foreach (EventHandler handler in _eventHandlers)
					if ((handler.EventType & EventType.Change) != 0)
					{
						IsChangeRemotable = IsChangeRemotable && handler.IsRemotable;
						if (!IsChangeRemotable)
							break;
					}

			#if USETYPEINHERITANCE				
			if (FIsChangeRemotable)
				foreach (ScalarType parentType in ADataType.ParentTypes)
					if (FIsChangeRemotable)
						SetIsChangeRemotable(parentType);
			#endif
		}
		
		private void SetIsChangeRemotable()
		{
			IsChangeRemotable = true;
			if (_eventHandlers != null)
				foreach (EventHandler handler in _eventHandlers)
					if ((handler.EventType & EventType.Change) != 0)
					{
						IsChangeRemotable = IsChangeRemotable && handler.IsRemotable;
						if (!IsChangeRemotable)
							break;
					}
				
			if (IsChangeRemotable && (DataType is ScalarType))
				SetIsChangeRemotable((ScalarType)DataType);
		}
		
        private void SetIsDefaultRemotable()
        {
			IsDefaultRemotable = IsChangeRemotable;
			
			if (IsDefaultRemotable)
			{
				if (_eventHandlers != null)
					foreach (EventHandler handler in _eventHandlers)
						if ((handler.EventType & EventType.Default) != 0)
						{
							IsDefaultRemotable = IsDefaultRemotable && handler.IsRemotable;
							if (!IsDefaultRemotable)
								break;
						}
			}
			
			if (IsDefaultRemotable)
			{
				if (_default != null)
					IsDefaultRemotable = IsDefaultRemotable && _default.IsRemotable;
				else
				{
					ScalarType scalarType = DataType as ScalarType;
					if (scalarType != null)
					{
						foreach (EventHandler handler in scalarType.EventHandlers)
							if ((handler.EventType & EventType.Default) != 0)
							{
								IsDefaultRemotable = IsDefaultRemotable && handler.IsRemotable;
								if (!IsDefaultRemotable)
									break;
							}
						
						if (IsDefaultRemotable && (scalarType.Default != null))
							IsDefaultRemotable = IsDefaultRemotable && scalarType.Default.IsRemotable;
					}
				}
			}
        }
        
        protected void SetColumn(Column column)
        {
			_column = column;
			if (_column.DataType is Schema.ScalarType)
				InheritMetaData(((Schema.ScalarType)_column.DataType).MetaData);

			SetIsValidateRemotable();
			SetIsDefaultRemotable();
			SetIsChangeRemotable();
        }
        
		internal void ConstraintsAdding(object sender, Object objectValue)
		{
			IsValidateRemotable = IsValidateRemotable && objectValue.IsRemotable;
		}
		
		internal void ConstraintsRemoving(object sender, Object objectValue)
		{
			SetIsValidateRemotable();
		}
		
		internal void EventHandlersAdding(object sender, Object objectValue)
		{
			EventHandler localObjectValue = (EventHandler)objectValue;
			if ((localObjectValue.EventType & EventType.Default) != 0)
				IsDefaultRemotable = IsDefaultRemotable && localObjectValue.IsRemotable;
			if ((localObjectValue.EventType & EventType.Validate) != 0)
				IsValidateRemotable = IsValidateRemotable && localObjectValue.IsRemotable;
			if ((localObjectValue.EventType & EventType.Change) != 0)
				IsChangeRemotable = IsChangeRemotable && localObjectValue.IsRemotable;
		}
		
		internal void EventHandlersRemoving(object sender, Object objectValue)
		{
			EventHandler localObjectValue = (EventHandler)objectValue;
			if ((localObjectValue.EventType & EventType.Default) != 0)
				SetIsDefaultRemotable();
			if ((localObjectValue.EventType & EventType.Validate) != 0)
				SetIsValidateRemotable();
			if ((localObjectValue.EventType & EventType.Change) != 0)
				SetIsChangeRemotable();
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumn"), DisplayName, TableVar.DisplayName); } }

		public override int CatalogObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }

		public override int ParentObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }
		
		public override bool IsATObject { get { return _tableVar == null ? false : _tableVar.IsATObject; } }

		// TableVar
		[Reference]
		internal TableVar _tableVar;
		public TableVar TableVar 
		{ 
			get { return _tableVar; } 
			set
			{
				if (_tableVar != null)
					_tableVar.Columns.Remove(this);
				if (value != null)
					value.Columns.Add(this);
			}
		}
		
		// Column
		[Reference]
		private Column _column;
		public Column Column { get { return _column; } }

		// DataType
		public IDataType DataType { get { return _column.DataType; } }
        
        // ColumnType
        private TableVarColumnType _columnType;
		public TableVarColumnType ColumnType { get { return _columnType; } }
		
        // ReadOnly
        public bool ReadOnly
        {
			get { return (_characteristics & ReadOnlyFlag) == ReadOnlyFlag; }
			set { if (value) _characteristics |= ReadOnlyFlag; else _characteristics &= NotReadOnlyFlag; }
		}
        
        // Default
        private TableVarColumnDefault _default;
        public TableVarColumnDefault Default
        {
			get { return _default; }
			set 
			{
				if (_default != null)
					_default._tableVarColumn = null; 
				_default = value; 
				if (_default != null)
					_default._tableVarColumn = this;
				SetIsDefaultRemotable();
			}
        }
        
        public bool HasConstraints()
        {
			return (_constraints != null) && (_constraints.Count > 0);
        }
        
		// Constraints
		private TableVarColumnConstraints _constraints;
		public TableVarColumnConstraints Constraints 
		{ 
			get 
			{ 
				if (_constraints == null)
					_constraints = new TableVarColumnConstraints(this);			
				return _constraints; 
			} 
		}
		
		public bool HasHandlers()
		{
			return (_eventHandlers != null) && (_eventHandlers.Count > 0);
		}
		
		public bool HasHandlers(EventType eventType)
		{
			return (_eventHandlers != null) && _eventHandlers.HasHandlers(eventType);
		}
		
		// EventHandlers
		private TableVarColumnEventHandlers _eventHandlers;
		public TableVarColumnEventHandlers EventHandlers 
		{ 
			get 
			{ 
				if (_eventHandlers == null)
					_eventHandlers = new TableVarColumnEventHandlers(this);
				return _eventHandlers; 
			} 
		}

        // IsDefaultRemotable
        public bool IsDefaultRemotable
        {
			get { return (_characteristics & IsDefaultRemotableFlag) == IsDefaultRemotableFlag; }
			set { if (value) _characteristics |= IsDefaultRemotableFlag; else _characteristics &= NotIsDefaultRemotableFlag; }
		}
        
        // IsValidateRemotable
        public bool IsValidateRemotable
        {
			get { return (_characteristics & IsValidateRemotableFlag) == IsValidateRemotableFlag; }
			set { if (value) _characteristics |= IsValidateRemotableFlag; else _characteristics &= NotIsValidateRemotableFlag; }
		}
        
        // IsChangeRemotable
        public bool IsChangeRemotable
        {
			get { return (_characteristics & IsChangeRemotableFlag) == IsChangeRemotableFlag; }
			set { if (value) _characteristics |= IsChangeRemotableFlag; else _characteristics &= NotIsChangeRemotableFlag; }
		}
        
        // ShouldDefault
        public bool ShouldDefault
        {
			get { return (_characteristics & ShouldDefaultFlag) == ShouldDefaultFlag; }
			set { if (value) _characteristics |= ShouldDefaultFlag; else _characteristics &= NotShouldDefaultFlag; }
		}
        
        // ShouldValidate
        public bool ShouldValidate
        {
			get { return (_characteristics & ShouldValidateFlag) == ShouldValidateFlag; }
			set { if (value) _characteristics |= ShouldValidateFlag; else _characteristics &= NotShouldValidateFlag; }
		}
        
        // ShouldChange
        public bool ShouldChange
        {
			get { return (_characteristics & ShouldChangeFlag) == ShouldChangeFlag; }
			set { if (value) _characteristics |= ShouldChangeFlag; else _characteristics &= NotShouldChangeFlag; }
		}
        
		// Equals
		public override bool Equals(object objectValue)
		{
			TableVarColumn column = objectValue as TableVarColumn;
			return (column != null) && (String.Compare(Name, column.Name) == 0);
		}

		// GetHashCode
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

        // ToString
        public override string ToString()
        {
			StringBuilder builder = new StringBuilder(Name);
			if (DataType != null)
			{
				builder.Append(Keywords.TypeSpecifier);
				builder.Append(DataType.Name);
			}
			return builder.ToString();
        }

		// Inheriting a column does not copy static tags, copying a column does        
        public TableVarColumn Inherit()
        {
			TableVarColumn column = new TableVarColumn(_column.Copy(), MetaData == null ? null : MetaData.Inherit(), _columnType);
			InternalCopy(column);
			return column;
        }
        
        public TableVarColumn Inherit(string prefix)
        {
			TableVarColumn column = new TableVarColumn(_column.Copy(prefix), MetaData == null ? null : MetaData.Inherit(), _columnType);
			InternalCopy(column);
			return column;
        }

		public TableVarColumn InheritAndRename(string name)
		{
			TableVarColumn column = new TableVarColumn(_column.CopyAndRename(name), MetaData == null ? null : MetaData.Inherit(), _columnType);
			InternalCopy(column);
			return column;
		}
		
        public TableVarColumn Copy()
        {
			TableVarColumn column = new TableVarColumn(_column.Copy(), MetaData == null ? null : MetaData.Copy(), _columnType);
			InternalCopy(column);
			return column;
		}

		protected void InternalCopy(TableVarColumn column)
        {
			column.IsNilable = IsNilable;
			column.ReadOnly = ReadOnly;
			column.IsRemotable = IsRemotable;
			column.IsDefaultRemotable = IsDefaultRemotable;
			column.IsValidateRemotable = IsValidateRemotable;
			column.IsChangeRemotable = IsChangeRemotable;
			column.ShouldChange = ShouldChange;
			column.ShouldDefault = ShouldDefault;
			column.ShouldValidate = ShouldValidate;
        }
        
		public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			DataType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);

			if ((_default != null) && ((mode != EmitMode.ForRemote) || _default.IsRemotable))
				_default.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
		
			if (_constraints != null)
				foreach (Constraint constraint in Constraints)
					if ((mode != EmitMode.ForRemote) || constraint.IsRemotable)
						constraint.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
		}
		
		public override void IncludeHandlers(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			if (_eventHandlers != null)
				foreach (EventHandler handler in _eventHandlers)
					if ((mode != EmitMode.ForRemote) || handler.IsRemotable)
						handler.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				ColumnDefinition column = new ColumnDefinition();
				column.ColumnName = Name;
				column.TypeSpecifier = DataType.EmitSpecifier(mode);
				column.IsNilable = IsNilable;
				if ((Default != null) && Default.IsRemotable && ((mode != EmitMode.ForStorage) || !Default.IsPersistent))
					column.Default = (DefaultDefinition)Default.EmitDefinition(mode);
		
				if (_constraints != null)	
					foreach (TableVarColumnConstraint constraint in Constraints)
						if (((mode != EmitMode.ForRemote) || constraint.IsRemotable) && ((mode != EmitMode.ForStorage) || !constraint.IsPersistent))
							column.Constraints.Add(constraint.EmitDefinition(mode));

				column.MetaData = MetaData == null ? null : MetaData.Copy();
				return column;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
		}
		
		public override Object GetObjectFromHeader(ObjectHeader header)
		{
			switch (header.ObjectType)
			{
				case "TableVarColumnConstraint" :
					if (_constraints != null)
						foreach (Constraint constraint in _constraints)
							if (header.ID == constraint.ID)
								return constraint;
				break;
								
				case "TableVarColumnDefault" :
					if ((_default != null) && (header.ID == _default.ID))
						return _default;
				break;
			}
			
			return base.GetObjectFromHeader(header);
		}
	}

	/// <remarks> Provides a container for TableVarColumn objects </remarks>
	public class TableVarColumnsBase : Objects<TableVarColumn>
    {
		// Column lists are equal if they contain the same number of columns and all columns in the left
		// list are also in the right list
        public override bool Equals(object objectValue)
        {
			TableVarColumnsBase columns = objectValue as TableVarColumnsBase;
			if ((columns != null) && (Count == columns.Count))
			{
				foreach (TableVarColumn column in this)
					if (!columns.Contains(column))
						return false;
				return true;
			}
			else
				return false;
        }
        
        public string[] ColumnNames
        {
			get
			{
				string[] result = new string[Count];
				for (int index = 0; index < Count; index++)
					result[index] = this[index].Name;
				return result;
			}
        }

		// Column lists are equivalent if they contain the same number of columns and the columns are equal, left to right
		// This is an internal notion for use in physical contexts only
        public bool Equivalent(TableVarColumnsBase columns)
        {
			if (Count == columns.Count)
			{
				for (int index = 0; index < Count; index++)
					if (!this[index].Equals(columns[index]))
						return false;
				return true;
			}
			return false;
        }

        public override int GetHashCode()
        {
			int hashCode = 0;
			for (int index = 0; index < Count; index++)
				hashCode ^= this[index].GetHashCode();
			return hashCode;
        }

        public bool Compatible(object objectValue)
        {
			return Is(objectValue) || ((objectValue is TableVarColumnsBase) && ((TableVarColumnsBase)objectValue).Is(this));
        }

        public bool Is(object objectValue)
        {
			// A column list is another column list if they both have the same number of columns
			// and the is of the datatypes for all columns evaluates to true by name
			TableVarColumnsBase columns = objectValue as TableVarColumnsBase;
			if ((columns != null) && (Count == columns.Count))
			{
				int columnIndex;
				for (int index = 0; index < Count; index++)
				{
					columnIndex = columns.IndexOfName(this[index].Name);
					if (columnIndex >= 0)
					{
						if (!this[index].DataType.Is(columns[columnIndex].DataType))
							return false;
					}
					else
						return false;
				}
				return true;
			}
			else
				return false;
        }

		public bool IsSubsetOf(TableVarColumnsBase columns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (TableVarColumn column in this)
				if (!columns.ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSubsetOf(TableVarColumnsBase columns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(columns) && (Count < columns.Count);
		}

		public bool IsSupersetOf(TableVarColumnsBase columns)
		{
			// true if every column in AColumnNames is in this set of columns
			foreach (TableVarColumn column in columns)
				if (!ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSupersetOf(TableVarColumnsBase columns)
		{
			// true if every column in AColumnNames is in this set of columns and this set of columns is strictly larger
			return IsSupersetOf(columns) && (Count > columns.Count);
		}
		
		public Key Intersect(TableVarColumnsBase columns)
		{
			Key key = new Key();
			foreach (TableVarColumn column in columns)
				if (ContainsName(column.Name))
					key.Columns.Add(column);
					
			return key;
		}
		
		public Key Difference(TableVarColumnsBase columns)
		{
			Key key = new Key();
			foreach (TableVarColumn column in this)
				if (!columns.ContainsName(column.Name))
					key.Columns.Add(column);
					
			return key;
		}
		
		public Key Union(TableVarColumnsBase columns)
		{
			Key key = new Key();
			foreach (TableVarColumn column in this)
				key.Columns.Add(column);
				
			foreach (TableVarColumn column in columns)
				if (!key.Columns.ContainsName(column.Name))
					key.Columns.Add(column);
			
			return key;
		}
		
        public TableVarColumn this[TableVarColumn column]
        {
			get { return this[IndexOfName(column.Name)]; }
			set { this[IndexOfName(column.Name)] = value; }
        }

        // ToString
        public override string ToString()
        {
			StringBuilder stringValue = new StringBuilder();
			foreach (TableVarColumn column in this)
			{
				if (stringValue.Length != 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(column.ToString());
			}
			stringValue.Insert(0, Keywords.BeginList);
			stringValue.Append(Keywords.EndList);
			return stringValue.ToString();
        }
    }
    
    public class TableVarColumns : TableVarColumnsBase
    {
		public TableVarColumns(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
		
		[Reference]
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }

		protected override void Validate(TableVarColumn objectValue)
		{
			base.Validate(objectValue);
			_tableVar.ValidateChildObjectName(objectValue.Name);
		}
		
		protected override void Adding(TableVarColumn item, int index)
		{
			base.Adding(item, index);
			item._tableVar = _tableVar;
		}
		
		protected override void Removing(TableVarColumn item, int index)
		{
			item._tableVar = null;
			base.Removing(item, index);
		}
    }
    
    public class KeyColumns : TableVarColumnsBase 
    {
		//public KeyColumns() : base() { }
		public KeyColumns(Key key) : base()
		{
			_key = key;
		}
		
		private Key _key;
		public Key Key { get { return _key; } }

		protected override void Adding(TableVarColumn objectValue, int index)
		{
			base.Adding(objectValue, index);
			if (_key != null)
				_key.UpdateKeyName();
		}

		protected override void Removing(TableVarColumn objectValue, int index)
		{
			base.Removing(objectValue, index);
			if (_key != null)
				_key.UpdateKeyName();
		}
    }

	public class JoinKeyColumns : TableVarColumnsBase
	{
		public JoinKeyColumns() : base() { }
	}
    
	public class OrderColumn : System.Object, ICloneable
    {
		public OrderColumn() : base(){}
		public OrderColumn(TableVarColumn column, bool ascending) : base()
		{
			Column = column;
			_ascending = ascending;
		}
		
		public OrderColumn(TableVarColumn column, bool ascending, bool includeNils) : base()
		{
			Column = column;
			_ascending = ascending;
			_includeNils = includeNils;
		}
		
		// Compare expression for this column in the order
		private Sort _sort;
		public Sort Sort
		{
			get { return _sort; }
			set { _sort = value; }
		}
		
		// IsDefaultSort
		private bool _isDefaultSort = true;
		public bool IsDefaultSort
		{
			get { return _isDefaultSort; }
			set { _isDefaultSort = value; }
		}
		
		// Column
		[Reference]
		protected TableVarColumn _column;
		public TableVarColumn Column
		{
			get { return _column; }
			set { _column = value; }
		}

		// Ascending
		protected bool _ascending = true;
		public bool Ascending
		{
			get { return _ascending; }
			set { _ascending = value; }
		}
		
		// IncludeNils
		protected bool _includeNils;
		public bool IncludeNils
		{
			get { return _includeNils; }
			set { _includeNils = value; }
		}

		// ICloneable
		public virtual object Clone()
		{
			return new OrderColumn(_column, _ascending, _includeNils);
		}
		
		public object Clone(bool reverse)
		{
			return new OrderColumn(_column, reverse ? !_ascending : _ascending, _includeNils);
		}
		
		public override string ToString()
		{
			StringBuilder stringValue = new StringBuilder(_column.Name);
			stringValue.Append(" ");
			if (!IsDefaultSort)
				stringValue.AppendFormat("{0} {1} ", Keywords.Sort, _sort.CompareNode.EmitStatementAsString());
			stringValue.Append(_ascending ? Keywords.Asc : Keywords.Desc);
			if (_includeNils)
				stringValue.AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
			return stringValue.ToString();
		}
		
		public Statement EmitStatement(EmitMode mode)
		{
			OrderColumnDefinition definition = new OrderColumnDefinition(Schema.Object.EnsureRooted(Column.Name), Ascending, IncludeNils);
			if (!IsDefaultSort)
				definition.Sort = _sort.EmitDefinition(mode);
			return definition;
		}

		public override int GetHashCode()
		{
			int result = _column.Name.GetHashCode();
			if (!IsDefaultSort)
				result ^= _sort.CompareNode.EmitStatementAsString().GetHashCode();
			if (_ascending)
				result = (result << 1) | (result >> 31);
			if (_includeNils)
				result = (result << 2) | (result >> 30);
			return result;
		}

		public bool Equivalent(OrderColumn orderColumn)
		{
			return 
				(orderColumn != null) 
					&& (orderColumn.Column.Name == Column.Name) 
					&& (orderColumn.Ascending == _ascending) 
					//&& (AOrderColumn.IncludeNils == FIncludeNils) // Should be here, but can't be yet (breaks existing code, and doesn't actually do anything in the physical layer yet, so doesn't matter)
					&& ((orderColumn.Sort == null) == (_sort == null))
					&& ((orderColumn.Sort == null) || orderColumn.Sort.Equivalent(_sort));
		}
    }

	public class OrderColumns : System.Object, IList<OrderColumn>
    {
		private List<OrderColumn> _orderColumns = new List<OrderColumn>();
		
		public void Add(OrderColumn orderColumn)
		{
			if (Contains(orderColumn))
				throw new SchemaException(SchemaException.Codes.DuplicateOrderColumnDefinition, orderColumn.ToString());
			_orderColumns.Add(orderColumn);
			_version++;
		}
		
		public void Insert(int index, OrderColumn item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public int IndexOf(OrderColumn orderColumn)
		{
			for (int index = 0; index < _orderColumns.Count; index++)
				if (_orderColumns[index].Equivalent(orderColumn))
					return index;
			return -1;
		}
		
		public bool Contains(OrderColumn orderColumn)
		{
			return IndexOf(orderColumn) >= 0;
		}
		
		public int Count { get { return _orderColumns.Count; } }
		
		public OrderColumn this[int index] 
		{ 
			get { return _orderColumns[index]; } 
			set { throw new NotImplementedException(); }
		}

		/// <summary>Returns the first column in the order referencing the given name, without name resolution</summary>		
		public OrderColumn this[string columnName]
		{
			get
			{
				int index = IndexOf(columnName);
				if (index < 0)
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, columnName);
				return _orderColumns[index];
			}
		}
		
		/// <summary>Returns the index of the first reference in the order to the given column name, without name resolution</summary>
		public int IndexOf(string columnName)
		{
			for (int index = 0; index < _orderColumns.Count; index++)
				if (_orderColumns[index].Column.Name == columnName)
					return index;
			return -1;
		}
		
		/// <summary>Returns true if the order contains any reference to the given column name, without name resolution</summary>
		public bool Contains(string columnName)
		{
			return IndexOf(columnName) >= 0;
		}
		
		/// <summary>Returns the index of the first reference in the order to the given column name, without name resolution, and using a sort equivalent to the given sort.</summary>
		public int IndexOf(string columnName, Schema.Sort sort)
		{
			for (int index = 0; index < _orderColumns.Count; index++)
				if ((_orderColumns[index].Column.Name == columnName) && _orderColumns[index].Sort.Equivalent(sort))
					return index;
			return -1;
		}
		
		/// <summary>Returns true if the order contains any reference to the given column name, without name resolution, and using a sort equivalent to the given sort.</summary>
		public bool Contains(string columnName, Schema.Sort sort)
		{
			return IndexOf(columnName, sort) >= 0;
		}
		
		private int _version;
		/// <summary>Returns the version of the order columns list. Beginning at zero, this number is incremented each time a column is added or removed from the order columns.</summary>
		/// <remarks>This number is used to coordinate changes to the column list with properties of the order that are dependent on the set of columns in the order, such as Name.</remarks>
		public int Version { get { return _version; } }
		
		public List<OrderColumn>.Enumerator GetEnumerator()
		{
			return _orderColumns.GetEnumerator();
		}
		
		public override string ToString()
		{
			StringBuilder stringValue = new StringBuilder();
			for (int index = 0; index < Count; index++)
			{
				if (stringValue.Length != 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(_orderColumns[index].ToString());
			}
			stringValue.Insert(0, Keywords.BeginList);
			stringValue.Append(Keywords.EndList);
			return stringValue.ToString();
		}

		public bool IsSubsetOf(Columns columns)
		{
			// true if every column in this set of columns is in AColumns
			for (int index = 0; index < _orderColumns.Count; index++)
				if (!columns.ContainsName(_orderColumns[index].Column.Name))
					return false;
			return true;
		}
		
		public bool IsSubsetOf(TableVarColumnsBase columns)
		{
			// true if every column in this set of columns is in AColumns
			for (int index = 0; index < _orderColumns.Count; index++)
				if (!columns.ContainsName(_orderColumns[index].Column.Name))
					return false;
			return true;
		}
		
		public bool IsSubsetOf(OrderColumns columns)
		{
			// true if every column in this set of columns is in AColumns
			for (int index = 0; index < _orderColumns.Count; index++)
				if (!columns.Contains(_orderColumns[index].Column.Name))
					return false;
			return true;
		}

		public bool IsProperSubsetOf(Columns columns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(columns) && (Count < columns.Count);
		}
		
		public bool IsProperSubsetOf(TableVarColumnsBase columns)
		{
			return IsSubsetOf(columns) && (Count < columns.Count);
		}

		public bool IsProperSubsetOf(OrderColumns columns)
		{
			return IsSubsetOf(columns) && (Count < columns.Count);
		}

		#region ICollection<OrderColumn> Members

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public void CopyTo(OrderColumn[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool IsReadOnly
		{
			get { throw new NotImplementedException(); }
		}

		public bool Remove(OrderColumn item)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable<OrderColumn> Members

		IEnumerator<OrderColumn> IEnumerable<OrderColumn>.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}

	public class Order : Object
    {		
		public Order() : base(String.Empty) {}
		public Order(int iD) : base(iD, String.Empty) {}

		public Order(MetaData metaData) : base(String.Empty)
		{
			MetaData = metaData;
		}
		
		public Order(int iD, MetaData metaData) : base(iD, String.Empty)
		{
			MetaData = metaData;
		}
		
		public Order(Order order) : base(String.Empty)
		{
			OrderColumn newOrderColumn;
			OrderColumn column;
			for (int index = 0; index < order.Columns.Count; index++)
			{
				column = order.Columns[index];
				newOrderColumn = new OrderColumn(column.Column, column.Ascending, column.IncludeNils);
				newOrderColumn.Sort = column.Sort;
				newOrderColumn.IsDefaultSort = column.IsDefaultSort;
				_columns.Add(newOrderColumn);
			}
		}
		
		public Order(Order order, bool reverse) : base(String.Empty)
		{
			OrderColumn newOrderColumn;
			OrderColumn column;
			for (int index = 0; index < order.Columns.Count; index++)
			{
				column = order.Columns[index];
				newOrderColumn = new OrderColumn(column.Column, reverse ? !column.Ascending : column.Ascending, column.IncludeNils);
				newOrderColumn.Sort = column.Sort;
				newOrderColumn.IsDefaultSort = column.IsDefaultSort;
				_columns.Add(newOrderColumn);
			}
		}
		
		public Order(Key key) : base(String.Empty)
		{
			for (int index = 0; index < key.Columns.Count; index++)
				_columns.Add(new OrderColumn(key.Columns[index], true, true));
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Order"), DisplayName); } }

		public override int CatalogObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }

		public override int ParentObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }
		
		/// <summary>Returns the full name of the order, a guaranteed parsable D4 order definition.</summary>
		/// <remarks>The Name property, in contrast, returns the full name limited to the max object name length of 200 characters.</remarks>
		private string GetFullName()
		{
			StringBuilder name = new StringBuilder();
			name.AppendFormat("{0} {1} ", Keywords.Order, Keywords.BeginList);
			for (int index = 0; index < _columns.Count; index++)
			{
				if (index > 0)
					name.AppendFormat("{0} ", Keywords.ListSeparator);
				name.Append(_columns[index].ToString());
			}
			name.AppendFormat("{0}{1}", _columns.Count > 0 ? " " : "", Keywords.EndList);
			return name.ToString();
		}

		public override string Name
		{
			get 
			{ 
				EnsureOrderName();
				return base.Name; 
			}
			set { base.Name = value; }
		}

		private int _columnsVersion = -1; // Initialize to -1 to ensure name is set the first time
		private void EnsureOrderName()
		{
			if (_columnsVersion < _columns.Version)
			{
				Name = GetFullName();
				_columnsVersion = _columns.Version;
			}
		}

		// TableVar
		[Reference]
		internal TableVar _tableVar;
		public TableVar TableVar 
		{ 
			get { return _tableVar; } 
			set
			{
				if (_tableVar != null)
					_tableVar.Orders.Remove(this);
				if (value != null)
					value.Orders.Add(this);
			}
		}
		
		// Columns
		private OrderColumns _columns = new OrderColumns();
		public OrderColumns Columns { get { return _columns; } }
		
		// IsAscending
		/// <summary>Returns true if all the columns in this order are in ascending order, false if any are descending.</summary>
		public bool IsAscending 
		{ 
			get 
			{
				for (int index = 0; index < _columns.Count; index++)
					if (!_columns[index].Ascending)
						return false;
				return true;
			}
		}
		
		// IsDescending
		/// <summary>Returns true if all the columns in this order are in descending order, false if any are ascending.</summary>
		public bool IsDescending
		{
			get
			{
				for (int index = 0; index < _columns.Count; index++)
					if (_columns[index].Ascending)
						return false;
				return true;
			}
		}

		// IsInherited
		private bool _isInherited;
		public bool IsInherited
		{
			get { return _isInherited; }
			set { _isInherited = value; }
		}

		/// <summary>Returns true if AOrder can be used to satisfy an ordering by this order.</summary>		
		public bool Equivalent(Order order)
		{
			if (Columns.Count > order.Columns.Count)
				return false;
				
			for (int index = 0; index < Columns.Count; index++)
				if (!Columns[index].Equivalent(order.Columns[index]))
					return false;
			
			return true;		
		}

        public override bool Equals(object objectValue)
        {
            // An order is equal to another order if it contains the same columns (by order and ascending)
            Order order = objectValue as Order;
            if (order != null)
				return (Columns.Count == order.Columns.Count) && Equivalent(order);

            return base.Equals(objectValue);
        }

        // GetHashCode
        public override int GetHashCode()
        {
			int hashCode = 0;
			for (int index = 0; index < _columns.Count; index++)
				hashCode ^= _columns[index].GetHashCode();
			return hashCode;
        }

        public override string ToString()
        {
			return Name;
        }
        
        public override Statement EmitStatement(EmitMode mode)
        {
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				OrderDefinition order = new OrderDefinition();
				for (int index = 0; index < Columns.Count; index++)
					order.Columns.Add(Columns[index].EmitStatement(mode));
				order.MetaData = MetaData == null ? null : MetaData.Copy();
				return order;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
        }

		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropOrderDefinition order = new DropOrderDefinition();
			for (int index = 0; index < Columns.Count; index++)
				order.Columns.Add(Columns[index].EmitStatement(mode));
			return order;
		}
        
        public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			
			OrderColumn column;
			for (int index = 0; index < Columns.Count; index++)
			{
				column = Columns[index];
				if (column.Sort != null)
					column.Sort.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			}
        }
    }

	#if USETYPEDLIST
	public class Orders : TypedList
    {
		public Orders() : base(typeof(Order)) { }
		
		public Orders(TableVar ATableVar) : base(typeof(Order)) 
		{
			FTableVar = ATableVar;
		}
	#else
	public class Orders : ValidatingBaseList<Order>
	{
	#endif
		public Orders() : base() { }
		public Orders(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
		
		[Reference]
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }
		
		#if USETYPEDLIST
		protected override void Adding(object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((Order)AItem).FTableVar = FTableVar;
		}
		
		protected override void Removing(object AItem, int AIndex)
		{
			((Order)AItem).FTableVar = null;
			base.Removing(AItem, AIndex);
		}

        public new Order this[int AIndex]
        {
            get { return (Order)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
        #else
        protected override void Adding(Order tempValue, int index)
		{
 			 //base.Adding(AValue, AIndex);
			 tempValue._tableVar = _tableVar;
		}
		
		protected override void Removing(Order tempValue, int index)
		{
 			 tempValue._tableVar = null;
 			 //base.Removing(AValue, AIndex);
		}
        #endif
        
        // ToString
        public override string ToString()
        {
			StringBuilder stringValue = new StringBuilder();
			foreach (Order order in this)
			{
				if (stringValue.Length != 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(order.ToString());
			}
			return stringValue.ToString();
        }
    }
    
	public class Key : Object
    {
		public Key() : base(String.Empty)
		{
			InternalInitialize();
			UpdateKeyName();
		}
		
		public Key(int iD) : base(iD, String.Empty)
		{
			InternalInitialize();
			UpdateKeyName();
		}
		
        public Key(MetaData metaData) : base(String.Empty)
        {
			MetaData = metaData;
			InternalInitialize();
			UpdateKeyName();
        }
        
        public Key(int iD, MetaData metaData) : base(iD, String.Empty)
        {
			MetaData = metaData;
			InternalInitialize();
			UpdateKeyName();
        }
        
        public Key(TableVarColumn[] columns) : base(String.Empty)
        {
			InternalInitialize();
			if (columns.Length > 0)
				foreach (TableVarColumn column in columns)
					_columns.Add(column);
			else
				UpdateKeyName();
        }
        
        public Key(MetaData metaData, TableVarColumn[] columns) : base(String.Empty)
        {
			MetaData = metaData;
			InternalInitialize();
			if (columns.Length > 0)
				foreach (TableVarColumn column in columns)
					_columns.Add(column);
			else
				UpdateKeyName();
        }

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Key"), DisplayName); } }

		public override int CatalogObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }

		public override int ParentObjectID { get { return _tableVar == null ? -1 : _tableVar.ID; } }

        private void InternalInitialize()
        {
			_columns = new KeyColumns(this);
        }
        
        private bool _nameCurrent = false;

        internal void UpdateKeyName()
        {
			_nameCurrent = false;
        }
        
        private void EnsureKeyName()
        {
			if (!_nameCurrent)
			{
				StringBuilder name = new StringBuilder();
				name.AppendFormat("{0} {1} ", Keywords.Key, Keywords.BeginList);
				for (int index = 0; index < _columns.Count; index++)
				{
					if (index > 0)
						name.AppendFormat("{0} ", Keywords.ListSeparator);
					name.Append(_columns[index].Name);
				}
				name.AppendFormat("{0}{1}", _columns.Count > 0 ? " " : "", Keywords.EndList);
				Name = name.ToString();
				_nameCurrent = true;
			}
        }
        
		public override string Name
        {
			get 
			{ 
				EnsureKeyName();
				return base.Name; 
			}
			set { base.Name = value; }
		}
        
        public override bool Equals(object objectValue)
        {
			Key key = objectValue as Key;
			return (key != null) && _columns.Equals(key.Columns) && (_isSparse == key.IsSparse);
        }
        
        // GetHashCode
        public override int GetHashCode()
        {
			int hashCode = 0;
			for (int index = 0; index < _columns.Count; index++)
				hashCode ^= _columns[index].Name.GetHashCode();
			return hashCode;
        }
        
        // Equivalent
        /// <summary>Returns true if this key has the same columns as the given key.</summary>
        /// <remarks>
        /// This is different than equality in that equality also considers sparseness of the key.
        /// </remarks>
        public bool Equivalent(Key key)
        {
			return _columns.Equals(key.Columns);
        }
        
		// TableVar
		[Reference]
		internal TableVar _tableVar;
		public TableVar TableVar 
		{ 
			get { return _tableVar; } 
			set
			{
				if (_tableVar != null)
					_tableVar.Keys.Remove(this);
				if (value != null)
					value.Keys.Add(this);
			}
		}
		
        // Columns
        private KeyColumns _columns;
		public KeyColumns Columns { get { return _columns; } }
        
        // IsInherited
        private bool _isInherited;
        public bool IsInherited
        {
			get { return _isInherited; }
			set { _isInherited = value; }
        }
        
        // IsSparse
        private bool _isSparse;
        /// <summary>Indicates whether or not the key will consider rows with nils for the purpose of duplicate detection</summary>
        /// <remarks>
        /// Sparse keys do not consider rows with nils, allowing multiple rows with nils for the columns of the key.
        /// Set by the DAE.IsSparse tag
        /// Sparse keys cannot be used as clustering keys.
        /// </remarks>
        public bool IsSparse
        {
			get { return _isSparse; }
			set { _isSparse = value; }
		}
		
		// IsNilable
		public bool IsNilable
		{
			get
			{
				for (int index = 0; index < _columns.Count; index++)
					if (_columns[index].IsNilable)
						return true;
				return false;
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
		
		private TransitionConstraint _constraint;
		public TransitionConstraint Constraint
		{
			get { return _constraint; }
			set { _constraint = value; }
		}
		
        // ToString
        public override string ToString()
        {
			return Name;
        }
        
        public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			
			TableVarColumn column;
			for (int index = 0; index < Columns.Count; index++)
			{
				column = Columns[index];
				// TODO: Fix this boundary-cross
				Schema.Sort sort = session.ServerProcess.ValueManager.GetUniqueSort(column.DataType);
				if (sort != null)
					sort.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
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
				KeyDefinition key = new KeyDefinition();
				foreach (TableVarColumn column in Columns)
					key.Columns.Add(new KeyColumnDefinition(Schema.Object.EnsureRooted(column.Name)));
				key.MetaData = MetaData == null ? null : MetaData.Copy();
				return key;
			}
			finally
			{
				if (mode == EmitMode.ForStorage)
					RemoveObjectID();
			}
        }

		public override Statement EmitDropStatement(EmitMode mode)
		{
			DropKeyDefinition definition = new DropKeyDefinition();
			foreach (TableVarColumn column in Columns)
				definition.Columns.Add(new KeyColumnDefinition(Schema.Object.EnsureRooted(column.Name)));
			return definition;
		}
    }
    
	public class JoinKey : System.Object
    {
		public JoinKey() : base() { }

		private JoinKeyColumns _columns = new JoinKeyColumns();
		public JoinKeyColumns Columns { get { return _columns; } }
		
		private bool _isUnique;
		public bool IsUnique
		{
			get { return _isUnique; }
			set { _isUnique = value; }
		}

        public override bool Equals(object objectValue)
        {
			JoinKey joinKey = objectValue as JoinKey;
			if (joinKey != null)
				return _columns.Equals(joinKey.Columns);

			Key key = objectValue as Key;
			if (key != null)
				return _columns.Equals(key.Columns);

			return false;
        }
        
        // GetHashCode
        public override int GetHashCode()
        {
			int hashCode = 0;
			for (int index = 0; index < _columns.Count; index++)
				hashCode ^= _columns[index].Name.GetHashCode();
			return hashCode;
        }

		public override string ToString()
		{
			StringBuilder name = new StringBuilder();
			name.AppendFormat("{0} {1} {2} ", Keywords.Join, Keywords.Key, Keywords.BeginList);
			for (int index = 0; index < _columns.Count; index++)
			{
				if (index > 0)
					name.AppendFormat("{0} ", Keywords.ListSeparator);
				name.Append(_columns[index].Name);
			}
			name.AppendFormat("{0}{1}", _columns.Count > 0 ? " " : "", Keywords.EndList);
			return name.ToString();
		}        
    }
    
    #if USETYPEDLIST
	public class Keys : TypedList
    {        
		public Keys() : base(typeof(Key)) { }
		
		public Keys(TableVar ATableVar) : base(typeof(Key))
		{
			FTableVar = ATableVar;
		}
	#else
	public class Keys : ValidatingBaseList<Key>
	{
		public Keys() : base() { }
		public Keys(TableVar tableVar) : base()
		{
			_tableVar = tableVar;
		}
	#endif
		
		[Reference]
		private TableVar _tableVar;
		public TableVar TableVar { get { return _tableVar; } }
		
		#if USETYPEDLIST
		protected override void Adding(object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((Key)AItem).FTableVar = FTableVar;
		}
		
		protected override void Removing(object AItem, int AIndex)
		{
			((Key)AItem).FTableVar = null;
			base.Removing(AItem, AIndex);
		}

        public new Key this[int AIndex]
        {
            get { return (Key)(base[AIndex]); }
            set { base[AIndex] = value; }
        }
		#else
        protected override void Adding(Key tempValue, int index)
		{
 			 //base.Adding(AValue, AIndex);
			 tempValue._tableVar = _tableVar;
		}
		
		protected override void Removing(Key tempValue, int index)
		{
 			 tempValue._tableVar = null;
 			 //base.Removing(AValue, AIndex);
		}
		#endif

        public bool IsKeyColumnName(string columnName)
        {
			foreach (Schema.Key key in this)
				if (key.Columns.ContainsName(columnName))
					return true;
			return false;
        }

		public Key SuperKey(bool allowSparse, bool allowNilable)
		{
			var isValid = false;
			var superKey = new Key();

			for (int index = 0; index < Count; index++)
			{
				if ((allowNilable || !this[index].IsNilable) && (allowSparse || !this[index].IsSparse))
				{
					isValid = true;

					for (int columnIndex = 0; columnIndex < this[index].Columns.Count; columnIndex++)
					{
						if (!superKey.Columns.ContainsName(this[index].Columns[columnIndex].Name))
							superKey.Columns.Add(this[index].Columns[columnIndex]);
					}
				}
			}

			if (!isValid)
			{
				if (allowSparse && allowNilable)
					throw new SchemaException(SchemaException.Codes.NoKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
				else if (allowNilable)
					throw new SchemaException(SchemaException.Codes.NoNonSparseKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
				else
					throw new SchemaException(SchemaException.Codes.NoNonNilableKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
			}

			return superKey;
		}
        
        public Key MinimumKey(bool allowSparse, bool allowNilable)
        {
			Key minimumKey = null;
			for (int index = Count - 1; index >= 0; index--)
			{
				if ((allowNilable || !this[index].IsNilable) && (allowSparse || !this[index].IsSparse))
					if (minimumKey == null)
						minimumKey = this[index];
					else
						if (this[index].Columns.Count < minimumKey.Columns.Count)
							minimumKey = this[index];
			}

			if (minimumKey == null)
				if (allowSparse && allowNilable)
					throw new SchemaException(SchemaException.Codes.NoKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
				else if (allowNilable)
					throw new SchemaException(SchemaException.Codes.NoNonSparseKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);
				else
					throw new SchemaException(SchemaException.Codes.NoNonNilableKeysAvailable, _tableVar == null ? "<unknown>" : _tableVar.DisplayName);

			return minimumKey;
        }
        
        public Key MinimumKey(bool allowSparse)
        {
			return MinimumKey(allowSparse, true);
        }
        
        public Key MinimumSubsetKey(TableVarColumnsBase columns, bool allowSparse, bool allowNilable)
        {
			Key minimumKey = null;
			for (int index = Count - 1; index >= 0; index--)
			{
				if ((allowNilable || !this[index].IsNilable) && (allowSparse || !this[index].IsSparse))
					if (this[index].Columns.IsSubsetOf(columns))
						if (minimumKey == null)
							minimumKey = this[index];
						else
							if (this[index].Columns.Count < minimumKey.Columns.Count)
								minimumKey = this[index];
			}

			return minimumKey;
        }

		public Key MinimumSubsetKey(TableVarColumnsBase columns, bool allowSparse)
		{
			return MinimumSubsetKey(columns, allowSparse, true);
		}

		public Key MinimumSubsetKey(TableVarColumnsBase columns)
		{
			return MinimumSubsetKey(columns, true, true);
		}
        
        // ToString
        public override string ToString()
        {
			StringBuilder stringValue = new StringBuilder();
			foreach (Key key in this)
			{
				if (stringValue.Length != 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(key.ToString());
			}
			return stringValue.ToString();
        }
    }
    
    public enum TableVarScope { Database, Session, Process }

	[Flags]
	public enum TableVarCharacteristics
	{
		None = 0,
		IsConstant = 1,
		IsModified = 2,
		ShouldDefault = 4,
		ShouldValidate = 8,
		ShouldChange = 16,
		IsDefaultRemotable = 32,
		AllDefaultsRemotable = 64,
		IsValidateRemotable = 128,
		AllValidatesRemotable = 256,
		IsChangeRemotable = 512,
		AllChangesRemotable = 1024,
		IsDeletedTable = 2048,
		HasDeferredConstraints = 4096,
		HasDeferredConstraintsComputed = 8192,
		InferredIsDefaultRemotable = 16384,
		InferredIsValidateRemotable = 32768,
		InferredIsChangeRemotable = 65536,
		ShouldReinferReferences = 131072
	}
    
    /// <summary> Base class for data table definitions </summary>
	public abstract class TableVar : CatalogObject
    {
		public TableVar(string name) : base(name)
		{
			IsRemotable = false;
			InternalInitialize();
		}
		
		public TableVar(int iD, string name) : base(iD, name)
		{
			IsRemotable = false;
			InternalInitialize();
		}
		
		public override string DisplayName { get { return (this.SourceTableName == null) ? base.DisplayName : SourceTableName; } }

		public override string[] GetRights()
		{
			return new string[]
			{
				Name + Schema.RightNames.Alter,
				Name + Schema.RightNames.Drop,
				Name + Schema.RightNames.Select,
				Name + Schema.RightNames.Insert,
				Name + Schema.RightNames.Update,
				Name + Schema.RightNames.Delete
			};
		}
		
		// DataType
		protected ITableType _dataType;
		public ITableType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}

		protected TableVarCharacteristics _characteristics;
		
		protected virtual void InternalInitialize()
		{
			_columns = new TableVarColumns(this);
			_keys = new Keys(this);
			_orders = new Orders(this);
			_characteristics =
				TableVarCharacteristics.ShouldDefault | TableVarCharacteristics.ShouldValidate | TableVarCharacteristics.ShouldChange 
					| TableVarCharacteristics.IsDefaultRemotable | TableVarCharacteristics.AllDefaultsRemotable 
					| TableVarCharacteristics.IsValidateRemotable | TableVarCharacteristics.AllValidatesRemotable 
					| TableVarCharacteristics.IsChangeRemotable | TableVarCharacteristics.AllChangesRemotable;
		}
		
		public void ResetHasDeferredConstraintsComputed()
		{
			hasDeferredConstraintsComputed = false;
		}
		
		public void ValidateChildObjectName(string name)
		{
			if 
				(
					(_columns.IndexOfName(name) >= 0) ||
					(_constraints != null && _constraints.IndexOfName(name) >= 0)
				)
			{
				throw new SchemaException(SchemaException.Codes.DuplicateChildObjectName, name);
			}
		}
		
		// Columns
		private TableVarColumns _columns;
		public TableVarColumns Columns { get { return _columns; } }
		
		// Keys
		private Keys _keys;
		public Keys Keys { get { return _keys; } }
		
		/// <summary>Returns the index of the key with the same columns as the given key.</summary>
		/// <remarks>
		/// This is not the same as using Keys.IndexOf, because that method is based on key equality,
		/// which includes the sparseness of the key. This method is used to find a key by columns only,
		/// and will return the first key with the same columns.
		/// </remarks>
		public int IndexOfKey(Key key)
		{
			for (int index = 0; index < _keys.Count; index++)
				if (Keys[index].Equivalent(key))
					return index;
			return -1;
		}
		
		// EnsureTableVarColumns()
		public void EnsureTableVarColumns()
		{
			foreach (Schema.Column column in DataType.Columns)
				if (!_columns.ContainsName(column.Name))
					_columns.Add(new Schema.TableVarColumn(column));
		}
		
		// Orders
		private Orders _orders;
		public Orders Orders { get { return _orders; } }
		
		public int IndexOfOrder(Order order)
		{
			for (int index = 0; index < Orders.Count; index++)
				if (Orders[index].Equals(order))
					return index;
					
			return -1;
		}

		public bool HasConstraints()
		{
			return _constraints != null && _constraints.Count > 0;
		}
		
		// Constraints
		private TableVarConstraints _constraints;
		public TableVarConstraints Constraints 
		{ 
			get 
			{ 
				if (_constraints == null)
					_constraints = new TableVarConstraints(this);
				return _constraints; 
			} 
		}
		
		private RowConstraints _rowConstraints;
		public RowConstraints RowConstraints 
		{ 
			get 
			{ 
				if (_rowConstraints == null)
					_rowConstraints = new RowConstraints();
				return _rowConstraints; 
			} 
		}

		public bool HasRowConstraints()
		{
			return _rowConstraints != null && _rowConstraints.Count > 0;
		}
		
		public bool IsConstant 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsConstant) == TableVarCharacteristics.IsConstant; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsConstant; else _characteristics &= ~TableVarCharacteristics.IsConstant; }
		}

		public bool IsModified 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsModified) == TableVarCharacteristics.IsModified; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsModified; else _characteristics &= ~TableVarCharacteristics.IsModified; }
		}
		
		/// <summary>Indicates whether this table variable is instanced at the database, session, or process level</summary>
		public TableVarScope Scope;

		// List of references in which this table variable is involved as either a source or target table variable
		[Reference]
		private References _references;
		public References References
		{
			get
			{
				if (_references == null)
					_references = new References();
				return _references;
			}
		}

		public bool HasReferences()
		{
			return _references != null && _references.Count > 0;
		}

		//// List of references in which this table variable is involved as a source
		//[Reference]
		//private References _sourceReferences;
		//public References SourceReferences 
		//{ 
		//	get 
		//	{ 
		//		if (_sourceReferences == null)
		//			_sourceReferences = new References();
		//		return _sourceReferences; 
		//	} 
		//}

		//public bool HasSourceReferences()
		//{
		//	return _sourceReferences != null && _sourceReferences.Count > 0;
		//}
		
		//// List of references in which this table variable is involved as a target
		//[Reference]
		//private References _targetReferences;
		//public References TargetReferences 
		//{ 
		//	get 
		//	{ 
		//		if (_targetReferences == null)
		//			_targetReferences = new References();
		//		return _targetReferences; 
		//	} 
		//}

		//public bool HasTargetReferences()
		//{
		//	return _targetReferences != null && _targetReferences.Count > 0;
		//}

		//// List of references derived by type inference, not actually present in the catalog.
		//[Reference]
		//private References _derivedReferences;
		//public References DerivedReferences 
		//{ 
		//	get 
		//	{ 
		//		if (_derivedReferences == null)
		//			_derivedReferences = new References();
		//		return _derivedReferences; 
		//	} 
		//}

		//public bool HasDerivedReferences()
		//{
		//	return _derivedReferences != null && _derivedReferences.Count > 0;
		//}
		
		public bool HasHandlers()
		{
			return (_eventHandlers != null) && (_eventHandlers.Count > 0);
		}
		
		public bool HasHandlers(EventType eventType)
		{
			return (_eventHandlers != null) && _eventHandlers.HasHandlers(eventType);
		}

		// List of EventHandlers associated with this table variable
		private TableVarEventHandlers _eventHandlers;
		public TableVarEventHandlers EventHandlers 
		{ 
			get 
			{ 
				if (_eventHandlers == null)
					_eventHandlers = new TableVarEventHandlers(this);
				return _eventHandlers; 
			} 
		}
		
        // ShouldDefault
		public bool ShouldDefault 
		{ 
			get { return (_characteristics & TableVarCharacteristics.ShouldDefault) == TableVarCharacteristics.ShouldDefault; } 
			set { if (value) _characteristics |= TableVarCharacteristics.ShouldDefault; else _characteristics &= ~TableVarCharacteristics.ShouldDefault; }
		}

        // ShouldValidate
		public bool ShouldValidate 
		{ 
			get { return (_characteristics & TableVarCharacteristics.ShouldValidate) == TableVarCharacteristics.ShouldValidate; } 
			set { if (value) _characteristics |= TableVarCharacteristics.ShouldValidate; else _characteristics &= ~TableVarCharacteristics.ShouldValidate; }
		}

        // ShouldChange
		public bool ShouldChange 
		{ 
			get { return (_characteristics & TableVarCharacteristics.ShouldChange) == TableVarCharacteristics.ShouldChange; } 
			set { if (value) _characteristics |= TableVarCharacteristics.ShouldChange; else _characteristics &= ~TableVarCharacteristics.ShouldChange; }
		}
        
        // IsDefaultRemotable
		public bool IsDefaultRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsDefaultRemotable) == TableVarCharacteristics.IsDefaultRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsDefaultRemotable; else _characteristics &= ~TableVarCharacteristics.IsDefaultRemotable; }
		}

		private bool allDefaultsRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.AllDefaultsRemotable) == TableVarCharacteristics.AllDefaultsRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.AllDefaultsRemotable; else _characteristics &= ~TableVarCharacteristics.AllDefaultsRemotable; }
		}

        public bool IsDefaultCallRemotable(string columnName)
        {
			// A default call is remotable if the table level default is remotable, 
			// and either a column is specified and that column level default is remotable, 
			// or no column name is specified and all column level defaults are remotable
			return
				IsDefaultRemotable &&
					(
						(columnName == String.Empty) ? 
							allDefaultsRemotable : 
							Columns[columnName].IsDefaultRemotable
					);
        }
        
        // IsValidateRemotable
		public bool IsValidateRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsValidateRemotable) == TableVarCharacteristics.IsValidateRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsValidateRemotable; else _characteristics &= ~TableVarCharacteristics.IsValidateRemotable; }
		}
        
		private bool allValidatesRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.AllValidatesRemotable) == TableVarCharacteristics.AllValidatesRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.AllValidatesRemotable; else _characteristics &= ~TableVarCharacteristics.AllValidatesRemotable; }
		}

        public bool IsValidateCallRemotable(string columnName)
        {
			// A Validate call is remotable if the table level Validate is remotable, 
			// and either a column is specified and that column level Validate is remotable, 
			// or no column name is specified and all column level Validates are remotable
			return
				IsValidateRemotable &&
					(
						(columnName == String.Empty) ? 
							allValidatesRemotable : 
							Columns[columnName].IsValidateRemotable
					);
        }
        
        // IsChangeRemotable
		public bool IsChangeRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsChangeRemotable) == TableVarCharacteristics.IsChangeRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsChangeRemotable; else _characteristics &= ~TableVarCharacteristics.IsChangeRemotable; }
		}
        
		private bool allChangesRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.AllChangesRemotable) == TableVarCharacteristics.AllChangesRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.AllChangesRemotable; else _characteristics &= ~TableVarCharacteristics.AllChangesRemotable; }
		}

        public bool IsChangeCallRemotable(string columnName)
        {
			// A Change call is remotable if the table level Change is remotable, 
			// and either a column is specified and that column level Change is remotable, 
			// or no column name is specified and all column level Changes are remotable
			return
				IsChangeRemotable &&
					(
						(columnName == String.Empty) ? 
							allChangesRemotable : 
							Columns[columnName].IsChangeRemotable
					);
        }
        
        public virtual void DetermineShouldCallProposables(bool reset)
        {
			if (reset)
			{
				ShouldChange = false;
				ShouldDefault = false;
				ShouldValidate = false;
			}
			
			foreach (TableVarColumn column in Columns)
			{
				column.DetermineShouldCallProposables(reset);
				ShouldChange = ShouldChange || column.ShouldChange;
				ShouldDefault = ShouldDefault || column.ShouldDefault;
				ShouldValidate = ShouldValidate || column.ShouldValidate;
			}
			
			if (_rowConstraints != null && _rowConstraints.Count > 0)
				ShouldValidate = true;
			
			if (HasHandlers())
			{
				foreach (EventHandler handler in _eventHandlers)
				{
					if ((handler.EventType & EventType.Default) != 0)
						ShouldDefault = true;
					
					if ((handler.EventType & EventType.Validate) != 0)
						ShouldValidate = true;
					
					if ((handler.EventType & EventType.Change) != 0)
						ShouldChange = true;
				}
			}
        }
        
		public override void DetermineRemotable(CatalogDeviceSession session)
		{
			IsDefaultRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsDefaultRemotable", "true"));
			IsValidateRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsValidateRemotable", "true"));
			IsChangeRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsChangeRemotable", "true"));
			
			foreach (TableVarColumn column in Columns)
			{
				allDefaultsRemotable = allDefaultsRemotable && column.IsDefaultRemotable;
				allValidatesRemotable = allValidatesRemotable && column.IsValidateRemotable;
				allChangesRemotable = allChangesRemotable && column.IsChangeRemotable;
			}
			
			allDefaultsRemotable = allChangesRemotable && allDefaultsRemotable;
			IsDefaultRemotable = IsChangeRemotable && IsDefaultRemotable;
			
			if (_eventHandlers != null)
			{
				foreach (EventHandler handler in _eventHandlers)
				{
					if ((handler.EventType & EventType.Default) != 0)
						IsDefaultRemotable = IsDefaultRemotable && handler.IsRemotable;
					
					if ((handler.EventType & EventType.Validate) != 0)
						IsValidateRemotable = IsValidateRemotable && handler.IsRemotable;
					
					if ((handler.EventType & EventType.Change) != 0)
						IsChangeRemotable = IsChangeRemotable && handler.IsRemotable;
				}
			}
		}
		
		// SourceTableName
		private string _sourceTableName = null;
		/// <summary>The name of the application transaction table variable.</summary>
		public string SourceTableName
		{
			get { return _sourceTableName; }
			set { _sourceTableName = value; }
		}
		
		/// <summary>Indicates whether or not this is the deleted tracking table for a translated table variable.</summary>
		/// <remarks>This property will only be set if SourceTableName is not null.</remarks>
		public bool IsDeletedTable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.IsDeletedTable) == TableVarCharacteristics.IsDeletedTable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.IsDeletedTable; else _characteristics &= ~TableVarCharacteristics.IsDeletedTable; }
		}
		
		public override bool IsATObject { get { return _sourceTableName != null; } }
		
		// ShouldTranslate
		/// <summary>Indicates whether or not this table variable should be included in an application transaction.</summary>
		/// <remarks>
		/// By default, all table variables are included in an application transaction.  To disable application 
		/// transaction inclusion of a table variable, use the DAE.ShouldTranslate tag.
		/// </remarks>
		public bool ShouldTranslate
		{
			get { return Boolean.Parse(MetaData.GetTag(MetaData, "DAE.ShouldTranslate", "true")); }
			set 
			{ 
				if (MetaData == null)
					MetaData = new MetaData();
				MetaData.Tags.AddOrUpdate("DAE.ShouldTranslate", value.ToString(), true); 
			}
		}
		
		#if USEINVARIANT
		// CalculateInvariant
		// The invariant of a table variable is the intersection of all source reference columns which involve a key column
		public Key CalculateInvariant()
		{
			Key LInvariant = null;
			foreach (Reference LReference in FSourceReferences)
			{
				bool LReferenceIncluded = false;
				foreach (TableVarColumn LColumn in LReference.SourceKey.Columns)
				{
					foreach (Key LKey in Keys)
						if (LKey.Columns.ContainsName(LColumn.Name))
						{
							LReferenceIncluded = true;
							break;
						}
					if (LReferenceIncluded)
						break;
				}
				
				if (LReferenceIncluded)
				{
					Key LIntersection = new Key();
					foreach (TableVarColumn LColumn in LReference.SourceKey.Columns)
						if ((LInvariant == null) || (LInvariant.Columns.ContainsName(LColumn.Name)))
							LIntersection.Columns.Add(LColumn);
					LInvariant = LIntersection;
				}
			}
			
			if (LInvariant == null)
				LInvariant = new Key();
				
			return LInvariant;
		}
		#endif

		// Transition Constraints which have an OnInsertNode		
		private TransitionConstraints _insertConstraints;
		public TransitionConstraints InsertConstraints 
		{ 
			get 
			{ 
				if (_insertConstraints == null)
					_insertConstraints = new TransitionConstraints();
				return _insertConstraints; 
			} 
		}

		public bool HasInsertConstraints()
		{
			return _insertConstraints != null && _insertConstraints.Count > 0;
		}

		// Transition constraints which have an OnUpdateNode
		private TransitionConstraints _updateConstraints;
		public TransitionConstraints UpdateConstraints 
		{ 
			get 
			{ 
				if (_updateConstraints == null)
					_updateConstraints = new TransitionConstraints();
				return _updateConstraints; 
			} 
		}

		public bool HasUpdateConstraints()
		{
			return _updateConstraints != null && _updateConstraints.Count > 0;
		}

		// Transition constraints which have an OnDeleteNode
		private TransitionConstraints _deleteConstraints;
		public TransitionConstraints DeleteConstraints 
		{ 
			get 
			{ 
				if (_deleteConstraints == null)
					_deleteConstraints = new TransitionConstraints();
				return _deleteConstraints; 
			} 
		}

		public bool HasDeleteConstraints()
		{
			return _deleteConstraints != null && _deleteConstraints.Count > 0;
		}

		// List of database-wide constraints that reference this table		
		[Reference]
		private CatalogConstraints _catalogConstraints; 
		public CatalogConstraints CatalogConstraints 
		{ 
			get 
			{ 
				if (_catalogConstraints == null)
					_catalogConstraints = new CatalogConstraints();
				return _catalogConstraints; 
			} 
		}

		public bool HasCatalogConstraints()
		{
			return _catalogConstraints != null && _catalogConstraints.Count > 0;
		}
		
		// HasDeferredConstraints
		private bool hasDeferredConstraints 
		{ 
			get { return (_characteristics & TableVarCharacteristics.HasDeferredConstraints) == TableVarCharacteristics.HasDeferredConstraints; } 
			set { if (value) _characteristics |= TableVarCharacteristics.HasDeferredConstraints; else _characteristics &= ~TableVarCharacteristics.HasDeferredConstraints; }
		}

		private bool hasDeferredConstraintsComputed 
		{ 
			get { return (_characteristics & TableVarCharacteristics.HasDeferredConstraintsComputed) == TableVarCharacteristics.HasDeferredConstraintsComputed; } 
			set { if (value) _characteristics |= TableVarCharacteristics.HasDeferredConstraintsComputed; else _characteristics &= ~TableVarCharacteristics.HasDeferredConstraintsComputed; }
		}
		
		/// <summary>Indicates whether this table variable has any enforced deferred constraints defined.</summary>
		public bool HasDeferredConstraints()
		{
			if (!hasDeferredConstraintsComputed)
			{
				hasDeferredConstraints = false;
				if (_constraints != null)
				{
					foreach (TableVarConstraint constraint in _constraints)
						if (constraint.Enforced && constraint.IsDeferred)
						{
							hasDeferredConstraints = true;
							break;
						}
				}
				hasDeferredConstraintsComputed = true;
			}
			
			return hasDeferredConstraints;
		}
		
		/// <summary>Indicates whether this table variable has any enforced deferred constraints defined that would need validation based on the given value flags</summary>
		public bool HasDeferredConstraints(BitArray valueFlags, Schema.Transition transition)
		{
			// TODO: Potential cache point, this only needs to be computed once per ValueFlags combination.
			if (_constraints != null)
				for (int index = 0; index < _constraints.Count; index++)
					if (_constraints[index].Enforced && _constraints[index].IsDeferred && _constraints[index].ShouldValidate(valueFlags, transition))
						return true;
			return false;
		}
		
		public void CopyTableVar(TableNode sourceNode)
		{
			CopyTableVar(sourceNode, false);
		}
		
		// IsInference indicates whether this copy should be an inference, as in the case of a table var inference on a view
		public void CopyTableVar(TableNode sourceNode, bool isInference)
		{
			// create datatype
			DataType = new Schema.TableType();
				
			// Copy MetaData for the table variable
			if (isInference)
				InheritMetaData(sourceNode.TableVar.MetaData);
			else
				MergeMetaData(sourceNode.TableVar.MetaData);

			// Copy columns
			Schema.TableVarColumn newColumn;
			foreach (Schema.TableVarColumn column in sourceNode.TableVar.Columns)
			{
				if (isInference)
					newColumn = column.Inherit();
				else
					newColumn = column.Copy();
				DataType.Columns.Add(newColumn.Column);
				Columns.Add(newColumn);
			}
			
			ShouldChange = sourceNode.TableVar.ShouldChange;
			ShouldDefault = sourceNode.TableVar.ShouldDefault;
			ShouldValidate = sourceNode.TableVar.ShouldValidate;
			
			if ((!sourceNode.TableVar.IsChangeRemotable || !sourceNode.TableVar.IsDefaultRemotable || !sourceNode.TableVar.IsValidateRemotable) && MetaData != null)
			{
				if (!sourceNode.TableVar.IsChangeRemotable && MetaData.Tags.Contains("DAE.IsChangeRemotable"))
					MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", "false", true);
					
				if (!sourceNode.TableVar.IsDefaultRemotable && MetaData.Tags.Contains("DAE.IsDefaultRemotable"))
					MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", "false", true);
					
				if (!sourceNode.TableVar.IsValidateRemotable && MetaData.Tags.Contains("DAE.IsValidateRemotable"))
					MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", "false", true);
			}
			
			// Copy keys
			Schema.Key newKey;
			foreach (Schema.Key key in sourceNode.TableVar.Keys)
			{
				newKey = new Schema.Key();
				newKey.IsInherited = true;
				newKey.IsSparse = key.IsSparse;
				if (isInference)
					newKey.InheritMetaData(key.MetaData);
				else
					newKey.MergeMetaData(key.MetaData);
				foreach (Schema.TableVarColumn column in key.Columns)
					newKey.Columns.Add(Columns[column]);
				Keys.Add(newKey);
			}
			
			// Copy orders
			Schema.Order newOrder;
			Schema.OrderColumn newOrderColumn;
			foreach (Schema.Order order in sourceNode.TableVar.Orders)
			{
				newOrder = new Schema.Order();
				newOrder.IsInherited = true;
				if (isInference)
					newOrder.InheritMetaData(order.MetaData);
				else
					newOrder.MergeMetaData(order.MetaData);
				Schema.OrderColumn orderColumn;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					orderColumn = order.Columns[index];
					newOrderColumn = new Schema.OrderColumn(Columns[orderColumn.Column], orderColumn.Ascending, orderColumn.IncludeNils);
					newOrderColumn.Sort = orderColumn.Sort;
					newOrderColumn.IsDefaultSort = orderColumn.IsDefaultSort;
					newOrder.Columns.Add(newOrderColumn);
					Error.AssertWarn(newOrderColumn.Sort != null, "Sort is null");
				}
				Orders.Add(newOrder);
			}
		}
		
		protected void EmitColumns(EmitMode mode, TableTypeSpecifier specifier)
		{
			NamedTypeSpecifier columnSpecifier;
			foreach (TableVarColumn column in Columns)
			{
				columnSpecifier = new NamedTypeSpecifier();
				columnSpecifier.Identifier = column.Name;
				columnSpecifier.TypeSpecifier = column.DataType.EmitSpecifier(mode);
				specifier.Columns.Add(columnSpecifier);
			}
		}
		
		public override void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			if ((SourceTableName != null) && (mode == EmitMode.ForRemote))
				sourceCatalog[sourceCatalog.IndexOfName(SourceTableName)].IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
			else
			{
				if (!targetCatalog.Contains(Name))
				{
					targetCatalog.Add(this);		// this needs to be added before tracing dependencies to avoid recursion

					base.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);

					foreach (TableVarColumn column in Columns)
						column.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
						
					foreach (Key key in Keys)
						key.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
						
					foreach (Order order in Orders)
						order.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
						
					if (HasConstraints())
						foreach (Constraint constraint in Constraints)
							if ((mode != EmitMode.ForRemote) || constraint.IsRemotable)
								constraint.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);	
				}
			}
		}
		
		public override void IncludeHandlers(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			foreach (TableVarColumn column in Columns)
			{
				if (column.DataType is Schema.ScalarType)
					((Schema.ScalarType)column.DataType).IncludeHandlers(session, sourceCatalog, targetCatalog, mode);
				column.IncludeHandlers(session, sourceCatalog, targetCatalog, mode);
			}

			if (_eventHandlers != null)
				foreach (EventHandler handler in _eventHandlers)
					if ((mode != EmitMode.ForRemote) || handler.IsRemotable)
						handler.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
		}
		
		public void IncludeLookupAndDetailReferences(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog)
		{
			if (HasReferences())
			{
				foreach (ReferenceBase reference in References)
				{
					if (reference.SourceTable.Equals(this) && !reference.IsDerived && !reference.SourceKey.IsUnique && !targetCatalog.Contains(reference.Name))
					{
						reference.IncludeDependencies(session, sourceCatalog, targetCatalog, EmitMode.ForRemote);
						reference.TargetTable.IncludeReferences(session, sourceCatalog, targetCatalog);
					}

					if (reference.TargetTable.Equals(this) && !reference.IsDerived && !reference.SourceKey.IsUnique && !targetCatalog.Contains(reference.Name))
						reference.IncludeDependencies(session, sourceCatalog, targetCatalog, EmitMode.ForRemote);
				}
			}
		}
		
		public void IncludeParentReferences(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog)
		{
			IncludeLookupAndDetailReferences(session, sourceCatalog, targetCatalog);

			if (HasReferences())
				foreach (ReferenceBase reference in References)
					if (reference.SourceTable.Equals(this) && !reference.IsDerived && reference.SourceKey.IsUnique && !targetCatalog.Contains(reference.Name))
					{
						reference.IncludeDependencies(session, sourceCatalog, targetCatalog, EmitMode.ForRemote);
						reference.TargetTable.IncludeParentReferences(session, sourceCatalog, targetCatalog);
					}
		}
		
		public void IncludeExtensionReferences(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog)
		{
			if (HasReferences())
				foreach (ReferenceBase reference in References)
					if (reference.TargetTable.Equals(this) && !reference.IsDerived && reference.SourceKey.IsUnique && !targetCatalog.Contains(reference.Name))
					{
						reference.IncludeDependencies(session, sourceCatalog, targetCatalog, EmitMode.ForRemote);
						reference.SourceTable.IncludeLookupAndDetailReferences(session, sourceCatalog, targetCatalog);
					}
		}
		
		public void IncludeReferences(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog)
		{
			IncludeParentReferences(session, sourceCatalog, targetCatalog);
			IncludeExtensionReferences(session, sourceCatalog, targetCatalog);
		}

		protected void EmitTableVarStatement(EmitMode mode, CreateTableVarStatement statement)
		{
			// The session view created to describe the results of a select statement through the CLI should not be created as a session view
			// in the remote host catalog. The session object name for this view is the same as the object name for the view, and this is
			// the only way that a given catalog object can have the same name in both the session and global catalogs.
			//if ((SessionObjectName != null) && (AMode != EmitMode.ForRemote) && (Schema.Object.EnsureRooted(SessionObjectName) != AStatement.TableVarName))
			if ((SessionObjectName != null) && (Schema.Object.EnsureRooted(SessionObjectName) != statement.TableVarName))
			{
				statement.IsSession = true;
				statement.TableVarName = Schema.Object.EnsureRooted(SessionObjectName);
			}
			
			if ((SourceTableName != null) && (mode == EmitMode.ForCopy))
				statement.TableVarName = Schema.Object.EnsureRooted(SourceTableName);

			for (int index = 0; index < Keys.Count; index++)
				if (!Keys[index].IsInherited || (statement is CreateTableStatement))
					statement.Keys.Add(Keys[index].EmitStatement(mode));
					
			for (int index = 0; index < Orders.Count; index++)
				if (!Orders[index].IsInherited || (statement is CreateTableStatement))
					statement.Orders.Add(Orders[index].EmitStatement(mode));
					
			if (HasConstraints())
				for (int index = 0; index < Constraints.Count; index++)
					if ((Constraints[index].ConstraintType == ConstraintType.Row) && ((mode != EmitMode.ForRemote) || Constraints[index].IsRemotable) && ((mode != EmitMode.ForStorage) || !Constraints[index].IsPersistent))
						statement.Constraints.Add(Constraints[index].EmitDefinition(mode));

			statement.MetaData = MetaData == null ? new MetaData() : MetaData.Copy();
			if (SessionObjectName != null)
				statement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", Name, true);
		}

		public Statement EmitDefinition(EmitMode mode)
		{
			CreateTableStatement statement = new CreateTableStatement();
			statement.TableVarName = Schema.Object.EnsureRooted(Name);
			EmitTableVarStatement(mode, statement);
			foreach (TableVarColumn column in Columns)
				statement.Columns.Add(column.EmitStatement(mode));
			statement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (mode == EmitMode.ForRemote)
			{
				if (statement.MetaData == null)
					statement.MetaData = new MetaData();
				statement.MetaData.Tags.Add(new Tag("DAE.IsDefaultRemotable", IsDefaultRemotable.ToString()));
				statement.MetaData.Tags.Add(new Tag("DAE.IsChangeRemotable", IsChangeRemotable.ToString()));
				statement.MetaData.Tags.Add(new Tag("DAE.IsValidateRemotable", IsValidateRemotable.ToString()));
			}
			return statement;
		}
		
		public override Object GetObjectFromHeader(ObjectHeader header)
		{
			switch (header.ObjectType)
			{
				case "TableVarColumn" :
					foreach (TableVarColumn column in Columns)
						if (header.ID == column.ID)
							return column;
				break;
							
				case "TableVarColumnDefault" :
				case "TableVarColumnConstraint" :
				//case "TableVarColumnEventHandler" :
					foreach (TableVarColumn column in Columns)
						if (header.ParentObjectID == column.ID)
							return column.GetObjectFromHeader(header);
				break;

				case "Key" :
					foreach (Key key in Keys)
						if (header.ID == key.ID)
							return key;
				break;
							
				case "Order" :
					foreach (Order order in Orders)
						if (header.ID == order.ID)
							return order;
				break;
							
				case "RowConstraint" :
				case "TransitionConstraint" :
					if (_constraints != null)
						foreach (Constraint constraint in _constraints)
							if (constraint.ID == header.ID)
								return constraint;
				break;
			}
			
			return base.GetObjectFromHeader(header);
		}

		public void SetShouldReinferReferences(CatalogDeviceSession session)
		{
			List<Schema.DependentObjectHeader> headers = session.SelectObjectDependents(ID, false);
			for (int index = 0; index < headers.Count; index++)
				if (headers[index].ObjectType == "DerivedTableVar")
					session.MarkViewForRecompile(headers[index].ID);
		}
	}
    
	public class BaseTableVar : TableVar
    {
		public BaseTableVar(string name) : base(name) {}
		public BaseTableVar(int iD, string name) : base(iD, name) {}
		public BaseTableVar(string name, ITableType tableType) : base(name)
		{
			DataType = tableType;
		}
		
		public BaseTableVar(string name, ITableType tableType, Device device) : base(name)
		{
			DataType = tableType;
			Device = device;
		}
		
		public BaseTableVar(string name, ITableType tableType, Device device, bool isConstant) : base(name)
		{
			DataType = tableType;
			Device = device;
			IsConstant = isConstant;
		}
		
		public BaseTableVar(ITableType tableType) : base(Schema.Object.GetUniqueName())
		{
			DataType = tableType;
		}
		
		public BaseTableVar(ITableType tableType, Device device) : base(Schema.Object.GetUniqueName())
		{
			DataType = tableType;
			Device = device;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.BaseTableVar"), DisplayName); } }

		// Device
		[Reference]
		private Device _device;
		public Device Device
		{
			get { return _device; }
			set { _device = value; }
		}
		
		public override void DetermineRemotable(CatalogDeviceSession session)
		{
			base.DetermineRemotable(session);
			DetermineShouldCallProposables(true);
		}
		
		public override Statement EmitStatement(EmitMode mode)
		{
			if (mode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();
			try
			{
				CreateTableStatement statement = new CreateTableStatement();
				statement.TableVarName = Schema.Object.EnsureRooted(Name);
				EmitTableVarStatement(mode, statement);
				statement.DeviceName = new IdentifierExpression(Device == null ? String.Empty : Device.Name);
				foreach (TableVarColumn column in Columns)
					statement.Columns.Add(column.EmitStatement(mode));
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
			DropTableStatement statement = new DropTableStatement();
			statement.ObjectName = Schema.Object.EnsureRooted(Name);
			return statement;
		}
	}

	// ResultTableVar is the schema stub for the intermediate result of a relational operator in D4.
	// These are not persisted in the catalog, they only exist in the context of a compiled plan.
	public class ResultTableVar : TableVar
	{
		public ResultTableVar(TableNode node) : base(Schema.Object.GetNextObjectID(), Schema.Object.GetUniqueName())
		{
			_node = node;
			DataType = node.DataType;
		}

		// Node
		private PlanNode _node;
		public PlanNode Node
		{
			get { return _node; }
			set { _node = value; }
		}

		public override string DisplayName { get { return "<intermediate result>"; } }

		protected override void InternalInitialize()
		{
			base.InternalInitialize();
			_characteristics |= TableVarCharacteristics.InferredIsDefaultRemotable | TableVarCharacteristics.InferredIsValidateRemotable | TableVarCharacteristics.InferredIsChangeRemotable;
		}

		public bool InferredIsDefaultRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.InferredIsDefaultRemotable) == TableVarCharacteristics.InferredIsDefaultRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.InferredIsDefaultRemotable; else _characteristics &= ~TableVarCharacteristics.InferredIsDefaultRemotable; }
		}

		public bool InferredIsValidateRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.InferredIsValidateRemotable) == TableVarCharacteristics.InferredIsValidateRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.InferredIsValidateRemotable; else _characteristics &= ~TableVarCharacteristics.InferredIsValidateRemotable; }
		}

		public bool InferredIsChangeRemotable 
		{ 
			get { return (_characteristics & TableVarCharacteristics.InferredIsChangeRemotable) == TableVarCharacteristics.InferredIsChangeRemotable; } 
			set { if (value) _characteristics |= TableVarCharacteristics.InferredIsChangeRemotable; else _characteristics &= ~TableVarCharacteristics.InferredIsChangeRemotable; }
		}
		
		public override void DetermineRemotable(CatalogDeviceSession session)
		{
			base.DetermineRemotable(session);
			
			IsDefaultRemotable = IsDefaultRemotable && InferredIsDefaultRemotable;
			IsValidateRemotable = IsValidateRemotable && InferredIsValidateRemotable;
			IsChangeRemotable = IsChangeRemotable && InferredIsChangeRemotable;
		}
	}

	public class DerivedTableVar : TableVar
    {	
		public DerivedTableVar(string name) : base(name) {}
		public DerivedTableVar(int iD, string name) : base(iD, name) {}
		public DerivedTableVar(string name, ITableType tableType) : base(name)
		{
			DataType = tableType;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DerivedTableVar"), DisplayName); } }
		
		// ShouldReinferReferences - True if the references for this view should be reinferred when it is next referenced
		public bool ShouldReinferReferences 
		{ 
			get { return (_characteristics & TableVarCharacteristics.ShouldReinferReferences) == TableVarCharacteristics.ShouldReinferReferences; } 
			set { if (value) _characteristics |= TableVarCharacteristics.ShouldReinferReferences; else _characteristics &= ~TableVarCharacteristics.ShouldReinferReferences; }
		}

		public override void DetermineRemotable(CatalogDeviceSession session)
		{
			base.DetermineRemotable(session);
			DetermineShouldCallProposables(false);
		}
		
		/*
			OriginalExpression is supposed to be the user-provided definition of the view.
			The problem with using OriginalExpression to emit the view definition is that the
			user-provided definition may be context-dependent, i.e. it may have to be resolved
			with a specific name-resolution path.  I believe that the reason we were using
			OriginalExpression in the first place is because statement emission for views
			expanded the view definition.  Because this is no longer the case (it is no
			longer required for A/T inclusion) the original expression should be safely discardable.
		*/
		#if USEORIGINALEXPRESSION
		// OriginalExpression is the original user-provided definition of the view
		protected Expression FOriginalExpression;
		public Expression OriginalExpression
		{
			get { return FOriginalExpression; }
			set { FOriginalExpression = value; }
		}
		#endif
		
		// InvocationExpression is the expression to be in-line compiled into the expression referencing this view
		protected Expression _invocationExpression;
		public Expression InvocationExpression
		{
			get { return _invocationExpression; }
			set { _invocationExpression = value; }
		}

		public void CopyReferences(TableNode sourceNode)
		{
			if (sourceNode.TableVar.HasReferences())
				foreach (Schema.ReferenceBase reference in sourceNode.TableVar.References)
				{
					if (reference.SourceTable.Equals(sourceNode.TableVar))
					{
						Schema.DerivedReference newReference = new Schema.DerivedReference(reference.Name, reference);
						newReference.IsExcluded = reference.IsExcluded;
						newReference.InheritMetaData(reference.MetaData);
						//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
						//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
						newReference.AddDependencies(reference.Dependencies);
						newReference.SourceTable = this;
						newReference.SourceKey.IsUnique = reference.SourceKey.IsUnique;
						newReference.SourceKey.Columns.AddRange(reference.SourceKey.Columns);
						newReference.TargetTable = reference.TargetTable;
						newReference.TargetKey.IsUnique = reference.TargetKey.IsUnique;
						newReference.TargetKey.Columns.AddRange(reference.TargetKey.Columns);
						References.Add(newReference);
					}
					else if (reference.TargetTable.Equals(sourceNode.TableVar) && !References.Contains(reference.Name))
					{
						Schema.DerivedReference newReference = new Schema.DerivedReference(reference.Name, reference);
						newReference.IsExcluded = reference.IsExcluded;
						newReference.InheritMetaData(reference.MetaData);
						//newReference.UpdateReferenceAction = reference.UpdateReferenceAction;
						//newReference.DeleteReferenceAction = reference.DeleteReferenceAction;
						newReference.AddDependencies(reference.Dependencies);
						newReference.SourceTable = reference.SourceTable;
						newReference.SourceKey.IsUnique = reference.SourceKey.IsUnique;
						newReference.SourceKey.Columns.AddRange(reference.SourceKey.Columns);
						newReference.TargetTable = this;
						newReference.TargetKey.IsUnique = reference.TargetKey.IsUnique;
						newReference.TargetKey.Columns.AddRange(reference.TargetKey.Columns);
						References.Add(newReference);
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
				CreateViewStatement statement = new CreateViewStatement();
				statement.TableVarName = Schema.Object.EnsureRooted(Name);
				EmitTableVarStatement(mode, statement);
				#if USEORIGINALEXPRESSION
				statement.Expression = FOriginalExpression;
				#else
				statement.Expression = _invocationExpression;
				#endif
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
			DropViewStatement statement = new DropViewStatement();
			statement.ObjectName = Schema.Object.EnsureRooted(Name);
			return statement;
		}
    }
}

