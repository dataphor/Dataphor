using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dataphor.Build
{
	/// <summary> Synchronizes WiX files and components with files in a given folder. </summary>
	public class SyncWiXFiles : Task
	{
		public const string CWiXNamespace = "http://schemas.microsoft.com/wix/2006/wi";
		
		private string FComponentId;
		/// <summary> The target component ID into which to synchronize the files. </summary>
		[Required]
		public string ComponentId
		{
			get { return FComponentId; }
			set { FComponentId = value; }
		}

		private string[] FSourceFiles;
		/// <summary> The files to which to synchronize. </summary>
		[Required]
		public string[] SourceFiles
		{
			get { return FSourceFiles; }
			set { FSourceFiles = value; }
		}
		
		private string FRelativePath;
		/// <summary> The path from which to make the source paths relative. </summary>
		/// <remarks> Defaults to the directory containing the WiXFile. </remarks>
		public string RelativePath
		{
			get { return FRelativePath; }
			set { FRelativePath = value; }
		}

		private string FWiXFile;
		[Required]
		public string WiXFile
		{
			get { return FWiXFile; }
			set { FWiXFile = value; }
		}
		
		public override bool Execute()
		{
			try
			{

				// Read the WiX file
				Log.LogMessage(MessageImportance.Low, "Reading WiX source file from ({0})...", Path.GetFullPath(FWiXFile));
				XmlDocument LDocument = new XmlDocument();
				using (FileStream LWiXFile = new FileStream(FWiXFile, FileMode.Open))
				{
					LDocument.Load(LWiXFile);
				}
				
				// Find the component
				Log.LogMessage(MessageImportance.Low, "Locating a Component by its ID ({0})...", Path.GetFullPath(FComponentId));
				XmlNamespaceManager LNamespaceManager = new XmlNamespaceManager(LDocument.NameTable);
				LNamespaceManager.AddNamespace("WixNs", CWiXNamespace);
				XmlElement LComponent = LDocument.DocumentElement.SelectSingleNode(@"//WixNs:Component[@Id='" + FComponentId + @"']", LNamespaceManager) as XmlElement;
				if (LComponent == null)
					throw new Exception(String.Format("Specified component ID ({0}) not found in the document.", FComponentId));
				
				// Add the file contents
				string LRelativePath = 
					String.IsNullOrEmpty(FRelativePath) 
						? Path.GetDirectoryName(Path.GetFullPath(FWiXFile)) 
						: FRelativePath;
				
				// Determine the rootmost folder for the given source file set
				string LRootmostPath = null;
				foreach (string LSourceFile in FSourceFiles)
					LRootmostPath = 
						String.IsNullOrEmpty(LRootmostPath) 
							? Path.GetDirectoryName(Path.GetFullPath(LSourceFile))
							: GetMostRooted(LRootmostPath, Path.GetDirectoryName(Path.GetFullPath(LSourceFile)));

				// Remove the file entries from the given component
				RemoveAllChildElements(LComponent);
				RemoveAllGeneratedDirectories(LDocument, LComponent);

				// Recursively add the files and directories
				AddFilesAndDirectories(LRootmostPath, LComponent, LDocument, LRelativePath);
				
				// Copy old WiX file
				string LBackupFileName = FWiXFile + ".bak";
				Log.LogMessage(MessageImportance.Low, "Backing up WiX file ({0}) to '{1}'...", FWiXFile, LBackupFileName);
				if (File.Exists(LBackupFileName))
					File.Delete(LBackupFileName);
				File.Move(FWiXFile, LBackupFileName);
				
				// Rewrite the WiX file
				Log.LogMessage(MessageImportance.Low, "Writing WiX file ({0})...", FWiXFile);
				using (FileStream LWiXFile = new FileStream(FWiXFile, FileMode.Create))
				{
					LDocument.Save(LWiXFile);
				}

				Log.LogMessage("Finished synchronizing {0} file(s) with '{1}'...", FSourceFiles.Length, FWiXFile);
			}
			catch (Exception LException)
			{
				Log.LogErrorFromException(LException);
				return false;
			}

			return true;
		}

		private int FGenerator = 0;
		
		private void AddFilesAndDirectories(string APath, XmlElement AParent, XmlDocument ADocument, string ARelativePath)
		{
			string LPrefix;
			
			// Add a component element (if AElement isn't one)
			XmlElement LComponent;
			if (AParent.LocalName != "Component")
			{
				LComponent = ADocument.CreateElement("Component", CWiXNamespace);
				LComponent.SetAttribute("Id", AParent.Attributes["Id"].Value + "Files");
				LComponent.SetAttribute("Guid", Guid.NewGuid().ToString("D"));
				LComponent.SetAttribute("DiskId", "1");
				LPrefix = "";
			}
			else
			{
				LComponent = AParent;
				LPrefix = LComponent.Attributes["Id"].Value;
			}

			// Add all files that belong directly in this path
			foreach (string LSourceFile in FSourceFiles)
			{
				if (APath == Path.GetDirectoryName(Path.GetFullPath(LSourceFile)))
				{
					XmlElement LFileElement = ADocument.CreateElement("File", CWiXNamespace);
					LFileElement.SetAttribute("Id", GenerateFileId(LSourceFile));
					LFileElement.SetAttribute("Name", Path.GetFileName(LSourceFile));
					LFileElement.SetAttribute("Source", MakePathRelative(Path.GetFullPath(ARelativePath) + "\\", Path.GetFullPath(LSourceFile)));
					LComponent.AppendChild(LFileElement);
					Log.LogMessage(MessageImportance.Low, "Added file element Id={0} Name={1} Source={2}.", LFileElement.Attributes["Id"].Value, LFileElement.Attributes["Name"].Value, LFileElement.Attributes["Source"].Value);
				}
			}
			
			if (LComponent.ChildNodes.Count > 0)
			{
				// Add the component if it was needed
				if (LComponent != AParent)
					AParent.AppendChild(LComponent);
			}
			else
			{
				// Ensure that the directory is created if no other files or directories are contained
				LComponent.AppendChild(ADocument.CreateElement("CreateFolder", CWiXNamespace));
			}
			
			var LAddedSubfolders = new List<string>();
			// Add each sub-folder
			foreach (string LSourceFile in FSourceFiles)
			{
				var LSubFolderName = SubFolderName(Path.GetFullPath(LSourceFile), APath);
				if (!String.IsNullOrEmpty(LSubFolderName))
				{
					if (!LAddedSubfolders.Contains(LSubFolderName))
					{
						// Generate a directory ID
						var LID = LPrefix + LSubFolderName + FGenerator.ToString();
						FGenerator++;

						// Determine the parent directory
						var LParentDirectory = AParent == LComponent ? AParent.ParentNode : AParent;
						
						// Add a directory element
						var LDirectoryElement = ADocument.CreateElement("Directory", CWiXNamespace);
						LDirectoryElement.SetAttribute("Id", LID);
						LDirectoryElement.SetAttribute("Name", LSubFolderName);
						LParentDirectory.AppendChild(LDirectoryElement);

						// Recurse on the given sub-folder
						AddFilesAndDirectories(Path.Combine(APath, LSubFolderName), LDirectoryElement, ADocument, ARelativePath);

						// Indicate that the given sub-folder has been handled
						LAddedSubfolders.Add(LSubFolderName);
					}
				}
			}
		}

		/// <summary> Returns the next sub-folder of the given file name, relative to the given path. </summary>
		private string SubFolderName(string AFile, string APath)
		{
			AFile = Path.GetDirectoryName(AFile);
			if (AFile.Length > APath.Length && AFile.StartsWith(APath, StringComparison.OrdinalIgnoreCase))
			{
				AFile = AFile.Substring(APath.Length + 1);
				var LSep = AFile.IndexOf(Path.DirectorySeparatorChar);
				if (LSep > 0)
					return AFile.Substring(0, LSep);
				else
					return AFile;
			}
			else
				return "";
		}

		/// <summary> Given two paths, returns the common subset. </summary>
		private string GetMostRooted(string ALeft, string ARight)
		{
			var LLeftSplit = ALeft.Split(Path.DirectorySeparatorChar);
			var LRightSplit = ARight.Split(Path.DirectorySeparatorChar);
			int i = 0;
			while (i < Math.Min(LLeftSplit.Length, LRightSplit.Length) && String.Equals(LLeftSplit[i], LRightSplit[i], StringComparison.OrdinalIgnoreCase))
				i++;
			return String.Join(Path.DirectorySeparatorChar.ToString(), LLeftSplit, 0, i);
		}

		private string GenerateFileId(string ASourceFile)
		{
			return FComponentId + "_" + Path.GetFileName(ASourceFile).Replace('.', '_').Replace('-', '_').Replace(' ', '_');
		}

		private static string MakePathRelative(string ABase, string APath)
		{
			int LCommonIndex = -1;
			int LCurrentIndex = 0;
			while 
			(
				(LCurrentIndex < ABase.Length) 
					&& (LCurrentIndex < APath.Length)
					&& (Char.ToLower(ABase[LCurrentIndex], CultureInfo.InvariantCulture) == Char.ToLower(APath[LCurrentIndex], CultureInfo.InvariantCulture))
			)
			{
				if (ABase[LCurrentIndex] == Path.DirectorySeparatorChar)
					LCommonIndex = LCurrentIndex;
				LCurrentIndex++;
			}

			if (LCurrentIndex == 0)
				return APath;

			if ((LCurrentIndex == ABase.Length) && (LCurrentIndex == APath.Length))
				return string.Empty;

			StringBuilder LBuilder = new StringBuilder();
			while (LCurrentIndex < ABase.Length)
			{
				if (ABase[LCurrentIndex] == Path.DirectorySeparatorChar)
					LBuilder.Append(".." + Path.DirectorySeparatorChar);
				LCurrentIndex++;
			}

			return (LBuilder.ToString() + APath.Substring(LCommonIndex + 1));
		}

		public static void RemoveAllChildElements(XmlElement AElement)
		{
			XmlNode LFirstChild = AElement.FirstChild;
			XmlNode LNextSibling = null;
			while (LFirstChild != null)
			{
				LNextSibling = LFirstChild.NextSibling;
				AElement.RemoveChild(LFirstChild);
				LFirstChild = LNextSibling;
			}
		}

		private void RemoveAllGeneratedDirectories(XmlDocument ADocument, XmlElement AComponent)
		{
			var LID = AComponent.Attributes["Id"].Value;
			XmlNamespaceManager LNamespaceManager = new XmlNamespaceManager(ADocument.NameTable);
			LNamespaceManager.AddNamespace("WixNs", CWiXNamespace);
			while (true)
			{
				var LDirectory = AComponent.ParentNode.SelectSingleNode(@"//WixNs:Directory[starts-with(@Id, '" + LID + "')]", LNamespaceManager) as XmlElement;
				if (LDirectory == null)
					break;
				else
					AComponent.ParentNode.RemoveChild(LDirectory);
			}
		}
	}
}
