/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Reflection;

namespace Alphora.Dataphor.Windows
{
	public sealed class FileUtility
	{
		/// <summary> Ensures that the specified file is writable (not read-only) if it exists. </summary>
		/// <remarks> If the file does not exist, this method does nothing. </remarks>
		public static void EnsureWriteable(string fileName)
		{
			if (File.Exists(fileName))
			{
				FileAttributes attributes = File.GetAttributes(fileName);
				if ((attributes & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(fileName, attributes & ~FileAttributes.ReadOnly);
			}
		}

		/// <summary> Ensures that the specified file is read-only if it exists. </summary>
		/// <remarks> If the file does not exist, this method does nothing. </remarks>
		public static void EnsureReadOnly(string fileName)
		{
			if (File.Exists(fileName))
			{
				FileAttributes attributes = File.GetAttributes(fileName);
				if ((attributes & FileAttributes.ReadOnly) == 0)
					File.SetAttributes(fileName, attributes | FileAttributes.ReadOnly);
			}
		}
		
		/// <summary>
		/// Returns true if the given file is an assembly, false otherwise.
		/// </summary>
		public static bool IsAssembly(string fileName)
		{
			// According to this: http://msdn.microsoft.com/en-us/library/ms173100%28VS.80%29.aspx
			// This is the Microsoft recommended approach. Thanks for helping follow your recommended
			// best practice of not using exceptions to indicate return values!
			try
			{
				AssemblyName.GetAssemblyName(fileName);
				return true;
			}
			catch (FileLoadException)
			{
				return true; // The assembly is already loaded
			}
			catch (BadImageFormatException)
			{
				return false;
			}
		}
	}

	public enum VersionModifier { None, VersionSpecific, BuildSpecific, MajorSpecific }

	/// <summary> Sundry static path related routines. </summary>
	public sealed class PathUtility
	{
		public static void EnsureWriteable(string directoryName, bool recursive)
		{
			if (Directory.Exists(directoryName))
			{
				foreach (string file in Directory.GetFiles(directoryName))
					FileUtility.EnsureWriteable(file);
						
				if (recursive)
					foreach (string directory in Directory.GetDirectories(directoryName))
						EnsureWriteable(directory, recursive);
			}
		}

		private static string BuildAppPath(string root, string applicationName, VersionModifier modifier)
		{
			StringBuilder result = new StringBuilder(root);
			result.AppendFormat(@"{0}Alphora{0}Dataphor{0}", Path.DirectorySeparatorChar);
			
			if (modifier != VersionModifier.None)
			{
				//Append version
				Version version = typeof(PathUtility).Assembly.GetName().Version;
				result.Append(version.Major.ToString());
				if (modifier != VersionModifier.MajorSpecific)
				{
					result.Append('.');
					result.Append(version.Minor.ToString());
					if (modifier == VersionModifier.BuildSpecific)
					{
						result.Append('.');
						result.Append(version.Build.ToString());
					}
				}
				result.Append(Path.DirectorySeparatorChar);
			}

			if ((applicationName != null) && (applicationName != String.Empty))
			{
				result.Append(applicationName);
				result.Append(Path.DirectorySeparatorChar);
			}

			Directory.CreateDirectory(result.ToString());
			return result.ToString();
		}

		public static string CommonAppDataPath()
		{
			return CommonAppDataPath(String.Empty, VersionModifier.VersionSpecific);
		}

		public static string CommonAppDataPath(string applicationName)
		{
			return CommonAppDataPath(applicationName, VersionModifier.VersionSpecific);
		}
		
		public static string CommonAppDataPath(string applicationName, bool buildSpecific)
		{
			return CommonAppDataPath(applicationName, buildSpecific ? VersionModifier.BuildSpecific : VersionModifier.VersionSpecific);
		}
		
		public static string CommonAppDataPath(string applicationName, VersionModifier versionModifier)
		{
			return BuildAppPath
			(
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				applicationName,
				versionModifier
			);
		}

		public static string UserAppDataPath()
		{
			return UserAppDataPath(String.Empty, VersionModifier.VersionSpecific);
		}

		public static string UserAppDataPath(string applicationName)
		{
			return UserAppDataPath(applicationName, VersionModifier.VersionSpecific);
		}
		
		public static string UserAppDataPath(string applicationName, bool buildSpecific)
		{
			return UserAppDataPath(applicationName, buildSpecific ? VersionModifier.BuildSpecific : VersionModifier.VersionSpecific);
		}

		public static string UserAppDataPath(string applicationName, VersionModifier versionModifier)
		{
			return BuildAppPath
			(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				applicationName,
				versionModifier
			);
		}

		public static string GetBinDirectory()
		{
			if (AppDomain.CurrentDomain.RelativeSearchPath != null)
			{
				string[] paths = AppDomain.CurrentDomain.RelativeSearchPath.Split(';');
				if (paths.Length > 0)
					return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, paths[0]);
			}
			return AppDomain.CurrentDomain.BaseDirectory;
		}

		public static string GetInstallationDirectory()
		{
			// Start with the base directory
			string result = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
			
			// if this directory is named "bin", we are in a development environment, pop out one more to get the actual Dataphor directory
			if (Path.GetFileName(result).ToLower() == "bin")
				result = Path.GetDirectoryName(result);
				
			// pop out one more to get the root installation directory
			return Path.GetDirectoryName(result);
		}
		
		public static string GetFullFileName(string fileName)
		{
			return Path.IsPathRooted(fileName) ? fileName : Path.Combine(PathUtility.GetBinDirectory(), fileName);
		}
	}
}
