/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

namespace Alphora.Dataphor.DAE.Storage
{
	/// <summary> Stream mapped into data pages </summary>
/*	public class PagedStream : Stream
	{
		public PagedStream(PagedFile AFile, UInt32 APageNumber) : base()
		{
			FFile = AFile;
			FPageNumber = APageNumber;
		}

		private UInt32 FPageNumber;
		private PagedFile FFile;

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}
		
		private long FSize;

		public override long Length
		{
			get
			{
				return FSize;
			}
		}

		public override void SetLength(long AValue)
		{
			if (AValue > FSize)	// Grow
			{
				
			}
			else if (AValue < FSize) // Shrink
			{
			}
		}


		private long FPosition;

		public override long Position
		{
			get
			{
				return FPosition;
			}
			set
			{
				Seek(value, SeekOrigin.Begin);
			}
		}

		public override long Seek(long AOffset, SeekOrigin AOrigin)
		{
			// Make AOrigin relative to the beginning of the file
			switch (AOrigin)
			{
				case SeekOrigin.Current :
					AOffset += FPosition;
					break;
				case SeekOrigin.End :
					AOffset += FSize;
					break;
			}

			// TODO: Finish Seek
		}

		public override int Read(out byte[] ABuffer, int AOffset, int ACount)
		{
		}

		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
		}

		public override void Flush()
		{
			// No buffering... nothing to do
		}
	}
		*/
}
