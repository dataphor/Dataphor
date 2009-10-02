/*
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	public enum DocumentType { Document, Derive, Other, None };

	/// <summary> Document Express Parsing/Creation class. </summary>
	/// <remarks>
	///		The DocumentExpression class can either take the string input for a Document
	///		or Derive function and create the corresponding command 
	///		string, or create create the string given the class object for that operator.
	/// </remarks>
	public class DocumentExpression
	{
		public DocumentExpression(string AExpression)
		{
			Expression = AExpression;
		}

		public DocumentExpression() {}

		private DocumentType FType = DocumentType.None;
		public DocumentType Type
		{
			get	{ return FType;	}
			set 
			{ 
				if (FType != value)
				{
					FOtherExpression = null;
					FDocumentArgs = null;
					FDeriveArgs = null;
					FType = value;
					switch (value)       
					{         
						case DocumentType.Document:
							FDocumentArgs = new DocumentArgs();
							break;
						case DocumentType.Derive:
							FDeriveArgs = new DeriveArgs();
							break; 
						case DocumentType.Other: 
							FOtherExpression = String.Empty;
							break;
					}
				}
			}
		}

		private DocumentArgs FDocumentArgs;
		public DocumentArgs DocumentArgs 
		{ 
			get { return FDocumentArgs; } 
		}
		
		private DeriveArgs FDeriveArgs;
		public DeriveArgs DeriveArgs
		{
			get { return FDeriveArgs; }
		}	

		private string FOtherExpression;
		
		public string Expression
		{
			get
			{
				switch (FType)
				{
					case DocumentType.Derive :
						CallExpression LCallExpression = 
							new CallExpression
							(
								"Derive", 
								new Expression[] 
								{
									new ValueExpression(FDeriveArgs.Query)
								}
							);
						if (FDeriveArgs.PageType != String.Empty)
							LCallExpression.Expressions.Add(new ValueExpression(FDeriveArgs.PageType));
						if ((FDeriveArgs.MasterKeyNames != String.Empty) && (FDeriveArgs.DetailKeyNames != String.Empty))
						{
							LCallExpression.Expressions.Add(new ValueExpression(FDeriveArgs.MasterKeyNames));
							LCallExpression.Expressions.Add(new ValueExpression(FDeriveArgs.DetailKeyNames));
						}
						if (FDeriveArgs.Elaborate != DeriveArgs.CDefaultElaborate)
							LCallExpression.Expressions.Add(new ValueExpression(FDeriveArgs.Elaborate));

						return new Alphora.Dataphor.DAE.Language.D4.D4TextEmitter().Emit(LCallExpression);	
					case DocumentType.Document :
						return 
							new Alphora.Dataphor.DAE.Language.D4.D4TextEmitter().Emit
							(
								new CallExpression
								(
									FDocumentArgs.OperatorName, 
									new Expression[]
									{
										new ValueExpression(FDocumentArgs.LibraryName), 
										new ValueExpression(FDocumentArgs.DocumentName)
									}
								)
							);
					case DocumentType.Other :
						return FOtherExpression;
					default :
						return String.Empty;
				}
			}
			set
			{
				if ((value == null) || (value == String.Empty))
					Type = DocumentType.None;
				else
				{
					Expression LParsedExpression;
					CallExpression LCallExpression;
				
					Alphora.Dataphor.DAE.Language.D4.Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
					LParsedExpression = LParser.ParseExpression(value);
					
					//check to see if its a qulifier expression first				
					if (LParsedExpression is QualifierExpression)
					{
						QualifierExpression LQualifierExpression;
						LQualifierExpression = LParsedExpression as QualifierExpression;
						if ((((IdentifierExpression)LQualifierExpression.LeftExpression).Identifier) == "Frontend" || 
							(((IdentifierExpression)LQualifierExpression.LeftExpression).Identifier) == ".Frontend")
						{
							LCallExpression = LQualifierExpression.RightExpression as CallExpression;
						}
						else 
							LCallExpression = LParsedExpression as CallExpression;
					}
					else 
						LCallExpression = LParsedExpression as CallExpression;
					
					if (LCallExpression == null)	//if the LCallExpression is null then we don't know what it is - it is OTHER
					{
						Type = DocumentType.Other;
						FOtherExpression = value;
					}
					else if (IsDocumentIdentifier(LCallExpression.Identifier))  //Check If the parsed expression is a document and assign FDocumentArgs
					{
						Type = DocumentType.Document;
						FDocumentArgs.OperatorName = LCallExpression.Identifier;
						FDocumentArgs.LibraryName = ((ValueExpression)LCallExpression.Expressions[0]).Value.ToString();
						FDocumentArgs.DocumentName = ((ValueExpression)LCallExpression.Expressions[1]).Value.ToString();
					}
					else if ((LCallExpression.Identifier) == "Derive")  //Check if the parsed expression is a Derive call and assign FDeriveArgs
					{
						Type = DocumentType.Derive;
						FDeriveArgs.Query = ((ValueExpression)LCallExpression.Expressions[0]).Value.ToString();
						if (LCallExpression.Expressions.Count >= 2)
						{
							if 
							(
								((ValueExpression)LCallExpression.Expressions[1]).Value.ToString() != "True" || 
								((ValueExpression)LCallExpression.Expressions[1]).Value.ToString() != "False"
							)
							{
								FDeriveArgs.PageType = ((ValueExpression)LCallExpression.Expressions[1]).Value.ToString();
								if (LCallExpression.Expressions.Count == 3)
									FDeriveArgs.Elaborate = (bool)((ValueExpression)LCallExpression.Expressions[2]).Value;
								else if (LCallExpression.Expressions.Count >= 4)
								{
									FDeriveArgs.MasterKeyNames = ((ValueExpression)LCallExpression.Expressions[2]).Value.ToString();
									FDeriveArgs.DetailKeyNames = ((ValueExpression)LCallExpression.Expressions[3]).Value.ToString();
									if (LCallExpression.Expressions.Count == 5)
										FDeriveArgs.Elaborate = (bool)((ValueExpression)LCallExpression.Expressions[4]).Value;
								}
							}
							else
								FDeriveArgs.Elaborate = (bool)((ValueExpression)LCallExpression.Expressions[1]).Value;
						}
					}
					else
					{
						Type = DocumentType.Other;
						FOtherExpression = value;
					}					
				}
			}
		}	

		private bool IsDocumentIdentifier(string AIdentifier)
		{
			switch (AIdentifier)
			{
				case "Form" :
				case "Load" :
				case "LoadAndProcess" :
				case "Image" :
				case "LoadBinary" :
					return true;
				default :
					return false;
			}
		}
	}

	public class DocumentArgs
	{
		public const string CDefaultOperatorName = "Load";

		private string FOperatorName = CDefaultOperatorName;
		public string OperatorName
		{
			get { return FOperatorName; }
			set { FOperatorName = (value == null ? String.Empty : value); }
		}
		
		private string FLibraryName = String.Empty;
		public string LibraryName
		{
			get { return FLibraryName; }
			set { FLibraryName = (value == null ? String.Empty : value); }
		}

		private string FDocumentName = String.Empty;
		public string DocumentName
		{
			get { return FDocumentName; }
			set { FDocumentName = (value == null ? String.Empty : value); }
		}
	}

	public class DeriveArgs
	{
		public const string CDefaultPageType = "Browse";
		public const bool CDefaultElaborate = true;

		private string FQuery = String.Empty;
		public string Query
		{
			get { return FQuery; }
			set { FQuery = (value == null ? String.Empty : value); }
		}
		
		private string FPageType = CDefaultPageType;
		public string PageType
		{
			get { return FPageType; }
			set { FPageType = (value == null ? String.Empty : value); }
		}
		
		private string FMasterKeyNames = String.Empty;
		public string MasterKeyNames
		{
			get { return FMasterKeyNames; }
			set { FMasterKeyNames = (value == null ? String.Empty : value); }
		}
		
		private string FDetailKeyNames = String.Empty;
		public string DetailKeyNames
		{
			get { return FDetailKeyNames; }
			set { FDetailKeyNames = (value == null ? String.Empty : value); }
		}
		
		private bool FElaborate = CDefaultElaborate;
		public bool Elaborate
		{
			get { return FElaborate; }
			set { FElaborate = value; }
		}
	}


}