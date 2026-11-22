/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>One library folder a corpus pass registers: what the server will know it as, where to copy it from, and what it requires.</summary>
	/// <remarks>
	/// Nothing here knows about samples, this repository or NUnit. SamplesCorpusTest points the discovery rule and the
	/// closure walk below at the repository's samples/ folder; the WS13 work item compat-corpus-harness points the same
	/// two at an application corpus outside this repository.
	/// </remarks>
	public sealed class CorpusLibrary
	{
		public CorpusLibrary(string name, string directory, IReadOnlyList<string> requisites)
		{
			Name = name;
			Directory = directory;
			Requisites = requisites;
		}

		/// <summary>The folder's name, which is the library name the server uses.</summary>
		/// <remarks>
		/// Folder name and library name have to agree: the server finds a library's folder by joining its library
		/// directory and the library's name (Schema.LibraryUtility.GetLibraryDirectory), so a .d4l named differently from
		/// its folder is not a library the server can load. The discovery rule below is that same rule.
		/// </remarks>
		public string Name { get; }

		/// <summary>The folder to copy into a test server's library directory.</summary>
		public string Directory { get; }

		/// <summary>The library names this library's .d4l requires, in the order it lists them.</summary>
		public IReadOnlyList<string> Requisites { get; }

		public override string ToString() => Name;

		/// <summary>The extension of the descriptor file that makes a folder a library.</summary>
		public const string DescriptorExtension = ".d4l";

		/// <summary>
		/// The library names that have no folder of their own: System is the built-in catalog library and the server
		/// creates General itself, so a requisite naming either is satisfied by a server that carries neither folder.
		/// They are skipped by the closure walk rather than reported missing.
		/// </summary>
		public static readonly string[] LibrariesWithNoFolder = ["System", "General"];

		/// <summary>
		/// Every library folder directly under <paramref name="rootDirectory"/>: a subfolder holding a .d4l file named
		/// after it. Ordered by name, so a run's case order does not depend on the file system. A subfolder with no such
		/// file is not a library and is skipped, as is a root directory that does not exist.
		/// </summary>
		public static IReadOnlyList<CorpusLibrary> DiscoverLibraries(string rootDirectory)
		{
			List<CorpusLibrary> result = new List<CorpusLibrary>();
			// System.IO.Directory and System.IO.Path, qualified: the Directory property above hides the type name in this
			// class, as FormDocument's Path property does in FormDocumentCorpusTest.cs.
			if (!System.IO.Directory.Exists(rootDirectory))
				return result;
			foreach (string directory in System.IO.Directory.EnumerateDirectories(rootDirectory))
			{
				CorpusLibrary library = ReadLibrary(directory);
				if (library != null)
					result.Add(library);
			}
			result.Sort((left, right) => String.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		/// <summary>
		/// The library <paramref name="name"/> names under <paramref name="rootDirectory"/>, or null when there is no
		/// such folder or it holds no .d4l named after it - which is how a library the build leaves out of the runtime
		/// layout is told from one it assembles.
		/// </summary>
		public static CorpusLibrary FindLibrary(string rootDirectory, string name)
		{
			return ReadLibrary(System.IO.Path.Combine(rootDirectory, name));
		}

		/// <summary>The library a folder holds, or null when it holds no .d4l named after the folder.</summary>
		private static CorpusLibrary ReadLibrary(string directory)
		{
			string name = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(directory));
			string descriptor = System.IO.Path.Combine(directory, name + DescriptorExtension);
			return File.Exists(descriptor) ? new CorpusLibrary(name, directory, ReadRequisites(descriptor)) : null;
		}

		/// <summary>
		/// The library names a .d4l requires, in the order it lists them. Read with the engine's own deserializer rather
		/// than as plain XML, so that a corpus pass and the server cannot disagree about what the file says.
		/// </summary>
		/// <remarks>
		/// LoadFromStream rather than LibraryUtility.LoadFromFile, which also reads the per-instance .d4l.info file out of
		/// a server's instance directory: a corpus walk has no instance, and the requisites are in the .d4l itself.
		/// </remarks>
		private static IReadOnlyList<string> ReadRequisites(string descriptorPath)
		{
			using (FileStream stream = new FileStream(descriptorPath, FileMode.Open, FileAccess.Read))
				return Schema.LibraryUtility.LoadFromStream(stream).Libraries.Select(reference => reference.Name).ToList();
		}

		/// <summary>
		/// What a server must carry to register <paramref name="libraryName"/>: the transitive closure of its requisites
		/// over the .d4l files under <paramref name="rootDirectory"/>, depth first and in the order each .d4l lists them,
		/// every library after the ones it requires, with <paramref name="libraryName"/> itself last. So the result can be
		/// copied into a server's library directory as it stands, and registering its last entry pulls the rest in.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// When a library in the closure has no folder under <paramref name="rootDirectory"/> and is not one of
		/// <see cref="LibrariesWithNoFolder"/>, which is a corpus a server could not carry.
		/// </exception>
		public static IReadOnlyList<CorpusLibrary> RequisiteClosure(string rootDirectory, string libraryName)
		{
			List<CorpusLibrary> result = new List<CorpusLibrary>();
			AddWithItsRequisites(rootDirectory, libraryName, null, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
			return result;
		}

		private static void AddWithItsRequisites(string rootDirectory, string name, string requiredBy, List<CorpusLibrary> result, HashSet<string> visited)
		{
			// The visited set also stops a circular reference from recurring for ever. The server rejects one
			// (Schema.LibraryUtility.CheckCircularLibraryReference), so a corpus with a cycle is a corpus that will not
			// register; this walk just declines to hang on it.
			if (LibrariesWithNoFolder.Contains(name, StringComparer.OrdinalIgnoreCase) || !visited.Add(name))
				return;

			CorpusLibrary library = FindLibrary(rootDirectory, name);
			if (library == null)
				throw new InvalidOperationException
				(
					requiredBy == null
						? String.Format("The library \"{0}\" has no folder under \"{1}\".", name, rootDirectory)
						: String.Format("\"{0}\" requires the library \"{1}\", which has no folder under \"{2}\".", requiredBy, name, rootDirectory)
				);

			foreach (string requisite in library.Requisites)
				AddWithItsRequisites(rootDirectory, requisite, name, result, visited);
			result.Add(library);
		}
	}
}
