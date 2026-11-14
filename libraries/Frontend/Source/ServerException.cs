/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.Frontend.Server
{
	[Serializable]
	public class ServerException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 200100: "Expression not prepared."</summary>
			ExpressionNotPrepared = 200100,

			/// <summary>Error code 200101: "Expression data for this page is unavailable."</summary>
			ExpressionDataNotAvailable = 200101,

			/// <summary>Error code 200102: "Unable to retrieve expression data for this page."</summary>
			UnableToRetrieveExpressionData = 200102,

			/// <summary>Error code 200103: "CGI Query parameter "{0}" expected."</summary>
			MissingParameter = 200103,

			/// <summary>Error code 200104: "Group "{0}" not found."</summary>
			GroupNotFound = 200104,

			/// <summary>Error code 200105: "No current group available."</summary>
			NoCurrentGroup = 200105,

			/// <summary>Error code 200106: "Main table already set for this expression."</summary>
			MainTableSet = 200106,

			/// <summary>Error code 200107: "Main table required."</summary>
			MainTableRequired = 200107,

			/// <summary>Error code 200108: "Elaborated table "{0}" not found."</summary>
			ElaboratedTableNotFound = 200108,

			/// <summary>Error code 200109: "Internal Error: Invalid reference "{0}" encountered while deriving expression from table "{1}"."</summary>
			InvalidReferenceEncountered = 200109,

			/// <summary>Error code 200110: "Internal Error: Elaborated reference is already set for derived table "{0}"."</summary>
			ElaboratedReferenceSet = 200110,
			
			/// <summary>Error code 200111: "Internal Error: Embedded flag is already set for derived table "{0}"."</summary>
			EmbeddedSet = 200111,
			
			/// <summary>Error code 200112: "Invalid XML document node "{0}"."</summary>
			InvalidNode = 200112,
			
			/// <summary>Error code 200115: "Unknown form type "{0}".  Form types are case sensitive."</summary>
			UnknownPageType = 200115,
			
			/// <summary>Error code 200116: "Table expression expected."</summary>
			TableExpressionExpected = 200116,
			
			/// <summary>Error code 200118: "Unable to process file with given extension "{0}"."</summary>
			UnknownExtension = 200118,
			
			/// <summary>Error code 200119: "Unable to start or connect to DAE Server."</summary>
			DatabaseUnreachable = 200119,
			
			/// <summary>Error code 200120: "Unqualified group name "{0}" is not a valid sub group of "{1}"."</summary>
			InvalidGrouping = 200120,
			
			/// <summary>Error code 200121: "Element name required."</summary>
			ElementNameRequired = 200121,
			
			/// <summary>Error code 200122: "Duplicate element name "{0}"."</summary>
			DuplicateElementName = 200122,
			
			/// <summary>Error code 200123: "Null element may not be added to the list."</summary>
			ElementRequired = 200123,

			/// <summary>Error code 200124: "The web.config file for the Frontend Server must have the FormsAuthenticationLoginUrl AppSetting set."</summary>
			MustHaveFormsAuthenticationLoginUrl = 200124,
	
			/// <summary>Error code 200125: "Invalid XML Document: {0}"</summary>
			InvalidXMLDocument = 200125,

			/// <summary>Error code 200126: "Cannot change the document type."</summary>
			CannotChangeDocumentType = 200126,
			
			/// <summary>Error code 200127: "Elaborated column "{0}" not found."</summary>
			ColumnNotFound = 200127,
			
			/// <summary>Error code 200128: "Cannot construct a search column for column "{0}" of "{1}"."</summary>
			CannotConstructSearchColumn
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Server.ServerException", typeof(ServerException).Assembly);

		// Constructors
		public ServerException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ServerException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ServerException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ServerException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ServerException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public ServerException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
	}
}
