// Throwaway check that the SDK-style core projects load with their legacy embedded-resource names and
// that an in-process engine starts and evaluates D4. Run in Debug and Release:
//     dotnet run --project tools/net-spike/EngineCheck -c Debug
// Deleted with tools/net-spike once test-harness-inprocess provides the permanent tests.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Server;

static class Program
{
	// Computed from the legacy naming rules: root namespace + folder + file name; a .resx with DependentUpon
	// metadata takes the name of the first class in that .cs file.
	static readonly Dictionary<string, string[]> ExpectedResources = new Dictionary<string, string[]>
	{
		["Alphora.Dataphor"] = new[]
		{
			"Alphora.Dataphor.Disposable.resources",
			"Alphora.Dataphor.BaseException.resources",
			"Alphora.Dataphor.BOP.BOPException.resources",
		},
		["Alphora.Dataphor.Windows"] = new string[0],
		["Alphora.Dataphor.DAE"] = new[]
		{
			"Compiling.CompilerException", "Connection.ConnectionException",
			"Device.ApplicationTransaction.ApplicationTransactionException", "Device.Catalog.CatalogException",
			"Device.Memory.MemoryDeviceException", "Device.DeviceException", "Language.LanguageException",
			"Language.LexerException", "Language.ParserException", "Language.SyntaxException",
			"Runtime.Data.IndexException", "Runtime.Data.ScanException", "Runtime.RuntimeException",
			"Schema.SchemaException", "Server.ServerException", "Store.StoreException", "Streams.ConveyorException",
			"Streams.StreamsException", "Strings",
		}
			.Select(name => "Alphora.Dataphor.DAE." + name + ".resources")
			.Concat(new[]
			{
				"Alphora.Dataphor.DAE.Schema.DataTypes.d4",
				"Alphora.Dataphor.DAE.Schema.SystemCatalog.d4",
				"Alphora.Dataphor.DAE.Debug.Debug.d4",
			})
			.ToArray(),
		["AlphoraDataphorCommon"] = new string[0],
		["Alphora.Dataphor.DAE.NativeCLI"] = new string[0],
		["AlphoraDataphorClient"] = new string[0],
		["Alphora.Dataphor.DAE.Device.SQL"] = new[] { "Alphora.Dataphor.DAE.Device.SQL.SQLException.resources" },
		["System.Platform"] = new[] { "Alphora.Dataphor.Libraries.System.Platform.Strings.resources" },
		["System.Internet"] = new string[0],
		["System.Integration"] = new[] { "Alphora.Dataphor.Libraries.System.Integration.Strings.resources" },
	};

	static readonly List<string> Failures = new List<string>();

	static int Main()
	{
		// The engine rewraps exceptions and loses their origin; ENGINECHECK_TRACE=1 shows where each one was thrown.
		if (Environment.GetEnvironmentVariable("ENGINECHECK_TRACE") == "1")
			AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
				Console.WriteLine("  [first chance] {0}: {1}\n{2}", e.Exception.GetType().Name, e.Exception.Message, new System.Diagnostics.StackTrace(1, false));

		Step("embedded resource names", CheckResourceNames);
		Step("System.Platform string table", CheckPlatformStrings);
		object operatorCount = null;
		object scalarTypeCount = null;
		Step("engine", () => RunEngine(out operatorCount, out scalarTypeCount));

		#if DEBUG
		const string configuration = "Debug";
		#else
		const string configuration = "Release";
		#endif
		Console.WriteLine();
		Console.WriteLine("Configuration: {0}", configuration);
		Console.WriteLine("Runtime: {0}", RuntimeInformation.FrameworkDescription);
		Console.WriteLine("Operators: {0}, scalar types: {1}", operatorCount ?? "n/a", scalarTypeCount ?? "n/a");
		if (Failures.Count == 0)
		{
			Console.WriteLine("RESULT: pass");
			return 0;
		}
		Console.WriteLine("RESULT: {0} failure(s)", Failures.Count);
		foreach (string failure in Failures)
			Console.WriteLine("  - {0}", failure);
		return 1;
	}

