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
	/// <summary> Like Stream but with one way read/write transport attributes to avoid extra read/write data marshalling. </summary>
	public interface IRemoteStream : IDisposable
	{
		long Length { get; }
		void SetLength(long AValue);
		long Position { get; set; }
		void Flush();
		bool CanRead { get; }
		bool CanSeek { get; }
		bool CanWrite { get; }
		int Read([Out] byte[] ABuffer, int offset, int ACount);
		int ReadByte();
		long Seek(long offset, SeekOrigin AOrigin);
		void Write([In] byte[] ABuffer, int offset, int ACount);
		void WriteByte(byte AByte);
		void Close();
	}

	/// <summary> Pass-through wrapper for IRemoteStream. </summary>
	public class RemoteStreamWrapper : Stream
	{
		public RemoteStreamWrapper(IRemoteStream remote)
		{
			_remote = remote;
		}

		private IRemoteStream _remote;
		public IRemoteStream Remote
		{
			get { return _remote; }
		}

		public override bool CanRead
		{
			get { return _remote.CanRead; }
		}
		
		public override bool CanSeek
		{
			get { return _remote.CanSeek; }
		}
		
		public override bool CanWrite
		{
			get { return _remote.CanWrite; }
		}
		
		public override void Flush()
		{
			_remote.Flush();
		}

		public override long Length 
		{ 
			get { return _remote.Length; } 
		}

		public override void SetLength(long length)
		{
			_remote.SetLength(length);
		}

		public override long Position 
		{ 
			get { return _remote.Position; }
			set { _remote.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _remote.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _remote.Read(buffer, offset, count);
		}

		public override int ReadByte()
		{
			return _remote.ReadByte();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_remote.Write(buffer, offset, count);
		}

		public override void WriteByte(byte byteValue)
		{
			_remote.WriteByte(byteValue);
		}

		public override void Close()
		{
			_remote.Close();
			base.Close();
		}
	}
}
