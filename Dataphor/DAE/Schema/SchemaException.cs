/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.DAE.Schema
{
	public class SchemaException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 113100: "Object "{0}" cannot be copied into a remote catalog."</summary>
			ObjectCannotBeCopied = 113100,

			/// <summary>Error code 113101: "Statement cannot be emitted for schema objects of type "{0}"."</summary>
			StatementCannotBeEmitted = 113101,

			/// <summary>Error code 113102: "Object reference cannot be null."</summary>
			ReferenceCannotBeNull = 113102,

			/// <summary>Error code 113103: "Schema object container."</summary>
			ObjectContainer = 113103,

			/// <summary>Error code 113104: "Object "{0}" not found."</summary>
			ObjectNotFound = 113104,

			/// <summary>Error code 113105: "Object name required."</summary>
			ObjectNameRequired = 113105,

			/// <summary>Error code 113106: "Duplicate object name "{0}"."</summary>
			DuplicateObjectName = 113106,

			/// <summary>Error code 113107: "Duplicate object identifier "{0}"."</summary>
			DuplicateObjectID = 113107,

			/// <summary>Error code 113108: "Object reference "{0}" is ambiguous between the following names: {1}."</summary>
			AmbiguousObjectReference = 113108,

			/// <summary>Error code 113109: "Internal Error: Name Index Bucket not found for name "{0}"."</summary>
			IndexBucketNotFound = 113109,

			/// <summary>Error code 113110: "Constraint container."</summary>
			ConstraintContainer = 113110,

			/// <summary>Error code 113111: "Duplicate child object "{0}"."</summary>
			DuplicateChildObjectName = 113111,

			/// <summary>Error code 113113: "Type specifier cannot be emitted for object class "{0}"."</summary>
			TypeSpecifierCannotBeEmitted = 113113,

			/// <summary>Error code 113114: "Property container."</summary>
			PropertyContainer = 113114,

			/// <summary>Error code 113115: "Representation container."</summary>
			RepresentationContainer = 113115,

			/// <summary>Error code 113116: "Special container."</summary>
			SpecialContainer = 113116,

			/// <summary>Error code 113117: "ScalarType container."</summary>
			ScalarTypeContainer = 113117,

			/// <summary>Error code 113118: "Column container."</summary>
			ColumnContainer = 113118,

			/// <summary>Error code 113119: "Order columns list may only contain order columns."</summary>
			OrderColumnContainer = 113119,

			/// <summary>Error code 113120: "Duplicate order column definition "{0}"."</summary>
			DuplicateOrderColumnDefinition = 113120,

			/// <summary>Error code 113121: "EmptyOrder."</summary>
			Empty = 113121,

			/// <summary>Error code 113122: "Order list may only contain orders."</summary>
			OrderContainer = 113122,

			/// <summary>Error code 113123: "Key columns list may only contain references to table columns."</summary>
			KeyColumnContainer = 113123,

			/// <summary>Error code 113124: "Duplicate key column name "{0}"."</summary>
			DuplicateColumnName = 113124,

			/// <summary>Error code 113125: "Key list may only contain keys."</summary>
			KeyContainer = 113125,

			/// <summary>Error code 113126: "Table "{0}" does not have any keys from which to select a minimum."</summary>
			NoKeysAvailable = 113126,

			/// <summary>Error code 113127: "No key is a subset of the given columns."</summary>
			NoSubsetKeyAvailable = 113127,

			/// <summary>Error code 113128: "Reference container."</summary>
			ReferenceContainer = 113128,

			/// <summary>Error code 113129: "Operand container."</summary>
			OperandContainer = 113129,

			/// <summary>Error code 113130: "Cannot insert into a sorted list."</summary>
			SortedList = 113130,

			/// <summary>Error code 113131: "Signature "{0}" not found."</summary>
			SignatureNotFound = 113131,

			/// <summary>Error code 113132: "OperatorSignature container."</summary>
			OperatorSignatureContainer = 113132,

			/// <summary>Error code 113133: "Duplicate operator signature "{0}"."</summary>
			DuplicateOperatorSignature = 113133,

			/// <summary>Error code 113134: "Ambiguous inherited invocation in operator "{0}"."</summary>
			AmbiguousInheritedCall = 113134,

			/// <summary>Error code 113135: "Operator map container."</summary>
			OperatorMapContainer = 113135,

			//			/// <summary>Error code 113136: "Signature "{0}" not found."</summary>
			//			SignatureNotFound = 113136,

			/// <summary>Error code 113137: "Signature group not found for operator "{0}"."</summary>
			SignatureGroupNotFound = 113137,

			/// <summary>Error code 113138: "Operator map not found for operator "{0}"."</summary>
			OperatorMapNotFound = 113138,
			
			/// <summary>Error code 113139: "Device scalar type map not found for scalar type "{0}"."</summary>
			DeviceScalarTypeNotFound = 113139,

			/// <summary>Error code 113140: "Device operator map not found for operator "{0}"."</summary>
			DeviceOperatorNotFound = 113140,

			/// <summary>Error code 113141: "{0} not supported by device "{1}"."</summary>
			CapabilityNotSupported = 113141,

			/// <summary>Error code 113142: "Operation cannot be performed because device "{0}" is not running."</summary>
			DeviceNotRunning = 113142,

			/// <summary>Error code 113143: "Operation cannot be performed because device "{0}" is running."</summary>
			DeviceRunning = 113143,

			/// <summary>Error code 113144: "No active transaction."</summary>
			NoActiveTransaction = 113144,

			/// <summary>Error code 113146: "Heap may only contain table variables."</summary>
			TableVarContainer = 113146,

			/// <summary>Error code 113147: "User "{0}" not found."</summary>
			UserNotFound = 113147,

			/// <summary>Error code 113148: "User container may only contain User objects."</summary>
			UserContainer = 113148,

			/// <summary>Error code 113149: "Device user "{0}" not found."</summary>
			DeviceUserNotFound = 113149,

			/// <summary>Error code 113150: "DeviceUser container may only contain DeviceUser objects."</summary>
			DeviceUserContainer = 113150,

			/// <summary>Error code 113151: "ServerLinkUser "{0}" not found."</summary>
			ServerLinkUserNotFound = 113151,

			/// <summary>Error code 113152: "ServerLinkUser container may only contain ServerLinkUser objects."</summary>
			ServerLinkUserContainer = 113152,

			/// <summary>Error code 113153: "Catalog may only contain scalar types, operators, devices, tables, views, presentations, references, or catalog constraints."</summary>
			TopLevelContainer = 113153,

			/// <summary>Error code 113154: "Catalog "{0}": Column "{1}" references unknown scalar type "{2}"."</summary>
			ColumnReferencesUnknownScalarType = 113154,

			/// <summary>Error code 113155: "Catalog "{0}": Reference "{1}" references unknown table "{2}"."</summary>
			ReferenceReferencesUnknownTable = 113155,

			/// <summary>Error code 113156: "Catalog "{0}": Constraint "{1}" references unknown object "{2}"."</summary>
			ConstraintReferencesUnknownObject = 113156,

			/// <summary>Error code 113157: "Catalog "{0}": DerivedTableVar "{1}" references unknown object "{2}"."</summary>
			DerivedTableVarReferencesUnknownObject = 113157,

			/// <summary>Error code 113158: "Catalog "{0}" is inconsistent."</summary>
			CatalogInconsistent = 113158,

			/// <summary>Error code 113159: "Object "{0}" cannot be changed while registered in catalog "{1}"."</summary>
			ObjectRegistered = 113159,

			/// <summary>Error code 113160: "Duplicate operator "{0}"."</summary>
			DuplicateOperator = 113160,

			/// <summary>Error code 113161: "Unable to resolve operator reference "{0}" with signature "{1}"."</summary>
			OperatorNotFound = 113161,

			/// <summary>Error code 113162: "Tag name required."</summary>
			TagNameRequired = 113162,

			/// <summary>Error code 113163: "Tag container."</summary>
			TagContainer = 113163,

			/// <summary>Error code 113164: "Tag "{0}" not found."</summary>
			TagNotFound = 113164,

			/// <summary>Error code 113165: "Duplicate tag name: "{0}"."</summary>
			DuplicateTagName = 113165,
		
			/// <summary>Error code 113166: "Ambiguous tag name: "{0}"."</summary>
			AmbiguousTagReference = 113166,

			/// <summary>Error code 113167: "Unimplemented: {0}."</summary>
			UnimplementedMethod = 113167,
			
			/// <summary>Error code 113168: "Event handler container."</summary>
			EventHandlerContainer = 113168,
			
			/// <summary>Error code 113169: "Operator "{0}" is already attached to event type "{1}" for this object."</summary>
			DuplicateEventHandler = 113169,

			/// <summary>Error code 113170: "Encrypted data exceeds maximum length (255)."</summary>
			EncryptedDataTooLong = 113170,
			
			/// <summary>Error code 113171: "Index ({0}) out of range."</summary>
			IndexOutOfRange = 113171,

			/// <summary>Error code 113172: "Invalid Connection String."</summary>
			InvalidConnectionString = 113172,
			
			/// <summary>Error code 113173: "No generator table defined."</summary>
			NoGeneratorTable = 113173,
			
			/// <summary>Error code 113174: "Object name "{0}" cannot be used because it would hide the name "{1}"."</summary>
			CreatingAmbiguousObjectName = 113174,
			
			/// <summary>Error code 113175: "Object name "{0}" cannot be used because it would be hidden by the name "{1}".</summary>
			AmbiguousObjectName = 113175,
			
			/// <summary>Error code 113176: "{0} container."</summary>
			InvalidContainer = 113176,
			
			/// <summary>Error code 113177: "Catalog object expected."</summary>
			CatalogObjectExpected = 113177,
			
			/// <summary>Error code 113178: "Right "{0}" not found."</summary>
			RightNotFound = 113178,
			
			/// <summary>Error code 113179: "Right assignment for right "{0}" not found."</summary>
			RightAssignmentNotFound = 113179,
			
			/// <summary>Error code 113180: "Group "{1}" not found." </summary>
			GroupNotFound = 113180,

			/// <summary>Error code 113181: "Group device user not found for Group "{0}", device "{1}"."</summary>
			GroupDeviceUserNotFound = 113181,

			/// <summary>Error code 113182: "GroupDeviceUser container may only contain GroupDeviceUser objects."</summary>
			GroupDeviceUserContainer = 113182,

			/// <summary>Error code 113183: "Column "{0}" not found."</summary>
			ColumnNotFound = 113183,
			
			/// <summary>Error code 113184: "Role "{0}" not found."</summary>
			RoleNotFound = 113184,
			
			/// <summary>Error code 113185: "Constraint "{0}" is not a transition constraint."</summary>
			ConstraintIsNotTransitionConstraint = 113185,
			
			/// <summary>Error code 113186: "Constraint "{0}" is a transition constraint."</summary>
			ConstraintIsTransitionConstraint = 113186,
			
			/// <summary>Error code 113187: "Library "{0}" is already registered."</summary>
			LibraryAlreadyRegistered = 113187,
			
			/// <summary>Error code 113188: "Library "{0}" cannot be unregistered because there are registered libraries that require it."</summary>
			LibraryIsRequired = 113188,
			
			/// <summary>Error code 113189: "Load library can only be called while loading catalog."</summary>
			InvalidLoadLibraryCall = 113189,
			
			/// <summary>Error code 113190: "Library "{0}" is already loaded."</summary>
			LibraryAlreadyLoaded = 113190,
			
			/// <summary>Error code 113191: "Library "{0}" cannot be loaded because required library "{1}" is not loaded."</summary>
			RequiredLibraryNotLoaded = 113191,
			
			/// <summary>Error code 113192: "Drop statement cannot be emitted for schema object of type "{0}"."</summary>
			DropStatementCannotBeEmitted = 113192,
			
			/// <summary>Error code 113193: "Library "{0}" cannot be registered because it would nested within library "{1}"."</summary>
			NestedLibraryCreation = 113193,
			
			/// <summary>Error code 113194: "Object "{0}" in library "{1}" cannot reference object "{2}" because it is not contained within a library."</summary>
			NonLibraryDependency = 113194,
			
			/// <summary>Error code 113195: "Object "{0}" in library "{1}" cannot reference object "{2}" in library "{3}" because the referenced library is not required by the referencing library."</summary>
			NonRequiredLibraryDependency = 113195,
			
			/// <summary>Error code 113196: "Library "{0}" is registered and cannot be dropped."</summary>
			CannotDropRegisteredLibrary = 113196,
			
			/// <summary>Error code 113197: "Library "{0}" is registered and cannot be renamed."</summary>
			CannotRenameRegisteredLibrary = 113197,
			
			/// <summary>Error code 113198: "Library "{0}" is registered and cannot have requisites removed."</summary>
			CannotRemoveRequisitesFromRegisteredLibrary = 113198,
			
			/// <summary>Error code 113199: "Library "{0}" cannot require library "{1}" because it is a circular reference."</summary>
			CircularLibraryReference = 113199,
			
			/// <summary>Error code 113200: "System library cannot be modified."</summary>
			CannotModifySystemLibrary = 113200,
			
			/// <summary>Error code 113201: "General library cannot be modified."</summary>
			CannotModifyGeneralLibrary = 113201,
			
			/// <summary>Error code 113202: "System library cannot be unregistered."</summary>
			CannotUnregisterSystemLibrary = 113202,
			
			/// <summary>Error code 113203: "General library cannot be unregistered."</summary>
			CannotUnregisterGeneralLibrary = 113203,
			
			/// <summary>Error code 113204: "Current session has no current library defined."</summary>
			NoCurrentLibrary = 113204,
			
			/// <summary>Error code 113205: "Object "{0}" cannot reference object "{1}" because it is a session-specific temporary object."</summary>
			SessionObjectDependency = 113205,
			
			/// <summary>Error code 113206: "Library "{0}" is registered and cannot have its version changed."</summary>
			CannotChangeRegisteredLibraryVersion = 113206,
			
			/// <summary>Error code 113207: "Library "{0}" is required to be version "{1}", but is available as version "{2}"."</summary>
			LibraryVersionMismatch = 113207,
			
			/// <summary>Error code 113208: "A default device name could not be determined for library "{0}" because it has no default device name specified and more than one required library has a default device name specified."</summary>
			AmbiguousDefaultDeviceName = 113208,
			
			/// <summary>Error code 113209: "Registered class "{0}" not found."</summary>
			RegisteredClassNotFound = 113209,
			
			/// <summary>Error code 113210: "Registered class "{0}" cannot be referenced because the current library "{1}" does not require the library in which the class is registered "{2}"."</summary>
			NonRequiredClassDependency = 113210,

			/// <summary>Error code 113211: "Object "{0}" cannot reference object "{1}" because it is an application transaction-specific temporary object."</summary>
			ApplicationTransactionObjectDependency = 113211,
			
			/// <summary>Error code 113212: "Transition constraint "{0}" already has a definition for the insert transition."</summary>
			InsertTransitionExists = 113212,

			/// <summary>Error code 113213: "Transition constraint "{0}" does not have a definition for the insert transition."</summary>
			NoInsertTransition = 113213,

			/// <summary>Error code 113214: "Transition constraint "{0}" already has a definition for the update transition."</summary>
			UpdateTransitionExists = 113214,

			/// <summary>Error code 113215: "Transition constraint "{0}" does not have a definition for the update transition."</summary>
			NoUpdateTransition = 113215,

			/// <summary>Error code 113216: "Transition constraint "{0}" already has a definition for the delete transition."</summary>
			DeleteTransitionExists = 113216,

			/// <summary>Error code 113217: "Transition constraint "{0}" does not have a definition for the delete transition."</summary>
			NoDeleteTransition = 113217,
			
			/// <summary>Error code 113218: "Unable to locate a representation of scalar type "{0}" for use as the native accessor "{1}"."</summary>
			UnableToLocateConversionRepresentation = 113218,
			
			/// <summary>Error code 113219: "Representation "{0}" of scalar type "{1}" cannot be used as a conversion representation because it has multiple properties."</summary>
			InvalidConversionRepresentation = 113219,
			
			/// <summary>Error code 113220: "Table "{0}" cannot be stored in device "{1}" because the device does not have a type map for the type "{2}" of column "{3}"."</summary>
			UnsupportedScalarType = 113220,
			
			/// <summary>Error code 113221: "Table "{0}" cannot be stored in device "{1}" because the device does not support comparison for the type "{2}" of key column "{3}"."</summary>
			UnsupportedKeyType = 113221,
			
			/// <summary>Error code 113222: "Table "{0}" cannot be stored in device "{1}" because the device does not support comparison for the type "{2}" of order column "{3}"."</summary>
			UnsupportedOrderType = 113222,
			
			/// <summary>Error code 113223: "Duplicate operator match for operator "{0}"."</summary>
			DuplicateOperatorMatch = 113223,
			
			/// <summary>Error code 113224: "Column "{0}" in table "{1}" cannot be created not nil because the column does not have a default definition."</summary>
			InvalidAlterTableVarCreateColumnStatement = 113224,
			
			/// <summary>Error code 113225: "Version number of registered library "{0}" cannot be changed because it would invalidate the reference from library "{1}", version number "{2}"."</summary>
			RegisteredLibraryHasDependents = 113225,
			
			/// <summary>Error code 113226: "The version number of library reference "{0}" in library "{1}" cannot be changed to "{2}" because the target library version is "{3}"."</summary>
			InvalidLibraryReference = 113226,
			
			/// <summary>Error code 113227: "Upgrade version number "{0}" must have Major, Minor and Revision specified, and must not have Build specified."</summary>
			InvalidUpgradeVersionNumber = 113227,
			
			/// <summary>Error code 113228: "Upgrades cannot be tracked for library "{0}" because the version number "{1}" does not have Revision specified."</summary>
			InvalidUpgradeLibraryVersionNumber = 113228,
			
			/// <summary>Error code 113229: "Unable to determine a clustering order for table variable "{0}" because it has no non-sparse keys defined."</summary>
			KeyRequired = 113229,
			
			/// <summary>Error code 113230: "Setting "{0}" is defined in at least the following libraries: {1}."</summary>
			AmbiguousLibrarySetting,
			
			/// <summary>Error code 113231: "Library "{0}" is registered and cannot be detached."</summary>
			CannotDetachRegisteredLibrary = 113231,
			
			/// <summary>Error code 113232: "Could not resolve object for ID ({0}) and name "{1}"."</summary>
			CouldNotResolveObjectHeader = 113232,
			
			/// <summary>Error code 113233: "Object header not found for ID ({0})."</summary>
			ObjectHeaderNotFound = 113233,
			
			/// <summary>Error code 113234: "Duplicate object ID ({0})."</summary>
			DuplicateObject = 113234,
			
			/// <summary>Error code 113235: "Name '{0}' exceeds the maximum allowable length for object names in the catalog ({1} characters)."</summary>
			MaxObjectNameLengthExceeded = 113235,
			
			/// <summary>Error code 113236: "A right with the name '{0}' already exists."</summary>
			DuplicateRightName = 113236,
			
			/// <summary>Error code 113237: "A user with the id '{0}' already exists."</summary>
			DuplicateUserID = 113237,
			
			/// <summary>Error code 113238: "A user mapping for user id '{0}' for device '{1}' already exists."</summary>
			DuplicateDeviceUser = 113238,
			
			/// <summary>Error code 113239: "Catalog object header for object id ({0}) does not exist in the catalog store."</summary>
			CatalogObjectHeaderNotFound = 113239,
			
			/// <summary>Error code 113240: "Catalog object header for object id ({0}) was found and the object was successfully deserialized, but the object is not present in the catalog cache."</summary>
			CatalogObjectLoadFailed = 113240,
			
			/// <summary>Error code 113241: "Errors occurred while attempting to deserialize catalog object id ({0})."</summary>
			CatalogDeserializationError = 113241,
			
			/// <summary>Error code 113242: "Library '{0}' is not registered."</summary>
			LibraryNotRegistered = 113242,
			
			/// <summary>Error code 113243: "Object id ({0}) cannot be loaded because there is already an object being loaded on this process."</summary>
			ObjectAlreadyLoading = 113243,
			
			/// <summary>Error code 113244: "Object id ({0}) was not found in the catalog cache."</summary>
			ObjectNotCached = 113244,
			
			/// <summary>Error code 113245: "Cannot enter a nonloading context because the current loading context is not an internal context."</summary>
			InvalidLoadingContext = 113245,
			
			/// <summary>Error code 113246: "Table "{0}" does not have any non-sparse keys from which to select a minimum."</summary>
			NoNonSparseKeysAvailable = 113246,
			
			/// <summary>Error code 113247: "Table "{0}" does not have any non-nilable keys from which to select a minimum."</summary>
			NoNonNilableKeysAvailable = 113247,
			
			/// <summary>Error code 113248: "Errors occurred while attempting to apply device settings for device "{0}", setting "{1}", value "{2}"."</summary>
			ErrorApplyingDeviceSetting = 113248,
		}		

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.DAE.Schema.SchemaException", typeof(SchemaException).Assembly);

		// Constructors
		public SchemaException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public SchemaException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public SchemaException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public SchemaException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public SchemaException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public SchemaException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public SchemaException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public SchemaException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
		
		public SchemaException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}
}