/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;

namespace Alphora.Dataphor
{
	public sealed class FileUtility
	{
		/// <summary> Ensures that the specified file is writable (not read-only) if it exists. </summary>
		/// <remarks> If the file does not exist, this method does nothing. </remarks>
		public static void EnsureWriteable(string AFileName)
		{
			if (File.Exists(AFileName))
			{
				FileAttributes LAttributes = File.GetAttributes(AFileName);
				if ((LAttributes & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(AFileName, LAttributes & ~FileAttributes.ReadOnly);
			}
		}

		/// <summary> Ensures that the specified file is read-only if it exists. </summary>
		/// <remarks> If the file does not exist, this method does nothing. </remarks>
		public static void EnsureReadOnly(string AFileName)
		{
			if (File.Exists(AFileName))
			{
				FileAttributes LAttributes = File.GetAttributes(AFileName);
				if ((LAttributes & FileAttributes.ReadOnly) == 0)
					File.SetAttributes(AFileName, LAttributes | FileAttributes.ReadOnly);
			}
		}
	}
	
	public enum VersionModifier { None, VersionSpecific, BuildSpecific }

	/// <summary> Sundry static path related routines. </summary>
	public sealed class PathUtility
	{
		public static void EnsureWriteable(string ADirectoryName, bool ARecursive)
		{
			if (Directory.Exists(ADirectoryName))
			{
				foreach (string LFile in Directory.GetFiles(ADirectoryName))
					FileUtility.EnsureWriteable(LFile);
						
				if (ARecursive)
					foreach (string LDirectory in Directory.GetDirectories(ADirectoryName))
						EnsureWriteable(LDirectory, ARecursive);
			}
		}

		private static string BuildAppPath(string ARoot, string AApplicationName, VersionModifier AModifier)
		{
			StringBuilder LResult = new StringBuilder(ARoot);
			LResult.Append(@"\Alphora\Dataphor\");
			
			if ((AModifier == VersionModifier.VersionSpecific) || (AModifier == VersionModifier.BuildSpecific))
			{
				//Append version
				Version LVersion = typeof(PathUtility).Assembly.GetName().Version;
				LResult.Append(LVersion.Major.ToString());
				LResult.Append('.');
				LResult.Append(LVersion.Minor.ToString());
				if (AModifier == VersionModifier.BuildSpecific)
				{
					LResult.Append('.');
					LResult.Append(LVersion.Build.ToString());
				}
				LResult.Append('\\');
			}

			if ((AApplicationName != null) && (AApplicationName != String.Empty))
			{
				LResult.Append(AApplicationName);
				LResult.Append('\\');
			}

			Directory.CreateDirectory(LResult.ToString());
			return LResult.ToString();
		}

		public static string CommonAppDataPath()
		{
			return CommonAppDataPath(String.Empty, VersionModifier.VersionSpecific);
		}

		public static string CommonAppDataPath(string AApplicationName)
		{
			return CommonAppDataPath(AApplicationName, VersionModifier.VersionSpecific);
		}
		
		public static string CommonAppDataPath(string AApplicationName, bool ABuildSpecific)
		{
			return CommonAppDataPath(AApplicationName, ABuildSpecific ? VersionModifier.BuildSpecific : VersionModifier.VersionSpecific);
		}
		
		public static string CommonAppDataPath(string AApplicationName, VersionModifier AVersionModifier)
		{
			return BuildAppPath
			(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				AApplicationName,
				AVersionModifier
			);
		}

		public static string UserAppDataPath()
		{
			return UserAppDataPath(String.Empty, VersionModifier.VersionSpecific);
		}

		public static string UserAppDataPath(string AApplicationName)
		{
			return UserAppDataPath(AApplicationName, VersionModifier.VersionSpecific);
		}
		
		public static string UserAppDataPath(string AApplicationName, bool ABuildSpecific)
		{
			return UserAppDataPath(AApplicationName, ABuildSpecific ? VersionModifier.BuildSpecific : VersionModifier.VersionSpecific);
		}

		public static string UserAppDataPath(string AApplicationName, VersionModifier AVersionModifier)
		{
			return BuildAppPath
			(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				AApplicationName,
				AVersionModifier
			);
		}

		public static string GetBinDirectory()
		{
			if (AppDomain.CurrentDomain.RelativeSearchPath != null)
			{
				string[] LPaths = AppDomain.CurrentDomain.RelativeSearchPath.Split(';');
				if (LPaths.Length > 0)
					return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LPaths[0]);
			}
			return AppDomain.CurrentDomain.BaseDirectory;
		}
		
		public static string GetFullFileName(string AFileName)
		{
			return Path.IsPathRooted(AFileName) ? AFileName : Path.Combine(PathUtility.GetBinDirectory(), AFileName);
		}
	}
}
