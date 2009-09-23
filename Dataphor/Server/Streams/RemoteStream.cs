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
		int Read([Out] byte[] ABuffer, int AOffset, int ACount);
		int ReadByte();
		long Seek(long AOffset, SeekOrigin AOrigin);
		void Write([In] byte[] ABuffer, int AOffset, int ACount);
		void WriteByte(byte AByte);
		void Close();
	}

	/// <summary> Pass-through wrapper for IRemoteStream. </summary>
	public class RemoteStreamWrapper : Stream
	{
		public RemoteStreamWrapper(IRemoteStream ARemote)
		{
			FRemote = ARemote;
		}

		private IRemoteStream FRemote;
		public IRemoteStream Remote
		{
			get { return FRemote; }
		}

		public override bool CanRead
		{
			get { return FRemote.CanRead; }
		}
		
		public override bool CanSeek
		{
			get { return FRemote.CanSeek; }
		}
		
		public override bool CanWrite
		{
			get { return FRemote.CanWrite; }
		}
		
		public override void Flush()
		{
			FRemote.Flush();
		}

		public override long Length 
		{ 
			get { return FRemote.Length; } 
		}

		public override void SetLength(long ALength)
		{
			FRemote.SetLength(ALength);
		}

		public override long Position 
		{ 
			get { return FRemote.Position; }
			set { FRemote.Position = value; }
		}

		public override long Seek(long AOffset, SeekOrigin AOrigin)
		{
			return FRemote.Seek(AOffset, AOrigin);
		}

		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			return FRemote.Read(ABuffer, AOffset, ACount);
		}

		public override int ReadByte()
		{
			return FRemote.ReadByte();
		}

		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			FRemote.Write(ABuffer, AOffset, ACount);
		}

		public override void WriteByte(byte AByte)
		{
			FRemote.WriteByte(AByte);
		}

		public override void Close()
		{
			FRemote.Close();
			base.Close();
		}
	}
}
