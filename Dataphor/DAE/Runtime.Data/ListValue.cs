/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Streams;

	public class DataTypeList : System.Object
	{
		public DataTypeList() : base() {}
		
		private List<Schema.IDataType> _dataTypes = new List<Schema.IDataType>();

		public Schema.IDataType this[int index]
		{
			get { return _dataTypes[index]; }
			set { _dataTypes[index] = value; }
		}
		
		public int Count { get { return _dataTypes.Count; } }
		
		public void Add(Schema.IDataType dataType)
		{
			_dataTypes.Add(dataType);
		}
		
		public void Insert(int index, Schema.IDataType dataType)
		{
			_dataTypes.Insert(index, dataType);
		}
		
		public void RemoveAt(int index)
		{
			_dataTypes.RemoveAt(index);
		}

		public void Clear()
		{
			_dataTypes.Clear();
		}
	}
	
	public class NativeList : System.Object, IList
	{
		public NativeList() : base() {}
		
		private DataTypeList _dataTypes = new DataTypeList();
		public DataTypeList DataTypes { get { return _dataTypes; } }
		
		private List<object> _values = new List<object>();
		public List<object> Values { get { return _values; } }

		int IList.Add(object value)
		{
			_dataTypes.Add(null);
			_values.Add(value);
			return _values.Count - 1;
		}

		void IList.Clear()
		{
			_dataTypes.Clear();
			_values.Clear();
		}

		bool IList.Contains(object value)
		{
			return _values.Contains(value);
		}

		int IList.IndexOf(object value)
		{
			return _values.IndexOf(value);
		}

		void IList.Insert(int index, object value)
		{
			_dataTypes.Insert(index, null);
			_values.Insert(index, value);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		void IList.Remove(object value)
		{
			int index = ((IList)this).IndexOf(value);
			if (index >= 0)
				((IList)this).RemoveAt(index);
		}

		void IList.RemoveAt(int index)
		{
			_dataTypes.RemoveAt(index);
			_values.RemoveAt(index);
		}

		object IList.this[int index]
		{
			get
			{
				return _values[index];
			}
			set
			{
				_dataTypes[index] = null;
				_values[index] = value;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		int ICollection.Count
		{
			get { return _values.Count; }
		}

		bool ICollection.IsSynchronized
		{
			get { return false; }
		}

		object ICollection.SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _values.GetEnumerator();
		}
	}
	
	public class ListValue : DataValue, IList
	{
		public ListValue(IValueManager manager, Schema.IListType dataType) : base(manager, dataType) 
		{
			_list = new NativeList();
		}

		public ListValue(IValueManager manager, Schema.IListType dataType, NativeList list) : base(manager, dataType)
		{
			_list = list;
		}
		
		public ListValue(IValueManager manager, Schema.IListType dataType, IEnumerable sourceList) : base(manager, dataType)
		{
			_list = new NativeList();
			foreach (object objectValue in sourceList)
				Add(objectValue);
		}

		private NativeList _list;
		
		public new Schema.IListType DataType { get { return (Schema.IListType)base.DataType; } }
		
		public override object AsNative
		{
			get { return _list; }
			set 
			{ 
				if (_list != null)
					Clear();
				_list = (NativeList)value; 
			}
		}
		
		public override bool IsNil { get { return _list == null; } }
		
		private object[] _writeList;
		private IDataValue[] _elementWriteList;
		
		/*
			List Value Format ->
			
				00 -> 0 - Indicates that the list is nil, no data follows 1 - Indicates that the list is non-nil, data follows as specified
				01 -> 0 - Indicates that non-native values are stored as a StreamID 1 - Indicates non-native values are stored inline
				02-05 -> Number of elements in the list
				06-XX -> N List Elements
				
			List Element Format ->

				There are five possibilities for an element ->			
					Nil Native
					Nil Non-Native (StreamID.Null)
					Standard Native
					Standard Non-Native
					Specialized Native
					Specialized Non-Native
				
					For non-native values, the value will be expanded or not dependending on the expanded setting for the list value

				00	-> 0 - 5
					0 if the list contains a native nil for this element - no data follows
					1 if the list contains a non-native nil for this element - no data follows
					2 if the list contains a native value of the element type of the list
						01-04 -> The length of the physical representation of this value
						05-XX -> The physical representation of this value
					3 if the list contains a non-native value of the element type of the list
						01-04 -> The length of the physical representation of this value
						05-XX -> The physical representation of this value (expanded based on the expanded setting for the list value)
					4 if the list contains a native value of some specialization of the element type of the list
						01-XX -> The data type name of this value, stored using a StringConveyor
						XX+1-XX+4 -> The length of the physical representation of this value
						XX+5-YY -> The physical representation of this value
					5 if the list contains a non-native value of some specialization of the element type of the list
						01-XX -> The data type name of this value, stored using a StringConveyor
						XX+1-XX+4 -> The lnegth of the physical representation of this value
						XX+5-YY -> The physical representation of this value (expanded based on the expanded setting for the list value)
		*/

		public override int GetPhysicalSize(bool expandStreams)
		{
			int size = 1; // write the value indicator
			if (!IsNil)
			{
				size += sizeof(int) + 1; // write the extended streams indicator and the number of elements in the list
				_writeList = new object[Count()]; // list for saving the sizes or streams of each element in the list
				_elementWriteList = new DataValue[Count()]; // list for saving host representations of values between the GetPhysicalSize and WriteToPhysical calls
				Stream stream;
				StreamID streamID;
				Schema.IScalarType scalarType;
				IDataValue element;
				int elementSize;
				for (int index = 0; index < _writeList.Length; index++)
				{
					size += sizeof(byte); // write a value indicator
					if (_list.Values[index] != null)
					{
						if (!DataType.ElementType.Equals(_list.DataTypes[index]))
							size += Manager.GetConveyor(Manager.DataTypes.SystemString).GetSize(_list.DataTypes[index].Name); // write the name of the data type of the value
							
						scalarType = _list.DataTypes[index] as Schema.IScalarType;
						if ((scalarType != null) && !scalarType.IsCompound)
						{
							if (_list.Values[index] is StreamID)
							{
								// If this is a non-native scalar
								streamID = (StreamID)_list.Values[index];
								if (expandStreams)
								{
									if (streamID != StreamID.Null)
									{
										stream = Manager.StreamManager.Open((StreamID)_list.Values[index], LockMode.Exclusive);
										_writeList[index] = stream;
										size += sizeof(int) + (int)stream.Length;
									}
								}
								else
								{
									if (streamID != StreamID.Null)
									{
										elementSize = StreamID.CSizeOf;
										_writeList[index] = elementSize;
										size += elementSize;
									}
								}
							}
							else
							{
								Streams.IConveyor conveyor = Manager.GetConveyor(scalarType);
								if (conveyor.IsStreaming)
								{
									stream = new MemoryStream(64);
									_writeList[index] = stream;
									conveyor.Write(_list.Values[index], stream);
									stream.Position = 0;
									size += sizeof(int) + (int)stream.Length;
								}
								else
								{
									elementSize = conveyor.GetSize(_list.Values[index]);
									_writeList[index] = elementSize;
									size += sizeof(int) + elementSize;;
								}
							}
						}
						else
						{
							element = DataValue.FromNativeList(Manager, DataType, _list, index);
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
				
			buffer[offset] = (byte)(expandStreams ? 0 : 1); // Write the expanded streams indicator
			offset++;
				
			Streams.IConveyor stringConveyor = null;
			Streams.IConveyor int64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
			Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
			int32Conveyor.Write(Count(), buffer, offset); // Write the number of elements in the list
			offset += sizeof(int);

			Stream stream;
			StreamID streamID;
			int elementSize;
			Schema.IScalarType scalarType;
			Streams.IConveyor conveyor;
			IDataValue element;
			for (int index = 0; index < _writeList.Length; index++)
			{
				if (_list.Values[index] == null)
				{
					buffer[offset] = (byte)0; // Write the native nil indicator
					offset++;
				}
				else
				{
					scalarType = _list.DataTypes[index] as Schema.IScalarType;
					if ((scalarType != null) && !scalarType.IsCompound)
					{
						if (_list.Values[index] is StreamID)
						{
							// If this is a non-native scalar
							streamID = (StreamID)_list.Values[index];
							if (streamID == StreamID.Null)
							{
								buffer[offset] = (byte)1; // Write the non-native nil indicator
								offset++;
							}
							else
							{
								if (DataType.ElementType.Equals(_list.DataTypes[index]))
								{
									buffer[offset] = (byte)3; // Write the native standard value indicator
									offset++;
								}
								else
								{
									buffer[offset] = (byte)5; // Write the native specialized value indicator
									offset++;
									if (stringConveyor == null)
										stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
									elementSize = stringConveyor.GetSize(_list.DataTypes[index].Name);
									stringConveyor.Write(_list.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
									offset += elementSize;
								}
								
								if (expandStreams)
								{
									stream = (Stream)_writeList[index];
									int32Conveyor.Write(Convert.ToInt32(stream.Length), buffer, offset);
									offset += sizeof(int);
									stream.Read(buffer, offset, (int)stream.Length);
									offset += (int)stream.Length;
								}
								else
								{
									int64Conveyor.Write(streamID.Value, buffer, offset);
									offset += sizeof(long);
								}
							}
						}
						else
						{
							if (DataType.ElementType.Equals(_list.DataTypes[index]))
							{
								buffer[offset] = (byte)2; // Write the native standard value indicator
								offset++;
							}
							else
							{
								buffer[offset] = (byte)4; // Write the native specialized value indicator
								offset++;
								if (stringConveyor == null)
									stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
								elementSize = stringConveyor.GetSize(_list.DataTypes[index].Name);
								stringConveyor.Write(_list.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
								offset += elementSize;
							}

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
								conveyor.Write(_list.Values[index], buffer, offset); // Write the value of this scalar
								offset += elementSize;
							}
						}
					}
					else
					{
						if (DataType.ElementType.Equals(_list.DataTypes[index]))
						{
							buffer[offset] = (byte)2; // Write the native standard value indicator
							offset++;
						}
						else
						{
							buffer[offset] = (byte)4; // Write the native specialized value indicator
							offset++;
							if (stringConveyor == null)
								stringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
							elementSize = stringConveyor.GetSize(_list.DataTypes[index].Name);
							stringConveyor.Write(_list.DataTypes[index].Name, buffer, offset); // Write the name of the data type of the value
							offset += elementSize;
						}

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

		public override void ReadFromPhysical(byte[] buffer, int offset)
		{
			Clear(); // Clear the current value of the list
			
			if (buffer[offset] == 0)
			{
				_list = null;
			}
			else
			{
				_list = new NativeList();
				if (buffer[offset] != 0)
				{
					offset++;

					bool expandStreams = buffer[offset] != 0; // Read the exapnded streams indicator
					offset++;
						
					Streams.IConveyor stringConveyor = null;
					Streams.IConveyor int64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
					Streams.IConveyor int32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);
					int count = (int)int32Conveyor.Read(buffer, offset); // Read the number of elements in the list
					offset += sizeof(int);

					Stream stream;
					StreamID streamID;
					int elementSize;
					string dataTypeName;
					Schema.IDataType dataType;
					Schema.IScalarType scalarType;
					Streams.IConveyor conveyor;
					for (int index = 0; index < count; index++)
					{
						byte valueIndicator = buffer[offset];
						offset++;
						
						switch (valueIndicator)
						{
							case 0 : // native nil
								_list.DataTypes.Add(DataType.ElementType);
								_list.Values.Add(null);
							break;
							
							case 1 : // non-native nil
								_list.DataTypes.Add(DataType.ElementType);
								_list.Values.Add(StreamID.Null);
							break;
							
							case 2 : // native standard value
								scalarType = DataType.ElementType as Schema.IScalarType;
								if ((scalarType != null) && !scalarType.IsCompound)
								{
									conveyor = Manager.GetConveyor(scalarType);
									if (conveyor.IsStreaming)
									{
										elementSize = (int)int32Conveyor.Read(buffer, offset);
										offset += sizeof(int);
										stream = new MemoryStream(buffer, offset, elementSize, false, true);
										_list.DataTypes.Add(DataType.ElementType);
										_list.Values.Add(conveyor.Read(stream));
										offset += elementSize;
									}
									else
									{
										elementSize = (int)int32Conveyor.Read(buffer, offset);
										offset += sizeof(int);
										_list.DataTypes.Add(DataType.ElementType);
										_list.Values.Add(conveyor.Read(buffer, offset));
										offset += elementSize;
									}
								}
								else
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									_list.DataTypes.Add(DataType.ElementType);
									using (IDataValue tempValue = DataValue.FromPhysical(Manager, DataType.ElementType, buffer, offset))
									{
										_list.Values.Add(tempValue.AsNative);
										tempValue.ValuesOwned = false;
									}
									offset += elementSize;
								}
							break;
							
							case 3 : // non-native standard value
								scalarType = DataType.ElementType as Schema.IScalarType;
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
										_list.DataTypes.Add(scalarType);
										_list.Values.Add(streamID);
										offset += elementSize;
									}
									else
									{
										_list.DataTypes.Add(scalarType);
										_list.Values.Add(int64Conveyor.Read(buffer, offset));
										offset += sizeof(long);
									}
								}
								else
								{
									// non-scalar values cannot be non-native
								}
							break;
							
							case 4 : // native specialized value
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
										_list.DataTypes.Add(scalarType);
										_list.Values.Add(conveyor.Read(stream));
										offset += elementSize;
									}
									else
									{
										elementSize = (int)int32Conveyor.Read(buffer, offset);
										offset += sizeof(int);
										_list.DataTypes.Add(DataType.ElementType);
										_list.Values.Add(conveyor.Read(buffer, offset));
										offset += elementSize;
									}
								}
								else
								{
									elementSize = (int)int32Conveyor.Read(buffer, offset);
									offset += sizeof(int);
									_list.DataTypes.Add(dataType);
									using (IDataValue tempValue = DataValue.FromPhysical(Manager, dataType, buffer, offset))
									{
										_list.Values.Add(tempValue.AsNative);
										tempValue.ValuesOwned = false;
									}
									offset += elementSize;
								}
							break;
							
							case 5 : // non-native specialized value
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
										_list.DataTypes.Add(scalarType);
										_list.Values.Add(streamID);
										offset += elementSize;
									}
									else
									{
										_list.DataTypes.Add(scalarType);
										_list.Values.Add(int64Conveyor.Read(buffer, offset));
										offset += sizeof(long);
									}
								}
								else
								{
									// non-scalar values cannot be non-native
								}
							break;
						}
					}
				}
			}
		}
		
		public IDataValue GetValue(int index)
		{
			return FromNativeList(Manager, DataType, _list, index);
		}
		
		public void SetValue(int index, DataValue tempValue)
		{
			this[index] = tempValue;
		}
		
		public object this[int index]
		{
			get 
			{ 
				if (_list.DataTypes[index] is Schema.IScalarType)
					return _list.Values[index];
				return FromNativeList(Manager, DataType, _list, index); 
			}
			set
			{
				DisposeValueAt(index);
				
				DataValue tempValue = value as DataValue;
				if (tempValue != null)
				{
					_list.DataTypes[index] = tempValue.DataType;
					_list.Values[index] = tempValue.CopyNative();
				}
				else
				{
					_list.DataTypes[index] = DataType.ElementType;
					_list.Values[index] = value;
				}
			}
		}
		
		private void DisposeValueAt(int index)
		{
			if (_list.Values[index] != null)
			{
				DataValue.DisposeNative(Manager, _list.DataTypes[index], _list.Values[index]);
			}
		}
		
		public int Add(object tempValue)
		{
			int index = _list.DataTypes.Count;
			DataValue localTempValue = tempValue as DataValue;
			if (localTempValue != null)
			{
				_list.DataTypes.Add(localTempValue.DataType);
				_list.Values.Add(localTempValue.CopyNative());
			}
			else
			{
				_list.DataTypes.Add(DataType.ElementType);
				_list.Values.Add(tempValue);
			}
			return index;
		}
		
		public void Insert(int index, object tempValue)
		{
			DataValue localTempValue = tempValue as DataValue;
			if (localTempValue != null)
			{
				_list.DataTypes.Insert(index, localTempValue.DataType);
				_list.Values.Insert(index, localTempValue.CopyNative());
			}
			else
			{
				_list.DataTypes.Insert(index, DataType.ElementType);
				_list.Values.Insert(index, tempValue);
			}
		}
		
		public void RemoveAt(int index)
		{
			DisposeValueAt(index);
			_list.DataTypes.RemoveAt(index);
			_list.Values.RemoveAt(index);
		}
		
		public void Clear()
		{
			while (_list.DataTypes.Count > 0)
				RemoveAt(_list.DataTypes.Count - 1);
		}
		
		public int Count()
		{
			return _list.DataTypes.Count;
		}
		
		public List<T> ToList<T>()
		{
			List<T> list = new List<T>();
			for (int index = 0; index < Count(); index++)
				list.Add((T)this[index]);
			return list;
		}

		public override object CopyNativeAs(Schema.IDataType dataType)
		{
			NativeList newList = new NativeList();
			for (int index = 0; index < _list.DataTypes.Count; index++)
			{
				newList.DataTypes.Add(_list.DataTypes[index]);
				newList.Values.Add(DataValue.CopyNative(Manager, _list.DataTypes[index], _list.Values[index]));
			}
			return newList;
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.AppendFormat("list({0}) {{ ", this.DataType.ElementType.Name);
			for (int index = 0; index < Count(); index++)
			{
				if (index > 0)
					result.Append(", ");
				result.Append(GetValue(index).ToString());
			}
			if (Count() > 0)
				result.Append(" ");
			result.Append("}");
			return result.ToString();
		}

		int IList.Add(object value)
		{
			return Add(value);
		}

		void IList.Clear()
		{
			Clear();
		}

		bool IList.Contains(object value)
		{
			throw new NotImplementedException();
		}

		int IList.IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		void IList.Insert(int index, object value)
		{
			this.Insert(index, value);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		bool IList.IsReadOnly
		{
			get { return false; }
		}

		void IList.Remove(object value)
		{
			throw new NotImplementedException();
		}

		void IList.RemoveAt(int index)
		{
			this.RemoveAt(index);
		}

		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				this[index] = value;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		int ICollection.Count
		{
			get { return this.Count(); }
		}

		bool ICollection.IsSynchronized
		{
			get { throw new NotImplementedException(); }
		}

		object ICollection.SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
