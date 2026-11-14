/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;
using System.Reflection;

namespace Alphora.Dataphor.DAE
{
	// Base exception class for all exceptions thrown by the DAE.
	// Exception classes deriving from this class should be marked serializable, and provide a serializable constructor.
	public abstract class DAEException : DataphorException
	{
		// Constructors
		protected DAEException(ResourceManager resourceManager, int errorCode, ErrorSeverity severity, Exception innerException, params object[] paramsValue) : base(resourceManager, errorCode, severity, innerException, paramsValue) {}
		protected DAEException(ErrorSeverity severity, int code, string message, string details, string serverContext, DataphorException innerException) 
			: base(severity, code, message, details, serverContext, innerException)
		{
		}
	}

	public interface ILocatedException
	{
		int Line { get; set; }
		int LinePos { get; set; }
	}
	
	public interface ILocatorException : ILocatedException
	{
		string Locator { get; set; }
	}

	public static class LocatedExceptionExtensions
	{
		public static void SetLocator(this Exception exception, Alphora.Dataphor.DAE.Debug.DebugLocator locator)
		{
			ILocatorException locatorException = exception as ILocatorException;
			if (locatorException != null)
			{
                if (locator != null && (String.IsNullOrEmpty(locatorException.Locator) || locatorException.Locator == locator.Locator))
				{
					locatorException.Locator = locator.Locator;
					locatorException.Line += locator.Line - 1;
					locatorException.LinePos += locator.LinePos - 1;
				}
				else
					locatorException.Locator = null;
			}
		}
	}
}
