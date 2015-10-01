/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USEDATATYPESINNATIVEROW // Determines whether or not the native row tracks the data type of the values it contains

using System;
using System.IO;
using System.Text;
using System.Collections;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	#if USEDATATYPESINNATIVEROW
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Compiling;
	#endif
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;

	/*
		Row ->
		
			Rows store data as an array of objects.
			Data values can be stored in one of two ways inside a row.
			If the Data value is a StreamID, it is considered a non-native
			value and enables deferred access values by effectively storing
			only the StreamID of the data.  Otherwise, the value is considered
			a native value and is stored in its host representation.
	*/
	
	public class NativeRow : System.Object
	{
		public NativeRow(int columnCount) : base()
		{
			#if USEDATATYPESINNATIVEROW
			_dataTypes = new Schema.IDataType[columnCount];
			#endif
			_values = new object[columnCount];
		}
		
		#if USEDATATYPESINNATIVEROW
		private Schema.IDataType[] _dataTypes;
		public Schema.IDataType[] DataTypes { get { return _dataTypes; } }
		#endif
		
		private object[] _values;
		public object[] Values { get { return _values; } }
		
		public BitArray ModifiedFlags;
	}
	
	/// <remarks>
	/// Provides a fixed length buffer for cell values with overflow management built in.
	/// Used in conjunction with the CellValueStream, provides transparent variable length
	/// value storage in a fixed length buffer.
	/// </remarks>    
	public class Row : DataValue, IRow
	{
		public Row(IValueManager manager, Schema.IRowType dataType) : base(manager, dataType)
		{
			_row = new NativeRow(DataType.Columns.Count);
			#if USEDATATYPESINNATIVEROW
			for (int index = 0; index < DataType.Columns.Count; index++)
				_row.DataTypes[index] = DataType.Columns[index].DataType;
			#endif
			ValuesOwned = true;
		}

		// The given object[] is assumed to contains values of the appropriate type for the given data type
		public Row(IValueManager manager, Schema.IRowType dataType, NativeRow row) : base(manager, dataType)
		{
			_row = row;
			ValuesOwned = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (_row != null)
			{
				if (ValuesOwned)
					ClearValues();
				_row = null;
			}
			base.Dispose(disposing);
		}
		
		public new Schema.IRowType DataType { get { return (Schema.IRowType)base.DataType; } }
		
		private NativeRow _row;
		
		public override object AsNative
		{
			get { return _row; }
			set 
			{
				if (_row != null)
					ClearValues();
				_row = (NativeRow)value; 
			}
		}
		
		private object[] _writeList;
		private IDataValue[] _elementWriteList;

		/*
			Row Value Format ->
			
				00 -> 0 - Indicates that non-native values are stored as a StreamID 1 - Indicates non-native values are stored inline
				01-XX -> Value for each attribute in the row
				
			Row Attribute Format ->

				There are five possibilities for an attribute ->			
					Nil Native
					Nil Non-Native (StreamID.Null)
					Standard Native
					Standard Non-Native
					Specialized Native
					Specialized Non-Native
				
					For non-native values, the value will be expanded or not dependending on the expanded setting for the row value

				00	-> 0 - 5
					0 if the row contains a native nil for this attribute - no data follows
					1 if the row contains a non-native nil for this attribute - no data follows
					2 if the row contains a native value of the data type of the attribute
						01-04 -> Length of the physical representation of this value
						05-XX -> The physical representation of this value
					3 if the row contains a non-native value of the data type of the attribute
						01-04 -> Length of the physical representation of this value
						05-XX -> The physical representation of this value (expanded based on the expanded setting for the list value)
					4 if the row contains a native value of some specialization of the data type of the attribute
						01-XX -> The data type name of this value, stored using a StringConveyor
						XX+1-XX+5 -> The length of the physical representation of this value
						XX+6-YY -> The physical representation of this value
					5 if the row contains a non-native value of some specialization of the data type of the attribute
						01-XX -> The data type name of this value, stored using a StringConveyor
						XX+1-XX+5 -> The length of the physical representation of this value
						XX+6-YY -> The physical representation of this value (expanded based on the expanded setting for the list value)
		*/
		public override int GetPhysicalSize(bool expandStreams)
		{
			int size = 1; // write the value indicator
			if (!IsNil)
			{
				size += 1; // write the extended streams indicator
				_writeList = new object[DataType.Columns.Count]; // list for saving the sizes or streams of each attribute in the row
				_elementWriteList = new DataValue[DataType.Columns.Count]; // list for saving host representations of values between the GetPhysicalSize and WriteToPhysical calls
				Stream stream;
				StreamID streamID;
				Schema.IScalarType scalarType;
				Streams.IConveyor conveyor;
				IDataValue element;
				int elementSize;
				for (int index = 0; index < _writeList.Length; index++)
				{
					size += sizeof(byte); // write a value indicator
					if (_row.Values[index] != null)
					{
						#if USEDATATYPESINNATIVEROW
						if (!DataType.Columns[index].DataType.Equals(_row.DataTypes[index]))
							size += Manager.GetConveyor(Manager.DataTypes.SystemString).GetSize(_row.DataTypes[index].Name); // write the name of the data type of the value
						#endif
						/*							
						LElement = DataValue.FromNativeRow(Manager, DataType, FRow, LIndex);
						FElementWriteList[LIndex] = LElement;
						LElementSize = LElement.GetPhysicalSize(AExpandStreams);
						FWriteList[LIndex] = LElementSize;
						LSize += sizeof(int) + LElementSize;
						*/
						#if USEDATATYPESINNATIVEROW
						scalarType = _row.DataTypes[index] as Schema.IScalarType;
						#else
						scalarType = DataType.Columns[index].DataType as Schema.IScalarType;
						#endif
						if ((scalarType != null) && !scalarType.IsCompound)
						{
							if (_row.Values[index] is StreamID)
							{
								// If this is a non-native scalar
								streamID = (StreamID)_row.Values[index];
								if (expandStreams)
								{
									if (streamID != StreamID.Null)
									{
										stream = Manager.StreamManager.Open((StreamID)_row.Values[index], LockMode.Exclusive);
										_writeList[index] = stream;
										size += sizeof(int) + (int)stream.Length;
									}
								}
								else
								{
									if (streamID != StreamID.Null)
									{
										elementSize = sizeof(long);
										_writeList[index] = elementSize;
										size += elementSize;
									}
								}
							}
							else
							{
								conveyor = Manager.GetConveyor(scalarType);
								if (conveyor.IsStreaming)
								{
									stream = new MemoryStream(64);
									_writeList[index] = stream;
									conveyor.Write(_row.Values[index], stream);
									stream.Position = 0;
									size += sizeof(int) + (int)stream.Length;
								}
								else
								{
									elementSize = conveyor.GetSize(_row.Values[index]);
									_writeList[index] = elementSize;
									size += sizeof(int) + elementSize;;
								}
							}
						}
						else
						{
							element = DataValue.FromNativeRow(Manager, DataType, _row, index);
							_elementWriteList[index] = element;
							elementSize = element.GetPhysicalSize(expandStreams);
							_writeList[index] = elementSize;
							size += sizeof(int) + elementSize;
						}						
					}
				}
			}
			return size;
		}
		
		public override void WriteToPhysical(byte[] buffer, int offset, bool expandStreams)
		{
			if (_writeList == null)
				throw new RuntimeException(RuntimeException.Codes.UnpreparedWriteToPhysicalCall);
				
			buffer[offset] = (byte)(IsNil ? 0 : 1); // Write the value indicator
			offset++;
			
			if (!IsNil)
			{
				buffer[offset] = (byte)(expandStreams ? 1 : 0); // Write the expanded streams indicator
				offset++;
					
				#if USEDATATYPESINNATIVEROW
				Streams.IConveyor stringConveyor = null;
				#endif
				Streams.IConveyor int64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
				Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);

				Stream stream;
				StreamID streamID;
				int elementSize;
				Schema.IScalarType scalarType;
				Streams.IConveyor conveyor;
				IDataValue element;
				for (int index = 0; index < _writeList.Length; index++)
				{
					if (_row.Values[index] == null)
					{
						buffer[offset] = (byte)0; // Write the native nil indicator
						offset++;
					}
					else
					{
						#if USEDATATYPESINNATIVEROW
						scalarType = _row.DataTypes[index] as Schema.IScalarType;
						#else
						scalarType = DataType.Columns[index].DataType as Schema.IScalarType;
						#endif
						if ((scalarType != null) && !scalarType.IsCompound)
						{
							if (_row.Values[index] is StreamID)
							{
								// If this is a non-native scalar
								streamID = (StreamID)_row.Values[index];
								if (streamID == StreamID.Null)
								{
									buffer[offset] = (byte)1; // Write the non-native nil indicator
									offset++;
								}
								else
								{
									#if USEDATATYPESINNATIVEROW
									if (DataType.Columns[index].DataType.Equals(_row.DataTypes[index]))
									{
									#endif
										buffer[offset] = (byte)3; // Write the native standard value indicator
										offset++;
									#if USEDATATYPESINNATIVEROW
									}
									else
									{
										buffer[offset] = (byte)5; // Write the native specialized value indicator
										offset++;
										if (stringConveyor == null)
											stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
										elementSize = stringConveyor.GetSize(_row.DataTypes[index].Name);
										stringConveyor.Write(_row.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
										offset += elementSize;
									}
									#endif
									
									if (expandStreams)
									{
										stream = (Stream)_writeList[index];
										int32Conveyor.Write(Convert.ToInt32(stream.Length), buffer, offset);
										offset += sizeof(int);
										stream.Read(buffer, offset, (int)stream.Length);
										offset += (int)stream.Length;
										stream.Close();
									}
									else
									{
										int64Conveyor.Write((long)streamID.Value, buffer, offset);
										offset += sizeof(long);
									}
								}
							}
							else
							{
								#if USEDATATYPESINNATIVEROW
								if (DataType.Columns[index].DataType.Equals(_row.DataTypes[index]))
								{
								#endif
									buffer[offset] = (byte)2; // Write the native standard value indicator
									offset++;
								#if USEDATATYPESINNATIVEROW
								}
								else
								{
									buffer[offset] = (byte)4; // Write the native specialized value indicator
									offset++;
									if (stringConveyor == null)
										stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
									elementSize = stringConveyor.GetSize(_row.DataTypes[index].Name);
									stringConveyor.Write(_row.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
									offset += elementSize;
								}
								#endif

								conveyor = Manager.GetConveyor(scalarType);
								if (conveyor.IsStreaming)
								{
									stream = (Stream)_writeList[index];
									int32Conveyor.Write(Convert.ToInt32(stream.Length), buffer, offset); // Write the length of the value
									offset += sizeof(int);
									stream.Read(buffer, offset, (int)stream.Length); // Write the value of this scalar
									offset += (int)stream.Length;
								}
								else
								{
									elementSize = (int)_writeList[index]; // Write the length of the value
									int32Conveyor.Write(elementSize, buffer, offset);
									offset += sizeof(int);
									conveyor.Write(_row.Values[index], buffer, offset); // Write the value of this scalar
									offset += elementSize;
								}
							}
						}
						else
						{
							#if USEDATATYPESINNATIVEROW
							if (DataType.Columns[index].DataType.Equals(_row.DataTypes[index]))
							{
							#endif
								buffer[offset] = (byte)2; // Write the native standard value indicator
								offset++;
							#if USEDATATYPESINNATIVEROW
							}
							else
							{
								buffer[offset] = (byte)4; // Write the native specialized value indicator
								offset++;
								if (stringConveyor == null)
									stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
								elementSize = stringConveyor.GetSize(_row.DataTypes[index].Name);
								stringConveyor.Write(_row.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
								offset += elementSize;
							}
							#endif

							element = _elementWriteList[index];
							elementSize = (int)_writeList[index];
							int32Conveyor.Write(elementSize, buffer, offset);
							offset += sizeof(int);
							element.WriteToPhysical(buffer, offset, expandStreams); // Write the physical representation of the value;
							offset += elementSize;
							element.Dispose();
						}
					}
				}
			}
		}

		public override void ReadFromPhysical(byte[] buffer, int offset)
		{
			ClearValues(); // Clear the current value of the row
			
			if (buffer[offset] == 0)
			{
				_row = null;
			}
			else
			{
				_row = new NativeRow(DataType.Columns.Count);
				offset++;
			
				bool expandStreams = buffer[offset] != 0; // Read the exapnded streams indicator
				offset++;
					
				#if USEDATATYPESINNATIVEROW
				Streams.IConveyor stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
				string dataTypeName;
				Schema.IDataType dataType;
				#endif
				Streams.IConveyor int64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
				Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);

				Stream stream;
				StreamID streamID;
				int elementSize;
				Schema.IScalarType scalarType;
				Streams.IConveyor conveyor;
				for (int index = 0; index < DataType.Columns.Count; index++)
				{
					byte valueIndicator = buffer[offset];
					offset++;
					
					switch (valueIndicator)
					{
						case 0 : // native nil
							#if USEDATATYPESINNATIVEROW
							_row.DataTypes[index] = DataType.Columns[index].DataType;
							#endif
							_row.Values[index] = null;
						break;
						
						case 1 : // non-native nil
							#if USEDATATYPESINNATIVEROW
							_row.DataTypes[index] = DataType.Columns[index].DataType;
							#endif
							_row.Values[index] = StreamID.Null;
						break;
						
						case 2 : // native standard value
							scalarType = DataType.Columns[index].DataType as Schema.IScalarType;
							if ((scalarType != null) && !scalarType.IsCompound)
							{
								conveyor = Manager.GetConveyor(scalarType);
								if (conveyor.IsStreaming)
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									stream = new MemoryStream(buffer, offset, elementSize, false, true);
									#if USEDATATYPESINNATIVEROW
									_row.DataTypes[index] = DataType.Columns[index].DataType;
									#endif
									_row.Values[index] = conveyor.Read(stream);
									offset += elementSize;
								}
								else
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									#if USEDATATYPESINNATIVEROW
									_row.DataTypes[index] = DataType.Columns[index].DataType;
									#endif
									_row.Values[index] = conveyor.Read(buffer, offset);
									offset += elementSize;
								}
							}
							else
							{
								elementSize = (int)int32Conveyor.Read(buffer, offset);
								offset += sizeof(int);
								#if USEDATATYPESINNATIVEROW
								_row.DataTypes[index] = DataType.Columns[index].DataType;
								#endif
								using (IDataValue tempValue = DataValue.FromPhysical(Manager, DataType.Columns[index].DataType, buffer, offset))
								{
									_row.Values[index] = tempValue.AsNative;
									tempValue.ValuesOwned = false;
								}
								offset += elementSize;
							}
						break;
						
						case 3 : // non-native standard value
							scalarType = DataType.Columns[index].DataType as Schema.IScalarType;
							if (scalarType != null)
							{
								if (expandStreams)
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									streamID = Manager.StreamManager.Allocate();
									stream = Manager.StreamManager.Open(streamID, LockMode.Exclusive);
									stream.Write(buffer, offset, elementSize);
									stream.Close();
									#if USEDATATYPESINNATIVEROW
									_row.DataTypes[index] = scalarType;
									#endif
									_row.Values[index] = streamID;
									offset += elementSize;
								}
								else
								{
									#if USEDATATYPESINNATIVEROW
									_row.DataTypes[index] = scalarType;
									#endif
									_row.Values[index] = new StreamID(Convert.ToUInt64(int64Conveyor.Read(buffer, offset)));
									offset += sizeof(long);
								}
							}
							else
							{
								// non-scalar values cannot be non-native
							}
						break;
						
						case 4 : // native specialized value
							#if USEDATATYPESINNATIVEROW
							dataTypeName = (string)stringConveyor.Read(buffer, offset);
							dataType = Manager.CompileTypeSpecifier(dataTypeName);
							offset += stringConveyor.GetSize(dataTypeName);
							scalarType = dataType as Schema.IScalarType;
							if ((scalarType != null) && !scalarType.IsCompound)
							{
								conveyor = Manager.GetConveyor(scalarType);
								if (conveyor.IsStreaming)
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									stream = new MemoryStream(buffer, offset, elementSize, false, true);
									_row.DataTypes[index] = scalarType;
									_row.Values[index] = conveyor.Read(stream);
									offset += elementSize;
								}
								else
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									_row.DataTypes[index] = scalarType;
									_row.Values[index] = conveyor.Read(buffer, offset);
									offset += elementSize;
								}
							}
							else
							{
								elementSize = (int)int32Conveyor.Read(buffer, offset);
								offset += sizeof(int);
								_row.DataTypes[index] = dataType;
								using (IDataValue tempValue = DataValue.FromPhysical(Manager, dataType, buffer, offset))
								{
									_row.Values[index] = tempValue.AsNative;
									tempValue.ValuesOwned = false;
								}
								offset += elementSize;
							}
						break;
							#else
							throw new NotSupportedException("Specialized data types in rows are not supported");
							#endif
						
						case 5 : // non-native specialized value
							#if USEDATATYPESINNATIVEROW
							dataTypeName = (string)stringConveyor.Read(buffer, offset);
							dataType = Manager.CompileTypeSpecifier(dataTypeName);
							offset += stringConveyor.GetSize(dataTypeName);
							scalarType = dataType as Schema.IScalarType;
							if (scalarType != null)
							{
								if (expandStreams)
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									streamID = Manager.StreamManager.Allocate();
									stream = Manager.StreamManager.Open(streamID, LockMode.Exclusive);
									stream.Write(buffer, offset, elementSize);
									stream.Close();
									_row.DataTypes[index] = scalarType;
									_row.Values[index] = streamID;
									offset += elementSize;
								}
								else
								{
									_row.DataTypes[index] = scalarType;
									_row.Values[index] = new StreamID(Convert.ToUInt64(int64Conveyor.Read(buffer, offset)));
									offset += sizeof(long);
								}
							}
							else
							{
								// non-scalar values cannot be non-native
							}
						break;
							#else
							throw new NotSupportedException("Specialized data types in rows are not supported");
							#endif
					}
				}
			}
		}
		
		public bool HasNonNativeValue(int index)
		{
			#if USEDATATYPESINNATIVEROW
			return (_row.DataTypes[index] is Schema.IScalarType) && (_row.Values[index] is StreamID);
			#else
			return (DataType.Columns[AIndex].DataType is Schema.IScalarType) && (FRow.Values[AIndex] is StreamID);
			#endif
		}
		
		public StreamID GetNonNativeStreamID(int index)
		{
			return (StreamID)_row.Values[index];
		}
		
		/// <summary>This is a by-reference access of the value, changes made to the resulting DataValue will be refelected in the actual row.</summary>
		public IDataValue GetValue(int index)
		{
			return FromNativeRow(Manager, DataType, _row, index);
		}
		
		public void SetValue(int index, IDataValue tempValue)
		{
			this[index] = tempValue;
		}
		
		/// <summary>
		/// Returns the native representation if the value is stored in non-native form (as a StreamID)
		/// </summary>
		public object GetNativeValue(int index)
		{
			// TODO: This should recursively ensure that no contained values are non-native
			if (HasNonNativeValue(index))
				return GetValue(index).AsNative;
				
			return this[index];
		}

		/// <summary>This is a by-reference access of the value, changes made to the resulting DataValue will be refelected in the actual row.</summary>
		public object this[int index]
		{
			get 
			{
				#if USEDATATYPESINNATIVEROW
				if (_row.DataTypes[index] is Schema.IScalarType)
				#else
				if (DataType.Columns[AIndex].DataType is Schema.IScalarType)
				#endif
					return _row.Values[index];
				return FromNativeRow(Manager, DataType, _row, index); 
			}
			set
			{
				if (_row.Values[index] != null)
					#if USEDATATYPESINNATIVEROW
					DataValue.DisposeNative(Manager, _row.DataTypes[index], _row.Values[index]);
					#else
					DataValue.DisposeNative(Manager, DataType.Columns[AIndex].DataType, FRow.Values[AIndex]);
					#endif
					
				IDataValue tempValue = value as IDataValue;
				if (tempValue != null)
				{
					#if USEDATATYPESINNATIVEROW
					_row.DataTypes[index] = tempValue.DataType;
					#endif
					_row.Values[index] = tempValue.CopyNative();
				}
				else if (value != null)
				{
					#if USEDATATYPESINNATIVEROW
					if ((DataType.Columns[index].DataType.Equals(Manager.DataTypes.SystemGeneric)) || (DataType.Columns[index].DataType.Equals(Manager.DataTypes.SystemScalar)))
						_row.DataTypes[index] = DataValue.NativeTypeToScalarType(Manager, value.GetType());
					else
						_row.DataTypes[index] = DataType.Columns[index].DataType;
					_row.Values[index] = DataValue.CopyNative(Manager, _row.DataTypes[index], value);
					#else
					FRow.Values[AIndex] = DataValue.CopyNative(Manager, DataType.Columns[AIndex].DataType, value);
					#endif
				}
				else
				{
					#if USEDATATYPESINNATIVEROW
					_row.DataTypes[index] = DataType.Columns[index].DataType;
					#endif
					_row.Values[index] = null;
				}
				
				if (_row.ModifiedFlags != null)
					_row.ModifiedFlags[index] = true;
			}
		}
		
		private int _modifiedContextCount;

		public void BeginModifiedContext()
		{
			if (_modifiedContextCount == 0)
			{
				_row.ModifiedFlags = new BitArray(_row.Values.Length);
				_row.ModifiedFlags.SetAll(false);
			}
			_modifiedContextCount++;
		}
		
		public BitArray EndModifiedContext()
		{
			_modifiedContextCount--;
			if (_modifiedContextCount == 0)
			{
				BitArray result = _row.ModifiedFlags;
				_row.ModifiedFlags = null;
				return result;
			}
			return _row.ModifiedFlags;
		}
		
		/// <summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.</summary>
		public int IndexOfColumn(string columnName)
		{
			return DataType.Columns.IndexOfColumn(columnName);
		}

		///	<summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.  Throws an exception if the column name is not found.</summary>
		public int GetIndexOfColumn(string columnName)
		{
			return DataType.Columns.GetIndexOfColumn(columnName);
		}
		
		public object this[string columnName]
		{
			get { return this[GetIndexOfColumn(columnName)]; }
			set { this[GetIndexOfColumn(columnName)] = value; }
		}
		
		public IDataValue GetValue(string columnName)
		{
			return GetValue(GetIndexOfColumn(columnName));
		}
		
		public void SetValue(string columnName, IDataValue tempValue)
		{
			SetValue(GetIndexOfColumn(columnName), tempValue);
		}

		public bool HasValue(int index)
		{
			return _row != null && _row.Values[index] != null;
		}
		
		public bool HasValue(string columnName)
		{
			return HasValue(GetIndexOfColumn(columnName));
		}
		
		public bool HasNils()
		{
			for (int index = 0; index < DataType.Columns.Count; index++)
				if (!HasValue(index))
					return true;
			return false;
		}
		
		public bool HasAnyNoValues()
		{
			for (int index = 0; index < DataType.Columns.Count; index++)
				if (!HasValue(index))
					return true;
			return false;
		}
		
		public bool HasNonNativeValues()
		{
			for (int index = 0; index < DataType.Columns.Count; index++)
				if (HasNonNativeValue(index))
					return true;
			return false;
		}
		
		public override bool IsNil { get { return _row == null; } }

		public void ClearValue(int index)
		{
			if (_row.Values[index] != null)
			{
				if (ValuesOwned)
					#if USEDATATYPESINNATIVEROW
					DataValue.DisposeNative(Manager, _row.DataTypes[index], _row.Values[index]);
					#else
					DataValue.DisposeNative(Manager, DataType.Columns[AIndex].DataType, FRow.Values[AIndex]);
					#endif
				
				#if USEDATATYPESINNATIVEROW
				_row.DataTypes[index] = DataType.Columns[index].DataType;
				#endif
				_row.Values[index] = null;
			}
		}

		public void ClearValue(string columnName)
		{
			ClearValue(GetIndexOfColumn(columnName));
		}
		
		public void ClearValues()
		{
			for (int index = 0; index < DataType.Columns.Count; index++)
				ClearValue(index);
		}
		
		/// <summary>Returns an array of ARow.DataType.Columns.Count boolean values indicating whether each column in ARow has a value.</summary>
		public BitArray GetValueFlags()
		{
			BitArray valueFlags = new BitArray(DataType.Columns.Count);
			for (int index = 0; index < DataType.Columns.Count; index++)
				valueFlags[index] = HasValue(index);
			return valueFlags;
		}
		
		public override object CopyNativeAs(Schema.IDataType dataType)
		{
			if (_row == null)
				return null;
				
			if (Object.ReferenceEquals(DataType, dataType))
			{
				NativeRow newRow = new NativeRow(DataType.Columns.Count);
				if (_row != null)
					for (int index = 0; index < DataType.Columns.Count; index++)
					{
						#if USEDATATYPESINNATIVEROW
						newRow.DataTypes[index] = _row.DataTypes[index];
						newRow.Values[index] = CopyNative(Manager, _row.DataTypes[index], _row.Values[index]);
						#else
						newRow.Values[index] = CopyNative(Manager, DataType.Columns[index].DataType, FRow.Values[index]);
						#endif
					}
				return newRow;
			}
			else
			{
				NativeRow newRow = new NativeRow(DataType.Columns.Count);
				Schema.IRowType newRowType = (Schema.IRowType)dataType;
				if (_row != null)
					for (int index = 0; index < DataType.Columns.Count; index++)
					{
						int newIndex = newRowType.Columns.IndexOfName(DataType.Columns[index].Name);
						#if USEDATATYPESINNATIVEROW
						newRow.DataTypes[newIndex] = _row.DataTypes[index];
						newRow.Values[newIndex] = CopyNative(Manager, _row.DataTypes[index], _row.Values[index]);
						#else
						newRow.Values[newIndex] = CopyNative(Manager, DataType.Columns[index].DataType, FRow.Values[index]);
						#endif
					}
				return newRow;
			}
		}

		public void CopyTo(IRow row)
		{
			int columnIndex;
			for (int index = 0; index < DataType.Columns.Count; index++)
			{
				columnIndex = row.IndexOfColumn(DataType.Columns[index].Name);
				if (columnIndex >= 0)
					if (HasValue(index))
						row[columnIndex] = this[index];
					else
						row.ClearValue(columnIndex);
			}
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("row { ");
			for (int index = 0; index < DataType.Columns.Count; index++)
			{
				if (index > 0)
					result.Append(", ");
				result.Append(GetValue(index).ToString());
			}
			if (DataType.Columns.Count > 0)
				result.Append(" ");
			result.Append("}");
			return result.ToString();
		}
	}
}
