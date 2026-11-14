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

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Device.Catalog;

namespace Alphora.Dataphor.DAE.Schema
{
	public interface ITableType : IDataType
	{
		Columns Columns { get; }
		IRowType CreateRowType(string APrefix, bool AIncludeColumns);
		IRowType CreateRowType(string APrefix);
		IRowType CreateRowType(bool AIncludeColumns);
		IRowType CreateRowType();
		IRowType RowType { get; }
		IRowType NewRowType { get; }
		IRowType OldRowType { get; }
		void ResetRowType();
	}
	
	public class TableType : ITableType
    {
		public TableType() : base()
		{
			InternalInitialize();
		}

		private void InternalInitialize()
		{
			IsDisposable = true;
			_columns = new Columns();
		}
		
		public IRowType CreateRowType(string prefix, bool includeColumns)
		{
			if (IsGeneric)
			{
				Schema.RowType rowType = new RowType();
				rowType.IsGeneric = true;
				return rowType;
			}

			if ((prefix == null) || (prefix == String.Empty))
			{
				if (includeColumns)
					return new RowType(Columns);
				return new RowType();
			}
			
			return new RowType(Columns, prefix);
		}

		public IRowType CreateRowType(string prefix)
		{
			return CreateRowType(prefix, false);
		}
		
		public IRowType CreateRowType(bool includeColumns)
		{
			return CreateRowType(null, includeColumns);
		}
		
		public IRowType CreateRowType()
		{
			return CreateRowType(null, true);
		}
		
		public void ResetRowType()
		{
			_rowType = null;
			_newRowType = null;
			_oldRowType = null;
		}
		
		private IRowType _rowType;
		public IRowType RowType
		{
			get
			{
				if (_rowType == null)
					_rowType = CreateRowType();
				return _rowType;
			}
		}
		
		private IRowType _newRowType;
		public IRowType NewRowType
		{
			get
			{
				if (_newRowType == null)
					_newRowType = CreateRowType(Keywords.New);
				return _newRowType;
			}
		}
		
		private IRowType _oldRowType;
		public IRowType OldRowType
		{
			get
			{
				if (_oldRowType == null)
					_oldRowType = CreateRowType(Keywords.Old);
				return _oldRowType;
			}
		}
		
		public string Name
		{
			get { return ToString(); }
			set { }
		}
		
		// StaticByteSize		
		private int _staticByteSize = 8; // sizeof(StreamID)
		public int StaticByteSize
		{
			get { return _staticByteSize; }
			set { }
		}
		
		// IsGeneric
		// Indicates whether this data type is a generic data type (i.e. table, not table{})
		private bool _isGeneric;
		public bool IsGeneric
		{
			get { return _isGeneric; }
			set { _isGeneric = value; }
		}
		
		public bool IsNil { get { return false; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool _isDisposable = false;
		public bool IsDisposable
		{
			get { return _isDisposable; }
			set { _isDisposable = value; }
		}

		// Columns
		private Columns _columns;
		public Columns Columns { get { return _columns; } }
		
		protected void EmitColumns(EmitMode mode, TableTypeSpecifier specifier)
		{
			NamedTypeSpecifier columnSpecifier;
			foreach (Column column in Columns)
			{
				columnSpecifier = new NamedTypeSpecifier();
				columnSpecifier.Identifier = column.Name;
				columnSpecifier.TypeSpecifier = column.DataType.EmitSpecifier(mode);
				specifier.Columns.Add(columnSpecifier);
			}
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is IDataType) && Equals((IDataType)objectValue);
		}
		
		public override int GetHashCode()
		{
			return Columns.GetHashCode();
		}
		
		// Equivalent
		public bool Equivalent(IDataType dataType)
		{
			return (dataType is ITableType) && Columns.Equivalent(((ITableType)dataType).Columns);
		}

		// Equals
		public bool Equals(IDataType dataType)
		{
			return (dataType is ITableType) && (Columns.Equals(((ITableType)dataType).Columns));
		}
		
		// Is
		public bool Is(IDataType dataType)
		{
			return
				(dataType is IGenericType) ||
				(
					(dataType is ITableType) &&
					(
						dataType.IsGeneric ||
						Columns.Is(((ITableType)dataType).Columns)
					)
				);
		}
		
		// ToString
		public override string ToString()
		{
			return Keywords.Table + (IsGeneric ? String.Empty : Columns.ToString());
		}
		
        public TypeSpecifier EmitSpecifier(EmitMode mode)
        {
			TableTypeSpecifier specifier = new TableTypeSpecifier();
			specifier.IsGeneric = IsGeneric;
			EmitColumns(mode, specifier);
			return specifier;
        }

		public bool Compatible(IDataType dataType)
		{
			return Is(dataType) || dataType.Is(this);
		}

        public void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			foreach (Column column in Columns)
				column.DataType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
        }
    }
}
