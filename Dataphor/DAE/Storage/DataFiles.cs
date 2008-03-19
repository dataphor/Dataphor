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
	DataFiles layout ->
		-Page IDs identify a unique page within the entire data file set
		-Page IDs are not necessarily contiguous because files in general will be smaller than 2^54 pages
		-File Numbers are not necessarily contiguous either
		-File Numbers range from 1 to 1023 (0 is reserved)
		-Page ID 0 is a special value representing an unknown or unapplicable page ID
		-File 1 is the Master File, this file constains the directory of files

		  File #       Page #
		|-10bits-|-----54bits---|
		1111111111000000...000000

*/

	public class DataFiles : Disposable
	{
		public const string CFileExtension = "ddb";
		public const string CLogFileExtension = "dlf";

		public DataFiles(string AMasterFileName, bool AReadOnly)
		{
			FMasterFileName = Path.GetFullPath(AMasterFileName);
			LoadDataFile(1, FMasterFileName, AReadOnly);
		}

		public DataFiles(string AMasterFileName, bool ACheckCRC32, int APageSize)
		{
			FID = Guid.NewGuid();
			FMasterFileName = Path.GetFullPath(AMasterFileName);
			EnsureFilePathExists(FMasterFileName);
			CreateDataFile(1, FMasterFileName, ACheckCRC32, APageSize);
		}

		protected override void Dispose(bool ADisposing)
		{
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


		private ReaderWriterLock FLatch = new ReaderWriterLock();
		// TODO: Latching for the DataFiles structure

		private Guid FID;
		public Guid ID { get { return FID; } }

		private DataFile[] FFiles = new DataFile[1023];
		/// <summary> Gets and sets the DataFile entries in the file buckets. </summary>
		/// <remarks> This will be null if there is no file in the bucket at the specified index.  This is a 1 based list (file 0 is reserved). </remarks>
		public DataFile this[short AFileNumber]
		{
			get { return FFiles[AFileNumber - 1]; }
		}

		/// <summary> The number of DataFile slots (always 1023). </summary>
		public int Count { get { return 1023; } }

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
		
		public void LoadDataFile(short AFileNumber, string AFileName, bool AReadOnly)
		{
			Error.AssertFail(FFiles[AFileNumber - 1] == null, "Attempting to load a file into a non-empty slot");

			DataFile LDataFile = new DataFile(AFileName, AReadOnly);

			// Validate the file's File Number
			if (LDataFile.FileHeader.FileNumber != AFileNumber)
				throw new StorageException(StorageException.Codes.InvalidFileNumber, LDataFile.FileHeader.FileNumber, AFileName, AFileNumber);

			if (AFileNumber == 1)
				FID = LDataFile.FileHeader.DataFilesID;
			else
			{
				// Verify the DataFilesID
				if (LDataFile.FileHeader.DataFilesID != FID)
					throw new StorageException(StorageException.Codes.InvalidDataFilesID, AFileName);
			}

			FFiles[AFileNumber - 1] = LDataFile;
		}

		public void CreateDataFile(short AFileNumber, string AFileName, bool ACheckCRC32, int APageSize)
		{
			Error.AssertFail(FFiles[AFileNumber - 1] == null, "Attempting to create a file into a non-empty slot");

			DataFileSettings LSettings;
			LSettings.DataFilesID = FID;
			LSettings.FileNumber = AFileNumber;
			LSettings.CheckCRC32 = ACheckCRC32;
			LSettings.PageSize = APageSize;

			DataFile LDataFile = new DataFile(AFileName, LSettings);

			FFiles[AFileNumber - 1] = LDataFile;
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

		/// <summary> Write a page of data to a file from a buffer using a page number. </summary>
		public void WritePage(IntPtr ABuffer, PageID APageID)
		{
			SafeGet(APageID.FileNumber).WritePage(ABuffer, APageID.PageNumber);
		}

		/// <summary> Begins asyncronous reading of a page of data from a file to a buffer using a page number. </summary>
		/// <remarks> 
		///		The requested page number is provided in the AsyncState member information of the returned IAsyncResult. 
		///	</remarks>
		public IAsyncResult BeginReadPage(IntPtr ABuffer, PageID APageID, AsyncCallback ACallback)
		{
			return SafeGet(APageID.FileNumber).BeginReadPage(ABuffer, APageID.PageNumber, ACallback);
		}

		/// <summary> Begins asyncronous writing of a page of data from a buffer to a file using a page number. </summary>
		/// <remarks> 
		///		The requested page number is provided in the AsyncState member information of the returned IAsyncResult. 
		///	</remarks>
		public IAsyncResult BeginWritePage(IntPtr ABuffer, PageID APageID, AsyncCallback ACallback)
		{
			return SafeGet(APageID.FileNumber).BeginWritePage(ABuffer, APageID.PageNumber, ACallback);
		}

		/// <summary> Returns the size of the specified page. </summary>
		/// <remarks> This is a "cheap" operation because it can be performed by merely examining the file portion of the page ID. </remarks>
		public int GetPageSize(PageID APageID)
		{
			return SafeGet(APageID.FileNumber).PageSize;
		}

		#endregion
	}
}
