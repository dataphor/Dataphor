/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime
{
    public class DataParam : object
    {
		public DataParam(string AName, Schema.IDataType ADataType, Modifier AModifier, object AValue)
		{
			Name = AName;
			DataType = ADataType;
			Value = AValue;
			Modifier = AModifier;
		}
		
		public DataParam(string AName, Schema.IDataType ADataType, Modifier AModifier)
		{
			Name = AName;
			DataType = ADataType;
			Modifier = AModifier;
		}
		
		public string Name;
		public Schema.IDataType DataType;
		public object Value;
		public Modifier Modifier;

		public static DataParam Create(IServerProcess AProcess, string AName, string AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemString, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, Guid AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemGuid, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, int AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemInteger, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, byte AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemByte, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, DateTime AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemDateTime, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, decimal AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemDecimal, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, long AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemLong, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, bool AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemBoolean, 
					DAE.Language.Modifier.In, 
					AValue
				);
		}

		public static DataParam Create(IServerProcess AProcess, string AName, object AValue, DAE.Schema.IScalarType ADataType)
		{
			return
				new DataParam
				(
					AName, 
					ADataType, 
					DAE.Language.Modifier.In, 
					AValue
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
		public DataParam this[string AIndex]
		{
			get
			{
				int LIndex = IndexOf(AIndex);
				if (LIndex >= 0)
					return this[LIndex];
				else
					throw new RuntimeException(RuntimeException.Codes.DataParamNotFound, AIndex);
			}
			set
			{
				int LIndex = IndexOf(AIndex);
				if (LIndex >= 0)
					this[LIndex] = value;
				else
					throw new RuntimeException(RuntimeException.Codes.DataParamNotFound, AIndex);
			}
		}
		
		public int IndexOf(string AName)
		{
			for (int LIndex = 0; LIndex < Count; LIndex++)
				if (Schema.Object.NamesEqual(this[LIndex].Name, AName))
					return LIndex;
			return -1;
		}
		
		public bool Contains(string AName)
		{
			return IndexOf(AName) >= 0;
		}
		
		public void Push(DataParam AObject)
		{
			Insert(0, AObject);
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


