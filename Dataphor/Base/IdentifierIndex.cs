/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Xml;

namespace Alphora.Dataphor
{
	/// <summary> Maintains an index of unique identifiers keyed by string values. </summary>
	/// <remarks> Builds unique strings which act as keys for the underlying data. </remarks>
	public class IdentifierIndex : IEnumerable
	{
		public IdentifierIndex()
		{
			FTable = new Hashtable(StringComparer.InvariantCulture);
		}

		private Hashtable FTable;

		/// <summary> Loads index entries from XML data contained within a stream. </summary>
		public void Load(Stream AStream)
		{
			StreamUtility.LoadDictionary(AStream, FTable, typeof(String), typeof(String));
		}

		public void Save(Stream AStream)
		{
			StreamUtility.SaveDictionary(AStream, FTable);
		}

		/// <summary> Looks up an identifier by the key. </summary>
		/// <remarks> Returns null if the key was not found. </remarks>
		public string this[string AKey]
		{
			get { return (string)FTable[AKey]; }
		}

		/// <summary> Adds an entry to the index, generating a unique Identifier. </summary>
		/// <param name="AKey"> The string used as a key to retrieve the unique identifier. </param>
		/// <returns> A unique identifier. </returns>
		public string Add(string AKey)
		{
			string LNewName = Guid.NewGuid().ToString();
			FTable.Add(AKey, LNewName);
			return LNewName;
		}

		/// <summary> Removes an entry from the index by it's string key. </summary>
		public void Remove(string AKey)
		{
			FTable.Remove(AKey);
		}

		/// <summary> Clears the index of all entries. </summary>
		public void Clear()
		{
			FTable.Clear();
		}

		/// <summary> Gets an enumerator to iterate through the identifiers. </summary>
		/// <returns> An <see cref="IEnumerator"/> instance containing <see cref="DictionaryEntry"/> objects. </returns>
		public IEnumerator GetEnumerator()
		{
			return FTable.GetEnumerator();
		}
	}
}