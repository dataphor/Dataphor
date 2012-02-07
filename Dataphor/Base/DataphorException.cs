/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.Resources;

namespace Alphora.Dataphor
{
	public enum ErrorSeverity { User, Application, System, Environment }
	
	public class DataphorException : System.Exception
	{
		public const int OR_E_EXCEPTION = -2146233088;
		public const int ApplicationError = 500000;

		public const string MessageNotFound = @"DataphorException: Message ({0}) not found in ""{1}"".";
		public const string ManifestNotFound = @"DataphorException: Message ({0}) Manifest not found for BaseName ""{1}"".";
		
		public DataphorException(string message) : base(message)
		{
			_code = ApplicationError;
			_severity = ErrorSeverity.Application;
			HResult = OR_E_EXCEPTION;
		}
		
		public DataphorException(int errorCode, string message) : base(message)
		{
			_code = errorCode;
			_severity = ErrorSeverity.Application;
			HResult = OR_E_EXCEPTION;
		}
		
		public DataphorException(ErrorSeverity severity, int errorCode, string message) : base(message)
		{
			_code = errorCode;
			_severity = severity;
			HResult = OR_E_EXCEPTION;
		}
		
		public DataphorException(ErrorSeverity severity, int errorCode, string message, Exception innerException) : base(message, innerException)
		{
			_code = errorCode;
			_severity = severity;
			HResult = OR_E_EXCEPTION;
		}
		
		public DataphorException(Exception exception) : base(exception.Message)
		{
			HResult = OR_E_EXCEPTION;
			DataphorException dataphorException = exception as DataphorException;
			_serverContext = exception.StackTrace;
			if (dataphorException != null)
			{
				_code = dataphorException.Code;
				_severity = dataphorException.Severity;
				_details = dataphorException.GetDetails();
			}
			else
			{
				_code = ApplicationError;
				_severity = ErrorSeverity.Application;
			}
		}
		
		public DataphorException(Exception exception, Exception innerException) : base(exception.Message, innerException)
		{
			HResult = OR_E_EXCEPTION;
			DataphorException dataphorException = exception as DataphorException;
			_serverContext = exception.StackTrace;
			if (dataphorException != null)
			{
				_code = dataphorException.Code;
				_severity = dataphorException.Severity;
				_details = dataphorException.GetDetails();
			}
			else
			{
				_code = ApplicationError;
				_severity = ErrorSeverity.Application;
			}
		}
		
		protected DataphorException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(paramsValue == null ? GetMessage(resourceManager, errorCode) : String.Format(GetMessage(resourceManager, errorCode), paramsValue), innerException)
		{
			_code = errorCode;
			_severity = severity;
			HResult = OR_E_EXCEPTION;
		}
		
	    #if !SILVERLIGHT // SerializationInfo

		public DataphorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) 
		{
			_code = info.GetInt32("Code");
			_severity = (ErrorSeverity)info.GetInt32("Severity");
			_details = info.GetString("Details");
			_serverContext = info.GetString("ServerContext");
		}
		
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Code", _code);
			info.AddValue("Severity", (int)_severity);
			info.AddValue("Details", _details);
			info.AddValue("ServerContext", _serverContext);
		}
		
		#endif
		
		private int _code;
		public int Code 
		{ 
			get { return _code; } 
			set { _code = value; }
		}
		
		private ErrorSeverity _severity;
		public ErrorSeverity Severity
		{
			get { return _severity; }
			set { _severity = value; }
		}
		
		private string _details;
		public string Details
		{
			get { return _details; }
			set { _details = value; }
		}
		
		private string _serverContext;
		public string ServerContext
		{
			get { return _serverContext; }
			set { _serverContext = value; }
		}

		public string CombinedMessages
		{
			get
			{
				string message = String.Empty;
				Exception exception = this;
				while (exception != null)
				{
					message += exception.InnerException != null ? exception.Message + ", " : exception.Message;
					exception = exception.InnerException;
				}
				return message;
			}
		}
		
		public virtual string GetDetails()
		{
			return _details != null ? _details : String.Empty;
		}
		
		public string GetServerContext()
		{
			return _serverContext != null ? _serverContext : (StackTrace != null ? StackTrace : String.Empty);
		}

		public static string GetMessage(ResourceManager resourceManager, int errorCode)
		{
			string result = null;
			try
			{
				result = resourceManager.GetString(errorCode.ToString());
			}
			catch
			{
				result = String.Format(ManifestNotFound, errorCode, resourceManager.BaseName);
			}

			if (result == null)
				result = String.Format(MessageNotFound, errorCode, resourceManager.BaseName);
			return result;
		}
		
		public DataphorException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) : base(message, innerException)
		{
			_severity = severity;
			_code = code;
			_details = details;
			_serverContext = serverContext;
			HResult = OR_E_EXCEPTION;
		}
	}
}

