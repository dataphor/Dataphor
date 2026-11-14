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
	public interface IGenericType : IDataType {}
	
    public class GenericType : IGenericType
    {
		public GenericType() : base()
		{
			_isGeneric = true;
		}
		
		public GenericType(bool isNil) : base()
		{
			_isGeneric = true;
			_isNil = isNil;
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is IDataType) && Equals((IDataType)objectValue);
		}
		
		public override int GetHashCode()
		{
			return 0;
		}
		
		public bool Equivalent(IDataType dataType)
		{
			return Equals(dataType);
		}

		public bool Equals(IDataType dataType)
		{
			return dataType is IGenericType;
		}

		// Is
		public bool Is(IDataType dataType)
		{
			return dataType is GenericType;
		}
		
		public bool Compatible(IDataType dataType)
		{
			return Is(dataType) || dataType.Is(this);
		}
		
		public string Name
		{
			get { return Schema.DataTypes.SystemGenericName; }
			set { }
		}

		#if NATIVEROW
		public int GetByteSize(object data)
		{
			return 0;
		}
		#else
		// StaticByteSize		
		private int FStaticByteSize = 0;
		public int StaticByteSize
		{
			get { return FStaticByteSize; }
			set { }
		}
		#endif
		
		// IsGeneric
		// Indicates whether this data type is a generic data type (i.e. table, not table{})
		private bool _isGeneric;
		public bool IsGeneric
		{
			get { return _isGeneric; }
			set { _isGeneric = value; }
		}
		
		// IsNil
		// True if the type is known to be the constant nil at compile-time
		private bool _isNil;
		public bool IsNil { get { return _isNil; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool _isDisposable = false;
		public bool IsDisposable
		{
			get { return _isDisposable; }
			set { _isDisposable = value; }
		}

        public TypeSpecifier EmitSpecifier(EmitMode mode)
        {
			return new GenericTypeSpecifier();
        }
        
        public void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
        }
    }
}