	static void Step(string name, Action action)
	{
		Console.WriteLine();
		Console.WriteLine("== {0}", name);
		try
		{
			action();
		}
		catch (Exception exception)
		{
			Fail(name, exception);
		}
	}

	static void Fail(string what, Exception exception)
	{
		Fail(what + ": " + exception.GetType().FullName + ": " + exception.Message);
		Console.WriteLine(exception);
	}

	static void Fail(string message)
	{
		Failures.Add(message);
		Console.WriteLine("FAIL {0}", message);
	}

	static void CheckResourceNames()
	{
		foreach (var entry in ExpectedResources)
		{
			try
			{
				string[] actual = Assembly.Load(entry.Key).GetManifestResourceNames().OrderBy(n => n, StringComparer.Ordinal).ToArray();
				string[] expected = entry.Value.OrderBy(n => n, StringComparer.Ordinal).ToArray();
				Console.WriteLine("{0} ({1}):", entry.Key, actual.Length);
				foreach (string name in actual)
					Console.WriteLine("    {0}", name);
				string[] missing = expected.Except(actual).ToArray();
				string[] extra = actual.Except(expected).ToArray();
				if (missing.Length > 0 || extra.Length > 0)
					Fail(String.Format("{0} resources differ; missing [{1}], unexpected [{2}]", entry.Key, String.Join(", ", missing), String.Join(", ", extra)));
			}
			catch (Exception exception)
			{
				Fail("load " + entry.Key, exception);
			}
		}
	}

	static void CheckPlatformStrings()
	{
		// System.Integration has a type of the same name, so resolve it in System.Platform by name.
		Type strings = Type.GetType("Alphora.Dataphor.Libraries.System.Integration.Strings, System.Platform", true);
		MethodInfo get = strings.GetMethod("Get", new[] { typeof(string) });
		const string expected = "{0} Rows Copied, {1} Rows Failed";
		string actual;
		try
		{
			actual = (string)get.Invoke(null, new object[] { "Results" });
		}
		catch (TargetInvocationException exception)
		{
			throw exception.InnerException;
		}
		Console.WriteLine("Strings.Get(\"Results\") => {0}", actual);
		if (actual != expected)
			Fail(String.Format("System.Platform Strings.Get returned \"{0}\", expected \"{1}\"", actual, expected));
	}

	static void RunEngine(out object operatorCount, out object scalarTypeCount)
	{
		operatorCount = null;
		scalarTypeCount = null;
		using (Engine engine = new Engine())
		{
			engine.Name = "EngineCheck";
			engine.Start();
			Console.WriteLine("Engine started");
			// A bare engine has no General library (SessionInfo's default), so default to System as LocalSession does.
			IServerSession session = ((IServer)engine).Connect(new SessionInfo("Admin", "", Engine.SystemLibraryName));
			try
			{
				IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
				try
				{
					RegisterSystemLibrary(process);
					Evaluate(process, "1 + 1", 2);
					Evaluate(process, "'Hello' + ' ' + 'D4'", "Hello D4");
					// Count(System.Operators) and Count(System.ScalarTypes) need the catalog tables, which the skipped
					// part of SystemCatalog.d4 creates; count the engine's in-memory catalog instead.
					operatorCount = CountCatalog<Alphora.Dataphor.DAE.Schema.Operator>(engine, "operators");
					scalarTypeCount = CountCatalog<Alphora.Dataphor.DAE.Schema.ScalarType>(engine, "scalar types");
					Evaluate(process, "Sum(X from table { row { 1 X }, row { 2 X }, row { 3 X } })", 6);
					CheckSyntaxError(process);
				}
				finally
				{
					session.StopProcess(process);
				}
			}
			finally
			{
				((IServer)engine).Disconnect(session);
			}
		}
		Console.WriteLine("Engine stopped and disposed");
	}

