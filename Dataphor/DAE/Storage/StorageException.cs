/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

namespace Alphora.Dataphor.DAE.Storage
{
	[Serializable]
	public class StorageException : DAEException
	{
		public enum Codes : int
		{
			/// <summary>Error code 116100: "The file type token in the file ('{0}') is not correct."</summary>
			FileTypeTokenMismatch = 116100,

			/// <summary>Error code 116101: "The page size ({0}) of the file '{1}' differs from the configured page size ({2})."</summary>
			FileVersionNewer = 116101,

			/// <summary>Error code 116102: "The page size ({0}) of the file ('{1}') is invalid (below minimum of [{2}])."</summary>
			FilePageSizeBelowMinimum = 116102,

			/// <summary>Error code 116103: "A file for the requested file number ({0}) does not exist."</summary>
			FileNumberNotFound = 116103,

			/// <summary>Error code 116104: "Cannot create a new datafile ('{0}'), this file already exists and contains data."</summary>
			FileCreateNotEmpty = 116104,

			/// <summary>Error code 116105: "Cannot perform operation.  File ('{0}') is not open."</summary>
			FileNotOpen = 116105,

			/// <summary>Error code 116106: "File ('{0}') not found."</summary>
			FileNotFound = 116106,

			/// <summary>Error code 116107: "Path not found for file '{0}'."</summary>
			PathNotFound = 116107,

			/// <summary>Error code 116108: "Unauthorized to access file ('{0}')."</summary>
			UnauthorizedAccess = 116108,

			/// <summary>Error code 116109: "File ('{0}') cannot be opened because it is already in use."</summary>
			SharingViolation = 116109,

			/// <summary>Error code 116110: "File ('{0}') cannot be created because it already exists."</summary>
			FileAlreadyExists = 116110,

			/// <summary>Error code 116111: "The specified path ('{0}') is too long."</summary>
			PathToLong = 116111,

			/// <summary>Error code 116112: "Error occurred while performing operation on file ('{0}')."</summary>
			GeneralIOError = 116112,

			/// <summary>Error code 116113: "Invalid File Number ({0}) found for file ({1}).  Expecting file number {2}." </summary>
			InvalidFileNumber = 116113,

			/// <summary>Error code 116114: "The file ('{0}') is not part of the file group." </summary>
			InvalidFileGroupID = 116114,

			/// <summary>Error code 116115: "The page size ({0}) of the file ('{1}') is invalid (must be a proper power of 2)."</summary>
			FilePageSizeInvalid = 116115,

			/// <summary>Error code 116116: "Invalid Buffer Sizes Specified: The GrowBy value must be greater than 0."</summary>
			GrowByMustBeGreaterThanZero = 116116,

			/// <summary>Error code 116117: "The '{0}' setting must be at least {1}."</summary>
			MinimumViolation = 116117,
		}

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.DAE.Storage.StorageException", typeof(StorageException).Assembly);

		// Constructors
		public StorageException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public StorageException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public StorageException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public StorageException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public StorageException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public StorageException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public StorageException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public StorageException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public StorageException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}
}