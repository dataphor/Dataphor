/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor
{
	[Serializable]
	public class BaseException : DataphorException
	{
		public enum Codes : int
		{
			/// <summary>Error code 100100: "Unable to set property ({0})."</summary>
			UnableToSetProperty = 100100,

			/// <summary>Error code 100101: "Member ({0}) not found for class ({1}).  Overloaded and indexed properties are not supported."</summary>
			MemberNotFound = 100101,

			/// <summary>Error code 100102: "Multiple attributes found.  Expecting single attribute ({0})."</summary>
			ExpectingSingleAttribute = 100102,

			/// <summary>Error code 100103: "A constructor matching the default constructor signature ({0}) is not found."</summary>
			DefaultConstructorNotFound = 100103,

			/// <summary>Error code 100104: "This list does not allow null items."</summary>
			CannotAddNull = 100104,

			/// <summary>Error code 100105: "Cannot add an item of type ({0}), collection only allows items of type ({1})."</summary>
			CollectionOfType = 100105,

			/// <summary>Error code 100106: "Child cover is read only."</summary>
			ChildCoverIsReadOnly = 100106,

			/// <summary>Error code 100107: "List index ({0}) out of bounds."</summary>
			InvalidListIndex = 100107,

			/// <summary>Error code 100108: "Method is not supported."</summary>
			NotSupported = 100108,

			/// <summary>Error code 100109: "Child ({0}) not found."</summary>
			ChildNotFound = 100109,

			/// <summary>Error code 100110: "Child component must derive from ComponentBase."</summary>
			ChildMustBeComponentBase = 100110,

			/// <summary>Error code 100111: "Parent component may not contain itself."</summary>
			CircularParentReference = 100111,

			/// <summary>Error code 100112: "Child component already referenced by this parent."</summary>
			DuplicateChildReference = 100112,

			/// <summary>Error code 100113: "Object ({0}) is not a container."</summary>
			NotAContainer = 100113,

			/// <summary>Error code 100114: "Component ({0}) not found."</summary>
			ComponentNotFound = 100114,

			/// <summary>Error code 100115: "Duplicate value inserted ({0})."</summary>
			Duplicate = 100115,

			/// <summary>Error code 100116: "List index out of bounds ({0}).  Valid range is (0-{1})."</summary>
			IndexOutOfBounds = 100116,

			/// <summary>Error code 100117: "Cannot perform this operation on a sorted list."</summary>
			SortedListError = 100117,

			/// <summary>Error code 100118: "List cover is read only."</summary>
			ListCoverIsReadOnly = 100118,

			/// <summary>Error code 100119: "Objects cannot be inserted into a hash table, use the Add method instead."</summary>
			InsertNotSupported = 100119,

			/// <summary>Error code 100120: "Object at index ({0}) not found."</summary>
			ObjectAtIndexNotFound = 100120,

			/// <summary>Error code 100121: "Size must be at least two (2)."</summary>
			MinimumSize = 100121,
			
			/// <summary>Error code 100122: "Finalizer invoked."</summary>
			FinalizerInvoked = 100122,

			/// <summary>Error code 100123: "Only 'Exception' based instances can be added to the errors list.  Attempted to add a ({0})."</summary>
			ExceptionsOnly = 100123,
		};

		// Resource manager for this exception class
		private static ResourceManager FResourceManager = new ResourceManager("Alphora.Dataphor.BaseException", typeof(BaseException).Assembly);

		// Default constructor
		public BaseException(Codes AErrorCode) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, null) {}
		public BaseException(Codes AErrorCode, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, null, AParams) {}
		public BaseException(Codes AErrorCode, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, null) {}
		public BaseException(Codes AErrorCode, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ErrorSeverity.Application, AInnerException, AParams) {}
		public BaseException(Codes AErrorCode, ErrorSeverity ASeverity) : base(FResourceManager, (int)AErrorCode, ASeverity, null, null) {}
		public BaseException(Codes AErrorCode, ErrorSeverity ASeverity, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, null, AParams) {}
		public BaseException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, null) {}
		public BaseException(Codes AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(FResourceManager, (int)AErrorCode, ASeverity, AInnerException, AParams) {}
		public BaseException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) {}
	}

	public class AbortException : Exception
	{
		public AbortException() : base() {}
	}

	public class AggregateException : DataphorException
	{
		public AggregateException(ErrorList AErrors) : this(AErrors, ErrorSeverity.Application, null) { }
		public AggregateException(ErrorList AErrors, ErrorSeverity ASeverity) : this(AErrors, ASeverity, null) { }
		public AggregateException(ErrorList AErrors, ErrorSeverity ASeverity, Exception AInnerException) : base(ASeverity, DataphorException.CApplicationError, AErrors.ToString(), AInnerException)
		{
			FErrors = AErrors;
		}

		private ErrorList FErrors;
		public ErrorList Errors { get { return FErrors; } }
	}
}
