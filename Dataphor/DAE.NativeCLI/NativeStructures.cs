/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	[Serializable]	
	public class NativeSessionHandle
	{
		public NativeSessionHandle(Guid AID)
		{
			ID = AID;
		}
		
		public Guid ID;
	}
	
	[Serializable]
	public enum NativeModifier { In, Var, Out }
	
	[Serializable]	
	public struct NativeParam
	{
		public string Name;
		public string DataTypeName;
		public NativeModifier Modifier;
		public object Value;
	}
	
	[Serializable]
	public enum NativeExecutionOptions { Default, SchemaOnly }
	
	[Serializable]	
	public struct NativeExecuteOperation
	{
		public string Statement;
		public NativeParam[] Params;
		public NativeExecutionOptions Options;
	}
	
	[Serializable]	
	public abstract class NativeValue { }
	
	[Serializable]	
	public class NativeScalarValue : NativeValue
	{
		public string DataTypeName;
		public object Value;
	}
	
	[Serializable]	
	public class NativeListValue : NativeValue
	{
		public NativeValue[] Elements;
	}
	
	[Serializable]	
	public struct NativeColumn
	{
		public string Name;
		public string DataTypeName;
	}
	
	[Serializable]
	public struct NativeKey
	{
		public string[] KeyColumns;
	}
	
	[Serializable]	
	public class NativeRowValue : NativeValue
	{
		public NativeColumn[] Columns;
		public object[] Values;
	}

	[Serializable]	
	public class NativeTableValue : NativeValue
	{
		public NativeColumn[] Columns;
		public NativeKey[] Keys;
		public object[][] Rows;
	}
	
	[Serializable]
	public class NativeResult
	{
		public NativeParam[] Params;
		public NativeValue Value;
	}
}
