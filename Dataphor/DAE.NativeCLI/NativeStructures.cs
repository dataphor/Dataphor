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
	public enum NativeIsolationLevel { Browse, CursorStability, Isolated };
	
	[DataContract]
	public enum NativeModifier { In, Var, Out }
	
	[DataContract]
	public struct NativeParam
	{
		public string Name;
		public string DataTypeName;
		public NativeModifier Modifier;
		public object Value;
	}
	
	[DataContract]
	public enum NativeExecutionOptions { Default, SchemaOnly }
	
	[DataContract]
	public struct NativeExecuteOperation
	{
		public string Statement;
		public NativeParam[] Params;
		public NativeExecutionOptions Options;
	}
	
	[DataContract]
	public abstract class NativeValue { }
	
	[DataContract]
	public class NativeScalarValue : NativeValue
	{
		public string DataTypeName;
		public object Value;
	}
	
	[DataContract]
	public class NativeListValue : NativeValue
	{
		public NativeValue[] Elements;
	}
	
	[DataContract]
	public struct NativeColumn
	{
		public string Name;
		public string DataTypeName;
	}
	
	[DataContract]
	public struct NativeKey
	{
		public string[] KeyColumns;
	}
	
	[DataContract]
	public class NativeRowValue : NativeValue
	{
		public NativeColumn[] Columns;
		public object[] Values;
	}

	[DataContract]
	public class NativeTableValue : NativeValue
	{
		public NativeColumn[] Columns;
		public NativeKey[] Keys;
		public object[][] Rows;
	}
	
	[DataContract]
	public class NativeResult
	{
		public NativeParam[] Params;
		public NativeValue Value;
	}
}
