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
		public const int CInitialBufferSize = 256;	// Initial size of the memory buffer
		public const int CThreshold = 0xFFFF;	// Maximum size of the in memory portion
		
		private byte[] FBuffer = new byte[CInitialBufferSize];
		private FileStream FFile;
		private long FPosition;
		private long FLength;
		
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
			get { return FLength; }
		}

		public override long Position
		{
			get { return FPosition; }
			set
			{
				if (value != FPosition)
				{
					if (value > FLength)
						throw new ArgumentOutOfRangeException("Position");
						
					if (value > CThreshold)
						FFile.Position = value - CThreshold;
					else
					{
						// If we have overflowed, but the position is in the buffer portion, reset as much as possible
						if (FLength > CThreshold)
							FFile.Position = 0;
					}					
					FPosition = value;
				}
			}
		}

		public override int Read(byte[] ABuffer, int AOffset, int ACount)
		{
			// Limit the count to the number of bytes available
			ACount += (int)Math.Min(FLength - (FPosition + (long)ACount), 0);
			
			// Ensure the range of the read
			if ((FPosition + ACount > FLength) || (ACount < 0))
				throw new ArgumentOutOfRangeException("ACount");
				
			// Compute the number of bytes that will be read from the buffer portion
			int LBufferCount = (int)Math.Max(Math.Min((long)CThreshold - FPosition, (long)ACount), 0L);
			
			// Copy the memory portion
			Buffer.BlockCopy(FBuffer, (int)FPosition, ABuffer, AOffset, LBufferCount);
			
			// Copy the file portion
			if (ACount - LBufferCount > 0)
				FFile.Read(ABuffer, AOffset + LBufferCount, ACount - LBufferCount);
			
			// Increment the offset
			FPosition += ACount;
			
			return ACount;
		}

		public override long Seek(long AOffset, SeekOrigin AOrigin)
		{
			long LOrigin;
			switch (AOrigin)
			{
				case SeekOrigin.Begin : LOrigin = 0; break;
				case SeekOrigin.End : LOrigin = FLength; break;
				default : LOrigin = FPosition; break;
			}
			Position = LOrigin + AOffset;
			return Position;
		}

		public override void SetLength(long AValue)
		{
			if (AValue > CThreshold)
			{
				FileStream LFile = EnsureFile();
				LFile.SetLength(AValue - CThreshold);
				FFile = LFile;
			}
			else
			{
				DisposeFile();
				Array.Clear(FBuffer, (int)AValue, FBuffer.Length - (int)AValue);
			}

			// Ensure the size of the buffer
			EnsureCapacity((int)Math.Min(AValue, CThreshold));

			// Constrain the position
			FPosition = Math.Min(FPosition, AValue);
			
			// Set the length
			FLength = AValue;
		}

		public override void Write(byte[] ABuffer, int AOffset, int ACount)
		{
			// Ensure the size of the buffer
			EnsureCapacity((int)Math.Min(FPosition + ACount, CThreshold));

			// Compute the number of bytes that will be written to the buffer portion
			int LBufferCount = (int)Math.Max(Math.Min(CThreshold - FPosition, ACount), 0);

			// Write to the buffer
			if (FPosition < CThreshold)
				Buffer.BlockCopy(ABuffer, AOffset, FBuffer, (int)FPosition, LBufferCount);
			
			if ((FPosition + ACount) - CThreshold > 0)
			{
				// Ensure that the file is prepared
				FileStream LFile = EnsureFile();
			
				// Write to the file
				LFile.Write(ABuffer, AOffset + LBufferCount, ACount - LBufferCount);
				
				// Only set (if not already set) the file if we successfully did the write
				FFile = LFile;
			}
			
			// Advance the position
			FPosition += ACount;
			
			// Increase the size (if necessary)
			FLength = Math.Max(FPosition, FLength);
		}

		private void EnsureCapacity(int ACapacity)
		{
			if (ACapacity >= FBuffer.Length)
			{
				// Determine the factor of two necessary to meet the capacity
				int LBufferSize = FBuffer.Length;
				while (LBufferSize < ACapacity)
					LBufferSize <<= 1;
		
				// Allocate new buffer and copy			
				byte[] LBuffer = new byte[Math.Min(LBufferSize, CThreshold)];
				Buffer.BlockCopy(FBuffer, 0, LBuffer, 0, Math.Min((int)FLength, FBuffer.Length));
				FBuffer = LBuffer;
			}
		}
		
		private FileStream EnsureFile()
		{
			if (FFile == null)
				return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
			else
				return FFile;
		}

		private void DisposeFile()
		{
			if (FFile != null)
			{
				string LFileName = FFile.Name;
				try
				{
					FFile.Dispose();
				}
				finally
				{
					File.Delete(LFileName);
				}
				FFile = null;
			}
		}

		protected override void Dispose(bool ADisposing)
		{
			DisposeFile();
			FBuffer = null;

			base.Dispose(ADisposing);
		}
	}
}
