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
	public class Column : Schema.ObjectBase
    {
		// constructor
		public Column(string name, IDataType dataType) : base(name)
		{
			_dataType = dataType;
		}
		
		// DataType		
		[Reference]
		private IDataType _dataType;
		public IDataType DataType 
		{ 
			get { return _dataType; } 
			set { _dataType = value; }
		}

		// Equals
		public override bool Equals(object objectValue)
		{
			Column column = objectValue as Column;
			return
				(column != null) &&
				(String.Compare(Name, column.Name) == 0) &&
				(
					((_dataType == null) && (column.DataType == null)) ||
					((_dataType != null) && _dataType.Equals(column.DataType))
				);
		}

		// GetHashCode
		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ (_dataType == null ? 0 : _dataType.GetHashCode());
		}

        // ToString
        public override string ToString()
        {
			StringBuilder builder = new StringBuilder(Name);
			if (_dataType != null)
			{
				builder.Append(Keywords.TypeSpecifier);
				builder.Append(_dataType.Name);
			}
			return builder.ToString();
        }
        
        public Column Copy()
        {
			return new Column(Name, DataType);
		}

		public Column Copy(string prefix)
		{
			return new Column(Schema.Object.Qualify(Name, prefix), DataType);
		}
		
		public Column CopyAndRename(string name)
		{
			return new Column(name, DataType);
		}
	}

	/// <remarks> Provides a container for Column objects </remarks>
	public class Columns : Schema.BaseObjects<Column>
    {
		public Columns() : base() {}
		
		// Column lists are equal if they contain the same number of columns and all columns in the left
		// list are also in the right list
        public override bool Equals(object objectValue)
        {
			Columns columns = objectValue as Columns;
			if ((columns != null) && (Count == columns.Count))
			{
				foreach (Column column in this)
					if (!columns.Contains(column))
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
				string[] result = new string[Count];
				for (int index = 0; index < Count; index++)
					result[index] = this[index].Name;
				return result;
			}
        }

		// Column lists are equivalent if they contain the same number of columns and the columns are equal, left to right
		// This is an internal notion for use in physical contexts only
        public bool Equivalent(Columns columns)
        {
			if (Count == columns.Count)
			{
				for (int index = 0; index < Count; index++)
					if (!this[index].Equals(columns[index]))
						return false;
				return true;
			}
			return false;
        }

        public override int GetHashCode()
        {
			int hashCode = 0;
			for (int index = 0; index < Count; index++)
				hashCode ^= this[index].GetHashCode();
			return hashCode;
        }

        public virtual bool Compatible(object objectValue)
        {
			return Is(objectValue) && ((Columns)objectValue).Is(this);
        }

        public virtual bool Is(object objectValue)
        {
			// A column list is another column list if they both have the same number of columns
			// and the is of the datatypes for all columns evaluates to true by name
			Columns columns = objectValue as Columns;
			if ((columns != null) && (Count == columns.Count))
			{
				int columnIndex;
				for (int index = 0; index < Count; index++)
				{
					columnIndex = columns.IndexOfName(this[index].Name);
					if (columnIndex >= 0)
					{
						if (!this[index].DataType.Is(columns[columnIndex].DataType))
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

		public bool IsSubsetOf(Columns columns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (Column column in this)
				if (!columns.ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSubsetOf(Columns columns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(columns) && (Count < columns.Count);
		}

		public bool IsSupersetOf(Columns columns)
		{
			// true if every column in AColumnNames is in this set of columns
			foreach (Column column in columns)
				if (!ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSupersetOf(Columns columns)
		{
			// true if every column in AColumnNames is in this set of columns and this set of columns is strictly larger
			return IsSupersetOf(columns) && (Count > columns.Count);
		}

		public bool IsSubsetOf(TableVarColumnsBase columns)
		{
			// true if every column in this set of columns is in AColumns
			foreach (Column column in this)
				if (!columns.ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSubsetOf(TableVarColumnsBase columns)
		{
			// true if every column in this set of columns is in AColumns and AColumns is strictly larger
			return IsSubsetOf(columns) && (Count < columns.Count);
		}

		public bool IsSupersetOf(TableVarColumnsBase columns)
		{
			// true if every column in AColumnNames is in this set of columns
			foreach (TableVarColumn column in columns)
				if (!ContainsName(column.Name))
					return false;
			return true;
		}
		
		public bool IsProperSupersetOf(TableVarColumnsBase columns)
		{
			// true if every column in AColumnNames is in this set of columns and this set of columns is strictly larger
			return IsSupersetOf(columns) && (Count > columns.Count);
		}

        public Column this[Column column]
        {
			get { return this[IndexOfName(column.Name)]; }
			set { this[IndexOfName(column.Name)] = value; }
		}
        
		/// <summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.</summary>
		public int IndexOfColumn(string columnName)
		{
			int columnIndex = IndexOfName(columnName);
			if (columnIndex < 0)
				columnIndex = IndexOf(columnName);
			return columnIndex;
		}

		///	<summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.  Throws an exception if the column name is not found.</summary>
		public int GetIndexOfColumn(string columnName)
		{
			int columnIndex = IndexOfName(columnName);
			if (columnIndex < 0)
				columnIndex = IndexOf(columnName);
			if (columnIndex < 0)
				throw new Schema.SchemaException(Schema.SchemaException.Codes.ColumnNotFound, columnName);
			return columnIndex;
		}
		
		// ToString
        public override string ToString()
        {
			StringBuilder stringValue = new StringBuilder();
			foreach (Column column in this)
			{
				if (stringValue.Length != 0)
				{
					stringValue.Append(Keywords.ListSeparator);
					stringValue.Append(" ");
				}
				stringValue.Append(column.ToString());
			}
			stringValue.Insert(0, Keywords.BeginList);
			stringValue.Append(Keywords.EndList);
			return stringValue.ToString();
        }
    }
}