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
        public override object InternalExecute(Program program, object[] arguments)
        {       
            //MailMessage constructor does not support semi-colon seperated list of "To" addresses
            MailMessage mailMessage = new MailMessage();       
            mailMessage.From = new MailAddress((string)arguments[1]);
            foreach (string emailAddress in ((string)arguments[2]).Split(';'))
                mailMessage.To.Add(new MailAddress(emailAddress.Trim()));
            mailMessage.Subject = (string)arguments[3];
            
            //if using AlternateView don't set Body properties (this and the order of the addition of the AlternateViews has an effect on what some clients display).
            if ((arguments.Length == 6) && (!Operator.Operands[5].DataType.Is(program.DataTypes.SystemBoolean)))
            {        
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString((string)arguments[4], null, MediaTypeNames.Text.Plain));   
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString((string)arguments[5], null, MediaTypeNames.Text.Html));               
            }          
            else
            {
                mailMessage.Body = (string)arguments[4];
                mailMessage.IsBodyHtml = (arguments.Length == 6) ? (bool)arguments[5] : false;
            }
            new SmtpClient((string)arguments[0]).Send(mailMessage);
            return null;
        }
	}

	// operator HTMLAttributeEncode(const AValue : String) : String
	public class HTMLAttributeEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			// I would have thought that the framework implementation of HtmlAttributeEncode would,
			// well, encode html attributes. However, it does not seem to replace carriage-returns
			// or line feeds, so I'm doing that here.
			string encodedString = HttpUtility.HtmlAttributeEncode((string)arguments[0]);
			StringBuilder result = new StringBuilder(encodedString.Length);
			for (int index = 0; index < encodedString.Length; index++)
				switch (encodedString[index])
				{
					case '\r': result.Append("&#xD;"); break;
					case '\n': result.Append("&#xA;"); break;
					default : result.Append(encodedString[index]); break;
				}
			return result.ToString();
		}
	}

	// operator HTMLEncode(const AValue : String) : String
	public class HTMLEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{			
			return HttpUtility.HtmlEncode((string)arguments[0]);
		}
	}

	// operator HTMLDecode(const AValue : String) : String
	public class HTMLDecodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{			
			return HttpUtility.HtmlDecode((string)arguments[0]);
		}
	}

	// operator URLEncode(const AValue : String) : String
	public class URLEncodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{			
			return HttpUtility.UrlEncode((string)arguments[0]);
		}
	}

	// operator URLDecode(const AValue : String) : String
	public class URLDecodeNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{			
			return HttpUtility.UrlDecode((string)arguments[0]);
		}
	}

	// operator HTTP(const AVerb : String, const AURL : String, const AHeaders : table { Header : String, Value : String }, const ABody : String) : String
	public class HTTPNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{
			string result;
			
			string verb = (string)arguments[0];
			string uRL = (string)arguments[1];
			TableValue headersTable = (TableValue)arguments[2];
			ITable headers = headersTable != null ? headersTable.OpenCursor() : null;
			string body = (string)arguments[3];

			// Prepare the request
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uRL);
			request.Method = verb;
			request.ProtocolVersion = new Version(1, 1);
			request.KeepAlive = false;

			if (headers != null)
			{
				using (Row row = new Row(program.ValueManager, headers.DataType.RowType))
				{
					while (headers.Next())
					{
						headers.Select(row);

						request.Headers[(HttpRequestHeader)Enum.Parse(typeof(HttpRequestHeader), (string)row["Header"])] = (string)row["Value"];
					}
				}
			}

			// Write the body
			if (!String.IsNullOrEmpty(body))
				using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
					writer.Write(body);

			// Get and read the response
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (Stream responseStream = response.GetResponseStream())
			{
				StreamReader reader = new StreamReader(responseStream);
				result = reader.ReadToEnd();
				reader.Close();
			}

			return result;
		}
	}

	// operator PostHTTP(const AURL : String, AFields : table { FieldName : String, Value : String }) : String
	public class PostHTTPNode : InstructionNode
	{
		public override object InternalExecute(Program program, object[] arguments)
		{			
			string result;
			
			string uRL = (string)arguments[0];
			TableValue fieldsTable = (TableValue)arguments[1];
			ITable fields = fieldsTable.OpenCursor();

			// Build the URL encoding for the body
			StringBuilder body = new StringBuilder();
			using (Row row = new Row(program.ValueManager, fields.DataType.RowType))
			{
				while (fields.Next())
				{
					fields.Select(row);
					
					if (body.Length > 0)
						body.Append("&");
					
					body.Append(HttpUtility.UrlEncode((string)row["FieldName"]));
					body.Append("=");
					body.Append(HttpUtility.UrlEncode((string)row["Value"]));
				}
			}
			
			// Prepare the request
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uRL);
			request.Method = "POST";
			request.ProtocolVersion = new Version(1, 1);
			request.KeepAlive = false;
			request.ContentType = "application/x-www-form-urlencoded";

			// Write the body
			using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
				writer.Write(body.ToString());

			// Get and read the response
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			using (Stream responseStream = response.GetResponseStream())
			{
				StreamReader reader = new StreamReader(responseStream);
				result = reader.ReadToEnd();
				reader.Close();
			}

			return result;
		}
	}

	// operator LoadXML(ADocument : String) : XMLDocumentID
	public class LoadXMLNode : InstructionNode
	{
		private static void WriteContent(IServerProcess process, Guid elementID, string content, int childSequence, byte type)
		{
			DataParams paramsValue = new DataParams();
			paramsValue.Add(DataParam.Create(process, "AElementID", elementID));
			paramsValue.Add(DataParam.Create(process, "ASequence", childSequence));
			paramsValue.Add(DataParam.Create(process, "AContent", content));
			paramsValue.Add(DataParam.Create(process, "AType", type));
			process.Execute
			(
				"insert table { row { AElementID Element_ID, ASequence Sequence, AContent Content, AType Type }, key { } } into .System.Internet.XMLContent",
				paramsValue
			);
		}


		private static Guid InsertElement(IServerProcess process, Guid documentID, XmlTextReader reader, Guid parentID, int sequence)
		{
			Guid elementID = Guid.NewGuid();
			DataParams paramsValue = new DataParams();

			// Insert the element
			paramsValue.Add(DataParam.Create(process, "AElementID", elementID));
			paramsValue.Add(DataParam.Create(process, "ADocumentID", documentID));
			paramsValue.Add(DataParam.Create(process, "ANamespaceAlias", reader.Prefix));
			paramsValue.Add(DataParam.Create(process, "AName", reader.LocalName));
			process.Execute
			(
				"insert table { row { AElementID ID, ADocumentID Document_ID, ANamespaceAlias NamespaceAlias, "
					+ "AName Name }, key { } } into .System.Internet.XMLElement",
				paramsValue
			);

			// Attach to parent
			if (parentID != Guid.Empty)
			{
				paramsValue.Clear();
				paramsValue.Add(DataParam.Create(process, "AElementID", elementID));
				paramsValue.Add(DataParam.Create(process, "AParentElementID", parentID));
				paramsValue.Add(DataParam.Create(process, "ASequence", sequence));
				process.Execute
				(
					"insert table { row { AElementID Element_ID, AParentElementID Parent_Element_ID, ASequence Sequence }, key { } } into .System.Internet.XMLElementParent",
					paramsValue
				);
			}

			// Add attributes
			while (reader.MoveToNextAttribute())
			{
				paramsValue.Clear();
				paramsValue.Add(DataParam.Create(process, "AElementID", elementID));
				paramsValue.Add(DataParam.Create(process, "AValue", reader.Value));
				if (String.Compare(reader.Name, "xmlns", true) == 0)	// Default namespace
					process.Execute
					(
						"insert table { row { AElementID Element_ID, AValue URI }, key { } } into .System.Internet.XMLDefaultNamespace",
						paramsValue
					);
				else if (String.Compare(reader.Prefix, "xmlns", true) == 0)	// Namespace alias
				{
					paramsValue.Add(DataParam.Create(process, "ANamespaceAlias", reader.LocalName));
					process.Execute
					(
						"insert table { row { AElementID Element_ID, ANamespaceAlias NamespaceAlias, AValue URI }, key { } } into .System.Internet.XMLNamespaceAlias",
						paramsValue
					);
				}
				else	// regular attribute
				{
					paramsValue.Add(DataParam.Create(process, "ANamespaceAlias", reader.Prefix));
					paramsValue.Add(DataParam.Create(process, "AName", reader.LocalName));
					process.Execute
					(
						"insert table { row { AElementID Element_ID, ANamespaceAlias NamespaceAlias, AName Name, AValue Value }, key { } } into .System.Internet.XMLAttribute",
						paramsValue
					);
				}
			}

			reader.MoveToElement();
			if (!reader.IsEmptyElement)
			{
				int childSequence = 0;
				XmlNodeType nodeType;

				// Add child elements
				do {
					reader.Read();
					nodeType = reader.NodeType;
					switch (nodeType)
					{
						case XmlNodeType.Text :	WriteContent(process, elementID, reader.Value, childSequence++, 0); break;
						case XmlNodeType.CDATA : WriteContent(process, elementID, reader.Value, childSequence++, 1); break;
						case XmlNodeType.Element : InsertElement(process, documentID, reader, elementID, childSequence++); break;
					}
				} while (nodeType != XmlNodeType.EndElement);
			}

			return elementID;
		}

		private static void InsertDocument(IServerProcess process, Guid documentID, Guid rootElementID)
		{
			DataParams paramsValue = new DataParams();
			paramsValue.Add(DataParam.Create(process, "ADocumentID", documentID));
			paramsValue.Add(DataParam.Create(process, "AElementID", rootElementID));
			process.Execute
			(
				"insert table { row { ADocumentID ID, AElementID Root_Element_ID }, key { } } into .System.Internet.XMLDocument",
				paramsValue
			);
		}

		public override object InternalExecute(Program program, object[] arguments)
		{	
			XmlTextReader reader = new XmlTextReader(new StringReader((string)arguments[0]));
			reader.WhitespaceHandling = WhitespaceHandling.None;
			
			// Move to the root element
			reader.MoveToContent();

			Guid documentID = Guid.NewGuid();
			
			program.ServerProcess.BeginTransaction(IsolationLevel.Isolated);
			try
			{
				InsertDocument(program.ServerProcess, documentID, InsertElement(program.ServerProcess, documentID, reader, Guid.Empty, 0));
				program.ServerProcess.CommitTransaction();
			}
			catch
			{
				program.ServerProcess.RollbackTransaction();
				throw;
			}
			
			return documentID;
		}
	}

	// operator SaveXML(ADocumentID : XMLDocumentID) : String
	public class SaveXMLNode : InstructionNode
	{
		private void WriteElement(Program program, XmlTextWriter writer, Guid elementID)
		{
			// Write the element header
			DataParams paramsValue = new DataParams();
			paramsValue.Add(DataParam.Create(program.ServerProcess, "AElementID", elementID));
			using 
			(
				IRow element = 
					(IRow)((IServerProcess)program.ServerProcess).Evaluate
					(
						"row from (XMLElement where ID = AElementID)",
						paramsValue
					)
			)
			{
				string namespaceAlias = (string)element["NamespaceAlias"];
				if (namespaceAlias != String.Empty)
					namespaceAlias = namespaceAlias + ":";

				writer.WriteStartElement(namespaceAlias + (string)element["Name"]);
			}

			// Write any default namespace changes
			IScalar defaultValue =
				(IScalar)((IServerProcess)program.ServerProcess).Evaluate
				(
					"URI from row from (XMLDefaultNamespace where Element_ID = AElementID)",
					paramsValue
				);
			if (defaultValue != null)
				writer.WriteAttributeString("xmlns", defaultValue.AsString);

			// Write namespace aliases
			IServerCursor aliases = 
				(IServerCursor)((IServerProcess)program.ServerProcess).OpenCursor
				(
					"XMLNamespaceAlias where Element_ID = AElementID",
					paramsValue
				);
			try
			{
				while (aliases.Next())
				{
					using (IRow row = aliases.Select())
						writer.WriteAttributeString("xmlns:" + (string)row["NamespaceAlias"], (string)row["URI"]);
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).CloseCursor(aliases);
			}

			// Write the attributes
			IServerCursor attributes = 
				(IServerCursor)((IServerProcess)program.ServerProcess).OpenCursor
				(
					"XMLAttribute where Element_ID = AElementID",
					paramsValue
				);
			try
			{
				while (attributes.Next())
				{
					using (IRow row = attributes.Select())
					{
						string alias = (string)row["NamespaceAlias"];
						if (alias != String.Empty)
							alias = alias + ":";
						writer.WriteAttributeString(alias + (string)row["Name"], (string)row["Value"]);
					}
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).CloseCursor(attributes);
			}

			// Write the child content and elements
			IServerCursor children = 
				(IServerCursor)((IServerProcess)program.ServerProcess).OpenCursor
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
					paramsValue
				);
			try
			{
				while (children.Next())
				{
					using (IRow row = children.Select())
					{
						if (row.HasValue("Content_Element_ID"))	// Content
						{
							if ((byte)row["Type"] == 0)
								writer.WriteString((string)row["Content"]);
							else
								writer.WriteCData((string)row["Content"]);
						}
						else	// Child element
						{
							WriteElement(program, writer, (Guid)row["Child_Element_ID"]);
						}
					}
				}
			}
			finally
			{
				((IServerProcess)program.ServerProcess).CloseCursor(children);
			}

			// Write the end element
			writer.WriteEndElement();
		}

		public override object InternalExecute(Program program, object[] arguments)
		{			
			StringWriter text = new StringWriter();
			XmlTextWriter writer = new XmlTextWriter(text);
			writer.Formatting = Formatting.Indented;

			// Find the root element
			DataParams paramsValue = new DataParams();
			paramsValue.Add(DataParam.Create(program.ServerProcess, "ADocumentID", (Guid)arguments[0]));
			Guid rootElementID =
				((IScalar)
					((IServerProcess)program.ServerProcess).Evaluate
					(
						"Root_Element_ID from row from (XMLDocument where ID = ADocumentID)",
						paramsValue
					)
				).AsGuid;

			// Write the root element
			WriteElement(program, writer, rootElementID);

			writer.Flush();
			return text.ToString();
		}
	}
}