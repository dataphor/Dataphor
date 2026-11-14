/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Maintains an index of unique identifiers keyed by string values. </summary>
	/// <remarks> Builds unique strings which act as keys for the underlying data. </remarks>
	public class IdentifierIndex
	{
		public IdentifierIndex()
		{
			_table = new Dictionary<string, string>(StringComparer.InvariantCulture);
		}

		private Dictionary<string, string> _table;

		/// <summary> Loads index entries from XML data contained within a stream. </summary>
		public void Load(Stream stream)
		{
			StreamUtility.LoadDictionary(stream, _table, typeof(String), typeof(String));
		}

		public void Save(Stream stream)
		{
			StreamUtility.SaveDictionary(stream, _table);
		}

		/// <summary> Looks up an identifier by the key. </summary>
		/// <remarks> Returns null if the key was not found. </remarks>
		public string this[string key]
		{
			get
			{ 
				string AValue;
				if (_table.TryGetValue(key, out AValue))
					return AValue;
				return null;
			}
		}

		/// <summary> Adds an entry to the index, generating a unique Identifier. </summary>
		/// <param name="key"> The string used as a key to retrieve the unique identifier. </param>
		/// <returns> A unique identifier. </returns>
		public string Add(string key)
		{
			string newName = Guid.NewGuid().ToString();
			_table.Add(key, newName);
			return newName;
		}

		/// <summary> Removes an entry from the index by it's string key. </summary>
		public void Remove(string key)
		{
			_table.Remove(key);
		}

		/// <summary> Clears the index of all entries. </summary>
		public void Clear()
		{
			_table.Clear();
		}
	}
}