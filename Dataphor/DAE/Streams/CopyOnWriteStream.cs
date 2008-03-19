/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
namespace Alphora.Dataphor.DAE.Streams
{
	using System;
	using System.IO;
	using System.Runtime.Remoting.Lifetime;

	using Alphora.Dataphor;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Runtime;

	// TODO: Handle copy on write streams > 4GB
	public class CopyOnWriteStream : StreamBase
	{
		public CopyOnWriteStream(Stream ASourceStream) : base()
		{
			FSourceStream = ASourceStream;
		}
		
		public override void Close()
		{
			if (FModifiedStream != null)
			{
				FModifiedStream.Close();
				FModifiedStream = null;
			}
			
			FSourceStream = null;
			base.Close();
		}
		
		// SourceStream
		private Stream FSourceStream;
		
		// ModifiedStream
		private Stream FModifiedStream;

		// IsModified
		public bool IsModified { get { return FModifiedStream != null; } }
		
		// Length
		public override long Length 
		{ 
			get { return IsModified ? FModifiedStream.Length : FSourceStream.Length; }
		}
		
		private void SetModified()
		{
			if (!IsModified)
			{
				long LPosition = FSourceStream.Position;
				FSourceStream.Position = 0;
				if (FSourceStream.Length > Int32.MaxValue)
					throw new StreamsException(StreamsException.Codes.CopyOnWriteOverflow);
				FModifiedStream = new MemoryStream((int)FSourceStream.Length);
				StreamUtility.CopyStream(FSourceStream, FModifiedStream);
				FModifiedStream.Position = LPosition;
			}
		}
		
		// SetLength
		public override void SetLength(long ALength)
		{
			SetModified();
			FModifiedStream.SetLength(ALength);
		}
		
		// Position
		public override long Position
		{
			get { return IsModified ? FModifiedStream.Position : FSourceStream.Position; }
			set 
			{ 
				if (value >= Length)
					SetModified();

				if (IsModified)
					FModifiedStream.Position = value; 
				else 
					FSourceStream.Position = value; 
			}
		}
		
		// Read
		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			if (IsModified)
				return FModifiedStream.Read(ABuffer, AOffset, ACount);
			else
				return FSourceStream.Read(ABuffer, AOffset, ACount);
		}
		
		// ReadByte
		public override int ReadByte()
		{
			if (IsModified)
				return FModifiedStream.ReadByte();
			else
				return FSourceStream.ReadByte();
		}
		
		// Write
		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			SetModified();
			FModifiedStream.Write(ABuffer, AOffset, ACount);
		}
		
		// WriteByte
		public override void WriteByte(byte AValue)
		{
			SetModified();
			FModifiedStream.WriteByte(AValue);
		}
	}
}

