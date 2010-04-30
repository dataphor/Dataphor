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
				
				// Remove the file entries from the given component
				RemoveAllChildElements(LComponent);

				// Add the file contents
				string LRelativePath = 
					String.IsNullOrEmpty(FRelativePath) 
						? Path.GetDirectoryName(Path.GetFullPath(FWiXFile)) 
						: FRelativePath;
				foreach (string LSourceFile in FSourceFiles)
				{
					XmlElement LFileElement = LDocument.CreateElement("File", CWiXNamespace);
					LFileElement.SetAttribute("Id", GenerateFileId(LSourceFile));
					LFileElement.SetAttribute("Name", Path.GetFileName(LSourceFile));
					LFileElement.SetAttribute("Source", MakePathRelative(Path.GetFullPath(LRelativePath) + "\\", Path.GetFullPath(LSourceFile)));
					LComponent.AppendChild(LFileElement);
					Log.LogMessage(MessageImportance.Low, "Added file element Id={0} Name={1} Source={2}.", LFileElement.Attributes["Id"].Value, LFileElement.Attributes["Name"].Value, LFileElement.Attributes["Source"].Value);
				}
				
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
	}
}
