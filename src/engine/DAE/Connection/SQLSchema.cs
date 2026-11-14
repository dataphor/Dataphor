/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	public class SQLDomain
	{
		public SQLDomain(Type type, int length, int scale, int precision, bool isLong) : base()
		{
			Type = type;
			Length = length;
			Scale = scale;
			Precision = precision;
			IsLong = isLong;
		}
		
		public Type Type;
		public int Length;
		public int Scale;
		public int Precision;
		public bool IsLong;
	}
	
	public class SQLColumn
	{
		public SQLColumn(string name, SQLDomain domain) : base()
		{
			_name = name;
			_domain = domain;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		private SQLDomain _domain;
		public SQLDomain Domain { get { return _domain; } }
	}
	
	#if USETYPEDLIST
	public class SQLColumns : TypedList
	{
		public SQLColumns() : base(typeof(SQLColumn)){}
		
		public new SQLColumn this[int AIndex]
		{
			get { return (SQLColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	
	#else
	public class SQLColumns : BaseList<SQLColumn>
	{
	#endif
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (name == this[index].Name)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class SQLIndexColumn
	{
		public SQLIndexColumn(string name) : base()
		{
			_name = name;
		}
		
		public SQLIndexColumn(string name, bool ascending) : base()
		{
			_name = name;
			_ascending = ascending;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		private bool _ascending = true;
		public bool Ascending { get { return _ascending; } }
	}
	
	#if USETYPEDLIST
	public class SQLIndexColumns : TypedList
	{
		public SQLIndexColumns() : base(typeof(SQLIndexColumn)){}
		
		public new SQLIndexColumn this[int AIndex]
		{
			get { return (SQLIndexColumn)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	
	#else
	public class SQLIndexColumns : BaseList<SQLIndexColumn>
	{
	#endif
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (name == this[index].Name)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class SQLIndex
	{
		public SQLIndex(string name) : base()
		{
			_name = name;
		}
		
		public SQLIndex(string name, SQLIndexColumn[] columns)
		{
			_name = name;
			_columns.AddRange(columns);
		}
		
		public SQLIndex(string name, SQLIndexColumns columns)
		{
			_name = name;
			_columns.AddRange(columns);
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		private bool _isUnique = false;
		public bool IsUnique 
		{ 
			get { return _isUnique; } 
			set { _isUnique = value; }
		}
		
		private SQLIndexColumns _columns = new SQLIndexColumns();
		public SQLIndexColumns Columns { get { return _columns; } }
	}
	
	#if USETYPEDLIST
	public class SQLIndexes : TypedList
	{
		public SQLIndexes() : base(typeof(SQLIndex)){}
		
		public new SQLIndex this[int AIndex]
		{
			get { return (SQLIndex)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class SQLIndexes : BaseList<SQLIndex>
	{
	#endif
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (name == this[index].Name)
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
	}
	
	public class SQLTableSchema
	{
		private SQLColumns _columns = new SQLColumns();
		public SQLColumns Columns { get { return _columns; } }
		
		private SQLIndexes _indexes = new SQLIndexes();
		public SQLIndexes Indexes { get { return _indexes; } }
	}
}
