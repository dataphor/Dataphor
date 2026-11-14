/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Server
{
	public class ServerException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 106100: "Unsupported interface."</summary>
			Unsupported = 106100,

			/// <summary>Error code 106101: "Internal Error: "Invalid server context."</summary>
			InvalidServerContext = 106101,

			/// <summary>Error code 106102: "Server must be "{0}" to perform this operation."</summary>
			InvalidServerState = 106102,

			/// <summary>Error code 106103: "{0}"</summary>
			ServerError = 106103,

			/// <summary>Error code 106104: "Unable to add server to server factory."</summary>
			ServerFactory = 106104,

			/// <summary>Error code 106105: "Server instance name required."</summary>
			ServerInstanceMustBeNamed = 106105,

			/// <summary>Error code 106106: "Session information required."</summary>
			SessionInformationRequired = 106106,

			/// <summary>Error code 106107: "Cannot login as system user."</summary>
			CannotLoginAsSystemUser = 106107,

			/// <summary>Error code 106108: "Cannot drop the system or admin users."</summary>
			CannotDropSystemUsers = 106108,

			/// <summary>Error code 106109: "User "{0}" has open sessions and cannot be dropped."</summary>
			UserHasOpenSessions = 106109,

			/// <summary>Error code 106110: "Invalid password."</summary>
			InvalidPassword = 106110,

			/// <summary>Error code 106111: "An exception occurred during startup of device "{0}"."</summary>
			DeviceStartupError = 106111,

			/// <summary>Error code 106112: "An exception occurred during shutdown of device "{0}"."</summary>
			DeviceShutdownError = 106112,

			/// <summary>Error code 106113: "An exception occurred during startup reconciliation of device "{0}"."</summary>
			StartupReconciliationError = 106113,

			/// <summary>Error code 106114: "Server Plan Exception."</summary> 
			PlanError = 106114,

			/// <summary>Error code 106115: "Server Session Exception."</summary> 
			SessionError = 106115,

			/// <summary>Error code 106116: "Transaction already in progress for this process."</summary> 
			TransactionActive = 106116,

			/// <summary>Error code 106117: "No transaction in progress for this process."</summary> 
			NoTransactionActive = 106117,

			/// <summary>Error code 106118: "Transaction cannot be started with active device sessions on this process."</summary> 
			DeviceSessionsActive = 106118,

			/// <summary>Error code 106119: "Microsoft Distributed Transaction Coordinator support is only available on Windows 2000 or later."</summary> 
			DTCNotSupported = 106119,

			/// <summary>Error code 106120: "No remote session open for server link "{0}"."</summary> 
			NoRemoteSessionForServerLink = 106120,

			/// <summary>Error code 106121: "Cursor "{0}" must be open to perform this operation."</summary> 
			CursorInactive = 106121,

			/// <summary>Error code 106122: "Cursor "{0}" must be closed to perform this operation."</summary> 
			CursorActive = 106122,

			/// <summary>Error code 106123: ""{0}" not supported by this cursor."</summary> 
			CapabilityNotSupported = 106123,

			/// <summary>Error code 106124: "Server Cursor Exception."</summary> 
			CursorError = 106124,

			/// <summary>Error code 106125: "Cannot perform this operation while the DAE server service is started."</summary> 
			ServerIsStarted = 106125,

			/// <summary>Error code 106126: "{0}"</summary> 
			GenericString = 106126,

			/// <summary>Error code 106129: "Server child object container."</summary> 
			ObjectContainer = 106129,

			/// <summary>Error code 106130: "Object "{0}" not found."</summary> 
			ObjectNotFound = 106130,

			/// <summary>Error code 106131: "Server session container."</summary> 
			ServerSessionContainer = 106131,

			/// <summary>Error code 106132: "Server plan container."</summary> 
			ServerPlanContainer = 106132,

			/// <summary>Error code 106133: "Server process container."</summary> 
			ServerProcessContainer = 106133,

			/// <summary>Error code 106134: "Server script container."</summary> 
			ServerScriptContainer = 106134,

			/// <summary>Error code 106135: "Server batch container."</summary> 
			ServerBatchContainer = 106135,

			/// <summary>Error code 106136: "Server cursor container."</summary> 
			ServerCursorContainer = 106136,

			/// <summary>Error code 106140: "Asynchronous message processing not supported by the CLI at this time."</summary>
			AsyncProcessMessageNotSupported = 106140,

			/// <summary>Error code 106141: "Internal Error: "Invalid local server context."</summary>
			InvalidLocalServerContext = 106141,
			
			/// <summary>Error code 106142: "Process is already executing plan "{0}"."</summary>
			PlanExecuting = 106142,
			
			/// <summary>Error code 106143: "Plan "{0}" is not executing on this process."</summary>
			PlanNotExecuting = 106143,
			
			/// <summary>Error code 106144: "Server Process Exception."</summary>
			ProcessError = 106144,
			
			/// <summary>Error code 106145: "Underlying row could not be located."</summary>
			CursorSyncError = 106145,

			/// <summary>Error code 106146: "Exceptions occurred during rollback: {0}."</summary>
			RollbackError = 106146,

			/// <summary>Error code 106147: "Exceptions occurred during commit."</summary>
			CommitError = 106147,
			
			/// <summary>Error code 106148: "Bookmark "{0}" is invalid."</summary>
			InvalidBookmark = 106148,
			
			/// <summary>Error code 106149: "Register class name not found in assembly "{0}"."</summary>
			RegisterClassNameNotFound = 106149,
			
			/// <summary>Error code 106150: "Class alias "{0}" not found."</summary>
			ClassAliasNotFound = 106150,
			
			/// <summary>Error code 106151: "Class "{0}" does not contain a property named "{1}"."</summary>
			PropertyNotFound = 106151,
			
			/// <summary>Error code 106152: "File "{0}" containing registered assemblies does not exist."</summary>
			RegisteredAssembliesFileNotFound = 106152,
			
			/// <summary>Error code 106153: "Process is not currently executing a plan. Compile-time state is not available."</summary>
			NoExecutingPlan = 106153,
			
			/// <summary>Error code 106154: "Exceptions occurred while attempting to load class "{0}"."</summary>
			ClassLoadError = 106154,
			
			/// <summary>Error code 106155: "User "{0}" is not authorized to perform this operation."</summary>
			UnauthorizedUser = 106155,
			
			/// <summary>Error code 106156: "Group "{0}" is not authorized to perform this operation."</summary>
			UnauthorizedGroup = 106156,
			
			/// <summary>Error code 106157: "User "{0}" does not have access to right "{1}"."</summary>
			UnauthorizedRight = 106157,
			
			/// <summary>Error code 106158: "System Group cannot be dropped."</summary>
			CannotDropSystemGroup = 106158,
			
			/// <summary>Error code 106159: "Admin Group cannot be dropped."</summary>
			CannotDropAdminGroup = 106159,
			
			/// <summary>Error code 106160: "User Group cannot be dropped."</summary>
			CannotDropUserGroup = 106160,
			
			/// <summary>Error code 106161: "Group "{0}" has child Groups and cannot be dropped."</summary>
			GroupHasChildGroups = 106161,
			
			/// <summary>Error code 106162: "Group "{0}" has users and cannot be dropped."</summary>
			GroupHasUsers = 106162,
			
			/// <summary>Error code 106163: "Right "{0}" is a generated right and cannot be altered or dropped."</summary>
			CannotDropGeneratedRight = 106163,
			
			/// <summary>Error code 106164: "User "{0}" has owned rights and cannot be dropped."</summary>
			UserOwnsRights = 106164,
			
			/// <summary>Error code 106165: "User "{0}" has owned objects and cannot be dropped."</summary>
			UserOwnsObjects = 106165,
			
			/// <summary>Error code 106166: "Circular Group assignment between Group "{0}" and Group "{1}"."</summary>
			CircularGroupAssignment = 106166,
			
			/// <summary>Error code 106167: "Process request timed out because the server is too busy."</summary>
			ProcessWaitTimeout = 106167,
			
			/// <summary>Error code 106168: "Process did not respond to the terminate request within the termination timeout."</summary>
			ProcessNotResponding = 106168,
			
			/// <summary>Error code 106169: "Role "{0}" has users assigned and cannot be dropped."</summary>
			RoleHasUsers = 106169,
			
			/// <summary>Error code 106170: "Role "{0}" has groups assigned and cannot be dropped."</summary>
			RoleHasGroups = 106170,
			
			/// <summary>Error code 106171: "Returning a value of type "{0}" through the CLI is unimplemented."</summary>
			UnimplementedValueType = 106171,
			
			/// <summary>Error code 106172: "This beta version of Dataphor has expired."</summary>
			BetaExpired = 106172,

			/// <summary>Error code 106173: "Unable to establish a connection with server "{0}"."</summary>
			UnableToConnectToServer = 106173,
			
			/// <summary>Error code 106174: "Unable to execute the plan because it was not successfully compiled.\r\n{0}"</summary>
			UncompiledPlan = 106174,
			
			/// <summary>Error code 106175: "Unable to compile the script because it was not successfully parsed.\r\n{0}"</summary>
			UnparsedScript = 106175,
			
			/// <summary>Error code 106176: "Process ({0}) not found."</summary>
			ProcessNotFound = 106176,
			
			/// <summary>Error code 106177: "Execution did not complete with the specified timeout."</summary>
			ExecutionTimeout = 106177,
			
			/// <summary>Error code 106178: "A cursor is already open on this plan."</summary>
			PlanCursorActive = 106178,

			/// <summary>Error code 106179: "Session ({0}) not found."</summary>
			SessionNotFound = 106179,
			
			/// <summary>Error code 106180: "Remote server has been unexpectedly terminated."</summary>
			InvalidServerInstanceID = 106180,
			
			/// <summary>Error code 106181: "Server connection container."</summary> 
			ServerConnectionContainer = 106181,
			
			/// <summary>Error code 106182: "Timed out waiting for the client-side catalog cache lock."</summary>
			CacheLockTimeout = 106182,
			
			/// <summary>Error code 106183: "Internal cache serialization error: Expected client cache time stamp ({0}), Actual: ({1})."</summary>
			CacheSerializationError = 106183,
			
			/// <summary>Error code 106184: "Cache serialization timeout."</summary>
			CacheSerializationTimeout = 106184,
			
			/// <summary>Error code 106185: "Errors occurred while deserializing client cache time stamp ({0})."</summary>
			CacheDeserializationError = 106185,
			
			/// <summary>Error code 106186: "Process aborted."</summary>
			ProcessAborted = 106186,
			
			/// <summary>Error code 106187: "Check table for table "{0}" could not be created."</summary>
			CouldNotCreateCheckTable = 106187,
			
			/// <summary>Error code 106188: "Instance configuration has not been loaded."</summary>
			InstanceConfigurationNotLoaded = 106188,
			
			/// <summary>Error code 106189: "Instance "{0}" not found."</summary>
			InstanceNotFound = 106189,
			
			/// <summary>Error code 106190: "Unable to connect to listener on "{0}"."</summary>
			UnableToConnectToListener = 106190,
			
			/// <summary>Error code 106191: "Only objects of type "{0}" may be added to this container."</summary>
			TypedObjectContainer = 106191,
			
			/// <summary>Error code 106192: "The debugger must be paused in order to perform this operation."</summary>
			DebuggerRunning = 106192,
			
			/// <summary>Error code 106193: "There is no debugger associated with session ({0})."</summary>
			DebuggerNotStarted = 106193,
			
			/// <summary>Error code 106194: "There is already a debugger started for session ({0})."</summary>
			DebuggerAlreadyStarted = 106194,
			
			/// <summary>Error code 106195: "There is already a debugger attached to process ({0})."</summary>
			DebuggerAlreadyAttached = 106195,
			
			/// <summary>Error code 106196: "There is already a debugger attached to session ({0})."</summary>
			DebuggerAlreadyAttachedToSession = 106196,
			
			/// <summary>Error code 106197: "The debugger cannot be attached to the debugger session ({0})."</summary>
			CannotAttachToDebuggerSession = 106197,
			
			/// <summary>Error code 106198: "Could not determine the current location for process ({0})."</summary>
			CouldNotDetermineProcessLocation = 106198,
			
			/// <summary>Error code 106199: "Process is not currently executing a program. Run-time state is not available."</summary>
			NoExecutingProgram = 106199,
			
			/// <summary>Error code 106200: "Program "{0}" is not executing on this process."</summary>
			ProgramNotExecuting = 106200,
			
			/// <summary>Error code 106201: "Could not determine the current location for program {0}."</summary>
			CouldNotDetermineProgramLocation = 106201,
			
			/// <summary>Error code 106202: "Invalid debug locator: "{0}". Only program and operator locators may be used to obtain program context."</summary>
			InvalidDebugLocator = 106202,
			
			/// <summary>Error code 106203: "Program {0} not found. Only running programs in processes currently attached to the debugger are reachable."</summary>
			ProgramNotFound = 106203,
			
			/// <summary>Error code 106204: "The debugger cannot be attached to an in-process session."</summary>
			CannotAttachToAnInProcessSession = 106204,
			
			/// <summary>Error code 106205: "Unknown object handle."</summary>
			UnknownObjectHandle = 106205,
			
			/// <summary>Error code 106206: "Plan "{0}" not found."</summary>
			PlanNotFound = 106206,
			
			/// <summary>Error code 106207: "Server connection must be inactive to perform this operation."</summary>
			ServerActive = 106207,
			
			/// <summary>Error code 106208: "Server connection must be active to perform this operation."</summary>
			ServerInactive = 106208,
			
			/// <summary>Error code 106209: "Service must be inactive to perform this operation."</summary>
			ServiceActive = 106209,
			
			/// <summary>Error code 106210: "Service must be active to perform this operation."</summary>
			ServiceInactive = 106210,

			/// <summary>Error code 106211: "An error occurred attempting to communicate with the service."</summary>
			CommunicationFailure = 106211,
			
		}
		
		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Server.ServerException", typeof(ServerException).Assembly);

		// Constructors
		public ServerException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public ServerException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public ServerException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public ServerException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public ServerException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public ServerException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public ServerException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}