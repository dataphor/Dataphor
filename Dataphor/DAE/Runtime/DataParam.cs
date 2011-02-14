/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime
{
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;

    public class DataParam : object
    {
		public DataParam(string name, Schema.IDataType dataType, Modifier modifier, object tempValue)
		{
			Name = name;
			DataType = dataType;
			Value = tempValue;
			Modifier = modifier;
		}
		
		public DataParam(string name, Schema.IDataType dataType, Modifier modifier)
		{
			Name = name;
			DataType = dataType;
			Modifier = modifier;
		}
		
		public string Name;
		public Schema.IDataType DataType;
		public object Value;
		public Modifier Modifier;

		public static DataParam Create(IServerProcess process, string name, string tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemString, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, Guid tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemGuid, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, int tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemInteger, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, byte tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemByte, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, DateTime tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemDateTime, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, decimal tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemDecimal, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, long tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemLong, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, bool tempValue)
		{
			return
				new DataParam
				(
					name, 
					process.DataTypes.SystemBoolean, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}

		public static DataParam Create(IServerProcess process, string name, object tempValue, DAE.Schema.IScalarType dataType)
		{
			return
				new DataParam
				(
					name, 
					dataType, 
					DAE.Language.Modifier.In, 
					tempValue
				);
		}
	}

	#if USETYPEDLIST
    public class DataParams : TypedList
    {
		public DataParams() : base()
		{
			FItemType = typeof(DataParam);
		}

		public new DataParam this[int AIndex]
		{
			get { return (DataParam)(base[AIndex]); }
			set { base[AIndex] = value; }
		}
	
	#else
	public class DataParams : BaseList<DataParam>
	{
	#endif	
		public DataParam this[string index]
		{
			get
			{
				int localIndex = IndexOf(index);
				if (localIndex >= 0)
					return this[localIndex];
				else
					throw new RuntimeException(RuntimeException.Codes.DataParamNotFound, index);
			}
			set
			{
				int localIndex = IndexOf(index);
				if (localIndex >= 0)
					this[localIndex] = value;
				else
					throw new RuntimeException(RuntimeException.Codes.DataParamNotFound, index);
			}
		}
		
		public int IndexOf(string name)
		{
			for (int index = 0; index < Count; index++)
				if (Schema.Object.NamesEqual(this[index].Name, name))
					return index;
			return -1;
		}
		
		public bool Contains(string name)
		{
			return IndexOf(name) >= 0;
		}
		
		public void Push(DataParam objectValue)
		{
			Insert(0, objectValue);
		}
		
		public DataParam Pop()
		{
			if (Count == 0)
				throw new RuntimeException(RuntimeException.Codes.ParamsEmpty);
			else
				#if USETYPEDLIST
				return (DataParam)RemoveItemAt(0);
				#else
				return RemoveAt(0);
				#endif
		}
		
		public DataParam Peek()
		{
			if (Count == 0)
				throw new RuntimeException(RuntimeException.Codes.ParamsEmpty);
			else
				return this[0];
		}
    }
}


