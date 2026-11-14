using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;

class Program
{
	static string Flat(Exception e)
	{
		var inner = e; while (inner.InnerException != null) inner = inner.InnerException;
		var m = e.Message.Replace((char)13, (char)32).Replace((char)10, (char)32);
		var im = inner == e ? "" : " <= " + inner.GetType().Name + ": " + inner.Message.Replace((char)13, (char)32).Replace((char)10, (char)32);
		var s = e.GetType().Name + ": " + m + im;
		return s.Length > 330 ? s.Substring(0, 330) : s;
	}

	static void Register(IServer isrv, string lib)
	{
		var s0 = isrv.Connect(new SessionInfo("Admin", ""));
		var p0 = s0.StartProcess(new ProcessInfo(s0.SessionInfo));
		try { p0.ExecuteScript("EnsureLibraryRegistered('" + lib + "');"); Console.WriteLine("REGISTERED " + lib); }
		catch (Exception e) { Console.WriteLine("REGISTER-FAIL " + lib + ": " + Flat(e)); }
		try { s0.StopProcess(p0); isrv.Disconnect(s0); } catch { }
	}

	static int Main(string[] args)
	{
		string root = args[0];
		string testFile = args[1];
		bool withBase = args.Length > 2 && args[2] == "base";
		string inst = Path.Combine(root, "instance-cov");
		string libs = Path.Combine(root, "covlibs");
		if (Directory.Exists(inst)) Directory.Delete(inst, true);
		var cfg = new ServerConfiguration();
		cfg.Name = "Cov";
		cfg.PortNumber = 8061;
		cfg.LibraryDirectories = libs;
		cfg.InstanceDirectory = inst;
		cfg.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
		cfg.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog;Journal Mode=WAL;BusyTimeout=30000";
		var server = new Server();
		cfg.ApplyTo(server);
		server.Start();
		IServer isrv = server;
		try
		{
			foreach (var lib in withBase ? new[] { "Frontend", "TestFramework.Coverage.Base" } : new[] { "Frontend" })
				Register(isrv, lib);
			int pass = 0, fail = 0, timeout = 0, ignored = 0;
			foreach (var line in File.ReadAllLines(testFile))
			{
				var m = Regex.Match(line, @"ExecuteScript\(""Coverage\.Scripts"", ""([A-Za-z0-9_.]+)""\)");
				if (!m.Success || line.TrimStart().StartsWith("//")) continue;
				string name = m.Groups[1].Value;
				bool isIgnored = line.Contains("Ignore(");
				if (isIgnored && !(args.Length > 3 && args[3] == "all")) { ignored++; continue; }
				Exception error = null;
				var sw = Stopwatch.StartNew();
				IServerSession s = null; IServerProcess p = null;
				var t = new Thread(() =>
				{
					try
					{
						s = isrv.Connect(new SessionInfo("Admin", ""));
						p = s.StartProcess(new ProcessInfo(s.SessionInfo));
						p.ExecuteScript(String.Format("ExecuteScript('Coverage.Scripts', '{0}');", name));
					}
					catch (Exception e) { error = e; }
				}, 16 * 1024 * 1024);
				t.IsBackground = true;
				t.Start();
				bool done = t.Join(TimeSpan.FromSeconds(240));
				if (!done) { timeout++; Console.WriteLine("TIMEOUT {0}{1}", name, isIgnored ? " [ignored-in-nunit]" : ""); continue; }
				try { if (p != null) s.StopProcess(p); if (s != null) isrv.Disconnect(s); } catch (Exception e) { Console.WriteLine("  cleanup: " + Flat(e)); }
				if (error == null) { pass++; Console.WriteLine("PASS {0} ({1} ms){2}", name, sw.ElapsedMilliseconds, isIgnored ? " [ignored-in-nunit]" : ""); }
				else { fail++; Console.WriteLine("FAIL {0} ({1} ms){2}: {3}", name, sw.ElapsedMilliseconds, isIgnored ? " [ignored-in-nunit]" : "", Flat(error)); }
			}
			Console.WriteLine("SUMMARY pass={0} fail={1} timeout={2} skipped-ignored={3}", pass, fail, timeout, ignored);
			if (timeout > 0) Environment.Exit(2);
			// Libraries with private package dependencies, registered from the runtime layout's copy. After the scripts, so that
			// these registrations cannot change a script's result.
			foreach (var lib in new[] { "Connection", "System.Internet" })
				Register(isrv, lib);
		}
		finally
		{
			try { server.Stop(); } catch (Exception e) { Console.WriteLine("stop: " + Flat(e)); }
		}
		return 0;
	}
}