	// On its own the engine is the client-side catalog cache (IsEngine is true), which never registers the System
	// library: Server does that (Engine.RegisterCatalog), and its scripts name classes that only the Server assembly
	// registers. Until Server is converted, run the two scripts batch by batch, loaded by their resource names, and
	// skip the batches that need those classes; any other error is a failure.
	static void RegisterSystemLibrary(IServerProcess process)
	{
		foreach (string resourceName in new[] { "Alphora.Dataphor.DAE.Schema.SystemCatalog.d4", "Alphora.Dataphor.DAE.Debug.Debug.d4" })
		{
			string text;
			using (var reader = new System.IO.StreamReader(typeof(Engine).Assembly.GetManifestResourceStream(resourceName)))
				text = reader.ReadToEnd();
			IServerScript script = process.PrepareScript(text);
			try
			{
				int executed = 0;
				var skipped = new List<string>();
				for (int index = 0; index < script.Batches.Count; index++)
				{
					IServerBatch batch = script.Batches[index];
					try
					{
						batch.Execute(null);
						executed++;
					}
					catch (Exception exception)
					{
						string message = exception.Message;
						if (message.Contains("Registered class \""))
							skipped.Add(String.Format("line {0}: {1}", batch.Line, message));
						else
							Fail(String.Format("{0} batch at line {1}", resourceName, batch.Line), exception);
					}
				}
				Console.WriteLine("  {0}: {1} batches run, {2} skipped (need the Server assembly)", resourceName, executed, skipped.Count);
				foreach (string line in skipped)
					Console.WriteLine("    skipped {0}", line);
			}
			finally
			{
				process.UnprepareScript(script);
			}
		}
	}

	static int CountCatalog<T>(Engine engine, string what)
	{
		int count = engine.Catalog.OfType<T>().Count();
		Console.WriteLine("  {0} in the catalog: {1}", what, count);
		if (count == 0)
			Fail("no " + what + " in the catalog");
		return count;
	}

	static void Evaluate(IServerProcess process, string expression, object expected)
	{
		try
		{
			IServerExpressionPlan plan = process.PrepareExpression(expression, null);
			try
			{
				IDataValue value = plan.Evaluate(null);
				object result = value == null ? null : value.AsNative;
				Console.WriteLine("  {0}  =>  {1}", expression, result);
				if (!expected.Equals(result))
					Fail(String.Format("{0} evaluated to {1}, expected {2}", expression, result, expected));
			}
			finally
			{
				process.UnprepareExpression(plan);
			}
		}
		catch (Exception exception)
		{
			Fail("evaluate " + expression, exception);
		}
	}

	// A message found by name in the parser's .resx shows that its resource name survived the conversion.
	static void CheckSyntaxError(IServerProcess process)
	{
		const string statement = "select 1 +;";
		try
		{
			IServerStatementPlan plan = process.PrepareStatement(statement, null);
			try
			{
				// Prepare records compile errors in the plan's messages; CheckCompiled raises them.
				plan.CheckCompiled();
			}
			finally
			{
				process.UnprepareStatement(plan);
			}
			Fail(String.Format("preparing \"{0}\" did not raise an error", statement));
		}
		catch (Exception exception)
		{
			bool sawParserMessage = false;
			for (Exception current = exception; current != null; current = current.InnerException)
			{
				DataphorException dataphorException = current as DataphorException;
				Console.WriteLine("  {0} {1}: {2}", current.GetType().Name, dataphorException == null ? "" : dataphorException.Code.ToString(), current.Message);
				if (current.Message.StartsWith("DataphorException: Message ("))
					Fail("error message not found in resources: " + current.Message);
				if (current is Alphora.Dataphor.DAE.Language.ParserException)
					sawParserMessage = true;
			}
			if (!sawParserMessage)
				Fail("syntax error raised no ParserException: " + exception.GetType().FullName);
		}
	}
}
