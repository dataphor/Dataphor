/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USEDATATYPESINNATIVEROW

using System;
using System.IO;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{	
	using Alphora.Dataphor.DAE.Streams;
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
	
	/// <remarks>
	/// Base class for all host representations in the DAE.
	/// All values have a data type and are associated with some process in the system.
	/// The host representation is an active wrapper for the native representation of some value.
	/// </remarks>
	public abstract class DataValue : System.Object, IDisposable //, IDisposableNotify
	{
		public DataValue(IValueManager AManager, Schema.IDataType ADataType) : base()
		{
			FManager = AManager;
			FDataType = ADataType;
		}
		
		protected IValueManager FManager;
		public IValueManager Manager { get { return FManager; } }
		
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
			if (FManager != null)
			{
				#if USEFINALIZER
				System.GC.SuppressFinalize(this);
				#endif
				Dispose(true);
				FManager = null;
				FDataType = null;
			}
		}
		
		protected virtual void Dispose(bool ADisposing) { }

		/// <summary>Indicates whether disposal of the value should deallocate any resources associated with the value.</summary>
		public bool ValuesOwned = true;

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
					Scalar LScalar = new Scalar(Manager, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(Manager, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(Manager, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(Manager, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(Manager, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(Manager, LCursorType, (int)LValue);
				
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
					Scalar LScalar = new Scalar(Manager, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(Manager, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = DataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(Manager, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = DataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(Manager, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = DataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(Manager, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = DataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(Manager, LCursorType, (int)LValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public object Copy(IValueManager AManager)
		{
			// This code is duplicated in the FromNative and CopyAs methods for performance...
			object LValue = CopyNative();

			Schema.IScalarType LScalarType = DataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (LValue is StreamID)
				{
					Scalar LScalar = new Scalar(AManager, LScalarType, (StreamID)LValue);
					LScalar.ValuesOwned = true;
					return LScalar;
				}
				return new Scalar(AManager, LScalarType, LValue);
			}
				
			Schema.IRowType LRowType = DataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(AManager, LRowType, (NativeRow)LValue);
				LRow.ValuesOwned = true;
				return LRow;
			}
			
			Schema.IListType LListType = DataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(AManager, LListType, (NativeList)LValue);
				LList.ValuesOwned = true;
				return LList;
			}
				
			Schema.ITableType LTableType = DataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(AManager, (NativeTable)LValue);
				LTable.ValuesOwned = true;
				return LTable;
			}
				
			Schema.ICursorType LCursorType = DataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AManager, LCursorType, (int)LValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public abstract object CopyNativeAs(Schema.IDataType ADataType);
		
		public object CopyNative()
		{
			return CopyNativeAs(DataType);
		}
		
		public static Schema.IScalarType NativeTypeToScalarType(IValueManager AManager, Type AType)
		{
			if (AType == NativeAccessors.AsBoolean.NativeType) return AManager.DataTypes.SystemBoolean;
			if (AType == NativeAccessors.AsByte.NativeType) return AManager.DataTypes.SystemByte;
			if (AType == NativeAccessors.AsByteArray.NativeType) return AManager.DataTypes.SystemBinary;
			if (AType == NativeAccessors.AsDateTime.NativeType) return AManager.DataTypes.SystemDateTime;
			if (AType == NativeAccessors.AsDecimal.NativeType) return AManager.DataTypes.SystemDecimal;
			if (AType == NativeAccessors.AsException.NativeType) return AManager.DataTypes.SystemError;
			if (AType == NativeAccessors.AsGuid.NativeType) return AManager.DataTypes.SystemGuid;
			if (AType == NativeAccessors.AsInt16.NativeType) return AManager.DataTypes.SystemShort;
			if (AType == NativeAccessors.AsInt32.NativeType) return AManager.DataTypes.SystemInteger;
			if (AType == NativeAccessors.AsInt64.NativeType) return AManager.DataTypes.SystemLong;
			if (AType == NativeAccessors.AsString.NativeType) return AManager.DataTypes.SystemString;
			if (AType == NativeAccessors.AsTimeSpan.NativeType) return AManager.DataTypes.SystemTimeSpan;
			return AManager.DataTypes.SystemScalar;
		}
		
		public static DataValue FromNative(IValueManager AManager, object AValue)
		{
			if (AValue == null)
				return new Scalar(AManager, AManager.DataTypes.SystemScalar, null);
				
			if (AValue is StreamID)
				return new Scalar(AManager, AManager.DataTypes.SystemScalar, (StreamID)AValue);
				
			return new Scalar(AManager, NativeTypeToScalarType(AManager, AValue.GetType()), AValue);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNative(IValueManager AManager, Schema.IDataType ADataType, object AValue)
		{
			if (AValue == null)
				return null;
				
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IScalarType LScalarType = ADataType as Schema.IScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					return new Scalar(AManager, LScalarType, (StreamID)AValue);
				if (LScalarType.IsGeneric)
					return new Scalar(AManager, NativeTypeToScalarType(AManager, AValue.GetType()), AValue);
				return new Scalar(AManager, LScalarType, AValue);
			}
				
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
				return new Row(AManager, LRowType, (NativeRow)AValue);
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
				return new ListValue(AManager, LListType, (NativeList)AValue);
				
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AManager, (NativeTable)AValue);
				
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AManager, LCursorType, (int)AValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNativeRow(IValueManager AManager, Schema.IRowType ARowType, NativeRow ANativeRow, int ANativeRowIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			#if USEDATATYPESINNATIVEROW
			Schema.IDataType LDataType = ANativeRow.DataTypes[ANativeRowIndex];
			if (LDataType == null)
				LDataType = ARowType.Columns[ANativeRowIndex].DataType;
			#else
			Schema.IDataType LDataType = ARowType.Columns[ANativeRowIndex].DataType;
			#endif

			Schema.IScalarType LScalarType = LDataType as Schema.IScalarType;
			if (LScalarType != null)
				return new RowInternedScalar(AManager, LScalarType, ANativeRow, ANativeRowIndex);
				
			Schema.IRowType LRowType = LDataType as Schema.IRowType;
			if (LRowType != null)
				return new Row(AManager, LRowType, (NativeRow)ANativeRow.Values[ANativeRowIndex]);
			
			Schema.IListType LListType = LDataType as Schema.IListType;
			if (LListType != null)
				return new ListValue(AManager, LListType, (NativeList)ANativeRow.Values[ANativeRowIndex]);
				
			Schema.ITableType LTableType = LDataType as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AManager, (NativeTable)ANativeRow.Values[ANativeRowIndex]);
				
			Schema.ICursorType LCursorType = LDataType as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AManager, LCursorType, (int)ANativeRow.Values[ANativeRowIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, LDataType == null ? "<null>" : LDataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static DataValue FromNativeList(IValueManager AManager, Schema.IListType AListType, NativeList ANativeList, int ANativeListIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IDataType LDataType = ANativeList.DataTypes[ANativeListIndex];
			if (LDataType == null)
				LDataType = AListType.ElementType;

			Schema.IScalarType LScalarType = ANativeList.DataTypes[ANativeListIndex] as Schema.IScalarType;
			if (LScalarType != null)
				return new ListInternedScalar(AManager, LScalarType, ANativeList, ANativeListIndex);
				
			Schema.IRowType LRowType = ANativeList.DataTypes[ANativeListIndex] as Schema.IRowType;
			if (LRowType != null)
				return new Row(AManager, LRowType, (NativeRow)ANativeList.Values[ANativeListIndex]);
			
			Schema.IListType LListType = ANativeList.DataTypes[ANativeListIndex] as Schema.IListType;
			if (LListType != null)
				return new ListValue(AManager, LListType, (NativeList)ANativeList.Values[ANativeListIndex]);
				
			Schema.ITableType LTableType = ANativeList.DataTypes[ANativeListIndex] as Schema.ITableType;
			if (LTableType != null)
				return new TableValue(AManager, (NativeTable)ANativeList.Values[ANativeListIndex]);
				
			Schema.ICursorType LCursorType = ANativeList.DataTypes[ANativeListIndex] as Schema.ICursorType;
			if (LCursorType != null)
				return new CursorValue(AManager, LCursorType, (int)ANativeList.Values[ANativeListIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ANativeList.DataTypes[ANativeListIndex] == null ? "<null>" : ANativeList.DataTypes[ANativeListIndex].GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given physical value.</summary>
		public static DataValue FromPhysical(IValueManager AManager, Schema.IDataType ADataType, byte[] ABuffer, int AOffset)
		{
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				Scalar LScalar = new Scalar(AManager, LScalarType, null);
				LScalar.ReadFromPhysical(ABuffer, AOffset);
				return LScalar;
			}
			
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				Row LRow = new Row(AManager, LRowType);
				LRow.ReadFromPhysical(ABuffer, AOffset);
				return LRow;
			}
			
			Schema.IListType LListType = ADataType as Schema.IListType;
			if (LListType != null)
			{
				ListValue LList = new ListValue(AManager, LListType);
				LList.ReadFromPhysical(ABuffer, AOffset);
				return LList;
			}
			
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				TableValue LTable = new TableValue(AManager, null);
				LTable.ReadFromPhysical(ABuffer, AOffset);
				return LTable;
			}
			
			Schema.ICursorType LCursorType = ADataType as Schema.ICursorType;
			if (LCursorType != null)
			{
				CursorValue LCursor = new CursorValue(AManager, LCursorType, -1);
				LCursor.ReadFromPhysical(ABuffer, AOffset);
				return LCursor;
			}

			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, ADataType == null ? "<null>" : ADataType.GetType().Name);
		}

		public static object CopyNative(IValueManager AManager, Schema.IDataType ADataType, object AValue)
		{
			// This code is duplicated in the descendent CopyNative methods for performance
			if (AValue == null)
				return AValue;
				
			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					return AManager.StreamManager.Reference((StreamID)AValue);
				
				ICloneable LCloneable = AValue as ICloneable;
				if (LCloneable != null)
					return LCloneable.Clone();
					
				if (LScalarType.IsCompound)
					return CopyNative(AManager, LScalarType.CompoundRowType, AValue);
					
				return AValue;
			}
			
			Schema.IRowType LRowType = ADataType as Schema.IRowType;
			if (LRowType != null)
			{
				NativeRow LNativeRow = (NativeRow)AValue;
				NativeRow LNewRow = new NativeRow(LRowType.Columns.Count);
				for (int LIndex = 0; LIndex < LRowType.Columns.Count; LIndex++)
				{
					#if USEDATATYPESINNATIVEROW
					LNewRow.DataTypes[LIndex] = LNativeRow.DataTypes[LIndex];
					LNewRow.Values[LIndex] = CopyNative(AManager, LNativeRow.DataTypes[LIndex], LNativeRow.Values[LIndex]);
					#else
					LNewRow.Values[LIndex] = CopyNative(AManager, LRowType.Columns[LIndex].DataType, LNativeRow.Values[LIndex]);
					#endif
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
					LNewList.Values.Add(CopyNative(AManager, LNativeList.DataTypes[LIndex], LNativeList.Values[LIndex]));
				}
				return LNewList;
			}
			
			Schema.ITableType LTableType = ADataType as Schema.ITableType;
			if (LTableType != null)
			{
				NativeTable LNativeTable = (NativeTable)AValue;
				NativeTable LNewTable = new NativeTable(AManager, LNativeTable.TableVar);
				using (Scan LScan = new Scan(AManager, LNativeTable, LNativeTable.ClusteredIndex, ScanDirection.Forward, null, null))
				{
					while (!LScan.EOF())
					{
						using (Row LRow = LScan.GetRow())
						{
							LNewTable.Insert(AManager, LRow);
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
		public static void DisposeNative(IValueManager AManager, Schema.IDataType ADataType, object AValue)
		{
			if (AValue == null)
				return;

			Schema.ScalarType LScalarType = ADataType as Schema.ScalarType;
			if (LScalarType != null)
			{
				if (AValue is StreamID)
					AManager.StreamManager.Deallocate((StreamID)AValue);
					
				if (LScalarType.IsCompound)
					DisposeNative(AManager, LScalarType.CompoundRowType, AValue);

				return;
			}
			
			using (DataValue LDataValue = DataValue.FromNative(AManager, ADataType, AValue))
			{
				LDataValue.ValuesOwned = true;
			}
		}
		
		public static object CopyValue(IValueManager AManager, object AValue)
		{
			DataValue LValue = AValue as DataValue;
			if (LValue != null)
				return LValue.Copy();
				
			ICloneable LCloneable = AValue as ICloneable;
			if (LCloneable != null)
				return LCloneable.Clone();
				
			if (AValue is StreamID)
				return AManager.StreamManager.Reference((StreamID)AValue);
				
			NativeRow LNativeRow = AValue as NativeRow;
			if (LNativeRow != null)
			{
				NativeRow LNewRow = new NativeRow(LNativeRow.Values.Length);
				for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
					LNewRow.Values[LIndex] = CopyValue(AManager, LNativeRow.Values[LIndex]);
				return LNewRow;
			}
			
			return AValue;
		}
		
		public static void DisposeValue(IValueManager AManager, object AValue)
		{
			DataValue LValue = AValue as DataValue;
			if (LValue != null)
			{
				LValue.Dispose();
				return;
			}
			
			if (AValue is StreamID)
			{
				AManager.StreamManager.Deallocate((StreamID)AValue);
				return;
			}
			
			NativeRow LNativeRow = AValue as NativeRow;
			if (LNativeRow != null)
			{
				for (int LIndex = 0; LIndex < LNativeRow.Values.Length; LIndex++)
					DisposeValue(AManager, LNativeRow.Values[LIndex]);
				return;
			}
		}
		
		/// <summary>
		/// Compares two native values directly.
		/// </summary>
		/// <remarks>
		/// The method expects both values to be non-null.
		/// The method uses direct comparison, it does not attempt to invoke the D4 equality operator for the values.
		/// Note that this method expects that neither argument is null.
		/// </remarks>
		/// <returns>True if the values are equal, false otherwise.</returns>
		public static bool NativeValuesEqual(IValueManager AManager, object LOldValue, object LCurrentValue)
		{
			if (((LOldValue is StreamID) || (LOldValue is byte[])) && ((LCurrentValue is StreamID) || (LCurrentValue is byte[])))
			{
				Stream LOldStream = 
					LOldValue is StreamID 
						? AManager.StreamManager.Open((StreamID)LOldValue, LockMode.Exclusive)
						: new MemoryStream((byte[])LOldValue, false);
				try
				{
					Stream LCurrentStream = 
						LCurrentValue is StreamID
							? AManager.StreamManager.Open((StreamID)LCurrentValue, LockMode.Exclusive)
							: new MemoryStream((byte[])LCurrentValue, false);
					try
					{
						bool LValuesEqual = true;
						int LOldByte;
						int LCurrentByte;
						while (LValuesEqual)
						{
							LOldByte = LOldStream.ReadByte();
							LCurrentByte = LCurrentStream.ReadByte();
							
							if (LOldByte != LCurrentByte)
							{
								LValuesEqual = false;
								break;
							}
							
							if (LOldByte == -1)
								break;
						}
						
						return LValuesEqual;
					}
					finally
					{
						LCurrentStream.Close();
					}
				}
				finally
				{
					LOldStream.Close();
				}
			}
			
			return LOldValue.Equals(LCurrentValue);
		}
	}
}

