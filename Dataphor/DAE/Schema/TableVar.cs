/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	public enum TableVarColumnType { Stored, Virtual, RowExists, Level, Sequence, InternalID }
	
	/// <remarks> Provides the representation for a column header (Name:DataType) </remarks>
	public class TableVarColumn : Object
    {
		public TableVarColumn(Column AColumn) : base(AColumn.Name)
		{
			SetColumn(AColumn);
		}
		
		public TableVarColumn(Column AColumn, TableVarColumnType AColumnType) : base(AColumn.Name)
		{
			SetColumn(AColumn);
			FColumnType = AColumnType;
			FReadOnly = !((FColumnType == TableVarColumnType.Stored) || (FColumnType == TableVarColumnType.RowExists));
		}
		
		public TableVarColumn(Column AColumn, MetaData AMetaData) : base(AColumn.Name)
		{
			SetColumn(AColumn);
			MergeMetaData(AMetaData);
			FIsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (FColumnType != TableVarColumnType.Stored).ToString()));
			FReadOnly = !((FColumnType == TableVarColumnType.Stored) || (FColumnType == TableVarColumnType.RowExists) || !IsComputed);
		}
		
		public TableVarColumn(Column AColumn, MetaData AMetaData, TableVarColumnType AColumnType) : base(AColumn.Name)
		{
			SetColumn(AColumn);
			MergeMetaData(AMetaData);
			FColumnType = AColumnType;
			FIsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (FColumnType != TableVarColumnType.Stored).ToString()));
			FReadOnly = !((FColumnType == TableVarColumnType.Stored) || (FColumnType == TableVarColumnType.RowExists) || !IsComputed);
		}
		
		public TableVarColumn(int AID, Column AColumn, MetaData AMetaData, TableVarColumnType AColumnType) : base(AID, AColumn.Name)
		{
			SetColumn(AColumn);
			MergeMetaData(AMetaData);
			FColumnType = AColumnType;
			FIsComputed = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsComputed", (FColumnType != TableVarColumnType.Stored).ToString()));
			FReadOnly = !((FColumnType == TableVarColumnType.Stored) || (FColumnType == TableVarColumnType.RowExists) || !IsComputed);
		}
		
		private bool FIsNilable;
		public bool IsNilable
		{
			get { return FIsNilable; }
			set { FIsNilable = value; }
		}
		
		private bool FIsComputed;
		public bool IsComputed
		{
			get { return FIsComputed; }
			set { FIsComputed = value; }
		}
		
		public void DetermineShouldCallProposables(ServerProcess AProcess, bool AReset)
		{
			if (AReset)
			{
				FShouldChange = false;
				FShouldDefault = false;
				FShouldValidate = false;
			}

			if (HasHandlers(AProcess, EventType.Change))
				FShouldChange = true;
				
			if (HasHandlers(AProcess, EventType.Default) || (FDefault != null))
				FShouldDefault = true;
				
			if (HasHandlers(AProcess, EventType.Validate) || HasConstraints())
				FShouldValidate = true;
				
			Schema.ScalarType LScalarType = DataType as Schema.ScalarType;
			if (LScalarType != null) 
			{
				if (LScalarType.HasHandlers(AProcess, EventType.Change))
					FShouldChange = true;
					
				if (LScalarType.HasHandlers(AProcess, EventType.Default) || (LScalarType.Default != null))
					FShouldDefault = true;
					
				if (LScalarType.HasHandlers(AProcess, EventType.Validate) || (LScalarType.Constraints.Count > 0))
					FShouldValidate = true;
			}
		}
		
		private void SetIsValidateRemotable(ScalarType ADataType)
		{	
			foreach (Constraint LConstraint in ADataType.Constraints)
			{
				FIsValidateRemotable = FIsValidateRemotable && LConstraint.IsRemotable;
				if (!FIsValidateRemotable)
					break;
			}

			if (FIsValidateRemotable)
			{
				foreach (EventHandler LHandler in ADataType.EventHandlers)
					if ((LHandler.EventType & EventType.Validate) != 0)
					{
						FIsValidateRemotable = FIsValidateRemotable && LHandler.IsRemotable;
						if (!FIsValidateRemotable)
							break;
					}

				#if USETYPEINHERITANCE
				if (FIsValidateRemotable)			
					foreach (ScalarType LParentType in ADataType.ParentTypes)
						if (FIsValidateRemotable)
							SetIsValidateRemotable(LParentType);
				#endif
			}
		}
		
		private void SetIsValidateRemotable()
		{
			FIsValidateRemotable = true;
			if (FConstraints != null)
				foreach (Constraint LConstraint in Constraints)
				{
					FIsValidateRemotable = FIsValidateRemotable && LConstraint.IsRemotable;
					if (!FIsValidateRemotable)
						break;
				}
			
			if (FIsValidateRemotable)
			{
				if (FEventHandlers != null)
					foreach (EventHandler LHandler in FEventHandlers)
						if ((LHandler.EventType & EventType.Validate) != 0)
						{
							FIsValidateRemotable = FIsValidateRemotable && LHandler.IsRemotable;
							if (!FIsValidateRemotable)
								break;
						}
					
				if (FIsValidateRemotable && (DataType is ScalarType))
					SetIsValidateRemotable((ScalarType)DataType);
			}
		}
		
		private void SetIsChangeRemotable(ScalarType ADataType)
		{
			if (FEventHandlers != null)
				foreach (EventHandler LHandler in FEventHandlers)
					if ((LHandler.EventType & EventType.Change) != 0)
					{
						FIsChangeRemotable = FIsChangeRemotable && LHandler.IsRemotable;
						if (!FIsChangeRemotable)
							break;
					}

			#if USETYPEINHERITANCE				
			if (FIsChangeRemotable)
				foreach (ScalarType LParentType in ADataType.ParentTypes)
					if (FIsChangeRemotable)
						SetIsChangeRemotable(LParentType);
			#endif
		}
		
		private void SetIsChangeRemotable()
		{
			FIsChangeRemotable = true;
			if (FEventHandlers != null)
				foreach (EventHandler LHandler in FEventHandlers)
					if ((LHandler.EventType & EventType.Change) != 0)
					{
						FIsChangeRemotable = FIsChangeRemotable && LHandler.IsRemotable;
						if (!FIsChangeRemotable)
							break;
					}
				
			if (FIsChangeRemotable && (DataType is ScalarType))
				SetIsChangeRemotable((ScalarType)DataType);
		}
		
        private void SetIsDefaultRemotable()
        {
			FIsDefaultRemotable = FIsChangeRemotable;
			
			if (FIsDefaultRemotable)
			{
				if (FEventHandlers != null)
					foreach (EventHandler LHandler in FEventHandlers)
						if ((LHandler.EventType & EventType.Default) != 0)
						{
							FIsDefaultRemotable = FIsDefaultRemotable && LHandler.IsRemotable;
							if (!FIsDefaultRemotable)
								break;
						}
			}
			
			if (FIsDefaultRemotable)
			{
				if (FDefault != null)
					FIsDefaultRemotable = FIsDefaultRemotable && FDefault.IsRemotable;
				else
				{
					ScalarType LScalarType = DataType as ScalarType;
					if (LScalarType != null)
					{
						foreach (EventHandler LHandler in LScalarType.EventHandlers)
							if ((LHandler.EventType & EventType.Default) != 0)
							{
								FIsDefaultRemotable = FIsDefaultRemotable && LHandler.IsRemotable;
								if (!FIsDefaultRemotable)
									break;
							}
						
						if (FIsDefaultRemotable && (LScalarType.Default != null))
							FIsDefaultRemotable = FIsDefaultRemotable && LScalarType.Default.IsRemotable;
					}
				}
			}
        }
        
        protected void SetColumn(Column AColumn)
        {
			FColumn = AColumn;
			if (FColumn.DataType is Schema.ScalarType)
				InheritMetaData(((Schema.ScalarType)FColumn.DataType).MetaData);

			SetIsValidateRemotable();
			SetIsDefaultRemotable();
			SetIsChangeRemotable();
        }
        
		internal void ConstraintsAdding(object ASender, Object AObject)
		{
			FIsValidateRemotable = FIsValidateRemotable && AObject.IsRemotable;
		}
		
		internal void ConstraintsRemoving(object ASender, Object AObject)
		{
			SetIsValidateRemotable();
		}
		
		internal void EventHandlersAdding(object ASender, Object AObject)
		{
			EventHandler LObject = (EventHandler)AObject;
			if ((LObject.EventType & EventType.Default) != 0)
				FIsDefaultRemotable = FIsDefaultRemotable && LObject.IsRemotable;
			if ((LObject.EventType & EventType.Validate) != 0)
				FIsValidateRemotable = FIsValidateRemotable && LObject.IsRemotable;
			if ((LObject.EventType & EventType.Change) != 0)
				FIsChangeRemotable = FIsChangeRemotable && LObject.IsRemotable;
		}
		
		internal void EventHandlersRemoving(object ASender, Object AObject)
		{
			EventHandler LObject = (EventHandler)AObject;
			if ((LObject.EventType & EventType.Default) != 0)
				SetIsDefaultRemotable();
			if ((LObject.EventType & EventType.Validate) != 0)
				SetIsValidateRemotable();
			if ((LObject.EventType & EventType.Change) != 0)
				SetIsChangeRemotable();
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.TableVarColumn"), DisplayName, TableVar.DisplayName); } }

		public override int CatalogObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

		public override int ParentObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }
		
		public override bool IsATObject { get { return FTableVar == null ? false : FTableVar.IsATObject; } }

		// TableVar
		[Reference]
		internal TableVar FTableVar;
		public TableVar TableVar 
		{ 
			get { return FTableVar; } 
			set
			{
				if (FTableVar != null)
					FTableVar.Columns.Remove(this);
				if (value != null)
					value.Columns.Add(this);
			}
		}
		
		// Column
		[Reference]
		private Column FColumn;
		public Column Column { get { return FColumn; } }

		// DataType
		public IDataType DataType { get { return FColumn.DataType; } }
        
        // ColumnType
        private TableVarColumnType FColumnType;
		public TableVarColumnType ColumnType { get { return FColumnType; } }
		
        // ReadOnly
        private bool FReadOnly;
        public bool ReadOnly
        {
			get { return FReadOnly; }
			set { FReadOnly = value; }
        }
        
        // Default
        private TableVarColumnDefault FDefault;
        public TableVarColumnDefault Default
        {
			get { return FDefault; }
			set 
			{
				if (FDefault != null)
					FDefault.FTableVarColumn = null; 
				FDefault = value; 
				if (FDefault != null)
					FDefault.FTableVarColumn = this;
				SetIsDefaultRemotable();
			}
        }
        
        public bool HasConstraints()
        {
			return (FConstraints != null) && (FConstraints.Count > 0);
        }
        
		// Constraints
		private TableVarColumnConstraints FConstraints;
		public TableVarColumnConstraints Constraints 
		{ 
			get 
			{ 
				if (FConstraints == null)
					FConstraints = new TableVarColumnConstraints(this);			
				return FConstraints; 
			} 
		}
		
		public bool HasHandlers(ServerProcess AProcess)
		{
			return (FEventHandlers != null) && (FEventHandlers.Count > 0);
		}
		
		public bool HasHandlers(ServerProcess AProcess, EventType AEventType)
		{
			return (FEventHandlers != null) && FEventHandlers.HasHandlers(AEventType);
		}
		
		// EventHandlers
		private TableVarColumnEventHandlers FEventHandlers;
		public TableVarColumnEventHandlers EventHandlers 
		{ 
			get 
			{ 
				if (FEventHandlers == null)
					FEventHandlers = new TableVarColumnEventHandlers(this);
				return FEventHandlers; 
			} 
		}

        // IsDefaultRemotable
        private bool FIsDefaultRemotable = true;
        public bool IsDefaultRemotable
        {
			get { return FIsDefaultRemotable; }
			set { FIsDefaultRemotable = value; }
        }
        
        // IsValidateRemotable
        private bool FIsValidateRemotable = true;
        public bool IsValidateRemotable
        {
			get { return FIsValidateRemotable; }
			set { FIsValidateRemotable = value; }
        }
        
        // IsChangeRemotable
        private bool FIsChangeRemotable = true;
        public bool IsChangeRemotable
        {
			get { return FIsChangeRemotable; }
			set { FIsChangeRemotable = value; }
        }
        
        // ShouldDefault
        private bool FShouldDefault = true;
        public bool ShouldDefault
        {
			get { return FShouldDefault; }
			set { FShouldDefault = value; } 
		}

        // ShouldValidate
        private bool FShouldValidate = true;
        public bool ShouldValidate
        {
			get { return FShouldValidate; }
			set { FShouldValidate = value; } 
		}

        // ShouldChange
        private bool FShouldChange = true;
        public bool ShouldChange
        {
			get { return FShouldChange; }
			set { FShouldChange = value; } 
		}
        
		// Equals
		public override bool Equals(object AObject)
		{
			TableVarColumn LColumn = AObject as TableVarColumn;
			return (LColumn != null) && (String.Compare(Name, LColumn.Name) == 0);
		}

		// GetHashCode
		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

        // ToString
        public override string ToString()
        {
			StringBuilder LBuilder = new StringBuilder(Name);
			if (DataType != null)
			{
				LBuilder.Append(Keywords.TypeSpecifier);
				LBuilder.Append(DataType.Name);
			}
			return LBuilder.ToString();
        }

		// Copying a column does not copy static tags, cloning a column does        
        public TableVarColumn Inherit()
        {
			TableVarColumn LColumn = new TableVarColumn(FColumn.Copy(), MetaData == null ? null : MetaData.Inherit(), FColumnType);
			InternalCopy(LColumn);
			return LColumn;
        }
        
        public TableVarColumn Inherit(string APrefix)
        {
			TableVarColumn LColumn = new TableVarColumn(FColumn.Copy(APrefix), MetaData == null ? null : MetaData.Inherit(), FColumnType);
			InternalCopy(LColumn);
			return LColumn;
        }

		public TableVarColumn InheritAndRename(string AName)
		{
			TableVarColumn LColumn = new TableVarColumn(FColumn.CopyAndRename(AName), MetaData == null ? null : MetaData.Inherit(), FColumnType);
			InternalCopy(LColumn);
			return LColumn;
		}
		
        public TableVarColumn Copy()
        {
			TableVarColumn LColumn = new TableVarColumn(FColumn.Copy(), MetaData == null ? null : MetaData.Copy(), FColumnType);
			InternalCopy(LColumn);
			return LColumn;
		}

		protected void InternalCopy(TableVarColumn LColumn)
        {
			LColumn.IsNilable = IsNilable;
			LColumn.ReadOnly = FReadOnly;
			LColumn.IsRemotable = IsRemotable;
			LColumn.IsDefaultRemotable = IsDefaultRemotable;
			LColumn.IsValidateRemotable = IsValidateRemotable;
			LColumn.IsChangeRemotable = IsChangeRemotable;
			LColumn.ShouldChange = ShouldChange;
			LColumn.ShouldDefault = ShouldDefault;
			LColumn.ShouldValidate = ShouldValidate;
        }
        
		public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			DataType.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);

			if ((FDefault != null) && ((AMode != EmitMode.ForRemote) || FDefault.IsRemotable))
				FDefault.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
		
			if (FConstraints != null)
				foreach (Constraint LConstraint in Constraints)
					if ((AMode != EmitMode.ForRemote) || LConstraint.IsRemotable)
						LConstraint.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
		}
		
		public override void IncludeHandlers(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if (FEventHandlers != null)
				foreach (EventHandler LHandler in FEventHandlers)
					if ((AMode != EmitMode.ForRemote) || LHandler.IsRemotable)
						LHandler.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			ColumnDefinition LColumn = new ColumnDefinition();
			LColumn.ColumnName = Name;
			LColumn.TypeSpecifier = DataType.EmitSpecifier(AMode);
			LColumn.IsNilable = IsNilable;
			if ((Default != null) && Default.IsRemotable && ((AMode != EmitMode.ForStorage) || !Default.IsPersistent))
				LColumn.Default = (DefaultDefinition)Default.EmitDefinition(AMode);
		
			if (FConstraints != null)	
				foreach (TableVarColumnConstraint LConstraint in Constraints)
					if (((AMode != EmitMode.ForRemote) || LConstraint.IsRemotable) && ((AMode != EmitMode.ForStorage) || !LConstraint.IsPersistent))
						LColumn.Constraints.Add(LConstraint.EmitDefinition(AMode));

			LColumn.MetaData = MetaData == null ? null : MetaData.Copy();
			return LColumn;
		}
		
		public override Object GetObjectFromHeader(ObjectHeader AHeader)
		{
			switch (AHeader.ObjectType)
			{
				case "TableVarColumnConstraint" :
					if (FConstraints != null)
						foreach (Constraint LConstraint in FConstraints)
							if (AHeader.ID == LConstraint.ID)
								return LConstraint;
				break;
								
				case "TableVarColumnDefault" :
					if ((FDefault != null) && (AHeader.ID == FDefault.ID))
						return FDefault;
				break;
			}
			
			return base.GetObjectFromHeader(AHeader);
		}
	}

	/// <remarks> Provides a container for TableVarColumn objects </remarks>
	public class TableVarColumnsBase : Objects
    {
		// Column lists are equal if they contain the same number of columns and all columns in the left
		// list are also in the right list
        public override bool Equals(object AObject)
        {
			TableVarColumnsBase LColumns = AObject as TableVarColumnsBase;
			if ((LColumns != null) && (Count == LColumns.Count))
			{
				foreach (TableVarColumn LColumn in this)
					if (!LColumns.Contains(LColumn))
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
				string[] LResult = new string[Count];
				for (int LIndex = 0; LIndex < Count; LIndex++)
					LResult[LIndex] = this[LIndex].Name;
				return LResult;
			}
        }

		// Column lists are equivalent if they contain the same number of columns and the columns are equal, left to right
		// This is an internal notion for use in physical contexts only
        public bool Equivalent(TableVarColumnsBase AColumns)
        {
			if (Count == AColumns.Count)
			{
				for (int LIndex = 0; LIndex < Count; LIndex++)
					if (!this[LIndex].Equals(AColumns[LIndex]))
						return false;
				return true;
			}
			return false;
        }

        public override int GetHashCode()
        {
			int LHashCode = 0;
			for (int LIndex = 0; LIndex < Count; LIndex++)
				LHashCode ^= this[LIndex].GetHashCode();
			return LHashCode;
        }

        public bool Compatible(object AObject)
        {
			return Is(AObject) || ((AObject is TableVarColumnsBase) && ((TableVarColumnsBase)AObject).Is(this));
        }

        public bool Is(object AObject)
        {
			// A column list is another column list if they both have the same number of columns
			// and the is of the datatypes for all columns evaluates to true by name
			TableVarColumnsBase LColumns = AObject as TableVarColumnsBase;
			if ((LColumns != null) && (Count == LColumns.Count))
			{
				int LColumnIndex;
				for (int LIndex = 0; LIndex < Count; LIndex++)
				{
					LColumnIndex = LColumns.IndexOfName(this[LIndex].Name);
					if (LColumnIndex >= 0)
					{
						if (!this[LIndex].DataType.Is(LColumns[LColumnIndex].DataType))
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

		public bool IsSubsetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (TableVarColumn LColumn in this)
				if (!AColumns.ContainsName(LColumn.Name))
					return false;
			return true;
		}
		
		public bool IsProperSubsetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(AColumns) && (Count < AColumns.Count);
		}

		public bool IsSupersetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in AColumnNames is in this set of columns
			foreach (TableVarColumn LColumn in AColumns)
				if (!ContainsName(LColumn.Name))
					return false;
			return true;
		}
		
		public bool IsProperSupersetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in AColumnNames is in this set of columns and this set of columns is strictly larger
			return IsSupersetOf(AColumns) && (Count > AColumns.Count);
		}
		
		public Key Intersect(TableVarColumnsBase AColumns)
		{
			Key LKey = new Key();
			foreach (TableVarColumn LColumn in AColumns)
				if (ContainsName(LColumn.Name))
					LKey.Columns.Add(LColumn);
					
			return LKey;
		}
		
		public Key Difference(TableVarColumnsBase AColumns)
		{
			Key LKey = new Key();
			foreach (TableVarColumn LColumn in this)
				if (!AColumns.ContainsName(LColumn.Name))
					LKey.Columns.Add(LColumn);
					
			return LKey;
		}
		
		public Key Union(TableVarColumnsBase AColumns)
		{
			Key LKey = new Key();
			foreach (TableVarColumn LColumn in this)
				LKey.Columns.Add(LColumn);
				
			foreach (TableVarColumn LColumn in AColumns)
				if (!LKey.Columns.ContainsName(LColumn.Name))
					LKey.Columns.Add(LColumn);
			
			return LKey;
		}
		
        public new TableVarColumn this[int AIndex]
        {
            get { return (TableVarColumn)(base[AIndex]); }
            set { base[AIndex] = value; }
        }

        public new TableVarColumn this[string AColumnName]
        {
			get { return (TableVarColumn)base[AColumnName]; }
            set { base[AColumnName] = value; }
        }
        
        public TableVarColumn this[TableVarColumn AColumn]
        {
			get { return this[IndexOfName(AColumn.Name)]; }
			set { this[IndexOfName(AColumn.Name)] = value; }
        }

		#if USEOBJECTVALIDATE
        protected override void Validate(Object AObject)
        {
            if (!(AObject is TableVarColumn))
                throw new SchemaException(SchemaException.Codes.InvalidContainer, "TableVarColumn");
            base.Validate(AObject);
        }
        #endif

        // ToString
        public override string ToString()
        {
			StringBuilder LString = new StringBuilder();
			foreach (TableVarColumn LColumn in this)
			{
				if (LString.Length != 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(LColumn.ToString());
			}
			LString.Insert(0, Keywords.BeginList);
			LString.Append(Keywords.EndList);
			return LString.ToString();
        }
    }
    
    public class TableVarColumns : TableVarColumnsBase
    {
		public TableVarColumns(TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
		}
		
		[Reference]
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }

		protected override void Validate(Object AObject)
		{
			base.Validate(AObject);
			FTableVar.ValidateChildObjectName(AObject.Name);
		}
		
		protected override void Adding(Object AItem, int AIndex)
		{
			base.Adding(AItem, AIndex);
			((TableVarColumn)AItem).FTableVar = FTableVar;
		}
		
		protected override void Removing(Object AItem, int AIndex)
		{
			((TableVarColumn)AItem).FTableVar = null;
			base.Removing(AItem, AIndex);
		}
    }
    
    public class KeyColumns : TableVarColumnsBase 
    {
		//public KeyColumns() : base() { }
		public KeyColumns(Key AKey) : base()
		{
			FKey = AKey;
		}
		
		private Key FKey;
		public Key Key { get { return FKey; } }

		protected override void Adding(Object AObject, int AIndex)
		{
			base.Adding(AObject, AIndex);
			if (FKey != null)
				FKey.UpdateKeyName();
		}

		protected override void Removing(Object AObject, int AIndex)
		{
			base.Removing(AObject, AIndex);
			if (FKey != null)
				FKey.UpdateKeyName();
		}
    }
    
	public class OrderColumn : System.Object, ICloneable
    {
		public OrderColumn() : base(){}
		public OrderColumn(TableVarColumn AColumn, bool AAscending) : base()
		{
			Column = AColumn;
			FAscending = AAscending;
		}
		
		public OrderColumn(TableVarColumn AColumn, bool AAscending, bool AIncludeNils) : base()
		{
			Column = AColumn;
			FAscending = AAscending;
			FIncludeNils = AIncludeNils;
		}
		
		// Compare expression for this column in the order
		private Sort FSort;
		public Sort Sort
		{
			get { return FSort; }
			set { FSort = value; }
		}
		
		// IsDefaultSort
		private bool FIsDefaultSort = true;
		public bool IsDefaultSort
		{
			get { return FIsDefaultSort; }
			set { FIsDefaultSort = value; }
		}
		
		// Column
		[Reference]
		protected TableVarColumn FColumn;
		public TableVarColumn Column
		{
			get { return FColumn; }
			set { FColumn = value; }
		}

		// Ascending
		protected bool FAscending = true;
		public bool Ascending
		{
			get { return FAscending; }
			set { FAscending = value; }
		}
		
		// IncludeNils
		protected bool FIncludeNils;
		public bool IncludeNils
		{
			get { return FIncludeNils; }
			set { FIncludeNils = value; }
		}

		// ICloneable
		public virtual object Clone()
		{
			return new OrderColumn(FColumn, FAscending, FIncludeNils);
		}
		
		public object Clone(bool AReverse)
		{
			return new OrderColumn(FColumn, AReverse ? !FAscending : FAscending, FIncludeNils);
		}
		
		public override string ToString()
		{
			StringBuilder LString = new StringBuilder(FColumn.Name);
			LString.Append(" ");
			if (!IsDefaultSort)
				LString.AppendFormat("{0} {1} ", Keywords.Sort, FSort.CompareNode.EmitStatementAsString());
			LString.Append(FAscending ? Keywords.Asc : Keywords.Desc);
			if (FIncludeNils)
				LString.AppendFormat(" {0} {1}", Keywords.Include, Keywords.Nil);
			return LString.ToString();
		}
		
		public Statement EmitStatement(EmitMode AMode)
		{
			OrderColumnDefinition LDefinition = new OrderColumnDefinition(Schema.Object.EnsureRooted(Column.Name), Ascending, IncludeNils);
			if (!IsDefaultSort)
				LDefinition.Sort = FSort.EmitDefinition(AMode);
			return LDefinition;
		}

		public override int GetHashCode()
		{
			int LResult = FColumn.Name.GetHashCode();
			if (!IsDefaultSort)
				LResult ^= FSort.CompareNode.EmitStatementAsString().GetHashCode();
			if (FAscending)
				LResult = (LResult << 1) | (LResult >> 31);
			if (FIncludeNils)
				LResult = (LResult << 2) | (LResult >> 30);
			return LResult;
		}

		public bool Equivalent(OrderColumn AOrderColumn)
		{
			return 
				(AOrderColumn != null) 
					&& (AOrderColumn.Column.Name == Column.Name) 
					&& (AOrderColumn.Ascending == FAscending) 
					//&& (AOrderColumn.IncludeNils == FIncludeNils) // Should be here, but can't be yet (breaks existing code, and doesn't actually do anything in the physical layer yet, so doesn't matter)
					&& ((AOrderColumn.Sort == null) == (FSort == null))
					&& ((AOrderColumn.Sort == null) || AOrderColumn.Sort.Equivalent(FSort));
		}
    }

	public class OrderColumns : System.Object
    {
		private List<OrderColumn> FOrderColumns = new List<OrderColumn>();
		
		public void Add(OrderColumn AOrderColumn)
		{
			if (Contains(AOrderColumn))
				throw new SchemaException(SchemaException.Codes.DuplicateOrderColumnDefinition, AOrderColumn.ToString());
			FOrderColumns.Add(AOrderColumn);
			FVersion++;
		}
		
		public int IndexOf(OrderColumn AOrderColumn)
		{
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if (FOrderColumns[LIndex].Equivalent(AOrderColumn))
					return LIndex;
			return -1;
		}
		
		public bool Contains(OrderColumn AOrderColumn)
		{
			return IndexOf(AOrderColumn) >= 0;
		}
		
		public int Count { get { return FOrderColumns.Count; } }
		
		public OrderColumn this[int AIndex] { get { return FOrderColumns[AIndex]; } }

		/// <summary>Returns the first column in the order referencing the given name, without name resolution</summary>		
		public OrderColumn this[string AColumnName]
		{
			get
			{
				int LIndex = IndexOf(AColumnName);
				if (LIndex < 0)
					throw new SchemaException(SchemaException.Codes.ObjectNotFound, AColumnName);
				return FOrderColumns[LIndex];
			}
		}
		
		/// <summary>Returns the index of the first reference in the order to the given column name, without name resolution</summary>
		public int IndexOf(string AColumnName)
		{
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if (FOrderColumns[LIndex].Column.Name == AColumnName)
					return LIndex;
			return -1;
		}
		
		/// <summary>Returns true if the order contains any reference to the given column name, without name resolution</summary>
		public bool Contains(string AColumnName)
		{
			return IndexOf(AColumnName) >= 0;
		}
		
		/// <summary>Returns the index of the first reference in the order to the given column name, without name resolution, and using a sort equivalent to the given sort.</summary>
		public int IndexOf(string AColumnName, Schema.Sort ASort)
		{
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if ((FOrderColumns[LIndex].Column.Name == AColumnName) && FOrderColumns[LIndex].Sort.Equivalent(ASort))
					return LIndex;
			return -1;
		}
		
		/// <summary>Returns true if the order contains any reference to the given column name, without name resolution, and using a sort equivalent to the given sort.</summary>
		public bool Contains(string AColumnName, Schema.Sort ASort)
		{
			return IndexOf(AColumnName, ASort) >= 0;
		}
		
		private int FVersion;
		/// <summary>Returns the version of the order columns list. Beginning at zero, this number is incremented each time a column is added or removed from the order columns.</summary>
		/// <remarks>This number is used to coordinate changes to the column list with properties of the order that are dependent on the set of columns in the order, such as Name.</remarks>
		public int Version { get { return FVersion; } }
		
		public List<OrderColumn>.Enumerator GetEnumerator()
		{
			return FOrderColumns.GetEnumerator();
		}
		
		public override string ToString()
		{
			StringBuilder LString = new StringBuilder();
			for (int LIndex = 0; LIndex < Count; LIndex++)
			{
				if (LString.Length != 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(FOrderColumns[LIndex].ToString());
			}
			LString.Insert(0, Keywords.BeginList);
			LString.Append(Keywords.EndList);
			return LString.ToString();
		}

		public bool IsSubsetOf(Columns AColumns)
		{
			// true if every column in this set of columns is in AColumns
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if (!AColumns.ContainsName(FOrderColumns[LIndex].Column.Name))
					return false;
			return true;
		}
		
		public bool IsSubsetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in this set of columns is in AColumns
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if (!AColumns.ContainsName(FOrderColumns[LIndex].Column.Name))
					return false;
			return true;
		}
		
		public bool IsSubsetOf(OrderColumns AColumns)
		{
			// true if every column in this set of columns is in AColumns
			for (int LIndex = 0; LIndex < FOrderColumns.Count; LIndex++)
				if (!AColumns.Contains(FOrderColumns[LIndex].Column.Name))
					return false;
			return true;
		}

		public bool IsProperSubsetOf(Columns AColumns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(AColumns) && (Count < AColumns.Count);
		}
		
		public bool IsProperSubsetOf(TableVarColumnsBase AColumns)
		{
			return IsSubsetOf(AColumns) && (Count < AColumns.Count);
		}

		public bool IsProperSubsetOf(OrderColumns AColumns)
		{
			return IsSubsetOf(AColumns) && (Count < AColumns.Count);
		}
    }

	public class Order : Object
    {		
		public Order() : base(String.Empty) {}
		public Order(int AID) : base(AID, String.Empty) {}

		public Order(MetaData AMetaData) : base(String.Empty)
		{
			MetaData = AMetaData;
		}
		
		public Order(int AID, MetaData AMetaData) : base(AID, String.Empty)
		{
			MetaData = AMetaData;
		}
		
		public Order(Order AOrder) : base(String.Empty)
		{
			OrderColumn LNewOrderColumn;
			OrderColumn LColumn;
			for (int LIndex = 0; LIndex < AOrder.Columns.Count; LIndex++)
			{
				LColumn = AOrder.Columns[LIndex];
				LNewOrderColumn = new OrderColumn(LColumn.Column, LColumn.Ascending, LColumn.IncludeNils);
				LNewOrderColumn.Sort = LColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LColumn.IsDefaultSort;
				FColumns.Add(LNewOrderColumn);
			}
		}
		
		public Order(Order AOrder, bool AReverse) : base(String.Empty)
		{
			OrderColumn LNewOrderColumn;
			OrderColumn LColumn;
			for (int LIndex = 0; LIndex < AOrder.Columns.Count; LIndex++)
			{
				LColumn = AOrder.Columns[LIndex];
				LNewOrderColumn = new OrderColumn(LColumn.Column, AReverse ? !LColumn.Ascending : LColumn.Ascending, LColumn.IncludeNils);
				LNewOrderColumn.Sort = LColumn.Sort;
				LNewOrderColumn.IsDefaultSort = LColumn.IsDefaultSort;
				FColumns.Add(LNewOrderColumn);
			}
		}
		
		public Order(Key AKey) : base(String.Empty)
		{
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
				FColumns.Add(new OrderColumn(AKey.Columns[LIndex], true, true));
		}
		
		public Order(Key AKey, Plan APlan) : base(String.Empty)
		{
			OrderColumn LOrderColumn;
			TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				LColumn = AKey.Columns[LIndex];
				LOrderColumn = new OrderColumn(LColumn, true, true);
				if (LColumn.DataType is Schema.ScalarType)
					LOrderColumn.Sort = ((Schema.ScalarType)LColumn.DataType).GetUniqueSort(APlan);
				else
					LOrderColumn.Sort = Compiler.CompileSortDefinition(APlan, LColumn.DataType);
				LOrderColumn.IsDefaultSort = true;
				if (LOrderColumn.Sort.HasDependencies())
					APlan.AttachDependencies(LOrderColumn.Sort.Dependencies);
				FColumns.Add(LOrderColumn);
			}
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Order"), DisplayName); } }

		public override int CatalogObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

		public override int ParentObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }
		
		/// <summary>Returns the full name of the order, a guaranteed parsable D4 order definition.</summary>
		/// <remarks>The Name property, in contrast, returns the full name limited to the max object name length of 200 characters.</remarks>
		private string GetFullName()
		{
			StringBuilder LName = new StringBuilder();
			LName.AppendFormat("{0} {1} ", Keywords.Order, Keywords.BeginList);
			for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
			{
				if (LIndex > 0)
					LName.AppendFormat("{0} ", Keywords.ListSeparator);
				LName.Append(FColumns[LIndex].ToString());
			}
			LName.AppendFormat("{0}{1}", FColumns.Count > 0 ? " " : "", Keywords.EndList);
			return LName.ToString();
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

		private int FColumnsVersion = -1; // Initialize to -1 to ensure name is set the first time
		private void EnsureOrderName()
		{
			if (FColumnsVersion < FColumns.Version)
			{
				Name = GetFullName();
				FColumnsVersion = FColumns.Version;
			}
		}

		// TableVar
		[Reference]
		internal TableVar FTableVar;
		public TableVar TableVar 
		{ 
			get { return FTableVar; } 
			set
			{
				if (FTableVar != null)
					FTableVar.Orders.Remove(this);
				if (value != null)
					value.Orders.Add(this);
			}
		}
		
		// Columns
		private OrderColumns FColumns = new OrderColumns();
		public OrderColumns Columns { get { return FColumns; } }
		
		// IsAscending
		/// <summary>Returns true if all the columns in this order are in ascending order, false if any are descending.</summary>
		public bool IsAscending 
		{ 
			get 
			{
				for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
					if (!FColumns[LIndex].Ascending)
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
				for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
					if (FColumns[LIndex].Ascending)
						return false;
				return true;
			}
		}

		// IsInherited
		private bool FIsInherited;
		public bool IsInherited
		{
			get { return FIsInherited; }
			set { FIsInherited = value; }
		}

		/// <summary>Returns true if AOrder can be used to satisfy an ordering by this order.</summary>		
		public bool Equivalent(Order AOrder)
		{
			if (Columns.Count > AOrder.Columns.Count)
				return false;
				
			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
				if (!Columns[LIndex].Equivalent(AOrder.Columns[LIndex]))
					return false;
			
			return true;		
		}

        public override bool Equals(object AObject)
        {
            // An order is equal to another order if it contains the same columns (by order and ascending)
            Order LOrder = AObject as Order;
            if (LOrder != null)
				return (Columns.Count == LOrder.Columns.Count) && Equivalent(LOrder);

            return base.Equals(AObject);
        }

		// returns true if the order includes the key as a subset, including the use of the unique sort algorithm for the type of each column
		public bool Includes(Plan APlan, Key AKey)
		{
			Schema.TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < AKey.Columns.Count; LIndex++)
			{
				LColumn = AKey.Columns[LIndex];
				if (!Columns.Contains(LColumn.Name, Compiler.GetUniqueSort(APlan, LColumn.DataType)))
					return false;
			}

			return true;
		}
		
		public bool Includes(Plan APlan, Order AOrder)
		{
			Schema.OrderColumn LColumn;
			for (int LIndex = 0; LIndex < AOrder.Columns.Count; LIndex++)
			{
				LColumn = AOrder.Columns[LIndex];
				if (!Columns.Contains(LColumn.Column.Name, LColumn.Sort))
					return false;
			}
			
			return true;
		}
		
        // GetHashCode
        public override int GetHashCode()
        {
			int LHashCode = 0;
			for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
				LHashCode ^= FColumns[LIndex].GetHashCode();
			return LHashCode;
        }

        public override string ToString()
        {
			return Name;
        }
        
        public override Statement EmitStatement(EmitMode AMode)
        {
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			OrderDefinition LOrder = new OrderDefinition();
			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
				LOrder.Columns.Add(Columns[LIndex].EmitStatement(AMode));
			LOrder.MetaData = MetaData == null ? null : MetaData.Copy();
			return LOrder;
        }

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropOrderDefinition LOrder = new DropOrderDefinition();
			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
				LOrder.Columns.Add(Columns[LIndex].EmitStatement(AMode));
			return LOrder;
		}
        
        public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			
			OrderColumn LColumn;
			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
			{
				LColumn = Columns[LIndex];
				if (LColumn.Sort != null)
					LColumn.Sort.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
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
		public Orders(TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
		}
		
		[Reference]
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }
		
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
        protected override void Adding(Order AValue, int AIndex)
		{
 			 //base.Adding(AValue, AIndex);
			 AValue.FTableVar = FTableVar;
		}
		
		protected override void Removing(Order AValue, int AIndex)
		{
 			 AValue.FTableVar = null;
 			 //base.Removing(AValue, AIndex);
		}
        #endif
        
        // ToString
        public override string ToString()
        {
			StringBuilder LString = new StringBuilder();
			foreach (Order LOrder in this)
			{
				if (LString.Length != 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(LOrder.ToString());
			}
			return LString.ToString();
        }
    }
    
	public class Key : Object
    {
		public Key() : base(String.Empty)
		{
			InternalInitialize();
			UpdateKeyName();
		}
		
		public Key(int AID) : base(AID, String.Empty)
		{
			InternalInitialize();
			UpdateKeyName();
		}
		
        public Key(MetaData AMetaData) : base(String.Empty)
        {
			MetaData = AMetaData;
			InternalInitialize();
			UpdateKeyName();
        }
        
        public Key(int AID, MetaData AMetaData) : base(AID, String.Empty)
        {
			MetaData = AMetaData;
			InternalInitialize();
			UpdateKeyName();
        }
        
        public Key(TableVarColumn[] AColumns) : base(String.Empty)
        {
			InternalInitialize();
			if (AColumns.Length > 0)
				foreach (TableVarColumn LColumn in AColumns)
					FColumns.Add(LColumn);
			else
				UpdateKeyName();
        }
        
        public Key(MetaData AMetaData, TableVarColumn[] AColumns) : base(String.Empty)
        {
			MetaData = AMetaData;
			InternalInitialize();
			if (AColumns.Length > 0)
				foreach (TableVarColumn LColumn in AColumns)
					FColumns.Add(LColumn);
			else
				UpdateKeyName();
        }

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.Key"), DisplayName); } }

		public override int CatalogObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

		public override int ParentObjectID { get { return FTableVar == null ? -1 : FTableVar.ID; } }

        private void InternalInitialize()
        {
			FColumns = new KeyColumns(this);
        }
        
        private bool FNameCurrent = false;

        internal void UpdateKeyName()
        {
			FNameCurrent = false;
        }
        
        private void EnsureKeyName()
        {
			if (!FNameCurrent)
			{
				StringBuilder LName = new StringBuilder();
				LName.AppendFormat("{0} {1} ", Keywords.Key, Keywords.BeginList);
				for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
				{
					if (LIndex > 0)
						LName.AppendFormat("{0} ", Keywords.ListSeparator);
					LName.Append(FColumns[LIndex].Name);
				}
				LName.AppendFormat("{0}{1}", FColumns.Count > 0 ? " " : "", Keywords.EndList);
				Name = LName.ToString();
				FNameCurrent = true;
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
        
        public override bool Equals(object AObject)
        {
			Key LKey = AObject as Key;
			return (LKey != null) && FColumns.Equals(LKey.Columns) && (FIsSparse == LKey.IsSparse);
        }
        
        // GetHashCode
        public override int GetHashCode()
        {
			int LHashCode = 0;
			for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
				LHashCode ^= FColumns[LIndex].Name.GetHashCode();
			return LHashCode;
        }
        
        // Equivalent
        /// <summary>Returns true if this key has the same columns as the given key.</summary>
        /// <remarks>
        /// This is different than equality in that equality also considers sparseness of the key.
        /// </remarks>
        public bool Equivalent(Key AKey)
        {
			return FColumns.Equals(AKey.Columns);
        }
        
		// TableVar
		[Reference]
		internal TableVar FTableVar;
		public TableVar TableVar 
		{ 
			get { return FTableVar; } 
			set
			{
				if (FTableVar != null)
					FTableVar.Keys.Remove(this);
				if (value != null)
					value.Keys.Add(this);
			}
		}
		
        // Columns
        private KeyColumns FColumns;
		public KeyColumns Columns { get { return FColumns; } }
        
        // IsInherited
        private bool FIsInherited;
        public bool IsInherited
        {
			get { return FIsInherited; }
			set { FIsInherited = value; }
        }
        
        // IsSparse
        private bool FIsSparse;
        /// <summary>Indicates whether or not the key will consider rows with nils for the purpose of duplicate detection</summary>
        /// <remarks>
        /// Sparse keys do not consider rows with nils, allowing multiple rows with nils for the columns of the key.
        /// Set by the DAE.IsSparse tag
        /// Sparse keys cannot be used as clustering keys.
        /// </remarks>
        public bool IsSparse
        {
			get { return FIsSparse; }
			set { FIsSparse = value; }
		}
		
		// IsNilable
		public bool IsNilable
		{
			get
			{
				for (int LIndex = 0; LIndex < FColumns.Count; LIndex++)
					if (FColumns[LIndex].IsNilable)
						return true;
				return false;
			}
		}

		// Enforced
		private bool FEnforced = true;
		/// <summary>Indicates whether or not the constraint is enforced.</summary>
		/// <remarks>Set by the DAE.Enforced tag when the constraint is created.</remarks>
		public bool Enforced
		{
			get { return FEnforced; }
			set { FEnforced = value; }
		}
		
		private TransitionConstraint FConstraint;
		public TransitionConstraint Constraint
		{
			get { return FConstraint; }
			set { FConstraint = value; }
		}
		
        // ToString
        public override string ToString()
        {
			return Name;
        }
        
        public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			
			TableVarColumn LColumn;
			for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
			{
				LColumn = Columns[LIndex];
				Schema.Sort LSort = Compiler.GetUniqueSort(AProcess.Plan, LColumn.DataType);
				if (LSort != null)
					LSort.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			}
        }

        public override Statement EmitStatement(EmitMode AMode)
        {
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			KeyDefinition LKey = new KeyDefinition();
			foreach (TableVarColumn LColumn in Columns)
				LKey.Columns.Add(new KeyColumnDefinition(Schema.Object.EnsureRooted(LColumn.Name)));
			LKey.MetaData = MetaData == null ? null : MetaData.Copy();
			return LKey;
        }

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropKeyDefinition LDefinition = new DropKeyDefinition();
			foreach (TableVarColumn LColumn in Columns)
				LDefinition.Columns.Add(new KeyColumnDefinition(Schema.Object.EnsureRooted(LColumn.Name)));
			return LDefinition;
		}
    }
    
	public class JoinKey : Key
    {
		public JoinKey() : base() {}
		
		private bool FIsUnique;
		public bool IsUnique
		{
			get { return FIsUnique; }
			set { FIsUnique = value; }
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
		public Keys(TableVar ATableVar) : base()
		{
			FTableVar = ATableVar;
		}
	#endif
		
		[Reference]
		private TableVar FTableVar;
		public TableVar TableVar { get { return FTableVar; } }
		
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
        protected override void Adding(Key AValue, int AIndex)
		{
 			 //base.Adding(AValue, AIndex);
			 AValue.FTableVar = FTableVar;
		}
		
		protected override void Removing(Key AValue, int AIndex)
		{
 			 AValue.FTableVar = null;
 			 //base.Removing(AValue, AIndex);
		}
		#endif

        public bool IsKeyColumnName(string AColumnName)
        {
			foreach (Schema.Key LKey in this)
				if (LKey.Columns.ContainsName(AColumnName))
					return true;
			return false;
        }
        
        public Key MinimumKey(bool AAllowSparse, bool AAllowNilable)
        {
			Key LMinimumKey = null;
			for (int LIndex = Count - 1; LIndex >= 0; LIndex--)
			{
				if ((AAllowNilable || !this[LIndex].IsNilable) && (AAllowSparse || !this[LIndex].IsSparse))
					if (LMinimumKey == null)
						LMinimumKey = this[LIndex];
					else
						if (this[LIndex].Columns.Count < LMinimumKey.Columns.Count)
							LMinimumKey = this[LIndex];
			}

			if (LMinimumKey == null)
				if (AAllowSparse && AAllowNilable)
					throw new SchemaException(SchemaException.Codes.NoKeysAvailable, FTableVar == null ? "<unknown>" : FTableVar.DisplayName);
				else if (AAllowNilable)
					throw new SchemaException(SchemaException.Codes.NoNonSparseKeysAvailable, FTableVar == null ? "<unknown>" : FTableVar.DisplayName);
				else
					throw new SchemaException(SchemaException.Codes.NoNonNilableKeysAvailable, FTableVar == null ? "<unknown>" : FTableVar.DisplayName);

			return LMinimumKey;
        }
        
        public Key MinimumKey(bool AAllowSparse)
        {
			return MinimumKey(AAllowSparse, true);
        }
        
        public Key MinimumSubsetKey(TableVarColumnsBase AColumns)
        {
			Key LMinimumKey = null;
			for (int LIndex = Count - 1; LIndex >= 0; LIndex--)
			{
				if (this[LIndex].Columns.IsSubsetOf(AColumns))
					if (LMinimumKey == null)
						LMinimumKey = this[LIndex];
					else
						if (this[LIndex].Columns.Count < LMinimumKey.Columns.Count)
							LMinimumKey = this[LIndex];
			}

			return LMinimumKey;
        }
        
        // ToString
        public override string ToString()
        {
			StringBuilder LString = new StringBuilder();
			foreach (Key LKey in this)
			{
				if (LString.Length != 0)
				{
					LString.Append(Keywords.ListSeparator);
					LString.Append(" ");
				}
				LString.Append(LKey.ToString());
			}
			return LString.ToString();
        }
    }
    
    public enum TableVarScope { Database, Session, Process }
    
    /// <summary> Base class for data table definitions </summary>
	public abstract class TableVar : CatalogObject
    {
		public TableVar(string AName) : base(AName)
		{
			IsRemotable = false;
			InternalInitialize();
		}
		
		public TableVar(int AID, string AName) : base(AID, AName)
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
		protected ITableType FDataType;
		public ITableType DataType
		{
			get { return FDataType; }
			set { FDataType = value; }
		}
		
		private void InternalInitialize()
		{
			FColumns = new TableVarColumns(this);
			FKeys = new Keys(this);
			FOrders = new Orders(this);
			FConstraints = new TableVarConstraints(this);
			FRowConstraints = new RowConstraints();
		}
		
		public void ResetHasDeferredConstraintsComputed()
		{
			FHasDeferredConstraintsComputed = false;
		}
		
		public void ValidateChildObjectName(string AName)
		{
			if 
				(
					(FColumns.IndexOfName(AName) >= 0) ||
					(FConstraints.IndexOfName(AName) >= 0)
				)
			{
				throw new SchemaException(SchemaException.Codes.DuplicateChildObjectName, AName);
			}
		}
		
		// Columns
		private TableVarColumns FColumns;
		public TableVarColumns Columns { get { return FColumns; } }
		
		// Keys
		private Keys FKeys;
		public Keys Keys { get { return FKeys; } }
		
		public Key KeyFromKeyColumnDefinitions(KeyColumnDefinitions AKeyColumns)
		{
			Key LKey = new Schema.Key();
			foreach (KeyColumnDefinition LColumn in AKeyColumns)
				LKey.Columns.Add(Columns[LColumn.ColumnName]);
			return LKey;
		}
		
		/// <summary>Returns the index of the key with the same columns as the given key.</summary>
		/// <remarks>
		/// This is not the same as using Keys.IndexOf, because that method is based on key equality,
		/// which includes the sparseness of the key. This method is used to find a key by columns only,
		/// and will return the first key with the same columns.
		/// </remarks>
		public int IndexOfKey(Key AKey)
		{
			for (int LIndex = 0; LIndex < FKeys.Count; LIndex++)
				if (Keys[LIndex].Equivalent(AKey))
					return LIndex;
			return -1;
		}
		
		public Key FindKey(KeyColumnDefinitions AKeyColumns)
		{
			Key LKey = KeyFromKeyColumnDefinitions(AKeyColumns);
			
			int LIndex = IndexOfKey(LKey);
			if (LIndex >= 0)
				return Keys[LIndex];
			
			throw new SchemaException(SchemaException.Codes.ObjectNotFound, LKey.Name);
		}
		
		public Key FindKey(KeyDefinitionBase AKeyDefinition)
		{
			return FindKey(AKeyDefinition.Columns);
		}
		
		public Schema.Key FindClusteringKey()
		{
			Schema.Key LMinimumKey = null;
			foreach (Schema.Key LKey in Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(LKey.MetaData, "DAE.IsClustered", "false")))
					return LKey;
				
				if (!LKey.IsSparse)
					if (LMinimumKey == null)
						LMinimumKey = LKey;
					else
						if (LMinimumKey.Columns.Count > LKey.Columns.Count)
							LMinimumKey = LKey;
			}
					
			if (LMinimumKey != null)
				return LMinimumKey;

			throw new SchemaException(SchemaException.Codes.KeyRequired, DisplayName);
		}
		
		public Schema.Order FindClusteringOrder(Plan APlan)
		{
			Schema.Key LMinimumKey = null;
			foreach (Schema.Key LKey in Keys)
			{
				if (Convert.ToBoolean(MetaData.GetTag(LKey.MetaData, "DAE.IsClustered", "false")))
					return new Schema.Order(LKey, APlan);
					
				if (!LKey.IsSparse)
					if (LMinimumKey == null)
						LMinimumKey = LKey;
					else
						if (LMinimumKey.Columns.Count > LKey.Columns.Count)
							LMinimumKey = LKey;
			}

			foreach (Schema.Order LOrder in Orders)
				if (Convert.ToBoolean(MetaData.GetTag(LOrder.MetaData, "DAE.IsClustered", "false")))
					return LOrder;
					
			if (LMinimumKey != null)
				return new Schema.Order(LMinimumKey, APlan);
					
			if (Orders.Count > 0)
				return Orders[0];
				
			throw new SchemaException(SchemaException.Codes.KeyRequired, DisplayName);
		}
		
		// EnsureTableVarColumns()
		public void EnsureTableVarColumns()
		{
			foreach (Schema.Column LColumn in DataType.Columns)
				if (!FColumns.ContainsName(LColumn.Name))
					FColumns.Add(new Schema.TableVarColumn(LColumn));
		}
		
		public void EnsureKey(Plan APlan)
		{
			if (FKeys.Count == 0)
			{
				Schema.Key LKey = new Schema.Key();
				foreach (Schema.TableVarColumn LColumn in FColumns)
					if (Compiler.SupportsComparison(APlan, LColumn.DataType))
						LKey.Columns.Add(LColumn);
				FKeys.Add(LKey);
			}
		}

		// Orders
		private Orders FOrders;
		public Orders Orders { get { return FOrders; } }
		
		public Order OrderFromOrderDefinition(Plan APlan, OrderDefinitionBase AOrderDefinition)
		{
			return Compiler.CompileOrderDefinition(APlan, this, AOrderDefinition, false);
		}
		
		public int IndexOfOrder(Order AOrder)
		{
			for (int LIndex = 0; LIndex < Orders.Count; LIndex++)
				if (Orders[LIndex].Equals(AOrder))
					return LIndex;
					
			return -1;
		}
		
		public Order FindOrder(Plan APlan, OrderDefinitionBase AOrderDefinition)
		{
			Order LOrder = OrderFromOrderDefinition(APlan, AOrderDefinition);
			
			int LIndex = IndexOfOrder(LOrder);
			if (LIndex >= 0)
				return Orders[LIndex];

			throw new SchemaException(SchemaException.Codes.ObjectNotFound, LOrder.Name);
		}
		
		// Constraints
		private TableVarConstraints FConstraints;
		public TableVarConstraints Constraints { get { return FConstraints; } }
		
		private RowConstraints FRowConstraints;
		public RowConstraints RowConstraints { get { return FRowConstraints; } }
		
		public bool IsConstant;
		public bool IsModified;
		
		/// <summary>Indicates whether this table variable is instanced at the database, session, or process level</summary>
		public TableVarScope Scope;

		// List of references in which this table variable is involved as a source
		[Reference]
		private References FSourceReferences = new References();
		public References SourceReferences { get { return FSourceReferences; } }
		
		// List of references in which this table variable is involved as a target
		[Reference]
		private References FTargetReferences = new References();
		public References TargetReferences { get { return FTargetReferences; } }

		// List of references derived by type inference, not actually present in the catalog.
		[Reference]
		private References FDerivedReferences = new References();
		public References DerivedReferences { get { return FDerivedReferences; } }
		
		public bool HasHandlers(ServerProcess AProcess)
		{
			return (FEventHandlers != null) && (FEventHandlers.Count > 0);
		}
		
		public bool HasHandlers(ServerProcess AProcess, EventType AEventType)
		{
			return (FEventHandlers != null) && FEventHandlers.HasHandlers(AEventType);
		}
		
		// List of EventHandlers associated with this table variable
		private TableVarEventHandlers FEventHandlers;
		public TableVarEventHandlers EventHandlers 
		{ 
			get 
			{ 
				if (FEventHandlers == null)
					FEventHandlers = new TableVarEventHandlers(this);
				return FEventHandlers; 
			} 
		}
		
        // ShouldDefault
        private bool FShouldDefault = true;
        public bool ShouldDefault
        {
			get { return FShouldDefault; }
			set { FShouldDefault = value; } 
		}

        // ShouldValidate
        private bool FShouldValidate = true;
        public bool ShouldValidate
        {
			get { return FShouldValidate; }
			set { FShouldValidate = value; } 
		}

        // ShouldChange
        private bool FShouldChange = true;
        public bool ShouldChange
        {
			get { return FShouldChange; }
			set { FShouldChange = value; } 
		}
        
        // IsDefaultRemotable
        private bool FIsDefaultRemotable = true;
        public bool IsDefaultRemotable
        {
			get { return FIsDefaultRemotable; }
			set { FIsDefaultRemotable = value; }
        }

		private bool FAllDefaultsRemotable = true;
        public bool IsDefaultCallRemotable(string AColumnName)
        {
			// A default call is remotable if the table level default is remotable, 
			// and either a column is specified and that column level default is remotable, 
			// or no column name is specified and all column level defaults are remotable
			return
				FIsDefaultRemotable &&
					(
						(AColumnName == String.Empty) ? 
							FAllDefaultsRemotable : 
							Columns[AColumnName].IsDefaultRemotable
					);
        }
        
        // IsValidateRemotable
        private bool FIsValidateRemotable = true;
        public bool IsValidateRemotable
        {
			get { return FIsValidateRemotable; }
			set { FIsValidateRemotable = value; }
        }
        
		private bool FAllValidatesRemotable = true;
        public bool IsValidateCallRemotable(string AColumnName)
        {
			// A Validate call is remotable if the table level Validate is remotable, 
			// and either a column is specified and that column level Validate is remotable, 
			// or no column name is specified and all column level Validates are remotable
			return
				FIsValidateRemotable &&
					(
						(AColumnName == String.Empty) ? 
							FAllValidatesRemotable : 
							Columns[AColumnName].IsValidateRemotable
					);
        }
        
        // IsChangeRemotable
        private bool FIsChangeRemotable = true;
        public bool IsChangeRemotable
        {
			get { return FIsChangeRemotable; }
			set { FIsChangeRemotable = value; }
        }
        
		private bool FAllChangesRemotable = true;
        public bool IsChangeCallRemotable(string AColumnName)
        {
			// A Change call is remotable if the table level Change is remotable, 
			// and either a column is specified and that column level Change is remotable, 
			// or no column name is specified and all column level Changes are remotable
			return
				FIsChangeRemotable &&
					(
						(AColumnName == String.Empty) ? 
							FAllChangesRemotable : 
							Columns[AColumnName].IsChangeRemotable
					);
        }
        
        public virtual void DetermineShouldCallProposables(ServerProcess AProcess, bool AReset)
        {
			if (AReset)
			{
				FShouldChange = false;
				FShouldDefault = false;
				FShouldValidate = false;
			}
			
			foreach (TableVarColumn LColumn in Columns)
			{
				LColumn.DetermineShouldCallProposables(AProcess, AReset);
				FShouldChange = FShouldChange || LColumn.ShouldChange;
				FShouldDefault = FShouldDefault || LColumn.ShouldDefault;
				FShouldValidate = FShouldValidate || LColumn.ShouldValidate;
			}
			
			if (FRowConstraints.Count > 0)
				FShouldValidate = true;
			
			if (HasHandlers(AProcess) && (FEventHandlers != null))
			{
				foreach (EventHandler LHandler in FEventHandlers)
				{
					if ((LHandler.EventType & EventType.Default) != 0)
						FShouldDefault = true;
					
					if ((LHandler.EventType & EventType.Validate) != 0)
						FShouldValidate = true;
					
					if ((LHandler.EventType & EventType.Change) != 0)
						FShouldChange = true;
				}
			}
        }
        
		public override void DetermineRemotable(ServerProcess AProcess)
		{
			IsDefaultRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsDefaultRemotable", "true"));
			IsValidateRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsValidateRemotable", "true"));
			IsChangeRemotable = Convert.ToBoolean(MetaData.GetTag(MetaData, "DAE.IsChangeRemotable", "true"));
			
			foreach (TableVarColumn LColumn in Columns)
			{
				FAllDefaultsRemotable = FAllDefaultsRemotable && LColumn.IsDefaultRemotable;
				FAllValidatesRemotable = FAllValidatesRemotable && LColumn.IsValidateRemotable;
				FAllChangesRemotable = FAllChangesRemotable && LColumn.IsChangeRemotable;
			}
			
			FAllDefaultsRemotable = FAllChangesRemotable && FAllDefaultsRemotable;
			IsDefaultRemotable = IsChangeRemotable && IsDefaultRemotable;
			
			if (FEventHandlers != null)
			{
				foreach (EventHandler LHandler in FEventHandlers)
				{
					if ((LHandler.EventType & EventType.Default) != 0)
						IsDefaultRemotable = IsDefaultRemotable && LHandler.IsRemotable;
					
					if ((LHandler.EventType & EventType.Validate) != 0)
						IsValidateRemotable = IsValidateRemotable && LHandler.IsRemotable;
					
					if ((LHandler.EventType & EventType.Change) != 0)
						IsChangeRemotable = IsChangeRemotable && LHandler.IsRemotable;
				}
			}
		}
		
		// SourceTableName
		private string FSourceTableName = null;
		/// <summary>The name of the application transaction table variable.</summary>
		public string SourceTableName
		{
			get { return FSourceTableName; }
			set { FSourceTableName = value; }
		}
		
		private bool FIsDeletedTable;
		/// <summary>Indicates whether or not this is the deleted tracking table for a translated table variable.</summary>
		/// <remarks>This property will only be set if SourceTableName is not null.</remarks>
		public bool IsDeletedTable
		{
			get { return FIsDeletedTable; }
			set { FIsDeletedTable = value; }
		}
		
		public override bool IsATObject { get { return FSourceTableName != null; } }
		
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
		private TransitionConstraints FInsertConstraints = new TransitionConstraints();
		public TransitionConstraints InsertConstraints { get { return FInsertConstraints; } }

		// Transition constraints which have an OnUpdateNode
		private TransitionConstraints FUpdateConstraints = new TransitionConstraints();
		public TransitionConstraints UpdateConstraints { get { return FUpdateConstraints; } }

		// Transition constraints which have an OnDeleteNode
		private TransitionConstraints FDeleteConstraints = new TransitionConstraints();
		public TransitionConstraints DeleteConstraints { get { return FDeleteConstraints; } }

		// List of database-wide constraints that reference this table		
		[Reference]
		private CatalogConstraints FCatalogConstraints = new CatalogConstraints(); 
		public CatalogConstraints CatalogConstraints { get { return FCatalogConstraints; } }
		
		// HasDeferredConstraints
		private bool FHasDeferredConstraints;
		private bool FHasDeferredConstraintsComputed;
		
		/// <summary>Indicates whether this table variable has any enforced deferred constraints defined.</summary>
		public bool HasDeferredConstraints()
		{
			if (!FHasDeferredConstraintsComputed)
			{
				FHasDeferredConstraints = false;
				foreach (TableVarConstraint LConstraint in FConstraints)
					if (LConstraint.Enforced && LConstraint.IsDeferred)
					{
						FHasDeferredConstraints = true;
						break;
					}
				FHasDeferredConstraintsComputed = true;
			}
			
			return FHasDeferredConstraints;
		}
		
		/// <summary>Indicates whether this table variable has any enforced deferred constraints defined that would need validation based on the given value flags</summary>
		public bool HasDeferredConstraints(BitArray AValueFlags, Schema.Transition ATransition)
		{
			// TODO: Potential cache point, this only needs to be computed once per ValueFlags combination.
			for (int LIndex = 0; LIndex < FConstraints.Count; LIndex++)
				if (FConstraints[LIndex].Enforced && FConstraints[LIndex].IsDeferred && FConstraints[LIndex].ShouldValidate(AValueFlags, ATransition))
					return true;
			return false;
		}
		
		public void CopyTableVar(TableNode ASourceNode)
		{
			CopyTableVar(ASourceNode, false);
		}
		
		// IsInference indicates whether this copy should be an inference, as in the case of a table var inference on a view
		public void CopyTableVar(TableNode ASourceNode, bool AIsInference)
		{
			// create datatype
			DataType = new Schema.TableType();
				
			// Copy MetaData for the table variable
			if (AIsInference)
				InheritMetaData(ASourceNode.TableVar.MetaData);
			else
				MergeMetaData(ASourceNode.TableVar.MetaData);

			// Copy columns
			Schema.TableVarColumn LNewColumn;
			foreach (Schema.TableVarColumn LColumn in ASourceNode.TableVar.Columns)
			{
				if (AIsInference)
					LNewColumn = LColumn.Inherit();
				else
					LNewColumn = LColumn.Copy();
				DataType.Columns.Add(LNewColumn.Column);
				Columns.Add(LNewColumn);
			}
			
			ShouldChange = ASourceNode.TableVar.ShouldChange;
			ShouldDefault = ASourceNode.TableVar.ShouldDefault;
			ShouldValidate = ASourceNode.TableVar.ShouldValidate;
			
			// Copy keys
			Schema.Key LNewKey;
			foreach (Schema.Key LKey in ASourceNode.TableVar.Keys)
			{
				LNewKey = new Schema.Key();
				LNewKey.IsInherited = true;
				LNewKey.IsSparse = LKey.IsSparse;
				if (AIsInference)
					LNewKey.InheritMetaData(LKey.MetaData);
				else
					LNewKey.MergeMetaData(LKey.MetaData);
				foreach (Schema.TableVarColumn LColumn in LKey.Columns)
					LNewKey.Columns.Add(Columns[LColumn]);
				Keys.Add(LNewKey);
			}
			
			// Copy orders
			Schema.Order LNewOrder;
			Schema.OrderColumn LNewOrderColumn;
			foreach (Schema.Order LOrder in ASourceNode.TableVar.Orders)
			{
				LNewOrder = new Schema.Order();
				LNewOrder.IsInherited = true;
				if (AIsInference)
					LNewOrder.InheritMetaData(LOrder.MetaData);
				else
					LNewOrder.MergeMetaData(LOrder.MetaData);
				Schema.OrderColumn LOrderColumn;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					LOrderColumn = LOrder.Columns[LIndex];
					LNewOrderColumn = new Schema.OrderColumn(Columns[LOrderColumn.Column], LOrderColumn.Ascending, LOrderColumn.IncludeNils);
					LNewOrderColumn.Sort = LOrderColumn.Sort;
					LNewOrderColumn.IsDefaultSort = LOrderColumn.IsDefaultSort;
					LNewOrder.Columns.Add(LNewOrderColumn);
					Error.AssertWarn(LNewOrderColumn.Sort != null, "Sort is null");
				}
				Orders.Add(LNewOrder);
			}
		}
		
		protected void EmitColumns(EmitMode AMode, TableTypeSpecifier ASpecifier)
		{
			NamedTypeSpecifier LColumnSpecifier;
			foreach (TableVarColumn LColumn in Columns)
			{
				LColumnSpecifier = new NamedTypeSpecifier();
				LColumnSpecifier.Identifier = LColumn.Name;
				LColumnSpecifier.TypeSpecifier = LColumn.DataType.EmitSpecifier(AMode);
				ASpecifier.Columns.Add(LColumnSpecifier);
			}
		}
		
		public override void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			if ((SourceTableName != null) && (AMode == EmitMode.ForRemote))
				ASourceCatalog[ASourceCatalog.IndexOfName(SourceTableName)].IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			else
			{
				if (!ATargetCatalog.Contains(Name))
				{
					ATargetCatalog.Add(this);		// this needs to be added before tracing dependencies to avoid recursion

					base.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);

					foreach (TableVarColumn LColumn in Columns)
						LColumn.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
						
					foreach (Key LKey in Keys)
						LKey.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
						
					foreach (Order LOrder in Orders)
						LOrder.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
						
					foreach (Constraint LConstraint in Constraints)
						if ((AMode != EmitMode.ForRemote) || LConstraint.IsRemotable)
							LConstraint.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);	
				}
			}
		}
		
		public override void IncludeHandlers(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			foreach (TableVarColumn LColumn in Columns)
			{
				if (LColumn.DataType is Schema.ScalarType)
					((Schema.ScalarType)LColumn.DataType).IncludeHandlers(AProcess, ASourceCatalog, ATargetCatalog, AMode);
				LColumn.IncludeHandlers(AProcess, ASourceCatalog, ATargetCatalog, AMode);
			}

			if (FEventHandlers != null)
				foreach (EventHandler LHandler in FEventHandlers)
					if ((AMode != EmitMode.ForRemote) || LHandler.IsRemotable)
						LHandler.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
		}
		
		public void IncludeLookupAndDetailReferences(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog)
		{
			foreach (Reference LReference in SourceReferences)
				if ((LReference.ParentReference == null) && !LReference.SourceKey.IsUnique && !ATargetCatalog.Contains(LReference.Name))
				{
					LReference.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, EmitMode.ForRemote);
					LReference.TargetTable.IncludeReferences(AProcess, ASourceCatalog, ATargetCatalog);
				}

			foreach (Reference LReference in TargetReferences)
				if ((LReference.ParentReference == null) && !LReference.SourceKey.IsUnique && !ATargetCatalog.Contains(LReference.Name))
					LReference.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, EmitMode.ForRemote);
		}
		
		public void IncludeParentReferences(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog)
		{
			IncludeLookupAndDetailReferences(AProcess, ASourceCatalog, ATargetCatalog);
			foreach (Reference LReference in SourceReferences)
				if ((LReference.ParentReference == null) && LReference.SourceKey.IsUnique && !ATargetCatalog.Contains(LReference.Name))
				{
					LReference.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, EmitMode.ForRemote);
					LReference.TargetTable.IncludeParentReferences(AProcess, ASourceCatalog, ATargetCatalog);
				}
		}
		
		public void IncludeExtensionReferences(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog)
		{
			foreach (Reference LReference in TargetReferences)
				if ((LReference.ParentReference == null) && LReference.SourceKey.IsUnique && !ATargetCatalog.Contains(LReference.Name))
				{
					LReference.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, EmitMode.ForRemote);
					LReference.SourceTable.IncludeLookupAndDetailReferences(AProcess, ASourceCatalog, ATargetCatalog);
				}
		}
		
		public void IncludeReferences(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog)
		{
			IncludeParentReferences(AProcess, ASourceCatalog, ATargetCatalog);
			IncludeExtensionReferences(AProcess, ASourceCatalog, ATargetCatalog);
		}

		protected void EmitTableVarStatement(EmitMode AMode, CreateTableVarStatement AStatement)
		{
			// The session view created to describe the results of a select statement through the CLI should not be created as a session view
			// in the remote host catalog. The session object name for this view is the same as the object name for the view, and this is
			// the only way that a given catalog object can have the same name in both the session and global catalogs.
			//if ((SessionObjectName != null) && (AMode != EmitMode.ForRemote) && (Schema.Object.EnsureRooted(SessionObjectName) != AStatement.TableVarName))
			if ((SessionObjectName != null) && (Schema.Object.EnsureRooted(SessionObjectName) != AStatement.TableVarName))
			{
				AStatement.IsSession = true;
				AStatement.TableVarName = Schema.Object.EnsureRooted(SessionObjectName);
			}
			
			if ((SourceTableName != null) && (AMode == EmitMode.ForCopy))
				AStatement.TableVarName = Schema.Object.EnsureRooted(SourceTableName);

			for (int LIndex = 0; LIndex < Keys.Count; LIndex++)
				if (!Keys[LIndex].IsInherited || (AStatement is CreateTableStatement))
					AStatement.Keys.Add(Keys[LIndex].EmitStatement(AMode));
					
			for (int LIndex = 0; LIndex < Orders.Count; LIndex++)
				if (!Orders[LIndex].IsInherited || (AStatement is CreateTableStatement))
					AStatement.Orders.Add(Orders[LIndex].EmitStatement(AMode));
					
			for (int LIndex = 0; LIndex < Constraints.Count; LIndex++)
				if ((Constraints[LIndex].ConstraintType == ConstraintType.Row) && ((AMode != EmitMode.ForRemote) || Constraints[LIndex].IsRemotable) && ((AMode != EmitMode.ForStorage) || !Constraints[LIndex].IsPersistent))
					AStatement.Constraints.Add(Constraints[LIndex].EmitDefinition(AMode));

			AStatement.MetaData = MetaData == null ? new MetaData() : MetaData.Copy();
			if (SessionObjectName != null)
				AStatement.MetaData.Tags.AddOrUpdate("DAE.GlobalObjectName", Name, true);
		}

		public Statement EmitDefinition(EmitMode AMode)
		{
			CreateTableStatement LStatement = new CreateTableStatement();
			LStatement.TableVarName = Schema.Object.EnsureRooted(Name);
			EmitTableVarStatement(AMode, LStatement);
			foreach (TableVarColumn LColumn in Columns)
				LStatement.Columns.Add(LColumn.EmitStatement(AMode));
			LStatement.MetaData = MetaData == null ? null : MetaData.Copy();
			if (AMode == EmitMode.ForRemote)
			{
				if (LStatement.MetaData == null)
					LStatement.MetaData = new MetaData();
				LStatement.MetaData.Tags.Add(new Tag("DAE.IsDefaultRemotable", IsDefaultRemotable.ToString()));
				LStatement.MetaData.Tags.Add(new Tag("DAE.IsChangeRemotable", IsChangeRemotable.ToString()));
				LStatement.MetaData.Tags.Add(new Tag("DAE.IsValidateRemotable", IsValidateRemotable.ToString()));
			}
			return LStatement;
		}
		
		public override Object GetObjectFromHeader(ObjectHeader AHeader)
		{
			switch (AHeader.ObjectType)
			{
				case "TableVarColumn" :
					foreach (TableVarColumn LColumn in Columns)
						if (AHeader.ID == LColumn.ID)
							return LColumn;
				break;
							
				case "TableVarColumnDefault" :
				case "TableVarColumnConstraint" :
				//case "TableVarColumnEventHandler" :
					foreach (TableVarColumn LColumn in Columns)
						if (AHeader.ParentObjectID == LColumn.ID)
							return LColumn.GetObjectFromHeader(AHeader);
				break;

				case "Key" :
					foreach (Key LKey in Keys)
						if (AHeader.ID == LKey.ID)
							return LKey;
				break;
							
				case "Order" :
					foreach (Order LOrder in Orders)
						if (AHeader.ID == LOrder.ID)
							return LOrder;
				break;
							
				case "RowConstraint" :
				case "TransitionConstraint" :
					foreach (Constraint LConstraint in FConstraints)
						if (LConstraint.ID == AHeader.ID)
							return LConstraint;
				break;
			}
			
			return base.GetObjectFromHeader(AHeader);
		}

		public void SetShouldReinferReferences(ServerProcess AProcess)
		{
			List<Schema.DependentObjectHeader> LHeaders = AProcess.CatalogDeviceSession.SelectObjectDependents(ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				if (LHeaders[LIndex].ObjectType == "DerivedTableVar")
					AProcess.CatalogDeviceSession.MarkViewForRecompile(LHeaders[LIndex].ID);
		}
	}
    
	public class BaseTableVar : TableVar
    {
		public BaseTableVar(string AName) : base(AName) {}
		public BaseTableVar(int AID, string AName) : base(AID, AName) {}
		public BaseTableVar(string AName, ITableType ATableType) : base(AName)
		{
			DataType = ATableType;
		}
		
		public BaseTableVar(string AName, ITableType ATableType, Device ADevice) : base(AName)
		{
			DataType = ATableType;
			Device = ADevice;
		}
		
		public BaseTableVar(string AName, ITableType ATableType, Device ADevice, bool AIsConstant) : base(AName)
		{
			DataType = ATableType;
			Device = ADevice;
			IsConstant = AIsConstant;
		}
		
		public BaseTableVar(ITableType ATableType) : base(Schema.Object.GetUniqueName())
		{
			DataType = ATableType;
		}
		
		public BaseTableVar(ITableType ATableType, Device ADevice) : base(Schema.Object.GetUniqueName())
		{
			DataType = ATableType;
			Device = ADevice;
		}
		
		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.BaseTableVar"), DisplayName); } }

		// Device
		[Reference]
		private Device FDevice;
		public Device Device
		{
			get { return FDevice; }
			set { FDevice = value; }
		}
		
		public override void DetermineRemotable(ServerProcess AProcess)
		{
			base.DetermineRemotable(AProcess);
			DetermineShouldCallProposables(AProcess, true);
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateTableStatement LStatement = new CreateTableStatement();
			LStatement.TableVarName = Schema.Object.EnsureRooted(Name);
			EmitTableVarStatement(AMode, LStatement);
			LStatement.DeviceName = new IdentifierExpression(Device == null ? String.Empty : Device.Name);
			foreach (TableVarColumn LColumn in Columns)
				LStatement.Columns.Add(LColumn.EmitStatement(AMode));
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropTableStatement LStatement = new DropTableStatement();
			LStatement.ObjectName = Schema.Object.EnsureRooted(Name);
			return LStatement;
		}
	}

	// ResultTableVar is the schema stub for the intermediate result of a relational operator in D4.
	// These are not persisted in the catalog, they only exist in the context of a compiled plan.
	public class ResultTableVar : TableVar
	{
		public ResultTableVar(TableNode ANode) : base(Schema.Object.GetNextObjectID(), Schema.Object.GetUniqueName())
		{
			FNode = ANode;
			DataType = ANode.DataType;
		}

		// Node
		private PlanNode FNode;
		public PlanNode Node
		{
			get { return FNode; }
			set { FNode = value; }
		}

		public override string DisplayName { get { return "<intermediate result>"; } }

		private bool FInferredIsDefaultRemotable = true;
		public bool InferredIsDefaultRemotable
		{
			get { return FInferredIsDefaultRemotable; }
			set { FInferredIsDefaultRemotable = value; }
		}

		private bool FInferredIsValidateRemotable = true;
		public bool InferredIsValidateRemotable
		{
			get { return FInferredIsValidateRemotable; }
			set { FInferredIsValidateRemotable = value; }
		}

		private bool FInferredIsChangeRemotable = true;
		public bool InferredIsChangeRemotable
		{
			get { return FInferredIsChangeRemotable; }
			set { FInferredIsChangeRemotable = value; }
		}
		
		public override void DetermineRemotable(ServerProcess AProcess)
		{
			base.DetermineRemotable(AProcess);
			
			IsDefaultRemotable = IsDefaultRemotable && InferredIsDefaultRemotable;
			IsValidateRemotable = IsValidateRemotable && InferredIsValidateRemotable;
			IsChangeRemotable = IsChangeRemotable && InferredIsChangeRemotable;
		}
	}

	public class DerivedTableVar : TableVar
    {	
		public DerivedTableVar(string AName) : base(AName) {}
		public DerivedTableVar(int AID, string AName) : base(AID, AName) {}
		public DerivedTableVar(string AName, ITableType ATableType) : base(AName)
		{
			DataType = ATableType;
		}

		public override string Description { get { return String.Format(Strings.Get("SchemaObjectDescription.DerivedTableVar"), DisplayName); } }
		
		// ShouldReinferReferences - True if the references for this view should be reinferred when it is next referenced
		private bool FShouldReinferReferences;
		public bool ShouldReinferReferences
		{
			get { return FShouldReinferReferences; }
			set { FShouldReinferReferences = value; }
		}
		
		public override void DetermineRemotable(ServerProcess AProcess)
		{
			base.DetermineRemotable(AProcess);
			DetermineShouldCallProposables(AProcess, false);
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
		protected Expression FInvocationExpression;
		public Expression InvocationExpression
		{
			get { return FInvocationExpression; }
			set { FInvocationExpression = value; }
		}

		public void CopyReferences(TableNode ASourceNode)
		{
			foreach (Schema.Reference LReference in ASourceNode.TableVar.SourceReferences)
			{
				Schema.Reference LNewReference = new Schema.Reference(LReference.Name);
				LNewReference.ParentReference = LReference;
				LNewReference.IsExcluded = LReference.IsExcluded;
				LNewReference.InheritMetaData(LReference.MetaData);
				LNewReference.UpdateReferenceAction = LReference.UpdateReferenceAction;
				LNewReference.DeleteReferenceAction = LReference.DeleteReferenceAction;
				LNewReference.AddDependencies(LReference.Dependencies);
				LNewReference.SourceTable = this;
				LNewReference.SourceKey.IsUnique = LReference.SourceKey.IsUnique;
				LNewReference.SourceKey.Columns.AddRange(LReference.SourceKey.Columns);
				LNewReference.TargetTable = LReference.TargetTable;
				LNewReference.TargetKey.IsUnique = LReference.TargetKey.IsUnique;
				LNewReference.TargetKey.Columns.AddRange(LReference.TargetKey.Columns);
				SourceReferences.Add(LNewReference);
				DerivedReferences.Add(LNewReference);
			}
			
			foreach (Schema.Reference LReference in ASourceNode.TableVar.TargetReferences)
			{
				if (!DerivedReferences.Contains(LReference.Name))
				{
					Schema.Reference LNewReference = new Schema.Reference(LReference.Name);
					LNewReference.ParentReference = LReference;
					LNewReference.IsExcluded = LReference.IsExcluded;
					LNewReference.InheritMetaData(LReference.MetaData);
					LNewReference.UpdateReferenceAction = LReference.UpdateReferenceAction;
					LNewReference.DeleteReferenceAction = LReference.DeleteReferenceAction;
					LNewReference.AddDependencies(LReference.Dependencies);
					LNewReference.SourceTable = LReference.SourceTable;
					LNewReference.SourceKey.IsUnique = LReference.SourceKey.IsUnique;
					LNewReference.SourceKey.Columns.AddRange(LReference.SourceKey.Columns);
					LNewReference.TargetTable = this;
					LNewReference.TargetKey.IsUnique = LReference.TargetKey.IsUnique;
					LNewReference.TargetKey.Columns.AddRange(LReference.TargetKey.Columns);
					TargetReferences.Add(LNewReference);
					DerivedReferences.Add(LNewReference);
				}
			}
		}
		
		public override Statement EmitStatement(EmitMode AMode)
		{
			if (AMode == EmitMode.ForStorage)
				SaveObjectID();
			else
				RemoveObjectID();

			CreateViewStatement LStatement = new CreateViewStatement();
			LStatement.TableVarName = Schema.Object.EnsureRooted(Name);
			EmitTableVarStatement(AMode, LStatement);
			#if USEORIGINALEXPRESSION
			LStatement.Expression = FOriginalExpression;
			#else
			LStatement.Expression = FInvocationExpression;
			#endif
			return LStatement;
		}

		public override Statement EmitDropStatement(EmitMode AMode)
		{
			DropViewStatement LStatement = new DropViewStatement();
			LStatement.ObjectName = Schema.Object.EnsureRooted(Name);
			return LStatement;
		}
    }
}

