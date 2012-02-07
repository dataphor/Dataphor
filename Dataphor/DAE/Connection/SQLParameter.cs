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
	public class SQLParameter
	{
		public SQLParameter() : base(){}
		public SQLParameter(string name, SQLType type) : base()
		{
			_name = name;
			_type = type;
		}
		
		public SQLParameter(string name, SQLType type, object tempValue) : base()
		{
			_name = name;
			_type = type;
			_value = tempValue;
		}
		
		public SQLParameter(string name, SQLType type, object tempValue, SQLDirection direction) : base()
		{
			_name = name;
			_type = type;
			_value = tempValue;
			_direction = direction;
		}
		
		public SQLParameter(string name, SQLType type, object tempValue, SQLDirection direction, string marker) : base()
		{
			_name = name;
			_type = type;
			_value = tempValue;
			_direction = direction;
			_marker = marker;
		}
		
		public SQLParameter(string name, SQLType type, object tempValue, SQLDirection direction, string marker, string literal) : base()
		{
			_name = name;
			_type = type;
			_value = tempValue;
			_direction = direction;
			_marker = marker;
			_literal = literal;
		}
		
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}	

		private SQLType _type;
		public SQLType Type
		{
			get { return _type; }
			set { _type = value; }
		}

		private SQLDirection _direction;
		public SQLDirection Direction
		{
			get { return _direction; }
			set { _direction = value; }
		}
		
		private object _value;
		public object Value
		{
			get { return _value; }
			set { _value = value; }
		}	
		
		private string _marker;
		public string Marker
		{
			get { return _marker; }
			set { _marker = value; }
		}
		
		private string _literal;
		public string Literal
		{
			get { return _literal; }
			set { _literal = value; }
		}
	}
	
	// Parameters are denoted by @<parameter name> in the Statement set on the command object, these will be replaced as necessary by each connection type
	#if USETYPEDLIST
	public class SQLParameters : TypedList
	{
		public SQLParameters() : base(typeof(SQLParameter)){}
		
		public new SQLParameter this[int AIndex]
		{
			get { return (SQLParameter)base[AIndex]; }
			set { base[AIndex] = value; }
		}

	#else
	public class SQLParameters : BaseList<SQLParameter>
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
}
