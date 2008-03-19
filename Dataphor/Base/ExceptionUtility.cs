/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define INVERTEXCEPTIONDESCRIPTIONS

using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

namespace Alphora.Dataphor
{
	[Serializable]
	public class AssertionException : Exception
	{
		public AssertionException(string AMessage, params object[] AArguments) : base(String.Format(AMessage, AArguments)) {}
		public AssertionException(string AMessage, Exception AInner, params object[] AArguments) : base(String.Format(AMessage, AArguments), AInner) {}
	}

	/// <summary> Error handling routines. </summary>
	public sealed class Error
	{
		/// <summary> Warns the developer of a non-critical condition. </summary>
		/// <remarks> Warn is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Warn(string AMessage, params object[] AArguments)
		{
			System.Diagnostics.Trace.WriteLine(String.Format(AMessage, AArguments));
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		public static void Fail(string AMessage, params object[] AArguments)
		{
			throw new AssertionException("Internal Error: {0}", String.Format(AMessage, AArguments));
		}

		/// <summary> Warns the developer of a non-critical condition. </summary>
		/// <remarks> AssertWarn is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void AssertWarn(bool ACondition, string AMessage, params object[] AArguments)
		{
			if (!ACondition)
				Warn(AMessage, AArguments);
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		public static void AssertFail(bool ACondition, string AMessage, params object[] AArguments)
		{
			if (!ACondition)
				Fail(AMessage, AArguments);
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		/// <remarks> DebugAssertFail is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void DebugAssertFail(bool ACondition, string AMessage, params object[] AArguments)
		{
			if (!ACondition)
				Fail(AMessage, AArguments);
		}
	}

	/// <summary> Sundry exception related utilitary routines. </summary>
	public sealed class ExceptionUtility
	{
		private static void InternalBriefDescription(StringBuilder ABuilder, Exception AException)
		{
			if (AException.InnerException != null)
				InternalBriefDescription(ABuilder, AException.InnerException);
				
			if (ABuilder.Length > 0)
				ABuilder.Append("\r\n----\r\n");
			ABuilder.Append(AException.Message);
		}
		
		/// <summary> Builds a string containing a brief description of a given exception. </summary>
		public static string BriefDescription(Exception AException)
		{
			#if INVERTEXCEPTIONDESCRIPTIONS
			StringBuilder LBuilder = new StringBuilder();
			InternalBriefDescription(LBuilder, AException);
			return LBuilder.ToString();
			#else
			Exception LCurrent = AException;
			while (LCurrent != null)
			{
				if (LBuilder.Length > 0)
					LBuilder.Append("; ");
				LBuilder.Append(LCurrent.Message);
				LCurrent = LCurrent.InnerException;
			}
			return LBuilder.ToString();
			#endif
		}
		
		private static void InternalDetailedDescription(StringBuilder ABuilder, Exception AException)
		{
			if (AException.InnerException != null)
				InternalDetailedDescription(ABuilder, AException.InnerException);
				
			if (ABuilder.Length > 0)
				ABuilder.Append("\r\n");

			DataphorException LDataphorException = AException as DataphorException;
			if (LDataphorException != null)
			{
				ABuilder.AppendFormat("{0}:{1} --->\r\n{2}", LDataphorException.Severity.ToString(), LDataphorException.Code.ToString(), LDataphorException.Message);
				string LDetails = LDataphorException.GetDetails();
				if (LDetails != String.Empty)
					ABuilder.AppendFormat("\r\n---- Details ----\r\n{0}", LDetails);
				LDetails = LDataphorException.GetServerContext();
				if (LDetails != String.Empty)
					ABuilder.AppendFormat("\r\n---- Server Context ----\r\n{0}", LDetails);
			}
			else
			{
				ABuilder.AppendFormat("{0} --->\r\n{1}", AException.GetType().Name, AException.Message);
				if (AException.StackTrace != null)
					ABuilder.AppendFormat("\r\n---- Stack Trace ----\r\n{0}", AException.StackTrace);
			}
		}

		/// <summary> Builds a string containing a detailed description of a given exception. </summary>
		public static string DetailedDescription(Exception AException)
		{
			#if INVERTEXCEPTIONDESCRIPTIONS
			StringBuilder LBuilder = new StringBuilder();
			InternalDetailedDescription(LBuilder, AException);
			return LBuilder.ToString();
			#else
			try
			{
				return AException.ToString();
			}
			catch
			{
				return BriefDescription(AException);
			}
			#endif
		}
		
		public static string StringsToCommaList(StringCollection AStrings)
		{
			StringBuilder LBuilder = new StringBuilder();
			for (int LIndex = 0; LIndex < AStrings.Count; LIndex++)
			{
				if (LIndex > 0)
					LBuilder.Append(", ");
				LBuilder.AppendFormat(@"""{0}""", AStrings[LIndex]);
			}
			return LBuilder.ToString();
		}
		
		public static string StringsToCommaList(string[] AStrings)
		{
			StringBuilder LBuilder = new StringBuilder();
			for (int LIndex = 0; LIndex < AStrings.Length; LIndex++)
			{
				if (LIndex > 0)
					LBuilder.Append(", ");
				LBuilder.AppendFormat(@"""{0}""", AStrings[LIndex]);
			}
			return LBuilder.ToString();
		}
		
		public static string StringsToList(StringCollection AStrings)
		{
			StringBuilder LBuilder = new StringBuilder();
			for (int LIndex = 0; LIndex < AStrings.Count; LIndex++)
				LBuilder.AppendFormat(@"{0}", AStrings[LIndex]);
			return LBuilder.ToString();
		}
		
		public static string StringsToList(string[] AStrings)
		{
			StringBuilder LBuilder = new StringBuilder();
			for (int LIndex = 0; LIndex < AStrings.Length; LIndex++)
				LBuilder.AppendFormat(@"{0}", AStrings[LIndex]);
			return LBuilder.ToString();
		}
		
		public static void AppendMessage(StringBuilder ABuilder, int AIndent, Exception AException)
		{
			for (int LIndex = 0; LIndex < AIndent; LIndex++)
				ABuilder.Append("\t");
				
			#if USEDETAILEDEXCEPTIONDESCRIPTION
			ABuilder.AppendFormat("{0}\r\n", ExceptionUtility.DetailedDescription(AException));
			#else
			ABuilder.AppendFormat("{0}\r\n", AException.Message);
			#endif
			if (AException.InnerException != null)
				AppendMessage(ABuilder, AIndent + 1, AException.InnerException);
		}
		
		public static void AppendMessage(StringBuilder ABuilder, int AIndent, string AMessage)
		{
			for (int LIndex = 0; LIndex < AIndent; LIndex++)
				ABuilder.Append("\t");
				
			ABuilder.AppendFormat("{0}\r\n", AMessage);
		}
	}
}
