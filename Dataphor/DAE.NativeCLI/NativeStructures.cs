/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	[DataContract]
	public class NativeSessionHandle
	{
		public NativeSessionHandle(Guid iD)
		{
			ID = iD;
		}
		
		[DataMember]
		public Guid ID;
	}
	
	[DataContract]
	public enum NativeIsolationLevel 
	{ 
		[EnumMember]
		Browse, 
		
		[EnumMember]
		CursorStability, 
		
		[EnumMember]
		Isolated 
	};
	
	[DataContract]
	public enum NativeModifier 
	{ 
		[EnumMember]
		In, 
		
		[EnumMember]
		Var, 
		
		[EnumMember]
		Out 
	}
	
	[DataContract]
	public struct NativeParam
	{
		[DataMember]
		public string Name;

		[DataMember]
		public NativeModifier Modifier;

		[DataMember]
		public NativeValue Value;
	}
	
	[DataContract]
	public enum NativeExecutionOptions 
	{ 
		[EnumMember]
		Default, 
		
		[EnumMember]
		SchemaOnly 
	}
	
	[DataContract]
	public struct NativeExecuteOperation
	{
		[DataMember]
		public string Statement;

		[DataMember]
		public NativeParam[] Params;

		[DataMember]
		public NativeExecutionOptions Options;
	}
	
	[DataContract]
	[KnownType(typeof(NativeScalarValue))]
	[KnownType(typeof(NativeListValue))]
	[KnownType(typeof(NativeRowValue))]
	[KnownType(typeof(NativeTableValue))]
	public abstract class NativeValue { }
	
	[DataContract]
	public class NativeScalarValue : NativeValue
	{
		[DataMember]
		public string DataTypeName;

		[DataMember]
		public object Value;
	}
	
	[DataContract]
	public class NativeListValue : NativeValue
	{
		[DataMember]
		public string ElementDataTypeName;

		[DataMember]
		public NativeValue[] Elements;
	}
	
	[DataContract]
	public struct NativeColumn
	{
		[DataMember]
		public string Name;

		[DataMember]
		public string DataTypeName;
	}
	
	[DataContract]
	public struct NativeKey
	{
		[DataMember]
		public string[] KeyColumns;
	}
	
	[DataContract]
	public class NativeRowValue : NativeValue
	{
		[DataMember]
		public NativeColumn[] Columns;

		[DataMember]
		public NativeValue[] Values;
	}

	[DataContract]
	public class NativeTableValue : NativeValue
	{
		[DataMember]
		public NativeColumn[] Columns;

		[DataMember]
		public NativeKey[] Keys;

		[DataMember]
		public object[][] Rows;
	}
	
	[DataContract]
	public class NativeResult
	{
		[DataMember]
		public NativeParam[] Params;

		[DataMember]
		public NativeValue Value;
	}
}
