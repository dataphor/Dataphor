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
	public interface ICursorType : IDataType
	{
		ITableType TableType { get; set; }
	}
	
	public class CursorType : ICursorType
    {
		public CursorType(ITableType tableType) : base()
		{
			_tableType = tableType;
		}
		
		private ITableType _tableType;
		public ITableType TableType
		{
			get { return _tableType; }
			set { _tableType = value; }
		}
		
		public string Name
		{
			get 
			{ 
				StringBuilder builder = new StringBuilder(Keywords.Cursor);
				if (!IsGeneric)
				{
					builder.Append(Keywords.BeginGroup);
					builder.Append(_tableType.Name);
					builder.Append(Keywords.EndGroup);
				}
				return builder.ToString();
			}
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
		// ByteSize
		public int GetByteSize(object tempValue)
		{
			return 4; // sizeof(int)
		}
		#else
		// StaticByteSize		
		private int FStaticByteSize = 4; // sizeof(int)
		public int StaticByteSize
		{
			get { return FStaticByteSize; }
			set { }
		}
		#endif
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is IDataType) && Equals((IDataType)objectValue);
		}
		
		public override int GetHashCode()
		{
			return _tableType.GetHashCode();
		}

		public bool Equivalent(IDataType dataType)
		{
			return (dataType is ICursorType) && _tableType.Equivalent(((ICursorType)dataType).TableType);
		}
		
		public bool Equals(IDataType dataType)
		{
			return (dataType is ICursorType) && _tableType.Equals(((ICursorType)dataType).TableType);
		}

		public bool Is(IDataType dataType)
		{
			return 
				(dataType is IGenericType) ||
				(
					(dataType is ICursorType) && 
					(
						dataType.IsGeneric ||
						_tableType.Is(((ICursorType)dataType).TableType)
					)
				);
		}
		
		public bool Compatible(IDataType dataType)
		{
			return Is(dataType) || dataType.Is(this);
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode mode)
		{
			CursorTypeSpecifier specifier = new CursorTypeSpecifier();
			specifier.IsGeneric = IsGeneric;
			specifier.TypeSpecifier = TableType == null ? null : TableType.EmitSpecifier(mode);
			return specifier;
		}
		
		public void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
		{
			TableType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
		}
    }
}