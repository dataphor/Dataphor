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
		public NativeRow(int AColumnCount) : base()
		{
			#if USEDATATYPESINNATIVEROW
			FDataTypes = new Schema.IDataType[AColumnCount];
			#endif
			FValues = new object[AColumnCount];
		}
		
		#if USEDATATYPESINNATIVEROW
		private Schema.IDataType[] FDataTypes;
		public Schema.IDataType[] DataTypes { get { return FDataTypes; } }
		#endif
		
		private object[] FValues;
		public object[] Values { get { return FValues; } }
		
		public BitArray ModifiedFlags;
	}
	
	/// <remarks>
	/// Provides a fixed length buffer for cell values with overflow management built in.
	/// Used in conjunction with the CellValueStream, provides transparent variable length
	/// value storage in a fixed length buffer.
	/// </remarks>    
	public class Row : DataValue
	{
		public Row(IValueManager AManager, Schema.IRowType ADataType) : base(AManager, ADataType)
		{
			FRow = new NativeRow(DataType.Columns.Count);
			#if USEDATATYPESINNATIVEROW
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
			#endif
			ValuesOwned = true;
		}

		// The given object[] is assumed to contains values of the appropriate type for the given data type
		public Row(IValueManager AManager, Schema.IRowType ADataType, NativeRow ARow) : base(AManager, ADataType)
		{
			FRow = ARow;
			ValuesOwned = false;
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FRow != null)
			{
				if (ValuesOwned)
					ClearValues();
				FRow = null;
			}
			base.Dispose(ADisposing);
		}
		
		public new Schema.IRowType DataType { get { return (Schema.IRowType)base.DataType; } }
		
		private NativeRow FRow;
		
		public override object AsNative
		{
			get { return FRow; }
			set 
			{
				if (FRow != null)
					ClearValues();
				FRow = (NativeRow)value; 
			}
		}
		
		private object[] FWriteList;
		private DataValue[] FElementWriteList;

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
		public override int GetPhysicalSize(bool AExpandStreams)
		{
			int LSize = 1; // write the value indicator
			if (!IsNil)
			{
				LSize += 1; // write the extended streams indicator
				FWriteList = new object[DataType.Columns.Count]; // list for saving the sizes or streams of each attribute in the row
				FElementWriteList = new DataValue[DataType.Columns.Count]; // list for saving host representations of values between the GetPhysicalSize and WriteToPhysical calls
				Stream LStream;
				StreamID LStreamID;
				Schema.ScalarType LScalarType;
				Streams.Conveyor LConveyor;
				DataValue LElement;
				int LElementSize;
				for (int LIndex = 0; LIndex < FWriteList.Length; LIndex++)
				{
					LSize += sizeof(byte); // write a value indicator
					if (FRow.Values[LIndex] != null)
					{
						#if USEDATATYPESINNATIVEROW
						if (!DataType.Columns[LIndex].DataType.Equals(FRow.DataTypes[LIndex]))
							LSize += Manager.GetConveyor(Manager.DataTypes.SystemString).GetSize(FRow.DataTypes[LIndex].Name); // write the name of the data type of the value
						#endif
						/*							
						LElement = DataValue.FromNativeRow(Manager, DataType, FRow, LIndex);
						FElementWriteList[LIndex] = LElement;
						LElementSize = LElement.GetPhysicalSize(AExpandStreams);
						FWriteList[LIndex] = LElementSize;
						LSize += sizeof(int) + LElementSize;
						*/
						#if USEDATATYPESINNATIVEROW
						LScalarType = FRow.DataTypes[LIndex] as Schema.ScalarType;
						#else
						LScalarType = DataType.Columns[LIndex].DataType as Schema.ScalarType;
						#endif
						if ((LScalarType != null) && !LScalarType.IsCompound)
						{
							if (FRow.Values[LIndex] is StreamID)
							{
								// If this is a non-native scalar
								LStreamID = (StreamID)FRow.Values[LIndex];
								if (AExpandStreams)
								{
									if (LStreamID != StreamID.Null)
									{
										LStream = Manager.StreamManager.Open((StreamID)FRow.Values[LIndex], LockMode.Exclusive);
										FWriteList[LIndex] = LStream;
										LSize += sizeof(int) + (int)LStream.Length;
									}
								}
								else
								{
									if (LStreamID != StreamID.Null)
									{
										LElementSize = sizeof(long);
										FWriteList[LIndex] = LElementSize;
										LSize += LElementSize;
									}
								}
							}
							else
							{
								LConveyor = Manager.GetConveyor(LScalarType);
								if (LConveyor.IsStreaming)
								{
									LStream = new MemoryStream(64);
									FWriteList[LIndex] = LStream;
									LConveyor.Write(FRow.Values[LIndex], LStream);
									LStream.Position = 0;
									LSize += sizeof(int) + (int)LStream.Length;
								}
								else
								{
									LElementSize = LConveyor.GetSize(FRow.Values[LIndex]);
									FWriteList[LIndex] = LElementSize;
									LSize += sizeof(int) + LElementSize;;
								}
							}
						}
						else
						{
							LElement = DataValue.FromNativeRow(Manager, DataType, FRow, LIndex);
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
		
		public override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			if (FWriteList == null)
				throw new RuntimeException(RuntimeException.Codes.UnpreparedWriteToPhysicalCall);
				
			ABuffer[AOffset] = (byte)(IsNil ? 0 : 1); // Write the value indicator
			AOffset++;
			
			if (!IsNil)
			{
				ABuffer[AOffset] = (byte)(AExpandStreams ? 1 : 0); // Write the expanded streams indicator
				AOffset++;
					
				#if USEDATATYPESINNATIVEROW
				Streams.Conveyor LStringConveyor = null;
				#endif
				Streams.Conveyor LInt64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
				Streams.Conveyor LInt32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);

				Stream LStream;
				StreamID LStreamID;
				int LElementSize;
				Schema.ScalarType LScalarType;
				Streams.Conveyor LConveyor;
				DataValue LElement;
				for (int LIndex = 0; LIndex < FWriteList.Length; LIndex++)
				{
					if (FRow.Values[LIndex] == null)
					{
						ABuffer[AOffset] = (byte)0; // Write the native nil indicator
						AOffset++;
					}
					else
					{
						#if USEDATATYPESINNATIVEROW
						LScalarType = FRow.DataTypes[LIndex] as Schema.ScalarType;
						#else
						LScalarType = DataType.Columns[LIndex].DataType as Schema.ScalarType;
						#endif
						if ((LScalarType != null) && !LScalarType.IsCompound)
						{
							if (FRow.Values[LIndex] is StreamID)
							{
								// If this is a non-native scalar
								LStreamID = (StreamID)FRow.Values[LIndex];
								if (LStreamID == StreamID.Null)
								{
									ABuffer[AOffset] = (byte)1; // Write the non-native nil indicator
									AOffset++;
								}
								else
								{
									#if USEDATATYPESINNATIVEROW
									if (DataType.Columns[LIndex].DataType.Equals(FRow.DataTypes[LIndex]))
									{
									#endif
										ABuffer[AOffset] = (byte)3; // Write the native standard value indicator
										AOffset++;
									#if USEDATATYPESINNATIVEROW
									}
									else
									{
										ABuffer[AOffset] = (byte)5; // Write the native specialized value indicator
										AOffset++;
										if (LStringConveyor == null)
											LStringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
										LElementSize = LStringConveyor.GetSize(FRow.DataTypes[LIndex].Name);
										LStringConveyor.Write(FRow.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
										AOffset += LElementSize;
									}
									#endif
									
									if (AExpandStreams)
									{
										LStream = (Stream)FWriteList[LIndex];
										LInt32Conveyor.Write(Convert.ToInt32(LStream.Length), ABuffer, AOffset);
										AOffset += sizeof(int);
										LStream.Read(ABuffer, AOffset, (int)LStream.Length);
										AOffset += (int)LStream.Length;
										LStream.Close();
									}
									else
									{
										LInt64Conveyor.Write((long)LStreamID.Value, ABuffer, AOffset);
										AOffset += sizeof(long);
									}
								}
							}
							else
							{
								#if USEDATATYPESINNATIVEROW
								if (DataType.Columns[LIndex].DataType.Equals(FRow.DataTypes[LIndex]))
								{
								#endif
									ABuffer[AOffset] = (byte)2; // Write the native standard value indicator
									AOffset++;
								#if USEDATATYPESINNATIVEROW
								}
								else
								{
									ABuffer[AOffset] = (byte)4; // Write the native specialized value indicator
									AOffset++;
									if (LStringConveyor == null)
										LStringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
									LElementSize = LStringConveyor.GetSize(FRow.DataTypes[LIndex].Name);
									LStringConveyor.Write(FRow.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
									AOffset += LElementSize;
								}
								#endif

								LConveyor = Manager.GetConveyor(LScalarType);
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
									LConveyor.Write(FRow.Values[LIndex], ABuffer, AOffset); // Write the value of this scalar
									AOffset += LElementSize;
								}
							}
						}
						else
						{
							#if USEDATATYPESINNATIVEROW
							if (DataType.Columns[LIndex].DataType.Equals(FRow.DataTypes[LIndex]))
							{
							#endif
								ABuffer[AOffset] = (byte)2; // Write the native standard value indicator
								AOffset++;
							#if USEDATATYPESINNATIVEROW
							}
							else
							{
								ABuffer[AOffset] = (byte)4; // Write the native specialized value indicator
								AOffset++;
								if (LStringConveyor == null)
									LStringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
								LElementSize = LStringConveyor.GetSize(FRow.DataTypes[LIndex].Name);
								LStringConveyor.Write(FRow.DataTypes[LIndex].Name, ABuffer, AOffset); // Write the name of the data type of the value
								AOffset += LElementSize;
							}
							#endif

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
		}

		public override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			ClearValues(); // Clear the current value of the row
			
			if (ABuffer[AOffset] == 0)
			{
				FRow = null;
			}
			else
			{
				FRow = new NativeRow(DataType.Columns.Count);
				AOffset++;
			
				bool LExpandStreams = ABuffer[AOffset] != 0; // Read the exapnded streams indicator
				AOffset++;
					
				#if USEDATATYPESINNATIVEROW
				Streams.Conveyor LStringConveyor = Manager.GetConveyor(Manager.DataTypes.SystemString);
				string LDataTypeName;
				Schema.IDataType LDataType;
				#endif
				Streams.Conveyor LInt64Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemLong);
				Streams.Conveyor LInt32Conveyor = Manager.GetConveyor(Manager.DataTypes.SystemInteger);

				Stream LStream;
				StreamID LStreamID;
				int LElementSize;
				Schema.ScalarType LScalarType;
				Streams.Conveyor LConveyor;
				for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				{
					byte LValueIndicator = ABuffer[AOffset];
					AOffset++;
					
					switch (LValueIndicator)
					{
						case 0 : // native nil
							#if USEDATATYPESINNATIVEROW
							FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
							#endif
							FRow.Values[LIndex] = null;
						break;
						
						case 1 : // non-native nil
							#if USEDATATYPESINNATIVEROW
							FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
							#endif
							FRow.Values[LIndex] = StreamID.Null;
						break;
						
						case 2 : // native standard value
							LScalarType = DataType.Columns[LIndex].DataType as Schema.ScalarType;
							if ((LScalarType != null) && !LScalarType.IsCompound)
							{
								LConveyor = Manager.GetConveyor(LScalarType);
								if (LConveyor.IsStreaming)
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									LStream = new MemoryStream(ABuffer, AOffset, LElementSize, false, true);
									#if USEDATATYPESINNATIVEROW
									FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
									#endif
									FRow.Values[LIndex] = LConveyor.Read(LStream);
									AOffset += LElementSize;
								}
								else
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									#if USEDATATYPESINNATIVEROW
									FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
									#endif
									FRow.Values[LIndex] = LConveyor.Read(ABuffer, AOffset);
									AOffset += LElementSize;
								}
							}
							else
							{
								LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
								AOffset += sizeof(int);
								#if USEDATATYPESINNATIVEROW
								FRow.DataTypes[LIndex] = DataType.Columns[LIndex].DataType;
								#endif
								using (DataValue LValue = DataValue.FromPhysical(Manager, DataType.Columns[LIndex].DataType, ABuffer, AOffset))
								{
									FRow.Values[LIndex] = LValue.AsNative;
									LValue.ValuesOwned = false;
								}
								AOffset += LElementSize;
							}
						break;
						
						case 3 : // non-native standard value
							LScalarType = DataType.Columns[LIndex].DataType as Schema.ScalarType;
							if (LScalarType != null)
							{
								if (LExpandStreams)
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									LStreamID = Manager.StreamManager.Allocate();
									LStream = Manager.StreamManager.Open(LStreamID, LockMode.Exclusive);
									LStream.Write(ABuffer, AOffset, LElementSize);
									LStream.Close();
									#if USEDATATYPESINNATIVEROW
									FRow.DataTypes[LIndex] = LScalarType;
									#endif
									FRow.Values[LIndex] = LStreamID;
									AOffset += LElementSize;
								}
								else
								{
									#if USEDATATYPESINNATIVEROW
									FRow.DataTypes[LIndex] = LScalarType;
									#endif
									FRow.Values[LIndex] = new StreamID(Convert.ToUInt64(LInt64Conveyor.Read(ABuffer, AOffset)));
									AOffset += sizeof(long);
								}
							}
							else
							{
								// non-scalar values cannot be non-native
							}
						break;
						
						case 4 : // native specialized value
							#if USEDATATYPESINNATIVEROW
							LDataTypeName = (string)LStringConveyor.Read(ABuffer, AOffset);
							LDataType = Manager.CompileTypeSpecifier(LDataTypeName);
							AOffset += LStringConveyor.GetSize(LDataTypeName);
							LScalarType = LDataType as Schema.ScalarType;
							if ((LScalarType != null) && !LScalarType.IsCompound)
							{
								LConveyor = Manager.GetConveyor(LScalarType);
								if (LConveyor.IsStreaming)
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									LStream = new MemoryStream(ABuffer, AOffset, LElementSize, false, true);
									FRow.DataTypes[LIndex] = LScalarType;
									FRow.Values[LIndex] = LConveyor.Read(LStream);
									AOffset += LElementSize;
								}
								else
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									FRow.DataTypes[LIndex] = LScalarType;
									FRow.Values[LIndex] = LConveyor.Read(ABuffer, AOffset);
									AOffset += LElementSize;
								}
							}
							else
							{
								LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
								AOffset += sizeof(int);
								FRow.DataTypes[LIndex] = LDataType;
								using (DataValue LValue = DataValue.FromPhysical(Manager, LDataType, ABuffer, AOffset))
								{
									FRow.Values[LIndex] = LValue.AsNative;
									LValue.ValuesOwned = false;
								}
								AOffset += LElementSize;
							}
						break;
							#else
							throw new NotSupportedException("Specialized data types in rows are not supported");
							#endif
						
						case 5 : // non-native specialized value
							#if USEDATATYPESINNATIVEROW
							LDataTypeName = (string)LStringConveyor.Read(ABuffer, AOffset);
							LDataType = Manager.CompileTypeSpecifier(LDataTypeName);
							AOffset += LStringConveyor.GetSize(LDataTypeName);
							LScalarType = LDataType as Schema.ScalarType;
							if (LScalarType != null)
							{
								if (LExpandStreams)
								{
									LElementSize = (int)LInt32Conveyor.Read(ABuffer, AOffset);
									AOffset += sizeof(int);
									LStreamID = Manager.StreamManager.Allocate();
									LStream = Manager.StreamManager.Open(LStreamID, LockMode.Exclusive);
									LStream.Write(ABuffer, AOffset, LElementSize);
									LStream.Close();
									FRow.DataTypes[LIndex] = LScalarType;
									FRow.Values[LIndex] = LStreamID;
									AOffset += LElementSize;
								}
								else
								{
									FRow.DataTypes[LIndex] = LScalarType;
									FRow.Values[LIndex] = new StreamID(Convert.ToUInt64(LInt64Conveyor.Read(ABuffer, AOffset)));
									AOffset += sizeof(long);
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
		
		public bool HasNonNativeValue(int AIndex)
		{
			#if USEDATATYPESINNATIVEROW
			return (FRow.DataTypes[AIndex] is Schema.IScalarType) && (FRow.Values[AIndex] is StreamID);
			#else
			return (DataType.Columns[AIndex].DataType is Schema.IScalarType) && (FRow.Values[AIndex] is StreamID);
			#endif
		}
		
		public StreamID GetNonNativeStreamID(int AIndex)
		{
			return (StreamID)FRow.Values[AIndex];
		}
		
		/// <summary>This is a by-reference access of the value, changes made to the resulting DataValue will be refelected in the actual row.</summary>
		public DataValue GetValue(int AIndex)
		{
			return FromNativeRow(Manager, DataType, FRow, AIndex);
		}
		
		public void SetValue(int AIndex, DataValue AValue)
		{
			this[AIndex] = AValue;
		}
		
		/// <summary>
		/// Returns the native representation if the value is stored in non-native form (as a StreamID)
		/// </summary>
		public object GetNativeValue(int AIndex)
		{
			// TODO: This should recursively ensure that no contained values are non-native
			if (HasNonNativeValue(AIndex))
				return GetValue(AIndex).AsNative;
				
			return this[AIndex];
		}

		/// <summary>This is a by-reference access of the value, changes made to the resulting DataValue will be refelected in the actual row.</summary>
		public object this[int AIndex]
		{
			get 
			{
				#if USEDATATYPESINNATIVEROW
				if (FRow.DataTypes[AIndex] is Schema.IScalarType)
				#else
				if (DataType.Columns[AIndex].DataType is Schema.IScalarType)
				#endif
					return FRow.Values[AIndex];
				return FromNativeRow(Manager, DataType, FRow, AIndex); 
			}
			set
			{
				if (FRow.Values[AIndex] != null)
					#if USEDATATYPESINNATIVEROW
					DataValue.DisposeNative(Manager, FRow.DataTypes[AIndex], FRow.Values[AIndex]);
					#else
					DataValue.DisposeNative(Manager, DataType.Columns[AIndex].DataType, FRow.Values[AIndex]);
					#endif
					
				DataValue LValue = value as DataValue;
				if (LValue != null)
				{
					#if USEDATATYPESINNATIVEROW
					FRow.DataTypes[AIndex] = LValue.DataType;
					#endif
					FRow.Values[AIndex] = LValue.CopyNative();
				}
				else if (value != null)
				{
					#if USEDATATYPESINNATIVEROW
					if ((DataType.Columns[AIndex].DataType == Manager.DataTypes.SystemGeneric) || (DataType.Columns[AIndex].DataType == Manager.DataTypes.SystemScalar))
						FRow.DataTypes[AIndex] = DataValue.NativeTypeToScalarType(Manager, value.GetType());
					else
						FRow.DataTypes[AIndex] = DataType.Columns[AIndex].DataType;
					FRow.Values[AIndex] = DataValue.CopyNative(Manager, FRow.DataTypes[AIndex], value);
					#else
					FRow.Values[AIndex] = DataValue.CopyNative(Manager, DataType.Columns[AIndex].DataType, value);
					#endif
				}
				else
				{
					#if USEDATATYPESINNATIVEROW
					FRow.DataTypes[AIndex] = DataType.Columns[AIndex].DataType;
					#endif
					FRow.Values[AIndex] = null;
				}
				
				if (FRow.ModifiedFlags != null)
					FRow.ModifiedFlags[AIndex] = true;
			}
		}
		
		private int FModifiedContextCount;

		public void BeginModifiedContext()
		{
			if (FModifiedContextCount == 0)
			{
				FRow.ModifiedFlags = new BitArray(FRow.Values.Length);
				FRow.ModifiedFlags.SetAll(false);
			}
			FModifiedContextCount++;
		}
		
		public BitArray EndModifiedContext()
		{
			FModifiedContextCount--;
			if (FModifiedContextCount == 0)
			{
				BitArray LResult = FRow.ModifiedFlags;
				FRow.ModifiedFlags = null;
				return LResult;
			}
			return FRow.ModifiedFlags;
		}
		
		/// <summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.</summary>
		public int IndexOfColumn(string AColumnName)
		{
			return DataType.Columns.IndexOfColumn(AColumnName);
		}

		///	<summary>Returns the index of the given column name, resolving first for the full name, then for a partial match.  Throws an exception if the column name is not found.</summary>
		public int GetIndexOfColumn(string AColumnName)
		{
			return DataType.Columns.GetIndexOfColumn(AColumnName);
		}
		
		public object this[string AColumnName]
		{
			get { return this[GetIndexOfColumn(AColumnName)]; }
			set { this[GetIndexOfColumn(AColumnName)] = value; }
		}
		
		public DataValue GetValue(string AColumnName)
		{
			return GetValue(GetIndexOfColumn(AColumnName));
		}
		
		public void SetValue(string AColumnName, DataValue AValue)
		{
			SetValue(GetIndexOfColumn(AColumnName), AValue);
		}

		public bool HasValue(int AIndex)
		{
			return FRow != null && FRow.Values[AIndex] != null;
		}
		
		public bool HasValue(string AColumnName)
		{
			return HasValue(GetIndexOfColumn(AColumnName));
		}
		
		public bool HasNils()
		{
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				if (!HasValue(LIndex))
					return true;
			return false;
		}
		
		public bool HasAnyNoValues()
		{
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				if (!HasValue(LIndex))
					return true;
			return false;
		}
		
		public bool HasNonNativeValues()
		{
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				if (HasNonNativeValue(LIndex))
					return true;
			return false;
		}
		
		public override bool IsNil { get { return FRow == null; } }

		public void ClearValue(int AIndex)
		{
			if (FRow.Values[AIndex] != null)
			{
				if (ValuesOwned)
					#if USEDATATYPESINNATIVEROW
					DataValue.DisposeNative(Manager, FRow.DataTypes[AIndex], FRow.Values[AIndex]);
					#else
					DataValue.DisposeNative(Manager, DataType.Columns[AIndex].DataType, FRow.Values[AIndex]);
					#endif
				
				#if USEDATATYPESINNATIVEROW
				FRow.DataTypes[AIndex] = DataType.Columns[AIndex].DataType;
				#endif
				FRow.Values[AIndex] = null;
			}
		}

		public void ClearValue(string AColumnName)
		{
			ClearValue(GetIndexOfColumn(AColumnName));
		}
		
		public void ClearValues()
		{
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				ClearValue(LIndex);
		}
		
		/// <summary>Returns an array of ARow.DataType.Columns.Count boolean values indicating whether each column in ARow has a value.</summary>
		public BitArray GetValueFlags()
		{
			BitArray LValueFlags = new BitArray(DataType.Columns.Count);
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
				LValueFlags[LIndex] = HasValue(LIndex);
			return LValueFlags;
		}
		
		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
			if (FRow == null)
				return null;
				
			if (Object.ReferenceEquals(DataType, ADataType))
			{
				NativeRow LNewRow = new NativeRow(DataType.Columns.Count);
				if (FRow != null)
					for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
					{
						#if USEDATATYPESINNATIVEROW
						LNewRow.DataTypes[LIndex] = FRow.DataTypes[LIndex];
						LNewRow.Values[LIndex] = CopyNative(Manager, FRow.DataTypes[LIndex], FRow.Values[LIndex]);
						#else
						LNewRow.Values[LIndex] = CopyNative(Manager, DataType.Columns[LIndex].DataType, FRow.Values[LIndex]);
						#endif
					}
				return LNewRow;
			}
			else
			{
				NativeRow LNewRow = new NativeRow(DataType.Columns.Count);
				Schema.IRowType LNewRowType = (Schema.IRowType)ADataType;
				if (FRow != null)
					for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
					{
						int LNewIndex = LNewRowType.Columns.IndexOfName(DataType.Columns[LIndex].Name);
						#if USEDATATYPESINNATIVEROW
						LNewRow.DataTypes[LNewIndex] = FRow.DataTypes[LIndex];
						LNewRow.Values[LNewIndex] = CopyNative(Manager, FRow.DataTypes[LIndex], FRow.Values[LIndex]);
						#else
						LNewRow.Values[LNewIndex] = CopyNative(Manager, DataType.Columns[LIndex].DataType, FRow.Values[LIndex]);
						#endif
					}
				return LNewRow;
			}
		}

		public void CopyTo(Row ARow)
		{
			int LColumnIndex;
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
			{
				LColumnIndex = ARow.IndexOfColumn(DataType.Columns[LIndex].Name);
				if (LColumnIndex >= 0)
					if (HasValue(LIndex))
						ARow[LColumnIndex] = this[LIndex];
					else
						ARow.ClearValue(LColumnIndex);
			}
		}

		public override string ToString()
		{
			StringBuilder LResult = new StringBuilder();
			LResult.Append("row { ");
			for (int LIndex = 0; LIndex < DataType.Columns.Count; LIndex++)
			{
				if (LIndex > 0)
					LResult.Append(", ");
				LResult.Append(GetValue(LIndex).ToString());
			}
			if (DataType.Columns.Count > 0)
				LResult.Append(" ");
			LResult.Append("}");
			return LResult.ToString();
		}
	}
}
