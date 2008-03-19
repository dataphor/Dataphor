/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Storage
{
/*
	[StructLayout(LayoutKind.Explicit, Size = 16)]
	public struct FileGroupFileHeader
	{
		public const int CSize = sizeof(FileGroupFileHeader);

		[FieldOffset(0)]	public Guid FileGroupID; // 16 bytes
	}

	public class FileGroup : Disposable
	{
		public const int CMaxFileGroup = 1023;
		public const int CFileHeaderSize = DataFileHeader.CFileHeaderSize + sizeof(FileGroupFileHeader);

		public FileGroup(string AMasterFileName, bool AReadOnly)
		{

			FMasterFileName = Path.GetFullPath(AMasterFileName);
			LoadDataFile(1, FMasterFileName, AReadOnly);
		}

		public FileGroup(string AMasterFileName, FileGroupSettings ASettings)
		{
			// Generate a unique FileGroup ID
			FID = Guid.NewGuid();

			// Ensure that the target path exists
			FMasterFileName = Path.GetFullPath(AMasterFileName);
			EnsureFilePathExists(FMasterFileName);

			// Create the master data file
			CreateDataFile(1, FMasterFileName, ASettings);
		}

		protected override void Dispose(bool ADisposing)
		{
			// Unload each error optimistically
			ErrorList LErrors = new ErrorList();
			for (short i = 1; i < FFiles.Length; i++)
			{
				try
				{
					UnloadDataFile(i);
				}
				catch (Exception LException)
				{
					LErrors.Add(LException);
				}
			}

			base.Dispose(ADisposing);
			
			LErrors.Throw();
		}

		// TODO: Latching for the FileGroup structure

		private Guid FID;
		/// <summary> The unique identifier for the file group. </summary>
		public Guid ID { get { return FID; } }

		private DataFile[] FFiles = new DataFile[CMaxFileGroup];
		/// <summary> Gets and sets the DataFile entries in the group. </summary>
		/// <remarks> The result will be null if there is no file in the bucket at the specified index.  This is a 1 based list (file 0 is reserved). </remarks>
		public DataFile this[short AFileNumber]
		{
			get 
			{ 
				Error.AssertFail((AFileNumber > 0) && (AFileNumber <= CMaxFileGroup), "Invalid file number ({0}) provided to FileGroup.DataFile[].Get.", AFileNumber);
				return FFiles[AFileNumber - 1]; 
			}
			set
			{
				Error.AssertFail((AFileNumber > 0) && (AFileNumber <= CMaxFileGroup), "Invalid file number ({0}) provided to FileGroup.DataFile[].Set.", AFileNumber);
				Error.AssertFail(FFiles[AFileNumber - 1] == null, "Attempting to load a file into a non-empty slot");

				// Load the FileGroup file header
				FileGroupFileHeader LHeader;
				byte* LPagePointer = stackalloc byte[DataFile.CMinPageSize];
				LDataFile.ReadPage((IntPtr)LPagePointer, 0);
				LHeader = *((FileGroupFileHeader*)(LPagePointer + DataFile.CFileHeaderSize));
				
				// Validate the file's File Number
				if (LHeader.FileNumber != AFileNumber)
					throw new StorageException(StorageException.Codes.InvalidFileNumber, LHeader.FileNumber, AFileName, AFileNumber);

				if (AFileNumber == 1)
					FID = LHeader.FileGroupID;
				else
				{
					// Verify the FileGroupID
					if (LHeader.FileGroupID != FID)
						throw new StorageException(StorageException.Codes.InvalidFileGroupID, AFileName);
				}

				FFiles[AFileNumber - 1] = LDataFile;
			}
		}

		/// <summary> The number of DataFile slots (always 1023). </summary>
		public int Slots { get { return CMaxFileGroup; } }

		private int FCount;
		/// <summary> The number of occupied data file slots. </summary>
		public int Count { get { return FCount; } }

		public DataFile SafeGet(short AFileNumber)
		{
			DataFile LResult = FFiles[AFileNumber - 1];
			if (LResult == null)
				throw new StorageException(StorageException.Codes.FileNumberNotFound, AFileNumber);
			return LResult;
		}

		public void UnloadDataFile(short AFileNumber)
		{
			DataFile LDataFile = FFiles[AFileNumber - 1];
			if (LDataFile != null)
			{
				FFiles[AFileNumber - 1] = null;
				LDataFile.Dispose();
			}
		}

		public void LoadFile(string AFileName, bool AReadOnly)
		{
			FastFile LFile = new FastFile(AFileName, AReadOnly);
			try
			{
				// Read enough of the page to the page size
				byte* LPartialPage = stackalloc byte[DataFile.CMinPageSize];
				LFile.Read((IntPtr)LPartialPage, CMinPageSize, 0);
				FileHeader LFileHeader = *((FileHeader*)LPartialPage);

				// Read the first full page
				byte* LPage = stackalloc byte[LFileHeader.PageSize];
				LFile.Read((IntPtr)LPage, FPageSize, 0);

				return new DataFile(LFile, LFileHeader);
			}
			catch
			{
				LFile.Dispose();
				throw;
			}
		}

		/// <summary> Loads an existing data file into the collection. </summary>
		public DataFile LoadDataFile(short AFileNumber, string AFileName, bool AReadOnly)
		{

			DataFile LDataFile = new DataFile(AFileName, AReadOnly);
			try
			{
			}
			catch
			{
				LDataFile.Dispose();
				throw;
			}
		}

		/// <summary> Creates a new data file and adds it to the FileGroup. </summary>
		public void CreateDataFile(short AFileNumber, string AFileName, FileGroupSettings AFileSettings)
		{
			Error.AssertFail(FFiles[AFileNumber - 1] == null, "Attempting to create a file into a non-empty slot");
			Error.AssertFail((AFileNumber > 0) && (AFileNumber <= CMaxFileGroup), "Invalid file number ({0}) provided to FileGroup.CreateDataFile().", AFileNumber);

			DataFile LDataFile = new DataFile(AFileName, AFileSettings);
			try
			{
				// Update the FileGroup file header
				FileGroupFileHeader LHeader = new FileGroupFileHeader();
				LHeader.FileGroupID = FID;
				LHeader.FileNumber = AFileNumber;
				byte* LPagePointer = stackalloc byte[DataFile.CMinPageSize];
				LDataFile.ReadPage((IntPtr)LPagePointer, 0);
				*((FileGroupFileHeader*)(LPagePointer + DataFile.CFileHeaderSize)) = LHeader;
				LDataFile.WritePage((IntPtr)LPagePointer, 0);

				FFiles[AFileNumber - 1] = LDataFile;
			}
			catch
			{
				LDataFile.Dispose();
				throw;
			}
		}

		private void EnsureFilePathExists(string AFileName)
		{
			string LPath = Path.GetDirectoryName(AFileName);
			if (!Directory.Exists(LPath))
				Directory.CreateDirectory(LPath);
		}

		private string FMasterFileName;

		public string GetFilePath()
		{
			return Path.GetDirectoryName(FMasterFileName);
		}

		#region Page Access

		/// <summary> Reads a page of data from a file into a buffer using a page number. </summary>
		public void ReadPage(IntPtr ABuffer, PageID APageID)
		{
			SafeGet(APageID.FileNumber).ReadPage(ABuffer, APageID.PageNumber);
		}

		/// <summary> Reads a list of pages from a file starting at the specified page ID. </summary>
		/// <remarks> The page numbers must be within the same file. </remarks>
		public void ReadPages(IntPtr[] ABuffers, int ACount, PageID APageID)
		{
			SafeGet(APageID.FileNumber).ReadPages(ABuffers, ACount, APageID.PageNumber);
		}

		/// <summary> Begins asyncronous reading of a page of data from a file to a buffer using a page number. </summary>
		/// <remarks> The AUserData argument is required so that the EndRead call knows what file is involved. </remarks>
		public IAsyncResult BeginReadPage(IntPtr ABuffer, PageID APageID, AsyncCallback ACallback, IPageAddressing AUserData)
		{
			if (AUserData == null)
				throw new ArgumentNullException("AUserData");
			return SafeGet(APageID.FileNumber).BeginReadPage(ABuffer, APageID.PageNumber, ACallback, AUserData);
		}

		/// <summary> Begins asyncronous reading of a list of pages from a file starting at the specified page ID. </summary>
		/// <remarks> The page numbers must be within the same file. The AUserData argument is required so that the 
		/// EndRead call knows what file is involved. </remarks>
		public IAsyncResult BeginReadPages(IntPtr[] ABuffers, int ACount, PageID APageID, AsyncCallback ACallback, IPageAddressing AUserData)
		{
			if (AUserData == null)
				throw new ArgumentNullException("AUserData");
			return SafeGet(APageID.FileNumber).BeginReadPages(ABuffers, ACount, APageID.PageNumber, ACallback, AUserData);
		}

		/// <summary> Ends asynchronous reading of a page or list of pages. </summary>
		/// <remarks> If the I/O is not complete, this call waits. This method will throw an exception if there was an error 
		/// during the I/O operation.  This method is generally called from within the async callback. </remarks>
		/// <returns> The number of whole pages read. </returns>
		public int EndReadPage(IAsyncResult AResult)
		{
			return SafeGet(((IPageAddressing)AResult.AsyncState).PageID.FileNumber).EndReadPage(AResult);
		}

		/// <summary> Write a page of data to a file from a buffer using a page number. </summary>
		public void WritePage(IntPtr ABuffer, PageID APageID)
		{
			SafeGet(APageID.FileNumber).WritePage(ABuffer, APageID.PageNumber);
		}

		/// <summary> Writes a list of pages to a file starting at the specified page ID. </summary>
		/// <remarks> The page numbers must be within the same file. </remarks>
		public void WritePages(IntPtr[] ABuffers, int ACount, PageID APageID)
		{
			SafeGet(APageID.FileNumber).WritePages(ABuffers, ACount, APageID.PageNumber);
		}

		/// <summary> Begins asyncronous writing of a page of data from a buffer to a file using a page number. </summary>
		/// <remarks> The AUserData argument is required so that the EndRead call knows what file is involved. </remarks>
		public IAsyncResult BeginWritePage(IntPtr ABuffer, PageID APageID, AsyncCallback ACallback, IPageAddressing AUserData)
		{
			if (AUserData == null)
				throw new ArgumentNullException("AUserData");
			return SafeGet(APageID.FileNumber).BeginWritePage(ABuffer, APageID.PageNumber, ACallback, AUserData);
		}

		/// <summary> Begins asyncronous writing of a list of pages to a file starting from the specified page ID.. </summary>
		/// <remarks> The page numbers must be within the same file. The AUserData argument is required so that the EndRead 
		/// call knows what file is involved.</remarks>
		public IAsyncResult BeginWritePages(IntPtr[] ABuffers, int ACount, PageID APageID, AsyncCallback ACallback, IPageAddressing AUserData)
		{
			if (AUserData == null)
				throw new ArgumentNullException("AUserData");
			return SafeGet(APageID.FileNumber).BeginWritePages(ABuffers, ACount, APageID.PageNumber, ACallback, AUserData);
		}

		/// <summary> Ends asynchronous writing of a page or list of pages. </summary>
		/// <remarks> If the I/O is not complete, this call waits. This method will throw an exception if there was an error 
		/// during the I/O operation.  This method is generally called from within the async callback. </remarks>
		/// <returns> The number of whole pages written. </returns>
		public int EndWritePage(IAsyncResult AResult)
		{
			return SafeGet(((IPageAddressing)AResult.AsyncState).PageID.FileNumber).EndWritePage(AResult);
		}

		/// <summary> Returns the size of the specified page. </summary>
		/// <remarks> This is a "cheap" operation because it can be performed by merely examining the file portion of the page ID. </remarks>
		public int GetPageSize(PageID APageID)
		{
			return SafeGet(APageID.FileNumber).PageSize;
		}

		#endregion
	}
*/
}
