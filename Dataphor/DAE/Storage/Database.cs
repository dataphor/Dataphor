/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace Alphora.Dataphor.DAE.Storage
{
/*
	public class Database
	{
		public Database(string AMasterFileName, bool AReadOnly)
		{
			DataFile LDataFile;

			FastFile LFile = new FastFile(AFileName, AReadOnly);
			try
			{
				// Read enough of a page to determine the page size
				byte* LPartialPage = stackalloc byte[DataFile.CMinPageSize];
				LFile.Read((IntPtr)LPartialPage, CMinPageSize, 0);
				int LPageSize = DataFile.GetPageSize((IntPtr)LPartialPage);

				// Allocate memory for two full pages
				IntPtr LFirstPages = MemoryUtility.Allocate(LPageSize * 2);
				try
				{
					// Read the first two full pages
					LFile.Read(LFirstPages, LPageSize * 2, 0);


					LDataFile = new DataFile(LFile, (IntPtr)LPage);
				}
				finally
				{
					MemoryUtility.Deallocate(LFirstPages);
				}
			}
			catch
			{
				LFile.Dispose();
				throw;
			}
			try
			{
			}
			catch
			{
				LDataFile.Dispose();
				throw;
			}

			// Verify the header page's CRC32
			ACRC32Succeeded = IsCRCValid((IntPtr)LPage);
			Error.AssertWarn(ACRC32Succeeded, "CRC32 Failed for file '{0}'", AFileName);

		}

		public Database(string AMasterFileName, DatabaseSettings ASettings)
		{
		}

		private FileGroup FFiles;
	}
*/
}
