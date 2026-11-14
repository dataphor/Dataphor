using System;
using System.IO;
using System.Diagnostics;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;

class Program
{
	static int Main(string[] args)
	{
		AppDomain.CurrentDomain.FirstChanceException += (o, e) => { if (Environment.GetEnvironmentVariable("SMOKE_TRACE") == "1") Console.WriteLine("  [first-chance] " + e.Exception.GetType().Name + ": " + e.Exception.Message.Split((char)10)[0] + " @ " + new System.Diagnostics.StackTrace(e.Exception, true).GetFrame(0)); };
		string root = args[0];
		string store = args.Length > 1 ? args[1] : "sqlite";
		string inst = Path.Combine(root, "instance-" + store);
		string libs = Path.Combine(root, "libraries");
		if (Directory.Exists(inst)) Directory.Delete(inst, true);
		Directory.CreateDirectory(libs);
		var cfg = new ServerConfiguration();
		cfg.Name = "Smoke";
		cfg.PortNumber = 8061;
		cfg.LibraryDirectories = libs;
		cfg.InstanceDirectory = inst;
		cfg.CatalogStoreClassName = "Alphora.Dataphor.DAE.Store.SQLite.SQLiteStore,Alphora.Dataphor.DAE.SQLite";
		cfg.CatalogStoreConnectionString = @"Data Source=%CatalogPath%\DAECatalog" + (Environment.GetEnvironmentVariable("SMOKE_CS") ?? "");
		var server = new Server();
		cfg.ApplyTo(server);
		var sw = Stopwatch.StartNew();
		server.Start();
		Console.WriteLine("Server started in {0} ms on {1}", sw.ElapsedMilliseconds, System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
		try
		{
			IServerSession session = ((IServer)server).Connect(new SessionInfo("Admin", ""));
			IServerProcess process = session.StartProcess(new ProcessInfo(session.SessionInfo));
			string[] exprs = {
				"1 + 1",
				"'Hello' + ' ' + 'D4'",
				"Count(System.Operators)",
				"Count(System.ScalarTypes)",
				"Sum(X from table { row { 1 X }, row { 2 X }, row { 3 X } })",
			};
			foreach (var e in exprs)
			{
				IServerExpressionPlan plan = process.PrepareExpression(e, null);
				try
				{
					var value = plan.Evaluate(null);
					Console.WriteLine("  {0}  =>  {1}", e, value is Alphora.Dataphor.DAE.Runtime.Data.IDataValue ? ((Alphora.Dataphor.DAE.Runtime.Data.IDataValue)value).AsNative : value);
				}
				finally { process.UnprepareExpression(plan); }
			}
			process.ExecuteScript(@"
				create table SmokeTest { ID : Integer, Name : String, key { ID } };
				insert table { row { 1 ID, 'one' Name }, row { 2 ID, 'two' Name } } into SmokeTest;
				update SmokeTest set { Name := Name + '!' } where ID = 2;
				create view SmokeView SmokeTest where ID > 1;
				create operator SmokeOp(const A : Integer) : Integer
				begin
					result := A * 2;
				end;
			");
			foreach (var e in new[] { "Count(SmokeTest)", "(SmokeView[2]).Name", "SmokeOp(21)" })
			{
				IServerExpressionPlan plan = process.PrepareExpression(e, null);
				try { var value = plan.Evaluate(null); Console.WriteLine("  {0}  =>  {1}", e, value is Alphora.Dataphor.DAE.Runtime.Data.IDataValue ? ((Alphora.Dataphor.DAE.Runtime.Data.IDataValue)value).AsNative : value); }
				finally { process.UnprepareExpression(plan); }
			}
			session.StopProcess(process);
			((IServer)server).Disconnect(session);

			int threads = args.Length > 2 ? int.Parse(args[2]) : 8;
			int iters = args.Length > 3 ? int.Parse(args[3]) : 15;
			var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
			var csw = Stopwatch.StartNew();
			var ts = new System.Collections.Generic.List<System.Threading.Thread>();
			for (int t = 0; t < threads; t++)
			{
				int id = t;
				var th = new System.Threading.Thread(() =>
				{
					try
					{
						IServerSession s2 = ((IServer)server).Connect(new SessionInfo("Admin", ""));
						IServerProcess p2 = s2.StartProcess(new ProcessInfo(s2.SessionInfo));
						for (int i = 0; i < iters; i++)
						{
							try
							{
								p2.ExecuteScript(String.Format(@"
									create table T{0}_{1} {{ ID : Integer, Name : String, key {{ ID }} }};
									insert table {{ row {{ 1 ID, 'a' Name }} }} into T{0}_{1};
									create view V{0}_{1} T{0}_{1} where ID > 0;
									create operator O{0}_{1}(const A : Integer) : Integer
									begin
										result := A + Count(V{0}_{1});
									end;
								", id, i));
								var plan = p2.PrepareExpression(String.Format("O{0}_{1}(1) + Count(System.Operators where Name = 'System.iAddition')", id, i), null);
								plan.Evaluate(null);
								p2.UnprepareExpression(plan);
								p2.ExecuteScript(String.Format("drop operator O{0}_{1}(Integer); drop view V{0}_{1}; drop table T{0}_{1};", id, i));
							}
							catch (Exception e)
							{
								var inner = e; while (inner.InnerException != null) inner = inner.InnerException;
								errors.Add(String.Format("t{0} i{1}: {2}: {3}", id, i, inner.GetType().Name, inner.Message.Replace((char)13, (char)32).Replace((char)10, (char)32)));
							}
						}
						s2.StopProcess(p2);
						((IServer)server).Disconnect(s2);
					}
					catch (Exception e) { errors.Add(String.Format("t{0} FATAL: {1}", id, e)); }
				});
				ts.Add(th); th.Start();
			}
			foreach (var th in ts) th.Join();
			Console.WriteLine("Concurrency: {0} threads x {1} iterations of DDL+query in {2} ms; {3} errors", threads, iters, csw.ElapsedMilliseconds, errors.Count);
			var groups = new System.Collections.Generic.Dictionary<string,int>();
			foreach (var e in errors) { var k = e.Substring(e.IndexOf(": ") + 2); k = System.Text.RegularExpressions.Regex.Replace(k, "[TVO][0-9]+_[0-9]+", "X"); int c; groups.TryGetValue(k, out c); groups[k] = c + 1; }
			foreach (var kv in groups) Console.WriteLine("   x{0}: {1}", kv.Value, kv.Key.Length > 300 ? kv.Key.Substring(0, 300) : kv.Key);

			// K3 reproduction: the catalog's operator resolution cache is shared by all sessions. The compiler adds to it
			// under lock(cache) and clears entries by name while compiling create/drop operator with no lock held.
			// Unsynchronized, this fails with "Collection was modified" within milliseconds; expected: 0 errors.
			var cache = new Alphora.Dataphor.DAE.Compiling.OperatorResolutionCache();
			var cachePath = new Alphora.Dataphor.DAE.Schema.NameResolutionPath();
			var cacheSig = new Alphora.Dataphor.DAE.Schema.Signature(new Alphora.Dataphor.DAE.Schema.SignatureElement[0]);
			for (int i = 0; i < 2000; i++) cache.Add(new Alphora.Dataphor.DAE.Compiling.OperatorBindingContext(null, "System.Seed" + i, cachePath, cacheSig, false));
			int cacheErrors = 0;
			string cacheError = null;
			var cacheStop = DateTime.UtcNow.AddMilliseconds(1000);
			var cts = new System.Collections.Generic.List<System.Threading.Thread>();
			for (int t = 0; t < 8; t++)
			{
				int id = t;
				var th = new System.Threading.Thread(() =>
				{
					for (int n = 0; DateTime.UtcNow < cacheStop; n++)
					{
						try
						{
							string name = "O" + id + "_" + n;
							if (id % 2 == 0)
								lock (cache) cache.Add(new Alphora.Dataphor.DAE.Compiling.OperatorBindingContext(null, name, cachePath, cacheSig, false));
							else
								cache.Clear(name);
						}
						catch (Exception e)
						{
							if (System.Threading.Interlocked.Increment(ref cacheErrors) == 1) cacheError = e.GetType().Name + ": " + e.Message;
						}
					}
				});
				cts.Add(th); th.Start();
			}
			foreach (var th in cts) th.Join();
			Console.WriteLine("Resolution cache: 8 threads x 1000 ms of add/clear; {0} errors{1}", cacheErrors, cacheError == null ? "" : " (first: " + cacheError + ")");
		}
		finally
		{
			server.Stop();
		}
		Console.WriteLine("Server stopped cleanly. Total {0} ms", sw.ElapsedMilliseconds);
		return 0;
	}
}
