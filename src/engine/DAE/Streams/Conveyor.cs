/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
    using System;
    using System.IO;
	using System.Text;
    using System.Collections;

    using Alphora.Dataphor;
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using System.Runtime.Serialization;
	
	/// <remarks>    
    /// A Conveyor provides host language access to the physical representation of Dataphor values stored in streams
    /// All Conveyors must descend from this base and provide a single constructor which takes a Stream as its only parameter
    /// All reading and writing of Dataphor values in the host language should be done through these Conveyors, 
    /// although it is not strictly necessary to do so (physical access could occur in instruction implementations, but is not recommended)
    /// The intent of the Conveyor is to provide a single access point for host language access to Dataphor values.
    /// </remarks>
    public abstract class Conveyor : IConveyor
    {
		public Conveyor() {}

		/// <summary>Indicates whether this conveyor uses the streaming read/write methods, or the byte[] read/write and GetSize methods.</summary>
		public virtual bool IsStreaming { get { return false; } }

		/// <summary>Returns the size in bytes required to store the given value.</summary>		
		public virtual int GetSize(object tempValue)
		{
			throw new NotSupportedException();
		}

		/// <summary>Returns the native representation of the value stored in the buffer given by ABuffer, beginning at the offset given by offset.</summary>
		public virtual object Read(byte[] buffer, int offset)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of AValue into the buffer given by ABuffer beginning at the offset given by offset</summary>		
		public virtual void Write(object tempValue, byte[] buffer, int offset)
		{
			throw new NotSupportedException();
		}

		/// <summary>Returns the native representation of the value stored in the stream given by AStream.</summary>
		public virtual object Read(Stream stream)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Writes the physical representation of the value given by AValue into the stream given by AStream.</summary>
		public virtual void Write(object tempValue, Stream stream)
		{
			throw new NotSupportedException();
		}
    }
    
    public class BooleanConveyor : Conveyor
    {
		public BooleanConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(bool));
		}

		public override object Read(byte[] buffer, int offset)
		{
			return buffer[offset] != 0;
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)((bool)tempValue ? 1 : 0);
		}
    }
    
    public class ByteConveyor : Conveyor
    {
		public ByteConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(byte));
		}

		public override object Read(byte[] buffer, int offset)
		{
			return buffer[offset];
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			buffer[offset] = (byte)tempValue;
		}
    }
    
    public class Int16Conveyor : Conveyor
    {
		public Int16Conveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(short));
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((short*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((short*)LBufferPtr) = (short)AValue;
			}
		}
		
		#else
		
		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadInt16(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteInt16(buffer, offset, (short)tempValue);
		}
		
		#endif		
    }
    
    public class Int32Conveyor : Conveyor
    {
		public Int32Conveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(int));
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((int*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((int*)LBufferPtr) = (int)AValue;
			}
		}
		
		#else

		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadInt32(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteInt32(buffer, offset, (int)tempValue);
		}

		#endif
	}
    	
    public class Int64Conveyor : Conveyor
    {
		public Int64Conveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(long));
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((long*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((long*)LBufferPtr) = (long)AValue;
			}
		}
		
		#else

		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadInt64(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteInt64(buffer, offset, (long)tempValue);
		}

		#endif
	}
    
    public class DecimalConveyor : Conveyor
    {
		public DecimalConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(decimal));
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((decimal*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((decimal*)LBufferPtr) = (decimal)AValue;
			}
		}
		
		#else

		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadDecimal(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteDecimal(buffer, offset, (decimal)tempValue);
		}

		#endif
	}
    
    public class StringConveyor : Conveyor
    {
		public StringConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return sizeof(int) + (((string)tempValue).Length * 2); 
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			int LLength = 0;
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				LLength = *((int*)LBufferPtr);				
			}
			
			if (LLength > 0)
			{
				fixed (byte* LBufferPtr = &(ABuffer[offset + sizeof(int)]))
				{
					return new String((char*)LBufferPtr, 0, LLength);
				}
			}

			return String.Empty;
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			string LString = (String)AValue;
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((int*)LBufferPtr) = LString.Length;
			}
			
			if (LString.Length > 0)
			{
				fixed (byte* LBufferPtr = &(ABuffer[offset + sizeof(int)]))
				{
					char* LCurrentPtr = (char*)LBufferPtr;
					for (int LIndex = 0; LIndex < LString.Length; LIndex++)
					{
						*LCurrentPtr = LString[LIndex];
						LCurrentPtr++;
					}
				}
			}
		}
		
		#else

		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadString(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteString(buffer, offset, (String)tempValue);
		}
		
		#endif
    }
    
    public class DateTimeConveyor : Conveyor
    {
		public DateTimeConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return (sizeof(long));
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return new DateTime(*((long*)LBufferPtr));
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((long*)LBufferPtr) = ((DateTime)AValue).Ticks;
			}
		}
	
		#else
		
		public override object Read(byte[] buffer, int offset)
		{
			return new DateTime(ByteArrayUtility.ReadInt64(buffer, offset));
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteInt64(buffer, offset, ((DateTime)tempValue).Ticks);
		}

		#endif		
    }
    
    public class TimeSpanConveyor : Conveyor
    {
		public TimeSpanConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return sizeof(long);
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return new TimeSpan(*((long*)LBufferPtr));
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((long*)LBufferPtr) = ((TimeSpan)AValue).Ticks;
			}
		}

		#else

		public override object Read(byte[] buffer, int offset)
		{
			return new TimeSpan(ByteArrayUtility.ReadInt64(buffer, offset));
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteInt64(buffer, offset, ((TimeSpan)tempValue).Ticks);
		}

		#endif
	}
    
    public class GuidConveyor : Conveyor
    {
		public GuidConveyor() : base() {}
		
		public override int GetSize(object tempValue)
		{
			return 16;
		}

		#if USE_UNSAFE
		
		public unsafe override object Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((Guid*)LBufferPtr);
			}
		}

		public unsafe override void Write(object AValue, byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((Guid*)LBufferPtr) = (Guid)AValue;
			}
		}

		#else

		public override object Read(byte[] buffer, int offset)
		{
			return ByteArrayUtility.ReadGuid(buffer, offset);
		}

		public override void Write(object tempValue, byte[] buffer, int offset)
		{
			ByteArrayUtility.WriteGuid(buffer, offset, (Guid)tempValue);
		}

		#endif
    }
    
	public class BOPObjectConveyor : Conveyor
	{
		public BOPObjectConveyor() : base() {}
		
		public override bool IsStreaming { get { return true; } }
		
		private Serializer _serializer = new Serializer();
		private Deserializer _deserializer = new Deserializer();
		
		public override object Read(Stream stream)
		{
			return _deserializer.Deserialize(stream, null);
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			_serializer.Serialize(stream, tempValue);
		}
	}

	public class BinaryConveyor : Conveyor
	{
		public override bool IsStreaming { get { return true; } }
		
		public override object Read(Stream stream)
		{
			byte[] data = new byte[stream.Length];
			stream.Read(data, 0, (int)stream.Length);
			return data;
		}
		
		public override void Write(object tempValue, Stream stream)
		{
			byte[] localTempValue = (byte[])tempValue;
			stream.Write(localTempValue, 0, localTempValue.Length);
		}
	}
}

