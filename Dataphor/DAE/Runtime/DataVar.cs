/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime
{
	using System;
	using System.Collections;
	
	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	
	/// <remarks> DataVar </remarks>
    public class DataVar : System.Object, ICloneable
    {
		public DataVar(string AName, Schema.IDataType ADataType) : base()
		{
			Name = AName;
			DataType = ADataType;
		}
		
		public DataVar(string AName, Schema.IDataType ADataType, bool AIsConstant) : base()
		{
			Name = AName;
			DataType = ADataType;
			IsConstant = AIsConstant;
		}
		
		public DataVar(string AName, Schema.IDataType ADataType, DataValue AValue) : base()
		{
			Name = AName;
			DataType = ADataType;
			Value = AValue;
		}
		
		public DataVar(Schema.IDataType ADataType) : base()
		{
			Name = String.Empty;
			DataType = ADataType;
		}
		
		public DataVar(Schema.IDataType ADataType, DataValue AValue) : base()
		{
			Name = String.Empty;
			DataType = ADataType;
			Value = AValue;
		}
		
		public string Name;
		public Schema.IDataType DataType;
		public DataValue Value;
		public bool IsConstant;
		public bool IsModified;

		// Clone
		public virtual object Clone()
		{	
			return new DataVar(Name, DataType, Value == null ? null : Value.Copy());
		}
		
        // ToString
        public override string ToString()
        {
			return (Name == null) ? DataType.Name : Name;
        }
    }
    
    public class DataParam : DataVar
    {
		public DataParam(string AName, Schema.IDataType ADataType, Modifier AModifier, DataValue AValue) : base(AName, ADataType, AValue)
		{
			Modifier = AModifier;
		}
		
		public DataParam(string AName, Schema.IDataType ADataType, Modifier AModifier) : base(AName, ADataType)
		{
			Modifier = AModifier;
		}
		
		public Modifier Modifier;

		public static DataParam Create(IServerProcess AProcess, string AName, string AValue)
		{
			return
				new DataParam
				(
					AName, 
					AProcess.DataTypes.SystemString, 
					DAE.Language.Modifier.In, 
					new Scalar(AProcess, AProcess.DataTypes.SystemString, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemGuid, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemInteger, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemByte, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemDateTime, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemDecimal, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemLong, AValue)
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
					new Scalar(AProcess, AProcess.DataTypes.SystemBoolean, AValue)
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
					new Scalar(AProcess, ADataType, AValue)
				);
		}
	}
    
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
				return (DataParam)RemoveItemAt(0);
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


