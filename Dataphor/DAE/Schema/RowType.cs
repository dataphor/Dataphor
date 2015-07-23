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
	public interface IRowType : IDataType
	{
		Columns Columns { get; }
	}
	
	/// <remarks> Representation of a row header </remarks>    
	public class RowType : IRowType
    {
		public RowType() : base()
		{
			InternalInitialize();
		}
		
		public RowType(bool isGeneric) : base()
		{
			IsGeneric = isGeneric;
			InternalInitialize();
		}
		
		public RowType(Columns columns) : base()
		{
			InternalInitialize();
			foreach (Column column in columns)
				_columns.Add(column.Copy());
		}
		
		public RowType(Columns columns, string prefix) : base()
		{
			InternalInitialize();
			foreach (Column column in columns)
				_columns.Add(column.Copy(prefix));
		}
		
		public RowType(TableVarColumnsBase columns) : base()
		{
			InternalInitialize();
			foreach (TableVarColumn column in columns)
				_columns.Add(column.Column.Copy());
		}
		
		public RowType(TableVarColumnsBase columns, string prefix) : base()
		{
			InternalInitialize();
			foreach (TableVarColumn column in columns)
				_columns.Add(column.Column.Copy(prefix));
		}
		
		public RowType(OrderColumns columns) : base()
		{
			InternalInitialize();
			for (int index = 0; index < columns.Count; index++)
				if (!_columns.ContainsName(columns[index].Column.Name))
					_columns.Add(columns[index].Column.Column.Copy());
		}
		
		public RowType(OrderColumns columns, string prefix) : base()
		{
			InternalInitialize();
			for (int index = 0; index < columns.Count; index++)
				if (!_columns.ContainsName(Schema.Object.Qualify(columns[index].Column.Name, prefix)))
					_columns.Add(columns[index].Column.Column.Copy(prefix));
		}

		private void InternalInitialize()
		{
			IsDisposable = true;
			_columns = new Columns();
		}
		
		private Columns _columns;
		public Columns Columns { get { return _columns; } }
		
		public string Name
		{
			get { return ToString(); }
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

		#if NATIVEROW
/*
		public unsafe int GetByteSize(object AValue)
		{
			checked
			{
				Row LValue = AValue as Row;
				object LColumnValue;
				IDataValue LDataValue;
				if (LValue != null)
				{
					int LSize = 2 * MathUtility.IntegerCeilingDivide(Columns.Count, 8);
					// return the size required to store the physical representation of the row in bytes
					for (int LIndex = 0; LIndex < Columns.Count; LIndex++)
					{
						if (LValue.HasValue(LIndex))
						{
							LColumnValue = LValue.GetValue(LIndex);
							LDataValue = LColumnValue as LDataValue;
							if (LDataValue != null)
							{
								LSize += LDataValue.DataType.GetByteSize(LDataValue);
								break;
							}
							
							LSize += System.Runtime.InteropServices.Marshal.SizeOf(LColumnValue);
							break;
						}
					}
					return LSize;
				}
				return 0;
			}
		}
*/
		#else
		public int StaticByteSize
		{
			get
			{
				checked
				{
					int LSize = (2 * MathUtility.IntegerCeilingDivide(Columns.Count, 8));
					foreach (Column LColumn in Columns)
						LSize += LColumn.DataType.StaticByteSize;
					return LSize;
				}
			}
			set { }
		}
		#endif
		
		protected void EmitColumns(EmitMode mode, RowTypeSpecifier specifier)
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

        public void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			foreach (Column column in Columns)
				column.DataType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
        }
        
        public bool Equivalent(IDataType dataType)
        {
			return (dataType is IRowType) && Columns.Equivalent(((IRowType)dataType).Columns);
        }

		public bool Equals(IDataType dataType)
		{
			return (dataType is IRowType) && Columns.Equals(((IRowType)dataType).Columns);
		}
		
		public bool Is(IDataType dataType)
		{
			return 
				(dataType is IGenericType) ||
				(
					(dataType is IRowType) && 
					(
						dataType.IsGeneric ||
						Columns.Is(((IRowType)dataType).Columns)
					)
				);
		}

		public bool Compatible(IDataType dataType)
		{
			return Is(dataType) || dataType.Is(this);
		}
		
		public override string ToString()
		{
			return Keywords.Row + (IsGeneric ? String.Empty : Columns.ToString());
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode mode)
		{
			RowTypeSpecifier specifier = new RowTypeSpecifier();
			specifier.IsGeneric = IsGeneric;
			EmitColumns(mode, specifier);
			return specifier;
		}
    }
}
