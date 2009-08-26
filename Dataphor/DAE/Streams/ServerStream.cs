/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Alphora.Dataphor.DAE.Streams
{
	public class ServerStream : StreamBase
	{
		public ServerStream(ServerStreamManager AStreamManager, StreamID AStreamID, Stream ASourceStream) : base()
		{
			FStreamManager = AStreamManager;
			FStreamID = AStreamID;
			FSourceStream = ASourceStream;
		}
		
		// StreamManager
		private ServerStreamManager FStreamManager;
		public ServerStreamManager StreamManager { get { return FStreamManager; } }
		
		// StreamID
		private StreamID FStreamID;
		public StreamID StreamID { get { return FStreamID; } }
		
		// SourceStream
		private Stream FSourceStream;
		public Stream SourceStream
		{
			get { return FSourceStream; }
			set 
			{
				long LSavePosition = FSourceStream == null ? -1 : FSourceStream.Position; 
				FSourceStream = value; 
				if ((FSourceStream != null) && (LSavePosition >= 0))
					FSourceStream.Position = LSavePosition;
			}
		}
		
		public override void Close()
		{
			System.Runtime.Remoting.RemotingServices.Disconnect(this);
			FSourceStream = null;
			base.Close();
		}
		
		// Length
		public override long Length { get { return FSourceStream.Length; } }

		// SetLength
		public override void SetLength(long ALength)
		{
			FStreamManager.Change(this);
			FSourceStream.SetLength(ALength);
		}
		
		// Position
		public override long Position
		{
			get { return FSourceStream.Position; }
			set 
			{
				if (value > Length)
					FStreamManager.Change(this);
				FSourceStream.Position = value;
			}
		}
		
		// Read
		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			return FSourceStream.Read(ABuffer, AOffset, ACount);
		}
		
		// ReadByte
		public override int ReadByte()
		{
			return FSourceStream.ReadByte();
		}
		
		// Write
		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			FStreamManager.Change(this);
			FSourceStream.Write(ABuffer, AOffset, ACount);
		}
		
		// WriteByte
		public override void WriteByte(byte AValue)
		{
			FStreamManager.Change(this);
			FSourceStream.WriteByte(AValue);
		}
	}
}
