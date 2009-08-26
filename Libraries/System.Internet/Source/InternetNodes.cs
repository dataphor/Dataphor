/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Web;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Libraries.System.Internet
{
	// operator System.Internet.SendEmail(const ASmtpServer: System.String, const AFromEmailAddress: System.String, const AToEmailAddress: System.String, const ASubject: System.String, const AMessage: System.String);
    // operator System.Internet.SendEmail(const ASmtpServer: System.String, const AFromEmailAddress: System.String, const AToEmailAddress: System.String, const ASubject: System.String, const AMessage: System.String, const AIsBodyHtml : Boolean);
    // operator System.Internet.SendEmail(const ASmtpServer: System.String, const AFromEmailAddress: System.String, const AToEmailAddress: System.String, const ASubject: System.String, const AMessage: System.String, const AHtmlAlternateView : String);
    public class SendEmailNode : InstructionNode
	{
        public override object InternalExecute(Program AProgram, object[] AArguments)
        {       
            //MailMessage constructor does not support semi-colon seperated list of "To" addresses
            MailMessage LMailMessage = new MailMessage();       
            LMailMessage.From = new MailAddress((string)AArguments[1]);
            foreach (string LEmailAddress in ((string)AArguments[2]).Split(';'))
                LMailMessage.To.Add(new MailAddress(LEmailAddress.Trim()));
            LMailMessage.Subject = (string)AArguments[3];
            
            //if using AlternateView don't set Body properties (this and the order of the addition of the AlternateViews has an effect on what some clients display).
            if ((AArguments.Length == 6) && (!Operator.Operands[5].DataType.Is(AProgram.DataTypes.SystemBoolean)))
            {        
                LMailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString((string)AArguments[4], null, MediaTypeNames.Text.Plain));   
                LMailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString((string)AArguments[5], null, MediaTypeNames.Text.Html));               
            }          
            else
            {
                LMailMessage.Body = (string)AArguments[4];
                LMailMessage.IsBodyHtml = (AArguments.Length == 6) ? (bool)AArguments[5] : false;
            }
            new SmtpClient((string)AArguments[0]).Send(LMailMessage);
            return null;
        }
	}

	// operator HTMLAttributeEncode(const AValue : String) : String
	public class HTMLAttributeEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			return HttpUtility.HtmlAttributeEncode((string)AArguments[0]);
		}
	}

	// operator HTMLEncode(const AValue : String) : String
	public class HTMLEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			return HttpUtility.HtmlEncode((string)AArguments[0]);
		}
	}

	// operator HTMLDecode(const AValue : String) : String
	public class HTMLDecodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			return HttpUtility.HtmlDecode((string)AArguments[0]);
		}
	}

	// operator URLEncode(const AValue : String) : String
	public class URLEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			return HttpUtility.UrlEncode((string)AArguments[0]);
		}
	}

	// operator URLDecode(const AValue : String) : String
	public class URLDecodeNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			return HttpUtility.UrlDecode((string)AArguments[0]);
		}
	}

	// operator PostHTTP(const AURL : String, AFields : table { FieldName : String, Value : String }) : String
	public class PostHTTPNode : InstructionNode
	{
		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			string LResult;
			
			string LURL = (string)AArguments[0];
			Table LFields = (Table)AArguments[1];

			// Build the URL encoding for the body
			StringBuilder LBody = new StringBuilder();
			using (Row LRow = new Row(AProgram.ValueManager, LFields.DataType.RowType))
			{
				while (LFields.Next())
				{
					LFields.Select(LRow);
					
					if (LBody.Length > 0)
						LBody.Append("&");
					
					LBody.Append(HttpUtility.UrlEncode((string)LRow["FieldName"]));
					LBody.Append("=");
					LBody.Append(HttpUtility.UrlEncode((string)LRow["Value"]));
				}
			}
			
			// Prepare the request
			HttpWebRequest LRequest = (HttpWebRequest)WebRequest.Create(LURL);
			LRequest.Method = "POST";
			LRequest.ProtocolVersion = new Version(1, 1);
			LRequest.KeepAlive = false;
			LRequest.ContentType = "application/x-www-form-urlencoded";

			// Write the body
			using (StreamWriter LWriter = new StreamWriter(LRequest.GetRequestStream()))
				LWriter.Write(LBody.ToString());

			// Get and read the response
			HttpWebResponse LResponse = (HttpWebResponse)LRequest.GetResponse();
			using (Stream LResponseStream = LResponse.GetResponseStream())
			{
				StreamReader LReader = new StreamReader(LResponseStream);
				LResult = LReader.ReadToEnd();
				LReader.Close();
			}

			return LResult;
		}
	}

	// operator LoadXML(ADocument : String) : XMLDocumentID
	public class LoadXMLNode : InstructionNode
	{
		private static void WriteContent(IServerProcess AProcess, Guid AElementID, string AContent, int AChildSequence, byte AType)
		{
			DataParams LParams = new DataParams();
			LParams.Add(DataParam.Create(AProcess, "AElementID", AElementID));
			LParams.Add(DataParam.Create(AProcess, "ASequence", AChildSequence));
			LParams.Add(DataParam.Create(AProcess, "AContent", AContent));
			LParams.Add(DataParam.Create(AProcess, "AType", AType));
			AProcess.Execute
			(
				"insert table { row { AElementID Element_ID, ASequence Sequence, AContent Content, AType Type }, key { } } into .System.Internet.XMLContent",
				LParams
			);
		}


		private static Guid InsertElement(IServerProcess AProcess, Guid ADocumentID, XmlTextReader AReader, Guid AParentID, int ASequence)
		{
			Guid LElementID = Guid.NewGuid();
			DataParams LParams = new DataParams();

			// Insert the element
			LParams.Add(DataParam.Create(AProcess, "AElementID", LElementID));
			LParams.Add(DataParam.Create(AProcess, "ADocumentID", ADocumentID));
			LParams.Add(DataParam.Create(AProcess, "ANamespaceAlias", AReader.Prefix));
			LParams.Add(DataParam.Create(AProcess, "AName", AReader.LocalName));
			AProcess.Execute
			(
				"insert table { row { AElementID ID, ADocumentID Document_ID, ANamespaceAlias NamespaceAlias, "
					+ "AName Name }, key { } } into .System.Internet.XMLElement",
				LParams
			);

			// Attach to parent
			if (AParentID != Guid.Empty)
			{
				LParams.Clear();
				LParams.Add(DataParam.Create(AProcess, "AElementID", LElementID));
				LParams.Add(DataParam.Create(AProcess, "AParentElementID", AParentID));
				LParams.Add(DataParam.Create(AProcess, "ASequence", ASequence));
				AProcess.Execute
				(
					"insert table { row { AElementID Element_ID, AParentElementID Parent_Element_ID, ASequence Sequence }, key { } } into .System.Internet.XMLElementParent",
					LParams
				);
			}

			// Add attributes
			while (AReader.MoveToNextAttribute())
			{
				LParams.Clear();
				LParams.Add(DataParam.Create(AProcess, "AElementID", LElementID));
				LParams.Add(DataParam.Create(AProcess, "AValue", AReader.Value));
				if (String.Compare(AReader.Name, "xmlns", true) == 0)	// Default namespace
					AProcess.Execute
					(
						"insert table { row { AElementID Element_ID, AValue URI }, key { } } into .System.Internet.XMLDefaultNamespace",
						LParams
					);
				else if (String.Compare(AReader.Prefix, "xmlns", true) == 0)	// Namespace alias
				{
					LParams.Add(DataParam.Create(AProcess, "ANamespaceAlias", AReader.LocalName));
					AProcess.Execute
					(
						"insert table { row { AElementID Element_ID, ANamespaceAlias NamespaceAlias, AValue URI }, key { } } into .System.Internet.XMLNamespaceAlias",
						LParams
					);
				}
				else	// regular attribute
				{
					LParams.Add(DataParam.Create(AProcess, "ANamespaceAlias", AReader.Prefix));
					LParams.Add(DataParam.Create(AProcess, "AName", AReader.LocalName));
					AProcess.Execute
					(
						"insert table { row { AElementID Element_ID, ANamespaceAlias NamespaceAlias, AName Name, AValue Value }, key { } } into .System.Internet.XMLAttribute",
						LParams
					);
				}
			}

			AReader.MoveToElement();
			if (!AReader.IsEmptyElement)
			{
				int LChildSequence = 0;
				XmlNodeType LNodeType;

				// Add child elements
				do {
					AReader.Read();
					LNodeType = AReader.NodeType;
					switch (LNodeType)
					{
						case XmlNodeType.Text :	WriteContent(AProcess, LElementID, AReader.Value, LChildSequence++, 0); break;
						case XmlNodeType.CDATA : WriteContent(AProcess, LElementID, AReader.Value, LChildSequence++, 1); break;
						case XmlNodeType.Element : InsertElement(AProcess, ADocumentID, AReader, LElementID, LChildSequence++); break;
					}
				} while (LNodeType != XmlNodeType.EndElement);
			}

			return LElementID;
		}

		private static void InsertDocument(IServerProcess AProcess, Guid ADocumentID, Guid ARootElementID)
		{
			DataParams LParams = new DataParams();
			LParams.Add(DataParam.Create(AProcess, "ADocumentID", ADocumentID));
			LParams.Add(DataParam.Create(AProcess, "AElementID", ARootElementID));
			AProcess.Execute
			(
				"insert table { row { ADocumentID ID, AElementID Root_Element_ID }, key { } } into .System.Internet.XMLDocument",
				LParams
			);
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{	
			XmlTextReader LReader = new XmlTextReader(new StringReader((string)AArguments[0]));
			LReader.WhitespaceHandling = WhitespaceHandling.None;
			
			// Move to the root element
			LReader.MoveToContent();

			Guid LDocumentID = Guid.NewGuid();
			
			AProgram.ServerProcess.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				InsertDocument(AProgram.ServerProcess, LDocumentID, InsertElement(AProgram.ServerProcess, LDocumentID, LReader, Guid.Empty, 0));
				AProgram.ServerProcess.CommitTransaction();
			}
			catch
			{
				AProgram.ServerProcess.RollbackTransaction();
				throw;
			}
			
			return LDocumentID;
		}
	}

	// operator SaveXML(ADocumentID : XMLDocumentID) : String
	public class SaveXMLNode : InstructionNode
	{
		private void WriteElement(Program AProgram, XmlTextWriter AWriter, Guid AElementID)
		{
			// Write the element header
			DataParams LParams = new DataParams();
			LParams.Add(DataParam.Create(AProgram.ServerProcess, "AElementID", AElementID));
			using 
			(
				Row LElement = 
					(Row)((IServerProcess)AProgram.ServerProcess).Evaluate
					(
						"row from (XMLElement where ID = AElementID)",
						LParams
					)
			)
			{
				string LNamespaceAlias = (string)LElement["NamespaceAlias"];
				if (LNamespaceAlias != String.Empty)
					LNamespaceAlias = LNamespaceAlias + ":";

				AWriter.WriteStartElement(LNamespaceAlias + (string)LElement["Name"]);
			}

			// Write any default namespace changes
			Scalar LDefault =
				(Scalar)((IServerProcess)AProgram.ServerProcess).Evaluate
				(
					"URI from row from (XMLDefaultNamespace where Element_ID = AElementID)",
					LParams
				);
			if (LDefault != null)
				AWriter.WriteAttributeString("xmlns", LDefault.AsString);

			// Write namespace aliases
			IServerCursor LAliases = 
				(IServerCursor)((IServerProcess)AProgram.ServerProcess).OpenCursor
				(
					"XMLNamespaceAlias where Element_ID = AElementID",
					LParams
				);
			try
			{
				while (LAliases.Next())
				{
					using (Row LRow = LAliases.Select())
						AWriter.WriteAttributeString("xmlns:" + (string)LRow["NamespaceAlias"], (string)LRow["URI"]);
				}
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).CloseCursor(LAliases);
			}

			// Write the attributes
			IServerCursor LAttributes = 
				(IServerCursor)((IServerProcess)AProgram.ServerProcess).OpenCursor
				(
					"XMLAttribute where Element_ID = AElementID",
					LParams
				);
			try
			{
				while (LAttributes.Next())
				{
					using (Row LRow = LAttributes.Select())
					{
						string LAlias = (string)LRow["NamespaceAlias"];
						if (LAlias != String.Empty)
							LAlias = LAlias + ":";
						AWriter.WriteAttributeString(LAlias + (string)LRow["Name"], (string)LRow["Value"]);
					}
				}
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).CloseCursor(LAttributes);
			}

			// Write the child content and elements
			IServerCursor LChildren = 
				(IServerCursor)((IServerProcess)AProgram.ServerProcess).OpenCursor
				(
					@"
						(XMLContent where Element_ID = AElementID over { Element_ID, Sequence })
							union 
							(
								XMLElementParent where Parent_Element_ID = AElementID over { Parent_Element_ID, Sequence } 
									rename { Parent_Element_ID Element_ID }
							)
							left join (XMLContent rename { Element_ID Content_Element_ID, Sequence Content_Sequence }) by Element_ID = Content_Element_ID and Sequence = Content_Sequence
							left join (XMLElementParent rename { Element_ID Child_Element_ID, Sequence Child_Sequence }) by Element_ID = Parent_Element_ID and Sequence = Child_Sequence
							order by { Element_ID, Sequence }
					",
					LParams
				);
			try
			{
				while (LChildren.Next())
				{
					using (Row LRow = LChildren.Select())
					{
						if (LRow.HasValue("Content_Element_ID"))	// Content
						{
							if ((byte)LRow["Type"] == 0)
								AWriter.WriteString((string)LRow["Content"]);
							else
								AWriter.WriteCData((string)LRow["Content"]);
						}
						else	// Child element
						{
							WriteElement(AProgram, AWriter, (Guid)LRow["Child_Element_ID"]);
						}
					}
				}
			}
			finally
			{
				((IServerProcess)AProgram.ServerProcess).CloseCursor(LChildren);
			}

			// Write the end element
			AWriter.WriteEndElement();
		}

		public override object InternalExecute(Program AProgram, object[] AArguments)
		{			
			StringWriter LText = new StringWriter();
			XmlTextWriter LWriter = new XmlTextWriter(LText);
			LWriter.Formatting = Formatting.Indented;

			// Find the root element
			DataParams LParams = new DataParams();
			LParams.Add(DataParam.Create(AProgram.ServerProcess, "ADocumentID", (Guid)AArguments[0]));
			Guid LRootElementID =
				((Scalar)
					((IServerProcess)AProgram.ServerProcess).Evaluate
					(
						"Root_Element_ID from row from (XMLDocument where ID = ADocumentID)",
						LParams
					)
				).AsGuid;

			// Write the root element
			WriteElement(AProgram, LWriter, LRootElementID);

			LWriter.Flush();
			return LText.ToString();
		}
	}
}