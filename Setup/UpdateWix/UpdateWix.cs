using System;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Text;

using CommandLine;

namespace UpdateWix
{
	class UpdateWix
	{
		[DefaultArgument(ArgumentType.Required, HelpText="Name of WXS source file")]
		public string WXSFile = "";

		[Argument(ArgumentType.Required, HelpText="Base path to locate the source files")]
		public string BasePath = "";

		[Argument(ArgumentType.Required, HelpText="Complete version of this build")]
		public string LongVersion = "";

		[Argument(ArgumentType.Required, HelpText="Short Version of this build (becomes part of service and folder names)")]
		public string ShortVersion = "";

		/// <summary> Get's the directory or file's name (preferring the long name if it exists). </summary>
		private  string GetName(XmlElement AElement)
		{
			XmlAttribute LName = AElement.Attributes["LongName"];
			if (LName != null)
				return LName.Value;
			else
				return AElement.Attributes["Name"].Value;
		}

		private void AddFiles(Folder ATarget, XmlElement AComponent)
		{
			XmlElement LChild;
			foreach (XmlNode LNode in AComponent.ChildNodes)
			{
				LChild = LNode as XmlElement;
				if ((LChild != null) && (LChild.LocalName == "File"))
					ATarget.Files.Add(GetName(LChild));
			}
		}
 
		private Folder BuildWixFolderTree(XmlElement AElement, Folder AParent)
		{
			Folder LFolder = new Folder(AParent, GetName(AElement));
			XmlElement LChild;
			foreach (XmlNode LNode in AElement.ChildNodes)
			{
				LChild = LNode as XmlElement;
				if (LChild != null)
				{
					if (LChild.LocalName == "Directory")
					{
						if (!IsSpecial(LChild.Attributes["Id"].Value))		// Ignore all special directory names
							LFolder.Folders.Add(BuildWixFolderTree(LChild, LFolder));
					}
					else if (LChild.LocalName == "Component")
						AddFiles(LFolder, LChild);
				}
			}
			return LFolder;
		}

		/// <summary> Returns true if the specified directory name is reserved. </summary>
		private bool IsSpecial(string AValue)
		{
			return (AValue == "SHORTCUTDIR");
		}

		private Folder BuildFileFolderTree(string ABasePath, string AOverrideName, Folder AParent)
		{
			string LName = (AOverrideName == null ? Path.GetFileName(ABasePath) : AOverrideName);
			Folder LFolder = new Folder(AParent, LName);

			// Populate the files
			foreach (string LFileName in Directory.GetFiles(ABasePath))
				LFolder.Files.Add(Path.GetFileName(LFileName));

			// Populate the sub-folders
			foreach (string LDirectoryName in Directory.GetDirectories(ABasePath))
				LFolder.Folders.Add(BuildFileFolderTree(Path.Combine(ABasePath, Path.GetFileName(LDirectoryName)), null, LFolder));

			return LFolder;
		}

		private void RemoveFolder(ArrayList AList, string AName)
		{
			Folder LFolder;
			for (int i = 0; i < AList.Count; i++)
			{
				LFolder = (Folder)AList[i];
				if (LFolder.Name == AName)
				{
					AList.RemoveAt(i);
					return;
				}
			}
		}

		private void CompareTrees(Folder ASource, Folder ATarget, string APath, StringBuilder ADifferences)
		{
			// Compare the files of the source and targets
			ArrayList LSourceFiles = new ArrayList(ASource.Files.Count);
			LSourceFiles.AddRange(ASource.Files);

			foreach (string LTargetFile in ATarget.Files)
				if (ASource.Files.Contains(LTargetFile))
					LSourceFiles.Remove(LTargetFile);
				else
					ADifferences.AppendFormat("File '{0}' not found at SOURCE location.\r\n", Path.Combine(APath, LTargetFile));

			foreach (string LSourceFile in LSourceFiles)
				ADifferences.AppendFormat("File '{0}' not found at TARGET location.\r\n", Path.Combine(APath, LSourceFile));

			// Compare the folders of the source and targets
			ArrayList LSourceFolders = new ArrayList(ASource.Folders.Count);
			LSourceFolders.AddRange(ASource.Folders);

			int LIndex;
			foreach (Folder LTargetFolder in ATarget.Folders)
			{
				LIndex = ASource.Folders.IndexOf(LTargetFolder.Name);
				if (LIndex >= 0)
				{
					CompareTrees(ASource.Folders[LIndex], LTargetFolder, Path.Combine(APath, LTargetFolder.Name), ADifferences);
					RemoveFolder(LSourceFolders, LTargetFolder.Name);
				}
				else
					ADifferences.AppendFormat("Folder '{0}' not found at SOURCE location.\r\n", Path.Combine(APath, LTargetFolder.Name));
			}

			foreach (Folder LSourceFolder in LSourceFolders)
				ADifferences.AppendFormat("Folder '{0}' not found at TARGET location.\r\n", Path.Combine(APath, LSourceFolder.Name));
		}

