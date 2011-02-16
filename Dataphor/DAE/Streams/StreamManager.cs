/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	using System.Runtime.Serialization;
	
	using Alphora.Dataphor.DAE.Runtime;
	
	/// <remarks>Value type for a stream identifier.</remarks>
	[DataContract]
	public struct StreamID : IComparable
	{
		public const int CSizeOf = sizeof(UInt64);
		public static StreamID Null = new StreamID(0);
		
		public StreamID(UInt64 AValue)
		{
			Value = AValue;
		}

		[DataMember]
		public UInt64 Value;

		public override string ToString()
		{
			return Value.ToString();
		}
		
		// Comparison
		public override bool Equals(object AObject)
		{
			return (AObject is StreamID) && (Value == ((StreamID)AObject).Value);
		}
		
		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
		
		public int CompareTo(object AObject)
		{
			if (!(AObject is StreamID))
				throw new StreamsException(StreamsException.Codes.InvalidComparison, AObject.GetType().Name);
				
			StreamID LValue = (StreamID)AObject;

			return (LValue.Value == Value ? 0 : (LValue.Value > Value ? 1 : -1));
		}

		// Operators
		public static bool operator ==(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return ALeftStreamID.Equals(ARightStreamID);
		}
		
		public static bool operator !=(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return !(ALeftStreamID.Equals(ARightStreamID));
		}
		
		public static bool operator >(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return ALeftStreamID.CompareTo(ARightStreamID) > 0;
		}
		
		public static bool operator >=(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return ALeftStreamID.CompareTo(ARightStreamID) >= 0;
		}
		
		public static bool operator <(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return ALeftStreamID.CompareTo(ARightStreamID) < 0;
		}
		
		public static bool operator <=(StreamID ALeftStreamID, StreamID ARightStreamID)
		{
			return ALeftStreamID.CompareTo(ARightStreamID) <= 0;
		}

		#if USE_UNSAFE
		
		public unsafe void Write(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				*((StreamID*)LBufferPtr) = (StreamID)Value;
			}
		}
		
		public static unsafe StreamID Read(byte[] ABuffer, int offset)
		{
			fixed (byte* LBufferPtr = &(ABuffer[offset]))
			{
				return *((StreamID*)LBufferPtr);
			}
		}
		
		#else

		public void Write(byte[] ABuffer, int offset)
		{
			ByteArrayUtility.WriteInt64(ABuffer, offset, (long)Value);
		}

		public static StreamID Read(byte[] ABuffer, int offset)
		{
			return new StreamID((ulong)ByteArrayUtility.ReadInt64(ABuffer, offset));
		}
		
		#endif
	}
	
	#if USETYPEDLIST
	public class StreamIDList : TypedList
	{
		public StreamIDList() : base(typeof(StreamID)){}
		
		public new StreamID this[int AIndex]
		{
			get { return (StreamID)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class StreamIDList : BaseList<StreamID> { }
	#endif
	
	public interface IStreamManager
	{
		StreamID Allocate();
		StreamID Reference(StreamID AStreamID);
		void Deallocate(StreamID AStreamID);
		Stream Open(StreamID AStreamID, LockMode AMode);
		IRemoteStream OpenRemote(StreamID AStreamID, LockMode AMode);
		#if UNMANAGEDSTREAM
		void Close(StreamID AStreamID);
		#endif
	}
	
	/// <remarks>Provides the central management object for storage level streams.</remarks>
	public abstract class StreamManager : Disposable
	{
		public abstract StreamID Allocate();
		public abstract StreamID Reference(StreamID streamID);
		public abstract void Deallocate(StreamID streamID);
		public abstract Stream Open(int ownerID, StreamID streamID, LockMode mode);
		public IRemoteStream OpenRemote(int ownerID, StreamID streamID, LockMode mode)
		{
			return (IRemoteStream)Open(ownerID, streamID, mode);
		}
		public abstract void Close(int ownerID, StreamID streamID);
	}
	
	public interface IStreamProvider : IDisposable
	{
		Stream Open(StreamID AStreamID);
		void Close(StreamID AStreamID);
		void Destroy(StreamID AStreamID);
		void Reassign(StreamID AOldStreamID, StreamID ANewStreamID);
	}
	
	public abstract class StreamProvider : Disposable, IStreamProvider
	{
		public abstract Stream Open(StreamID streamID);
		public abstract void Close(StreamID streamID);
		public abstract void Destroy(StreamID streamID);
		public abstract void Reassign(StreamID oldStreamID, StreamID newStreamID);
	}
}

