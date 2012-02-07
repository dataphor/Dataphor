/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.Frontend.Client
{
	public class ClientException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 201100: "Unable to find value ({0})."</summary>
			ValueNotFound = 201100,

			/// <summary>Error code 201101: "Unable to find modified row."</summary>
			UnableToFindModifiedRow = 201101,

			/// <summary>Error code 201102: "Cannot perform this operation on a non root node."</summary>
			NotRootNode = 201102,

			/// <summary>Error code 201104: "Node ({0}) not found."</summary>
			NodeNotFound = 201104,

			/// <summary>Error code 201105: "Node must not be active to perform this operation."</summary>
			NodeActive = 201105,

			/// <summary>Error code 201106: "Node must be active to perform this operation."</summary>
			NodeInactive = 201106,

			/// <summary>Error code 201107: "Node ({0}) of type ({1}) is not a valid child of ({2})."</summary>
			InvalidChild = 201107,

			/// <summary>Error code 201108: "Cannot add an active node as a child."</summary>
			CannotAddActiveChild = 201108,

			/// <summary>Error code 201109: "Unable to load type table from file ({0})."</summary>
			UnableToLoadTypeTable = 201109,

			/// <summary>Error code 201110: DEPRECATED</summary>
			OneColumnRequired = 201110,

			/// <summary>Error code 201114: "Interfaces can only contain a single element node.  Use a container as the base element."</summary>
			UseSingleElementNode = 201114,

			/// <summary>Error code 201115: "Interface node can contain only one (1) interface element."</summary>
			MultipleInterfaceNodes = 201115,

			/// <summary>Error code 201116: "MainSource property is not set for interface ({0})."</summary>
			MainSourceRequired = 201116,

			/// <summary>Error code 201117: "StaticText width must be at least one (1)."</summary>
			CharsPerLineInvalid = 201117,

			/// <summary>Error code 201118: "TextBox Height cannot be less than one (1)."</summary>
			HeightMinimum = 201118,

			/// <summary>Error code 201123: "Unsupported Script Language ({0})."</summary>
			UnsupportedScriptLanguage = 201123,

			/// <summary>Error code 201124: "Script compiler error: {0}."</summary>
			ScriptCompilerError = 201124,

			/// <summary>Error code 201125: "The form is already displayed modally."</summary>
			FormAlreadyModal = 201125,

			/// <summary>Error code 201127: "Invalid node name ({0})."</summary>
			InvalidNodeName = 201127,

			/// <summary>Error code 201128: "Choice node ({0}) items must be comma or semicolon separated list of available name-value pairs. (First=1, Second=2)"</summary>
			InvalidChoiceItems = 201128,

//			/// <summary>Error code 201129: Deprecated. </summary>

			/// <summary>Error code 201130: "Invalid argument.  Syntax: &gt;application name&lt;.exe [-alias &gt;alias name&lt;] [-application &gt;application ID&lt;]"</summary>
			InvalidCommandLine = 201130,

			/// <summary>Error code 201133: "The specified notebook page is invalid because it is not a child of this node."</summary>
			InvalidActivePage = 201133,
			
			/// <summary>Error code 201134: "The document returned by the server could not be parsed. Document text: "{0}"."</summary>
			DocumentDeserializationError = 201134,

			/// <summary>Error code 201136: "Cannot perform this operation while the server object is connected."</summary>
			Connected = 201136,

			/// <summary>Error code 201137: "Cannot connect to server.  No server alias specified."</summary>
			NoServerAliasSpecified = 201137,

			/// <summary>Error code 201139: "FormMode ({0}) not supported when there is no MainSource for the form."</summary>
			MainSourceNotSpecified = 201139,

			/// <summary>Error code 201140: "The node types table does not contain required entries.  This usually occurs when 'Frontend' is not included in the library or application requisites."</summary>
			EmptyOrIncompleteNodeTypesTable = 201140,

			/// <summary>Error code 201141: "The specified parent form ({0}) is not found (for modal child)."</summary>
			InvalidParentForm = 201141,

			/// <summary>Error code 201142: "Unable to locate form ({0}) because it is not visible or is disabled by another form."</summary>
			UnableToFindTopmostForm = 201142,
			
			/// <summary>Error code 201143: "Errors occurred while executing the script for ScriptAction "{0}"."</summary>
			ScriptExecutionError = 201143,

			/// <summary>Error code 201145: "Warning: Unable to load windows client settings."</summary>
			ErrorLoadingSettings = 201145,
			
			/// <summary>Error code 201146: "Source must be enabled to perform this operation."</summary>
			SourceNotEnabled = 201146,

			/// <summary>Error code 201147: "Action '{0}' cannot reference itself."</summary>
			RecursiveActionReference = 201147,
			
			/// <summary>Error code 201148: "Expecting only value expressions in the expression list." </summary>
			ValueExpressionExpected = 201148,
			
			/// <summary>Error code 201149: "{0}".</summary>
			Error = 201149,
		}

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.Frontend.Client.ClientException", typeof(ClientException).Assembly);

		// Constructors
		public ClientException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ClientException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ClientException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ClientException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ClientException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ClientException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	}
}
