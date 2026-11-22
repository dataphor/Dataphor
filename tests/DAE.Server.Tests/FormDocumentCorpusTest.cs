/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.DAE.Server.Tests
{
	/// <summary>
	/// One form document in the repository: a .dfd (a form, as BOP XML) or a .dfdx (a customization - a list of ancestor
	/// document expressions plus a delta to merge over them).
	/// </summary>
	public sealed class FormDocument
	{
		public FormDocument(string path, string libraryName)
		{
			Path = path;
			LibraryName = libraryName;
		}

		/// <summary>The file's absolute path.</summary>
		public string Path { get; }

		/// <summary>The D4 library the document belongs to, which is the name of the library's folder.</summary>
		public string LibraryName { get; }

		/// <summary>The D4 document name, which is the file name without its extension.</summary>
		public string DocumentName => System.IO.Path.GetFileNameWithoutExtension(Path);

		/// <summary>Library and document name joined, as a document is named in a test case and in the exclusion list.</summary>
		public string QualifiedName => LibraryName + "." + DocumentName;

		/// <summary>Whether this is a .dfdx - a customization - rather than a plain .dfd form.</summary>
		public bool IsCustomization => String.Equals(System.IO.Path.GetExtension(Path), FormDocumentCorpusTest.CustomizationExtension, StringComparison.OrdinalIgnoreCase);

		public override string ToString() => QualifiedName;
	}

	/// <summary>
	/// Part of L5 of the WS3 test layer: every form document in the repository is readable - a .dfd parses as BOP XML and
	/// a .dfdx parses as a customization whose embedded document is itself BOP XML. No server; this pass holds the two
	/// file formats and the readers in BOP (Dilx.cs) in agreement as the codebase evolves.
	/// </summary>
	[TestFixture]
	public class FormDocumentCorpusTest
	{
		public const string FormExtension = ".dfd";
		public const string CustomizationExtension = ".dfdx";

		/// <summary>The BOP XML element a form document's root must be.</summary>
		public const string InterfaceElementName = "interface";

		/// <summary>Each library folder keeps its D4 scripts and form documents in this subfolder.</summary>
		public const string DocumentsFolderName = "Documents";

		/// <summary>
		/// The repository folders that hold library folders. Read from the repository rather than from the runtime layout:
		/// the layout leaves out the libraries whose C# project is not converted yet, and the point of this pass is that
		/// no form document in the tree goes unread.
		/// </summary>
		public static readonly string[] LibraryRoots = ["libraries", "samples", Path.Combine("tests", "libraries")];

		/// <summary>
		/// The form documents in the Documents folder of every library folder directly under each of
		/// <paramref name="rootDirectories"/>. Takes the roots as an argument so that a corpus of application libraries
		/// outside this repository (compat-corpus-harness) adds a caller rather than a second walk.
		/// </summary>
		public static List<FormDocument> EnumerateFormDocuments(IEnumerable<string> rootDirectories)
		{
			List<FormDocument> result = new List<FormDocument>();
			foreach (string rootDirectory in rootDirectories)
			{
				if (!Directory.Exists(rootDirectory))
					continue;
				foreach (string libraryDirectory in Directory.EnumerateDirectories(rootDirectory))
				{
					string documentsDirectory = Path.Combine(libraryDirectory, DocumentsFolderName);
					if (!Directory.Exists(documentsDirectory))
						continue;
					// A library's D4 name is the name of its folder, which the build checks by looking for <Name>.d4l there.
					string libraryName = Path.GetFileName(libraryDirectory);
					foreach (string file in Directory.EnumerateFiles(documentsDirectory, "*", SearchOption.AllDirectories))
					{
						string extension = Path.GetExtension(file);
						if (String.Equals(extension, FormExtension, StringComparison.OrdinalIgnoreCase) || String.Equals(extension, CustomizationExtension, StringComparison.OrdinalIgnoreCase))
							result.Add(new FormDocument(file, libraryName));
					}
				}
			}
			result.Sort((left, right) => String.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		/// <summary>Every form document in this repository.</summary>
		public static List<FormDocument> RepositoryFormDocuments()
		{
			string repository = TestEnvironment.RepositoryDirectory;
			return EnumerateFormDocuments(LibraryRoots.Select(root => Path.Combine(repository, root)));
		}

		/// <summary>The path a case is named after and an assertion message quotes.</summary>
		public static string RepositoryRelativePath(FormDocument document)
		{
			return Path.GetRelativePath(TestEnvironment.RepositoryDirectory, document.Path).Replace('\\', '/');
		}

		public static IEnumerable<TestCaseData> FormDocuments()
		{
			foreach (FormDocument document in RepositoryFormDocuments())
				yield return new TestCaseData(document).SetName(RepositoryRelativePath(document));
		}

		[TestCaseSource(nameof(FormDocuments))]
		public void Reads(FormDocument document)
		{
			string relativePath = RepositoryRelativePath(document);

			// Read the bytes as text and parse from there, never XDocument.Load(path): all but two documents in the tree
			// declare encoding="utf-16" while holding UTF-8 bytes, so a reader that honours the declaration refuses the
			// file. The product reads them the same way - FrontendDevice.LoadAndProcessDocument uses a StreamReader - so
			// the declaration is dead text, and these tests must not be stricter than the product.
			string text = File.ReadAllText(document.Path);

			if (document.IsCustomization)
				ReadsAsCustomization(text, relativePath);
			else
				ReadsAsForm(text, relativePath);
		}

		private static void ReadsAsForm(string text, string relativePath)
		{
			XDocument form = Parse(text, relativePath, "the form");
			Assert.That(form.Root.Name.LocalName, Is.EqualTo(InterfaceElementName), String.Format("The root element of \"{0}\" is not <{1}>.", relativePath, InterfaceElementName));
			Assert.That(DeclaresBOPNamespace(form.Root), Is.True, String.Format("\"{0}\" does not declare the BOP namespace \"{1}\"; its bop: attributes would not be the ones the serializer reads.", relativePath, Persistence.BOPNamespaceURI));
		}

		private static void ReadsAsCustomization(string text, string relativePath)
		{
			DilxDocument customization = new DilxDocument();
			try
			{
				customization.Read(text);
			}
			catch (Exception exception)
			{
				Assert.Fail(String.Format("\"{0}\" could not be read as a customization: {1}", relativePath, exception.Message));
				return;
			}

			// With no ancestor there is nothing for the delta to be a delta of, and FrontendDevice.ProcessDilxDocument
			// returns the delta unmerged - a form that was never the form the designer saw.
			Assert.That(customization.Ancestors, Is.Not.Empty, String.Format("\"{0}\" names no ancestor document.", relativePath));

			XDocument delta = Parse(customization.Content, relativePath, "the embedded document of");
			Assert.That(delta.Root.Name.LocalName, Is.EqualTo(InterfaceElementName), String.Format("The embedded document of \"{0}\" has root element <{1}>, not <{2}>.", relativePath, delta.Root.Name.LocalName, InterfaceElementName));
		}

		private static XDocument Parse(string text, string relativePath, string what)
		{
			try
			{
				return XDocument.Parse(text);
			}
			catch (Exception exception)
			{
				Assert.Fail(String.Format("Parsing {0} \"{1}\" as XML failed: {2}", what, relativePath, exception.Message));
				return null;
			}
		}

		private static bool DeclaresBOPNamespace(XElement element)
		{
			return element.Attributes().Any(attribute => attribute.IsNamespaceDeclaration && String.Equals(attribute.Value, Persistence.BOPNamespaceURI, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Which whitespace in an attribute of an embedded document survives being read, which is what the conversion in
		/// Dilx.cs is for: a tab written as a character reference comes through as a tab, while a tab the file stores
		/// literally is gone before Dataphor sees it, because XML attribute-value normalization replaces literal
		/// whitespace in an attribute value with a space. A customized form written by the designer uses the first form;
		/// FormCustomizationTest.MultiLineScriptInAnAttributeSurvivesLoading is the one document in the tree with the second.
		/// </summary>
		[Test]
		public void CustomizationReadKeepsCharacterReferencedWhitespace()
		{
			DilxDocument customization = new DilxDocument();
			customization.Read(
				"""
				<dilx xmlns="http://www.alphora.com/schemas/dilx">
				  <ancestor document="Derive('Nothing', 'Browse')" />
				  <document>
					<interface xmlns:bop="www.alphora.com/schemas/bop">
					  <textbox bop:name="Referenced" title="a&#x9;b" />
					  <textbox bop:name="Literal" title="a	b" />
					</interface>
				  </document>
				</dilx>
				""");

			XDocument delta = XDocument.Parse(customization.Content);
			Assert.That(Title(delta, "Referenced"), Is.EqualTo("a\tb"), "A tab written as a character reference did not survive the read.");
			Assert.That(Title(delta, "Literal"), Is.EqualTo("a b"), "A tab the file stores literally is normalized to a space; that is no longer what happens.");
		}

		private static string Title(XDocument document, string name)
		{
			return (string)document.Descendants().Single(element => (string)element.Attribute(Persistence.XmlBOPName) == name).Attribute("title");
		}

		/// <summary>
		/// A corpus pass whose discovery finds nothing passes silently, so the walk itself is asserted: a renamed root or
		/// Documents folder, or a lost extension, has to fail the run rather than shrink the corpus to zero.
		/// </summary>
		[Test]
		public void TheWalkFindsBothKindsOfFormDocument()
		{
			List<FormDocument> documents = RepositoryFormDocuments();
			Assert.That(documents.Where(document => !document.IsCustomization), Is.Not.Empty, String.Format("The walk found no {0} file under {1}.", FormExtension, String.Join(", ", LibraryRoots)));
			Assert.That(documents.Where(document => document.IsCustomization), Is.Not.Empty, String.Format("The walk found no {0} file under {1}.", CustomizationExtension, String.Join(", ", LibraryRoots)));
		}
	}
}
