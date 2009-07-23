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
	
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Streams;
	using System.Text;
	
	public class DataTypeList : System.Object
	{
		public DataTypeList() : base() {}
		
		private ArrayList FDataTypes = new ArrayList();

		public Schema.IDataType this[int AIndex]
		{
			get { return (Schema.IDataType)FDataTypes[AIndex]; }
			set { FDataTypes[AIndex] = value; }
		}
		
		public int Count { get { return FDataTypes.Count; } }
		
		public void Add(Schema.IDataType ADataType)
		{
			FDataTypes.Add(ADataType);
		}
		
		public void Insert(int AIndex, Schema.IDataType ADataType)
		{
			FDataTypes.Insert(AIndex, ADataType);
		}
		
		public void RemoveAt(int AIndex)
		{
			FDataTypes.RemoveAt(AIndex);
		}
	}
	
	public class NativeList : System.Object
	{
		public NativeList() : base() {}
		
		private DataTypeList FDataTypes = new DataTypeList();
		public DataTypeList DataTypes { get { return FDataTypes; } }
		
		private ArrayList FValues = new ArrayList();
		public ArrayList Values { get { return FValues; } }
	}
	
	public class ListValue : DataValue
	{
		public ListValue(IServerProcess AProcess, Schema.IListType ADataType) : base(AProcess, ADataType) 
		{
			FList = new NativeList();
		}

		public ListValue(IServerProcess AProcess, Schema.IListType ADataType, NativeList AList) : base(AProcess, ADataType)
		{
			FList = AList;
		}

		private NativeList FList;
		
		public new Schema.IListType DataType { get { return (Schema.IListType)base.DataType; } }
		
		public override object AsNative
		{
			get { return FList; }
			set 
			{ 
				if (FList != null)
					Clear();
				FList = (NativeList)value; 
			}
		}
		
		public override bool IsNil { get { return FList == null; } }
		
		private object[] FWriteList;
		private DataValue[] FElementWriteList;
		
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
		
		public unsafe override int GetPhysicalSize(bool AExpandStreams)
		{
			int LSize = 1; // write the value indicator
			if (!IsNil)
			{
				LSize += sizeof(int) + 1; // write the extended streams indicator and the number of elements in the list
				FWriteList = new object[Count()]; // list for saving the sizes or streams of each element in the list
				FElementWriteList = new DataValue[Count()]; // list for saving host representations of values between the GetPhysicalSize and WriteToPhysical calls
				Stream LStream;
				StreamID LStreamID;
				Schema.ScalarType LScalarType;
				DataValue LElement;
				int LElementSize;
				for (int LIndex = 0; LIndex < FWriteList.Length; LIndex++)
				{
					LSize += sizeof(byte); // write a value indicator
					if (FList.Values[LIndex] != null)
					{
						if (!DataType.ElementType.Equals(FList.DataTypes[LIndex]))
							LSize += Process.DataTypes.SystemString.GetConveyor(Process).GetSize(FList.DataTypes[LIndex].Name); // write the name of the data type of the value
							
						LScalarType = FList.DataTypes[LIndex] as Schema.ScalarType;
						if ((LScalarType != null) && !LScalarType.IsCompound)
						{
							if (FList.Values[LIndex] is StreamID)
							{
								// If this is a non-native scalar
								LStreamID = (StreamID)FList.Values[LIndex];
								if (AExpandStreams)
								{
									if (LStreamID != StreamID.Null)
									{
										LStream = Process.Open((StreamID)FList.Values[LIndex], LockMode.Exclusive);
										FWriteList[LIndex] = LStream;
										LSize += sizeof(int) + (int)LStream.Length;
									}
								}
								else
								{
									if (LStreamID != StreamID.Null)
									{
										LElementSize = sizeof(StreamID);
										FWriteList[LIndex] = LElementSize;
										LSize += LElementSize;
									}
								}
							}
							else
							{
								Streams.Conveyor LConveyor = LScalarType.GetConveyor(Process);
								if (LConveyor.IsStreaming)
								{
									LStream = new MemoryStream(64);
									FWriteList[LIndex] = LStream;
									LConveyor.Write(FList.Values[LIndex], LStream);
									LStream.Position = 0;
									LSize += sizeof(int) + (int)LStream.Length;
								}
								else
								{
									LElementSize = LConveyor.GetSize(FList.Values[LIndex]);
									FWriteList[LIndex] = LElementSize;
									LSize += sizeof(int) + LElementSize;;
								}
							}
						}
						else
						{
							LElement = DataValue.FromNativeList(Process, DataType, FList, LIndex);
							FElementWriteList[LIndex] = LElement;
							LElementSize = LElement.GetPhysicalSize(AExpandStreams);
							FWriteList[LIndex] = LElementSize;
							LSize += sizeof(int) + LElementSize;
						}						
					}
				}
			}
			return LSize;
		}
		
		public unsafe override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			if (FWriteList == null)
				throw new RuntimeException(RuntimeException.Codes.UnpreparedWriteToPhysicalCall);
				
			ABuffer[AOffset] = (byte)(IsNil ? 0 : 1); // Write the value indicator
			AOffset++;
				
			ABuffer[AOffset] = (byte)(AExpandStreams ? 0 : 1); // Write the expanded streams indicator
			AOffset++;
				
			Streams.Conveyor LStringConveyor = null;
			Streams.Conveyor LInt64Conveyor = Process.DataTypes.SystemLong.GetConveyor(Process);
			Streams.Conveyor LInt32Conveyor = Process.DataTypes.SystemInteger.GetConveyor(Process);
			LInt32Conveyor.Write(Count(), ABuffer, AOffset); // Write the number of elements in the list
			AOffset += sizeof(int);

			Stream LStream;
			StreamID LStreamID;
			int LElementSize;
			Schema.ScalarType LScalarType;
			Streams.Conveyor LConveyor;
			DataValue LElement;
			for (int LIndex = 0; LIndex < FWriteList.Length; LIndex++)
			{
				if (FList.Values[LIndex] == null)
				{
					ABuffer[AOffset] = (byte)0; // Write the native nil indicator
					AOffset++;
				}
				else
				{
					LScalarType = FList.DataTypes[LIndex] as Schema.ScalarType;
					if ((LScalarType != null) && !LScalarType.IsCompound)
					{
						if (FList.Values[LIndex] is StreamID)
						{
							// If this is a non-native scalar
							LStreamID = (StreamID)FList.Values[LIndex];
							if (LStreamID == StreamID.Null)
							{
								ABuffer[AOffset] = (byte)1; // Write the non-native nil indicator
								AOffset++;
							}
							else
							{
								if (DataType.ElementType.Equals(FList.DataTypes[LIndex]))
								{
									ABuffer[AOffset] = (byte)3; // Write the native standard value indicator
									AOffset++;
								}
								else
								{
									ABuffer[AOffset] = (byte)5; // Write the native specialized value indicator
									AOffset++;
									if (LStringConveyor == null)
										LStringConveyor = Process.DataTypes.SystemString.GetConveyor(Process);
									LElementSize = LStringConveyor.GetSize(FList.DataTypes[LIndex].Name);
									LStringConveyor.Write(FList.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
									AOffset += LElementSize;
								}
								
								if (AExpandStreams)
								{
									LStream = (Stream)FWriteList[LIndex];
									LInt32Conveyor.Write(Convert.ToInt32(LStream.Length), ABuffer, AOffset);
									AOffset += sizeof(int);
									LStream.Read(ABuffer, AOffset, (int)LStream.Length);
									AOffset += (int)LStream.Length;
								}
								else
								{
									LInt64Conveyor.Write(LStreamID.Value, ABuffer, AOffset);
									AOffset += sizeof(long);
								}
							}
						}
						else
						{
							if (DataType.ElementType.Equals(FList.DataTypes[LIndex]))
							{
								ABuffer[AOffset] = (byte)2; // Write the native standard value indicator
								AOffset++;
							}
							else
							{
								ABuffer[AOffset] = (byte)4; // Write the native specialized value indicator
								AOffset++;
								if (LStringConveyor == null)
									LStringConveyor = Process.DataTypes.SystemString.GetConveyor(Process);
								LElementSize = LStringConveyor.GetSize(FList.DataTypes[LIndex].Name);
								LStringConveyor.Write(FList.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
								AOffset += LElementSize;
							}

							LConveyor = LScalarType.GetConveyor(Process);
							if (LConveyor.IsStreaming)
							{
								LStream = (Stream)FWriteList[LIndex];
								LInt32Conveyor.Write(Convert.ToInt32(LStream.Length), ABuffer, AOffset); // Write the length of the value
								AOffset += sizeof(int);
								LStream.Read(ABuffer, AOffset, (int)LStream.Length); // Write the value of this scalar
								AOffset += (int)LStream.Length;
							}
							else
							{
								LElementSize = (int)FWriteList[LIndex]; // Write the length of the value
								LInt32Conveyor.Write(LElementSize, ABuffer, AOffset);
								AOffset += sizeof(int);
								LConveyor.Write(FList.Values[LIndex], ABuffer, AOffset); // Write the value of this scalar
								AOffset += LElementSize;
							}
						}
					}
					else
					{
						if (DataType.ElementType.Equals(FList.DataTypes[LIndex]))
						{
							ABuffer[AOffset] = (byte)2; // Write the native standard value indicator
							AOffset++;
						}
						else
						{
							ABuffer[AOffset] = (byte)4; // Write the native specialized value indicator
							AOffset++;
							if (LStringConveyor == null)
								LStringConveyor = Process.DataTypes.SystemString.GetConveyor(Process);
							LElementSize = LStringConveyor.GetSize(FList.DataTypes[LIndex].Name);
							LStringConveyor.Write(FList.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
							AOffset += LElementSize;
						}

						LElement = FElementWriteList[LIndex];
						LElementSize = (int)FWriteList[LIndex];
						LInt32Conveyor.Write(LElementSize, ABuffer, AOffset);
						AOffset += sizeof(int);
						LElement.WriteToPhysical(ABuffer, AOffset, AExpandStreams); // Write the physical representation of the value;
						AOffset += LElementSize;
						LElement.Dispose();
					}
				}
			}
		}

		public unsafe override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			Clear(); // Clear the current value of the list
			
			if (ABuffer[AOffset] == 0)
			{
				FList = null;
			}
			else
			{
				FList = new NativeList();
				if (ABuffer[AOffset] != 0)
				{
					AOffset++;

					bool LExpandStreams = ABuffer[AOffset] != 0; // Read the exapnded streams indicator
					AOffset++;
						
					Streams.Conveyor LStringConveyor = null;
					Streams.Conveyor LInt64Conveyor = Process.DataTypes.SystemLong.GetConveyor(Process);
					Streams.Conveyor LInt32Conveyor = Process.DataTypes.SystemInteger.GetConveyor(Process);
					int LCount = (int)LInt32Conveyor.Read(ABuffer, AOffset); // Read the number of elements in the list
					AOffset += sizeof(int);

					Stream LStream;
					StreamID LStreamID;
					int LElementSize;
					string LDataTypeName;
					Schema.IDataType LDataType;
					Schema.ScalarType LScalarType;
					Streams.Conveyor LConveyor;
					for (int LIndex = 0; LIndex < LCount; LIndex++)
					{
						byte LValueIndicator = ABuffer[AOffset];
						AOffset++;
						
						switch (LValueIndicator)
						{
							case 0 : // native nil
								FList.DataTypes.Add(DataType.ElementType);
								FList.Values.Add(null);
							break;
							
							case 1 : // non-native nil
								FList.DataTypes.Add(DataType.ElementType);
								FList.Values.Add(StreamID.Null);
							break;
							
							case 2 : // native standard value
								LScalarType = DataType.ElementType as Schema.ScalarType;
								if ((LScalarType != null) && !LScalarType.IsCompound)
								{
									LConveyor = LScalarType.GetConveyor(Process);
									if (LConveyor.IsStreaming)
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										LStream = new MemoryStream(ABuffer, AOffset, LElementSize, false, true);
										FList.DataTypes.Add(DataType.ElementType);
										FList.Values.Add(LConveyor.Read(LStream));
										AOffset += LElementSize;
									}
									else
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										FList.DataTypes.Add(DataType.ElementType);
										FList.Values.Add(LConveyor.Read(ABuffer, AOffset));
										AOffset += LElementSize;
									}
								}
								else
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									FList.DataTypes.Add(DataType.ElementType);
									using (DataValue LValue = DataValue.FromPhysical(Process, DataType.ElementType, ABuffer, AOffset))
									{
										FList.Values.Add(LValue.AsNative);
										LValue.ValuesOwned = false;
									}
									AOffset += LElementSize;
								}
							break;
							
							case 3 : // non-native standard value
								LScalarType = DataType.ElementType as Schema.ScalarType;
								if (LScalarType != null)
								{
									if (LExpandStreams)
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										LStreamID = Process.Allocate();
										LStream = Process.Open(LStreamID, LockMode.Exclusive);
										LStream.Write(ABuffer, AOffset, LElementSize);
										LStream.Close();
										FList.DataTypes.Add(LScalarType);
										FList.Values.Add(LStreamID);
										AOffset += LElementSize;
									}
									else
									{
										FList.DataTypes.Add(LScalarType);
										FList.Values.Add(LInt64Conveyor.Read(ABuffer, AOffset));
										AOffset += sizeof(long);
									}
								}
								else
								{
									// non-scalar values cannot be non-native
								}
							break;
							
							case 4 : // native specialized value
								LDataTypeName = (string)LStringConveyor.Read(ABuffer, AOffset);
								LDataType = Language.D4.Compiler.CompileTypeSpecifier(Process.GetServerProcess().Plan, new Language.D4.Parser().ParseTypeSpecifier(LDataTypeName));
								AOffset += LStringConveyor.GetSize(LDataTypeName);
								LScalarType = LDataType as Schema.ScalarType;
								if ((LScalarType != null) && !LScalarType.IsCompound)
								{
									LConveyor = LScalarType.GetConveyor(Process);
									if (LConveyor.IsStreaming)
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										LStream = new MemoryStream(ABuffer, AOffset, LElementSize, false, true);
										FList.DataTypes.Add(LScalarType);
										FList.Values.Add(LConveyor.Read(LStream));
										AOffset += LElementSize;
									}
									else
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										FList.DataTypes.Add(DataType.ElementType);
										FList.Values.Add(LConveyor.Read(ABuffer, AOffset));
										AOffset += LElementSize;
									}
								}
								else
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									FList.DataTypes.Add(LDataType);
									using (DataValue LValue = DataValue.FromPhysical(Process, LDataType, ABuffer, AOffset))
									{
										FList.Values.Add(LValue.AsNative);
										LValue.ValuesOwned = false;
									}
									AOffset += LElementSize;
								}
							break;
							
							case 5 : // non-native specialized value
								LDataTypeName = (string)LStringConveyor.Read(ABuffer, AOffset);
								LDataType = Language.D4.Compiler.CompileTypeSpecifier(Process.GetServerProcess().Plan, new Language.D4.Parser().ParseTypeSpecifier(LDataTypeName));
								AOffset += LStringConveyor.GetSize(LDataTypeName);
								LScalarType = LDataType as Schema.ScalarType;
								if (LScalarType != null)
								{
									if (LExpandStreams)
									{
										LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
										AOffset += sizeof(int);
										LStreamID = Process.Allocate();
										LStream = Process.Open(LStreamID, LockMode.Exclusive);
										LStream.Write(ABuffer, AOffset, LElementSize);
										LStream.Close();
										FList.DataTypes.Add(LScalarType);
										FList.Values.Add(LStreamID);
										AOffset += LElementSize;
									}
									else
									{
										FList.DataTypes.Add(LScalarType);
										FList.Values.Add(LInt64Conveyor.Read(ABuffer, AOffset));
										AOffset += sizeof(long);
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
		
		public DataValue GetValue(int AIndex)
		{
			return FromNativeList(Process, DataType, FList, AIndex);
		}
		
		public void SetValue(int AIndex, DataValue AValue)
		{
			this[AIndex] = AValue;
		}
		
		public object this[int AIndex]
		{
			get 
			{ 
				if (FList.DataTypes[AIndex] is Schema.IScalarType)
					return FList.Values[AIndex];
				return FromNativeList(Process, DataType, FList, AIndex); 
			}
			set
			{
				DisposeValueAt(AIndex);
				
				DataValue LValue = value as DataValue;
				if (LValue != null)
				{
					FList.DataTypes[AIndex] = LValue.DataType;
					FList.Values[AIndex] = LValue.CopyNative();
				}
				else
				{
					FList.DataTypes[AIndex] = DataType.ElementType;
					FList.Values[AIndex] = value;
				}
			}
		}
		
		private void DisposeValueAt(int AIndex)
		{
			if (FList.Values[AIndex] != null)
			{
				DataValue.DisposeNative(Process, FList.DataTypes[AIndex], FList.Values[AIndex]);
			}
		}
		
		public int Add(object AValue)
		{
			int LIndex = FList.DataTypes.Count;
			DataValue LValue = AValue as DataValue;
			if (LValue != null)
			{
				FList.DataTypes.Add(LValue.DataType);
				FList.Values.Add(LValue.CopyNative());
			}
			else
			{
				FList.DataTypes.Add(DataType.ElementType);
				FList.Values.Add(AValue);
			}
			return LIndex;
		}
		
		public void Insert(int AIndex, object AValue)
		{
			DataValue LValue = AValue as DataValue;
			if (LValue != null)
			{
				FList.DataTypes.Insert(AIndex, LValue.DataType);
				FList.Values.Insert(AIndex, LValue.CopyNative());
			}
			else
			{
				FList.DataTypes.Insert(AIndex, DataType.ElementType);
				FList.Values.Insert(AIndex, AValue);
			}
		}
		
		public void RemoveAt(int AIndex)
		{
			DisposeValueAt(AIndex);
			FList.DataTypes.RemoveAt(AIndex);
			FList.Values.RemoveAt(AIndex);
		}
		
		public void Clear()
		{
			while (FList.DataTypes.Count > 0)
				RemoveAt(FList.DataTypes.Count - 1);
		}
		
		public int Count()
		{
			return FList.DataTypes.Count;
		}

		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
			NativeList LNewList = new NativeList();
			for (int LIndex = 0; LIndex < FList.DataTypes.Count; LIndex++)
			{
				LNewList.DataTypes.Add(FList.DataTypes[LIndex]);
				LNewList.Values.Add(DataValue.CopyNative(Process, FList.DataTypes[LIndex], FList.Values[LIndex]));
			}
			return LNewList;
		}

		public override string ToString()
		{
			StringBuilder LResult = new StringBuilder();
			LResult.AppendFormat("list({0}) {{ ", this.DataType.ElementType.Name);
			for (int LIndex = 0; LIndex < Count(); LIndex++)
			{
				if (LIndex > 0)
					LResult.Append(", ");
				LResult.Append(GetValue(LIndex).ToString());
			}
			if (Count() > 0)
				LResult.Append(" ");
			LResult.Append("}");
			return LResult.ToString();
		}
	}
}
