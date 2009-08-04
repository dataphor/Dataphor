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
		public SQLParameter(string AName, SQLType AType) : base()
		{
			FName = AName;
			FType = AType;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection, string AMarker) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
			FMarker = AMarker;
		}
		
		public SQLParameter(string AName, SQLType AType, object AValue, SQLDirection ADirection, string AMarker, string ALiteral) : base()
		{
			FName = AName;
			FType = AType;
			FValue = AValue;
			FDirection = ADirection;
			FMarker = AMarker;
			FLiteral = ALiteral;
		}
		
		private string FName;
		public string Name
		{
			get { return FName; }
			set { FName = value; }
		}	

		private SQLType FType;
		public SQLType Type
		{
			get { return FType; }
			set { FType = value; }
		}

		private SQLDirection FDirection;
		public SQLDirection Direction
		{
			get { return FDirection; }
			set { FDirection = value; }
		}
		
		private object FValue;
		public object Value
		{
			get { return FValue; }
			set { FValue = value; }
		}	
		
		private string FMarker;
		public string Marker
		{
			get { return FMarker; }
			set { FMarker = value; }
		}
		
		private string FLiteral;
		public string Literal
		{
			get { return FLiteral; }
			set { FLiteral = value; }
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
}
