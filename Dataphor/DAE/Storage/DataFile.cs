/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

/*
	File Layout ->
		-A file is made up of n fixed sized (per file) pages
		-File page size must be a power of 2 and be >= 1024)
		-Pages relative to a file are identified by their sequential page number, starting at zero
		-The 54 least significant bits of a page ID identify a page number within a file
		-The 10 most significant bits of a page ID identify a file number within a data files set
		
	File Header Page ->
		-On Shutdown the system computes and stores a CRC32 for the file (including the master page). Because the
		 CRC value itself would be reflective, the CRC32 computation behavies as though this value is 0 when computing the CRC.
*/		
namespace Alphora.Dataphor.DAE.Storage
{
/*
	[StructLayout(LayoutKind.Explicit, Size = 10)]
	public struct FileVersion
	{
		[FieldOffset(0)]	public byte Major;
		[FieldOffset(1)]	public byte Minor;
		[FieldOffset(2)]	public uint Revision;
		[FieldOffset(6)]	public uint Build;

		public Version ToVersion()
		{
			return new Version(Major, Minor, (int)Revision, (int)Build);	// Note: FileVersion uses opposite nomeclature for Revision and Build
		}

		public static FileVersion FromVersion(Version AVersion)
		{
			FileVersion LResult = new FileVersion();
			LResult.Major = (byte)AVersion.Major;
			LResult.Minor = (byte)AVersion.Minor;
			LResult.Revision = (uint)AVersion.Build;
			LResult.Build = (uint)AVersion.Revision;
			return LResult;
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 43)]
	public struct FileHeader
	{
		public const int CSize = sizeof(FileHeader);

		[FieldOffset(0)]	public PageHeader PageHeader; // 12	bytes	(this should never move)
		[FieldOffset(12)]	public Guid TypeToken; // 16 bytes			(this should never move or change!)
		[FieldOffset(28)]	public FileVersion Version; // 10 bytes		(this should never move!)
		[FieldOffset(38)]	public int PageSize;
		[FieldOffset(42)]	public bool CheckedPageCount;
	}
	
	/// <summary> Represents a paged data file. </summary>
	public class DataFile : Disposable
	{
		/// <summary> This token is a unique identifier for files that can be read by the DataFile manager. </summary>
		public readonly static Guid CTypeToken = new Guid("EBBBBBBB-8AAA-4888-ACCC-7DFF1CE1ABC9");
		public const int CMinPageSize = 1024;

		#region Constructors and Dispose

		/// <summary> Initializes from an existing data file. </summary>
		/// <remarks> This class takes ownership of the provided file. </remarks>
		public unsafe DataFile(FastFile AFile, IntPtr ABuffer)
		{
			FFile = AFile;	// Take ownership

			FileHeader LFileHeader = *((FileHeader*)ABuffer);

			SetPageSize(LFileHeader.PageSize);

			// Verify the TypeToken
			if (LFileHeader.TypeToken != CTypeToken)
				throw new StorageException(StorageException.Codes.FileTypeTokenMismatch, AFileName);

			// Verify the file version
			Version LCurrentVersion = GetType().Assembly.GetName(false).Version;
			if (LFileHeader.Version.ToVersion() > LCurrentVersion)
				throw new StorageException(StorageException.Codes.FileVersionNewer, AFileName, LFileHeader.Version.ToVersion().ToString(), LCurrentVersion.ToString());

			FCheckedPageCount = LFileHeader.CheckedPageCount;
			FVersion = LFileHeader.Version;
		}

		/// <summary> Creates a new data file with the given file header page. </summary>
		/// <remarks> The provided file header page is assumed to have been initialized in all aspects except for the page size and file CRC32. </remarks>
		public unsafe DataFile(string AFileName, DataFileSettings ASettings)
		{
			if (ASettings.InitialPages < 1)
				throw new StorageException(StorageException.Codes.MinimumViolation, "InitialPages", 1);

			FFile = new FastFile(AFileName, false);
			try
			{
				SetPageSize(ASettings.PageSize);

				if (PageCount != 0)
					throw new StorageException(StorageException.Codes.FileCreateNotEmpty, AFileName);

				PageCount = ASettings.InitialPages;

			}
			catch
			{
				FFile.Dispose();
				FFile = null;
				throw;
			}
		}

		public static unsafe int GetPageSize(IntPtr ABuffer)
		{
			return ((FileHeader*)ABuffer)->PageSize;
		}

		public void Initialize(IntPtr ABuffer, long APageNumber)
		{
			if (APageNumber == 0)
			{
				FFileHeader.TypeToken = CTypeToken;
				FFileHeader.Version = Alphora.Dataphor.DAE.Storage.FileVersion.FromVersion(GetType().Assembly.GetName(false).Version);
				FFileHeader.PageSize = ASettings.PageSize;
				FFileHeader.CheckedPageCount = ASettings.CheckedPageCount;

				byte* LFilePage = stackalloc byte[FPageSize];
				*((FileHeader*)ABuffer) = FFileHeader;
			}
		}

		protected unsafe override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					if (FCheckCRC32 && !File.ReadOnly)
					{
						uint LCRC32 = ComputeCRC32();

						// Set the CRC32 in the file header page
						byte* LPagePointer = stackalloc byte[FPageSize];
						ReadPage((IntPtr)LPagePointer, 0);
						((FileHeader*)LPagePointer)->CRC32 = LCRC32;
						WritePage((IntPtr)LPagePointer, 0);
					}
				}
				finally
				{
					FFile.Dispose();
					FFile = null;
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}

		#endregion

		private FastFile FFile;
		public FastFile File { get { return FFile; } }

        /// <summary> Gets or sets the size of the actual file in whole pages. </summary>
		public long PageCount
		{
			get { return FFile.Length >> FPageSizeShift; }
			set { FFile.Length = value << FPageSizeShift; }
		}

		private int FCheckedPageCount;
		/// <summary> The number of pages that are checked for a valid CRC32. </summary>
		/// <remarks> A value of -1 indicates that all pages are checked. </remarks>
		public int CheckedPageCount { get { return FCheckedPageCount; } }

		private FileVersion FVersion;
		/// <summary> The version of the system that created the file. </summary>
		public FileVersion Version { get { return FVersion; } }

		#region Redundancy Checking

		/// <summary> Sets the CRC32 for the given page buffer. </summary>
		/// <remarks> The CRC of the page buffer is skipped for the purpose of CRC computation. </remarks>
		public unsafe void SetCRC32(IntPtr ABuffer)
		{
			((FileHeader*)ABuffer)->PageHeader.CRC32 = 
				CRC32Utility.GetAmendedCRC32
				(
					CRC32Utility.GetCRC32((byte*)ABuffer, 4), 
					((byte*)ABuffer)[8], 
					FPageSize - 8
				);
		}

		/// <summary> Returns true if the CRC32 for the given page is correct. </summary>
		/// <remarks> The CRC of the page buffer is skipped for the purpose of CRC computation. </remarks>
		public unsafe bool IsCRCValid(IntPtr ABuffer)
		{
			return
				((FileHeader*)ABuffer)->PageHeader.CRC32 ==
					CRC32Utility.GetAmendedCRC32
					(
						CRC32Utility.GetCRC32((byte*)ABuffer, 4), 
						((byte*)ABuffer)[8], 
						FPageSize - 8
					);
		}

		#endregion

		#region PagedSize

		private int FPageSize;
		/// <summary> The page size of the data file. </summary>
		public int PageSize { get { return FPageSize; } }

		private int FPageSizeShift;
		/// <summary> The power of two shift necessary to convert a byte offset to/from a page number. </summary>
		public int PageSizeShift { get { return FPageSizeShift; } }

		/// <summary> Validates and sets the page size for the data file. </summary>
		private void SetPageSize(int APageSize)
		{
			if (APageSize < CMinPageSize)
				throw new StorageException(StorageException.Codes.FilePageSizeBelowMinimum, APageSize, FFile.FileName, CMinPageSize);
			FPageSizeShift = ComputeAndVerifyShift(APageSize);
			FPageSize = APageSize;
		}

		private int ComputeAndVerifyShift(int APageSize)
		{
			// Note: this could be accomplished more eligantly if there were an effecient way to compute a log base 2 of APageSize
			for (int LShift = 0; LShift < 32; LShift++)
				if ((1 << LShift) == APageSize)
					return LShift;
			throw new StorageException(StorageException.Codes.FilePageSizeInvalid, APageSize, FFile.FileName);
		}

		#endregion

		#region Read & Write

		/// <summary> Reads a page of data from a file into a buffer using a page number. </summary>
		public void ReadPage(IntPtr ABuffer, long APageNumber)
		{
			FFile.Read(ABuffer, FPageSize, APageNumber << FPageSizeShift);
		}

		/// <summary> Reads the specified number of a list of pages from the file starting from the specified page number. </summary>
		public void ReadPages(IntPtr[] ABuffers, int ACount, long APageNumber)
		{
			FFile.Read(ABuffers, ACount, FPageSize, APageNumber << FPageSizeShift);
		}

		/// <summary> Begins asynchronous reading of a page of data from a file to a buffer using a page number. </summary>
		public IAsyncResult BeginReadPage(IntPtr ABuffer, long APageNumber, AsyncCallback ACallback, object AUserData)
		{
			return FFile.BeginRead(ABuffer, FPageSize, APageNumber << FPageSizeShift, ACallback, AUserData);
		}

		/// <summary> Begins asynchronous reading of a list of pages from a file starting from the specified page number. </summary>
		public IAsyncResult BeginReadPages(IntPtr[] ABuffers, int ACount, long APageNumber, AsyncCallback ACallback, object AUserData)
		{
			return FFile.BeginRead(ABuffers, ACount, FPageSize, APageNumber << FPageSizeShift, ACallback, AUserData);
		}

		/// <summary> Ends asynchronous reading of a page or list of pages. </summary>
		/// <remarks> If the I/O is not complete, this call waits. This method will throw an exception if there was an error 
		/// during the I/O operation.  This method is generally called from within the async callback.  Do nothing with the 
		/// AsyncResult object after calling this method. </remarks>
		/// <returns> The number of whole pages read. </returns>
		public int EndReadPage(IAsyncResult AResult)
		{
			return FFile.EndRead(AResult) >> FPageSizeShift;
		}

		/// <summary> Write a page of data to a file from a buffer using a page number. </summary>
		public void WritePage(IntPtr ABuffer, long APageNumber)
		{
			ComputeCRC32(ABuffer)
			FFile.Write(ABuffer, FPageSize, APageNumber << FPageSizeShift);
		}

		/// <summary> Writes the specified number of a list of pages to the file starting from the specified page number. </summary>
		public void WritePages(IntPtr[] ABuffers, int ACount, long APageNumber)
		{
			FFile.Write(ABuffers, ACount, FPageSize, APageNumber << FPageSizeShift);
		}

		/// <summary> Begins asynchronous writing of a page of data from a buffer to a file using a page number. </summary>
		public IAsyncResult BeginWritePage(IntPtr ABuffer, long APageNumber, AsyncCallback ACallback, object AUserData)
		{
			return FFile.BeginWrite(ABuffer, FPageSize, APageNumber << FPageSizeShift, ACallback, AUserData);
		}
		
		/// <summary> Begins asynchronous writing of a list of pages to a file starting from the specified page number. </summary>
		public IAsyncResult BeginWritePages(IntPtr[] ABuffers, int ACount, long APageNumber, AsyncCallback ACallback, object AUserData)
		{
			return FFile.BeginWrite(ABuffers, ACount, FPageSize, APageNumber << FPageSizeShift, ACallback, AUserData);
		}
		
		/// <summary> Ends asynchronous writing of a page or list of pages. </summary>
		/// <remarks> If the I/O is not complete, this call waits. This method will throw an exception if there was an error 
		/// during the I/O operation.  This method is generally called from within the async callback. Do nothing with the 
		/// AsyncResult object after calling this method. </remarks>
		/// <returns> The number of whole pages written. </returns>
		public int EndWritePage(IAsyncResult AResult)
		{
			return FFile.EndWrite(AResult) >> FPageSizeShift;
		}

		#endregion
	}

	public struct DataFileSettings
	{
		public bool CheckedPageCount;
		public int PageSize;
		public int InitialPages;
	}

/*
	public sealed class BufferUtility
	{
		/// <summary> Copies the contents of a string into a specified buffer. </summary>
		/// <param name="ATarget"> The target buffer size must be at least (ASource.Length * 2) + 1 bytes.</param>
		public static unsafe void EncodeString(string ASource, byte* ATarget)
		{
			Error.DebugAssertFail(ASource.Length < 256, "Cannot invoke CopyString on a string longer than 255.");
			*ATarget = (byte)ASource.Length;
			ATarget++;
			char* LCharTarget = (char*)ATarget;
			for (int i = 0; i < ASource.Length; i++)
				LCharTarget[i] = ASource[i];
		}

		/// <summary> Constructs a string (a copy) from a specified buffer. </summary>
		/// <param name="ASource"> The source must have a length byte followed by a series of characters. </param>
		public static unsafe string DecodeString(TypedReference ASource)
		{
			object LSource = TypedReference.ToObject(ASource);
			byte LSize = (byte)LSource;
			StringBuilder LResult = new StringBuilder(LSize);
			GCHandle LHandle = GCHandle.Alloc(LSource, GCHandleType.Pinned);
			try
			{
				byte* LSourcePtr = (byte*)LHandle.AddrOfPinnedObject();
				LSourcePtr++;
				char* LCharSource = (char*)LSourcePtr;
				for (int i = 0; i < LSize; i++)
					LResult.Append(LCharSource[i]);
			}
			finally
			{
				LHandle.Free();
			}
			return LResult.ToString();
		}
	}
*/
}
