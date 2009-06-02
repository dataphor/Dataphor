/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{	
	using System;
	using System.IO;
	using System.Collections;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Server;
	using Schema = Alphora.Dataphor.DAE.Schema;
	
	/*
		Value Representation in the DAE ->

			host representation is a DataValue descendent which is actively being processed in the DAE.
			native representation is a .NET representation of the value which has no tie to a given process.
			physical representation is an array of bytes used to transfer the value into and out of the DAE.

			Host representations carry the data type of the associated value.  This is required because we
			support generics.  If we did not support generics (in lists and rows) we could dispense with
			this representation and have the calling convention pass only the native representation, relying
			on the declared type of the variable to specify the type for all values in the container.  With
			generics, the type of the value will not be known until run-time so the host representation must
			specify the value.  When storing contained values in native representation, the data type must
			also be specified.  Physical representations must always specify the data type of the stored value.

			scalar values ->
				host -> Scalar
				native -> object
				physical -> [Data type name][physical representation of value]

				Deferred access to values is accomplished by leaving the value in its physical representation
					in a stream until it is asked for.  Some deferred access will materialize the entire
					value in its native representation (e.g. String), others will only be accessed via stream (e.g. Binary)

			row values ->
				host -> Row
				native -> NativeRow { DataType[], object[] }
				physical -> [Data type name][Values header][physical representation of values]

			table values ->
				host -> TableValue
				native -> RowSet (set of RowTree or RowHashtable) (RowTree BTree of NativeRows) (RowHashtable Hashtable of NativeRows)
				physical -> [Data type name][Row count][physical representation of rows] // not for disk storage!!!

			list values ->
				host -> ListValue
				native -> NativeList { DataType[], object[] }
				physical -> [Data type name][Element count][physical representation of elements]

			cursor values ->
				host -> CursorValue
				native -> Int32 (cursor handle)
				physical -> [Data type name][Int32]

			interval values -> ??? (this will probably have to be deferred to the next version because it requires language changes)
				host -> IntervalValue
				native -> object, object
				physical -> [Data type name][physical representation of endpoints]

			operator values -> ??? what about object values, type values, etc.,. (this should be part of a reflection support pass)
				host -> OperatorValue
				native -> String (operator name)
				physical -> [Data type name][Operator Name]
	*/
	
	public class NativeAccessor
	{
		public NativeAccessor(string AName, Type ANativeType) : base()
		{
			FName = AName;
			FNativeType = ANativeType;
		}
		
		private string FName;
		public string Name { get { return FName; } }
		
		private Type FNativeType;
		public Type NativeType { get { return FNativeType; } }
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
	
	/// <remarks>
	/// Base class for all host representations in the DAE.
	/// All values have a data type and are associated with some process in the system.
	/// The host representation is an active wrapper for the native representation of some value.
	/// </remarks>
	public abstract class DataValue : System.Object, IDisposable //, IDisposableNotify
	{
		public DataValue(IServerProcess AProcess, Schema.IDataType ADataType) : base()
		{
			FProcess = AProcess;
			FDataType = ADataType;
		}
		
		protected IServerProcess FProcess;
		public IServerProcess Process { get { return FProcess; } }
		
		protected Schema.IDataType FDataType;
		public Schema.IDataType DataType { get	{ return FDataType;	} }
		
		#if USEFINALIZER
		~DataValue()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
		
		public void Dispose()
		{
			if (FProcess != null)
			{
				#if USEFINALIZER
				System.GC.SuppressFinalize(this);
				#endif
				Dispose(true);
				FProcess = null;
				FDataType = null;
			}
		}
		
		protected virtual void Dispose(bool ADisposing) { }

		/// <summary>Indicates whether disposal of the value should deallocate any resources associated with the value.</summary>
		public bool ValuesOwned = true;

		/// <summary>Gets or sets this value as a native boolean using the boolean representation of the type, if avaiable.</summary>		
		public virtual bool AsBoolean 
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemBoolean); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemBoolean, DataType.Name); } 
		}

		/// <summary>Gets this value as a native boolean using the given representation of the type.</summary>		
		public virtual bool GetAsBoolean(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemBoolean);
		}

		/// <summary>Sets this value as a native boolean using the given representation of the type.</summary>		
		public virtual void SetAsBoolean(string ARepresentationName, bool AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemBoolean, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native byte using the byte representation of the type, if avaiable.</summary>		
		public virtual byte AsByte
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemByte); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemByte, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native byte using the given representation of the type.</summary>		
		public virtual byte GetAsByte(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemByte);
		}

		/// <summary>Sets this value as a native byte using the given representation of the type.</summary>		
		public virtual void SetAsByte(string ARepresentationName, byte AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemByte, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native int16 using the int16 representation of the type, if avaiable.</summary>		
		public virtual short AsInt16
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemShort); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemShort, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native short using the given representation of the type.</summary>		
		public virtual short GetAsInt16(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemShort);
		}

		/// <summary>Sets this value as a native short using the given representation of the type.</summary>		
		public virtual void SetAsInt16(string ARepresentationName, short AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemShort, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native int32 using the int32 representation of the type, if avaiable.</summary>		
		public virtual int AsInt32
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemInteger); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemInteger, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native integer using the given representation of the type.</summary>		
		public virtual int GetAsInt32(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemInteger);
		}

		/// <summary>Sets this value as a native integer using the given representation of the type.</summary>		
		public virtual void SetAsInt32(string ARepresentationName, int AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemInteger, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native int64 using the int64 representation of the type, if avaiable.</summary>		
		public virtual long AsInt64
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemLong); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemLong, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native long using the given representation of the type.</summary>		
		public virtual long GetAsInt64(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemLong);
		}

		/// <summary>Sets this value as a native long using the given representation of the type.</summary>		
		public virtual void SetAsInt64(string ARepresentationName, long AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemLong, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native decimal using the decimal representation of the type, if avaiable.</summary>		
		public virtual Decimal AsDecimal
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemDecimal); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemDecimal, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native decimal using the given representation of the type.</summary>		
		public virtual Decimal GetAsDecimal(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemDecimal);
		}

		/// <summary>Sets this value as a native decimal using the given representation of the type.</summary>		
		public virtual void SetAsDecimal(string ARepresentationName, Decimal AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemDecimal, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native timespan using the timespan representation of the type, if avaiable.</summary>		
		public virtual TimeSpan AsTimeSpan
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemTimeSpan); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemTimeSpan, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native timespan using the given representation of the type.</summary>		
		public virtual TimeSpan GetAsTimeSpan(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemTimeSpan);
		}

		/// <summary>Sets this value as a native timespan using the given representation of the type.</summary>		
		public virtual void SetAsTimeSpan(string ARepresentationName, TimeSpan AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemTimeSpan, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native datetime using the datetime representation of the type, if avaiable.</summary>		
		public virtual DateTime AsDateTime
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemDateTime); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemDateTime, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native datetime using the given representation of the type.</summary>		
		public virtual DateTime GetAsDateTime(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemDateTime);
		}

		/// <summary>Sets this value as a native datetime using the given representation of the type.</summary>		
		public virtual void SetAsDateTime(string ARepresentationName, DateTime AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemDateTime, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native guid using the guid representation of the type, if avaiable.</summary>		
		public virtual Guid AsGuid
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemGuid); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemGuid, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native guid using the given representation of the type.</summary>		
		public virtual Guid GetAsGuid(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemGuid);
		}

		/// <summary>Sets this value as a native guid using the given representation of the type.</summary>		
		public virtual void SetAsGuid(string ARepresentationName, Guid AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemGuid, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native string using the string representation of the type, if avaiable.</summary>		
		public virtual string AsString
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemString); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemString, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native string using the given representation of the type.</summary>		
		public virtual string GetAsString(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemString);
		}

		/// <summary>Sets this value as a native string using the given representation of the type.</summary>		
		public virtual void SetAsString(string ARepresentationName, string AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemString, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native string using the display string representation of the type, if avaiable.</summary>		
		public virtual string AsDisplayString
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemString); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemString, DataType.Name); } 
		}
		
		/// <summary>Gets or sets this value as a native byte array using the binary representation of the type, if avaiable.</summary>		
		public virtual byte[] AsByteArray
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemBinary); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemBinary, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native byte[] using the given representation of the type.</summary>		
		public virtual byte[] GetAsByteArray(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemBinary);
		}

		/// <summary>Sets this value as a native byte[] using the given representation of the type.</summary>		
		public virtual void SetAsByteArray(string ARepresentationName, byte[] AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemBinary, DataType.Name);
		}
		
		/// <summary>Gets or sets this value as a native string using the base 64 string representation of the type, if avaiable.</summary>		
		public virtual string AsBase64String
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemBinary); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemBinary, DataType.Name); } 
		}
		
		/// <summary>Gets or sets this value as a native exception using the exception representation of the type, if avaiable.</summary>		
		public virtual Exception AsException
		{ 
			get { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemError); } 
			set { throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemError, DataType.Name); } 
		}
		
		/// <summary>Gets this value as a native exception using the given representation of the type.</summary>		
		public virtual Exception GetAsException(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, DataType.Name, Schema.DataTypes.CSystemError);
		}

		/// <summary>Sets this value as a native exception using the given representation of the type.</summary>		
		public virtual void SetAsException(string ARepresentationName, Exception AValue)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToConvertValue, Schema.DataTypes.CSystemError, DataType.Name);
		}
		
		/// <summary>Indicates whether or not this value is initialized.</summary>
		public abstract bool IsNil { get; }

		/// <summary>Indicates whether this value is stored in its native representation.</summary>		
		public virtual bool IsNative { get { return false; } }
		
		/// <summary>Gets or sets this value in its native representation.</summary>		
		public abstract object AsNative { get; set; }

		/// <summary>Gets or sets this value in its physical representation.</summary>
		public byte[] AsPhysical 
		{ 
			get
			{
				if (IsPhysicalStreaming)
				{
					MemoryStream LStream = new MemoryStream(64);
					WriteToPhysical(LStream, false);
					return LStream.GetBuffer();
				}
				else
				{
					byte[] LValue = new byte[GetPhysicalSize(false)];
					WriteToPhysical(LValue, 0, false);
					return LValue;
				}
			} 
			set
			{
				if (IsPhysicalStreaming)
				{
					MemoryStream LStream = new MemoryStream(value, 0, value.Length, false, true);
					ReadFromPhysical(LStream);
				}
				else
					ReadFromPhysical(value, 0);
			}
		}

		/// <summary>Indicates whether or not the conveyor for values of this type uses streams to read/write.  If this is false, the conveyor will use byte arrays.</summary>
		public virtual bool IsPhysicalStreaming { get { return false; } }
		
		/// <summary>Returns the number of bytes required to store the physical representation of this value.</summary>
		public virtual int GetPhysicalSize(bool AExpandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of this value into the byte array given in ABuffer, beginning at the offset given by AOffset.</summary>		
		public virtual void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of this value into the stream given in AStream.</summary>
		public virtual void WriteToPhysical(Stream AStream, bool AExpandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Sets the native representation of this value by reading the physical representation from the byte array given in ABuffer, beginning at the offset given by AOffset.</summary>
		public virtual void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Sets the native representation of this value by reading the physical representation from the stream given in AStream.</summary>
		public virtual void ReadFromPhysical(Stream AStream)
		{
			throw new NotSupportedException();
		}
		
		public virtual Stream OpenStream()
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		public virtual Stream OpenStream(string ARepresentationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		public virtual Table OpenCursor()
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideCursorAccess, DataType.Name);
		}
		
		/// <summary>Copies the native representation of this value and returns the host representation as the given type.</summary>
		public DataValue CopyAs(Schema.IDataType ADataType)
		{
			// This code is duplicated in the Copy and FromNative methods for performance...
			object LValue = CopyNativeAs(ADataType);
			Schema.IScalarType LScalarType = ADataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (LValue is StreamID)
				{
					Scalar LScalar = new Scalar(Process, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(Process, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(Process, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(Process, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(Process, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(Process, LCursorType, (int)LValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}
		
		public DataValue Copy()
		{
			// This code is duplicated in the FromNative and CopyAs methods for performance...
			object LValue = CopyNative();
			Schema.IScalarType LScalarType = DataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (LValue is StreamID)
				{
					Scalar LScalar = new Scalar(Process, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(Process, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = DataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(Process, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = DataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(Process, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = DataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(Process, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = DataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(Process, LCursorType, (int)LValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public DataValue Copy(IServerProcess AProcess)
		{
			// This code is duplicated in the FromNative and CopyAs methods for performance...
			object LValue = CopyNative();
			Schema.IScalarType LScalarType = DataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (LValue is StreamID)
				{
					Scalar LScalar = new Scalar(AProcess, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(AProcess, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = DataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(AProcess, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = DataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(AProcess, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = DataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(AProcess, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = DataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AProcess, LCursorType, (int)LValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public abstract object CopyNativeAs(Schema.IDataType ADataType);
		
		public object CopyNative()
		{
			return CopyNativeAs(DataType);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNative(IServerProcess AProcess, Schema.IDataType ADataType, object AValue)
		{
			if (AValue == null)
				return null;
				
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IScalarType LScalarType = ADataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					return new Scalar(AProcess, LScalarType, (StreamID)AValue);
				return new Scalar(AProcess, LScalarType, AValue);
			}
				
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
				return new Row(AProcess, LRowType, (NativeRow)AValue);
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
				return new ListValue(AProcess, LListType, (NativeList)AValue);
				
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AProcess, (NativeTable)AValue);
				
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AProcess, LCursorType, (int)AValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNativeRow(IServerProcess AProcess, Schema.IRowType ARowType, NativeRow ANativeRow, int ANativeRowIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IDataType LDataType = ANativeRow.DataTypes[ANativeRowIndex];
			if (LDataType == null)
				LDataType = ARowType.Columns[ANativeRowIndex].DataType;

			Schema.IScalarType LScalarType = LDataType as Schema.IScalarType;
			if (LScalarType != null)
				return new RowInternedScalar(AProcess, LScalarType, ANativeRow, ANativeRowIndex);
				
			Schema.IRowType LRowType = LDataType as Schema.IRowType;
			if (LRowType != null)
				return new Row(AProcess, LRowType, (NativeRow)ANativeRow.Values[ANativeRowIndex]);
			
			Schema.IListType LListType = LDataType as Schema.IListType;
			if (LListType != null)
				return new ListValue(AProcess, LListType, (NativeList)ANativeRow.Values[ANativeRowIndex]);
				
			Schema.ITableType LTableType = LDataType as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AProcess, (NativeTable)ANativeRow.Values[ANativeRowIndex]);
				
			Schema.ICursorType LCursorType = LDataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AProcess, LCursorType, (int)ANativeRow.Values[ANativeRowIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, LDataType == null ? "<null>" : LDataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNativeList(IServerProcess AProcess, Schema.IListType AListType, NativeList ANativeList, int ANativeListIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IDataType LDataType = ANativeList.DataTypes[ANativeListIndex];
			if (LDataType == null)
				LDataType = AListType.ElementType;

			Schema.IScalarType LScalarType = ANativeList.DataTypes[ANativeListIndex] as Schema.IScalarType;
			if (LScalarType != null)
				return new ListInternedScalar(AProcess, LScalarType, ANativeList, ANativeListIndex);
				
			Schema.IRowType LRowType = ANativeList.DataTypes[ANativeListIndex] as Schema.IRowType;
			if (LRowType != null)
				return new Row(AProcess, LRowType, (NativeRow)ANativeList.Values[ANativeListIndex]);
			
			Schema.IListType LListType = ANativeList.DataTypes[ANativeListIndex] as Schema.IListType;
			if (LListType != null)
				return new ListValue(AProcess, LListType, (NativeList)ANativeList.Values[ANativeListIndex]);
				
			Schema.ITableType LTableType = ANativeList.DataTypes[ANativeListIndex] as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AProcess, (NativeTable)ANativeList.Values[ANativeListIndex]);
				
			Schema.ICursorType LCursorType = ANativeList.DataTypes[ANativeListIndex] as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AProcess, LCursorType, (int)ANativeList.Values[ANativeListIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ANativeList.DataTypes[ANativeListIndex] == null ? "<null>" : ANativeList.DataTypes[ANativeListIndex].GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given physical value.</summary>
		public static DataValue FromPhysical(IServerProcess AProcess, Schema.IDataType ADataType, byte[] ABuffer, int AOffset)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				Scalar LScalar = new Scalar(AProcess, LScalarType, null);
				LScalar.ReadFromPhysical(ABuffer, AOffset);
				return LScalar;
			}
			
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(AProcess, LRowType);
				LRow.ReadFromPhysical(ABuffer, AOffset);
				return LRow;
			}
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(AProcess, LListType);
				LList.ReadFromPhysical(ABuffer, AOffset);
				return LList;
			}
			
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(AProcess, null);
				LTable.ReadFromPhysical(ABuffer, AOffset);
				return LTable;
			}
			
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
			{
				CursorValue LCursor = new CursorValue(AProcess, LCursorType, -1);
				LCursor.ReadFromPhysical(ABuffer, AOffset);
				return LCursor;
			}

			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}

		public static object CopyNative(IServerProcess AProcess, Schema.IDataType ADataType, object AValue)
		{
			// This code is duplicated in the descendent CopyNative methods for performance
			if (AValue == null)
				return AValue;
				
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					return AProcess.Reference((StreamID)AValue);
				
				ICloneable LCloneable = AValue as ICloneable;
				if (LCloneable != null)
					return LCloneable.Clone();
					
				if (LScalarType.IsCompound)
					return CopyNative(AProcess, LScalarType.CompoundRowType, AValue);
					
				return AValue;
			}
			
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				NativeRow LNativeRow = (NativeRow)AValue;
				NativeRow LNewRow = new NativeRow(LRowType.Columns.Count);
				for (int LIndex = 0; LIndex < LRowType.Columns.Count; LIndex++)
				{
					LNewRow.DataTypes[LIndex] = LNativeRow.DataTypes[LIndex];
					LNewRow.Values[LIndex] = CopyNative(AProcess, LNativeRow.DataTypes[LIndex], LNativeRow.Values[LIndex]);
				}
				return LNewRow;
			}
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
			{
				NativeList LNativeList = (NativeList)AValue;
				NativeList LNewList = new NativeList();
				for (int LIndex = 0; LIndex < LNativeList.DataTypes.Count; LIndex++)
				{
					LNewList.DataTypes.Add(LNativeList.DataTypes[LIndex]);
					LNewList.Values.Add(CopyNative(AProcess, LNativeList.DataTypes[LIndex], LNativeList.Values[LIndex]));
				}
				return LNewList;
			}
			
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				ServerProcess LServerProcess = AProcess.GetServerProcess();
				NativeTable LNativeTable = (NativeTable)AValue;
				NativeTable LNewTable = new NativeTable(AProcess.GetServerProcess(), LNativeTable.TableVar);
				using (Scan LScan = new Scan(LServerProcess, LNativeTable, LNativeTable.ClusteredIndex, ScanDirection.Forward, null, null))
				{
					while (!LScan.EOF())
					{
						using (Row LRow = LScan.GetRow())
						{
							LNewTable.Insert(LServerProcess, LRow);
						}
					}
				}
				return LNewTable;
			}
			
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
				return AValue;
			
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}

		/// <summary>Disposes the given native value.</summary>		
		public static void DisposeNative(IServerProcess AProcess, Schema.IDataType ADataType, object AValue)
		{
			if (AValue == null)
				return;

			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					AProcess.Deallocate((StreamID)AValue);
					
				if (LScalarType.IsCompound)
					DisposeNative(AProcess, LScalarType.CompoundRowType, AValue);

				return;
			}
			
			using (DataValue LDataValue = DataValue.FromNative(AProcess, ADataType, AValue))
			{
				LDataValue.ValuesOwned = true;
			}
		}
		
		public new bool ToString()
		{
			Error.Fail("ToString will not work on a DataValue, use the AsString accessor instead.");
			return false;
		}
	}
}

