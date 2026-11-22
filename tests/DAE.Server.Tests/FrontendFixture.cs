/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

using NUnit.Framework;

using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// One in-process server carrying the Frontend library and the shipped and sample libraries whose form documents the
	/// L5 tests read. No assertions of its own: FormCustomizationTest and the derivation golden test both derive from it.
	/// </summary>
	/// <remarks>
	/// Nothing here needs a database server. Every library in the list below either has no device of its own or uses the
	/// in-memory SimpleDevice, which is why the whole list registers on a machine with no SQL Server installed.
	/// </remarks>
	// Not [Parallelizable]: each derived fixture starts a server, and two engines in one process share the engine's static
	// state (see the note on TestServerInstance).
	public abstract class FrontendFixture : InProcessTestFixture
	{
		/// <summary>
		/// The libraries this fixture copies and registers, in registration order. One list serves both: the copy order
		/// has no effect, and registering in this order is what the comment below requires.
		/// </summary>
		/// <remarks>
		/// Frontend comes before System.Internet because System.Internet's registration script calls the
		/// Frontend.ExecuteScript operator, so it cannot register on a server that has no Frontend - a defect in
		/// System.Internet, tracked by bug-system-internet-registration-needs-frontend; drop the special ordering when
		/// that lands. Every other library's requisites are already in this list ahead of it, so EnsureLibraryRegistered
		/// never has to pull one in on its own.
		/// </remarks>
		public static readonly string[] FrontendLibraries =
		[
			"System.Platform",
			"Frontend",
			"System.Internet",
			"SimpleDevice",
			"Security",
			"Sample.Common",
			"Sample.DetailAdd",
			"Sample.Forms",
			"Sample.IssueTracker",
			"Sample.SendEmail",
		];

		protected override string[] Libraries => FrontendLibraries;

		protected override string[] LibrariesToRegister => FrontendLibraries;

		/// <summary>
		/// Evaluates a D4 expression returning a String on this test's process. Values go in as parameters rather than as
		/// literals in the expression: the documents these tests pass around are XML holding quotes of both kinds.
		/// </summary>
		protected string EvaluateString(string expression, params (string Name, string Value)[] arguments)
		{
			DataParams parameters = null;
			if (arguments.Length > 0)
			{
				parameters = new DataParams();
				foreach ((string name, string value) in arguments)
					parameters.Add(DataParam.Create(Process, name, value));
			}
			return (string)((ServerProcess)Process).Evaluate(expression, parameters).AsNative;
		}

		/// <summary>Reads a form document of a registered library through the server, resolving a customization's ancestors.</summary>
		protected string LoadAndProcess(string libraryName, string documentName)
		{
			return EvaluateString("Frontend.LoadAndProcess(ALibraryName, AName)", ("ALibraryName", libraryName), ("AName", documentName));
		}

		/// <summary>Applies a delta to a form document, as loading a customization does.</summary>
		protected string Merge(string form, string delta)
		{
			return EvaluateString("Frontend.Merge(AForm, ADelta)", ("AForm", form), ("ADelta", delta));
		}

		/// <summary>
		/// The delta that turns <paramref name="form"/> into <paramref name="modified"/>. Despite the operator's parameter
		/// name (ADelta), its second argument is the modified document and its result is the delta.
		/// </summary>
		protected string Difference(string form, string modified)
		{
			return EvaluateString("Frontend.Difference(AForm, ADelta)", ("AForm", form), ("ADelta", modified));
		}

		/// <summary>Every message in an exception chain, so that an assertion can look for text the engine wrapped.</summary>
		protected static string FlattenMessages(Exception exception)
		{
			System.Text.StringBuilder result = new System.Text.StringBuilder();
			for (Exception current = exception; current != null; current = current.InnerException)
			{
				if (result.Length > 0)
					result.Append(" -> ");
				result.Append(current.Message);
			}
			return result.ToString();
		}
	}
}
