/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Runtime.InteropServices;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Streams
{
	/// <remarks> Provides a stream class which exposes a subset of another stream </remarks>
	public class CoverStream : StreamBase, IRemoteStream
	{
		public CoverStream(Stream source)
		{
			_source = source;
			if (_source == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);
		}
		
		public CoverStream(Stream source, bool shouldClose)
		{
			_source = source;
			if (_source == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);
			_shouldClose = shouldClose;
		}
		
		public CoverStream(Stream source, long offset, long count)
		{
			_source = source;
			if (_source == null)
				throw new StreamsException(StreamsException.Codes.CoverStreamSourceNull);

			if (offset < 0)
				throw new StreamsException(StreamsException.Codes.OffsetMustBeNonNegative);

			_offset = offset;
			_count = count;
		}
		
		private bool _shouldClose;
		public bool ShouldClose
		{
			get { return _shouldClose; }
			set { _shouldClose = value; }
		}
		
		public override void Close()
		{
			if ((_source != null) && _shouldClose)
				_source.Close();
			_source = null;
			base.Close();
		}

		private Stream _source;
		private long _offset;
		private long _count;
		
		public override long Length
		{
			get
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					return _count == 0 ? _source.Length : _count;
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
		}
		
		public override void SetLength(long length)
		{
			if (_count == 0)
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					_source.SetLength(length);
					_position = _source.Position;
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
			else
			{
				throw new NotSupportedException();
			}
		}
		
		private long _position;
		public override long Position
		{
			get
			{
				return _position;
			}
			set
			{
				long length = Length;
				_position = value > length ? length : value;
			}
		}
		
		public override int Read(byte[] buffer, int offset, int count)
		{
			long length = Length;
			if (count + _position > length)
				count = (int)(length - _position); // This cast is safe because LLength - Position < ACount <= int.MaxValue
			
			int localCount = 0;
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{
			#endif
				_source.Position = _offset + _position;
				localCount = _source.Read(buffer, offset, count);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			_position += localCount;
			return localCount;
		}
		
		public override int ReadByte()
		{
			if (_position == Length)
				return -1;
			else
			{
				#if USECONCURRENTCOVERSTREAM
				lock (FSource)
				{
				#endif
					_source.Position = _offset + _position;
					_position++;
					return _source.ReadByte();
				#if USECONCURRENTCOVERSTREAM
				}
				#endif
			}
		}
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			long length = Length;
			if ((_count != 0) && (count + _position > length))
				count = (int)(length - _position); // This cast is safe because _count - Position < ACount <= int.MaxValue
			
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{
			#endif
				_source.Position = _offset + _position;
				_source.Write(buffer, offset, count);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			_position += count;
		}
		
		public override void WriteByte(byte tempValue)
		{
			if ((_count != 0) && (_position == Length))
				throw new EndOfStreamException();
	
			#if USECONCURRENTCOVERSTREAM
			lock (FSource)
			{				
			#endif
				_source.Position = _offset + _position;
				_source.WriteByte(tempValue);
			#if USECONCURRENTCOVERSTREAM
			}
			#endif
			_position++;
		}

		void IRemoteStream.Write([In] byte[] buffer, int offset, int count)
		{
			Write(buffer, offset, count);
		}

		int IRemoteStream.Read([Out] byte[] buffer, int offset, int count)
		{
			return Read(buffer, offset, count);
		}
	}
}

