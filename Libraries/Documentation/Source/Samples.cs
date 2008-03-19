/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

namespace DocSamples
{
	using System.IO;
	using System.Xml;
	using System.Xml.Serialization;
	using System.Xml.XPath;
	using System.Xml.Xsl;
	
	using Alphora.Dataphor.BOP;
	using Alphora.Dataphor.DAE;
	using Alphora.Dataphor.DAE.Server;
	using DAEClient = Alphora.Dataphor.DAE.Client;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Schema = Alphora.Dataphor.DAE.Schema;
	using Instructions = Alphora.Dataphor.DAE.Runtime.Instructions;
	

	/// <summary>
	/// Generates a file which includes the name, comment, and selected metadata from general Catalog objects.
	/// Param[0] filename for template, may be blank/null. Template file selects the desired metadata and provides output file format.
	///		if blank, then default template listing of name \n comment, blank line separating objects/comment lines pairings.
	/// Param[1] outputfile name, required.
	/// param[2] cursor of table{name}, requrired, selection and ordering responsibility of the expression
	/// 
	/// REGISTRATION SCRIPT:
	/// create operator DocCatalogObjec ( const ATemplate: string, const AOutput: string, const AObjects: cursor(table {name: string }) ) class "DocSamples.DocOperator,DocSamples"; 
	/// </summary>
	public class DocCatalogObject : Instructions.InstructionNode
	{
		public const string CFilenameExpected = @"Filename expected";
		public const string CExpressionExpected = @"Expression expected";
		protected string FTitle = "";
		
		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			// AArguments[0] is template name, may be blank
			// AArguments[1] is the title
			//if (AArguments[1].Value == null)
			//	throw new RuntimeException(CFilenameExpected); // output filename
			//if (AArguments[2].Value == null)
			//	throw new RuntimeException(CExpressionExpected); // expression to "export"
			FTitle = AArguments[1].Value.AsString;
			if (FTitle == "")
				FTitle = "Catalog Objects Documentation";

