/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.Debug
{
	/// <summary>
	/// Represents a debug locator used to specify the originating location for the source text of a compiled plan.
	/// </summary>
	[Serializable]
	public class DebugLocator
	{
		/// <summary>
		/// Constructs a new DebugLocator instance.
		/// </summary>
		/// <param name="ALocator">The locator reference.</param>
		/// <param name="ALine">The line number (1-based) at which the locator begins.</param>
		/// <param name="ALinePos">The character position (1-based) at which the locator begins.</param>
		public DebugLocator(string ALocator, int ALine, int ALinePos) : base()
		{
			FLocator = ALocator;
			FLine = ALine;
			FLinePos = ALinePos;
		}
		
		/// <summary>
		/// Constructs a new DebugLocator based on a source locator and current line information.
		/// </summary>
		/// <param name="ALocator">The source locator.</param>
		/// <param name="ALine">The current line number (1-based)</param>
		/// <param name="ALinePos">The current line position (1-based)</param>
		public DebugLocator(DebugLocator ALocator, int ALine, int ALinePos) : base()
		{
			FLocator = ALocator.Locator;
			FLine = ALocator.Line - 1 + ALine;
			FLinePos = ALocator.Line == ALine ? ALocator.LinePos + ALinePos : ALinePos;
		}
		
		private string FLocator;
		/// <summary>
		/// Gets the locator reference for this instance.
		/// </summary>
		public string Locator { get { return FLocator; } }
		
		private int FLine;
		/// <summary>
		/// Gets the line number (1-based) at which the locator begins.
		/// </summary>
		public int Line { get { return FLine; } }
		
		private int FLinePos;
		/// <summary>
		/// Gets the character position (1-based) at which the locator begins.
		/// </summary>
		public int LinePos { get { return FLinePos; } }

		/// <summary>
		/// Returns a string representation of the locator.
		/// </summary>
		public override string ToString()
		{
			return String.Format("{0}@{1}:{2}", FLocator, FLine, FLinePos);
		}
		
		/// <summary>
		/// Parses the given string and returns a debug locator instance.
		/// </summary>
		/// <remarks>
		/// If the string is not a valid encoding of a debug locator, an ArgumentException is thrown.</remarks>
		public static DebugLocator Parse(string AValue)
		{
			int LIndexOfAt = AValue.IndexOf("@");
			int LIndexOfColon = AValue.IndexOf(":");
			if ((LIndexOfAt < 0) || (LIndexOfColon < 0))
				throw new ArgumentException("Invalid locator string");
				
			return 
				new DebugLocator
				(
					AValue.Substring(0, LIndexOfAt),
					Int32.Parse(AValue.Substring(LIndexOfAt + 1, LIndexOfColon - LIndexOfAt)),
					Int32.Parse(AValue.Substring(LIndexOfColon + 1, AValue.Length - LIndexOfColon))
				);
		}
		
		/// <summary>
		/// Dynamic locator string used to indicate that a locator is a reference to dynamic or ad-hoc execution
		/// </summary>
		public const string CDynamicLocator = "DYNAMIC";
		
		/// <summary>
		/// Operator locator string used to indicate that a locator is a reference to an operator created by a dynamic or ad-hoc execution
		/// </summary>
		public const string COperatorLocator = "OPERATOR";
		
		/// <summary>
		/// Constructs an operator locator string based on an operator specifier.
		/// </summary>
		public static string OperatorLocator(string AOperatorSpecifier)
		{
			return String.Format("{0}:{1}", COperatorLocator, AOperatorSpecifier);
		}

		/// <summary>
		/// Dynamic debug locator used as the locator for all dynamic execution.
		/// </summary>
		//public static DebugLocator Dynamic = new DebugLocator("DYNAMIC", 1, 1);
	}
}
