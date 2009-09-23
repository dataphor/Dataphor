using System;
using System.IO;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.DAE.Schema
{
	public static class LibraryUtility
	{
		public static string GetInstanceLibraryDirectory(string AInstanceDirectory, string ALibraryName)
		{
			string LResult = Path.Combine(Path.Combine(AInstanceDirectory, Server.Server.CDefaultLibraryDataDirectory), ALibraryName);
			System.IO.Directory.CreateDirectory(LResult);
			return LResult;
		}
		
		public static Library LoadFromFile(string AFileName, string AInstanceDirectory)
		{
			using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
			{
				Library LLibrary = LoadFromStream(LStream);
				LLibrary.Name = Path.GetFileNameWithoutExtension(AFileName);
				LLibrary.LoadInfoFromFile(Path.Combine(GetInstanceLibraryDirectory(AInstanceDirectory, LLibrary.Name), GetInfoFileName(LLibrary.Name)));
				return LLibrary;
			}
		}

		public static string GetInfoFileName(string ALibraryName)
		{
			return String.Format("{0}.d4l.info", ALibraryName);
		}
		
		public static Library LoadFromStream(Stream AStream)
		{
			return new Deserializer().Deserialize(AStream, null) as Library;
		}
		
		public static string GetFileName(string ALibraryName)
		{
			return String.Format("{0}.d4l", ALibraryName);
		}
		
		public static string GetLibraryDirectory(string AServerLibraryDirectory, string ALibraryName, string ALibraryDirectory)
		{
			return ALibraryDirectory == String.Empty ? Path.Combine(GetDefaultLibraryDirectory(AServerLibraryDirectory), ALibraryName) : ALibraryDirectory;
		}
		
		public static string GetDefaultLibraryDirectory(string AServerLibraryDirectory)
		{
			return AServerLibraryDirectory.Split(';')[0];
		}

		public static void GetAvailableLibraries(string AInstanceDirectory, string ALibraryDirectory, Libraries ALibraries)
		{
			ALibraries.Clear();
			string[] LDirectories = ALibraryDirectory.Split(';');
			for (int LIndex = 0; LIndex < LDirectories.Length; LIndex++)
				GetAvailableLibraries(AInstanceDirectory, LDirectories[LIndex], ALibraries, LIndex > 0);
		}
		
		public static void GetAvailableLibraries(string AInstanceDirectory, string ALibraryDirectory, Libraries ALibraries, bool ASetLibraryDirectory)
		{
			string LLibraryName;
			string LLibraryFileName;
			string[] LLibraries = System.IO.Directory.GetDirectories(ALibraryDirectory);
			for (int LIndex = 0; LIndex < LLibraries.Length; LIndex++)
			{
				LLibraryName = Path.GetFileName(LLibraries[LIndex]);
				LLibraryFileName = Path.Combine(LLibraries[LIndex], GetFileName(LLibraryName));
				if (File.Exists(LLibraryFileName))
				{
					Schema.Library LLibrary = LoadFromFile(LLibraryFileName, AInstanceDirectory);
					if (ASetLibraryDirectory)
						LLibrary.Directory = LLibraries[LIndex];
					ALibraries.Add(LLibrary);
				}
			}
		}
		
		public static Schema.Library GetAvailableLibrary(string AInstanceDirectory, string ALibraryName, string ALibraryDirectory)
		{
			string LLibraryFileName = Path.Combine(ALibraryDirectory, GetFileName(ALibraryName));
			if (File.Exists(LLibraryFileName))
			{
				Schema.Library LLibrary = LoadFromFile(LLibraryFileName, AInstanceDirectory);
				LLibrary.Directory = ALibraryDirectory;
				return LLibrary;
			}
			return null;
		}

		public static string GetLibraryDirectory(this Library ALibrary, string AServerLibraryDirectory)
		{
			return GetLibraryDirectory(AServerLibraryDirectory, ALibrary.Name, ALibrary.Directory);
		}
		
		public static string GetInstanceLibraryDirectory(this Library ALibrary, string AInstanceDirectory)
		{
			return GetInstanceLibraryDirectory(AInstanceDirectory, ALibrary.Name);
		}
		
		public static void SaveToFile(this Library ALibrary, string AFileName)
		{
			#if !RESPECTREADONLY
			FileUtility.EnsureWriteable(AFileName);
			#endif
			using (FileStream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				ALibrary.SaveToStream(LStream);
			}
		}
		
		public static void SaveInfoToFile(this Library ALibrary, string AFileName)
		{
			FileUtility.EnsureWriteable(AFileName);
			using (FileStream LStream = new FileStream(AFileName, FileMode.Create, FileAccess.Write))
			{
				new LibraryInfo(ALibrary.Name, ALibrary.IsSuspect, ALibrary.SuspectReason).SaveToStream(LStream);
			}
		}
		
		public static void LoadInfoFromFile(this Library ALibrary, string AFileName)
		{
			if (File.Exists(AFileName))
			{
				using (FileStream LStream = new FileStream(AFileName, FileMode.Open, FileAccess.Read))
				{
					LibraryInfo LLibraryInfo = LibraryInfo.LoadFromStream(LStream);
					ALibrary.IsSuspect = LLibraryInfo.IsSuspect;
					ALibrary.SuspectReason = LLibraryInfo.SuspectReason;
				}
			}
		}
	}
}