			if (AArguments.Length == 4)
				ProcessTable
				(
					AProcess, 
					AArguments[0].Value.AsString, // template
					AArguments[2].Value.AsString, // output file name
					AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[3].Value).ID).Table
				);
			else
				ProcessTable
				(
					AProcess, 
					"", 
					AArguments[2].Value.AsString, // Output file name
					AProcess.Plan.CursorManager.GetCursor(((CursorValue)AArguments[3].Value).ID).Table
				);

			return(null);
			// goal is to walk through the expression and extract metadata from the catalog for each item
		}

		protected virtual void ProcessTable(ServerProcess AProcess, string ATemplateName, string AOutputFileName, Table ATable)
		{
			// ServerProcess is the connection to the Server and context of what is going on in this layer
			Row LRow;
			StreamWriter LOutputFile = File.CreateText(AOutputFileName);
			try
			{
				switch(ATemplateName)
				{
					case "TopicXML":
						WriteTopicXMLHeader(AProcess, LOutputFile);
						break;
					case "Docbook":
						WriteDocbookHeader(AProcess, LOutputFile);
						break;
					default:
						break;
				}

				// the burden of ordering AND selecting the data on the cursor parameter
				while(ATable.Next())
				{
					LRow = ATable.Select();
					// select the row in the Catalog Objects table and write it out
					switch(ATemplateName)
					{
						case "TopicXML":
							WriteTopicXMLData(AProcess, AOutputFileName.Substring(0,AOutputFileName.LastIndexOf("\\")), LOutputFile, ATable, LRow);
							break;
						case "Docbook":
							WriteDocbookData(AProcess, AOutputFileName.Substring(0,AOutputFileName.LastIndexOf("\\")), LOutputFile, ATable, LRow);
							break;
						default:
							WriteData(AProcess, AOutputFileName.Substring(0,AOutputFileName.LastIndexOf("\\")), LOutputFile, ATable, LRow);
							break;
					}
				}
			
				switch(ATemplateName)
				{
					case "TopicXML":
						WriteTopicXMLFooter(AProcess, LOutputFile);
						break;
					case "Docbook":
						WriteDocbookFooter(AProcess, LOutputFile);
						break;
					default:
						break;
				}
			}
			finally
			{
				LOutputFile.Close();
			}
		}

		protected virtual void WriteData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			// non-formatted output is name \n comment \n\n

			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];

			// get the metadata for the row and print it

			AOutputFile.WriteLine(LObject.Name);
			//AOutputFile.WriteLine(LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment);
			try
			{
				AOutputFile.WriteLine(MetaData.GetTag(LObject.MetaData, "Catalog.Comment", String.Empty));
			}
			catch
			{
				// do nothing
			}
			AOutputFile.WriteLine("");
		}

		protected virtual void WriteTopicXMLHeader(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			AOutputFile.WriteLine("<AlphoraTopic>");
			AOutputFile.WriteLine("	<topics>");
		}

		protected virtual void WriteTopicXMLFooter(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("	</topics>");
			AOutputFile.WriteLine("</AlphoraTopics>");
		}

		protected virtual void WriteTopicXMLData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];

			AOutputFile.WriteLine("		<topic name=\"{0}\">", LObject.Name);
			AOutputFile.WriteLine("			<title>{0}</title>", LObject.Name);
			AOutputFile.WriteLine("			<summary>");
			//AOutputFile.WriteLine("				{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
			try
			{
				AOutputFile.WriteLine("				{0}", (MetaData.GetTag( LObject.MetaData, "Catalog.Comment", String.Empty)));
			}
			catch
			{
				// do nothing;
			}
			AOutputFile.WriteLine("			</summary>");
			AOutputFile.WriteLine("		</topic>");
		}

		protected virtual void WriteDocbookHeader(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("<sect1 id='{0}'>",FTitle.Replace(" ",""));
			AOutputFile.WriteLine("<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", FTitle);
		}

		protected virtual void WriteDocbookFooter(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("</sect1>");
		}

		protected virtual void WriteDocbookData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];
			AOutputFile.WriteLine("		<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", LObject.Name);
			AOutputFile.WriteLine("		<para>");
			//AOutputFile.WriteLine("			{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
			try
			{
				AOutputFile.WriteLine("			{0}", (MetaData.GetTag(LObject.MetaData, "Catalog.Comment", String.Empty)));
			}
			catch
			{
				// do nothing

			}
			AOutputFile.WriteLine("		</para>");
			AOutputFile.WriteLine("		<bridgehead renderas='sect4'>Overloads</bridgehead>");
			AOutputFile.WriteLine("		</programlisting>");
			AOutputFile.WriteLine("	</sect2>");
		}

	}

	/// <summary>
	/// Generates a file which includes the name, syntax, and selected metadata from Operator objects.
	/// Param[0] filename for template, may be blank/null. Template file selects the desired metadata and provides output file format.
	///		if blank, then default template listing of name \n comment, blank line separating objects/comment lines pairings.
	/// Param[1] outputfile name, required.
	/// param[2] expression of cursor {table {id}), required.
	/// NOTE: items selected from the catalog must be operators!
	/// REGISTRATION SCRIPT:
	/// before case sensitivity: create operator DocOperator ( const ATemplate: string, const AOutput: string, const AObjects: cursor(table {id: guid, name: string, type: string }) ) class "DocSamples.DocOperator,DocSamples"; 
	/// create operator DocOperator ( const ATemplate: String, const AOutput: String, const AObjects: cursor(table {ID: Guid, Name: String, Type: String }) ) class "DocSamples.DocOperator,DocSamples";
	/// USAGE SCRIPT:
	/// old: DocOperator("","c:\DocCatalog.dxc", cursor (objects over {ID, name, type} where Type="Operator" order by {name}));
	/// DocOperator("","c:\tmp\SystemLibrary.xml", cursor (Objects over {ID, Name, Type} where Type=ObjectType("Operator") order by {Name}));
	/// REMOVAL SCRIPT:
	/// drop operator DocOperator(const string, const string, const cursor(table{ID: guid, Name: string, Type: string}));
	/// </summary>
	public class DocOperator : DocCatalogObject
	{
		protected override void WriteData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];

			// todo: see if it is an operator, in which case may check IsBuiltin

			//if (LOperator.IsSystem && ! LOperator.IsBuiltin)
			//{
				D4TextEmitter Emitter = new D4TextEmitter();

				string LString = Emitter.Emit(LObject.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));

				AOutputFile.WriteLine(LString);
				AOutputFile.WriteLine("");
			//}
		}

		protected override void WriteTopicXMLData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];			
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];
			// todo: see if it is an operator, in which case may check IsBuiltin
			//if (LOperator.IsSystem && ! LOperator.IsBuiltin)
			//{
				string LName;
				string LFilename;

				if (LObject.Name.IndexOf("(") > -1)
					LName = LObject.Name.Substring(0,LObject.Name.IndexOf("("));
				else
					LName = LObject.Name;

				AOutputFile.WriteLine("		<entry name=\"{0}\" href=\"{1}\\{2}.dxt\" topic=\"//topic[@id='{3}']\" />", LName,AFilepath, LObject.ID.ToString().Replace("-",""), LObject.ID.ToString());

				LFilename = AFilepath + "\\" + LObject.ID.ToString().Replace("-","") + ".dxt";
				if (System.IO.File.Exists(LFilename))
					LFilename = AFilepath + "\\" + LObject.ID.ToString().Replace("-","") + ".new.dxt";

				StreamWriter LOutputFile = File.CreateText(LFilename);

				try
				{
					WriteTopicFile(AProcess, AFilepath, LOutputFile, ATable, ARow);
				}
				finally
				{
					LOutputFile.Close();
				}
			//}

		}
		

		protected override void WriteTopicXMLHeader(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			AOutputFile.WriteLine("<AlphoraDoc>");
			AOutputFile.WriteLine("	<entries>");
		}

		protected override void WriteTopicXMLFooter(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("	</entries>");
			AOutputFile.WriteLine("</AlphoraDoc>");
		}

		protected void WriteTopicFile(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			D4TextEmitter Emitter = new D4TextEmitter();
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];
			string LString = Emitter.Emit(LObject.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
			
			string LName;

			if (LObject.Name.IndexOf("(") > -1)
				LName = LObject.Name.Substring(0,LObject.Name.IndexOf("("));
			else
				LName = LObject.Name;

			AOutputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			AOutputFile.WriteLine("<AlphoraTopic>");
			AOutputFile.WriteLine("	<topics>");
			AOutputFile.WriteLine("		<topic name=\"{0}\" id=\"{1}\">",LName,LObject.ID.ToString());
			AOutputFile.WriteLine("			<title>{0}</title>",LName);
			AOutputFile.WriteLine("			<summary>");
			AOutputFile.WriteLine("				<!-- provide a brief description of the operator -->");
			AOutputFile.WriteLine("			</summary>");

			AOutputFile.WriteLine("			<part name=\"Syntax\">");
			AOutputFile.WriteLine("				<title>Syntax</title>");
			AOutputFile.WriteLine("				<para>");
			AOutputFile.WriteLine("					<code>");
			AOutputFile.WriteLine("{0}",LString.Trim());
			AOutputFile.WriteLine("					</code>");
			AOutputFile.WriteLine("				</para>");
			AOutputFile.WriteLine("			</part>");
			
			AOutputFile.WriteLine("			<part name=\"Parameters\">");
			AOutputFile.WriteLine("				<title>Parameters</title>");
			AOutputFile.WriteLine("				<!-- each parameter should be described using one of the following parameter markups -->");
			AOutputFile.WriteLine("				<!-- examples:");
			AOutputFile.WriteLine("				<param name=\"paramname\">param description</param>");
			AOutputFile.WriteLine("				<parameter name=\"paramname\" type=\"paratype,fullyqualified\" optional=\"0|1\" direction=\"in|out\" />");
			AOutputFile.WriteLine("				-->");
			AOutputFile.WriteLine("			</part>");

			AOutputFile.WriteLine("			<part name=\"Return\">");
			AOutputFile.WriteLine("				<title>Return</title>");
			AOutputFile.WriteLine("				<para>");
			AOutputFile.WriteLine("				<!-- describe the return value -->");
			AOutputFile.WriteLine("				</para>");
			AOutputFile.WriteLine("			</part>");

			AOutputFile.WriteLine("			<part name=\"Remarks\">");
			AOutputFile.WriteLine("				<title>Remarks</title>");
			AOutputFile.WriteLine("				<para>");
			AOutputFile.WriteLine("				<!-- describe other feature of the operator, exceptions that may occur, unexpected side effects, etc. -->");
			AOutputFile.WriteLine("				</para>");
			AOutputFile.WriteLine("			</part>");

			AOutputFile.WriteLine("			<part name=\"Example\">");
			AOutputFile.WriteLine("				<title>Example</title>");
			AOutputFile.WriteLine("				<para>");
			AOutputFile.WriteLine("				<!-- provide a code sample or more showing use of the operator -->");
			AOutputFile.WriteLine("				</para>");
			AOutputFile.WriteLine("			</part>");

			AOutputFile.WriteLine("		</topic>");
			AOutputFile.WriteLine("	</topics>");
			AOutputFile.WriteLine("</AlphoraTopic>");
		}

		protected override void WriteDocbookHeader(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("<sect1 id='SLR{0}'>", FTitle.Replace(" ",""));
			AOutputFile.WriteLine("<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", FTitle);
		}

		protected override void WriteDocbookFooter(ServerProcess AProcess, StreamWriter AOutputFile)
		{
			AOutputFile.WriteLine("</sect1>");
		}

		protected string GetJustName(Schema.Object AObject)
		{
			if (AObject is Schema.Operator)
				return(((Schema.Operator)AObject).OperatorName);
			else
				return(AObject.Name);
		}

		protected override void WriteDocbookData(ServerProcess AProcess, string AFilepath, StreamWriter AOutputFile, Table ATable, Row ARow)
		{
			//Schema.Object LObject = AProcess.Plan.Catalog[ARow["ID"].ToGuid()];
			Schema.Object LObject = AProcess.Plan.Catalog.Objects[ARow["Name"].AsString];
			string LFilename = "c:\\src\\Alphora\\Docs\\DocbookManuals\\OperatorDocs\\" + GetJustName(LObject) + ".xml";
			
			if (System.IO.File.Exists(LFilename))
			{
				if (LObject.IsSystem /*&& ! LObject.IsBuiltin*/)
				{
					AOutputFile.WriteLine("		<sect2 id=\"SLR{0}\">", GetJustName(LObject));
					AOutputFile.WriteLine("			<title><indexterm><primary>{0}</primary></indexterm><indexterm><primary>D4 Operators</primary><secondary>{0}</secondary></indexterm>{0}</title>", GetJustName(LObject));
					AOutputFile.WriteLine("			<para>");
					//AOutputFile.WriteLine("				{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
					try
					{
						AOutputFile.WriteLine("				{0}", (MetaData.GetTag(LObject.MetaData, "Catalog.Comment", String.Empty)));
					}
					catch
					{
						// do nothing
					}
					AOutputFile.WriteLine("			</para>");
					AOutputFile.WriteLine("			<bridgehead renderas='sect4'>Declarations</bridgehead>");
					AOutputFile.WriteLine("			<programlisting>");
					// here walk the table for all of the same name (overloads)
					WriteDocbookDeclarations(AProcess, AOutputFile, ATable, LObject);

					AOutputFile.WriteLine("			</programlisting>");

					// here merge the external docs
					WriteMergeExtDocs(AProcess, AOutputFile, LObject);

					AOutputFile.WriteLine("		</sect2>");
				}
			}
		}

		protected void WriteDocbookDeclarations(ServerProcess AProcess, StreamWriter AOutputFile, Table ATable, Schema.Object AObject)
		{
			// walk the table until the name of the object no longer matches, place the syntax in the outputfile for each overload
			Row LRow;
			D4TextEmitter Emitter = new D4TextEmitter();
			Schema.Object LObject = null;
			string LString = "";

			// write the definition of the current object
			LString = Emitter.Emit(AObject.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
			AOutputFile.WriteLine("{0}", LString.TrimEnd(null));

			// write the definitions for all the overloads
			while (ATable.Next())
			{
				LRow = ATable.Select();
				//LObject = AProcess.Plan.Catalog[LRow["ID"].ToGuid()];
				LObject = AProcess.Plan.Catalog.Objects[LRow["Name"].AsString];
				if (GetJustName(LObject) == GetJustName(AObject))
				{
					LString = Emitter.Emit(LObject.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
					AOutputFile.WriteLine("");
					AOutputFile.WriteLine("{0}", LString.TrimEnd(null));
				}
				else
				{
					ATable.Prior();
					break;
				}
			}
		}

		protected void WriteMergeExtDocs(ServerProcess AProcess, StreamWriter AOutputFile, Schema.Object AObject)
		{
			// load the external file, remove title, stuff innerxml into the outputfile
			XmlDocument LRemarks = null;
			XmlNode LTitle = null;
			XmlNode LSect2 = null;

			// magic directory used on the docs merge machine, (should be a parameter?...)
			string LFilename = "c:\\src\\Alphora\\Docs\\DocbookManuals\\OperatorDocs\\" + GetJustName(AObject) + ".xml";
			
			if (System.IO.File.Exists(LFilename))
			{
				System.IO.FileStream LStream = new System.IO.FileStream(LFilename,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
				LRemarks = new XmlDocument();
				LRemarks.Load(LStream);
				try
				{
					LSect2 = LRemarks.SelectSingleNode("//sect2");
					if (LSect2 != null)
					{
						LTitle = LSect2.SelectSingleNode("./title");
						if (LTitle != null)
							LSect2.RemoveChild(LTitle);
						LTitle = LSect2.SelectSingleNode("./sect2info");
						if (LTitle != null)
							LSect2.RemoveChild(LTitle);
						AOutputFile.Write(LSect2.InnerXml);
					}
				}
				finally
				{
					LStream.Close();
				}
			}
		}
	}
	
	/*
	/// <summary>
	/// This orders operators selected based on the order the operators are
	/// defined in the catalog. This provides for grouping based on when the
	/// operators were installed in the Catalog
	/// </summary>
	public class DocOperatorOrdered : DocOperator
	{
		protected override void ProcessTable(ServerProcess AProcess, string ATemplateName, string AOutputFileName, Table ATable)
		{
			// ServerProcess is the connection to the Server and context of what is going on in this layer
			Scalar LID;
			StreamWriter LOutputFile = File.CreateText(AOutputFileName);
			try
			{
				switch(ATemplateName)
				{
					case "TopicXML":
						WriteTopicXMLHeader(AProcess, LOutputFile);
						break;
					default:
						break;
				}

				// order is determined by entry order into the catalog
				Schema.RowType LRowType = new Schema.RowType();
				LRowType.Columns.Add(new Schema.Column("ID", Schema.DataType.SystemGuid));
				Row LRow = new Row(LRowType, AProcess);
				for (int LIndex = 0; LIndex < AProcess.Plan.Catalog.Count; LIndex++)
				{
					LRow[0] = Scalar.FromGuid(AProcess.Plan.Catalog[LIndex].ID);
					switch(ATemplateName)
					{
						case "TopicXML":
							WriteTopicXMLData(AProcess, AOutputFileName.Substring(0,AOutputFileName.LastIndexOf("\\")), LOutputFile, ATable, LRow);
							break;
						default:
							WriteData(AProcess, AOutputFileName.Substring(0,AOutputFileName.LastIndexOf("\\")), LOutputFile, ATable, LRow);
							break;
					}
				}

				switch(ATemplateName)
				{
					case "TopicXML":
						WriteTopicXMLFooter(AProcess, LOutputFile);
						break;
					default:
						break;
				}
			}
			finally
			{
				LOutputFile.Close();
			}
		}

	}
	*/

	/// <summary>
	/// Documents a Dataphor library in docbook format. Each library is a chapter node. The document controls this.
	/// Selections out of the catalog are what are documented, there may be human generated docs for merge.
	/// </summary>
	public class DocLibrary : Instructions.InstructionNode
	{

		/// <summary>The main navigator which keeps track of where we
		/// are in the document.</summary>
		DAEClient.DataSession FDataSession;
		XPathNavigator FxPathNavigator;
		string FExtDocPath;
		bool FTraceOn;
		StreamWriter FTrace;


		public override DataVar InternalExecute(ServerProcess AProcess, DataVar[] AArguments)
		{
			//if (AArguments[0].Value == null)
			//	throw new RuntimeException(CExpressionExpected); // library name to doc
			//if (AArguments[1].Value == null)
			//	throw new RuntimeException(CExpressionExpected); // XML template file
			//if (AArguments[2].Value == null)
			//	throw new RuntimeException(CFilenameExpected); // "extdoc" source directory
			//if (AArguments[3].Value == null)
			//	throw new RuntimeException(CFilenameExpected); // output path, name is <libraryname>.xml
			// AArguments[4].Value  trace on or off

			// todo: consider append/overwrite flag and write direct to disk
			
			string LLibraryName = AArguments[0].Value.AsString;
			string LTemplateFile = AArguments[1].Value.AsString;
			string LFilename = LLibraryName + ".xml";
			string LOutputPath = AArguments[3].Value.AsString; // assumes trailing backslash
			string LOutputFilename = Path.Combine(LOutputPath, LFilename);

			FExtDocPath = AArguments[2].Value.AsString;
			if (AArguments[4].Value == null)
				FTraceOn = false;
			else
				FTraceOn = AArguments[4].Value.AsString.Equals("on");

			System.IO.FileStream LStream = new System.IO.FileStream(LTemplateFile,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
			XmlDocument LxmlTemplate = new XmlDocument();
			LxmlTemplate.Load(LStream);
			if (FTraceOn)
				FTrace = File.CreateText(LOutputFilename + ".trc");

			try
			{

				FxPathNavigator = LxmlTemplate.CreateNavigator();

				// new dataconnection
				FDataSession = new DAEClient.DataSession();
				FDataSession.SessionInfo.UserID = AProcess.ServerSession.SessionInfo.UserID;
				FDataSession.SessionInfo.Password = AProcess.ServerSession.SessionInfo.Password;
				FDataSession.ServerConnection = new DAEClient.ServerConnection(AProcess.ServerSession.Server);
				FDataSession.Open();

				ProcessTemplate(AProcess, LOutputFilename);

				FDataSession.Close();
				return(null);
			}
			finally
			{
				LStream.Close();
				if (FTraceOn)
					FTrace.Close();
			}
			

		}

		protected void UpdateParams(DAEClient.DataView LDataView, Row ARow, string AParams)
		{
			if (ARow != null && AParams != "")
			{
				DAEClient.DataSetParamGroup LParamGroup = new DAEClient.DataSetParamGroup();
				DAEClient.DataSetParam LParam;
				string[] LParams = AParams.Split(new char[] {';', ',', '\n', '\r'});
				string LParamName;
				string LColumnName;
				int LPos;

				LParamGroup.Source = null;

				foreach(String LParamStr in LParams)
				{
					LPos = LParamStr.IndexOf('=');
					if (LPos >= 0)
					{
						LColumnName = LParamStr.Substring(LPos + 1).Trim();
						LParamName = LParamStr.Substring(0, LPos).Trim();
					}
					else
					{
						LColumnName = LParamStr;
						LParamName = LParamStr;
					}
					
					LParam = new DAEClient.DataSetParam();
					LParam.Name = LParamName;
					LParam.Modifier =  Alphora.Dataphor.DAE.Language.Modifier.Const;
					LParam.DataType = ARow.DataType.Columns[LColumnName].DataType;
					LParam.Value = (Scalar)ARow[LColumnName];
					//LParam.ColumnName = LColumnName;
					LParamGroup.Params.Add(LParam);
				}
				LDataView.ParamGroups.Add(LParamGroup);
			}
		}

		protected DAEClient.DataView GetParamDataView(string AExpression, Row ARow)
		{
			string LParamStr;
			string LExpression = AExpression;
			int LSpacePos = AExpression.IndexOf(";");

			if (LSpacePos > 0)
			{
				LParamStr = LExpression.Substring(LSpacePos + 1);
				LExpression = LExpression.Substring(0, LSpacePos);
			}
			else
				LParamStr = String.Empty;

			// todo: how to create a new process for the evaluation
			// todo: how does getting a new process fit into this?
			//ServerProcess LProcess = (ServerProcess)FDataphorConnection.ServerSession.StartProcess(new ProcessInfo(AProcess.ServerSession.SessionInfo));

			DAEClient.DataView LDataView = new DAEClient.DataView();
			LDataView.Session = FDataSession;
			LDataView.Expression = LExpression;
			LDataView.IsReadOnly = true;
			LDataView.CursorType = CursorType.Static;
			// attach parameters from row to the new view
			UpdateParams(LDataView, ARow, LParamStr);
			LDataView.Open(DAEClient.DataSetState.Browse);

			return (LDataView);
		}

		protected string D4ExpressionField(ServerProcess AProcess, Row ARow, string AParamsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <fieldname> <expression>

			DAEClient.DataView LDataView;
			string LExpression;
			string LFieldName = AParamsString;
			int LSpacePos = LFieldName.IndexOf(" ");
			LExpression = LFieldName.Substring(LSpacePos + 1);
			LFieldName = LFieldName.Substring(0, LSpacePos);
			

			LDataView = GetParamDataView(LExpression, ARow);
			try
			{
				LDataView.First();
				if (! LDataView.EOF)
					return D4Field(LFieldName, LDataView.ActiveRow);
				else
					return String.Format("Message: No data from D4ExpressionField: {0}", LExpression);

			}
			finally
			{
				LDataView.Dispose();
			}
		}

		protected bool D4ExpressionIf(ServerProcess AProcess, Row ARow, string AParamsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <expression>
			//   where expression is of the form: TableDee add { <expression> Result }
			//     and the result is boolean, True is required for the block to process

			DAEClient.DataView LDataView;
			
			LDataView = GetParamDataView(AParamsString, ARow);
			try
			{
				LDataView.First();
				if (! LDataView.EOF)
					return D4Field("Result", LDataView.ActiveRow) == "True";
				else
					return false;
			}
			finally
			{
				LDataView.Dispose();
			}
		}

		protected bool ProcessBlockNode(ServerProcess AProcess, XPathNavigator ATemplate, XmlTextWriter AOutput, string ARowTag, DAEClient.DataView ADataView)
		{

			XPathNavigator LNav;
			
			bool LParentContainsRowTag = ((IHasXmlNode)ATemplate).GetNode().SelectNodes(String.Format(".//{0}",ARowTag)).Count > 0;
			bool LContainsPI;
				
			if (FTraceOn)
				FTrace.WriteLine(String.Format("Process BlockNode Tag = {0} ContainsRowTag = {1}",ATemplate.LocalName, LParentContainsRowTag.ToString()));

			ProcessNode(AProcess, ATemplate, AOutput, ADataView.ActiveRow, ! LParentContainsRowTag);

			if (LParentContainsRowTag)
			{

				if (ATemplate.HasChildren)
				{
					ATemplate.MoveToFirstChild();
					do 
					{
						LContainsPI = ((IHasXmlNode)ATemplate).GetNode().SelectNodes(".//processing-instruction('DocLib')").Count > 0;
						if (FTraceOn)
							FTrace.WriteLine(String.Format("Process BlockNode child Tag = {0} ContainsProcessing Instruction = {1}",ATemplate.LocalName, LContainsPI.ToString()));

						if (LContainsPI)
						{
							if (ARowTag.Equals(ATemplate.LocalName))
							{
								LNav = ATemplate.Clone();
								while(! ADataView.EOF)
								{
									ProcessNode(AProcess, LNav, AOutput, ADataView.ActiveRow, true);
									ADataView.Next();
									LNav.MoveTo(ATemplate); // move to the first tag of the fragment
								}
							}
							else
								ProcessBlockNode(AProcess, ATemplate, AOutput, ARowTag, ADataView);
						}
						else
							ProcessNode(AProcess, ATemplate, AOutput, ADataView.ActiveRow, true);

					} while(ATemplate.MoveToNext());

					ATemplate.MoveToParent();
				}
		
				if (FTraceOn)
					FTrace.WriteLine(String.Format("Closing BlockNode Tag = {0}",ATemplate.LocalName));
				AOutput.WriteEndElement();
			}

			return(true);
		}

		protected bool D4ExpressionBlock(ServerProcess AProcess, XPathNavigator ATemplate, XmlTextWriter AOutput, Row ARow, string AParamsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <rowtag> <expression>

			
			XPathNavigator LNav;
			DAEClient.DataView LDataView;
			int LSpacePos;
			string LRowTag;
			string LExpression = AParamsString;

		
			LSpacePos = LExpression.IndexOf(" ");
			LRowTag = LExpression.Substring(0, LSpacePos);
			LExpression =  LExpression.Substring(LSpacePos + 1);
			
			LDataView = GetParamDataView(LExpression, ARow);
			try
			{
				LDataView.First();
				ATemplate.MoveToNext();

				if (FTraceOn)
					FTrace.WriteLine(String.Format("D4ExpressionBlock: LRowTag='{0}' LExpression='{1}' FollowingTag='{2}'", LRowTag, LExpression, ATemplate.LocalName));

				/*
				 * if there is the row tag, then a sub block is repeated for each row
				 * otherwise, repeat the "block" tag for each row
				 * 
				 *	This model allows for the "block" as an external document, which
				 * allows all libraries, for example, to be processed by a D4ExpressionBlock and a ExtDoc combination
				 * of tags.
				 */
				if (! LDataView.EOF)
				{
					if (LRowTag.Equals("*"))
					{
						LNav = ATemplate.Clone();
						LDataView.First();
						while(! LDataView.EOF)
						{
							// process the template
							ProcessNode(AProcess, LNav, AOutput, LDataView.ActiveRow, true);
							LDataView.Next();
							LNav.MoveTo(ATemplate); // move to the first tag of the fragment
						}
					}
					else
						if (((IHasXmlNode)ATemplate).GetNode().SelectNodes(String.Format(".//{0}",LRowTag)).Count > 0)
							ProcessBlockNode(AProcess, ATemplate, AOutput, LRowTag, LDataView);
						else
						{
							AOutput.WriteComment(String.Format("Error: D4Expression Block missing row tag name [ {0} ]", LRowTag));
							AOutput.WriteComment(String.Format("Expression: {0}", LExpression));
						}
				}
				else
					AOutput.WriteComment(String.Format("Message: No data from D4ExpressionBlock: {0}", LExpression));

			}
			finally
			{
				LDataView.Dispose();
			}
			return (true);
			
		}

		protected string D4Field(string AParamString, Row ARow)
		{
			// data assumed to be simple text to be inserted into the file
			// AParamString should be this format: <fieldname>
			if (ARow != null)
				return(ARow[AParamString].AsString);
			else
				return(String.Empty);
		}

		protected bool D4ExtDoc(ServerProcess AProcess, XPathNavigator ATemplate, XmlTextWriter AOutput, string AParamsString, Row ARow)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <inner | outer> droptags="tag,tag,.." <fieldname> <expression>
			// may use the extdoc path passed in with the filename for finding the file

			int LSpacePos;
			string LSelection;
			string LDropTags;
			string LPassThrough = AParamsString;
			XmlDocument LTemplate = null;
			XmlNode LNode;
			XmlNode LTopNode;

			XPathNavigator LxPathNavigator;

			LSpacePos = LPassThrough.IndexOf(" ");
			LSelection = LPassThrough.Substring(0, LSpacePos);
			LPassThrough = LPassThrough.Substring(LSpacePos + 1);
			LSpacePos = LPassThrough.IndexOf("droptags=\"");
			LDropTags = LPassThrough.Substring(LSpacePos + 10);
			LSpacePos = LDropTags.IndexOf("\"");
			LPassThrough = LDropTags.Substring(LSpacePos + 1);
			LDropTags = LDropTags.Substring(0, LSpacePos);
			LPassThrough =  LPassThrough.Trim();
			

			string LTemplateStr = D4ExpressionField(AProcess, ARow, LPassThrough);

			if (LTemplateStr != "")
			{
				LTemplate = new XmlDocument();
				LTemplate.LoadXml(LTemplateStr);

				
				// process LDropTags
				string delimStr = " ,";
				char [] delimiter = delimStr.ToCharArray();

				string [] LTags = null;
				LTags = LDropTags.Split(delimiter);

				if (FTraceOn)
					FTrace.WriteLine(String.Format("D4Extdoc: xml.{0} drop='{1}' passthrough='{2}'", LSelection, LDropTags, LPassThrough));

				LTopNode = LTemplate.FirstChild;
				while (LTopNode.NodeType != XmlNodeType.Element)
					LTopNode = LTopNode.NextSibling;

				foreach(string LTag in LTags)
				{
					if (LTag != "")
					{
						LNode = LTopNode.SelectSingleNode(String.Format("./{0}",LTag));
						if (FTraceOn)
							FTrace.WriteLine(String.Format("	droptag {0} from {1} (can do? {2}", LTag, LTopNode.LocalName, LTopNode.IsReadOnly));
						if (LNode != null)
						{
							if (FTraceOn)
								FTrace.WriteLine("		dropped");
							LTopNode.RemoveChild(LNode);
						}
					}
				}

				// navigate to the first node: Root(), FirstChild() that is an element not a PI
				LxPathNavigator = LTemplate.CreateNavigator();
				LxPathNavigator.MoveToRoot();
				LxPathNavigator.MoveToFirstChild();
				while (LxPathNavigator.NodeType != XPathNodeType.Element)
					LxPathNavigator.MoveToNext();

				//  process through ProcessNode so external documents can use DocLib processing instructions
				if (LSelection.Equals("inner"))
				{
					LxPathNavigator.MoveToFirstChild();
					do 
					{
						ProcessNode(AProcess, LxPathNavigator, AOutput, ARow, true);
					} while(LxPathNavigator.MoveToNext());
				}
				else
					ProcessNode(AProcess, LxPathNavigator, AOutput, ARow, true);
			}

			return(true);
		}

		protected bool ExtDoc(ServerProcess AProcess, XPathNavigator ATemplate, XmlTextWriter AOutput, string AParamsString, Row ARow)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <inner | outer> droptags="tag,tag,.." <filename>
			// may use the extdoc path passed in with the filename for finding the file

			int LSpacePos;
			string LSelection;
			string LDropTags;
			string LFilename = AParamsString;
			XmlDocument LRemarks = null;
			XmlNode LNode;
			XmlNode LTopNode;

			XPathNavigator LxPathNavigator;

			LSpacePos = LFilename.IndexOf(" ");
			LSelection = LFilename.Substring(0, LSpacePos);
			LFilename = LFilename.Substring(LSpacePos + 1);
			LSpacePos = LFilename.IndexOf("droptags=\"");
			LDropTags = LFilename.Substring(LSpacePos + 10);
			LSpacePos = LDropTags.IndexOf("\"");
			LFilename = LDropTags.Substring(LSpacePos + 1);
			LDropTags = LDropTags.Substring(0, LSpacePos);
			LFilename =  LFilename.Trim();
			
			// load the external file, remove title, stuff innerxml into the outputfile

			if (! System.IO.File.Exists(LFilename))
			{
				if (System.IO.File.Exists(Path.Combine(FExtDocPath,  LFilename)))
					LFilename = Path.Combine(FExtDocPath, LFilename);
			}

			if(System.IO.File.Exists(LFilename))
			{
				System.IO.FileStream LStream = new System.IO.FileStream(LFilename,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
				LRemarks = new XmlDocument();
				
				// assumes the document is a fragment and that the first element child of the root is what is wanted
				LRemarks.Load(LStream);

				try
				{
					// process LDropTags
					string delimStr = " ,";
					char [] delimiter = delimStr.ToCharArray();

					string [] LTags = null;
					LTags = LDropTags.Split(delimiter);

					if (FTraceOn)
						FTrace.WriteLine(String.Format("Extdoc: xml.{0} drop='{1}' filename='{2}'", LSelection, LDropTags, LFilename));

					LTopNode = LRemarks.FirstChild;
					while (LTopNode.NodeType != XmlNodeType.Element)
						LTopNode = LTopNode.NextSibling;

					foreach(string LTag in LTags)
					{
						LNode = LTopNode.SelectSingleNode(String.Format("./{0}",LTag));
						if (FTraceOn)
							FTrace.WriteLine(String.Format("	droptag {0} from {1} (can do? {2}", LTag, LTopNode.LocalName, LTopNode.IsReadOnly));
						if (LNode != null)
						{
							if (FTraceOn)
								FTrace.WriteLine("		dropped");
							LTopNode.RemoveChild(LNode);
						}
					}

					// navigate to the first node: Root(), FirstChild() that is an element not a PI
					LxPathNavigator = LRemarks.CreateNavigator();
					LxPathNavigator.MoveToRoot();
					LxPathNavigator.MoveToFirstChild();
					while (LxPathNavigator.NodeType != XPathNodeType.Element)
						LxPathNavigator.MoveToNext();

					//  process through ProcessNode so external documents can use DocLib processing instructions
					if (LSelection.Equals("inner"))
					{
						LxPathNavigator.MoveToFirstChild();
						do 
						{
							ProcessNode(AProcess, LxPathNavigator, AOutput, ARow, true);
						} while(LxPathNavigator.MoveToNext());
					}
					else
						ProcessNode(AProcess, LxPathNavigator, AOutput, ARow, true);
				}
				finally
				{
					LStream.Close();
				}
			}

			return(true);
		}
		
		protected bool WriteNodeWithAttr(XPathNavigator ATemplate, XmlTextWriter AOutput, Row ARow)
		{
			XmlNode LNode = ((IHasXmlNode)ATemplate).GetNode();
			AOutput.WriteStartElement(LNode.LocalName);

			// preserve node attributes from the template
			foreach (XmlAttribute LAttribute in LNode.Attributes)
			{
				// todo: implement processing instructions with "parameters" (D4Field, D4ExpressionField) probably use ? ? as delimiter in Value rather than <? ?> illegal
				if (LAttribute.Value.IndexOf("DocLib:D4Field_") == 0)
				{
					AOutput.WriteAttributeString(LAttribute.LocalName, D4Field(LAttribute.Value.Substring(15), ARow));
				}
				else
					AOutput.WriteAttributeString(LAttribute.LocalName, LAttribute.Value);
			}
			return(true);
		}

		protected bool ProcessNode(ServerProcess AProcess, XPathNavigator ATemplate, XmlTextWriter AOutput, Row ARow, bool AProcessChildren)
		{
			XPathNodeType LNodeType = ATemplate.NodeType;
			XmlProcessingInstruction LPI = null;
			int LSpacePos;
			string LPICommand;
			string LPIData;

			if (FTraceOn)
				FTrace.WriteLine(String.Format("Process Node {0} {1}",ATemplate.LocalName,LNodeType.ToString()));

			switch(LNodeType)
			{
				case XPathNodeType.ProcessingInstruction:
					// execute DocLib instruction
					LPI = (XmlProcessingInstruction)((IHasXmlNode)ATemplate).GetNode();
					if (LPI.Target.Equals("DocLib"))
					{
						// get the command from the text of the processing instruction
						LPIData = LPI.Data;
						LSpacePos = LPIData.IndexOf(" ");
						LPICommand = LPIData.Substring(0, LSpacePos);
						LPIData = LPIData.Substring(LSpacePos + 1);

						if (FTraceOn)
							FTrace.WriteLine(String.Format("Process PI {0} = {1}", LPICommand, LPIData));

						switch(LPICommand)
						{
							case "D4ExpressionField":
								AOutput.WriteString(D4ExpressionField(AProcess, ARow, LPIData));
								break;
							case "D4ExpressionBlock":
								D4ExpressionBlock(AProcess, ATemplate, AOutput, ARow, LPIData);
								break;
							case "D4ExpressionIf":
								// TableDee add { <expression> Result }
								if (! D4ExpressionIf(AProcess, ARow, LPIData) )
								{
									// skip to next
									ATemplate.MoveToNext();
									// if next isn't an element, skip to the element
									if (ATemplate.NodeType != XPathNodeType.Element)
									{
										while (ATemplate.NodeType != XPathNodeType.Element)
										{
											ATemplate.MoveToNext();
										}
									}
								}
								break;
							case "D4Field": // illegal at this point?
								AOutput.WriteString(D4Field(LPIData, ARow));
								break;
							case "D4ExtDoc":
								D4ExtDoc(AProcess, ATemplate, AOutput, LPIData, ARow);
								break;
							case "ExtDoc":
								ExtDoc(AProcess, ATemplate, AOutput, LPIData, ARow);
								break;
							default:
								// passthrough unknown commands
								AOutput.WriteProcessingInstruction(LPI.Target, LPI.Data);
								break;
						}
					}
					else
					{
						// preserve other processing instructions
						AOutput.WriteProcessingInstruction(LPI.Target, LPI.Data);
					}
					break;
				case XPathNodeType.Text:
					AOutput.WriteString(ATemplate.Value);
					break;
				case XPathNodeType.Comment:
					AOutput.WriteComment(ATemplate.Value);
					break;
				case XPathNodeType.Element:
					// write out regular nodes and process children
					WriteNodeWithAttr(ATemplate, AOutput, ARow);
					if (AProcessChildren)
					{
						if (ATemplate.HasChildren)
						{
							ATemplate.MoveToFirstChild();
							do 
							{
								ProcessNode(AProcess, ATemplate, AOutput, ARow, true);
							} while(ATemplate.MoveToNext());

							ATemplate.MoveToParent();
						}
						AOutput.WriteEndElement();
					}
					else
						if (ATemplate.IsEmptyElement)
							AOutput.WriteEndElement();

					break;
			}
			return(true);
		}

		protected bool ProcessTemplate(ServerProcess AProcess, string AOutputFilename)
		{
			// create a writer and move to the starting node
			XmlTextWriter LDocbookWriter  = new XmlTextWriter(new MemoryStream(), System.Text.Encoding.UTF8);
			LDocbookWriter.Formatting = System.Xml.Formatting.Indented;
			LDocbookWriter.IndentChar = '\t';
			LDocbookWriter.Indentation = 1;

			FxPathNavigator.MoveToRoot();
			FxPathNavigator.MoveToFirstChild(); // moves to first tag, as this is a fragment probably the right node?

			do
			{
				ProcessNode(AProcess, FxPathNavigator, LDocbookWriter, null, true); // assumes one top ` like chapter, not 2 chapters at once
			} while(FxPathNavigator.MoveToNext());

			return(EmitXml(LDocbookWriter, AOutputFilename));
		}

		protected bool EmitXml(XmlTextWriter AData, string AOutputFilename)
		{

			StreamWriter sw = File.CreateText(AOutputFilename);
			Stream fs = sw.BaseStream;
			AData.Flush();
			MemoryStream ms = (MemoryStream)AData.BaseStream;
			ms.Position = 0;
			fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
			fs.Close();
			return(true);
		}


	}
}
