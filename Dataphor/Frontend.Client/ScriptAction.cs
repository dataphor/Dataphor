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
using System.Linq;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Executes a client side script. </summary>
	public class ScriptAction : Action, IScriptAction
	{
		public const string SharpTemplate = 
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

		public const string VBTemplate =
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

		public const string CompileUnitToken = "#unit";
		public const string EndCompileUnitToken = "#endunit";
		public const string ClassToken = "#class";
		public const string EndClassToken = "#endclass";
		
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		// Language
		private ScriptLanguages _language = ScriptLanguages.CSharp;
		[DefaultValue(ScriptLanguages.CSharp)]
		[Description("Specified the language for the script.")]
		public ScriptLanguages Language
		{
			get { return _language; }
			set
			{
				if (_language != value)
				{
					_language = value;
					InvalidateResults();
				}
			}
		}

		// Script
		private string _script = String.Empty;
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Description(@"The script to execute.")]
		public string Script
		{
			get { return _script; }
			set
			{
				if (_script != value)
				{
					_script = (value == null ? String.Empty : value);
					InvalidateResults();
				}
			}
		}

		// ScriptDocument
		private string _scriptDocument = String.Empty;
		// TODO: Detect changes to the document and invalidate results
		[Description("A Document expression returning a form interface to be shown. Note that the ScriptAction does not detect changes made to the document once it has been successfully compiled. If you change the document, you must invalidate the cached compiled assembly by changing the value of the document expression.")]
		[DefaultValue("")]
		[Editor("Alphora.Dataphor.Dataphoria.DocumentExpressionUIEditor,Dataphoria", "System.Drawing.Design.UITypeEditor,System.Drawing")]
		[DocumentExpressionOperator("Load")]
		public string ScriptDocument
		{
			get { return _scriptDocument; }
			set
			{
				if (_scriptDocument != value)
				{
					_scriptDocument = (value == null ? String.Empty : value);
					InvalidateResults();
				}
			}
		}

		// CompileWithDebug
		private bool _compileWithDebug = false;
		[Description("When true, the script will be compiled with debug information.")]
		[DefaultValue(false)]
		public bool CompileWithDebug
		{
			get { return _compileWithDebug; }
			set { _compileWithDebug = value; }
		}

		private string _references = "";
		[Description("Set of assembly references to use when compiling the dynamic script.  One fully qualified assembly name per line.")]
		[DefaultValue("")]
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		public string References
		{
			get { return _references; }
			set
			{
				if (_references != value)
				{
					_references = (value == null ? String.Empty : value);
					InvalidateResults();
				}
			}
		}

		private static void PreprocessScript(string script, out string unit, out string classValue, out string execute)
		{
			// TODO: Put in lexor in for pre-processor (for comment/string support)

			int pos = script.IndexOf(CompileUnitToken);
			int endPos;
			if (pos >= 0)
			{
				endPos = script.IndexOf(EndCompileUnitToken);
				unit = script.Substring(pos + 5, endPos - (pos + 5));
				script = script.Substring(0, pos) + script.Substring(endPos + 8);
			}
			else
				unit = String.Empty;

			pos = script.IndexOf(ClassToken);
			if (pos >= 0)
			{
				endPos = script.IndexOf(EndClassToken);
				classValue = script.Substring(pos + 6, endPos - (pos + 6));
				script = script.Substring(0, pos) + script.Substring(endPos + 9);
			}
			else
				classValue = String.Empty;
			
			execute = script;
		}
		
		private static object _assemblyCacheLock = new Object();
		private static Dictionary<string, CompilerResults> _assemblyCache = new Dictionary<string, CompilerResults>(); // key - FSourceCode, value - FResults
		
		private CompilerResults FindCompilerResults()
		{
			if (_sourceCode == null)
				BuildSourceCode();
				
			lock (_assemblyCacheLock)
				if (_assemblyCache.ContainsKey(_sourceCode))
					return _assemblyCache[_sourceCode];

			return null;
		}
		
		private static string AssemblyReferenceName(AssemblyName LName)
		{
			// TODO : Come up with way to compile against assemblies in the GAC
			return LName.Name + ".dll";
		}
		
		private string[] GetReferencedAssemblyNames(Assembly assembly)
		{
			return assembly.GetReferencedAssemblies().Select(a => Assembly.Load(a).Location)
				.Concat(References.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(a => Assembly.Load(a).Location))
				.Concat(new[] { assembly.Location })
				.ToArray();
		}
		
		private void AddReferencedAssemblies(List<string> assemblies, Assembly assembly)
		{
			// Add all assemblies referenced by the ScriptAction itself
			string[] localAssemblies = GetReferencedAssemblyNames(assembly);
			for (int index = 0; index < localAssemblies.Length; index++)
				if (!assemblies.Contains(localAssemblies[index]))
					assemblies.Add(localAssemblies[index]);

			// Add all assemblies in the node types table
			string assemblyName;
			foreach (NodeTypeEntry entry in HostNode.Session.NodeTypeTable.Entries)
			{
				assemblyName = Assembly.Load(entry.Assembly).Location;
				if (!assemblies.Contains(assemblyName))
					assemblies.Add(assemblyName);
			}
		}
		
		private string _sourceCode;
		private List<string> _referencedAssemblies;
		private CompilerResults _results;

		private void InvalidateResults()
		{
			_results = null;
			_referencedAssemblies = null;
			_sourceCode = null;
		}

		private bool HasScript()
		{
			return _script != String.Empty || _scriptDocument != String.Empty;
		}

		private string GetScript()
		{
			if (_scriptDocument != String.Empty)
			{
				return HostNode.Pipe.RequestDocument(_scriptDocument).AsString;
			}
			
			return _script;
		}
		
		private void BuildSourceCode()
		{
			string unit;
			string classValue;
			string execute;
			PreprocessScript(GetScript(), out unit, out classValue, out execute);
			string template;

			switch (_language)
			{
				case ScriptLanguages.CSharp : template = SharpTemplate; break;
				case ScriptLanguages.VisualBasic : template = VBTemplate; break;
				default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, _language);
			}

			_referencedAssemblies = new List<string>();
			AddReferencedAssemblies(_referencedAssemblies, typeof(ScriptAction).Assembly);
			_sourceCode = String.Format
			(
				template,
				GetType().Namespace,
				unit,
				typeof(ScriptBase).FullName,
				BuildNodeReferences(_referencedAssemblies),
				classValue,
				execute
			);
		}
		
		private Assembly GetCompiledAssembly()
		{
			if (_results == null)
			{
				_results = FindCompilerResults();
				if (_results == null)
				{
					CompilerParameters parameters = new CompilerParameters();
					foreach (string stringValue in _referencedAssemblies)
						parameters.ReferencedAssemblies.Add(stringValue);
					parameters.GenerateExecutable = false;
					parameters.GenerateInMemory = true;
					parameters.IncludeDebugInformation = _compileWithDebug;
					//CompilerParameters LParameters = new CompilerParameters(LReferencedAssembliesArray, GetTempAssemblyFileName(), FCompileWithDebug);
	
					CodeDomProvider provider;
					switch (_language)
					{
						case ScriptLanguages.CSharp : provider = new CSharpCodeProvider(); break;
						case ScriptLanguages.VisualBasic : provider = new VBCodeProvider(); break;
						default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, _language);
					}

					_results = provider.CompileAssemblyFromSource(parameters, _sourceCode);
					lock (_assemblyCacheLock)
						if (!_assemblyCache.ContainsKey(_sourceCode))
							_assemblyCache.Add(_sourceCode, _results);
				}
			}

			if (_results.NativeCompilerReturnValue != 0)
				lock (_assemblyCacheLock)	// Error enumeration is not concurrent, must lock
				{
					StringBuilder errors = new StringBuilder();
					foreach (CompilerError error in _results.Errors)
						errors.AppendFormat
						(
							"{0} {1} at line ({2}) column ({3}): {4}\r\n",
							(error.IsWarning ? "Warning" : "Error"),
							error.ErrorNumber,
							error.Line,
							error.Column,
							error.ErrorText
						);
					errors.AppendFormat("Script:\r\n{0}\r\n----------End Script----------\r\n", EmbedErrorAnnotatedSourceScript(_sourceCode, _results));
					throw new ClientException(ClientException.Codes.ScriptCompilerError, errors.ToString());
				}

			return _results.CompiledAssembly;
		}

		/// <summary> Emits the given source code with the given set of error messages annotated within. </summary>
		private static string EmbedErrorAnnotatedSourceScript(string sourceCode, CompilerResults results)
		{
			var result = new StringBuilder(sourceCode.Length);
			var pos = 0;
			var line = 1;																	 
			while (true)
			{
				var match = sourceCode.IndexOf("\n", pos);
				if (match >= 0)
				{
					var sourceLine = sourceCode.Substring(pos, match - pos + 1);
					result.Append(sourceLine);
					foreach (CompilerError error in results.Errors)
						if (error.Line == line)
						{
							result.Append(GenerateSpaces(sourceLine, error.Column - 1));
							result.AppendFormat("^{0}\r\n", error.ErrorNumber);
						}
					pos = match + 1;
					line++;
				}
				else
					break;
			}
			return result.ToString();
		}

		/// <summary> Generate a given number of leading whitespace characters, using a given string as preference for those characters. </summary>
		/// <remarks> This ensures that the resulting string has tabs where the source line had them. </remarks>
		private static string GenerateSpaces(string source, int count)
		{
			var spaces = new StringBuilder(count);
			for (int i = 0; i < count; i++)
				if (source.Length > i && source[i] == '\t')
					spaces.Append('\t');
				else
					spaces.Append(' ');
			return spaces.ToString();
		}
		
		private ScriptBase CreateScript(Assembly assembly)
		{
			return (ScriptBase)Activator.CreateInstance(assembly.GetType("Script", true, false), new object[]{this});
		}
		
		private string GetValidName(string name)
		{
			return name.Replace(".", "_");
		}
		
		private bool IsValidName(string name)
		{
			if ((name == null) || (name == String.Empty) || (name == "Action") || (name == "Host") || (name == "Interface") || (name == "Form"))
				return false;

			for (int index = 0; index < name.Length; index++)
				if 
					(
						(
							(index == 0) && 
							!(Char.IsLetter(name[index]) || (name[index] == '_'))
						) || 
						(
							(index != 0) && 
							!(Char.IsLetterOrDigit(name[index]) || (name[index] == '_') || (name[index] == '.'))
						)
					)
					return false;
					
			return true;
		}
		
		private void SetNodeReferences(ScriptBase script)
		{
			SetNodeReferences(script, HostNode);
		}
		
		private void SetNodeReferences(ScriptBase script, INode node)
		{
			foreach (INode localNode in node.Children)
			{
				if (IsValidName(localNode.Name))
					SetNodeReference(script, localNode);
				SetNodeReferences(script, localNode);
			}
		}
		
		private void SetNodeReference(ScriptBase script, INode node)
		{
			FieldInfo field = script.GetType().GetField(GetValidName(node.Name), BindingFlags.Instance | BindingFlags.Public);
			if (field != null)
				field.SetValue(script, node);
		}
		
		private string BuildNodeReferences(List<string> assemblies)
		{
			StringBuilder script = new StringBuilder();
			BuildNodeReferences(assemblies, script, HostNode);
			return script.ToString();
		}
		
		private void BuildNodeReferences(List<string> assemblies, StringBuilder script, INode node)
		{
			foreach (INode localNode in node.Children)
			{
				if (IsValidName(localNode.Name))
					BuildNodeReference(assemblies, script, localNode);
				BuildNodeReferences(assemblies, script, localNode);
			}
		}
		
		private void BuildNodeReference(List<string> assemblies, StringBuilder script, INode node)
		{
			AddReferencedAssemblies(assemblies, node.GetType().Assembly);
			switch (_language)
			{
				case ScriptLanguages.CSharp : script.AppendFormat("public {0} {1};\r\n", node.GetType().FullName, GetValidName(node.Name)); break;
				case ScriptLanguages.VisualBasic : script.AppendFormat("Public {0} As {1}\r\n", GetValidName(node.Name), node.GetType().FullName); break;
				default : throw new ClientException(ClientException.Codes.UnsupportedScriptLanguage, _language);
			}
		}
		
		protected override void InternalExecute(INode sender, EventParams paramsValue)
		{
			if (HasScript())
			{
				Assembly assembly = GetCompiledAssembly();
				ScriptBase script = CreateScript(assembly);
				
				try
				{
					SetNodeReferences(script);
					script.Execute(sender, paramsValue);
				}
				catch (AbortException)
				{
					throw;
				}
				catch (Exception exception)
				{
					throw new ClientException(ClientException.Codes.ScriptExecutionError, exception, Name);
				}
			}
		}
	}

	[Description("Executes an action if the script condition evaluates to true.")]
	public class ConditionalAction : BaseConditionalAction
	{
		public const string MultiStatementExpressionTemplate = "{0}.Evaluate += () => {{ {1} }};";
		public const string SingleExpressionTemplate = "{0}.Evaluate += () => {{ return {1}; }};";

		private ScriptAction _scriptAction;
		private ScriptAction ScriptAction
		{
			get 
			{ 
				if (_scriptAction == null) 			
					_scriptAction = new ScriptAction();	 					
			
				return _scriptAction; 
			}
		}
						 		
		[Editor(typeof(Alphora.Dataphor.DAE.Client.Controls.Design.MultiLineEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[Description("The boolean expression to evaluate.  The expression can either be a single expression or a compound statement ending in an expression. The latter must include the keyword for returning the result.")]
		[DAE.Client.Design.EditorDocumentType("cs")]
		public override string Condition
		{
			get { return base.Condition; }
			set { base.Condition = value; }
		}

		// Language
		[DefaultValue(ScriptLanguages.CSharp)]
		[Description("Specified the language for the script.")]
		public ScriptLanguages Language
		{
			get { return ScriptAction.Language; }
			set { ScriptAction.Language = value; }
		}
		
		// CompileWithDebug
		[Description("When true, the script will be compiled with debug information.")]
		[DefaultValue(false)]
		public bool CompileWithDebug
		{
			get { return ScriptAction.CompileWithDebug; }
			set { ScriptAction.CompileWithDebug = value; }
		}

		/// <remarks> Only one IAction is allowed as a child action (other than the internal ScriptAction). </remarks>
		public override bool IsValidChild(Type childType)
		{
			if (_settingOwner)
				return true;
			return typeof(IAction).IsAssignableFrom(childType) && Children.Count < 1;	 			
		}	

		private bool _settingOwner;
		protected override bool EvaluateCondition()
		{
			if (String.IsNullOrEmpty(Condition))
				return false;
			
			string template = Condition.IndexOf("return ") > -1 ? MultiStatementExpressionTemplate : SingleExpressionTemplate; 
			// temporarily set Owner to provide Execute dependencies, but immediately remove to prevent ScriptAction from appearing as a dependent.
			ScriptAction.Script = String.Format(template, Name, Condition);
			
			_settingOwner = true;
			ScriptAction.Owner = this;
			_settingOwner = false; 	
			try
			{			
				ScriptAction.Execute();				
			}
			finally
			{
				ScriptAction.Owner = null;
			}
			return Evaluate();	
		}
		  
		public delegate bool OnEvaluate();
		public event OnEvaluate Evaluate;
	}

	// The ScriptBase class could potentially be defined by each client allowing a set of "common" 
	// functions such as "ShowMessage" to work appropriately in each client
	/// <summary> Internal class from which executed scripts descend. </summary>
	public abstract class ScriptBase
	{
		public ScriptBase(IAction action)
		{
			_action = action;
			_host = action.HostNode;
			_form = _action.FindParent(typeof(IFormInterface)) as IFormInterface;
			_interface = _action.FindParent(typeof(IInterface)) as IInterface;
		}

		private IAction _action;
		public IAction Action { get { return _action; } }
		
		private IHost _host;
		public IHost Host { get { return _host; } }
		
		private IFormInterface _form;
		public IFormInterface Form { get { return _form; } }
		
		private IInterface _interface;
		public IInterface Interface { get { return _interface; } }

		public abstract void Execute(INode sender, EventParams paramsValue);
	}
}
