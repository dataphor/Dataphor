/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	
	public class DeferredWriteStream : StreamBase
	{
		public DeferredWriteStream(Stream ASourceStream)
		{
			FSourceStream = ASourceStream;
			FStream = new MemoryStream();
		}

		protected Stream FSourceStream;
		protected virtual Stream SourceStream { get { return FSourceStream; } }

		private long FSourceLength = -1;
		private bool FSourcePositionReset;
		private Stream FStream;
		private bool FModified;
		public bool Modified { get { return FModified; } }
		
		public override void Close()
		{
			if (FSourceStream != null)
			{
				Flush();
				FStream.Close();
				FStream = null;
				SourceStream.Close();
				FSourceStream = null;
			}
			base.Close();
		}
		
		public void Reset()
		{
			FSourcePositionReset = true;
			FSourceLength = -1;
			FStream = new MemoryStream();
			FModified = false;
		}

		private long SourceLength
		{
			get
			{
				if (FSourceLength == -1)
					FSourceLength = SourceStream.Length;
				return FSourceLength;
			}
		}
		
		public override long Length 
		{ 
			get
			{
				if (FModified)
					return FStream.Length;
				else
					return SourceLength;
			}
		}
		
		public override void SetLength(long ALength)
		{
			if (Length != ALength)
			{
				if (!FModified)
				{
					FModified = true;
					CopyLocal(ALength);
				}
				
				FStream.SetLength(ALength);
			}
		}
		
		private void EnsureSourcePosition()
		{
			if (FSourcePositionReset)
			{
				SourceStream.Position = 0;
				FSourcePositionReset = false;
			}
		}
		
		private void CopyLocal(long ALength)
		{
			// Make sure that we have copied locally enough data to fill the stream to the given length
			if ((ALength >= FStream.Length) && (FStream.Length < SourceLength))
			{
				FStream.Position = FStream.Length;
				EnsureSourcePosition();
				StreamUtility.CopyStreamWithBufferSize(SourceStream, FStream, ALength - FStream.Length, StreamUtility.CCopyBufferSize);
			}
		}
		
		public override long Position 
		{ 
			get { return FStream.Position; }
			set
			{
				if (FStream.Position != value)
				{
					if (!FModified)
					{
						CopyLocal(value);
						if (FStream.Length < value)
						{
							FModified = true;
							FStream.SetLength(value);
						}
					}
						
					FStream.Position = value;
				}
			}
		}
		
		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			if ((FStream.Position + ACount) >= FStream.Length)
			{
				long LCurrentPosition = FStream.Position;
				CopyLocal(FStream.Position + ACount);
				FStream.Position = LCurrentPosition;
			}
			return FStream.Read(ABuffer, AOffset, ACount);
		}
		
		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			if (!FModified)
			{
				long LCurrentPosition = FStream.Position;
				CopyLocal(SourceLength);
				FStream.Position = LCurrentPosition;
				FModified = true;
			}
			
			FStream.Write(ABuffer, AOffset, ACount);
		}
		
		public override void Flush()
		{
			if (FModified)
			{
				SourceStream.SetLength(0);
				FStream.Position = 0;
				StreamUtility.CopyStream(FStream, SourceStream);
				FModified = false;
				FSourcePositionReset = true;
				FStream.Position = 0;
				FSourceLength = FStream.Length;
			}
		}
	}

	/// <summary>
	/// Primary focus of the LocalStream class is to keep data in this stream once it has been read
	/// for as long as possible.  Data will be read from the source stream as necessary, and, if
	/// modified, only a call to Flush will push the data back into the source stream.
	/// The given source stream is assumed to be at position 0.
	/// </summary>
	public class LocalStream : DeferredWriteStream
	{
		public LocalStream(LocalStreamManager AStreamManager, StreamID AStreamID, DAE.Runtime.LockMode AMode) : base(null)
		{
			FStreamManager = AStreamManager;
			FStreamID = AStreamID;
			FMode = AMode;
		}

		private LocalStreamManager FStreamManager;		
		private DAE.Runtime.LockMode FMode;
		private StreamID FStreamID;
		
		protected override Stream SourceStream
		{
			get
			{
				if (FSourceStream == null)
					FSourceStream = new RemoteStreamWrapper(FStreamManager.SourceStreamManager.OpenRemote(FStreamID, FMode));
				return FSourceStream;
			}
		}
		
		public override void Close()
		{
			if (FStreamManager != null)
			{
				base.Close();
				FStreamManager.Close(FStreamID);
				FStreamManager = null;
			}
		}
	}
}
