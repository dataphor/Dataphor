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
		public NativeCLIException(string AMessage) : base(AMessage) { }
		public NativeCLIException(string AMessage, Exception AInnerException) : base(AMessage, AInnerException) { }
		
		public NativeCLIException(string AMessage, int ACode, ErrorSeverity ASeverity) : base(AMessage)
		{
			FCode = ACode;
			FSeverity = ASeverity;
		}
		
		public NativeCLIException(string AMessage, int ACode, ErrorSeverity ASeverity, Exception AInnerException) : base(AMessage, AInnerException)
		{
			FCode = ACode;
			FSeverity = ASeverity;
		}
		
		public NativeCLIException(string AMessage, int ACode, ErrorSeverity ASeverity, string ADetails, string AServerContext) : base(AMessage)
		{
			FCode = ACode;
			FSeverity = ASeverity;
			FDetails = ADetails;
			FServerContext = AServerContext;
		}
		
		public NativeCLIException(string AMessage, int ACode, ErrorSeverity ASeverity, string ADetails, string AServerContext, Exception AInnerException) : base(AMessage, AInnerException)
		{
			FCode = ACode;
			FSeverity = ASeverity;
			FDetails = ADetails;
			FServerContext = AServerContext;
		}
		
		public NativeCLIException(SerializationInfo AInfo, StreamingContext AContext) : base(AInfo, AContext) 
		{
			FCode = AInfo.GetInt32("Code");
			FSeverity = (ErrorSeverity)AInfo.GetInt32("Severity");
			FDetails = AInfo.GetString("Details");
			FServerContext = AInfo.GetString("ServerContext");
		}
		
		public override void GetObjectData(SerializationInfo AInfo, StreamingContext AContext)
		{
			base.GetObjectData(AInfo, AContext);
			AInfo.AddValue("Code", FCode);
			AInfo.AddValue("Severity", (int)FSeverity);
			AInfo.AddValue("Details", FDetails);
			AInfo.AddValue("ServerContext", FServerContext);
		}
		
		private int FCode;
		public int Code 
		{ 
			get { return FCode; } 
			set { FCode = value; }
		}
		
		private ErrorSeverity FSeverity;
		public ErrorSeverity Severity
		{
			get { return FSeverity; }
			set { FSeverity = value; }
		}
		
		private string FDetails;
		public string Details
		{
			get { return FDetails; }
			set { FDetails = value; }
		}
		
		private string FServerContext;
		public string ServerContext
		{
			get { return FServerContext; }
			set { FServerContext = value; }
		}

		public string CombinedMessages
		{
			get
			{
				string LMessage = String.Empty;
				Exception LException = this;
				while (LException != null)
				{
					LMessage += LException.InnerException != null ? LException.Message + ", " : LException.Message;
					LException = LException.InnerException;
				}
				return LMessage;
			}
		}
		
		public virtual string GetDetails()
		{
			return FDetails != null ? FDetails : String.Empty;
		}
		
		public string GetServerContext()
		{
			return FServerContext != null ? FServerContext : (StackTrace != null ? StackTrace : String.Empty);
		}
	}
}

