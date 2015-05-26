/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Represents a DILX Document. </summary>
	public class DilxDocument
	{
		// Do not localize
		public const string DilxElementName = "dilx";
		public const string DilxNamespace = "http://www.alphora.com/schemas/dilx";
		public const string AncestorElementName = "ancestor";
		public const string AncestorDocumentAttributeName = "document";
		public const string DocumentElementName = "document";

		public const bool EmbedDocumentInCDATASection = false;

		public DilxDocument()
		{
			_ancestors = new Ancestors();
		}

		private void InternalRead(XmlReader reader)
		{
			_ancestors.Clear();
			reader.MoveToContent();
			reader.ReadStartElement(DilxElementName, DilxNamespace);
			while (reader.MoveToContent() == XmlNodeType.Element)
			{
				if
				(
					String.Equals(reader.Name, AncestorElementName, StringComparison.OrdinalIgnoreCase) &&
					String.Equals(reader.NamespaceURI, DilxNamespace, StringComparison.OrdinalIgnoreCase)
				)
				{
					if (!reader.MoveToAttribute(AncestorDocumentAttributeName))
						throw new BOPException(BOPException.Codes.InvalidNode, reader.Name);
					_ancestors.Add(reader.Value);
					reader.Skip();
				}
				else if
				(
					String.Equals(reader.Name, DocumentElementName, StringComparison.OrdinalIgnoreCase) &&
					String.Equals(reader.NamespaceURI, DilxNamespace, StringComparison.OrdinalIgnoreCase)
				)
				{
					reader.ReadStartElement();
					// NOTE: This mechanism was added to prevent non-space whitespace from being converted to spaces by attribute normalization when reading the embedded document
					// However, the use of a CDATA section makes the resulting DFDXs unreadable, so the writer will no longer write this way, but the reader must still be able to
					// read content this way.
					// Support either CDATA (new method) or direct embedding of the interface (for backwards compatability)
					if (reader.MoveToContent() == XmlNodeType.CDATA)
						_content = reader.ReadContentAsString();
					else
					{
						_content = reader.ReadOuterXml();
						// If we are reading as an Xml document, entities in attribute values will be converted to tab characters. 
						// These must be converted back to entities or they will be normalized away by subsequent xml readers.
						_content = ConvertSignificantWhitespaceToEntities(_content);
					}

					// Strip out the dilx namespace due to embedding of document withing outer document
					// TODO: Refactor this to actually parse the arguments
					string defaultNamespace = String.Format("xmlns=\"{0}\"", DilxNamespace);
					int contentIndex = _content.IndexOf(defaultNamespace);
					if (contentIndex >= 0)
						_content = _content.Remove(contentIndex, defaultNamespace.Length);

					// Make sure there is nothing after the document node
					reader.Skip();
					while (reader.MoveToContent() == XmlNodeType.EndElement)
						reader.Skip();
					if (!reader.EOF)
						throw new BOPException(BOPException.Codes.DocumentElementLast);

					return;
				}
				else
					throw new BOPException(BOPException.Codes.InvalidNode, reader.Name);
			}
			throw new BOPException(BOPException.Codes.DocumentElementRequired);
		}

		public void Read(string data)
		{
			InternalRead
			(
				XmlReader.Create
				(
					new StringReader(data), 
					new XmlReaderSettings() { IgnoreWhitespace = true }
				)
			);
		}

		/// <summary> Reads the DILX document from a stream containing the XML. </summary>
		/// <remarks> The document is cleared first. </remarks>
		public void Read(Stream stream)
		{
			InternalRead
			(
				XmlReader.Create
				(
					stream,
					new XmlReaderSettings() { IgnoreWhitespace = true }
				)
			);
		}

		private static string ConvertSignificantWhitespaceToEntities(string content)
		{
			// NOTE: This is a naive implementation, a more sophisticated implementation should only replace these within attributes.
			return content.Replace("\t", "&#x9;"); //.Replace("\n", "&#xa").Replace("\r", "&#xd");
		}

		private static XmlWriterSettings GetXmlWriterSettings()
		{
			return new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8, Indent = true };
		}

		/// <summary> Writes the DILX document to a stream in an XML format. </summary>
		public void Write(Stream stream)
		{
			InternalWrite(XmlWriter.Create(stream, GetXmlWriterSettings()));
		}

		public string Write()
		{
			// Use a stream because if we use a StringWriter it uses UTF16 encoding
			var memoryStream = new MemoryStream();
			var writer = new StreamWriter(memoryStream);
			InternalWrite(XmlWriter.Create(writer, GetXmlWriterSettings()));
			return Encoding.UTF8.GetString(memoryStream.ToArray());
		}

		private void InternalWrite(XmlWriter writer)
		{
			writer.WriteStartDocument(true);

			writer.WriteStartElement(DilxElementName, DilxNamespace);

			// Write ancestor(s)
			foreach (string ancestor in _ancestors)
			{
				writer.WriteStartElement(AncestorElementName);
				writer.WriteAttributeString(AncestorDocumentAttributeName, ancestor);
				writer.WriteEndElement();
			}

			// Write document element
			writer.WriteStartElement(DocumentElementName);
			if (EmbedDocumentInCDATASection)
			{
				writer.WriteCData(_content);
			}
			else
			{
				writer.WriteRaw(Environment.NewLine);
				writer.WriteRaw(_content);
				writer.WriteRaw(Environment.NewLine);
			}
			writer.WriteFullEndElement();

			writer.WriteEndElement();
			writer.Flush(); // this must be here or nothing winds up on the stream.
		}

		/// <summary> Clears the DILX document.</summary>
		public void Clear()
		{
			_content = String.Empty;
			_ancestors.Clear();
		}

		private String _content = String.Empty;
		/// <summary> The embedded DIL document Content. </summary>
		public String Content
		{
			get { return _content; }
			set { _content = (value == null ? String.Empty : value); }
		}

		private Ancestors _ancestors;
		/// <summary> Collection of ancestor document expressions. </summary>
		public Ancestors Ancestors
		{
			get { return _ancestors; }
			set 
			{
				if (value == null)
					throw new ArgumentNullException("Ancestors");
				_ancestors = value;
			}
		}
	}

	/// <summary> DilxDocument Ancestor Collection </summary>
	/// <remarks> Maintains a list of ancestor document expressions. </remarks>
	public class Ancestors : List<String>
	{
	}
}