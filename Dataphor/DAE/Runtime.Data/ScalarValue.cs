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

	/// <summary>
	/// Provides native representation of a scalar that does not map directly to a .NET type
	/// </summary>
	/// <typeparam name="T">The underlying .NET type</typeparam>
	public struct Scalar<T>
	{
		public Scalar(string typeName, T value)
		{
			TypeName = typeName;
			Value = value;
		}

		public string TypeName { get; set; }
		public T Value { get; set; }
	}

	/// <summary>
	/// Provides native representation of a compound scalar type.
	/// </summary>
	public class CompoundScalar
	{
		public CompoundScalar(Schema.IScalarType scalarType, NativeRow value)
        {
			ScalarType = scalarType;
			Value = value;
		}

		public Schema.IScalarType ScalarType { get; set; }
		public NativeRow Value { get; set; }
	}

	/// <summary>Provides the host representation for scalar values in the DAE.</summary>
	/// <remarks>
	/// The host representation will wrap either a native representation of the value, or a stream representation of the value.
	/// </remarks>
	public class Scalar : DataValue, IScalar
	{
		public Scalar(IValueManager manager, Schema.IScalarType dataType, object tempValue) : base(manager, dataType)
		{
			_value = tempValue;
			_isNative = true;
		}
		
		public Scalar(IValueManager manager, Schema.IScalarType dataType) : base(manager, dataType)
		{
			_streamID = manager.StreamManager.Allocate();
			ValuesOwned = true;
		}

		public Scalar(IValueManager manager, Schema.IScalarType dataType, StreamID streamID) : base(manager, dataType)
		{
			_streamID = streamID;
			ValuesOwned = false;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (!IsNative && (StreamID != StreamID.Null))
			{
				if (ValuesOwned)
					Manager.StreamManager.Deallocate(StreamID);
				_streamID = StreamID.Null;
			}
			base.Dispose(disposing);
		}
		
		private bool _isNative;		
		private StreamID _streamID;
		private object _value;

		public new Schema.IScalarType DataType { get { return (Schema.ScalarType)base.DataType; } }

		public override bool IsNative { get { return _isNative; } }

		protected void CheckNonNative()
		{
			if (IsNative)
				throw new RuntimeException(RuntimeException.Codes.UnableToProvideStreamAccess, DataType.Name);
		}
		
		protected virtual object Value
		{
			get { return _isNative ? _value : _streamID; }
			set
			{
				_isNative = !(value is StreamID);
				if (!_isNative)
				{
					_streamID = (StreamID)value;
					_value = null;
				}
				else
				{
					_streamID = StreamID.Null;
					_value = value;
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
				
				Stream stream = OpenStream();
				try
				{
					IConveyor conveyor = Manager.GetConveyor(DataType);
					if (conveyor.IsStreaming)
						return conveyor.Read(stream);
					else
					{
						byte[] tempValue = new byte[(int)stream.Length];
						stream.Read(tempValue, 0, tempValue.Length);
						return conveyor.Read(tempValue, 0);
					}
				}
				finally
				{
					stream.Close();
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
						
					Stream stream = OpenStream();
					try
					{
						stream.SetLength(0);
						IConveyor conveyor = Manager.GetConveyor(DataType);
						if (conveyor.IsStreaming)
							conveyor.Write(value, stream);
						else
						{
							byte[] tempValue = new byte[conveyor.GetSize(value)];
							conveyor.Write(value, tempValue, 0);
							stream.Write(tempValue, 0, tempValue.Length);
						}
					}
					finally
					{
						stream.Close();
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
		
		public bool GetAsBoolean(string representationName)
		{
			return (bool)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsBoolean(string representationName, bool tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public byte GetAsByte(string representationName)
		{
			return (byte)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsByte(string representationName, byte tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public short GetAsInt16(string representationName)
		{
			return (short)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsInt16(string representationName, short tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public int GetAsInt32(string representationName)
		{
			return (int)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsInt32(string representationName, int tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public long GetAsInt64(string representationName)
		{
			return (long)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsInt64(string representationName, long tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public decimal GetAsDecimal(string representationName)
		{
			return (decimal)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsDecimal(string representationName, decimal tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public TimeSpan GetAsTimeSpan(string representationName)
		{
			return (TimeSpan)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsTimeSpan(string representationName, TimeSpan tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public DateTime GetAsDateTime(string representationName)
		{
			return (DateTime)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsDateTime(string representationName, DateTime tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public Guid GetAsGuid(string representationName)
		{
			return (Guid)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsGuid(string representationName, Guid tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public string GetAsString(string representationName)
		{
			return (string)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsString(string representationName, string tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

		public Exception GetAsException(string representationName)
		{
			return (Exception)Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsException(string representationName, Exception tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
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

					Stream stream = OpenStream();
					try
					{
						byte[] tempValue = new byte[stream.Length];
						stream.Read(tempValue, 0, (int)stream.Length);
						return tempValue;
					}
					finally
					{
						stream.Close();
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
						Stream stream = OpenStream();
						try
						{
							stream.Write(value, 0, value.Length);
						}
						finally
						{
							stream.Close();
						}
					}
				}
				else
					Value = Manager.SetAsNative(DataType.GetRepresentation(NativeAccessors.AsByteArray), Value, value);
			}
		}

		public byte[] GetAsByteArray(string representationName)
		{
			return (byte[])Manager.GetAsNative(DataType.Representations[representationName], Value);
		}
		
		public void SetAsByteArray(string representationName, byte[] tempValue)
		{
			Value = Manager.SetAsNative(DataType.Representations[representationName], Value, tempValue);
		}

		public string AsBase64String
		{
			get { return Convert.ToBase64String(AsByteArray); }
			set { AsByteArray = Convert.FromBase64String(value); }
		}
		
		/// <summary>Opens a stream to read the data for this value. If this instance is native, the stream will be read only.</summary>
		public Stream OpenStream()
		{
			if (IsNative)
			{
				byte[] tempValue = 
					(DataType.NativeType == NativeAccessors.AsByteArray.NativeType) && !DataType.HasRepresentation(NativeAccessors.AsByteArray, true)
						? (byte[])Value
						: (byte[])Manager.GetAsNative(DataType.GetRepresentation(NativeAccessors.AsByteArray), this);
				return new MemoryStream(tempValue, 0, tempValue.Length, false, true);
			}
			return Manager.StreamManager.Open(StreamID, LockMode.Exclusive);
		}
		
		public Stream OpenStream(string representationName)
		{
			return ((IScalar)Manager.GetAsDataValue(DataType.Representations[representationName], AsNative)).OpenStream();
		}

		public override string ToString()
		{
			return AsDisplayString;
		}

		private class ScalarWriteContext : IWriteContext
		{
			public int Size { get; set; }
			public Stream WriteStream { get; set; }
			public IDataValue WriteValue { get; set; }
			public IWriteContext WriteContext { get; set; }
		}

		public static object CopyNativeAs(IScalar scalar, Schema.IDataType dataType)
		{
			if (scalar.IsNative)
			{
				ICloneable cloneable = scalar.AsNative as ICloneable;
				if (cloneable != null)
					return cloneable.Clone();
					
				if (scalar.DataType.IsCompound)
					return new CompoundScalar(scalar.DataType, (NativeRow)DataValue.CopyNative(scalar.Manager, scalar.DataType.CompoundRowType, ((CompoundScalar)scalar.AsNative).Value));
					
				return scalar.AsNative;
			}

			if (scalar.StreamID == StreamID.Null)
				return scalar.StreamID;

			return scalar.Manager.StreamManager.Reference(scalar.StreamID);
		}

		public static IWriteContext GetPhysicalSize(IScalar scalar, bool expandStreams)
		{
			int size = 1; // Scalar header
			Stream _writeStream; // saves the write stream between the GetPhysicalSize and WriteToPhysical calls
			IDataValue _writeValue; // saves the row instantiated to write the compound value if this is a compound scalar
			IWriteContext _writeContext; // saves the write context between the GetPhysicalSize and WriteToPhyhsical calls
		
			if (!scalar.IsNil)
			{
				if (scalar.IsNative)
				{
					if (scalar.DataType.IsCompound)
					{
						_writeValue = DataValue.FromNative(scalar.Manager, scalar.DataType.CompoundRowType, ((CompoundScalar)scalar.AsNative).Value);
						_writeContext = Row.GetPhysicalSize((IRow)_writeValue, expandStreams);
						return new ScalarWriteContext { Size = size + _writeContext.Size, WriteValue = _writeValue, WriteContext = _writeContext };
					}
					else
					{
						Streams.IConveyor conveyor = scalar.Manager.GetConveyor(scalar.DataType);
						if (conveyor.IsStreaming)
						{
							_writeStream = new MemoryStream(64);
							conveyor.Write(scalar.AsNative, _writeStream);
							return new ScalarWriteContext { Size = size + (int)_writeStream.Length, WriteStream = _writeStream };
						}

						return new ScalarWriteContext { Size = size + conveyor.GetSize(scalar.AsNative) };
					}
				}
					
				if (expandStreams)
				{
					_writeStream = scalar.Manager.StreamManager.Open(scalar.StreamID, LockMode.Exclusive);
					return new ScalarWriteContext { Size = size + (int)_writeStream.Length, WriteStream = _writeStream };
				}

				return new ScalarWriteContext { Size = size + StreamID.CSizeOf };
			}

			return new ScalarWriteContext { Size = size };
		}

		public static void WriteToPhysical(IScalar scalar, IWriteContext context, byte[] buffer, int offset, bool expandStreams)
		{
			var scalarContext = context as ScalarWriteContext;
			if (scalarContext == null)
				throw new RuntimeException(RuntimeException.Codes.UnpreparedWriteToPhysicalCall);

			// Write scalar header
			byte header = (byte)(scalar.IsNil ? 0 : 1);
			header |= (byte)(scalar.IsNative ? 2 : 0);
			header |= (byte)(expandStreams ? 4 : 0);
			buffer[offset] = header;
			offset++;

			if (!scalar.IsNil)
			{
				if (scalar.IsNative)
				{
					if (scalar.DataType.IsCompound)
					{
						DataValue.WriteToPhysical(scalarContext.WriteValue, scalarContext.WriteContext, buffer, offset, expandStreams);
						scalarContext.WriteValue.Dispose();
						scalarContext.WriteValue = null;
					}
					else
					{
						Streams.IConveyor conveyor = scalar.Manager.GetConveyor(scalar.DataType);
						if (conveyor.IsStreaming)
						{
							scalarContext.WriteStream.Position = 0;
							scalarContext.WriteStream.Read(buffer, offset, (int)scalarContext.WriteStream.Length);
							scalarContext.WriteStream.Close();
						}
						else
							conveyor.Write(scalar.AsNative, buffer, offset);
					}
				}
				else
				{
					if (expandStreams)
					{
						scalarContext.WriteStream.Position = 0;
						scalarContext.WriteStream.Read(buffer, offset, (int)scalarContext.WriteStream.Length);
						scalarContext.WriteStream.Close();
					}
					else
						scalar.StreamID.Write(buffer, offset);
				}
			}
		}

		public static void ReadFromPhysical(IScalar scalar, byte[] buffer, int offset)
		{
			// Clear current value
			if (scalar.ValuesOwned && !scalar.IsNative && (scalar.StreamID != StreamID.Null))
				scalar.Manager.StreamManager.Deallocate(scalar.StreamID);

			// Read scalar header
			byte header = buffer[offset];
			offset++;
			if ((header & 1) != 0) // if not nil
			{
				if ((header & 2) != 0)
				{
					if (scalar.DataType.IsCompound)
					{
						using (IRow row = (IRow)DataValue.FromPhysical(scalar.Manager, scalar.DataType.CompoundRowType, buffer, offset))
						{
							scalar.AsNative = new CompoundScalar(scalar.DataType, (NativeRow)row.AsNative);
							row.ValuesOwned = false;
						}
					}
					else
					{
						Streams.IConveyor conveyor = scalar.Manager.GetConveyor(scalar.DataType);
						if (conveyor.IsStreaming)
						{
							Stream stream = new MemoryStream(buffer, offset, buffer.Length - offset, false, true);
							scalar.AsNative = conveyor.Read(stream);
							stream.Close();
						}
						else
						{
							scalar.AsNative = conveyor.Read(buffer, offset);
						}
					}
				}
				else
				{
					if ((header & 4) != 0) // if expanded form
					{
						scalar.StreamID = scalar.Manager.StreamManager.Allocate();
						Stream stream = scalar.Manager.StreamManager.Open(scalar.StreamID, LockMode.Exclusive);
						stream.Write(buffer, offset, buffer.Length - offset);
						stream.Close();
					}
					else
						scalar.StreamID = StreamID.Read(buffer, offset);
				}
			}
			else
			{
				if ((header & 2) != 0)
					scalar.AsNative = null;
				else
					scalar.StreamID = StreamID.Null;
			}
		}
	}
	
	/// <summary>A scalar value which is currently contained inside a native row or list.</summary>	
	public abstract class InternedScalar : Scalar
	{
		public InternedScalar(IValueManager manager, Schema.IScalarType dataType) : base(manager, dataType, null)
		{
			ValuesOwned = false;
		}

		/// <summary>Indicates whether the value for this scalar is stored in its native representation.</summary>
		public override bool IsNative { get { return !(Value is StreamID); } }
	}
	
	public class RowInternedScalar : InternedScalar
	{
		public RowInternedScalar(IValueManager manager, Schema.IScalarType dataType, NativeRow nativeRow, int index) : base(manager, dataType)
		{
			_nativeRow = nativeRow;
			_index = index;
		}
		
		private NativeRow _nativeRow;
		private int _index;
		
		protected override object Value
		{
			get { return _nativeRow.Values[_index]; }
			set 
			{ 
				_nativeRow.Values[_index] = value; 
				if (_nativeRow.ModifiedFlags != null)
					_nativeRow.ModifiedFlags[_index] = true;
			}
		}
	}
	
	public class ListInternedScalar : InternedScalar
	{
		public ListInternedScalar(IValueManager manager, Schema.IScalarType dataType, NativeList nativeList, int index) : base(manager, dataType)
		{
			_nativeList = nativeList;
			_index = index;
		}
		
		private NativeList _nativeList;
		private int _index;
		
		protected override object Value
		{
			get { return _nativeList.Values[_index]; }
			set { _nativeList.Values[_index] = value; }
		}
	}
}

