/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

namespace Alphora.Dataphor.DAE.Streams
{
	using Alphora.Dataphor.DAE.Runtime;

	// TODO: Handle copy on write streams > 4GB
	public class CopyOnWriteStream : StreamBase
	{
		public CopyOnWriteStream(Stream sourceStream) : base()
		{
			_sourceStream = sourceStream;
		}
		
		public override void Close()
		{
			if (_modifiedStream != null)
			{
				_modifiedStream.Close();
				_modifiedStream = null;
			}
			
			_sourceStream = null;
			base.Close();
		}
		
		// SourceStream
		private Stream _sourceStream;
		
		// ModifiedStream
		private Stream _modifiedStream;

		// IsModified
		public bool IsModified { get { return _modifiedStream != null; } }
		
		// Length
		public override long Length 
		{ 
			get { return IsModified ? _modifiedStream.Length : _sourceStream.Length; }
		}
		
		private void SetModified()
		{
			if (!IsModified)
			{
				long position = _sourceStream.Position;
				_sourceStream.Position = 0;
				if (_sourceStream.Length > Int32.MaxValue)
					throw new StreamsException(StreamsException.Codes.CopyOnWriteOverflow);
				_modifiedStream = new MemoryStream((int)_sourceStream.Length);
				StreamUtility.CopyStream(_sourceStream, _modifiedStream);
				_modifiedStream.Position = position;
			}
		}
		
		// SetLength
		public override void SetLength(long length)
		{
			SetModified();
			_modifiedStream.SetLength(length);
		}
		
		// Position
		public override long Position
		{
			get { return IsModified ? _modifiedStream.Position : _sourceStream.Position; }
			set 
			{ 
				if (value >= Length)
					SetModified();

				if (IsModified)
					_modifiedStream.Position = value; 
				else 
					_sourceStream.Position = value; 
			}
		}
		
		// Read
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (IsModified)
				return _modifiedStream.Read(buffer, offset, count);
			else
				return _sourceStream.Read(buffer, offset, count);
		}
		
		// ReadByte
		public override int ReadByte()
		{
			if (IsModified)
				return _modifiedStream.ReadByte();
			else
				return _sourceStream.ReadByte();
		}
		
		// Write
		public override void Write(byte[] buffer, int offset, int count)
		{
			SetModified();
			_modifiedStream.Write(buffer, offset, count);
		}
		
		// WriteByte
		public override void WriteByte(byte tempValue)
		{
			SetModified();
			_modifiedStream.WriteByte(tempValue);
		}
	}
}

