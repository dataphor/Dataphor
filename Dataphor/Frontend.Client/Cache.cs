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
using System.Collections.Generic;

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
	public class DocumentCache : IDisposable, IDocumentCache
	{
		public const string IndexFileName = "Index.dfi";
		public const string CacheFileExtension = ".dfc";
		public const string RC32FileName = "CRC32s.dfcrc";
		public const string LockFileName = "LockFile";

		/// <summary> Instantiates a new DocumentCache. </summary>
		/// <param name="cachePath"> Specifies the path to use to store cached items.  </param>
		public DocumentCache(string cachePath, int size)
		{
			// Prepare the folder
			_cachePath = cachePath;

			// Prepare the structures
			_identifiers = new IdentifierIndex();
			_cRC32s = new Dictionary<string, uint>(size);
			_cache = new FixedSizeCache<string, string>(size);

			LockDirectory();
			try
			{
				try
				{
					DateTime indexFileTime = DateTime.MinValue;	// Init as invalid
					string indexFileName = Path.Combine(_cachePath, IndexFileName);
					string[] cacheFiles = Directory.GetFiles(_cachePath, "*" + CacheFileExtension);

					// Load the identifier index
					if (File.Exists(indexFileName))
					{
						indexFileTime = File.GetLastWriteTimeUtc(indexFileName);
						using (Stream stream = new FileStream(indexFileName, FileMode.Open, FileAccess.Read))
						{
							_identifiers.Load(stream);
						}

						// Initialize the CRC32s
						string cRC32TableFileName = Path.Combine(_cachePath, RC32FileName);
						if (File.Exists(cRC32TableFileName))
						{
							if (File.GetLastWriteTimeUtc(cRC32TableFileName) == indexFileTime)
								using (FileStream stream = new FileStream(cRC32TableFileName, FileMode.Open, FileAccess.Read))
								{
									StreamUtility.LoadDictionary(stream, _cRC32s, typeof(String), typeof(UInt32));
								}
							else
								indexFileTime = DateTime.MinValue;	// CRC32 and Index file times doen't match.  Invalid index.
						}
						else
							indexFileTime = DateTime.MinValue;	// No CRC32 table.  Invalid index.

						// Verify that ther are no cache files newer than the index
						foreach (string fileName in cacheFiles)
							if (File.GetLastWriteTimeUtc(fileName) > indexFileTime)
							{
								indexFileTime = DateTime.MinValue;
								break;
							}
					}

					if (indexFileTime == DateTime.MinValue)	// If cache invalid, delete the files and clear cache
					{
						// Delete any existing cache files in the cache directory and clear the Identifiers
						foreach (string fileName in Directory.GetFiles(_cachePath, "*." + CacheFileExtension))
						{
							if (File.GetLastWriteTimeUtc(fileName) > indexFileTime)
								File.Delete(fileName);
						}
						_identifiers.Clear();
						_cRC32s.Clear();
					}
					// Don't bother initializing the fixed sized cache.  We'll let it be populated as requests come in.  As a result, if the cache size has been reduced, it 
					//  will not be effective until the new cache size limit has been reached.
				}
				catch
				{
					//Prevent future problems by clearing the directory
					EmptyCacheDirectory();
					throw;
				}
			}
			catch
			{
				UnlockDirectory(false);
				throw;
			}
		}

		private void EmptyCacheDirectory()
		{
			foreach (string fileName in Directory.GetFiles(_cachePath, "*.*"))
			{
				if (!String.Equals(Path.GetFileName(fileName), LockFileName, StringComparison.OrdinalIgnoreCase))
					File.Delete(fileName);
			}
		}

		public void Dispose()
		{
			var success = false;
			try
			{
				string indexFileName = Path.Combine(_cachePath, IndexFileName);
				using (FileStream stream = new FileStream(indexFileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					_identifiers.Save(stream);
				}

				string cRC32FileName = Path.Combine(_cachePath, RC32FileName);
				using (FileStream stream = new FileStream(cRC32FileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					StreamUtility.SaveDictionary(stream, _cRC32s);
				}

				// Synchronize the index and CRC3 file times so that we know they were saved properly
				File.SetLastWriteTimeUtc(indexFileName, File.GetLastWriteTimeUtc(cRC32FileName));
				
				success = true;
			}
			finally
			{
				UnlockDirectory(success);
			}
		}

		private string _cachePath;
		public string CachePath { get { return _cachePath; } }

		private FileStream _lockFile;
		private bool _usingPrivatePath = false;

		private void LockDirectory()
		{
			if (_lockFile == null)
			{
				if (!Directory.Exists(_cachePath))
					Directory.CreateDirectory(_cachePath);
				try
				{
					_lockFile = new FileStream(Path.Combine(_cachePath, LockFileName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
					var lockValue = _lockFile.Length > 0 ? _lockFile.ReadByte() : 1;
					_lockFile.Position = 0;
					_lockFile.WriteByte(1);
					_lockFile.Flush();
					
					// The cache may not have been shut-down properly... must clear cache to be safe
					if (lockValue != 0)
						EmptyCacheDirectory();
				}
				catch (IOException)
				{
					if (_usingPrivatePath)	// Only go one level deep... if we can't get a guid based private folder throw
						throw;
					else
					{
						_usingPrivatePath = true;
						_cachePath = Path.Combine(_cachePath, Guid.NewGuid().ToString());
						LockDirectory();
					}
				}
			}
		}

		private void UnlockDirectory(bool succeeded)
		{
			if (_lockFile != null)
			{
				_lockFile.Position = 0;
				_lockFile.WriteByte(succeeded ? (byte)0 : (byte)1);
				_lockFile.Close();
				_lockFile = null;

				if (_usingPrivatePath)
					Directory.Delete(_cachePath, true);
			}
		}

		private string BuildFileName(string identifier)
		{
			return Path.Combine(_cachePath, identifier + CacheFileExtension);
		}

		private IdentifierIndex _identifiers;
		private FixedSizeCache<string, string> _cache;

		private void ReferenceName(string name)
		{
			Remove(_cache.Reference(name, null));
		}

		private void Remove(string name)
		{
			if (name != null)
			{
				string identifier = _identifiers[name];
				if (identifier != null)
				{
					_identifiers.Remove(name);
					_cRC32s.Remove(identifier);
					File.Delete(BuildFileName(identifier));
				}
			}
		}

		private Dictionary<string, uint> _cRC32s;

		private uint GetIdentifierCRC32(string identifier)
		{
			using (Stream stream = File.OpenRead(BuildFileName(identifier)))
			{
				return CRC32Utility.GetCRC32(stream);
			}
		}

		public uint GetCRC32(string name)
		{
			string identifier = _identifiers[name];
			if (identifier != null)
				return _cRC32s[identifier];
			else
				return 0;
		}

		public Stream Reference(string name)
		{
			string identifier = _identifiers[name];
			if (identifier != null)
			{
				ReferenceName(name);
				return new FileStream(BuildFileName(identifier), FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			else
				return null;
		}

		public Stream Freshen(string name, uint cRC32)
		{
			string identifier = _identifiers[name];
            if (identifier == null)
                identifier = _identifiers.Add(name);
            _cRC32s[identifier] = cRC32;
			ReferenceName(name);

			return new FileStream(BuildFileName(identifier), FileMode.Create, FileAccess.Write, FileShare.None);
		}
	}
}
