/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
using System.IO;
using System.Text;

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

		private void InternalRead(XmlTextReader AReader)
		{
			FAncestors.Clear();
			AReader.WhitespaceHandling = WhitespaceHandling.None;
			AReader.MoveToContent();
			AReader.ReadStartElement(CDilxElementName, CDilxNamespace);
			while (AReader.MoveToContent() == XmlNodeType.Element)
			{
				if
				(
					(String.Compare(AReader.Name, CAncestorElementName, true) == 0) &&
					(String.Compare(AReader.NamespaceURI, CDilxNamespace, true) == 0)
				)
				{
					if (!AReader.MoveToAttribute(CAncestorDocumentAttributeName))
						throw new BOPException(BOPException.Codes.InvalidNode, AReader.Name);
					FAncestors.Add(AReader.Value);
					AReader.Skip();
				}
				else if
				(
					(String.Compare(AReader.Name, CDocumentElementName, true) == 0) &&
					(String.Compare(AReader.NamespaceURI, CDilxNamespace, true) == 0)
				)
				{
					FContent = AReader.ReadInnerXml();

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
			InternalRead(new XmlTextReader(new StringReader(AData)));
		}

		/// <summary> Reads the DILX document from a stream containing the XML. </summary>
		/// <remarks> The document is cleared first. </remarks>
		public void Read(Stream AStream)
		{
			InternalRead(new XmlTextReader(AStream));
		}

		/// <summary> Writes the DILX document to a stream in an XML format. </summary>
		public void Write(Stream AStream)
		{
			InternalWrite(new XmlTextWriter(AStream, System.Text.Encoding.UTF8));
		}

		public string Write()
		{
			StringWriter LWriter = new StringWriter();
			InternalWrite(new XmlTextWriter(LWriter));
			return LWriter.ToString();
		}

		private void InternalWrite(XmlTextWriter AWriter)
		{
			AWriter.Formatting = Formatting.Indented;

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
			AWriter.WriteRaw("\r\n");
			AWriter.WriteRaw(FContent);
			AWriter.WriteRaw("\r\n");
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
	public class Ancestors : StringCollection
	{
	}
}