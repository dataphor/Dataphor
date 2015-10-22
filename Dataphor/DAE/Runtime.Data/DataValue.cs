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
	public abstract class DataValue : IDataValue, IDisposable //, IDisposableNotify
	{
		public DataValue(IValueManager manager, Schema.IDataType dataType) : base()
		{
			_manager = manager;
			_dataType = dataType;
		}
		
		protected IValueManager _manager;
		public IValueManager Manager { get { return _manager; } }
		
		protected Schema.IDataType _dataType;
		public Schema.IDataType DataType { get	{ return _dataType;	} }
		
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
			if (_manager != null)
			{
				#if USEFINALIZER
				System.GC.SuppressFinalize(this);
				#endif
				Dispose(true);
				_manager = null;
				_dataType = null;
			}
		}
		
		protected virtual void Dispose(bool disposing) { }

		private bool _valuesOwned = true;
		/// <summary>Indicates whether disposal of the value should deallocate any resources associated with the value.</summary>
		public bool ValuesOwned { get { return _valuesOwned; } set { _valuesOwned = value; } }

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
					MemoryStream stream = new MemoryStream(64);
					WriteToPhysical(stream, false);
					return stream.GetBuffer();
				}
				else
				{
					byte[] tempValue = new byte[GetPhysicalSize(false)];
					WriteToPhysical(tempValue, 0, false);
					return tempValue;
				}
			} 
			set
			{
				if (IsPhysicalStreaming)
				{
					MemoryStream stream = new MemoryStream(value, 0, value.Length, false, true);
					ReadFromPhysical(stream);
				}
				else
					ReadFromPhysical(value, 0);
			}
		}

		/// <summary>Indicates whether or not the conveyor for values of this type uses streams to read/write.  If this is false, the conveyor will use byte arrays.</summary>
		public virtual bool IsPhysicalStreaming { get { return false; } }
		
		/// <summary>Returns the number of bytes required to store the physical representation of this value.</summary>
		public virtual int GetPhysicalSize(bool expandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of this value into the byte array given in ABuffer, beginning at the offset given by offset.</summary>		
		public virtual void WriteToPhysical(byte[] buffer, int offset, bool expandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of this value into the stream given in AStream.</summary>
		public virtual void WriteToPhysical(Stream stream, bool expandStreams)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Sets the native representation of this value by reading the physical representation from the byte array given in ABuffer, beginning at the offset given by offset.</summary>
		public virtual void ReadFromPhysical(byte[] buffer, int offset)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Sets the native representation of this value by reading the physical representation from the stream given in AStream.</summary>
		public virtual void ReadFromPhysical(Stream stream)
		{
			throw new NotSupportedException();
		}
		
		public virtual Stream OpenStream()
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		public virtual Stream OpenStream(string representationName)
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		public virtual ITable OpenCursor()
		{
			throw new RuntimeException(RuntimeException.Codes.UnableToProvideCursorAccess, DataType.Name);
		}
		
		/// <summary>Copies the native representation of this value and returns the host representation as the given type.</summary>
		public IDataValue CopyAs(Schema.IDataType dataType)
		{
			// This code is duplicated in the Copy and FromNative methods for performance...
			object tempValue = CopyNativeAs(dataType);

			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (tempValue is StreamID)
				{
					Scalar scalar = new Scalar(Manager, scalarType, (StreamID)tempValue);
					scalar.ValuesOwned = true;
					return scalar;
				}
				return new Scalar(Manager, scalarType, tempValue);
			}
				
			Schema.IRowType rowType = dataType as Schema.IRowType;
			if (rowType != null)
			{
				Row row = new Row(Manager, rowType, (NativeRow)tempValue);
				row.ValuesOwned = true;
				return row;
			}
			
			Schema.IListType listType = dataType as Schema.IListType;
			if (listType != null)
			{
				ListValue list = new ListValue(Manager, listType, (NativeList)tempValue);
				list.ValuesOwned = true;
				return list;
			}
				
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
			{
				TableValue table = new TableValue(Manager, tableType, (NativeTable)tempValue);
				table.ValuesOwned = true;
				return table;
			}
				
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(Manager, cursorType, (int)tempValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, dataType == null ? "<null>" : dataType.GetType().Name);
		}
		
		public IDataValue Copy()
		{
			// This code is duplicated in the FromNative and CopyAs methods for performance...
			object tempValue = CopyNative();
			Schema.IScalarType scalarType = DataType as Schema.IScalarType;

			if (scalarType != null)
			{
				if (tempValue is StreamID)
				{
					Scalar scalar = new Scalar(Manager, scalarType, (StreamID)tempValue);
					scalar.ValuesOwned = true;
					return scalar;
				}
				return new Scalar(Manager, scalarType, tempValue);
			}
				
			Schema.IRowType rowType = DataType as Schema.IRowType;
			if (rowType != null)
			{
				Row row = new Row(Manager, rowType, (NativeRow)tempValue);
				row.ValuesOwned = true;
				return row;
			}
			
			Schema.IListType listType = DataType as Schema.IListType;
			if (listType != null)
			{
				ListValue list = new ListValue(Manager, listType, (NativeList)tempValue);
				list.ValuesOwned = true;
				return list;
			}
				
			Schema.ITableType tableType = DataType as Schema.ITableType;
			if (tableType != null)
			{
				TableValue table = new TableValue(Manager, tableType, (NativeTable)tempValue);
				table.ValuesOwned = true;
				return table;
			}
				
			Schema.ICursorType cursorType = DataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(Manager, cursorType, (int)tempValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public object Copy(IValueManager manager)
		{
			// This code is duplicated in the FromNative and CopyAs methods for performance...
			object tempValue = CopyNative();

			Schema.IScalarType scalarType = DataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (tempValue is StreamID)
				{
					Scalar scalar = new Scalar(manager, scalarType, (StreamID)tempValue);
					scalar.ValuesOwned = true;
					return scalar;
				}
				return new Scalar(manager, scalarType, tempValue);
			}
				
			Schema.IRowType rowType = DataType as Schema.IRowType;
			if (rowType != null)
			{
				Row row = new Row(manager, rowType, (NativeRow)tempValue);
				row.ValuesOwned = true;
				return row;
			}
			
			Schema.IListType listType = DataType as Schema.IListType;
			if (listType != null)
			{
				ListValue list = new ListValue(manager, listType, (NativeList)tempValue);
				list.ValuesOwned = true;
				return list;
			}
				
			Schema.ITableType tableType = DataType as Schema.ITableType;
			if (tableType != null)
			{
				TableValue table = new TableValue(manager, tableType, (NativeTable)tempValue);
				table.ValuesOwned = true;
				return table;
			}
				
			Schema.ICursorType cursorType = DataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(manager, cursorType, (int)tempValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, DataType == null ? "<null>" : DataType.GetType().Name);
		}
		
		public abstract object CopyNativeAs(Schema.IDataType dataType);
		
		public object CopyNative()
		{
			return CopyNativeAs(DataType);
		}
		
		public static Schema.IScalarType NativeTypeToScalarType(IValueManager manager, Type type)
		{
			if (type == NativeAccessors.AsBoolean.NativeType) return manager.DataTypes.SystemBoolean;
			if (type == NativeAccessors.AsByte.NativeType) return manager.DataTypes.SystemByte;
			if (type == NativeAccessors.AsByteArray.NativeType) return manager.DataTypes.SystemBinary;
			if (type == NativeAccessors.AsDateTime.NativeType) return manager.DataTypes.SystemDateTime;
			if (type == NativeAccessors.AsDecimal.NativeType) return manager.DataTypes.SystemDecimal;
			if (type == NativeAccessors.AsException.NativeType) return manager.DataTypes.SystemError;
			if (type == NativeAccessors.AsGuid.NativeType) return manager.DataTypes.SystemGuid;
			if (type == NativeAccessors.AsInt16.NativeType) return manager.DataTypes.SystemShort;
			if (type == NativeAccessors.AsInt32.NativeType) return manager.DataTypes.SystemInteger;
			if (type == NativeAccessors.AsInt64.NativeType) return manager.DataTypes.SystemLong;
			if (type == NativeAccessors.AsString.NativeType) return manager.DataTypes.SystemString;
			if (type == NativeAccessors.AsTimeSpan.NativeType) return manager.DataTypes.SystemTimeSpan;
			return manager.DataTypes.SystemScalar;
		}
		
		public static IDataValue FromNative(IValueManager manager, object tempValue)
		{
			if (tempValue == null)
				return new Scalar(manager, manager.DataTypes.SystemScalar, null);
				
			if (tempValue is StreamID)
				return new Scalar(manager, manager.DataTypes.SystemScalar, (StreamID)tempValue);
				
			return new Scalar(manager, NativeTypeToScalarType(manager, tempValue.GetType()), tempValue);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static IDataValue FromNative(IValueManager manager, Schema.IDataType dataType, object tempValue)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (tempValue is StreamID)
					return new Scalar(manager, scalarType, (StreamID)tempValue);
				if (scalarType.IsGeneric)
					return new Scalar(manager, NativeTypeToScalarType(manager, tempValue.GetType()), tempValue);
				return new Scalar(manager, scalarType, tempValue);
			}
				
			if (tempValue == null)
				return null;
				
			Schema.IRowType rowType = dataType as Schema.IRowType;
			if (rowType != null)
				return new Row(manager, rowType, (NativeRow)tempValue);
			
			Schema.IListType listType = dataType as Schema.IListType;
			if (listType != null)
				return new ListValue(manager, listType, (NativeList)tempValue);
				
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
				return new TableValue(manager, tableType, (NativeTable)tempValue);
				
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(manager, cursorType, (int)tempValue);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, dataType == null ? "<null>" : dataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static IDataValue FromNativeRow(IValueManager manager, Schema.IRowType rowType, NativeRow nativeRow, int nativeRowIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			#if USEDATATYPESINNATIVEROW
			Schema.IDataType dataType = nativeRow.DataTypes[nativeRowIndex];
			if (dataType == null)
				dataType = rowType.Columns[nativeRowIndex].DataType;
			#else
			Schema.IDataType dataType = ARowType.Columns[ANativeRowIndex].DataType;
			#endif

			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
				return new RowInternedScalar(manager, scalarType, nativeRow, nativeRowIndex);
				
			Schema.IRowType localRowType = dataType as Schema.IRowType;
			if (localRowType != null)
				return new Row(manager, localRowType, (NativeRow)nativeRow.Values[nativeRowIndex]);
			
			Schema.IListType listType = dataType as Schema.IListType;
			if (listType != null)
				return new ListValue(manager, listType, (NativeList)nativeRow.Values[nativeRowIndex]);
				
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
				return new TableValue(manager, tableType, (NativeTable)nativeRow.Values[nativeRowIndex]);
				
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(manager, cursorType, (int)nativeRow.Values[nativeRowIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, dataType == null ? "<null>" : dataType.GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given native value.  This is a by-reference operation.</summary>		
		public static IDataValue FromNativeList(IValueManager manager, Schema.IListType listType, NativeList nativeList, int nativeListIndex)
		{
			// This code is duplicated in the Copy method and the FromNative overloads for performance
			Schema.IDataType dataType = nativeList.DataTypes[nativeListIndex];
			if (dataType == null)
				dataType = listType.ElementType;

			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
				return new ListInternedScalar(manager, scalarType, nativeList, nativeListIndex);
				
			Schema.IRowType rowType = dataType as Schema.IRowType;
			if (rowType != null)
				return new Row(manager, rowType, (NativeRow)nativeList.Values[nativeListIndex]);
			
			Schema.IListType localListType = dataType as Schema.IListType;
			if (localListType != null)
				return new ListValue(manager, localListType, (NativeList)nativeList.Values[nativeListIndex]);
				
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
				return new TableValue(manager, tableType, (NativeTable)nativeList.Values[nativeListIndex]);
				
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
				return new CursorValue(manager, cursorType, (int)nativeList.Values[nativeListIndex]);
				
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, nativeList.DataTypes[nativeListIndex] == null ? "<null>" : nativeList.DataTypes[nativeListIndex].GetType().Name);
		}
		
		/// <summary>Returns the host representation of the given physical value.</summary>
		public static IDataValue FromPhysical(IValueManager manager, Schema.IDataType dataType, byte[] buffer, int offset)
		{
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				Scalar scalar = new Scalar(manager, scalarType, null);
				scalar.ReadFromPhysical(buffer, offset);
				return scalar;
			}
			
			Schema.IRowType rowType = dataType as Schema.IRowType;
			if (rowType != null)
			{
				Row row = new Row(manager, rowType);
				row.ReadFromPhysical(buffer, offset);
				return row;
			}
			
			Schema.IListType listType = dataType as Schema.IListType;
			if (listType != null)
			{
				ListValue list = new ListValue(manager, listType);
				list.ReadFromPhysical(buffer, offset);
				return list;
			}
			
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
			{
				TableValue table = new TableValue(manager, tableType, null);
				table.ReadFromPhysical(buffer, offset);
				return table;
			}
			
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
			{
				CursorValue cursor = new CursorValue(manager, cursorType, -1);
				cursor.ReadFromPhysical(buffer, offset);
				return cursor;
			}

			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, dataType == null ? "<null>" : dataType.GetType().Name);
		}

		public static object CopyNative(IValueManager manager, Schema.IDataType dataType, object tempValue)
		{
			// This code is duplicated in the descendent CopyNative methods for performance
			if (tempValue == null)
				return tempValue;
				
			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (tempValue is StreamID)
					return manager.StreamManager.Reference((StreamID)tempValue);
				
				ICloneable cloneable = tempValue as ICloneable;
				if (cloneable != null)
					return cloneable.Clone();
					
				if (scalarType.IsCompound)
					return CopyNative(manager, scalarType.CompoundRowType, tempValue);
					
				return tempValue;
			}
			
			Schema.IRowType rowType = dataType as Schema.IRowType;
			if (rowType != null)
			{
				NativeRow nativeRow = (NativeRow)tempValue;
				NativeRow newRow = new NativeRow(rowType.Columns.Count);
				for (int index = 0; index < rowType.Columns.Count; index++)
				{
					#if USEDATATYPESINNATIVEROW
					newRow.DataTypes[index] = nativeRow.DataTypes[index];
					newRow.Values[index] = CopyNative(manager, nativeRow.DataTypes[index], nativeRow.Values[index]);
					#else
					newRow.Values[index] = CopyNative(AManager, rowType.Columns[index].DataType, nativeRow.Values[index]);
					#endif
				}
				return newRow;
			}
			
			Schema.IListType listType = dataType as Schema.IListType;
			if (listType != null)
			{
				NativeList nativeList = (NativeList)tempValue;
				NativeList newList = new NativeList();
				for (int index = 0; index < nativeList.DataTypes.Count; index++)
				{
					newList.DataTypes.Add(nativeList.DataTypes[index]);
					newList.Values.Add(CopyNative(manager, nativeList.DataTypes[index], nativeList.Values[index]));
				}
				return newList;
			}
			
			Schema.ITableType tableType = dataType as Schema.ITableType;
			if (tableType != null)
			{
				NativeTable nativeTable = (NativeTable)tempValue;
				NativeTable newTable = new NativeTable(manager, nativeTable.TableVar);
				using (Scan scan = new Scan(manager, nativeTable, nativeTable.ClusteredIndex, ScanDirection.Forward, null, null))
				{
					scan.Open();
					while (scan.Next())
					{
						using (IRow row = scan.GetRow())
						{
							newTable.Insert(manager, row);
						}
					}
				}
				return newTable;
			}
			
			Schema.ICursorType cursorType = dataType as Schema.ICursorType;
			if (cursorType != null)
				return tempValue;
			
			throw new RuntimeException(RuntimeException.Codes.InvalidValueType, dataType == null ? "<null>" : dataType.GetType().Name);
		}

		/// <summary>Disposes the given native value.</summary>		
		public static void DisposeNative(IValueManager manager, Schema.IDataType dataType, object tempValue)
		{
			if (tempValue == null)
				return;

			Schema.IScalarType scalarType = dataType as Schema.IScalarType;
			if (scalarType != null)
			{
				if (tempValue is StreamID)
					manager.StreamManager.Deallocate((StreamID)tempValue);
					
				if (scalarType.IsCompound)
					DisposeNative(manager, scalarType.CompoundRowType, tempValue);

				return;
			}
			
			using (IDataValue dataValue = DataValue.FromNative(manager, dataType, tempValue))
			{
				dataValue.ValuesOwned = true;
			}
		}
		
		public static object CopyValue(IValueManager manager, object value)
		{
			IDataValue dataValue = value as IDataValue;
			if (dataValue != null)
				return dataValue.Copy();
				
			ICloneable cloneable = value as ICloneable;
			if (cloneable != null)
				return cloneable.Clone();
				
			#if SILVERLIGHT
			
			System.Array array = value as System.Array;
			if (array != null)
				return array.Clone();
			
			#endif
				
			if (value is StreamID)
				return manager.StreamManager.Reference((StreamID)value);
				
			NativeRow nativeRow = value as NativeRow;
			if (nativeRow != null)
			{
				NativeRow newRow = new NativeRow(nativeRow.Values.Length);
				for (int index = 0; index < nativeRow.Values.Length; index++)
				{
					#if USEDATATYPESINNATIVEROW
					newRow.DataTypes[index] = nativeRow.DataTypes[index];
					#endif
					newRow.Values[index] = CopyValue(manager, nativeRow.Values[index]);
				}
				return newRow;
			}
			
			NativeList nativeList = value as NativeList;
			if (nativeList != null)
			{
				NativeList newList = new NativeList();
				for (int index = 0; index < nativeList.Values.Count; index++)
				{
					newList.DataTypes.Add(nativeList.DataTypes[index]);
					newList.Values.Add(CopyValue(manager, nativeList.Values[index]));
				}
				return newList;
			}

			NativeTable nativeTable = value as NativeTable;
			if (nativeTable != null)
			{
				return CopyNative(manager, nativeTable.TableType, nativeTable);
			}

			return value;
		}
		
		public static void DisposeValue(IValueManager manager, object tempValue)
		{
			IDataValue localTempValue = tempValue as IDataValue;
			if (localTempValue != null)
			{
				localTempValue.Dispose();
				return;
			}
			
			if (tempValue is StreamID)
			{
				manager.StreamManager.Deallocate((StreamID)tempValue);
				return;
			}
			
			NativeRow nativeRow = tempValue as NativeRow;
			if (nativeRow != null)
			{
				for (int index = 0; index < nativeRow.Values.Length; index++)
					DisposeValue(manager, nativeRow.Values[index]);
				return;
			}

			NativeList nativeList = tempValue as NativeList;
			if (nativeList != null)
			{
				for (int index = 0; index < nativeList.Values.Count; index++)
					DisposeValue(manager, nativeList.Values[index]);
			}

			NativeTable nativeTable = tempValue as NativeTable;
			if (nativeTable != null)
			{
				DisposeNative(manager, nativeTable.TableType, nativeTable);
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
		public static bool NativeValuesEqual(IValueManager manager, object LOldValue, object LCurrentValue)
		{
			if (((LOldValue is StreamID) || (LOldValue is byte[])) && ((LCurrentValue is StreamID) || (LCurrentValue is byte[])))
			{
				Stream oldStream = 
					LOldValue is StreamID 
						? manager.StreamManager.Open((StreamID)LOldValue, LockMode.Exclusive)
						: new MemoryStream((byte[])LOldValue, false);
				try
				{
					Stream currentStream = 
						LCurrentValue is StreamID
							? manager.StreamManager.Open((StreamID)LCurrentValue, LockMode.Exclusive)
							: new MemoryStream((byte[])LCurrentValue, false);
					try
					{
						bool valuesEqual = true;
						int oldByte;
						int currentByte;
						while (valuesEqual)
						{
							oldByte = oldStream.ReadByte();
							currentByte = currentStream.ReadByte();
							
							if (oldByte != currentByte)
							{
								valuesEqual = false;
								break;
							}
							
							if (oldByte == -1)
								break;
						}
						
						return valuesEqual;
					}
					finally
					{
						currentStream.Close();
					}
				}
				finally
				{
					oldStream.Close();
				}
			}
			
			return LOldValue.Equals(LCurrentValue);
		}
	}
}

