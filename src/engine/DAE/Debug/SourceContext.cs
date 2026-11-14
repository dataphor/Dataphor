/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.DAE.Debug
{
	/// <summary>
	/// Represents a compile-time source context, i.e. the script currently being compiled.
	/// </summary>
	/// <remarks>
	/// Note that the script may be a subset of the overall script identified by the
	/// locator. Line and LinePos are the offset in the locator at which AScript starts.
	/// </remarks>
	public class SourceContext
	{
		public SourceContext(string script, DebugLocator locator) : base()
		{
			_script = script;
			_locator = locator;
		}
		
		private string _script;
		public string Script { get { return _script; } }
		
		private DebugLocator _locator;
		/// <summary>
		/// Provides a locator that is used to determine the location for the given script. May be null.
		/// </summary>
		/// <remarks>
		/// If the locator is not specified, the source context is used to allow debugging
		/// of dynamic or ad-hoc scripts.
		/// </remarks>
		public DebugLocator Locator { get { return _locator; } }
	}
	
	public class SourceContexts : System.Collections.Generic.Stack<SourceContext> { }
}
