/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Xml;
using System.IO;

namespace Alphora.Dataphor.Frontend.Server.Customization
{
	/// <summary> List of link maps. </summary>
	public class LinkTable
	{
		// Do not localize
		public const string CLinkTableElementName = "linktable";
		public const string CLinkTableNamespace = "http://www.alphora.com/schemas/linktable";
		public const string CEntryElementName = "entry";
		public const string CLinkAttributeName = "link";
		public const string CRemapAttributeName = "remap";

		public LinkTable()
		{
			FTable = new Hashtable(System.Collections.CaseInsensitiveHashCodeProvider.Default, CaseInsensitiveComparer.Default);
		}

		private Hashtable FTable;

		/// <summary> Adds a table entry to the table. </summary>
		/// <remarks> If the entry already exists, it is overwritten. </remarks>
		/// <param name="ALink"> The link source and key of the table entry. </param>
		/// <param name="ARemap"> The link the table entry targets. </param>
		public void Add(string ALink, string ARemap)
		{
			FTable.Remove(ALink);
			FTable.Add(ALink, ARemap);
		}

		/// <summary> Removes a table entry from the table. </summary>
		/// <remarks> If the entry doesn't exist, the call is ignored. </remarks>
		/// <param name="ALink"> The link source identifying an entry to be removed. </param>
		public void Remove(string ALink)
		{
			FTable.Remove(ALink);
		}

		/// <summary> Empties the table of all entries. </summary>
		public void Clear()
		{
			FTable.Clear();
		}

		/// <summary> Returns the target associated with the specified source mapping. </summary>
		/// <remarks> Returns null if the mapping was not found (not String.Empty). </remarks>
		public string this[string ASource]
		{
			get { return (string)FTable[ASource]; }
		}

		/// <summary> Reads the link table from a stream containing XML. </summary>
		/// <remarks> The table is not cleared before the load. </remarks>
		public void Read(Stream ASource)
		{
			XmlTextReader LReader = new XmlTextReader(ASource);
			LReader.WhitespaceHandling = WhitespaceHandling.None;
			LReader.MoveToContent();
			LReader.ReadStartElement(CLinkTableElementName, CLinkTableNamespace);

			string LLink;
			while (LReader.MoveToContent() == XmlNodeType.Element)
			{
				if 
				(
					(String.Compare(LReader.Name, CEntryElementName) != 0) ||
					(String.Compare(LReader.NamespaceURI, CLinkTableNamespace) != 0)
				)
					throw new ServerException(ServerException.Codes.InvalidNode, LReader.Name);
				if (!LReader.MoveToAttribute(CLinkAttributeName))
					throw new ServerException(ServerException.Codes.InvalidNode, LReader.Name);
				LLink = LReader.Value;
				if (!LReader.MoveToAttribute(CRemapAttributeName))
					throw new ServerException(ServerException.Codes.InvalidNode, LReader.Name);
				FTable.Add(LLink, LReader.Value);
				LReader.Skip();
			}
		}

		/// <summary> Writes the link table as XML to a stream. </summary>
		public void Write(Stream ATarget)
		{
			XmlTextWriter LWriter = new XmlTextWriter(ATarget, System.Text.Encoding.UTF8);
			LWriter.Formatting = Formatting.Indented;
			LWriter.WriteStartDocument(true);

			LWriter.WriteStartElement(CLinkTableElementName, CLinkTableNamespace);

			foreach (DictionaryEntry LEntry in FTable)
			{
				LWriter.WriteStartElement(CEntryElementName);
				LWriter.WriteAttributeString(CLinkAttributeName, (string)LEntry.Key);
				LWriter.WriteAttributeString(CRemapAttributeName, (string)LEntry.Value);
				LWriter.WriteEndElement();
			}

			LWriter.WriteEndElement();

			LWriter.Flush();
		}
	}
}
