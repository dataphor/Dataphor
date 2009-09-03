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
			FLine = Math.Max(ALocator.Line - 1, 0) + ALine;
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
			int LIndexOfAt = AValue.LastIndexOf("@");
			int LIndexOfColon = AValue.LastIndexOf(":");
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
		/// Program locator string used to indicate that a locator is a reference to dynamic or ad-hoc execution
		/// </summary>
		public const string CProgramLocator = "program";
		
		/// <summary>
		/// Operator locator string used to indicate that a locator is a reference to an operator created by a dynamic or ad-hoc execution
		/// </summary>
		public const string COperatorLocator = "operator";
		
		public static string ProgramLocator(Guid APlanID)
		{
			return String.Format("{0}:{1}", CProgramLocator, APlanID.ToString());
		}
		
		/// <summary>
		/// Returns true if the locator is a program locator
		/// </summary>
		public static bool IsProgramLocator(string ALocator)
		{
			return ALocator.StartsWith(CProgramLocator + ":");
		}
		
		/// <summary>
		/// Returns the program id encoded in the given program locator.
		/// </summary>
		public static Guid GetProgramID(string AProgramLocator)
		{
			return new Guid(AProgramLocator.Substring(AProgramLocator.IndexOf(':') + 1));
		}
		
		/// <summary>
		/// Constructs an operator locator string based on an operator specifier.
		/// </summary>
		public static string OperatorLocator(string AOperatorSpecifier)
		{
			return String.Format("{0}:{1}", COperatorLocator, AOperatorSpecifier);
		}
		
		/// <summary>
		/// Returns true if the locator is an operator locator
		/// </summary>
		public static bool IsOperatorLocator(string ALocator)
		{
			return ALocator.StartsWith(COperatorLocator + ":");
		}

		/// <summary>
		/// Returns the operator specifier encoded in the given operator locator.
		/// </summary>
		public static string GetOperatorSpecifier(string AOperatorLocator)
		{
			return AOperatorLocator.Substring(AOperatorLocator.IndexOf(':') + 1);
		}
		
		/// <summary>
		/// Dynamic debug locator used as the locator for all dynamic execution.
		/// </summary>
		//public static DebugLocator Dynamic = new DebugLocator("DYNAMIC", 1, 1);

		public override bool Equals(object AOther)
		{
			DebugLocator LOther = AOther as DebugLocator;
			if (LOther != null && LOther.FLocator == this.FLocator && LOther.FLine == this.FLine && LOther.FLinePos == this.FLinePos)
				return true;
			else
				return base.Equals(AOther);
		}

		public override int GetHashCode()
		{
			return (FLocator == null ? 0 : FLocator.GetHashCode()) ^ FLine.GetHashCode() ^ FLinePos.GetHashCode();
		}
		
		public static bool operator ==(DebugLocator ALeft, DebugLocator ARight)
		{
			return (Object.ReferenceEquals(ALeft, null) && Object.ReferenceEquals(ARight, null)) 
				|| (!Object.ReferenceEquals(ALeft, null) && ALeft.Equals(ARight));
		}

		public static bool operator !=(DebugLocator ALeft, DebugLocator ARight)
		{
			return !(ALeft == ARight);
		}
	}
}
