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
		public CursorType(ITableType ATableType) : base()
		{
			FTableType = ATableType;
		}
		
		private ITableType FTableType;
		public ITableType TableType
		{
			get { return FTableType; }
			set { FTableType = value; }
		}
		
		public string Name
		{
			get 
			{ 
				StringBuilder LBuilder = new StringBuilder(Keywords.Cursor);
				if (!IsGeneric)
				{
					LBuilder.Append(Keywords.BeginGroup);
					LBuilder.Append(FTableType.Name);
					LBuilder.Append(Keywords.EndGroup);
				}
				return LBuilder.ToString();
			}
			set { }
		}
		
		// IsGeneric
		// Indicates whether this data type is a generic data type (i.e. table, not table{})
		private bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
		}
		
		public bool IsNil { get { return false; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool FIsDisposable = false;
		public bool IsDisposable
		{
			get { return FIsDisposable; }
			set { FIsDisposable = value; }
		}

		#if NATIVEROW
		// ByteSize
		public int GetByteSize(object AValue)
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
		
		public override bool Equals(object AObject)
		{
			return (AObject is IDataType) && Equals((IDataType)AObject);
		}
		
		public override int GetHashCode()
		{
			return FTableType.GetHashCode();
		}

		public bool Equivalent(IDataType ADataType)
		{
			return (ADataType is ICursorType) && FTableType.Equivalent(((ICursorType)ADataType).TableType);
		}
		
		public bool Equals(IDataType ADataType)
		{
			return (ADataType is ICursorType) && FTableType.Equals(((ICursorType)ADataType).TableType);
		}

		public bool Is(IDataType ADataType)
		{
			return 
				(ADataType is IGenericType) ||
				(
					(ADataType is ICursorType) && 
					(
						ADataType.IsGeneric ||
						FTableType.Is(((ICursorType)ADataType).TableType)
					)
				);
		}
		
		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode AMode)
		{
			CursorTypeSpecifier LSpecifier = new CursorTypeSpecifier();
			LSpecifier.IsGeneric = IsGeneric;
			LSpecifier.TypeSpecifier = TableType == null ? null : TableType.EmitSpecifier(AMode);
			return LSpecifier;
		}
		
		public void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
		{
			TableType.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
		}
    }
}