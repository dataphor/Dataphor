/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	public abstract class SQLType {}
	
	public class SQLStringType : SQLType
	{
		public SQLStringType() : base(){}
		public SQLStringType(int length) : base()
		{
			Length = length;
		}
		
		public int Length;
	}
	
	public class SQLBooleanType : SQLType {}
	
	public class SQLIntegerType : SQLType
	{	
		public SQLIntegerType() : base(){}
		public SQLIntegerType(byte byteCount) : base()
		{
			ByteCount = byteCount;
		}
		
		public byte ByteCount;
	}
	
	public class SQLNumericType : SQLType
	{
		public SQLNumericType() : base(){}
		public SQLNumericType(byte precision, byte scale) : base()
		{
			Precision = precision;
			Scale = scale;
		}
		
		public byte Precision;
		public byte Scale;
	}
	
	public class SQLFloatType : SQLType
	{
		public SQLFloatType() : base() {}
		public SQLFloatType(byte width) : base()
		{
			Width = width;
		}
		
		/// <summary>
		/// Specifies whether the floating point value is single-precision (1) or double-precision (2).
		/// </summary>
		public byte Width = 1;
	}
	
	public class SQLMoneyType : SQLType {}
	
	public abstract class SQLLongType : SQLType {}
	
	public class SQLTextType : SQLLongType {}
	
	public class SQLBinaryType : SQLLongType {}

	public class SQLByteArrayType : SQLType 
	{
		public SQLByteArrayType() : base(){}
		public SQLByteArrayType(int length) : base()
		{
			Length = length;	
		}
		
		public int Length;
	}
	
	public class SQLDateTimeType : SQLType {}
	
	public class SQLDateType : SQLType {}
	
	public class SQLTimeType : SQLType {}
	
	public class SQLGuidType : SQLType {}
}

