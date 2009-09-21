/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	using Alphora.Dataphor.DAE.Streams;
	
	/// <summary>Provides the host representation for scalar values in the DAE.</summary>
	/// <remarks>
	/// The host representation will wrap either a native representation of the value, or a stream representation of the value.
	/// </remarks>
	public class Scalar : DataValue
	{
		public Scalar(IValueManager AManager, Schema.IScalarType ADataType, object AValue) : base(AManager, ADataType)
		{
			FValue = AValue;
			FIsNative = true;
		}
		
		public Scalar(IValueManager AManager, Schema.IScalarType ADataType) : base(AManager, ADataType)
		{
			FStreamID = AManager.StreamManager.Allocate();
			ValuesOwned = true;
		}

		public Scalar(IValueManager AManager, Schema.IScalarType ADataType, StreamID AStreamID) : base(AManager, ADataType)
		{
			FStreamID = AStreamID;
			ValuesOwned = false;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (!IsNative && (StreamID != StreamID.Null))
			{
				if (ValuesOwned)
					Manager.StreamManager.Deallocate(StreamID);
				FStreamID = StreamID.Null;
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
					Manager.StreamManager.Deallocate((StreamID)Value);
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
					Conveyor LConveyor = Manager.GetConveyor(DataType);
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
						StreamID = Manager.StreamManager.Allocate();
						
					Stream LStream = OpenStream();
					try
					{
						LStream.SetLength(0);
						Conveyor LConveyor = Manager.GetConveyor(DataType);
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
				
				return (bool)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsBoolean), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsBoolean), Value, value);
			}
		}
		
		public bool GetAsBoolean(string ARepresentationName)
		{
			return (bool)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsBoolean(string ARepresentationName, bool AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (byte)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsByte), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsByte), Value, value);
			}
		}

		public byte GetAsByte(string ARepresentationName)
		{
			return (byte)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsByte(string ARepresentationName, byte AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (short)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt16), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt16), Value, value);
			}
		}

		public short GetAsInt16(string ARepresentationName)
		{
			return (short)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsInt16(string ARepresentationName, short AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (int)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt32), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt32), Value, value);
			}
		}

		public int GetAsInt32(string ARepresentationName)
		{
			return (int)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsInt32(string ARepresentationName, int AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (long)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt64), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsInt64), Value, value);
			}
		}

		public long GetAsInt64(string ARepresentationName)
		{
			return (long)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsInt64(string ARepresentationName, long AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (decimal)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsDecimal), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsDecimal), Value, value);
			}
		}

		public decimal GetAsDecimal(string ARepresentationName)
		{
			return (decimal)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsDecimal(string ARepresentationName, decimal AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (TimeSpan)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsTimeSpan), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsTimeSpan), Value, value);
			}
		}

		public TimeSpan GetAsTimeSpan(string ARepresentationName)
		{
			return (TimeSpan)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsTimeSpan(string ARepresentationName, TimeSpan AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (DateTime)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsDateTime), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsDateTime), Value, value);
			}
		}

		public DateTime GetAsDateTime(string ARepresentationName)
		{
			return (DateTime)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsDateTime(string ARepresentationName, DateTime AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (Guid)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsGuid), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsGuid), Value, value);
			}
		}

		public Guid GetAsGuid(string ARepresentationName)
		{
			return (Guid)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsGuid(string ARepresentationName, Guid AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (String)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsString), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsString), Value, value);
			}
		}

		public string GetAsString(string ARepresentationName)
		{
			return (string)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsString(string ARepresentationName, string AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
		}

		public string AsDisplayString
		{
			get { return (string)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsDisplayString), Value); }
			set { Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsDisplayString), Value, value); }
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
				
				return (Exception)Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsException), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsException), Value, value);
			}
		}

		public Exception GetAsException(string ARepresentationName)
		{
			return (Exception)Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsException(string ARepresentationName, Exception AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
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
				
				return (byte[])Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsByteArray), Value);
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
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsByteArray), Value, value);
			}
		}

		public byte[] GetAsByteArray(string ARepresentationName)
		{
			return (byte[])Manager.GetAsNative(DataType.Representations[ARepresentationName], Value);
		}
		
		public void SetAsByteArray(string ARepresentationName, byte[] AValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[ARepresentationName], Value, AValue);
		}

		public string AsBase64String
		{
			get { return Convert.ToBase64String(AsByteArray); }
			set { AsByteArray = Convert.FromBase64String(value); }
		}
		
		private Stream FWriteStream; // saves the write stream between the GetPhysicalSize and WriteToPhysical calls
		private DataValue FWriteValue; // saves the row instantiated to write the compound value if this is a compound scalar
		
		public override int GetPhysicalSize(bool AExpandStreams)
		{
			int LSize = 1; // Scalar header
			if (!IsNil)
			{
				if (IsNative)
				{
					if (DataType.IsCompound)
					{
						FWriteValue = DataValue.FromNative(Manager, DataType.CompoundRowType, Value);
						return LSize + FWriteValue.GetPhysicalSize(AExpandStreams);
					}
					else
					{
						Streams.Conveyor LConveyor = Manager.GetConveyor(DataType);
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
					FWriteStream = Manager.StreamManager.Open(StreamID, LockMode.Exclusive);
					return LSize + (int)FWriteStream.Length;
				}

				return LSize + StreamID.CSizeOf;
			}
			return LSize;
		}

		public override void WriteToPhysical(byte[] ABuffer, int AOffset, bool AExpandStreams)
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
						Streams.Conveyor LConveyor = Manager.GetConveyor(DataType);
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
						((StreamID)Value).Write(ABuffer, AOffset);
				}
			}
		}

		public override void ReadFromPhysical(byte[] ABuffer, int AOffset)
		{
			// Clear current value
			if (ValuesOwned && !IsNative && (StreamID != StreamID.Null))
				Manager.StreamManager.Deallocate(StreamID);

			// Read scalar header
			byte LHeader = ABuffer[AOffset];
			AOffset++;
			if ((LHeader & 1) != 0) // if not nil
			{
				if ((LHeader & 2) != 0)
				{
					if (DataType.IsCompound)
					{
						using (Row LRow = (Row)DataValue.FromPhysical(Manager, DataType.CompoundRowType, ABuffer, AOffset))
						{
							Value = LRow.AsNative;
							LRow.ValuesOwned = false;
						}
					}
					else
					{
						Streams.Conveyor LConveyor = Manager.GetConveyor(DataType);
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
						StreamID = Manager.StreamManager.Allocate();
						Stream LStream = Manager.StreamManager.Open(StreamID, LockMode.Exclusive);
						LStream.Write(ABuffer, AOffset, ABuffer.Length - AOffset);
						LStream.Close();
					}
					else
						StreamID = StreamID.Read(ABuffer, AOffset);
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
						: (byte[])Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsByteArray), this);
				return new MemoryStream(LValue, 0, LValue.Length, false, true);
			}
			return Manager.StreamManager.Open(StreamID, LockMode.Exclusive);
		}
		
		public override Stream OpenStream(string ARepresentationName)
		{
			return Manager.GetAsDataValue(DataType.Representations[ARepresentationName], AsNative).OpenStream();
		}

		public override object CopyNativeAs(Schema.IDataType ADataType)
		{
			if (IsNative)
			{
				ICloneable LCloneable = Value as ICloneable;
				if (LCloneable != null)
					return LCloneable.Clone();
					
				if (DataType.IsCompound)
					return DataValue.CopyNative(Manager, DataType.CompoundRowType, Value);
					
				return Value;
			}

			if (StreamID == StreamID.Null)
				return StreamID;
			return Manager.StreamManager.Reference(StreamID);
		}

		public override string ToString()
		{
			return AsDisplayString;
		}
	}
	
	/// <summary>A scalar value which is currently contained inside a native row or list.</summary>	
	public abstract class InternedScalar : Scalar
	{
		public InternedScalar(IValueManager AManager, Schema.IScalarType ADataType) : base(AManager, ADataType, null)
		{
			ValuesOwned = false;
		}

		/// <summary>Indicates whether the value for this scalar is stored in its native representation.</summary>
		public override bool IsNative { get { return !(Value is StreamID); } }
	}
	
	public class RowInternedScalar : InternedScalar
	{
		public RowInternedScalar(IValueManager AManager, Schema.IScalarType ADataType, NativeRow ANativeRow, int AIndex) : base(AManager, ADataType)
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
		public ListInternedScalar(IValueManager AManager, Schema.IScalarType ADataType, NativeList ANativeList, int AIndex) : base(AManager, ADataType)
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

