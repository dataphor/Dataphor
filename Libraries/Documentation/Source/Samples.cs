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
	using System.Collections.Generic;
	

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
		public const string FilenameExpected = @"Filename expected";
		public const string ExpressionExpected = @"Expression expected";
		protected string _title = "";
		
		public override object InternalExecute(Program program, object[] arguments)
		{
			// AArguments[0] is template name, may be blank
			// AArguments[1] is the title
			//if (AArguments[1].Value == null)
			//	throw new RuntimeException(CFilenameExpected); // output filename
			//if (AArguments[2].Value == null)
			//	throw new RuntimeException(CExpressionExpected); // expression to "export"
			_title = (string)arguments[1];
			if (_title == "")
				_title = "Catalog Objects Documentation";

			if (arguments.Length == 4)
				ProcessTable
				(
					program.ServerProcess, 
					(string)arguments[0], // template
					(string)arguments[2], // output file name
					program.CursorManager.GetCursor(((CursorValue)arguments[3]).ID).Table
				);
			else
				ProcessTable
				(
					program.ServerProcess, 
					"", 
					(string)arguments[2], // Output file name
					program.CursorManager.GetCursor(((CursorValue)arguments[3]).ID).Table
				);

			return(null);
			// goal is to walk through the expression and extract metadata from the catalog for each item
		}

		protected virtual void ProcessTable(ServerProcess process, string templateName, string outputFileName, ITable table)
		{
			// ServerProcess is the connection to the Server and context of what is going on in this layer
			IRow row;
			StreamWriter outputFile = File.CreateText(outputFileName);
			try
			{
				switch(templateName)
				{
					case "TopicXML":
						WriteTopicXMLHeader(process, outputFile);
						break;
					case "Docbook":
						WriteDocbookHeader(process, outputFile);
						break;
					default:
						break;
				}

				// the burden of ordering AND selecting the data on the cursor parameter
				while(table.Next())
				{
					row = table.Select();
					// select the row in the Catalog Objects table and write it out
					switch(templateName)
					{
						case "TopicXML":
							WriteTopicXMLData(process, outputFileName.Substring(0,outputFileName.LastIndexOf("\\")), outputFile, table, row);
							break;
						case "Docbook":
							WriteDocbookData(process, outputFileName.Substring(0,outputFileName.LastIndexOf("\\")), outputFile, table, row);
							break;
						default:
							WriteData(process, outputFileName.Substring(0,outputFileName.LastIndexOf("\\")), outputFile, table, row);
							break;
					}
				}
			
				switch(templateName)
				{
					case "TopicXML":
						WriteTopicXMLFooter(process, outputFile);
						break;
					case "Docbook":
						WriteDocbookFooter(process, outputFile);
						break;
					default:
						break;
				}
			}
			finally
			{
				outputFile.Close();
			}
		}

		protected virtual void WriteData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			// non-formatted output is name \n comment \n\n

			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];

			// get the metadata for the row and print it

			outputFile.WriteLine(objectValue.Name);
			//AOutputFile.WriteLine(LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment);
			try
			{
				outputFile.WriteLine(MetaData.GetTag(objectValue.MetaData, "Catalog.Comment", String.Empty));
			}
			catch
			{
				// do nothing
			}
			outputFile.WriteLine("");
		}

		protected virtual void WriteTopicXMLHeader(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			outputFile.WriteLine("<AlphoraTopic>");
			outputFile.WriteLine("	<topics>");
		}

		protected virtual void WriteTopicXMLFooter(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("	</topics>");
			outputFile.WriteLine("</AlphoraTopics>");
		}

		protected virtual void WriteTopicXMLData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];

			outputFile.WriteLine("		<topic name=\"{0}\">", objectValue.Name);
			outputFile.WriteLine("			<title>{0}</title>", objectValue.Name);
			outputFile.WriteLine("			<summary>");
			//AOutputFile.WriteLine("				{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
			try
			{
				outputFile.WriteLine("				{0}", (MetaData.GetTag( objectValue.MetaData, "Catalog.Comment", String.Empty)));
			}
			catch
			{
				// do nothing;
			}
			outputFile.WriteLine("			</summary>");
			outputFile.WriteLine("		</topic>");
		}

		protected virtual void WriteDocbookHeader(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("<sect1 id='{0}'>",_title.Replace(" ",""));
			outputFile.WriteLine("<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", _title);
		}

		protected virtual void WriteDocbookFooter(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("</sect1>");
		}

		protected virtual void WriteDocbookData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];
			outputFile.WriteLine("		<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", objectValue.Name);
			outputFile.WriteLine("		<para>");
			//AOutputFile.WriteLine("			{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
			try
			{
				outputFile.WriteLine("			{0}", (MetaData.GetTag(objectValue.MetaData, "Catalog.Comment", String.Empty)));
			}
			catch
			{
				// do nothing

			}
			outputFile.WriteLine("		</para>");
			outputFile.WriteLine("		<bridgehead renderas='sect4'>Overloads</bridgehead>");
			outputFile.WriteLine("		</programlisting>");
			outputFile.WriteLine("	</sect2>");
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
		protected override void WriteData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];

			// todo: see if it is an operator, in which case may check IsBuiltin

			//if (LOperator.IsSystem && ! LOperator.IsBuiltin)
			//{
				D4TextEmitter Emitter = new D4TextEmitter();

				string stringValue = Emitter.Emit(objectValue.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));

				outputFile.WriteLine(stringValue);
				outputFile.WriteLine("");
			//}
		}

		protected override void WriteTopicXMLData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];			
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];
			// todo: see if it is an operator, in which case may check IsBuiltin
			//if (LOperator.IsSystem && ! LOperator.IsBuiltin)
			//{
				string name;
				string filename;

				if (objectValue.Name.IndexOf("(") > -1)
					name = objectValue.Name.Substring(0,objectValue.Name.IndexOf("("));
				else
					name = objectValue.Name;

				outputFile.WriteLine("		<entry name=\"{0}\" href=\"{1}\\{2}.dxt\" topic=\"//topic[@id='{3}']\" />", name,filepath, objectValue.ID.ToString().Replace("-",""), objectValue.ID.ToString());

				filename = filepath + "\\" + objectValue.ID.ToString().Replace("-","") + ".dxt";
				if (System.IO.File.Exists(filename))
					filename = filepath + "\\" + objectValue.ID.ToString().Replace("-","") + ".new.dxt";

				StreamWriter localOutputFile = File.CreateText(filename);

				try
				{
					WriteTopicFile(process, filepath, localOutputFile, table, row);
				}
				finally
				{
					localOutputFile.Close();
				}
			//}

		}
		

		protected override void WriteTopicXMLHeader(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			outputFile.WriteLine("<AlphoraDoc>");
			outputFile.WriteLine("	<entries>");
		}

		protected override void WriteTopicXMLFooter(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("	</entries>");
			outputFile.WriteLine("</AlphoraDoc>");
		}

		protected void WriteTopicFile(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			D4TextEmitter Emitter = new D4TextEmitter();
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			Schema.Object objectValue = process.Catalog.Objects[(string)row["Name"]];
			string stringValue = Emitter.Emit(objectValue.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
			
			string name;

			if (objectValue.Name.IndexOf("(") > -1)
				name = objectValue.Name.Substring(0,objectValue.Name.IndexOf("("));
			else
				name = objectValue.Name;

			outputFile.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
			outputFile.WriteLine("<AlphoraTopic>");
			outputFile.WriteLine("	<topics>");
			outputFile.WriteLine("		<topic name=\"{0}\" id=\"{1}\">",name,objectValue.ID.ToString());
			outputFile.WriteLine("			<title>{0}</title>",name);
			outputFile.WriteLine("			<summary>");
			outputFile.WriteLine("				<!-- provide a brief description of the operator -->");
			outputFile.WriteLine("			</summary>");

			outputFile.WriteLine("			<part name=\"Syntax\">");
			outputFile.WriteLine("				<title>Syntax</title>");
			outputFile.WriteLine("				<para>");
			outputFile.WriteLine("					<code>");
			outputFile.WriteLine("{0}",stringValue.Trim());
			outputFile.WriteLine("					</code>");
			outputFile.WriteLine("				</para>");
			outputFile.WriteLine("			</part>");
			
			outputFile.WriteLine("			<part name=\"Parameters\">");
			outputFile.WriteLine("				<title>Parameters</title>");
			outputFile.WriteLine("				<!-- each parameter should be described using one of the following parameter markups -->");
			outputFile.WriteLine("				<!-- examples:");
			outputFile.WriteLine("				<param name=\"paramname\">param description</param>");
			outputFile.WriteLine("				<parameter name=\"paramname\" type=\"paratype,fullyqualified\" optional=\"0|1\" direction=\"in|out\" />");
			outputFile.WriteLine("				-->");
			outputFile.WriteLine("			</part>");

			outputFile.WriteLine("			<part name=\"Return\">");
			outputFile.WriteLine("				<title>Return</title>");
			outputFile.WriteLine("				<para>");
			outputFile.WriteLine("				<!-- describe the return value -->");
			outputFile.WriteLine("				</para>");
			outputFile.WriteLine("			</part>");

			outputFile.WriteLine("			<part name=\"Remarks\">");
			outputFile.WriteLine("				<title>Remarks</title>");
			outputFile.WriteLine("				<para>");
			outputFile.WriteLine("				<!-- describe other feature of the operator, exceptions that may occur, unexpected side effects, etc. -->");
			outputFile.WriteLine("				</para>");
			outputFile.WriteLine("			</part>");

			outputFile.WriteLine("			<part name=\"Example\">");
			outputFile.WriteLine("				<title>Example</title>");
			outputFile.WriteLine("				<para>");
			outputFile.WriteLine("				<!-- provide a code sample or more showing use of the operator -->");
			outputFile.WriteLine("				</para>");
			outputFile.WriteLine("			</part>");

			outputFile.WriteLine("		</topic>");
			outputFile.WriteLine("	</topics>");
			outputFile.WriteLine("</AlphoraTopic>");
		}

		protected override void WriteDocbookHeader(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("<sect1 id='SLR{0}'>", _title.Replace(" ",""));
			outputFile.WriteLine("<title><indexterm><primary>{0}</primary></indexterm>{0}</title>", _title);
		}

		protected override void WriteDocbookFooter(ServerProcess process, StreamWriter outputFile)
		{
			outputFile.WriteLine("</sect1>");
		}

		protected string GetJustName(Schema.Object objectValue)
		{
			if (objectValue is Schema.Operator)
				return(((Schema.Operator)objectValue).OperatorName);
			else
				return(objectValue.Name);
		}

		protected override void WriteDocbookData(ServerProcess process, string filepath, StreamWriter outputFile, ITable table, IRow row)
		{
			//Schema.Object LObject = AProcess.Catalog[ARow["ID"].ToGuid()];
			var names = new List<String>();
			Schema.Object objectValue = process.CatalogDeviceSession.ResolveName((string)row["Name"], process.ServerSession.NameResolutionPath, names);
			if (objectValue == null)
				throw new ArgumentException(String.Format("Could not resolve object name: {0}", (string)row["Name"]));

			string filename = "c:\\src\\Alphora\\Docs\\DocbookManuals\\OperatorDocs\\" + GetJustName(objectValue) + ".xml";
			
			if (System.IO.File.Exists(filename))
			{
				if (objectValue.IsSystem /*&& ! LObject.IsBuiltin*/)
				{
					outputFile.WriteLine("		<sect2 id=\"SLR{0}\">", GetJustName(objectValue));
					outputFile.WriteLine("			<title><indexterm><primary>{0}</primary></indexterm><indexterm><primary>D4 Operators</primary><secondary>{0}</secondary></indexterm>{0}</title>", GetJustName(objectValue));
					outputFile.WriteLine("			<para>");
					//AOutputFile.WriteLine("				{0}", (LObject.MetaData == null ? String.Empty : LObject.MetaData.Comment));
					try
					{
						outputFile.WriteLine("				{0}", (MetaData.GetTag(objectValue.MetaData, "Catalog.Comment", String.Empty)));
					}
					catch
					{
						// do nothing
					}
					outputFile.WriteLine("			</para>");
					outputFile.WriteLine("			<bridgehead renderas='sect4'>Declarations</bridgehead>");
					outputFile.WriteLine("			<programlisting>");
					// here walk the table for all of the same name (overloads)
					WriteDocbookDeclarations(process, outputFile, table, objectValue);

					outputFile.WriteLine("			</programlisting>");

					// here merge the external docs
					WriteMergeExtDocs(process, outputFile, objectValue);

					outputFile.WriteLine("		</sect2>");
				}
			}
		}

		protected void WriteDocbookDeclarations(ServerProcess process, StreamWriter outputFile, ITable table, Schema.Object objectValue)
		{
			// walk the table until the name of the object no longer matches, place the syntax in the outputfile for each overload
			IRow row;
			D4TextEmitter Emitter = new D4TextEmitter();
			Schema.Object localObjectValue = null;
			string stringValue = "";

			// write the definition of the current object
			stringValue = Emitter.Emit(objectValue.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
			outputFile.WriteLine("{0}", stringValue.TrimEnd(null));

			// write the definitions for all the overloads
			while (table.Next())
			{
				row = table.Select();
				//LObject = AProcess.Catalog[LRow["ID"].ToGuid()];
				localObjectValue = process.Catalog.Objects[(string)row["Name"]];
				if (GetJustName(localObjectValue) == GetJustName(objectValue))
				{
					stringValue = Emitter.Emit(localObjectValue.EmitStatement(Alphora.Dataphor.DAE.Language.D4.EmitMode.ForCopy));
					outputFile.WriteLine("");
					outputFile.WriteLine("{0}", stringValue.TrimEnd(null));
				}
				else
				{
					table.Prior();
					break;
				}
			}
		}

		protected void WriteMergeExtDocs(ServerProcess process, StreamWriter outputFile, Schema.Object objectValue)
		{
			// load the external file, remove title, stuff innerxml into the outputfile
			XmlDocument remarks = null;
			XmlNode title = null;
			XmlNode sect2 = null;

			// magic directory used on the docs merge machine, (should be a parameter?...)
			string filename = "c:\\src\\Alphora\\Docs\\DocbookManuals\\OperatorDocs\\" + GetJustName(objectValue) + ".xml";
			
			if (System.IO.File.Exists(filename))
			{
				System.IO.FileStream stream = new System.IO.FileStream(filename,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
				remarks = new XmlDocument();
				remarks.Load(stream);
				try
				{
					sect2 = remarks.SelectSingleNode("//sect2");
					if (sect2 != null)
					{
						title = sect2.SelectSingleNode("./title");
						if (title != null)
							sect2.RemoveChild(title);
						title = sect2.SelectSingleNode("./sect2info");
						if (title != null)
							sect2.RemoveChild(title);
						outputFile.Write(sect2.InnerXml);
					}
				}
				finally
				{
					stream.Close();
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
				for (int LIndex = 0; LIndex < AProcess.Catalog.Count; LIndex++)
				{
					LRow[0] = Scalar.FromGuid(AProcess.Catalog[LIndex].ID);
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
		DAEClient.DataSession _dataSession;
		XPathNavigator FxPathNavigator;
		string _extDocPath;
		bool _traceOn;
		StreamWriter _trace;


		public override object InternalExecute(Program program, object[] arguments)
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
			
			string libraryName = (string)arguments[0];
			string templateFile = (string)arguments[1];
			string filename = libraryName + ".xml";
			string outputPath = (string)arguments[3]; // assumes trailing backslash
			string outputFilename = Path.Combine(outputPath, filename);

			_extDocPath = (string)arguments[2];
			if (arguments[4] == null)
				_traceOn = false;
			else
				_traceOn = ((string)arguments[4]).Equals("on");

			System.IO.FileStream stream = new System.IO.FileStream(templateFile,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
			XmlDocument LxmlTemplate = new XmlDocument();
			LxmlTemplate.Load(stream);
			if (_traceOn)
				_trace = File.CreateText(outputFilename + ".trc");

			try
			{

				FxPathNavigator = LxmlTemplate.CreateNavigator();

				// new dataconnection
				_dataSession = new DAEClient.DataSession();
				_dataSession.Alias = new DAEClient.ConnectionAlias { Name = program.ServerProcess.ServerSession.Server.Name, InstanceName = program.ServerProcess.ServerSession.Server.Name };
				_dataSession.SessionInfo.UserID = program.ServerProcess.ServerSession.SessionInfo.UserID;
				_dataSession.SessionInfo.Password = program.ServerProcess.ServerSession.SessionInfo.Password;
				//FDataSession.ServerConnection = new DAEClient.ServerConnection(AProgram.ServerProcess.ServerSession.Server);
				_dataSession.Open();

				ProcessTemplate(program.ServerProcess, outputFilename);

				_dataSession.Close();
				return(null);
			}
			finally
			{
				stream.Close();
				if (_traceOn)
					_trace.Close();
			}
			

		}

		protected void UpdateParams(DAEClient.DataView LDataView, DAEClient.DataView sourceDataView, string paramsValue)
		{
			if (sourceDataView != null && !sourceDataView.IsEmpty() && paramsValue != "")
			{
				DAEClient.DataSetParamGroup paramGroup = new DAEClient.DataSetParamGroup();
				DAEClient.DataSetParam param;
				string[] localParamsValue = paramsValue.Split(new char[] {';', ',', '\n', '\r'});
				string paramName;
				string columnName;
				int pos;

				paramGroup.Source = null;

				foreach(String paramStr in localParamsValue)
				{
					pos = paramStr.IndexOf('=');
					if (pos >= 0)
					{
						columnName = paramStr.Substring(pos + 1).Trim();
						paramName = paramStr.Substring(0, pos).Trim();
					}
					else
					{
						columnName = paramStr;
						paramName = paramStr;
					}
					
					param = new DAEClient.DataSetParam();
					param.Name = paramName;
					param.Modifier =  Alphora.Dataphor.DAE.Language.Modifier.Const;
					param.DataType = sourceDataView.TableType.Columns[columnName].DataType;
					param.Value = sourceDataView[columnName];
					//LParam.ColumnName = LColumnName;
					paramGroup.Params.Add(param);
				}
				LDataView.ParamGroups.Add(paramGroup);
			}
		}

		protected DAEClient.DataView GetParamDataView(string expression, DAEClient.DataView dataView)
		{
			string paramStr;
			string localExpression = expression;
			int spacePos = expression.IndexOf(";");

			if (spacePos > 0)
			{
				paramStr = localExpression.Substring(spacePos + 1);
				localExpression = localExpression.Substring(0, spacePos);
			}
			else
				paramStr = String.Empty;

			// todo: how to create a new process for the evaluation
			// todo: how does getting a new process fit into this?
			//ServerProcess LProcess = (ServerProcess)FDataphorConnection.ServerSession.StartProcess(new ProcessInfo(AProcess.ServerSession.SessionInfo));

			DAEClient.DataView resultDataView = new DAEClient.DataView();
			resultDataView.Session = _dataSession;
			resultDataView.Expression = localExpression;
			resultDataView.IsReadOnly = true;
			resultDataView.CursorType = CursorType.Static;
			// attach parameters from row to the new view
			UpdateParams(resultDataView, dataView, paramStr);
			resultDataView.Open(DAEClient.DataSetState.Browse);

			return (resultDataView);
		}

		protected string D4ExpressionField(ServerProcess process, DAEClient.DataView dataView, string paramsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <fieldname> <expression>

			DAEClient.DataView subDataView;
			string expression;
			string fieldName = paramsString;
			int spacePos = fieldName.IndexOf(" ");
			expression = fieldName.Substring(spacePos + 1);
			fieldName = fieldName.Substring(0, spacePos);
			

			subDataView = GetParamDataView(expression, dataView);
			try
			{
				subDataView.First();
				if (! subDataView.EOF)
					return D4Field(fieldName, subDataView);
				else
					return String.Format("Message: No data from D4ExpressionField: {0}", expression);

			}
			finally
			{
				subDataView.Dispose();
			}
		}

		protected bool D4ExpressionIf(ServerProcess process, DAEClient.DataView dataView, string paramsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <expression>
			//   where expression is of the form: TableDee add { <expression> Result }
			//     and the result is boolean, True is required for the block to process

			DAEClient.DataView localDataView;
			
			localDataView = GetParamDataView(paramsString, dataView);
			try
			{
				localDataView.First();
				if (! localDataView.EOF)
					return D4Field("Result", localDataView) == "True";
				else
					return false;
			}
			finally
			{
				localDataView.Dispose();
			}
		}

		protected bool ProcessBlockNode(ServerProcess process, XPathNavigator template, XmlTextWriter output, string rowTag, DAEClient.DataView dataView)
		{

			XPathNavigator nav;
			
			bool parentContainsRowTag = ((IHasXmlNode)template).GetNode().SelectNodes(String.Format(".//{0}",rowTag)).Count > 0;
			bool containsPI;
				
			if (_traceOn)
				_trace.WriteLine(String.Format("Process BlockNode Tag = {0} ContainsRowTag = {1}",template.LocalName, parentContainsRowTag.ToString()));

			ProcessNode(process, template, output, dataView, !parentContainsRowTag);

			if (parentContainsRowTag)
			{

				if (template.HasChildren)
				{
					template.MoveToFirstChild();
					do 
					{
						containsPI = ((IHasXmlNode)template).GetNode().SelectNodes(".//processing-instruction('DocLib')").Count > 0;
						if (_traceOn)
							_trace.WriteLine(String.Format("Process BlockNode child Tag = {0} ContainsProcessing Instruction = {1}",template.LocalName, containsPI.ToString()));

						if (containsPI)
						{
							if (rowTag.Equals(template.LocalName))
							{
								nav = template.Clone();
								while(! dataView.EOF)
								{
									ProcessNode(process, nav, output, dataView, true);
									dataView.Next();
									nav.MoveTo(template); // move to the first tag of the fragment
								}
							}
							else
								ProcessBlockNode(process, template, output, rowTag, dataView);
						}
						else
							ProcessNode(process, template, output, dataView, true);

					} while(template.MoveToNext());

					template.MoveToParent();
				}
		
				if (_traceOn)
					_trace.WriteLine(String.Format("Closing BlockNode Tag = {0}",template.LocalName));
				output.WriteEndElement();
			}

			return(true);
		}

		protected bool D4ExpressionBlock(ServerProcess process, XPathNavigator template, XmlTextWriter output, DAEClient.DataView dataView, string paramsString)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <rowtag> <expression>

			
			XPathNavigator nav;
			DAEClient.DataView localDataView;
			int spacePos;
			string rowTag;
			string expression = paramsString;

		
			spacePos = expression.IndexOf(" ");
			rowTag = expression.Substring(0, spacePos);
			expression =  expression.Substring(spacePos + 1);
			
			localDataView = GetParamDataView(expression, dataView);
			try
			{
				localDataView.First();
				template.MoveToNext();

				if (_traceOn)
					_trace.WriteLine(String.Format("D4ExpressionBlock: LRowTag='{0}' LExpression='{1}' FollowingTag='{2}'", rowTag, expression, template.LocalName));

				/*
				 * if there is the row tag, then a sub block is repeated for each row
				 * otherwise, repeat the "block" tag for each row
				 * 
				 *	This model allows for the "block" as an external document, which
				 * allows all libraries, for example, to be processed by a D4ExpressionBlock and a ExtDoc combination
				 * of tags.
				 */
				if (! localDataView.EOF)
				{
					if (rowTag.Equals("*"))
					{
						nav = template.Clone();
						localDataView.First();
						while(! localDataView.EOF)
						{
							// process the template
							ProcessNode(process, nav, output, localDataView, true);
							localDataView.Next();
							nav.MoveTo(template); // move to the first tag of the fragment
						}
					}
					else
						if (((IHasXmlNode)template).GetNode().SelectNodes(String.Format(".//{0}",rowTag)).Count > 0)
							ProcessBlockNode(process, template, output, rowTag, localDataView);
						else
						{
							output.WriteComment(String.Format("Error: D4Expression Block missing row tag name [ {0} ]", rowTag));
							output.WriteComment(String.Format("Expression: {0}", expression));
						}
				}
				else
					output.WriteComment(String.Format("Message: No data from D4ExpressionBlock: {0}", expression));

			}
			finally
			{
				localDataView.Dispose();
			}
			return (true);
			
		}

		protected string D4Field(string paramString, DAEClient.DataView dataView)
		{
			// data assumed to be simple text to be inserted into the file
			// AParamString should be this format: <fieldname>
			if (dataView != null && !dataView.IsEmpty())
				return(dataView[paramString].AsString);
			else
				return(String.Empty);
		}

		protected bool D4ExtDoc(ServerProcess process, XPathNavigator template, XmlTextWriter output, string paramsString, DAEClient.DataView dataView)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <inner | outer> droptags="tag,tag,.." <fieldname> <expression>
			// may use the extdoc path passed in with the filename for finding the file

			int spacePos;
			string selection;
			string dropTags;
			string passThrough = paramsString;
			XmlDocument localTemplate = null;
			XmlNode node;
			XmlNode topNode;

			XPathNavigator LxPathNavigator;

			spacePos = passThrough.IndexOf(" ");
			selection = passThrough.Substring(0, spacePos);
			passThrough = passThrough.Substring(spacePos + 1);
			spacePos = passThrough.IndexOf("droptags=\"");
			dropTags = passThrough.Substring(spacePos + 10);
			spacePos = dropTags.IndexOf("\"");
			passThrough = dropTags.Substring(spacePos + 1);
			dropTags = dropTags.Substring(0, spacePos);
			passThrough =  passThrough.Trim();
			

			string templateStr = D4ExpressionField(process, dataView, passThrough);

			if (templateStr != "")
			{
				localTemplate = new XmlDocument();
				localTemplate.LoadXml(templateStr);

				
				// process LDropTags
				string delimStr = " ,";
				char [] delimiter = delimStr.ToCharArray();

				string [] tags = null;
				tags = dropTags.Split(delimiter);

				if (_traceOn)
					_trace.WriteLine(String.Format("D4Extdoc: xml.{0} drop='{1}' passthrough='{2}'", selection, dropTags, passThrough));

				topNode = localTemplate.FirstChild;
				while (topNode.NodeType != XmlNodeType.Element)
					topNode = topNode.NextSibling;

				foreach(string tag in tags)
				{
					if (tag != "")
					{
						node = topNode.SelectSingleNode(String.Format("./{0}",tag));
						if (_traceOn)
							_trace.WriteLine(String.Format("	droptag {0} from {1} (can do? {2}", tag, topNode.LocalName, topNode.IsReadOnly));
						if (node != null)
						{
							if (_traceOn)
								_trace.WriteLine("		dropped");
							topNode.RemoveChild(node);
						}
					}
				}

				// navigate to the first node: Root(), FirstChild() that is an element not a PI
				LxPathNavigator = localTemplate.CreateNavigator();
				LxPathNavigator.MoveToRoot();
				LxPathNavigator.MoveToFirstChild();
				while (LxPathNavigator.NodeType != XPathNodeType.Element)
					LxPathNavigator.MoveToNext();

				//  process through ProcessNode so external documents can use DocLib processing instructions
				if (selection.Equals("inner"))
				{
					LxPathNavigator.MoveToFirstChild();
					do 
					{
						ProcessNode(process, LxPathNavigator, output, dataView, true);
					} while(LxPathNavigator.MoveToNext());
				}
				else
					ProcessNode(process, LxPathNavigator, output, dataView, true);
			}

			return(true);
		}

		protected bool ExtDoc(ServerProcess process, XPathNavigator template, XmlTextWriter output, string paramsString, DAEClient.DataView dataView)
		{
			// assumes the processing instruction is the current node
			// AParamString should be this format: <inner | outer> droptags="tag,tag,.." <filename>
			// may use the extdoc path passed in with the filename for finding the file

			int spacePos;
			string selection;
			string dropTags;
			string filename = paramsString;
			XmlDocument remarks = null;
			XmlNode node;
			XmlNode topNode;

			XPathNavigator LxPathNavigator;

			spacePos = filename.IndexOf(" ");
			selection = filename.Substring(0, spacePos);
			filename = filename.Substring(spacePos + 1);
			spacePos = filename.IndexOf("droptags=\"");
			dropTags = filename.Substring(spacePos + 10);
			spacePos = dropTags.IndexOf("\"");
			filename = dropTags.Substring(spacePos + 1);
			dropTags = dropTags.Substring(0, spacePos);
			filename =  filename.Trim();
			
			// load the external file, remove title, stuff innerxml into the outputfile

			if (! System.IO.File.Exists(filename))
			{
				if (System.IO.File.Exists(Path.Combine(_extDocPath,  filename)))
					filename = Path.Combine(_extDocPath, filename);
			}

			if(System.IO.File.Exists(filename))
			{
				System.IO.FileStream stream = new System.IO.FileStream(filename,System.IO.FileMode.Open,FileAccess.Read, FileShare.Read);
				remarks = new XmlDocument();
				
				// assumes the document is a fragment and that the first element child of the root is what is wanted
				remarks.Load(stream);

				try
				{
					// process LDropTags
					string delimStr = " ,";
					char [] delimiter = delimStr.ToCharArray();

					string [] tags = null;
					tags = dropTags.Split(delimiter);

					if (_traceOn)
						_trace.WriteLine(String.Format("Extdoc: xml.{0} drop='{1}' filename='{2}'", selection, dropTags, filename));

					topNode = remarks.FirstChild;
					while (topNode.NodeType != XmlNodeType.Element)
						topNode = topNode.NextSibling;

					foreach(string tag in tags)
					{
						node = topNode.SelectSingleNode(String.Format("./{0}",tag));
						if (_traceOn)
							_trace.WriteLine(String.Format("	droptag {0} from {1} (can do? {2}", tag, topNode.LocalName, topNode.IsReadOnly));
						if (node != null)
						{
							if (_traceOn)
								_trace.WriteLine("		dropped");
							topNode.RemoveChild(node);
						}
					}

					// navigate to the first node: Root(), FirstChild() that is an element not a PI
					LxPathNavigator = remarks.CreateNavigator();
					LxPathNavigator.MoveToRoot();
					LxPathNavigator.MoveToFirstChild();
					while (LxPathNavigator.NodeType != XPathNodeType.Element)
						LxPathNavigator.MoveToNext();

					//  process through ProcessNode so external documents can use DocLib processing instructions
					if (selection.Equals("inner"))
					{
						LxPathNavigator.MoveToFirstChild();
						do 
						{
							ProcessNode(process, LxPathNavigator, output, dataView, true);
						} while(LxPathNavigator.MoveToNext());
					}
					else
						ProcessNode(process, LxPathNavigator, output, dataView, true);
				}
				finally
				{
					stream.Close();
				}
			}

			return(true);
		}
		
		protected bool WriteNodeWithAttr(XPathNavigator template, XmlTextWriter output, DAEClient.DataView dataView)
		{
			XmlNode node = ((IHasXmlNode)template).GetNode();
			output.WriteStartElement(node.LocalName);

			// preserve node attributes from the template
			foreach (XmlAttribute attribute in node.Attributes)
			{
				// todo: implement processing instructions with "parameters" (D4Field, D4ExpressionField) probably use ? ? as delimiter in Value rather than <? ?> illegal
				if (attribute.Value.IndexOf("DocLib:D4Field_") == 0)
				{
					output.WriteAttributeString(attribute.LocalName, D4Field(attribute.Value.Substring(15), dataView));
				}
				else
					output.WriteAttributeString(attribute.LocalName, attribute.Value);
			}
			return(true);
		}

		protected bool ProcessNode(ServerProcess process, XPathNavigator template, XmlTextWriter output, DAEClient.DataView dataView, bool processChildren)
		{
			XPathNodeType nodeType = template.NodeType;
			XmlProcessingInstruction pI = null;
			int spacePos;
			string pICommand;
			string pIData;

			if (_traceOn)
				_trace.WriteLine(String.Format("Process Node {0} {1}",template.LocalName,nodeType.ToString()));

			switch(nodeType)
			{
				case XPathNodeType.ProcessingInstruction:
					// execute DocLib instruction
					pI = (XmlProcessingInstruction)((IHasXmlNode)template).GetNode();
					if (pI.Target.Equals("DocLib"))
					{
						// get the command from the text of the processing instruction
						pIData = pI.Data;
						spacePos = pIData.IndexOf(" ");
						pICommand = pIData.Substring(0, spacePos);
						pIData = pIData.Substring(spacePos + 1);

						if (_traceOn)
							_trace.WriteLine(String.Format("Process PI {0} = {1}", pICommand, pIData));

						switch(pICommand)
						{
							case "D4ExpressionField":
								output.WriteString(D4ExpressionField(process, dataView, pIData));
								break;
							case "D4ExpressionBlock":
								D4ExpressionBlock(process, template, output, dataView, pIData);
								break;
							case "D4ExpressionIf":
								// TableDee add { <expression> Result }
								if (! D4ExpressionIf(process, dataView, pIData) )
								{
									// skip to next
									template.MoveToNext();
									// if next isn't an element, skip to the element
									if (template.NodeType != XPathNodeType.Element)
									{
										while (template.NodeType != XPathNodeType.Element)
										{
											template.MoveToNext();
										}
									}
								}
								break;
							case "D4Field": // illegal at this point?
								output.WriteString(D4Field(pIData, dataView));
								break;
							case "D4ExtDoc":
								D4ExtDoc(process, template, output, pIData, dataView);
								break;
							case "ExtDoc":
								ExtDoc(process, template, output, pIData, dataView);
								break;
							default:
								// passthrough unknown commands
								output.WriteProcessingInstruction(pI.Target, pI.Data);
								break;
						}
					}
					else
					{
						// preserve other processing instructions
						output.WriteProcessingInstruction(pI.Target, pI.Data);
					}
					break;
				case XPathNodeType.Text:
					output.WriteString(template.Value);
					break;
				case XPathNodeType.Comment:
					output.WriteComment(template.Value);
					break;
				case XPathNodeType.Element:
					// write out regular nodes and process children
					WriteNodeWithAttr(template, output, dataView);
					if (processChildren)
					{
						if (template.HasChildren)
						{
							template.MoveToFirstChild();
							do 
							{
								ProcessNode(process, template, output, dataView, true);
							} while(template.MoveToNext());

							template.MoveToParent();
						}
						output.WriteEndElement();
					}
					else
						if (template.IsEmptyElement)
							output.WriteEndElement();

					break;
			}
			return(true);
		}

		protected bool ProcessTemplate(ServerProcess process, string outputFilename)
		{
			// create a writer and move to the starting node
			XmlTextWriter docbookWriter  = new XmlTextWriter(new MemoryStream(), System.Text.Encoding.UTF8);
			docbookWriter.Formatting = System.Xml.Formatting.Indented;
			docbookWriter.IndentChar = '\t';
			docbookWriter.Indentation = 1;

			FxPathNavigator.MoveToRoot();
			FxPathNavigator.MoveToFirstChild(); // moves to first tag, as this is a fragment probably the right node?

			do
			{
				ProcessNode(process, FxPathNavigator, docbookWriter, null, true); // assumes one top ` like chapter, not 2 chapters at once
			} while(FxPathNavigator.MoveToNext());

			return(EmitXml(docbookWriter, outputFilename));
		}

		protected bool EmitXml(XmlTextWriter data, string outputFilename)
		{

			StreamWriter sw = File.CreateText(outputFilename);
			Stream fs = sw.BaseStream;
			data.Flush();
			MemoryStream ms = (MemoryStream)data.BaseStream;
			ms.Position = 0;
			fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
			fs.Close();
			return(true);
		}


	}
}
