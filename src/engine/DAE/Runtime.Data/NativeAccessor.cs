/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public class NativeAccessor
	{
		public NativeAccessor(string name, Type nativeType) : base()
		{
			_name = name;
			_nativeType = nativeType;
		}
		
		private string _name;
		public string Name { get { return _name; } }
		
		private Type _nativeType;
		public Type NativeType { get { return _nativeType; } }
	}
	
	public class NativeAccessors
	{
		public static NativeAccessor AsBoolean = new NativeAccessor("AsBoolean", typeof(bool));
		public static NativeAccessor AsByte = new NativeAccessor("AsByte", typeof(byte));
		public static NativeAccessor AsInt16 = new NativeAccessor("AsInt16", typeof(short));
		public static NativeAccessor AsInt32 = new NativeAccessor("AsInt32", typeof(int));
		public static NativeAccessor AsInt64 = new NativeAccessor("AsInt64", typeof(long));
		public static NativeAccessor AsDecimal = new NativeAccessor("AsDecimal", typeof(decimal));
		public static NativeAccessor AsTimeSpan = new NativeAccessor("AsTimeSpan", typeof(TimeSpan));
		public static NativeAccessor AsDateTime = new NativeAccessor("AsDateTime", typeof(DateTime));
		public static NativeAccessor AsGuid = new NativeAccessor("AsGuid", typeof(Guid));
		public static NativeAccessor AsString = new NativeAccessor("AsString", typeof(string));
		public static NativeAccessor AsDisplayString = new NativeAccessor("AsDisplayString", typeof(string));
		public static NativeAccessor AsException = new NativeAccessor("AsException", typeof(Exception));
		public static NativeAccessor AsByteArray = new NativeAccessor("AsByteArray", typeof(byte[]));
	}
}
