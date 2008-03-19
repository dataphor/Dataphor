/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.InteropServices;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Streams
{
	/// <remarks> Provides a stream class which exposes a subset of another stream </remarks>
	public class CoverStream : StreamBase, IRemoteStream
	{
		public CoverStream(Stream ASource)
		{
			FSource = ASource;
			if (FSource == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);
		}
		
		public CoverStream(Stream ASource, bool AShouldClose)
		{
			FSource = ASource;
			if (FSource == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);
			FShouldClose = AShouldClose;
		}
		
		public CoverStream(Stream ASource, long AOffset, long ACount)
		{
			FSource = ASource;
			if (FSource == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);

			if (AOffset < 0)
				throw new StreamsException(StreamsException.Codes.OffsetMustBeNonNegative);

			FOffset = AOffset;
			FCount = ACount;
		}
		
		public override object InitializeLifetimeService()
		{
			return null;
		}
		
		private bool FShouldClose;
		public bool ShouldClose
		{
			get { return FShouldClose; }
			set { FShouldClose = value; }
		}
		
		public override void Close()
		{
			if ((FSource != null) && FShouldClose)
				FSource.Close();
			FSource = null;
			System.Runtime.Remoting.RemotingServices.Disconnect(this);
			base.Close();
		}

		private Stream FSource;
		private long FOffset;
		private long FCount;
		
		public override long Length
		{
			get
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					return FCount == 0 ? FSource.Length : FCount;
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
		}
		
		public override void SetLength(long ALength)
		{
			if (FCount == 0)
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					FSource.SetLength(ALength);
					FPosition = FSource.Position;
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
			else
			{
				throw new NotSupportedException();
			}
		}
		
		private long FPosition;
		public override long Position
		{
			get
			{
				return FPosition;
			}
			set
			{
				long LLength = Length;
				FPosition = value > LLength ? LLength : value;
			}
		}
		
		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			long LLength = Length;
			if (ACount + FPosition > LLength)
				ACount = (int)(LLength - FPosition); // This cast is safe because LLength - Position < ACount <= int.MaxValue
			
			int LCount = 0;
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{
			#endif
				FSource.Position = FOffset + FPosition;
				LCount = FSource.Read(ABuffer, AOffset, ACount);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			FPosition += LCount;
			return LCount;
		}
		
		public override int ReadByte()
		{
			if (FPosition == Length)
				return -1;
			else
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					FSource.Position = FOffset + FPosition;
					FPosition++;
					return FSource.ReadByte();
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
		}
		
		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			long LLength = Length;
			if ((FCount != 0) && (ACount + FPosition > LLength))
				ACount = (int)(LLength - FPosition); // This cast is safe because FCount - Position < ACount <= int.MaxValue
			
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{
			#endif
				FSource.Position = FOffset + FPosition;
				FSource.Write(ABuffer, AOffset, ACount);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			FPosition += ACount;
		}
		
		public override void WriteByte(byte AValue)
		{
			if ((FCount != 0) && (FPosition == Length))
				throw new EndOfStreamException();
	
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{				
			#endif
				FSource.Position = FOffset + FPosition;
				FSource.WriteByte(AValue);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			FPosition++;
		}

		void IRemoteStream.Write([In] byte[] ABuffer, int AOffset, int ACount)
		{
			Write(ABuffer, AOffset, ACount);
		}

		int IRemoteStream.Read([Out] byte[] ABuffer, int AOffset, int ACount)
		{
			return Read(ABuffer, AOffset, ACount);
		}
	}
}

