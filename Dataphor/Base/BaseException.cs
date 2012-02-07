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

			/// <summary>Error code 100115: "Duplicate value inserted ({0})."</summary>
			Duplicate = 100115,

			/// <summary>Error code 100116: "List index out of bounds ({0}).  Valid range is (0-{1})."</summary>
			IndexOutOfBounds = 100116,

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

			/// <summary>Error code 100124: "Stack empty."</summary>
			StackEmpty = 100124,

			/// <summary>Error code 100125: "Maximum stack depth ({0}) has been exceeded."</summary>
			StackOverflow = 100125,
			
			/// <summary>Error code 100126: "Stack index out of range ({0})."</summary>
			InvalidStackIndex = 100126,
			
			/// <summary>Error code 100127: "Stack depth ({0}) already exceeds new setting ({1})."</summary>
			StackDepthExceedsNewSetting = 100127,
			
			/// <summary>Error code 100128: "Call depth ({0}) alread exceeds new setting ({1})."</summary>
			CallDepthExceedsNewSetting = 100128,
			
			/// <summary>Error code 100129: "Maximum call stack depth ({0}) has been exceeded."</summary>
			CallStackOverflow = 100129,

			/// <summary>Error code 100130: "Cannot convert a null reference."</summary>
			CannotConvertNull = 100130,

			/// <summary>Error code 100131: "Cannot convert type ({0}) from string."</summary>
			CannotConvertFromString = 100131,

			/// <summary>Error code 100132: "Cannot convert to string from type ({0})."</summary>
			CannotConvertToString = 100132,
			
			/// <summary>Error code 100133: "Class ({0}) not found."</summary>
			ClassNotFound = 100133,
		};

		// Resource manager for this exception class
		private static ResourceManager _resourceManager = new ResourceManager("Alphora.Dataphor.BaseException", typeof(BaseException).Assembly);

		// Default constructor
		public BaseException(Codes errorCode) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, null) {}
		public BaseException(Codes errorCode, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, null, paramsValue) {}
		public BaseException(Codes errorCode, Exception innerException) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, null) {}
		public BaseException(Codes errorCode, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, ErrorSeverity.Application, innerException, paramsValue) {}
		public BaseException(Codes errorCode, ErrorSeverity severity) : base(_resourceManager, (int)errorCode, severity, null, null) {}
		public BaseException(Codes errorCode, ErrorSeverity severity, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, null, paramsValue) {}
		public BaseException(Codes errorCode, ErrorSeverity severity, Exception innerException) : base(_resourceManager, (int)errorCode, severity, innerException, null) {}
		public BaseException(Codes errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(_resourceManager, (int)errorCode, severity, innerException, paramsValue) {}
	    #if !SILVERLIGHT // SerializationInfo
		public BaseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) {}
		#endif
		
		public BaseException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}

	public class AbortException : Exception
	{
		public AbortException() : base() {}
	}

	public class AggregateException : DataphorException
	{
		public AggregateException(ErrorList errors) : this(errors, ErrorSeverity.Application, null) { }
		public AggregateException(ErrorList errors, ErrorSeverity severity) : this(errors, severity, null) { }
		public AggregateException(ErrorList errors, ErrorSeverity severity, Exception innerException) : base(severity, DataphorException.ApplicationError, errors.ToString(), innerException)
		{
			_errors = errors;
		}

		private ErrorList _errors;
		public ErrorList Errors { get { return _errors; } }
	}
}
