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
		public const int COR_E_EXCEPTION = -2146233088;
		public const int CApplicationError = 500000;

		public const string CMessageNotFound = @"DataphorException: Message ({0}) not found in ""{1}"".";
		public const string CManifestNotFound = @"DataphorException: Message ({0}) Manifest not found for BaseName ""{1}"".";
		
		public DataphorException(string AMessage) : base(AMessage)
		{
			FCode = CApplicationError;
			FSeverity = ErrorSeverity.Application;
			HResult = COR_E_EXCEPTION;
		}
		
		public DataphorException(int AErrorCode, string AMessage) : base(AMessage)
		{
			FCode = AErrorCode;
			FSeverity = ErrorSeverity.Application;
			HResult = COR_E_EXCEPTION;
		}
		
		public DataphorException(ErrorSeverity ASeverity, int AErrorCode, string AMessage) : base(AMessage)
		{
			FCode = AErrorCode;
			FSeverity = ASeverity;
			HResult = COR_E_EXCEPTION;
		}
		
		public DataphorException(ErrorSeverity ASeverity, int AErrorCode, string AMessage, Exception AInnerException) : base(AMessage, AInnerException)
		{
			FCode = AErrorCode;
			FSeverity = ASeverity;
			HResult = COR_E_EXCEPTION;
		}
		
		public DataphorException(Exception AException) : base(AException.Message)
		{
			HResult = COR_E_EXCEPTION;
			DataphorException LDataphorException = AException as DataphorException;
			FServerContext = AException.StackTrace;
			if (LDataphorException != null)
			{
				FCode = LDataphorException.Code;
				FSeverity = LDataphorException.Severity;
				FDetails = LDataphorException.GetDetails();
			}
			else
			{
				FCode = CApplicationError;
				FSeverity = ErrorSeverity.Application;
			}
		}
		
		public DataphorException(Exception AException, Exception AInnerException) : base(AException.Message, AInnerException)
		{
			HResult = COR_E_EXCEPTION;
			DataphorException LDataphorException = AException as DataphorException;
			FServerContext = AException.StackTrace;
			if (LDataphorException != null)
			{
				FCode = LDataphorException.Code;
				FSeverity = LDataphorException.Severity;
				FDetails = LDataphorException.GetDetails();
			}
			else
			{
				FCode = CApplicationError;
				FSeverity = ErrorSeverity.Application;
			}
		}
		
		protected DataphorException(ResourceManager AResourceManager, int AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(AParams == null ? GetMessage(AResourceManager, AErrorCode) : String.Format(GetMessage(AResourceManager, AErrorCode), AParams), AInnerException)
		{
			FCode = AErrorCode;
			FSeverity = ASeverity;
			HResult = COR_E_EXCEPTION;
		}
		
	    #if !SILVERLIGHT // SerializationInfo

		public DataphorException(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext) : base(AInfo, AContext) 
		{
			FCode = AInfo.GetInt32("Code");
			FSeverity = (ErrorSeverity)AInfo.GetInt32("Severity");
			FDetails = AInfo.GetString("Details");
			FServerContext = AInfo.GetString("ServerContext");
		}
		
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo AInfo, System.Runtime.Serialization.StreamingContext AContext)
		{
			base.GetObjectData(AInfo, AContext);
			AInfo.AddValue("Code", FCode);
			AInfo.AddValue("Severity", (int)FSeverity);
			AInfo.AddValue("Details", FDetails);
			AInfo.AddValue("ServerContext", FServerContext);
		}
		
		#endif
		
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

		public static string GetMessage(ResourceManager AResourceManager, int AErrorCode)
		{
			string LResult = null;
			try
			{
				LResult = AResourceManager.GetString(AErrorCode.ToString());
			}
			catch
			{
				LResult = String.Format(CManifestNotFound, AErrorCode, AResourceManager.BaseName);
			}

			if (LResult == null)
				LResult = String.Format(CMessageNotFound, AErrorCode, AResourceManager.BaseName);
			return LResult;
		}
		
		public DataphorException(ErrorSeverity ASeverity, int ACode, string AMessage, string ADetails, string AServerContext, DataphorException AInnerException) : base(AMessage, AInnerException)
		{
			FSeverity = ASeverity;
			FCode = ACode;
			FDetails = ADetails;
			FServerContext = AServerContext;
			HResult = COR_E_EXCEPTION;
		}
	}
}

