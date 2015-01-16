/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.Debug
{
	/// <summary>
	/// Represents a debug locator used to specify the originating location for the source text of a compiled plan.
	/// </summary>
	//[DataContract]
	public class DebugLocator
	{
		/// <summary>
		/// Constructs a new DebugLocator instance.
		/// </summary>
		/// <param name="locator">The locator reference.</param>
		/// <param name="line">The line number (1-based) at which the locator begins.</param>
		/// <param name="linePos">The character position (1-based) at which the locator begins.</param>
		public DebugLocator(string locator, int line, int linePos) : base()
		{
			_locator = locator;
			_line = line;
			_linePos = linePos;
		}
		
		/// <summary>
		/// Constructs a new DebugLocator based on a source locator and current line information.
		/// </summary>
		/// <param name="locator">The source locator.</param>
		/// <param name="line">The current line number (1-based)</param>
		/// <param name="linePos">The current line position (1-based)</param>
		public DebugLocator(DebugLocator locator, int line, int linePos) : base()
		{
			_locator = locator.Locator;
			_line = Math.Max(locator.Line - 1, 0) + line;
			_linePos = locator.Line == line ? Math.Max(locator.LinePos - 1, 0) + linePos : linePos;
		}
		
		//[DataMember]
		internal string _locator;
		/// <summary>
		/// Gets the locator reference for this instance.
		/// </summary>
		public string Locator { get { return _locator; } }
		
		//[DataMember]
		internal int _line;
		/// <summary>
		/// Gets the line number (1-based) at which the locator begins.
		/// </summary>
		public int Line { get { return _line; } }
		
		//[DataMember]
		internal int _linePos;
		/// <summary>
		/// Gets the character position (1-based) at which the locator begins.
		/// </summary>
		public int LinePos { get { return _linePos; } }

		/// <summary>
		/// Returns a string representation of the locator.
		/// </summary>
		public override string ToString()
		{
			return String.Format("{0}@{1}:{2}", _locator, _line, _linePos);
		}
		
		/// <summary>
		/// Parses the given string and returns a debug locator instance.
		/// </summary>
		/// <remarks>
		/// If the string is not a valid encoding of a debug locator, an ArgumentException is thrown.</remarks>
		public static DebugLocator Parse(string tempValue)
		{
			int indexOfAt = tempValue.LastIndexOf("@");
			int indexOfColon = tempValue.LastIndexOf(":");
			if ((indexOfAt < 0) || (indexOfColon < 0))
				throw new ArgumentException("Invalid locator string");
				
			return 
				new DebugLocator
				(
					tempValue.Substring(0, indexOfAt),
					Int32.Parse(tempValue.Substring(indexOfAt + 1, indexOfColon - indexOfAt - 1)),
					Int32.Parse(tempValue.Substring(indexOfColon + 1, tempValue.Length - indexOfColon - 1))
				);
		}
		
		/// <summary>
		/// Program locator string used to indicate that a locator is a reference to dynamic or ad-hoc execution
		/// </summary>
		public const string ProgramLocatorPrefix = "program";
		
		/// <summary>
		/// Operator locator string used to indicate that a locator is a reference to an operator created by a dynamic or ad-hoc execution
		/// </summary>
		public const string OperatorLocatorPrefix = "operator";
		
		public static string ProgramLocator(Guid planID)
		{
			return String.Format("{0}:{1}", ProgramLocatorPrefix, planID.ToString());
		}
		
		/// <summary>
		/// Returns true if the locator is a program locator
		/// </summary>
		public static bool IsProgramLocator(string locator)
		{
			return locator.StartsWith(ProgramLocatorPrefix + ":");
		}
		
		/// <summary>
		/// Returns the program id encoded in the given program locator.
		/// </summary>
		public static Guid GetProgramID(string programLocator)
		{
			return new Guid(programLocator.Substring(programLocator.IndexOf(':') + 1));
		}
		
		/// <summary>
		/// Constructs an operator locator string based on an operator specifier.
		/// </summary>
		public static string OperatorLocator(string operatorSpecifier)
		{
			return String.Format("{0}:{1}", OperatorLocatorPrefix, operatorSpecifier);
		}
		
		/// <summary>
		/// Returns true if the locator is an operator locator
		/// </summary>
		public static bool IsOperatorLocator(string locator)
		{
			return locator.StartsWith(OperatorLocatorPrefix + ":");
		}

		/// <summary>
		/// Returns the operator specifier encoded in the given operator locator.
		/// </summary>
		public static string GetOperatorSpecifier(string operatorLocator)
		{
			return operatorLocator.Substring(operatorLocator.IndexOf(':') + 1);
		}
		
		/// <summary>
		/// Dynamic debug locator used as the locator for all dynamic execution.
		/// </summary>
		//public static DebugLocator Dynamic = new DebugLocator("DYNAMIC", 1, 1);

		public override bool Equals(object other)
		{
			DebugLocator localOther = other as DebugLocator;
			if (localOther != null && localOther._locator == this._locator && localOther._line == this._line && localOther._linePos == this._linePos)
				return true;
			else
				return base.Equals(other);
		}

		public override int GetHashCode()
		{
			return (_locator == null ? 0 : _locator.GetHashCode()) ^ _line.GetHashCode() ^ _linePos.GetHashCode();
		}
		
		public static bool operator ==(DebugLocator left, DebugLocator right)
		{
			return (Object.ReferenceEquals(left, null) && Object.ReferenceEquals(right, null)) 
				|| (!Object.ReferenceEquals(left, null) && left.Equals(right));
		}

		public static bool operator !=(DebugLocator left, DebugLocator right)
		{
			return !(left == right);
		}
	}
}
