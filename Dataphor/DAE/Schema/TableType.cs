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
			FColumns = new Columns();
		}
		
		public IRowType CreateRowType(string APrefix, bool AIncludeColumns)
		{
			if (IsGeneric)
			{
				Schema.RowType LRowType = new RowType();
				LRowType.IsGeneric = true;
				return LRowType;
			}

			if ((APrefix == null) || (APrefix == String.Empty))
			{
				if (AIncludeColumns)
					return new RowType(Columns);
				return new RowType();
			}
			
			return new RowType(Columns, APrefix);
		}

		public IRowType CreateRowType(string APrefix)
		{
			return CreateRowType(APrefix, false);
		}
		
		public IRowType CreateRowType(bool AIncludeColumns)
		{
			return CreateRowType(null, AIncludeColumns);
		}
		
		public IRowType CreateRowType()
		{
			return CreateRowType(null, true);
		}
		
		public void ResetRowType()
		{
			FRowType = null;
			FNewRowType = null;
			FOldRowType = null;
		}
		
		private IRowType FRowType;
		public IRowType RowType
		{
			get
			{
				if (FRowType == null)
					FRowType = CreateRowType();
				return FRowType;
			}
		}
		
		private IRowType FNewRowType;
		public IRowType NewRowType
		{
			get
			{
				if (FNewRowType == null)
					FNewRowType = CreateRowType(Keywords.New);
				return FNewRowType;
			}
		}
		
		private IRowType FOldRowType;
		public IRowType OldRowType
		{
			get
			{
				if (FOldRowType == null)
					FOldRowType = CreateRowType(Keywords.Old);
				return FOldRowType;
			}
		}
		
		public string Name
		{
			get { return ToString(); }
			set { }
		}
		
		// StaticByteSize		
		private int FStaticByteSize = 8; // sizeof(StreamID)
		public int StaticByteSize
		{
			get { return FStaticByteSize; }
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

		// Columns
		private Columns FColumns;
		public Columns Columns { get { return FColumns; } }
		
		protected void EmitColumns(EmitMode AMode, TableTypeSpecifier ASpecifier)
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
		
		// Equivalent
		public bool Equivalent(IDataType ADataType)
		{
			return (ADataType is ITableType) && Columns.Equivalent(((ITableType)ADataType).Columns);
		}

		// Equals
		public bool Equals(IDataType ADataType)
		{
			return (ADataType is ITableType) && (Columns.Equals(((ITableType)ADataType).Columns));
		}
		
		// Is
		public bool Is(IDataType ADataType)
		{
			return
				(ADataType is IGenericType) ||
				(
					(ADataType is ITableType) &&
					(
						ADataType.IsGeneric ||
						Columns.Is(((ITableType)ADataType).Columns)
					)
				);
		}
		
		// ToString
		public override string ToString()
		{
			return Keywords.Table + (IsGeneric ? String.Empty : Columns.ToString());
		}
		
        public TypeSpecifier EmitSpecifier(EmitMode AMode)
        {
			TableTypeSpecifier LSpecifier = new TableTypeSpecifier();
			LSpecifier.IsGeneric = IsGeneric;
			EmitColumns(AMode, LSpecifier);
			return LSpecifier;
        }

		public bool Compatible(IDataType ADataType)
		{
			return Is(ADataType) || ADataType.Is(this);
		}

        public void IncludeDependencies(ServerProcess AProcess, Catalog ASourceCatalog, Catalog ATargetCatalog, EmitMode AMode)
        {
			foreach (Column LColumn in Columns)
				LColumn.DataType.IncludeDependencies(AProcess, ASourceCatalog, ATargetCatalog, AMode);
        }
    }
}
