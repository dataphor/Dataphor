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
		public DocumentExpression(string expression)
		{
			Expression = expression;
		}

		public DocumentExpression() {}

		private DocumentType _type = DocumentType.None;
		public DocumentType Type
		{
			get	{ return _type;	}
			set 
			{ 
				if (_type != value)
				{
					_otherExpression = null;
					_documentArgs = null;
					_deriveArgs = null;
					_type = value;
					switch (value)       
					{         
						case DocumentType.Document:
							_documentArgs = new DocumentArgs();
							break;
						case DocumentType.Derive:
							_deriveArgs = new DeriveArgs();
							break; 
						case DocumentType.Other: 
							_otherExpression = String.Empty;
							break;
					}
				}
			}
		}

		private DocumentArgs _documentArgs;
		public DocumentArgs DocumentArgs 
		{ 
			get { return _documentArgs; } 
		}
		
		private DeriveArgs _deriveArgs;
		public DeriveArgs DeriveArgs
		{
			get { return _deriveArgs; }
		}	

		private string _otherExpression;
		
		public string Expression
		{
			get
			{
				switch (_type)
				{
					case DocumentType.Derive :
						CallExpression callExpression = 
							new CallExpression
							(
								"Derive", 
								new Expression[] 
								{
									new ValueExpression(_deriveArgs.Query)
								}
							);
						if (_deriveArgs.PageType != String.Empty)
							callExpression.Expressions.Add(new ValueExpression(_deriveArgs.PageType));
						if ((_deriveArgs.MasterKeyNames != String.Empty) && (_deriveArgs.DetailKeyNames != String.Empty))
						{
							callExpression.Expressions.Add(new ValueExpression(_deriveArgs.MasterKeyNames));
							callExpression.Expressions.Add(new ValueExpression(_deriveArgs.DetailKeyNames));
						}
						if (_deriveArgs.Elaborate != DeriveArgs.DefaultElaborate)
							callExpression.Expressions.Add(new ValueExpression(_deriveArgs.Elaborate));

						return new Alphora.Dataphor.DAE.Language.D4.D4TextEmitter().Emit(callExpression);	
					case DocumentType.Document :
						return 
							new Alphora.Dataphor.DAE.Language.D4.D4TextEmitter().Emit
							(
								new CallExpression
								(
									_documentArgs.OperatorName, 
									new Expression[]
									{
										new ValueExpression(_documentArgs.LibraryName), 
										new ValueExpression(_documentArgs.DocumentName)
									}
								)
							);
					case DocumentType.Other :
						return _otherExpression;
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
					Expression parsedExpression;
					CallExpression callExpression;
				
					Alphora.Dataphor.DAE.Language.D4.Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
					parsedExpression = parser.ParseExpression(value);
					
					//check to see if its a qulifier expression first				
					if (parsedExpression is QualifierExpression)
					{
						QualifierExpression qualifierExpression;
						qualifierExpression = parsedExpression as QualifierExpression;
						if ((((IdentifierExpression)qualifierExpression.LeftExpression).Identifier) == "Frontend" || 
							(((IdentifierExpression)qualifierExpression.LeftExpression).Identifier) == ".Frontend")
						{
							callExpression = qualifierExpression.RightExpression as CallExpression;
						}
						else 
							callExpression = parsedExpression as CallExpression;
					}
					else 
						callExpression = parsedExpression as CallExpression;
					
					if (callExpression == null)	//if the LCallExpression is null then we don't know what it is - it is OTHER
					{
						Type = DocumentType.Other;
						_otherExpression = value;
					}
					else if (IsDocumentIdentifier(callExpression.Identifier))  //Check If the parsed expression is a document and assign FDocumentArgs
					{
						Type = DocumentType.Document;
						_documentArgs.OperatorName = callExpression.Identifier;
						_documentArgs.LibraryName = ((ValueExpression)callExpression.Expressions[0]).Value.ToString();
						_documentArgs.DocumentName = ((ValueExpression)callExpression.Expressions[1]).Value.ToString();
					}
					else if ((callExpression.Identifier) == "Derive")  //Check if the parsed expression is a Derive call and assign FDeriveArgs
					{
						Type = DocumentType.Derive;
						_deriveArgs.Query = ((ValueExpression)callExpression.Expressions[0]).Value.ToString();
						if (callExpression.Expressions.Count >= 2)
						{
							if 
							(
								((ValueExpression)callExpression.Expressions[1]).Value.ToString() != "True" || 
								((ValueExpression)callExpression.Expressions[1]).Value.ToString() != "False"
							)
							{
								_deriveArgs.PageType = ((ValueExpression)callExpression.Expressions[1]).Value.ToString();
								if (callExpression.Expressions.Count == 3)
									_deriveArgs.Elaborate = (bool)((ValueExpression)callExpression.Expressions[2]).Value;
								else if (callExpression.Expressions.Count >= 4)
								{
									_deriveArgs.MasterKeyNames = ((ValueExpression)callExpression.Expressions[2]).Value.ToString();
									_deriveArgs.DetailKeyNames = ((ValueExpression)callExpression.Expressions[3]).Value.ToString();
									if (callExpression.Expressions.Count == 5)
										_deriveArgs.Elaborate = (bool)((ValueExpression)callExpression.Expressions[4]).Value;
								}
							}
							else
								_deriveArgs.Elaborate = (bool)((ValueExpression)callExpression.Expressions[1]).Value;
						}
					}
					else
					{
						Type = DocumentType.Other;
						_otherExpression = value;
					}					
				}
			}
		}	

		private bool IsDocumentIdentifier(string identifier)
		{
			switch (identifier)
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
		public const string DefaultOperatorName = "Load";

		private string _operatorName = DefaultOperatorName;
		public string OperatorName
		{
			get { return _operatorName; }
			set { _operatorName = (value == null ? String.Empty : value); }
		}
		
		private string _libraryName = String.Empty;
		public string LibraryName
		{
			get { return _libraryName; }
			set { _libraryName = (value == null ? String.Empty : value); }
		}

		private string _documentName = String.Empty;
		public string DocumentName
		{
			get { return _documentName; }
			set { _documentName = (value == null ? String.Empty : value); }
		}
	}

	public class DeriveArgs
	{
		public const string DefaultPageType = "Browse";
		public const bool DefaultElaborate = true;

		private string _query = String.Empty;
		public string Query
		{
			get { return _query; }
			set { _query = (value == null ? String.Empty : value); }
		}
		
		private string _pageType = DefaultPageType;
		public string PageType
		{
			get { return _pageType; }
			set { _pageType = (value == null ? String.Empty : value); }
		}
		
		private string _masterKeyNames = String.Empty;
		public string MasterKeyNames
		{
			get { return _masterKeyNames; }
			set { _masterKeyNames = (value == null ? String.Empty : value); }
		}
		
		private string _detailKeyNames = String.Empty;
		public string DetailKeyNames
		{
			get { return _detailKeyNames; }
			set { _detailKeyNames = (value == null ? String.Empty : value); }
		}
		
		private bool _elaborate = DefaultElaborate;
		public bool Elaborate
		{
			get { return _elaborate; }
			set { _elaborate = value; }
		}
	}


}