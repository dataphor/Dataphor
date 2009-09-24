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
		protected DAEException(ResourceManager AResourceManager, int AErrorCode, ErrorSeverity ASeverity, Exception AInnerException, params object[] AParams) : base(AResourceManager, AErrorCode, ASeverity, AInnerException, AParams) {}
	}

	public interface ILocatedException
	{
		int Line { get; set; }
		int LinePos { get; set; }
	}
}
