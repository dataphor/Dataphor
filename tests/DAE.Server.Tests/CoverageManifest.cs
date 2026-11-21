/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	public enum CoverageStatus
	{
		/// <summary>The script must succeed.</summary>
		Pass,
		/// <summary>The script must fail, with an error text that contains the entry's error substring.</summary>
		KnownFailure,
		/// <summary>The script's result was observed to vary between identical runs: it runs, and never fails the run.</summary>
		Unstable,
		/// <summary>The script is a step that other scripts run (its entry's partOf); it is not run on its own.</summary>
		Fragment
	}

	/// <summary>One manifest entry: what running one coverage script must do. The fields are described on <see cref="CoverageManifest"/>.</summary>
	public sealed class CoverageEntry
	{
		[JsonPropertyName("script")] public string Script { get; set; }
		[JsonPropertyName("status")] public string StatusText { get; set; }
		[JsonPropertyName("ticket")] public string Ticket { get; set; }
		[JsonPropertyName("error")] public string Error { get; set; }
		[JsonPropertyName("timeoutSeconds")] public int? TimeoutSeconds { get; set; }
		[JsonPropertyName("preamble")] public string Preamble { get; set; }
		[JsonPropertyName("batches")] public bool? Batches { get; set; }
		[JsonPropertyName("linkedServer")] public bool? LinkedServer { get; set; }
		[JsonPropertyName("register")] public string[] Register { get; set; }
		[JsonPropertyName("partOf")] public string[] PartOf { get; set; }
		[JsonPropertyName("note")] public string Note { get; set; }

		/// <summary>Valid only once the manifest has validated the entry.</summary>
		[JsonIgnore] public CoverageStatus Status => ParseStatus(StatusText).Value;

		[JsonIgnore] public int Timeout => TimeoutSeconds ?? CoverageManifest.DefaultTimeoutSeconds;

		internal static CoverageStatus? ParseStatus(string text)
		{
			switch (text)
			{
				case "pass": return CoverageStatus.Pass;
				case "known-failure": return CoverageStatus.KnownFailure;
				case "unstable": return CoverageStatus.Unstable;
				case "fragment": return CoverageStatus.Fragment;
				default: return null;
			}
		}
	}

	/// <summary>
	/// The coverage manifest, tests/DAE.Server.Tests/coverage-manifest.json, embedded in the test assembly: one entry for each
	/// D4 script in the TestFramework.Coverage.Scripts library, saying what running it must do. A script with no entry, or an
	/// entry with no script, fails the run.
	/// </summary>
	/// <remarks>
	/// <para>Format: strict JSON (no comments, no trailing commas), UTF-8 without a byte-order mark, LF line endings; one array,
	/// one entry (an object) per line, sorted by <c>script</c> in ordinal order. Fields, and nothing else:</para>
	/// <list type="bullet">
	/// <item><c>script</c> (always): the file name in Documents/ without ".d4", exact case.</item>
	/// <item><c>status</c> (always): <c>pass</c>, <c>known-failure</c>, <c>unstable</c> or <c>fragment</c> (see
	/// <see cref="CoverageStatus"/>). Use <c>unstable</c> only with evidence that the result varies between identical runs, and
	/// <c>fragment</c> only for a script that fails on its own because it needs what other scripts run before it; a helper
	/// script that passes on its own stays <c>pass</c>.</item>
	/// <item><c>partOf</c> (<c>fragment</c> only, required there): a non-empty array of the scripts that run the fragment.
	/// Each must have an entry, not be the fragment itself and not be a <c>fragment</c>; discovery also fails the fragment's
	/// case unless each one's file text mentions the fragment's name as a D4 string literal.</item>
	/// <item><c>ticket</c> (required on <c>known-failure</c> and <c>unstable</c>, forbidden otherwise): the slug of the ticket that owns the cause,
	/// lower-case and hyphenated, without a sequence prefix.</item>
	/// <item><c>error</c> (<c>known-failure</c> only, required there): an ordinal, case-sensitive substring the actual error
	/// text must contain. Never the full text, and never a part that changes between runs (program identifiers, the line
	/// positions of program locators, timings).</item>
	/// <item><c>timeoutSeconds</c> (optional): 1 to 600, default 120.</item>
	/// <item><c>preamble</c> (optional): D4 run on the script's process before the script; a transaction it leaves open is
	/// committed before the script starts.</item>
	/// <item><c>batches</c> (optional, <c>true</c> only): run the script's text batch by batch on the script's process, one
	/// implicit transaction per batch, as Dataphoria runs a script, instead of nested in one ExecuteScript statement whose
	/// transaction stays open for the whole script. The text is read from the script server's own copy of the scripts library.
	/// Use it only for a script that depends on batch boundaries (one that leaves an application transaction it changed, or
	/// that must see a batch's work committed - a block run as another user through <c>ExecuteAs</c>, for instance, runs in
	/// a session of its own, which sees only committed catalog changes), and say why in <c>note</c>.</item>
	/// <item><c>linkedServer</c> (optional, <c>true</c> only): a second Dataphor server, in a child process, named
	/// <c>Dataphor</c> and hosting a listener on a dynamically chosen port, started before the script and stopped after it;
	/// while it runs, this process's client-side listener port
	/// (<see cref="Alphora.Dataphor.DAE.Contracts.DataphorServiceUtility.ListenerPortNumber"/>) points at it. Use it only for
	/// a script that creates a server link with no port override - such a link resolves to the instance named
	/// <c>Dataphor</c> on <c>localhost</c> through a listener - and say why in <c>note</c>.</item>
	/// <item><c>register</c> (optional): libraries registered in the script's server before it, each one of
	/// <see cref="CoverageScripts.Libraries"/>.</item>
	/// <item><c>note</c> (optional): free text for the next reader.</item>
	/// </list>
	/// <para>On a <c>fragment</c> entry only <c>script</c>, <c>status</c>, <c>partOf</c> and <c>note</c> are allowed.</para>
	/// </remarks>
	public sealed class CoverageManifest
	{
		/// <summary>The LogicalName of the embedded resource in DAE.Server.Tests.csproj.</summary>
		public const string ResourceName = "coverage-manifest.json";

		public const int DefaultTimeoutSeconds = 120;
		public const int MinTimeoutSeconds = 1;
		public const int MaxTimeoutSeconds = 600;

		private static readonly Regex TicketSlug = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$");

		private static readonly JsonSerializerOptions Options =
			new JsonSerializerOptions
			{
				UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
				AllowDuplicateProperties = false
			};

		private CoverageManifest(List<CoverageEntry> entries, List<string> problems)
		{
			Entries = problems.Count == 0 ? entries : new List<CoverageEntry>();
			Problems = problems;
		}

		/// <summary>Empty when the manifest has problems.</summary>
		public IReadOnlyList<CoverageEntry> Entries { get; }

		/// <summary>Every problem found; the manifest is usable only when there are none.</summary>
		public IReadOnlyList<string> Problems { get; }

		/// <summary>Reads the manifest embedded in the test assembly.</summary>
		public static CoverageManifest Load(ICollection<string> libraries)
		{
			using (Stream stream = typeof(CoverageManifest).Assembly.GetManifestResourceStream(ResourceName))
			{
				if (stream == null)
					return new CoverageManifest(null, new List<string> { String.Format("The test assembly has no embedded resource \"{0}\".", ResourceName) });
				return Parse(stream, libraries);
			}
		}

		public static CoverageManifest Parse(Stream json, ICollection<string> libraries)
		{
			List<CoverageEntry> entries;
			try
			{
				entries = JsonSerializer.Deserialize<List<CoverageEntry>>(json, Options);
			}
			catch (JsonException exception)
			{
				return new CoverageManifest(null, new List<string> { exception.Message });
			}

			if (entries == null)
				return new CoverageManifest(null, new List<string> { "The manifest is null; it must be a JSON array of entries." });

			return new CoverageManifest(entries, Validate(entries, libraries));
		}

		private static List<string> Validate(List<CoverageEntry> entries, ICollection<string> libraries)
		{
			List<string> problems = new List<string>();
			HashSet<string> scripts = new HashSet<string>(StringComparer.Ordinal);
			Dictionary<string, CoverageStatus?> statuses = new Dictionary<string, CoverageStatus?>(StringComparer.Ordinal);
			foreach (CoverageEntry entry in entries)
				if (entry?.Script != null)
					statuses.TryAdd(entry.Script, CoverageEntry.ParseStatus(entry.StatusText));
			string previous = null;
			for (int index = 0; index < entries.Count; index++)
			{
				CoverageEntry entry = entries[index];
				if (entry == null)
				{
					problems.Add(String.Format("Entry {0} is null.", index + 1));
					continue;
				}

				string where = String.Format("Entry {0} ({1})", index + 1, entry.Script ?? "no script");
				void Problem(string text) => problems.Add(where + ": " + text);

				if (String.IsNullOrEmpty(entry.Script))
					Problem("\"script\" is missing or empty.");
				else
				{
					if (!scripts.Add(entry.Script))
						Problem("the script has another entry.");
					else if (previous != null && String.CompareOrdinal(previous, entry.Script) > 0)
						Problem(String.Format("out of order: entries are sorted by script in ordinal order, and it follows \"{0}\".", previous));
					previous = entry.Script;
				}

				CoverageStatus? status = CoverageEntry.ParseStatus(entry.StatusText);
				if (status == null)
					Problem(entry.StatusText == null ? "\"status\" is missing." : String.Format("unknown status \"{0}\"; expected pass, known-failure, unstable or fragment.", entry.StatusText));

				if (status == CoverageStatus.Fragment)
				{
					ValidateFragment(entry, statuses, Problem);
					continue;
				}

				if (entry.PartOf != null)
					Problem("\"partOf\" is allowed only on a fragment entry.");

				if (status == CoverageStatus.Pass)
				{
					if (entry.Ticket != null)
						Problem("\"ticket\" is not allowed on a pass entry.");
				}
				else if (status != null)
				{
					if (entry.Ticket == null)
						Problem("\"ticket\" is required unless the status is pass.");
					else if (!TicketSlug.IsMatch(entry.Ticket))
						Problem(String.Format("\"ticket\" \"{0}\" is not a ticket slug (lower-case words joined by hyphens, no sequence prefix or .md).", entry.Ticket));
				}

				if (status == CoverageStatus.KnownFailure)
				{
					if (entry.Error == null)
						Problem("\"error\" is required on a known-failure entry.");
					else if (entry.Error.Length == 0)
						Problem("\"error\" is empty; it would match any failure.");
				}
				else if (status != null && entry.Error != null)
					Problem("\"error\" is allowed only on a known-failure entry.");

				if (entry.TimeoutSeconds.HasValue && (entry.TimeoutSeconds < MinTimeoutSeconds || entry.TimeoutSeconds > MaxTimeoutSeconds))
					Problem(String.Format("\"timeoutSeconds\" is {0}; it must be from {1} to {2}.", entry.TimeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds));

				if (entry.Batches == false)
					Problem("\"batches\" is false; leave the field off for the default (the script runs nested in one statement).");

				if (entry.LinkedServer == false)
					Problem("\"linkedServer\" is false; leave the field off for the default (no second server is started).");

				if (entry.Register != null)
					foreach (string library in entry.Register)
						if (library == null || !libraries.Contains(library))
							Problem(String.Format("\"register\" names \"{0}\", which is not one of the libraries copied for the coverage scripts ({1}).", library, String.Join(", ", libraries)));
			}
			return problems;
		}

		private static void ValidateFragment(CoverageEntry entry, Dictionary<string, CoverageStatus?> statuses, Action<string> problem)
		{
			if (entry.Ticket != null)
				problem("\"ticket\" is not allowed on a fragment entry.");
			if (entry.Error != null)
				problem("\"error\" is not allowed on a fragment entry.");
			if (entry.TimeoutSeconds.HasValue)
				problem("\"timeoutSeconds\" is not allowed on a fragment entry; it is not run.");
			if (entry.Preamble != null)
				problem("\"preamble\" is not allowed on a fragment entry; it is not run.");
			if (entry.Batches.HasValue)
				problem("\"batches\" is not allowed on a fragment entry; it is not run.");
			if (entry.LinkedServer.HasValue)
				problem("\"linkedServer\" is not allowed on a fragment entry; it is not run.");
			if (entry.Register != null)
				problem("\"register\" is not allowed on a fragment entry; it is not run.");

			if (entry.PartOf == null || entry.PartOf.Length == 0)
			{
				problem("\"partOf\" is required on a fragment entry and must name at least one script.");
				return;
			}
			foreach (string parent in entry.PartOf)
			{
				if (parent == null || !statuses.TryGetValue(parent, out CoverageStatus? parentStatus))
					problem(String.Format("\"partOf\" names \"{0}\", which has no entry.", parent));
				else if (String.Equals(parent, entry.Script, StringComparison.Ordinal))
					problem("\"partOf\" names the fragment itself.");
				else if (parentStatus == CoverageStatus.Fragment)
					problem(String.Format("\"partOf\" names \"{0}\", which is itself a fragment; name the scripts that run it.", parent));
			}
		}

		/// <summary>Whether a script's error text is the failure a known-failure entry expects.</summary>
		// NOTE: an ordinal substring test on English text: the engine, server and libraries ship only neutral resources (no
		// satellite resource folders for them in artifacts/bin). If localized resources ever ship, matching must pin the UI
		// culture or move to error codes. Messages from Windows or from packages that do ship satellites (sockets, WCF) are
		// localized already, so an entry for such a failure names the exception type instead.
		public static bool Matches(string errorText, string expected)
		{
			return errorText.Contains(expected, StringComparison.Ordinal);
		}
	}
}
