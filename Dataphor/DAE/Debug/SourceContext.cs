using System;
using System.Collections.Generic;
using System.Text;

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
		public SourceContext(string AScript, DebugLocator ALocator) : base()
		{
			FScript = AScript;
			FLocator = ALocator;
		}
		
		private string FScript;
		public string Script { get { return FScript; } }
		
		private DebugLocator FLocator;
		/// <summary>
		/// Provides a locator that is used to determine the location for the given script. May be null.
		/// </summary>
		/// <remarks>
		/// If the locator is not specified, the source context is used to allow debugging
		/// of dynamic or ad-hoc scripts.
		/// </remarks>
		public DebugLocator Locator { get { return FLocator; } }
	}
	
	public class SourceContexts : Stack<SourceContext> { }
}
