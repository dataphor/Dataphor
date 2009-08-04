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
		public SQLDomain(Type AType, int ALength, int AScale, int APrecision, bool AIsLong) : base()
		{
			Type = AType;
			Length = ALength;
			Scale = AScale;
			Precision = APrecision;
			IsLong = AIsLong;
		}
		
		public Type Type;
		public int Length;
		public int Scale;
		public int Precision;
		public bool IsLong;
	}
	
	public class SQLColumn
	{
		public SQLColumn(string AName, SQLDomain ADomain) : base()
		{
			FName = AName;
			FDomain = ADomain;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private SQLDomain FDomain;
		public SQLDomain Domain { get { return FDomain; } }
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
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLIndexColumn
	{
		public SQLIndexColumn(string AName) : base()
		{
			FName = AName;
		}
		
		public SQLIndexColumn(string AName, bool AAscending) : base()
		{
			FName = AName;
			FAscending = AAscending;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private bool FAscending = true;
		public bool Ascending { get { return FAscending; } }
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
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLIndex
	{
		public SQLIndex(string AName) : base()
		{
			FName = AName;
		}
		
		public SQLIndex(string AName, SQLIndexColumn[] AColumns)
		{
			FName = AName;
			FColumns.AddRange(AColumns);
		}
		
		public SQLIndex(string AName, SQLIndexColumns AColumns)
		{
			FName = AName;
			FColumns.AddRange(AColumns);
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private bool FIsUnique = false;
		public bool IsUnique 
		{ 
			get { return FIsUnique; } 
			set { FIsUnique = value; }
		}
		
		private SQLIndexColumns FColumns = new SQLIndexColumns();
		public SQLIndexColumns Columns { get { return FColumns; } }
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
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (AName == this[LIndex].Name)
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
	}
	
	public class SQLTableSchema
	{
		private SQLColumns FColumns = new SQLColumns();
		public SQLColumns Columns { get { return FColumns; } }
		
		private SQLIndexes FIndexes = new SQLIndexes();
		public SQLIndexes Indexes { get { return FIndexes; } }
	}
}
