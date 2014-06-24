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
        public const int MaxIDLength = 37;
		public const string CWiXNamespace = "http://schemas.microsoft.com/wix/2006/wi";
		
		private string FComponentId;
		/// <summary> The target component ID into which to synchronize the files. </summary>
		[Required]
		public string ComponentId
		{
			get { return FComponentId; }
			set { FComponentId = value; }
		}
		
		private string FComponentGroupId;
		/// <summary> Optional component group to first clear clear, then add all related components to. </summary>
		public string ComponentGroupId
		{
			get { return FComponentGroupId; }
			set { FComponentGroupId = value; }
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

				// Create namespace manager for XPath
				XmlNamespaceManager LNamespaceManager = new XmlNamespaceManager(LDocument.NameTable);
				LNamespaceManager.AddNamespace("WixNs", CWiXNamespace);
				
				// If specified, find and empty the component group
				XmlElement LComponentGroup = null;
				if (!String.IsNullOrEmpty(FComponentGroupId))
				{
					LComponentGroup = LDocument.DocumentElement.SelectSingleNode(@"//WixNs:ComponentGroup[@Id='" + FComponentGroupId + @"']", LNamespaceManager) as XmlElement;
					if (LComponentGroup == null)
						throw new Exception(String.Format("Specified component group ID ({0}) not found in the document.", FComponentGroupId));
					
					RemoveAllChildElements(LComponentGroup);
				}
				
				// Find the component
				Log.LogMessage(MessageImportance.Low, "Locating a Component by its ID ({0})...", Path.GetFullPath(FComponentId));
				XmlElement LComponent = LDocument.DocumentElement.SelectSingleNode(@"//WixNs:Component[@Id='" + FComponentId + @"']", LNamespaceManager) as XmlElement;
				if (LComponent == null)
					throw new Exception(String.Format("Specified component ID ({0}) not found in the document.", FComponentId));
				
				// Remove the file entries from the given component
				RemoveAllChildElements(LComponent);
				RemoveAllGeneratedDirectories(LDocument, LComponent.ParentNode);

				// Determine the relative path to the source files
				string LRelativePath =
					String.IsNullOrEmpty(FRelativePath)
						? Path.GetDirectoryName(Path.GetFullPath(FWiXFile))
						: FRelativePath;

				// Determine the rootmost folder for the given source files
				string LRootmostPath = null;
				foreach (string LSourceFile in FSourceFiles)
					LRootmostPath =
						String.IsNullOrEmpty(LRootmostPath)
							? Path.GetDirectoryName(Path.GetFullPath(LSourceFile))
							: GetMostRooted(LRootmostPath, Path.GetDirectoryName(Path.GetFullPath(LSourceFile)));

				// Recursively add the files and directories
				AddFilesAndDirectories(LRootmostPath, LComponent, LComponentGroup, LDocument, LRelativePath);
				
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

		private void AddFilesAndDirectories(string APath, XmlElement AParent, XmlElement AComponentGroup, XmlDocument ADocument, string ARelativePath)
		{
			// Add a component element (if AElement isn't one)
			XmlElement LComponent;
			if (AParent.LocalName != "Component")
			{
				LComponent = ADocument.CreateElement("Component", CWiXNamespace);
				LComponent.SetAttribute("Id", AParent.Attributes["Id"].Value + "Component");
				LComponent.SetAttribute("Guid", Guid.NewGuid().ToString("D"));
				LComponent.SetAttribute("DiskId", "1");
				AParent.AppendChild(LComponent);
				Log.LogMessage(MessageImportance.Low, "Added component ({0}).", LComponent.Attributes["Id"].Value);
			}
			else
				LComponent = AParent;
			
			// If a component group is specified, add a component ref to the component 
			if (AComponentGroup != null)
			{
				var LRef = ADocument.CreateElement("ComponentRef", CWiXNamespace);
				LRef.SetAttribute("Id", LComponent.Attributes["Id"].Value);
				AComponentGroup.AppendChild(LRef);
				Log.LogMessage(MessageImportance.Low, "Added component ref ({0}) to component group element ({1}).", LRef.Attributes["Id"].Value, AComponentGroup.Attributes["Id"].Value);
			}

			// Add all files that belong directly in this path
			foreach (string LSourceFile in FSourceFiles)
			{
				if (APath == Path.GetDirectoryName(Path.GetFullPath(LSourceFile)))
				{
					var LFileName = Path.GetFileName(LSourceFile);
					XmlElement LFileElement = ADocument.CreateElement("File", CWiXNamespace);
                    LFileElement.SetAttribute("Id", FileNameToID(LFileName, LComponent.ParentNode.Attributes["Id"].Value));
					LFileElement.SetAttribute("Name", LFileName);
					LFileElement.SetAttribute("Source", MakePathRelative(Path.GetFullPath(ARelativePath) + "\\", Path.GetFullPath(LSourceFile)));
					LComponent.AppendChild(LFileElement);
					Log.LogMessage(MessageImportance.Low, "Added file element Id={0} Name={1} Source={2}.", LFileElement.Attributes["Id"].Value, LFileElement.Attributes["Name"].Value, LFileElement.Attributes["Source"].Value);
				}
			}

			// Ensure that the directory is created if no files are contained
			if (LComponent.ChildNodes.Count == 0)
				LComponent.AppendChild(ADocument.CreateElement("CreateFolder", CWiXNamespace));

			var LAddedSubfolders = new List<string>();
			// Add each sub-folder
			foreach (string LSourceFile in FSourceFiles)
			{
				var LSubFolderName = SubFolderName(Path.GetFullPath(LSourceFile), APath);
				if (!String.IsNullOrEmpty(LSubFolderName))
				{
					if (!LAddedSubfolders.Contains(LSubFolderName))
					{
						// Add a directory element
						var LDirectoryElement = ADocument.CreateElement("Directory", CWiXNamespace);
                        LDirectoryElement.SetAttribute("Id", FileNameToID(LSubFolderName, LComponent.ParentNode.Attributes["Id"].Value));
						LDirectoryElement.SetAttribute("Name", LSubFolderName);
						LComponent.ParentNode.AppendChild(LDirectoryElement);

						// Recurse on the given sub-folder
						AddFilesAndDirectories(Path.Combine(APath, LSubFolderName), LDirectoryElement, AComponentGroup, ADocument, ARelativePath);

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

        private string FileNameToID(string ASourceFile, string AParent = null)
        {
            var baseName = 
                (
                    (AParent == null ? "" : AParent + "_") + ASourceFile
                ).Replace('.', '_').Replace('-', '_').Replace(' ', '_');
            var hash = ((UInt32)baseName.GetHashCode()).ToString();
            return "id_" + baseName.Substring(Math.Max(0, baseName.Length - MaxIDLength - hash.Length)) + hash;
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

		private void RemoveAllGeneratedDirectories(XmlDocument ADocument, XmlNode ADirectory)
		{
			var LID = ADirectory.Attributes["Id"].Value;
			XmlNamespaceManager LNamespaceManager = new XmlNamespaceManager(ADocument.NameTable);
			LNamespaceManager.AddNamespace("WixNs", CWiXNamespace);
			while (true)
			{
				var LDirectory = ADirectory.SelectSingleNode(@"self::node()/descendant::WixNs:Directory[starts-with(@Id, '" + LID + "')]", LNamespaceManager) as XmlElement;
				if (LDirectory == null)
					break;
				else
					ADirectory.RemoveChild(LDirectory);
			}
		}
	}
}
