/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// The severity of the error that occurred.
	/// </summary>
	[Serializable]
	public enum ErrorSeverity 
	{ 
		/// <summary>
		/// Indicates the severity is unknown, usually because the exception that occurred was not a Dataphor exception.
		/// </summary>
		Unspecified, 
		
		/// <summary>
		/// Indicates that the error is a user-correctable problem such as a constraint violation.
		/// </summary>
		User, 
		
		/// <summary>
		/// Indicates that the exception is a developer-correctable problem with the application.
		/// </summary>
		Application, 

		/// <summary>
		/// Indicates that the exception is a system-level problem in the Dataphor server.
		/// </summary>
		System, 

		/// <summary>
		/// Indicates that the exception is an environment-level problem such as a network connectivity issue.
		/// </summary>
		Environment 
	}
	
	[Serializable]
	public class NativeCLIException : Exception
	{
		public NativeCLIException(string message) : base(message) { }
		public NativeCLIException(string message, Exception innerException) : base(message, innerException) { }
		
		public NativeCLIException(string message, int code, ErrorSeverity severity) : base(message)
		{
			_code = code;
			_severity = severity;
		}
		
		public NativeCLIException(string message, int code, ErrorSeverity severity, Exception innerException) : base(message, innerException)
		{
			_code = code;
			_severity = severity;
		}
		
		public NativeCLIException(string message, int code, ErrorSeverity severity, string details, string serverContext) : base(message)
		{
			_code = code;
			_severity = severity;
			_details = details;
			_serverContext = serverContext;
		}
		
		public NativeCLIException(string message, int code, ErrorSeverity severity, string details, string serverContext, Exception innerException) : base(message, innerException)
		{
			_code = code;
			_severity = severity;
			_details = details;
			_serverContext = serverContext;
		}
		
		public NativeCLIException(SerializationInfo info, StreamingContext context) : base(info, context) 
		{
			_code = info.GetInt32("Code");
			_severity = (ErrorSeverity)info.GetInt32("Severity");
			_details = info.GetString("Details");
			_serverContext = info.GetString("ServerContext");
		}
		
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Code", _code);
			info.AddValue("Severity", (int)_severity);
			info.AddValue("Details", _details);
			info.AddValue("ServerContext", _serverContext);
		}
		
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
	}
}

