/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Executes a client side script. </summary>
	public class ScriptAction : Action, IScriptAction
	{
		public const string CCSharpTemplate = 
			@"
				using System;
				using {0};
				{1}
				public class Script : {2}
				{{
					public Script(IAction AAction) : base(AAction) {{}}
					{3}
					{4}
					public override void Execute(INode ANode, EventParams AParams)
					{{
						{5}
					}}
				}}
			";

		public const string CVBTemplate =
			@"
				Imports System
				Imports {0}
				{1}
				Public Class Script
						Inherits {2}
					Public Sub New(AAction As IAction)
						MyBase.New(AAction)
					End Sub
					{3}
					{4}
					Public Overrides Sub Execute(ANode as INode, AParams As EventParams)
						{5}
					End Sub
				End Class
			";

		public const string CCompileUnitToken = "#unit";
		public const string CEndCompileUnitToken = "#endunit";
		public const string CClassToken = "#class";
		public const string CEndClassToken = "#endclass";
		
		protected override void Dispose(bool ADisposing)
		{
			base.Dispose(ADisposing);
		}

		// Language
		private ScriptLanguages FLanguage = ScriptLanguages.CSharp;
		[DefaultValue(ScriptLanguages.CSharp)]
		[Description("Specified the language for the script.")]
		public ScriptLanguages Language
		{
			get { return FLanguage; }
			set
			{
				if (FLanguage != value)
				{
					FLanguage = value;
					InvalidateResults();
				}
			}
		}

		// Script
		private string FScript = String.Empty;
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Description(@"The script to execute.")]
		public string Script
		{
			get { return FScript; }
			set
			{
				if (FScript != value)
				{
					FScript = (value == null ? String.Empty : value);
					InvalidateResults();
				}
			}
		}

		// CompileWithDebug
		private bool FCompileWithDebug = false;
		[Description("When true, the script will be compiled with debug information.")]
		[DefaultValue(false)]
		public bool CompileWithDebug
		{
			get { return FCompileWithDebug; }
			set { FCompileWithDebug = value; }
		}

		private static void PreprocessScript(string AScript, out string AUnit, out string AClass, out string AExecute)
		{
			// TODO: Put in lexor in for pre-processor (for comment/string support)

			int LPos = AScript.IndexOf(CCompileUnitToken);
			int LEndPos;
			if (LPos >= 0)
			{
				LEndPos = AScript.IndexOf(CEndCompileUnitToken);
				AUnit = AScript.Substring(LPos + 5, LEndPos - (LPos + 5));
				AScript = AScript.Substring(0, LPos) + AScript.Substring(LEndPos + 8);
			}
			else
				AUnit = String.Empty;

			LPos = AScript.IndexOf(CClassToken);
			if (LPos >= 0)
			{
				LEndPos = AScript.IndexOf(CEndClassToken);
				AClass = AScript.Substring(LPos + 6, LEndPos - (LPos + 6));
				AScript = AScript.Substring(0, LPos) + AScript.Substring(LEndPos + 9);
			}
			else
				AClass = String.Empty;
			
			AExecute = AScript;
		}
		
		private static object FAssemblyCacheLock = new Object();
		private static Dictionary<string, CompilerResults> FAssemblyCache = new Dictionary<string, CompilerResults>(); // key - FSourceCode, value - FResults
		
		private CompilerResults FindCompilerResults()
		{
			if (FSourceCode == null)
				BuildSourceCode();
				
			lock (FAssemblyCacheLock)
				if (FAssemblyCache.ContainsKey(FSourceCode))
					return FAssemblyCache[FSourceCode];

			return null;
		}
		
		private static string AssemblyReferenceName(AssemblyName LName)
		{
			// TODO : Come up with way to compile against assemblies in the GAC
			return LName.Name + ".dll";
		}
		
		private static string[] GetReferencedAssemblyNames(Assembly AAssembly)
		{
			AssemblyName[] LReferencedAssemblies = AAssembly.GetReferencedAssemblies();
			string[] LResult = new string[LReferencedAssemblies.Length + 1];
			for (int i = 0; i < LReferencedAssemblies.Length; i++)
				LResult[i] = Assembly.Load(LReferencedAssemblies[i]).Location;
			LResult[LResult.Length - 1] = AAssembly.Location;
			return LResult;
		}
		
		private void AddReferencedAssemblies(List<string> AAssemblies, Assembly AAssembly)
		{
			// Add all assemblies referenced by the ScriptAction itself
			string[] LAssemblies = GetReferencedAssemblyNames(AAssembly);
			for (int LIndex = 0; LIndex < LAssemblies.Length; LIndex++)
				if (!AAssemblies.Contains(LAssemblies[LIndex]))
					AAssemblies.Add(LAssemblies[LIndex]);

			// Add all assemblies in the node types table
			string LAssemblyName;
			foreach (NodeTypeEntry LEntry in HostNode.Session.NodeTypeTable.Entries)
			{
				LAssemblyName = Assembly.Load(LEntry.Assembly, null).Location;
				if (!AAssemblies.Contains(LAssemblyName))
					AAssemblies.Add(LAssemblyName);
			}
		}
		
		private string FSourceCode;
		private List<string> FReferencedAssemblies;
		private CompilerResults FResults;

		private void InvalidateResults()
		{
			FResults = null;
			FReferencedAssemblies = null;
			FSourceCode = null;
		}
		
		private void BuildSourceCode()
		{
			string LUnit;
			string LClass;
			string LExecute;
			PreprocessScript(FScript, out LUnit, out LClass, out LExecute);
			string LTemplate;

			switch (FLanguage)
			{
				case ScriptLanguages.CSharp : LTemplate = CCSharpTemplate; break;
				case ScriptLanguages.VisualBasic : LTemplate = CVBTemplate; break;
				default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, FLanguage);
			}

			FReferencedAssemblies = new List<string>();
			AddReferencedAssemblies(FReferencedAssemblies, typeof(ScriptAction).Assembly);
			FSourceCode = String.Format
			(
				LTemplate,
				GetType().Namespace,
				LUnit,
				typeof(ScriptBase).FullName,
				BuildNodeReferences(FReferencedAssemblies),
				LClass,
				LExecute
			);
		}
		
		private Assembly GetCompiledAssembly()
		{
			if (FResults == null)
			{
				FResults = FindCompilerResults();
				if (FResults == null)
				{
					CompilerParameters LParameters = new CompilerParameters();
					foreach (string LString in FReferencedAssemblies)
						LParameters.ReferencedAssemblies.Add(LString);
					LParameters.GenerateExecutable = false;
					LParameters.GenerateInMemory = true;
					LParameters.IncludeDebugInformation = FCompileWithDebug;
					//CompilerParameters LParameters = new CompilerParameters(LReferencedAssembliesArray, GetTempAssemblyFileName(), FCompileWithDebug);
	
					CodeDomProvider LProvider;
					switch (FLanguage)
					{
						case ScriptLanguages.CSharp : LProvider = new CSharpCodeProvider(); break;
						case ScriptLanguages.VisualBasic : LProvider = new VBCodeProvider(); break;
						default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, FLanguage);
					}

					FResults = LProvider.CompileAssemblyFromSource(LParameters, FSourceCode);
					lock (FAssemblyCacheLock)
						if (!FAssemblyCache.ContainsKey(FSourceCode))
							FAssemblyCache.Add(FSourceCode, FResults);
				}
			}

			if (FResults.NativeCompilerReturnValue != 0)
				lock (FAssemblyCacheLock)	// Error enumeration is not concurrent, must lock
				{
					StringBuilder LErrors = new StringBuilder();
					foreach (CompilerError LError in FResults.Errors)
						LErrors.AppendFormat
						(
							"{0} {1} at line ({2}) column ({3}): {4}\r\n",
							(LError.IsWarning ? "Warning" : "Error"),
							LError.ErrorNumber,
							LError.Line,
							LError.Column,
							LError.ErrorText
						);
					LErrors.AppendFormat("Script:\r\n{0}\r\n----------End Script----------\r\n", EmbedErrorAnnotatedSourceScript(FSourceCode, FResults));
					throw new ClientException(ClientException.Codes.ScriptCompilerError, LErrors.ToString());
				}

			return FResults.CompiledAssembly;
		}

		/// <summary> Emits the given source code with the given set of error messages annotated within. </summary>
		private static string EmbedErrorAnnotatedSourceScript(string ASourceCode, CompilerResults AResults)
		{
			var LResult = new StringBuilder(ASourceCode.Length);
			var LPos = 0;
			var LLine = 1;																	 
			while (true)
			{
				var LMatch = ASourceCode.IndexOf("\n", LPos);
				if (LMatch >= 0)
				{
					var LSourceLine = ASourceCode.Substring(LPos, LMatch - LPos + 1);
					LResult.Append(LSourceLine);
					foreach (CompilerError LError in AResults.Errors)
						if (LError.Line == LLine)
						{
							LResult.Append(GenerateSpaces(LSourceLine, LError.Column - 1));
							LResult.AppendFormat("^{0}\r\n", LError.ErrorNumber);
						}
					LPos = LMatch + 1;
					LLine++;
				}
				else
					break;
			}
			return LResult.ToString();
		}

		/// <summary> Generate a given number of leading whitespace characters, using a given string as preference for those characters. </summary>
		/// <remarks> This ensures that the resulting string has tabs where the source line had them. </remarks>
		private static string GenerateSpaces(string ASource, int ACount)
		{
			var LSpaces = new StringBuilder(ACount);
			for (int i = 0; i < ACount; i++)
				if (ASource.Length > i && ASource[i] == '\t')
					LSpaces.Append('\t');
				else
					LSpaces.Append(' ');
			return LSpaces.ToString();
		}
		
		private ScriptBase CreateScript(Assembly AAssembly)
		{
			return (ScriptBase)Activator.CreateInstance(AAssembly.GetType("Script", true, false), new object[]{this});
		}
		
		private string GetValidName(string AName)
		{
			return AName.Replace(".", "_");
		}
		
		private bool IsValidName(string AName)
		{
			if ((AName == null) || (AName == String.Empty) || (AName == "Action") || (AName == "Host") || (AName == "Interface") || (AName == "Form"))
				return false;

			for (int LIndex = 0; LIndex < AName.Length; LIndex++)
				if 
					(
						(
							(LIndex == 0) && 
							!(Char.IsLetter(AName[LIndex]) || (AName[LIndex] == '_'))
						) || 
						(
							(LIndex != 0) && 
							!(Char.IsLetterOrDigit(AName[LIndex]) || (AName[LIndex] == '_') || (AName[LIndex] == '.'))
						)
					)
					return false;
					
			return true;
		}
		
		private void SetNodeReferences(ScriptBase AScript)
		{
			SetNodeReferences(AScript, HostNode);
		}
		
		private void SetNodeReferences(ScriptBase AScript, INode ANode)
		{
			foreach (INode LNode in ANode.Children)
			{
				if (IsValidName(LNode.Name))
					SetNodeReference(AScript, LNode);
				SetNodeReferences(AScript, LNode);
			}
		}
		
		private void SetNodeReference(ScriptBase AScript, INode ANode)
		{
			FieldInfo LField = AScript.GetType().GetField(GetValidName(ANode.Name), BindingFlags.Instance | BindingFlags.Public);
			if (LField != null)
				LField.SetValue(AScript, ANode);
		}
		
		private string BuildNodeReferences(List<string> AAssemblies)
		{
			StringBuilder LScript = new StringBuilder();
			BuildNodeReferences(AAssemblies, LScript, HostNode);
			return LScript.ToString();
		}
		
		private void BuildNodeReferences(List<string> AAssemblies, StringBuilder AScript, INode ANode)
		{
			foreach (INode LNode in ANode.Children)
			{
				if (IsValidName(LNode.Name))
					BuildNodeReference(AAssemblies, AScript, LNode);
				BuildNodeReferences(AAssemblies, AScript, LNode);
			}
		}
		
		private void BuildNodeReference(List<string> AAssemblies, StringBuilder AScript, INode ANode)
		{
			AddReferencedAssemblies(AAssemblies, ANode.GetType().Assembly);
			switch (FLanguage)
			{
				case ScriptLanguages.CSharp : AScript.AppendFormat("public {0} {1};\r\n", ANode.GetType().FullName, GetValidName(ANode.Name)); break;
				case ScriptLanguages.VisualBasic : AScript.AppendFormat("Public {0} As {1}\r\n", GetValidName(ANode.Name), ANode.GetType().FullName); break;
				default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, FLanguage);
			}
		}
		
		protected override void InternalExecute(INode ASender, EventParams AParams)
		{
			if (FScript != String.Empty)
			{
				Assembly LAssembly = GetCompiledAssembly();
				ScriptBase LScript = CreateScript(LAssembly);
				
				try
				{
					SetNodeReferences(LScript);
					LScript.Execute(ASender, AParams);
				}
				catch (AbortException)
				{
					throw;
				}
				catch (Exception LException)
				{
					throw new ClientException(ClientException.Codes.ScriptExecutionError, LException, Name);
				}
			}
		}
	}

	// The ScriptBase class could potentially be defined by each client allowing a set of "common" 
	// functions such as "ShowMessage" to work appropriately in each client
	/// <summary> Internal class from which executed scripts descend. </summary>
	public abstract class ScriptBase
	{
		public ScriptBase(IAction AAction)
		{
			FAction = AAction;
			FHost = AAction.HostNode;
			FForm = FAction.FindParent(typeof(IFormInterface)) as IFormInterface;
			FInterface = FAction.FindParent(typeof(IInterface)) as IInterface;
		}

		private IAction FAction;
		public IAction Action { get { return FAction; } }
		
		private IHost FHost;
		public IHost Host { get { return FHost; } }
		
		private IFormInterface FForm;
		public IFormInterface Form { get { return FForm; } }
		
		private IInterface FInterface;
		public IInterface Interface { get { return FInterface; } }

		public abstract void Execute(INode ASender, EventParams AParams);
	}
}
