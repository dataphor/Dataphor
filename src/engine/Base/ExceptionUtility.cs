/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
#define INVERTEXCEPTIONDESCRIPTIONS

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor
{
	public class AssertionException : Exception
	{
		public AssertionException(string message, params object[] arguments) : base(String.Format(message, arguments)) {}
		public AssertionException(string message, Exception inner, params object[] arguments) : base(String.Format(message, arguments), inner) {}
	}

	/// <summary> Error handling routines. </summary>
	public sealed class Error
	{
		/// <summary> Warns the developer of a non-critical condition. </summary>
		/// <remarks> Warn is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Warn(string message, params object[] arguments)
		{
			#if SILVERLIGHT
			System.Diagnostics.Debug.WriteLine(String.Format(message, arguments));
			#else
			System.Diagnostics.Trace.WriteLine(String.Format(message, arguments));
			#endif
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		public static void Fail(string message, params object[] arguments)
		{
			throw new AssertionException("Internal Error: {0}", String.Format(message, arguments));
		}

		/// <summary> Warns the developer of a non-critical condition. </summary>
		/// <remarks> AssertWarn is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void AssertWarn(bool condition, string message, params object[] arguments)
		{
			if (!condition)
				Warn(message, arguments);
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		public static void AssertFail(bool condition, string message, params object[] arguments)
		{
			if (!condition)
				Fail(message, arguments);
		}

		/// <summary> Throws an "internal error" assertion exception for an unexpected critical condition. </summary>
		/// <remarks> DebugAssertFail is only called if DEBUG is defined. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void DebugAssertFail(bool condition, string message, params object[] arguments)
		{
			if (!condition)
				Fail(message, arguments);
		}
	}

	/// <summary> Sundry exception related utilitary routines. </summary>
	public sealed class ExceptionUtility
	{
		private static void InternalBriefDescription(StringBuilder builder, Exception exception)
		{
			if (exception.InnerException != null)
				InternalBriefDescription(builder, exception.InnerException);
				
			if (builder.Length > 0)
				builder.Append("\r\n----\r\n");
			builder.Append(exception.Message);
		}
		
		/// <summary> Builds a string containing a brief description of a given exception. </summary>
		public static string BriefDescription(Exception exception)
		{
			#if INVERTEXCEPTIONDESCRIPTIONS
			StringBuilder builder = new StringBuilder();
			InternalBriefDescription(builder, exception);
			return builder.ToString();
			#else
			Exception current = exception;
			while (current != null)
			{
				if (builder.Length > 0)
					builder.Append("; ");
				builder.Append(current.Message);
				current = current.InnerException;
			}
			return builder.ToString();
			#endif
		}
		
		private static void InternalDetailedDescription(StringBuilder builder, Exception exception)
		{
			if (exception.InnerException != null)
				InternalDetailedDescription(builder, exception.InnerException);
				
			if (builder.Length > 0)
				builder.Append("\r\n");

			DataphorException dataphorException = exception as DataphorException;
			if (dataphorException != null)
			{
				builder.AppendFormat("{0}:{1} --->\r\n{2}", dataphorException.Severity.ToString(), dataphorException.Code.ToString(), dataphorException.Message);
				string details = dataphorException.GetDetails();
				if (details != String.Empty)
					builder.AppendFormat("\r\n---- Details ----\r\n{0}", details);
				details = dataphorException.GetServerContext();
				if (details != String.Empty)
					builder.AppendFormat("\r\n---- Server Context ----\r\n{0}", details);
			}
			else
			{
				builder.AppendFormat("{0} --->\r\n{1}", exception.GetType().Name, exception.Message);
				if (exception.StackTrace != null)
					builder.AppendFormat("\r\n---- Stack Trace ----\r\n{0}", exception.StackTrace);
			}
		}

		/// <summary> Builds a string containing a detailed description of a given exception. </summary>
		public static string DetailedDescription(Exception exception)
		{
			#if INVERTEXCEPTIONDESCRIPTIONS
			StringBuilder builder = new StringBuilder();
			InternalDetailedDescription(builder, exception);
			return builder.ToString();
			#else
			try
			{
				return exception.ToString();
			}
			catch
			{
				return BriefDescription(exception);
			}
			#endif
		}
		
		public static string StringsToCommaList(List<string> strings)
		{
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < strings.Count; index++)
			{
				if (index > 0)
					builder.Append(", ");
				builder.AppendFormat(@"""{0}""", strings[index]);
			}
			return builder.ToString();
		}
		
		public static string StringsToCommaList(string[] strings)
		{
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < strings.Length; index++)
			{
				if (index > 0)
					builder.Append(", ");
				builder.AppendFormat(@"""{0}""", strings[index]);
			}
			return builder.ToString();
		}
		
		public static string StringsToList(List<string> strings)
		{
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < strings.Count; index++)
				builder.AppendFormat(@"{0}", strings[index]);
			return builder.ToString();
		}
		
		public static string StringsToList(string[] strings)
		{
			StringBuilder builder = new StringBuilder();
			for (int index = 0; index < strings.Length; index++)
				builder.AppendFormat(@"{0}", strings[index]);
			return builder.ToString();
		}
		
		public static void AppendMessage(StringBuilder builder, int indent, Exception exception)
		{
			for (int index = 0; index < indent; index++)
				builder.Append("\t");
				
			#if USEDETAILEDEXCEPTIONDESCRIPTION
			builder.AppendLine(ExceptionUtility.DetailedDescription(exception));
			#else
			builder.AppendLine(exception.Message);
			#endif
			if (exception.InnerException != null)
				AppendMessage(builder, indent + 1, exception.InnerException);
		}
		
		public static void AppendMessage(StringBuilder builder, int indent, string message)
		{
			for (int index = 0; index < indent; index++)
				builder.Append("\t");
				
			builder.AppendLine(message);
		}
	}
}
