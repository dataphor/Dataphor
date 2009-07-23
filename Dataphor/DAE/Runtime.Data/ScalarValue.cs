/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using System;
	using System.IO;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	
	/// <summary>Provides the host representation for scalar values in the DAE.</summary>
	/// <remarks>
	/// The host representation will wrap either a native representation of the value, or a stream representation of the value.
	/// </remarks>
	public class Scalar : DataValue
	{
		public Scalar(IServerProcess AProcess, Schema.IScalarType ADataType, object AValue) : base(AProcess, ADataType)
		{
			FValue = AValue;
			FIsNative = true;
		}
		
		public Scalar(IServerProcess AProcess, Schema.IScalarType ADataType) : base(AProcess, ADataType)
		{
			FStreamID = AProcess.Allocate();
			ValuesOwned = true;
		}

		public Scalar(IServerProcess AProcess, Schema.IScalarType ADataType, StreamID AStreamID) : base(AProcess, ADataType)
		{
			FStreamID = AStreamID;
			ValuesOwned = false;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (!IsNative && (StreamID != StreamID.Null))
			{
				if (ValuesOwned)
					Process.Deallocate(StreamID);
				StreamID = StreamID.Null;
			}
			base.Dispose(ADisposing);
		}
		
		private bool FIsNative;		
		private StreamID FStreamID;
		private object FValue;

		public new Schema.ScalarType DataType { get { return (Schema.ScalarType)base.DataType; } }

		public override bool IsNative { get { return FIsNative; } }

		protected void CheckNonNative()
		{
			if (IsNative)
				throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		protected virtual object Value
		{
			get { return FIsNative ? FValue : FStreamID; }
			set
			{
				FIsNative = !(value is StreamID);
				if (!FIsNative)
				{
					FStreamID = (StreamID)value;
					FValue = null;
				}
				else
				{
					FStreamID = StreamID.Null;
					FValue = value;
				}
			}
		}

		/// <summary>Returns the stream id that contains the data for the physical representation of this scalar.</summary>
		public StreamID StreamID 
		{ 
			get 
			{ 
				CheckNonNative();
				return (StreamID)Value; 
			} 
			set 
			{ 
				CheckNonNative();
				if (ValuesOwned && ((StreamID)Value != StreamID.Null))
					Process.Deallocate((StreamID)Value);
				Value = value; 
			}
		}

		public override bool IsNil
		{
			get
			{
				if (IsNative)
					return Value == null;
				else
					return StreamID == StreamID.Null;
			}
		}
		
		public override object AsNative
		{
			get
			{
				if (IsNative)
				{
					if (Value == null)
						throw new RuntimeException(RuntimeException.Codes.NilEncountered);
					return Value;
				}
				
				if (StreamID == StreamID.Null)
					throw new RuntimeException(RuntimeException.Codes.NilEncountered);
				
				Stream LStream = OpenStream();
				try
				{
					Conveyor LConveyor = DataType.GetConveyor(Process);
					if (LConveyor.IsStreaming)
						return LConveyor.Read(LStream);
					else
					{
						byte[] LValue = new byte[(int)LStream.Length];
						LStream.Read(LValue, 0, LValue.Length);
						return LConveyor.Read(LValue, 0);
					}
				}
				finally
				{
					LStream.Close();
				}
			}
			set
			{
				if (IsNative)
					Value = value;
				else
				{
					if (StreamID == StreamID.Null)
						StreamID = Process.Allocate();
						
					Stream LStream = OpenStream();
					try
					{
						LStream.SetLength(0);
						Conveyor LConveyor = DataType.GetConveyor(Process);
						if (LConveyor.IsStreaming)
							LConveyor.Write(value, LStream);
						else
						{
							byte[] LValue = new byte[LConveyor.GetSize(value)];
							LConveyor.Write(value, LValue, 0);
							LStream.Write(LValue, 0, LValue.Length);
						}
					}
					finally
					{
						LStream.Close();
					}
				}
			}
		}

		public bool AsBoolean
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsBoolean.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsBoolean, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (bool)Value;
					}
					return (bool)AsNative;
				}
				
				return (bool)DataType.GetRepresentation(NativeAccessors.AsBoolean).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsBoolean.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsBoolean, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsBoolean).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}
		
		public bool GetAsBoolean(string ARepresentationName)
		{
			return (bool)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsBoolean(string ARepresentationName, bool AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public byte AsByte
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsByte.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByte, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (byte)Value;
					}
					return (byte)AsNative;
				}
				
				return (byte)DataType.GetRepresentation(NativeAccessors.AsByte).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsByte.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByte, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsByte).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public byte GetAsByte(string ARepresentationName)
		{
			return (byte)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsByte(string ARepresentationName, byte AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public short AsInt16
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsInt16.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt16, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (short)Value;
					}
					return (short)AsNative;
				}
				
				return (short)DataType.GetRepresentation(NativeAccessors.AsInt16).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsInt16.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt16, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsInt16).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public short GetAsInt16(string ARepresentationName)
		{
			return (short)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsInt16(string ARepresentationName, short AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public int AsInt32
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsInt32.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt32, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (int)Value;
					}
					return (int)AsNative;
				}
				
				return (int)DataType.GetRepresentation(NativeAccessors.AsInt32).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsInt32.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt32, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsInt32).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public int GetAsInt32(string ARepresentationName)
		{
			return (int)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsInt32(string ARepresentationName, int AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public long AsInt64
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsInt64.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt64, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (long)Value;
					}
					return (long)AsNative;
				}
				
				return (long)DataType.GetRepresentation(NativeAccessors.AsInt64).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsInt64.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsInt64, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsInt64).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public long GetAsInt64(string ARepresentationName)
		{
			return (long)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsInt64(string ARepresentationName, long AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public decimal AsDecimal
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsDecimal.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsDecimal, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (decimal)Value;
					}
					return (decimal)AsNative;
				}
				
				return (decimal)DataType.GetRepresentation(NativeAccessors.AsDecimal).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsDecimal.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsDecimal, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsDecimal).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public decimal GetAsDecimal(string ARepresentationName)
		{
			return (decimal)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsDecimal(string ARepresentationName, decimal AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public TimeSpan AsTimeSpan
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsTimeSpan.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsTimeSpan, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (TimeSpan)Value;
					}
					return (TimeSpan)AsNative;
				}
				
				return (TimeSpan)DataType.GetRepresentation(NativeAccessors.AsTimeSpan).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsTimeSpan.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsTimeSpan, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsTimeSpan).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public TimeSpan GetAsTimeSpan(string ARepresentationName)
		{
			return (TimeSpan)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsTimeSpan(string ARepresentationName, TimeSpan AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public DateTime AsDateTime
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsDateTime.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsDateTime, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (DateTime)Value;
					}
					return (DateTime)AsNative;
				}
				
				return (DateTime)DataType.GetRepresentation(NativeAccessors.AsDateTime).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsDateTime.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsDateTime, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsDateTime).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public DateTime GetAsDateTime(string ARepresentationName)
		{
			return (DateTime)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsDateTime(string ARepresentationName, DateTime AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public Guid AsGuid
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsGuid.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsGuid, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (Guid)Value;
					}
					return (Guid)AsNative;
				}
				
				return (Guid)DataType.GetRepresentation(NativeAccessors.AsGuid).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsGuid.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsGuid, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsGuid).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public Guid GetAsGuid(string ARepresentationName)
		{
			return (Guid)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsGuid(string ARepresentationName, Guid AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public String AsString
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsString.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsString, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (String)Value;
					}
					return (String)AsNative;
				}
				
				return (String)DataType.GetRepresentation(NativeAccessors.AsString).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsString.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsString, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsString).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public string GetAsString(string ARepresentationName)
		{
			return (string)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsString(string ARepresentationName, string AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public string AsDisplayString
		{
			get { return (string)DataType.GetRepresentation(NativeAccessors.AsDisplayString).GetAsNative(Process.GetServerProcess(), Value); }
			set { Value = DataType.GetRepresentation(NativeAccessors.AsDisplayString).SetAsNative(Process.GetServerProcess(), Value, value); }
		}

		public Exception AsException
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsException.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsException, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (Exception)Value;
					}
					return (Exception)AsNative;
				}
				
				return (Exception)DataType.GetRepresentation(NativeAccessors.AsException).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsException.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsException, true))
				{
					if (IsNative)
						Value = value;
					else
						AsNative = value;
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsException).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public Exception GetAsException(string ARepresentationName)
		{
			return (Exception)DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsException(string ARepresentationName, Exception AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public byte[] AsByteArray
		{
			get
			{
				if ((DataType.NativeType == NativeAccessors.AsByteArray.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByteArray, true))
				{
					if (IsNative)
					{
						if (Value == null)
							throw new RuntimeException(RuntimeException.Codes.NilEncountered);
						return (byte[])Value;
					}

					Stream LStream = OpenStream();
					try
					{
						byte[] LValue = new byte[LStream.Length];
						LStream.Read(LValue, 0, (int)LStream.Length);
						return LValue;
					}
					finally
					{
						LStream.Close();
					}
				}
				
				return (byte[])DataType.GetRepresentation(NativeAccessors.AsByteArray).GetAsNative(Process.GetServerProcess(), Value);
			}
			set
			{
				if ((DataType.NativeType == NativeAccessors.AsByteArray.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByteArray, true))
				{
					if (IsNative)
						Value = value;
					else
					{
						Stream LStream = OpenStream();
						try
						{
							LStream.Write(value, 0, value.Length);
						}
						finally
						{
							LStream.Close();
						}
					}
				}
				else
					Value = DataType.GetRepresentation(NativeAccessors.AsByteArray).SetAsNative(Process.GetServerProcess(), Value, value);
			}
		}

		public byte[] GetAsByteArray(string ARepresentationName)
		{
			return (byte[])DataType.Representations[ARepresentationName].GetAsNative(Process.GetServerProcess(), Value);
		}
		
		public void SetAsByteArray(string ARepresentationName, byte[] AValue)
		{
			Value = DataType.Representations[ARepresentationName].SetAsNative(Process.GetServerProcess(), Value, AValue);
		}

		public string AsBase64String
		{
			get { return Convert.ToBase64String(AsByteArray); }
			set { AsByteArray = Convert.FromBase64String(value); }
		}
		
		private Stream FWriteStream; // saves the write stream between the GetPhysicalSize and WriteToPhysical calls
		private DataValue FWriteValue; // saves the row instantiated to write the compound value if this is a compound scalar
		
		public unsafe override int GetPhysicalSize(bool AExpandStreams)
		{
			int LSize = 1; // Scalar header
			if (!IsNil)
			{
				if (IsNative)
				{
					if (DataType.IsCompound)
					{
						FWriteValue = DataValue.FromNative(Process, DataType.CompoundRowType, Value);
						return LSize + FWriteValue.GetPhysicalSize(AExpandStreams);
					}
					else
					{
						Streams.Conveyor LConveyor = DataType.GetConveyor(Process);
						if (LConveyor.IsStreaming)
						{
							FWriteStream = new MemoryStream(64);
							LConveyor.Write(Value, FWriteStream);
							return LSize + (int)FWriteStream.Length;
						}
						return LSize + LConveyor.GetSize(Value);
					}
				}
					
				if (AExpandStreams)
				{
					FWriteStream = Process.Open(StreamID, LockMode.Exclusive);
					return LSize + (int)FWriteStream.Length;
				}

				return LSize + sizeof(StreamID);
			}
			return LSize;
		}

		public unsafe override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
		{
			// Write scalar header
			byte LHeader = (byte)(IsNil ? 0 : 1);
			LHeader |= (byte)(IsNative ? 2 : 0);
			LHeader |= (byte)(AExpandStreams ? 4 : 0);
			ABuffer[AOffset] = LHeader;
			AOffset++;

			if (!IsNil)
			{
				if (IsNative)
				{
					if (DataType.IsCompound)
					{
						FWriteValue.WriteToPhysical(ABuffer, AOffset, AExpandStreams);
						FWriteValue.Dispose();
						FWriteValue = null;
					}
					else
					{
						Streams.Conveyor LConveyor = DataType.GetConveyor(Process);
						if (LConveyor.IsStreaming)
						{
							FWriteStream.Position = 0;
							FWriteStream.Read(ABuffer, AOffset, (int)FWriteStream.Length);
							FWriteStream.Close();
						}
						else
							LConveyor.Write(Value, ABuffer, AOffset);
					}
				}
				else
				{
					if (AExpandStreams)
					{
						FWriteStream.Position = 0;
						FWriteStream.Read(ABuffer, AOffset, (int)FWriteStream.Length);
						FWriteStream.Close();
					}
					else
					{
						fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
						{
							*((StreamID*)LBufferPtr) = (StreamID)Value;
						}
					}
				}
			}
		}

		public unsafe override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			// Clear current value
			if (ValuesOwned && !IsNative && (StreamID != StreamID.Null))
				Process.Deallocate(StreamID);

			// Read scalar header
			byte LHeader = ABuffer[AOffset];
			AOffset++;
			if ((LHeader & 1) != 0) // if not nil
			{
				if ((LHeader & 2) != 0)
				{
					if (DataType.IsCompound)
					{
						using (Row LRow = (Row)DataValue.FromPhysical(Process, DataType.CompoundRowType, ABuffer, AOffset))
						{
							Value = LRow.AsNative;
							LRow.ValuesOwned = false;
						}
					}
					else
					{
						Streams.Conveyor LConveyor = DataType.GetConveyor(Process);
						if (LConveyor.IsStreaming)
						{
							Stream LStream = new MemoryStream(ABuffer, AOffset, ABuffer.Length - AOffset, false, true);
							Value = LConveyor.Read(LStream);
							LStream.Close();
						}
						else
						{
							Value = LConveyor.Read(ABuffer, AOffset);
						}
					}
				}
				else
				{
					if ((LHeader & 4) != 0) // if expanded form
					{
						StreamID = Process.Allocate();
						Stream LStream = Process.Open(StreamID, LockMode.Exclusive);
						LStream.Write(ABuffer, AOffset, ABuffer.Length - AOffset);
						LStream.Close();
					}
					else
					{
						fixed (byte* LBufferPtr = &(ABuffer[AOffset]))
						{
							StreamID = *((StreamID*)LBufferPtr);
						}
					}
				}
			}
			else
			{
				if ((LHeader & 2) != 0)
					Value = null;
				else
					StreamID = StreamID.Null;
			}
		}
		
		/// <summary>Opens a stream to read the data for this value. If this instance is native, the stream will be read only.</summary>
		public override Stream OpenStream()
		{
			if (IsNative)
			{
				byte[] LValue = 
					(DataType.NativeType == NativeAccessors.AsByteArray.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByteArray, true)
						? (byte[])Value
						: (byte[])DataType.GetRepresentation(NativeAccessors.AsByteArray).GetAsNative(Process.GetServerProcess(), this);
				return new MemoryStream(LValue, 0, LValue.Length, false, true);
			}
			return Process.Open(StreamID, LockMode.Exclusive);
		}
		
		public override Stream OpenStream(string ARepresentationName)
		{
			return DataType.Representations[ARepresentationName].GetAsDataValue(Process.GetServerProcess(), AsNative).OpenStream();
		}

		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
			if (IsNative)
			{
				ICloneable LCloneable = Value as ICloneable;
				if (LCloneable != null)
					return LCloneable.Clone();
					
				if (DataType.IsCompound)
					return DataValue.CopyNative(Process, DataType.CompoundRowType, Value);
					
				return Value;
			}

			if (StreamID == StreamID.Null)
				return StreamID;
			return Process.Reference(StreamID);
		}

		public override string ToString()
		{
			return AsDisplayString;
		}
	}
	
	/// <summary>A scalar value which is currently contained inside a native row or list.</summary>	
	public abstract class InternedScalar : Scalar
	{
		public InternedScalar(IServerProcess AProcess, Schema.IScalarType ADataType) : base(AProcess, ADataType)
		{
			ValuesOwned = false;
		}

		/// <summary>Indicates whether the value for this scalar is stored in its native representation.</summary>
		public override bool IsNative { get { return !(Value is StreamID); } }
	}
	
	public class RowInternedScalar : InternedScalar
	{
		public RowInternedScalar(IServerProcess AProcess, Schema.IScalarType ADataType, NativeRow ANativeRow, int AIndex) : base(AProcess, ADataType)
		{
			FNativeRow = ANativeRow;
			FIndex = AIndex;
		}
		
		private NativeRow FNativeRow;
		private int FIndex;
		
		protected override object Value
		{
			get { return FNativeRow.Values[FIndex]; }
			set 
			{ 
				FNativeRow.Values[FIndex] = value; 
				if (FNativeRow.ModifiedFlags != null)
					FNativeRow.ModifiedFlags[FIndex] = true;
			}
		}
	}
	
	public class ListInternedScalar : InternedScalar
	{
		public ListInternedScalar(IServerProcess AProcess, Schema.IScalarType ADataType, NativeList ANativeList, int AIndex) : base(AProcess, ADataType)
		{
			FNativeList = ANativeList;
			FIndex = AIndex;
		}
		
		private NativeList FNativeList;
		private int FIndex;
		
		protected override object Value
		{
			get { return FNativeList.Values[FIndex]; }
			set { FNativeList.Values[FIndex] = value; }
		}
	}
}