		public int Run()
		{
			try
			{
				// Load the WXS file
				Console.WriteLine("Loading WiX file");
				XmlDocument LDocument = new XmlDocument();
				using (Stream LFileStream = new FileStream(WXSFile, FileMode.Open, FileAccess.Read))
					LDocument.Load(LFileStream);

				// Load the wix document folder tree
				Console.WriteLine("Generating WiX folder tree...");
				Folder LWixTree = BuildWixFolderTree(LDocument.DocumentElement["Product"]["Directory"], null);

				// Load the file folder tree
				Console.WriteLine("Generating file folder tree....");
				Folder LFileTree = BuildFileFolderTree(BasePath, "SourceDir", null);

				Console.WriteLine("Comparing trees...");
				StringBuilder LDifferences = new StringBuilder();
				CompareTrees(LWixTree, LFileTree, "SourceDir", LDifferences);

				if (LDifferences.Length > 0)
					throw new Exception("Source (WSX File) and Target (Base Path Directory) trees differ as follows:\r\n" + LDifferences.ToString());

				// Check for a component without a key path
				Console.WriteLine("Checking for components missing key paths...");
				XmlNamespaceManager LNamespaceManager = new XmlNamespaceManager(LDocument.NameTable);
				LNamespaceManager.AddNamespace("WixNs", "http://schemas.microsoft.com/wix/2003/01/wi");
				XmlNodeList LMissingKeyPaths = LDocument.DocumentElement.SelectNodes(@"//WixNs:Component[not (@KeyPath or WixNs:File/@KeyPath)]", LNamespaceManager);
				if (LMissingKeyPaths.Count > 0)
				{
					StringBuilder LKeyPaths = new StringBuilder();
					LKeyPaths.Append("The following components are missing a KeyPath:\r\n");
					foreach (XmlNode LNode in LMissingKeyPaths)
						LKeyPaths.AppendFormat("Component '{0}'\r\n", LNode.Attributes["Id"].Value);
					throw new Exception(LKeyPaths.ToString());
				}
  
				// Make sure that every component has a GUID
				Console.WriteLine("Checking for components that are missing GUIDs...");
				XmlNodeList LNonGuidComponents = LDocument.DocumentElement.SelectNodes(@"//WixNs:Component[not (@Guid)]", LNamespaceManager);
				if (LNonGuidComponents.Count > 0)
				{
					foreach (XmlNode LNode in LNonGuidComponents)
					{
						Console.WriteLine("Adding GUID to component '{0}'", LNode.Attributes["Id"].Value);
						XmlAttribute LGuid = LDocument.CreateAttribute("Guid");
						LGuid.Value = Guid.NewGuid().ToString().ToUpper();
						LNode.Attributes.Append(LGuid);
					}
				}

				Console.WriteLine("Updating the version information...");

				// Update Id of Product
				LDocument.DocumentElement["Product"].Attributes["Id"].Value = Guid.NewGuid().ToString().ToUpper();

				// Update the Version of the Product
				LDocument.DocumentElement["Product"].Attributes["Version"].Value = LongVersion;

				// Update the Minimum attribute of the UpgradeVersion element w/ long version #
				LDocument.DocumentElement["Product"]["Upgrade"]["UpgradeVersion"].Attributes["Minimum"].Value = LongVersion;

				// Update the ShortVersion property to the short version #
				LDocument.DocumentElement.SelectSingleNode(@"//WixNs:Property[@Id='ShortVersion']", LNamespaceManager).InnerText = ShortVersion;

				// Backup the WXSFile
				Console.WriteLine("Backing up WiX source file...");
				File.Copy(WXSFile, Path.ChangeExtension(WXSFile, ".bak"), true);

				// Save the WXS file
				Console.WriteLine("Saving the updated WiX source file...");
				using (Stream LFileStream = new FileStream(WXSFile, FileMode.Create, FileAccess.Write))
					LDocument.Save(LFileStream);

				return 0;
			}
			catch (Exception LException)
			{
				Console.WriteLine(LException.ToString());
				return 1;
			}
		}

		[STAThread]
		static int Main(string[] AArgs)
		{
			UpdateWix LApp = new UpdateWix();
			if (CommandLine.Parser.ParseArgumentsWithUsage(AArgs, LApp))
                return LApp.Run();
			else
				return 1;
		}
	}

	public class Folder
	{
		public Folder(Folder AParent, string AName)
		{
			FParent = AParent;
			FName = AName;
		}

		private Folder FParent;
		public Folder Parent { get { return FParent; } }

		private string FName;
		public string Name { get { return FName; } }

		private Folders FFolders = new Folders();
		public Folders Folders { get { return FFolders; } }

		private Files FFiles = new Files();
		public Files Files { get { return FFiles; } }
	}

	public class Folders : ArrayList
	{
		public new Folder this[int AIndex]
		{
			get { return (Folder)base[AIndex]; }
		}

		public int IndexOf(string AName)
		{
			Folder LItem;
			for (int i = 0; i < Count; i++)
			{
				LItem = (Folder)this[i];
				if (LItem.Name == AName)
					return i;
			}
			return -1;
		}

		public void Remove(string AName)
		{
			int LIndex = IndexOf(AName);
			if (LIndex >= 0)
				RemoveAt(LIndex);
		}
	}

	public class Files : ArrayList
	{
		public new string this[int AIndex]
		{
			get { return (string)base[AIndex]; }
		}

		public bool Contains(string AName)
		{
			foreach (string LItem in this)
				if (LItem == AName)
					return true;
			return false;
		}
	}
}
