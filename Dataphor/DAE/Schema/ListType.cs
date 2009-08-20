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
		public ListType(IDataType AElementType) : base()
		{
			FIsDisposable = true;
			FElementType = AElementType;
		}
		
		[Reference]
		private IDataType FElementType;
		public IDataType ElementType
		{
			get { return FElementType; }
			set { FElementType = value; }
		}
		
		public string Name
		{
			get 
			{ 
				StringBuilder LBuilder = new StringBuilder(Keywords.List);
				if (!IsGeneric)
				{
					LBuilder.Append(Keywords.BeginGroup);
					LBuilder.Append(FElementType.Name);
					LBuilder.Append(Keywords.EndGroup);
				}
				return LBuilder.ToString();
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
		private bool FIsGeneric;
		public bool IsGeneric
		{
			get { return FIsGeneric; }
			set { FIsGeneric = value; }
		}
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool FIsDisposable = false;
		public bool IsDisposable
		{
			get { return FIsDisposable; }
			set { FIsDisposable = value; }
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is IDataType) && Equals((IDataType)AObject);
		}
		
		public override int GetHashCode()
		{
			return FElementType.GetHashCode();
		}
		
		public bool Equivalent(IDataType ADataType)
		{
			return (ADataType is IListType) && FElementType.Equivalent(((IListType)ADataType).ElementType);
		}

		public bool Equals(IDataType ADataType)
		{
			return (ADataType is IListType) && FElementType.Equals(((IListType)ADataType).ElementType);
		}

		public bool Is(IDataType ADataType)
		{
			return 
				(ADataType is IGenericType) ||
				(
					(ADataType is IListType) && 
					(
						ADataType.IsGeneric ||
						FElementType.Is(((IListType)ADataType).ElementType)
					)
				);
		}
		
		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode AMode)
		{
			ListTypeSpecifier LSpecifier = new ListTypeSpecifier();
			LSpecifier.IsGeneric = IsGeneric;
			LSpecifier.TypeSpecifier = ElementType == null ? null : ElementType.EmitSpecifier(AMode);
			return LSpecifier;
		}

        public void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			ElementType.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
        }
   }
}