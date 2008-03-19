/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Net;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Data cache. </summary>
	/// <remarks>
	///		<para>DocumentCache implements a fixed maximum sized, persisted data cache.  Data items are added using a 
	///		"name" that uniquely identifies them.  This name can be any string (doesn't have to be a valid file 
	///		name).  The fixed size of the cache is maintained in a Most Recently Used (MRU) manner.  In other words,
	///		if a new item is added to the cache (this is full), then the lest recently accessed item is removed. </para>
	///		<para>To use a DocumentCache, first look for the item in the cache by calling GetCRC32 for the item's name.  
	///		If the return is 0 the item is not in the cache (a cache miss), otherwise the item is in the cache (a cache 
	///		hit).  If a miss, load the item from outside the cache then call Freshen() and fill the resulting stream with
	///		the loaded data.  If a hit, call Reference() to retrieve the data (and to inform the cache that the item has 
	///		been recently used). </para>
	/// </remarks>
	public class DocumentCache : IDisposable
	{
		public const string CIndexFileName = "Index.dfi";
		public const string CCacheFileExtension = ".dfc";
		public const string CCRC32FileName = "CRC32s.dfcrc";
		public const string CLockFileName = "LockFile";

		/// <summary> Instantiates a new DocumentCache. </summary>
		/// <param name="ACachePath"> Specifies the path to use to store cached items.  </param>
		public DocumentCache(string ACachePath, int ASize)
		{
			// Prepare the folder
			FCachePath = ACachePath;

			// Prepare the structures
			FIdentifiers = new IdentifierIndex();
			FCRC32s = new Hashtable(ASize);
			FCache = new FixedSizeCache(ASize);

			LockDirectory();
			try
			{
				try
				{
					DateTime LIndexFileTime = DateTime.MinValue;	// Init as invalid
					string LIndexFileName = Path.Combine(FCachePath, CIndexFileName);
					string[] LCacheFiles = Directory.GetFiles(FCachePath, "*" + CCacheFileExtension);

					// Load the identifier index
					if (File.Exists(LIndexFileName))
					{
						LIndexFileTime = File.GetLastWriteTimeUtc(LIndexFileName);
						using (Stream LStream = new FileStream(LIndexFileName, FileMode.Open, FileAccess.Read))
						{
							FIdentifiers.Load(LStream);
						}

						// Initialize the CRC32s
						string LCRC32TableFileName = Path.Combine(FCachePath, CCRC32FileName);
						if (File.Exists(LCRC32TableFileName))
						{
							if (File.GetLastWriteTimeUtc(LCRC32TableFileName) == LIndexFileTime)
								using (FileStream LStream = new FileStream(LCRC32TableFileName, FileMode.Open, FileAccess.Read))
								{
									StreamUtility.LoadDictionary(LStream, FCRC32s, typeof(String), typeof(UInt32));
								}
							else
								LIndexFileTime = DateTime.MinValue;	// CRC32 and Index file times doen't match.  Invalid index.
						}
						else
							LIndexFileTime = DateTime.MinValue;	// No CRC32 table.  Invalid index.

						// Verify that ther are no cache files newer than the index
						foreach (string LFileName in LCacheFiles)
							if (File.GetLastWriteTimeUtc(LFileName) > LIndexFileTime)
							{
								LIndexFileTime = DateTime.MinValue;
								break;
							}
					}

					if (LIndexFileTime == DateTime.MinValue)	// If cache invalid, delete the files and clear cache
					{
						// Delete any existing cache files in the cache directory and clear the Identifiers
						foreach (string LFileName in Directory.GetFiles(FCachePath, "*." + CCacheFileExtension))
						{
							if (File.GetLastWriteTimeUtc(LFileName) > LIndexFileTime)
								File.Delete(LFileName);
						}
						FIdentifiers.Clear();
						FCRC32s.Clear();
					}
					// Don't bother initializing the fixed sized cache.  We'll let it be populated as requests come in.  As a result, if the cache size has been reduced, it 
					//  will not be effective until the new cache size limit has been reached.
				}
				catch
				{
					//Prevent future problems by clearing the directory
					foreach (string LFileName in Directory.GetFiles(FCachePath, "*.*"))
					{
						if (!String.Equals(Path.GetFileName(LFileName), CLockFileName, StringComparison.OrdinalIgnoreCase))
							File.Delete(LFileName);
					}
					throw;
				}
			}
			catch
			{
				UnlockDirectory();
				throw;
			}
		}

		public void Dispose()
		{
			try
			{
				string LIndexFileName = Path.Combine(FCachePath, CIndexFileName);
				using (FileStream LStream = new FileStream(LIndexFileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					FIdentifiers.Save(LStream);
				}

				string LCRC32FileName = Path.Combine(FCachePath, CCRC32FileName);
				using (FileStream LStream = new FileStream(LCRC32FileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					StreamUtility.SaveDictionary(LStream, FCRC32s);
				}

				// Synchronize the index and CRC3 file times so that we know they were saved properly
				File.SetLastWriteTimeUtc(LIndexFileName, File.GetLastWriteTimeUtc(LCRC32FileName));
			}
			finally
			{
				UnlockDirectory();
			}
		}

		private string FCachePath;
		public string CachePath { get { return FCachePath; } }

		private FileStream FLockFile;
		private bool FUsingPrivatePath = false;

		private void LockDirectory()
		{
			if (FLockFile == null)
			{
				if (!Directory.Exists(FCachePath))
					Directory.CreateDirectory(FCachePath);
				try
				{
					FLockFile = new FileStream(Path.Combine(FCachePath, CLockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				}
				catch (IOException)
				{
					if (FUsingPrivatePath)	// Only go one level deep... if we can't get a guid based private folder throw
						throw;
					else
					{
						FUsingPrivatePath = true;
						FCachePath = Path.Combine(FCachePath, Guid.NewGuid().ToString());
						LockDirectory();
					}
				}
			}
		}

		private void UnlockDirectory()
		{
			if (FLockFile != null)
			{
				FLockFile.Close();
				FLockFile = null;

				if (FUsingPrivatePath)
					Directory.Delete(FCachePath, true);
			}
		}

		private string BuildFileName(string AIdentifier)
		{
			return Path.Combine(FCachePath, AIdentifier + CCacheFileExtension);
		}

		private IdentifierIndex FIdentifiers;
		private FixedSizeCache FCache;

		private void ReferenceName(string AName)
		{
			Remove((string)FCache.Reference(AName, AName));
		}

		private void Remove(string AName)
		{
			if (AName != null)
			{
				string LIdentifier = FIdentifiers[AName];
				if (LIdentifier != null)
				{
					FIdentifiers.Remove(AName);
					FCRC32s.Remove(LIdentifier);
					File.Delete(BuildFileName(LIdentifier));
				}
			}
		}

		private Hashtable FCRC32s;

		private uint GetIdentifierCRC32(string AIdentifier)
		{
			using (Stream LStream = File.OpenRead(BuildFileName(AIdentifier)))
			{
				return CRC32Utility.GetCRC32(LStream);
			}
		}

		public uint GetCRC32(string AName)
		{
			string LIdentifier = FIdentifiers[AName];
			if (LIdentifier != null)
				return (uint)FCRC32s[LIdentifier];
			else
				return 0;
		}

		public Stream Reference(string AName)
		{
			string LIdentifier = FIdentifiers[AName];
			if (LIdentifier != null)
			{
				ReferenceName(AName);
				return new FileStream(BuildFileName(LIdentifier), FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			else
				return null;
		}

		public Stream Freshen(string AName, uint ACRC32)
		{
			string LIdentifier = FIdentifiers[AName];
            if (LIdentifier == null)
                LIdentifier = FIdentifiers.Add(AName);
            FCRC32s[LIdentifier] = ACRC32;
			ReferenceName(AName);

			return new FileStream(BuildFileName(LIdentifier), FileMode.Create, FileAccess.Write, FileShare.None);
		}
	}
}
