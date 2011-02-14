/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.Dataphoria
{
	[Serializable]
	public partial class DataphoriaException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 124107: "Unable to rename node.  A node named '{0}' already exists."</summary>
			InvalidRename = 124107,

			/// <summary>Error code 124109: "The name is an invalid identifier or a reserved word."</summary>
			InvalidIdentifierName = 124109,

			/// <summary>Error code 124110: "Unable to register Window Message ({0})."</summary>
			CannotRegisterMessage = 124110,

			/// <summary>Error code 124111: "Document type ({0}) is not defined, or there is no default designer specified for it."</summary>
			NoDefaultDesignerForDocumentType = 124111,

			/// <summary>Error code 124112: "There are no designers associated with document type ({0})."</summary>
			NoDesignersForDocumentType = 124112,

			/// <summary>Error code 124113: "Cannot customize this form because it has no document name associated with it (it have been saved as a file)."</summary>
			CannotCustomizeDocument = 124113,

			/// <summary>Error code 124114: "Live edits are only allowed for expressions that load documents."</summary>
			CanOnlyLiveEditDocuments = 124114,

			/// <summary>Error code 124115: "Invalid XML document: {0}"</summary>
			InvalidXMLDocument = 124115,

			/// <summary>Error code 124116: "Document type ({0}) cannot be edited using a live designer."</summary>
			DocumentTypeLiveEditNotSupported = 124116,

			/// <summary>Error code 124117: "Unable to open help file ({0}).  It may have not been installed."</summary>
			UnableToOpenHelp = 124117,

			/// <summary>Error code 124118: "'{0}' is already being designed."</summary>
			AlreadyDesigning = 124118,

			/// <summary>Error code 124120: "Only one designer is allowed to be open on a form."</summary>
			SingleDesigner = 124120,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.Dataphoria.DataphoriaException", typeof(DataphoriaException).Assembly);

		// Constructors
		public DataphoriaException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public DataphoriaException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public DataphoriaException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public DataphoriaException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public DataphoriaException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public DataphoriaException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public DataphoriaException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public DataphoriaException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		public DataphoriaException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}

		public static string FullDescription(System.Exception exception)
		{
			return ExceptionUtility.DetailedDescription(exception);
		}
	}
}
