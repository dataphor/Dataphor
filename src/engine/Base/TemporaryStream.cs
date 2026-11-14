/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Alphora.Dataphor
{
	/// <summary> A non-persistant stream that overflows to a file when it grows sufficiently. </summary>
	public class TemporaryStream : Stream, IDisposable
	{
		public const int InitialBufferSize = 256;	// Initial size of the memory buffer
		public const int Threshold = 0xFFFF;	// Maximum size of the in memory portion
		
		private byte[] _buffer = new byte[InitialBufferSize];
		private FileStream _file;
		private long _position;
		private long _length;
		
		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return true; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override void Flush()
		{
			// No need to flush a temporary stream
		}

		public override long Length
		{
			get { return _length; }
		}

		public override long Position
		{
			get { return _position; }
			set
			{
				if (value != _position)
				{
					if (value > _length)
						throw new ArgumentOutOfRangeException("Position");
						
					if (value > Threshold)
						_file.Position = value - Threshold;
					else
					{
						// If we have overflowed, but the position is in the buffer portion, reset as much as possible
						if (_length > Threshold)
							_file.Position = 0;
					}					
					_position = value;
				}
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			// Limit the count to the number of bytes available
			count += (int)Math.Min(_length - (_position + (long)count), 0);
			
			// Ensure the range of the read
			if ((_position + count > _length) || (count < 0))
				throw new ArgumentOutOfRangeException("ACount");
				
			// Compute the number of bytes that will be read from the buffer portion
			int bufferCount = (int)Math.Max(Math.Min((long)Threshold - _position, (long)count), 0L);
			
			// Copy the memory portion
			Buffer.BlockCopy(_buffer, (int)_position, buffer, offset, bufferCount);
			
			// Copy the file portion
			if (count - bufferCount > 0)
				_file.Read(buffer, offset + bufferCount, count - bufferCount);
			
			// Increment the offset
			_position += count;
			
			return count;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long localOrigin;
			switch (origin)
			{
				case SeekOrigin.Begin : localOrigin = 0; break;
				case SeekOrigin.End : localOrigin = _length; break;
				default : localOrigin = _position; break;
			}
			Position = localOrigin + offset;
			return Position;
		}

		public override void SetLength(long value)
		{
			if (value > Threshold)
			{
				FileStream file = EnsureFile();
				file.SetLength(value - Threshold);
				_file = file;
			}
			else
			{
				DisposeFile();
				Array.Clear(_buffer, (int)value, _buffer.Length - (int)value);
			}

			// Ensure the size of the buffer
			EnsureCapacity((int)Math.Min(value, Threshold));

			// Constrain the position
			_position = Math.Min(_position, value);
			
			// Set the length
			_length = value;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			// Ensure the size of the buffer
			EnsureCapacity((int)Math.Min(_position + count, Threshold));

			// Compute the number of bytes that will be written to the buffer portion
			int bufferCount = (int)Math.Max(Math.Min(Threshold - _position, count), 0);

			// Write to the buffer
			if (_position < Threshold)
				Buffer.BlockCopy(buffer, offset, _buffer, (int)_position, bufferCount);
			
			if ((_position + count) - Threshold > 0)
			{
				// Ensure that the file is prepared
				FileStream file = EnsureFile();
			
				// Write to the file
				file.Write(buffer, offset + bufferCount, count - bufferCount);
				
				// Only set (if not already set) the file if we successfully did the write
				_file = file;
			}
			
			// Advance the position
			_position += count;
			
			// Increase the size (if necessary)
			_length = Math.Max(_position, _length);
		}

		private void EnsureCapacity(int capacity)
		{
			if (capacity >= _buffer.Length)
			{
				// Determine the factor of two necessary to meet the capacity
				int bufferSize = _buffer.Length;
				while (bufferSize < capacity)
					bufferSize <<= 1;
		
				// Allocate new buffer and copy			
				byte[] buffer = new byte[Math.Min(bufferSize, Threshold)];
				Buffer.BlockCopy(_buffer, 0, buffer, 0, Math.Min((int)_length, _buffer.Length));
				_buffer = buffer;
			}
		}
		
		private FileStream EnsureFile()
		{
			if (_file == null)
				return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
			else
				return _file;
		}

		private void DisposeFile()
		{
			if (_file != null)
			{
				string fileName = _file.Name;
				try
				{
					_file.Dispose();
				}
				finally
				{
					File.Delete(fileName);
				}
				_file = null;
			}
		}

		protected override void Dispose(bool disposing)
		{
			DisposeFile();
			_buffer = null;

			base.Dispose(disposing);
		}
	}
}
