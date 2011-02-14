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
		public DeferredWriteStream(Stream sourceStream)
		{
			_sourceStream = sourceStream;
			_stream = new MemoryStream();
		}

		protected Stream _sourceStream;
		protected virtual Stream SourceStream { get { return _sourceStream; } }

		private long _sourceLength = -1;
		private bool _sourcePositionReset;
		private Stream _stream;
		private bool _modified;
		public bool Modified { get { return _modified; } }
		
		public override void Close()
		{
			if (_sourceStream != null)
			{
				Flush();
				_stream.Close();
				_stream = null;
				SourceStream.Close();
				_sourceStream = null;
			}
			base.Close();
		}
		
		public void Reset()
		{
			_sourcePositionReset = true;
			_sourceLength = -1;
			_stream = new MemoryStream();
			_modified = false;
		}

		private long SourceLength
		{
			get
			{
				if (_sourceLength == -1)
					_sourceLength = SourceStream.Length;
				return _sourceLength;
			}
		}
		
		public override long Length 
		{ 
			get
			{
				if (_modified)
					return _stream.Length;
				else
					return SourceLength;
			}
		}
		
		public override void SetLength(long length)
		{
			if (Length != length)
			{
				if (!_modified)
				{
					_modified = true;
					CopyLocal(length);
				}
				
				_stream.SetLength(length);
			}
		}
		
		private void EnsureSourcePosition()
		{
			if (_sourcePositionReset)
			{
				SourceStream.Position = 0;
				_sourcePositionReset = false;
			}
		}
		
		private void CopyLocal(long length)
		{
			// Make sure that we have copied locally enough data to fill the stream to the given length
			if ((length >= _stream.Length) && (_stream.Length < SourceLength))
			{
				_stream.Position = _stream.Length;
				EnsureSourcePosition();
				StreamUtility.CopyStreamWithBufferSize(SourceStream, _stream, length - _stream.Length, StreamUtility.CopyBufferSize);
			}
		}
		
		public override long Position 
		{ 
			get { return _stream.Position; }
			set
			{
				if (_stream.Position != value)
				{
					if (!_modified)
					{
						CopyLocal(value);
						if (_stream.Length < value)
						{
							_modified = true;
							_stream.SetLength(value);
						}
					}
						
					_stream.Position = value;
				}
			}
		}
		
		public override int Read(byte[] buffer, int offset, int count)
		{
			if ((_stream.Position + count) >= _stream.Length)
			{
				long currentPosition = _stream.Position;
				CopyLocal(_stream.Position + count);
				_stream.Position = currentPosition;
			}
			return _stream.Read(buffer, offset, count);
		}
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (!_modified)
			{
				long currentPosition = _stream.Position;
				CopyLocal(SourceLength);
				_stream.Position = currentPosition;
				_modified = true;
			}
			
			_stream.Write(buffer, offset, count);
		}
		
		public override void Flush()
		{
			if (_modified)
			{
				SourceStream.SetLength(0);
				_stream.Position = 0;
				StreamUtility.CopyStream(_stream, SourceStream);
				_modified = false;
				_sourcePositionReset = true;
				_stream.Position = 0;
				_sourceLength = _stream.Length;
			}
		}
	}
}
