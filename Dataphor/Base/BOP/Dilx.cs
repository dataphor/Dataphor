/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Alphora.Dataphor.BOP
{
	/// <summary> Represents a DILX Document. </summary>
	public class DilxDocument
	{
		// Do not localize
		public const string CDilxElementName = "dilx";
		public const string CDilxNamespace = "http://www.alphora.com/schemas/dilx";
		public const string CAncestorElementName = "ancestor";
		public const string CAncestorDocumentAttributeName = "document";
		public const string CDocumentElementName = "document";

		public DilxDocument()
		{
			FAncestors = new Ancestors();
		}

		private void InternalRead(XmlReader AReader)
		{
			FAncestors.Clear();
			AReader.MoveToContent();
			AReader.ReadStartElement(CDilxElementName, CDilxNamespace);
			while (AReader.MoveToContent() == XmlNodeType.Element)
			{
				if
				(
					String.Equals(AReader.Name, CAncestorElementName, StringComparison.OrdinalIgnoreCase) &&
					String.Equals(AReader.NamespaceURI, CDilxNamespace, StringComparison.OrdinalIgnoreCase)
				)
				{
					if (!AReader.MoveToAttribute(CAncestorDocumentAttributeName))
						throw new BOPException(BOPException.Codes.InvalidNode, AReader.Name);
					FAncestors.Add(AReader.Value);
					AReader.Skip();
				}
				else if
				(
					String.Equals(AReader.Name, CDocumentElementName, StringComparison.OrdinalIgnoreCase) &&
					String.Equals(AReader.NamespaceURI, CDilxNamespace, StringComparison.OrdinalIgnoreCase)
				)
				{
					AReader.ReadStartElement();
					// Support either CDATA (new method) or direct embedding of the interface (for backwards compatability)
					if (AReader.MoveToContent() == XmlNodeType.CDATA)
						FContent = AReader.ReadContentAsString();
					else
						FContent = AReader.ReadOuterXml();

					// Strip out the dilx namespace due to embedding of document withing outer document
					// TODO: Refactor this to actually parse the arguments
					string LDefaultNamespace = String.Format("xmlns=\"{0}\"", CDilxNamespace);
					int LContentIndex = FContent.IndexOf(LDefaultNamespace);
					if (LContentIndex >= 0)
						FContent = FContent.Remove(LContentIndex, LDefaultNamespace.Length);
					
					// Make sure there is nothing after the document node
					AReader.Skip();
					while (AReader.MoveToContent() == XmlNodeType.EndElement)
						AReader.Skip();
					if (!AReader.EOF)
						throw new BOPException(BOPException.Codes.DocumentElementLast);

					return;
				}
				else
					throw new BOPException(BOPException.Codes.InvalidNode, AReader.Name);
			}
			throw new BOPException(BOPException.Codes.DocumentElementRequired);
		}

		public void Read(string AData)
		{
			InternalRead
			(
				XmlReader.Create
				(
					new StringReader(AData), 
					new XmlReaderSettings() { IgnoreWhitespace = true }
				)
			);
		}

		/// <summary> Reads the DILX document from a stream containing the XML. </summary>
		/// <remarks> The document is cleared first. </remarks>
		public void Read(Stream AStream)
		{
			InternalRead
			(
				XmlReader.Create
				(
					AStream,
					new XmlReaderSettings() { IgnoreWhitespace = true }
				)
			);
		}

		private static XmlWriterSettings GetXmlWriterSettings()
		{
			return new XmlWriterSettings { Encoding = System.Text.Encoding.UTF8, Indent = true };
		}

		/// <summary> Writes the DILX document to a stream in an XML format. </summary>
		public void Write(Stream AStream)
		{
			InternalWrite(XmlWriter.Create(AStream, GetXmlWriterSettings()));
		}

		public string Write()
		{
			StringWriter LWriter = new StringWriter();
			InternalWrite(XmlWriter.Create(LWriter, GetXmlWriterSettings()));
			return LWriter.ToString();
		}

		private void InternalWrite(XmlWriter AWriter)
		{
			AWriter.WriteStartDocument(true);

			AWriter.WriteStartElement(CDilxElementName, CDilxNamespace);

			// Write ancestor(s)
			foreach (string LAncestor in FAncestors)
			{
				AWriter.WriteStartElement(CAncestorElementName);
				AWriter.WriteAttributeString(CAncestorDocumentAttributeName, LAncestor);
				AWriter.WriteEndElement();
			}

			// Write document element
			AWriter.WriteStartElement(CDocumentElementName);
			AWriter.WriteCData(FContent);
			AWriter.WriteFullEndElement();

			AWriter.WriteEndElement();
			AWriter.Flush(); // this must be here or nothing winds up on the stream.
		}

		/// <summary> Clears the DILX document.</summary>
		public void Clear()
		{
			FContent = String.Empty;
			FAncestors.Clear();
		}

		private String FContent = String.Empty;
		/// <summary> The embedded DIL document Content. </summary>
		public String Content
		{
			get { return FContent; }
			set { FContent = (value == null ? String.Empty : value); }
		}

		private Ancestors FAncestors;
		/// <summary> Collection of ancestor document expressions. </summary>
		public Ancestors Ancestors
		{
			get { return FAncestors; }
			set 
			{
				if (value == null)
					throw new ArgumentNullException("Ancestors");
				FAncestors = value;
			}
		}
	}

	/// <summary> DilxDocument Ancestor Collection </summary>
	/// <remarks> Maintains a list of ancestor document expressions. </remarks>
	public class Ancestors : List<String>
	{
	}
}