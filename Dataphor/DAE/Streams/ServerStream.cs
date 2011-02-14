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
		public ServerStream(ServerStreamManager streamManager, StreamID streamID, Stream sourceStream) : base()
		{
			_streamManager = streamManager;
			_streamID = streamID;
			_sourceStream = sourceStream;
		}
		
		// StreamManager
		private ServerStreamManager _streamManager;
		public ServerStreamManager StreamManager { get { return _streamManager; } }
		
		// StreamID
		private StreamID _streamID;
		public StreamID StreamID { get { return _streamID; } }
		
		// SourceStream
		private Stream _sourceStream;
		public Stream SourceStream
		{
			get { return _sourceStream; }
			set 
			{
				long savePosition = _sourceStream == null ? -1 : _sourceStream.Position; 
				_sourceStream = value; 
				if ((_sourceStream != null) && (savePosition >= 0))
					_sourceStream.Position = savePosition;
			}
		}
		
		public override void Close()
		{
			_sourceStream = null;
			base.Close();
		}
		
		// Length
		public override long Length { get { return _sourceStream.Length; } }

		// SetLength
		public override void SetLength(long length)
		{
			_streamManager.Change(this);
			_sourceStream.SetLength(length);
		}
		
		// Position
		public override long Position
		{
			get { return _sourceStream.Position; }
			set 
			{
				if (value > Length)
					_streamManager.Change(this);
				_sourceStream.Position = value;
			}
		}
		
		// Read
		public override int Read(byte[] buffer, int offset, int count)
		{
			return _sourceStream.Read(buffer, offset, count);
		}
		
		// ReadByte
		public override int ReadByte()
		{
			return _sourceStream.ReadByte();
		}
		
		// Write
		public override void Write(byte[] buffer, int offset, int count)
		{
			_streamManager.Change(this);
			_sourceStream.Write(buffer, offset, count);
		}
		
		// WriteByte
		public override void WriteByte(byte tempValue)
		{
			_streamManager.Change(this);
			_sourceStream.WriteByte(tempValue);
		}
	}
}
