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
	public interface IListType : IDataType
	{
		IDataType ElementType { get; set; }
	}
	
	public class ListType : IListType
    {
		public ListType(IDataType elementType) : base()
		{
			_isDisposable = true;
			_elementType = elementType;
		}
		
		[Reference]
		private IDataType _elementType;
		public IDataType ElementType
		{
			get { return _elementType; }
			set { _elementType = value; }
		}
		
		public string Name
		{
			get 
			{ 
				StringBuilder builder = new StringBuilder(Keywords.List);
				if (!IsGeneric)
				{
					builder.Append(Keywords.BeginGroup);
					builder.Append(_elementType.Name);
					builder.Append(Keywords.EndGroup);
				}
				return builder.ToString();
			}
			set { }
		}
		
		#if NATIVEROW
/*
		public int GetByteSize(object AValue)
		{
			int LSize = 4; // sizeof(int) to write the count of items in the list
			ListValue LValue = AValue as ListValue;
			IDataValue LDataValue;
			if (LValue != null)
				foreach (object LItem in LValue)
				{
					LDataValue = LItem as IDataValue;
					if (LItem != null)
					{
						LSize += LItem.DataType.GetByteSize(LItem);
						break;
					}
					
					LSize += System.Runtime.InteropServices.Marshal.SizeOf(LItem);
				}
			return LSize;
		}
*/
		#else
		// StaticByteSize		
		private int FStaticByteSize = 8; // sizeof(StreamID)
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
		
		public bool IsNil { get { return false; } }
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool _isDisposable = false;
		public bool IsDisposable
		{
			get { return _isDisposable; }
			set { _isDisposable = value; }
		}
		
		public override bool Equals(object objectValue)
		{
			return (objectValue is IDataType) && Equals((IDataType)objectValue);
		}
		
		public override int GetHashCode()
		{
			return _elementType.GetHashCode();
		}
		
		public bool Equivalent(IDataType dataType)
		{
			return (dataType is IListType) && _elementType.Equivalent(((IListType)dataType).ElementType);
		}

		public bool Equals(IDataType dataType)
		{
			return (dataType is IListType) && _elementType.Equals(((IListType)dataType).ElementType);
		}

		public bool Is(IDataType dataType)
		{
			return 
				(dataType is IGenericType) ||
				(
					(dataType is IListType) && 
					(
						dataType.IsGeneric ||
						_elementType.Is(((IListType)dataType).ElementType)
					)
				);
		}
		
		public bool Compatible(IDataType dataType)
		{
			return Is(dataType) || dataType.Is(this);
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode mode)
		{
			ListTypeSpecifier specifier = new ListTypeSpecifier();
			specifier.IsGeneric = IsGeneric;
			specifier.TypeSpecifier = ElementType == null ? null : ElementType.EmitSpecifier(mode);
			return specifier;
		}

        public void IncludeDependencies(CatalogDeviceSession session, Catalog sourceCatalog, Catalog targetCatalog, EmitMode mode)
        {
			ElementType.IncludeDependencies(session, sourceCatalog, targetCatalog, mode);
        }
   }
}