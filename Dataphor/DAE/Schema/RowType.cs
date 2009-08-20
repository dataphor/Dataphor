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
		
		public RowType(bool AIsGeneric) : base()
		{
			IsGeneric = AIsGeneric;
			InternalInitialize();
		}
		
		public RowType(Columns AColumns) : base()
		{
			InternalInitialize();
			foreach (Column LColumn in AColumns)
				FColumns.Add(LColumn.Copy());
		}
		
		public RowType(Columns AColumns, string APrefix) : base()
		{
			InternalInitialize();
			foreach (Column LColumn in AColumns)
				FColumns.Add(LColumn.Copy(APrefix));
		}
		
		public RowType(KeyColumns AColumns) : base()
		{
			InternalInitialize();
			foreach (TableVarColumn LColumn in AColumns)
				FColumns.Add(LColumn.Column.Copy());
		}
		
		public RowType(KeyColumns AColumns, string APrefix) : base()
		{
			InternalInitialize();
			foreach (TableVarColumn LColumn in AColumns)
				FColumns.Add(LColumn.Column.Copy(APrefix));
		}
		
		public RowType(OrderColumns AColumns) : base()
		{
			InternalInitialize();
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				if (!FColumns.ContainsName(AColumns[LIndex].Column.Name))
					FColumns.Add(AColumns[LIndex].Column.Column.Copy());
		}
		
		public RowType(OrderColumns AColumns, string APrefix) : base()
		{
			InternalInitialize();
			for (int LIndex = 0; LIndex < AColumns.Count; LIndex++)
				if (!FColumns.ContainsName(Schema.Object.Qualify(AColumns[LIndex].Column.Name, APrefix)))
					FColumns.Add(AColumns[LIndex].Column.Column.Copy(APrefix));
		}

		private void InternalInitialize()
		{
			IsDisposable = true;
			FColumns = new Columns();
		}
		
		private Columns FColumns;
		public Columns Columns { get { return FColumns; } }
		
		public string Name
		{
			get { return ToString(); }
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
		
		// IsDisposable
		// Indicates whether the host representation for this data type must be disposed
		private bool FIsDisposable = false;
		public bool IsDisposable
		{
			get { return FIsDisposable; }
			set { FIsDisposable = value; }
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
		public unsafe int StaticByteSize
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
		
		protected void EmitColumns(EmitMode AMode, RowTypeSpecifier ASpecifier)
		{
			NamedTypeSpecifier LColumnSpecifier;
			foreach (Column LColumn in Columns)
			{
				LColumnSpecifier = new NamedTypeSpecifier();
				LColumnSpecifier.Identifier = LColumn.Name;
				LColumnSpecifier.TypeSpecifier = LColumn.DataType.EmitSpecifier(AMode);
				ASpecifier.Columns.Add(LColumnSpecifier);
			}
		}
		
		public override bool Equals(object AObject)
		{
			return (AObject is IDataType) && Equals((IDataType)AObject);
		}
		
		public override int GetHashCode()
		{
			return Columns.GetHashCode();
		}

        public void IncludeDependencies(CatalogDeviceSession ASession, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			foreach (Column LColumn in Columns)
				LColumn.DataType.IncludeDependencies(ASession, ASourceCatalog, ATargetCatalog, AMode);
        }
        
        public bool Equivalent(IDataType ADataType)
        {
			return (ADataType is IRowType) && Columns.Equivalent(((IRowType)ADataType).Columns);
        }

		public bool Equals(IDataType ADataType)
		{
			return (ADataType is IRowType) && Columns.Equals(((IRowType)ADataType).Columns);
		}
		
		public bool Is(IDataType ADataType)
		{
			return 
				(ADataType is IGenericType) ||
				(
					(ADataType is IRowType) && 
					(
						ADataType.IsGeneric ||
						Columns.Is(((IRowType)ADataType).Columns)
					)
				);
		}

		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}
		
		public override string ToString()
		{
			return Keywords.Row + (IsGeneric ? String.Empty : Columns.ToString());
		}
		
		public TypeSpecifier EmitSpecifier(EmitMode AMode)
		{
			RowTypeSpecifier LSpecifier = new RowTypeSpecifier();
			LSpecifier.IsGeneric = IsGeneric;
			EmitColumns(AMode, LSpecifier);
			return LSpecifier;
		}
    }
}
