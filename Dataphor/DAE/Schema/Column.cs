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
	/// <remarks> Provides the representation for a column header (Name:DataType) </remarks>
	public class Column : Schema.Object
    {
		// constructor
		public Column(string AName, IDataType ADataType) : base(AName)
		{
			FDataType = ADataType;
		}
		
		// DataType		
		[Reference]
		private IDataType FDataType;
		public IDataType DataType 
		{ 
			get { return FDataType; } 
			set { FDataType = value; }
		}

		// Equals
		public override bool Equals(object AObject)
		{
			Column LColumn = AObject as Column;
			return
				(LColumn != null) &&
				(String.Compare(Name, LColumn.Name) == 0) &&
				(
					((FDataType == null) && (LColumn.DataType == null)) ||
					((FDataType != null) && FDataType.Equals(LColumn.DataType))
				);
		}

		// GetHashCode
		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ (FDataType == null ? 0 : FDataType.GetHashCode());
		}

        // ToString
        public override string ToString()
        {
			StringBuilder LBuilder = new StringBuilder(Name);
			if (FDataType != null)
			{
				LBuilder.Append(Keywords.TypeSpecifier);
				LBuilder.Append(FDataType.Name);
			}
			return LBuilder.ToString();
        }
        
        public Column Copy()
        {
			return new Column(Name, DataType);
		}

		public Column Copy(string APrefix)
		{
			return new Column(Schema.Object.Qualify(Name, APrefix), DataType);
		}
		
		public Column CopyAndRename(string AName)
		{
			return new Column(AName, DataType);
		}
	}

	/// <remarks> Provides a container for Column objects </remarks>
	public class Columns : Schema.Objects
    {
		public Columns() : base() {}
		
		// Column lists are equal if they contain the same number of columns and all columns in the left
		// list are also in the right list
        public override bool Equals(object AObject)
        {
			Columns LColumns = AObject as Columns;
			if ((LColumns != null) && (Count == LColumns.Count))
			{
				foreach (Column LColumn in this)
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
        public bool Equivalent(Columns AColumns)
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

        public virtual bool Compatible(object AObject)
        {
			return Is(AObject) && ((Columns)AObject).Is(this);
        }

        public virtual bool Is(object AObject)
        {
			// A column list is another column list if they both have the same number of columns
			// and the is of the datatypes for all columns evaluates to true by name
			Columns LColumns = AObject as Columns;
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

		public bool IsSubsetOf(Columns AColumns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (Column LColumn in this)
				if (!AColumns.ContainsName(LColumn.Name))
					return false;
			return true;
		}
		
		public bool IsProperSubsetOf(Columns AColumns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(AColumns) && (Count < AColumns.Count);
		}

		public bool IsSupersetOf(Columns AColumns)
		{
			// true if every column in AColumnNames is in this set of columns
			foreach (Column LColumn in AColumns)
				if (!ContainsName(LColumn.Name))
					return false;
			return true;
		}
		
		public bool IsProperSupersetOf(Columns AColumns)
		{
			// true if every column in AColumnNames is in this set of columns and this set of columns is strictly larger
			return IsSupersetOf(AColumns) && (Count > AColumns.Count);
		}

		public bool IsSubsetOf(TableVarColumnsBase AColumns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (Column LColumn in this)
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

        public new Column this[int AIndex]
        {
            get { return (Column)(base[AIndex]); }
            set { base[AIndex] = value; }
        }

        public new Column this[string AColumnName]
        {
			get { return (Column)base[AColumnName]; }
			set { base[AColumnName] = value; }
        }
        
        public Column this[Column AColumn]
        {
			get { return this[IndexOfName(AColumn.Name)]; }
			set { this[IndexOfName(AColumn.Name)] = value; }
		}
        
		/// <summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.</summary>
		public int IndexOfColumn(string AColumnName)
		{
			int LColumnIndex = IndexOfName(AColumnName);
			if (LColumnIndex < 0)
				LColumnIndex = IndexOf(AColumnName);
			return LColumnIndex;
		}

		///	<summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.  Throws an exception if the column name is not found.</summary>
		public int GetIndexOfColumn(string AColumnName)
		{
			int LColumnIndex = IndexOfName(AColumnName);
			if (LColumnIndex < 0)
				LColumnIndex = IndexOf(AColumnName);
			if (LColumnIndex < 0)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ColumnNotFound, AColumnName);
			return LColumnIndex;
		}
		
		// ToString
        public override string ToString()
        {
			StringBuilder LString = new StringBuilder();
			foreach (Column LColumn in this)
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
}