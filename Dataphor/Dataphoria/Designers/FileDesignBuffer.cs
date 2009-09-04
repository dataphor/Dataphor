using System;
using System.IO;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class FileDesignBuffer : DesignBuffer
	{
		public FileDesignBuffer(IDataphoria ADataphoria, string AFileName)
			: this(ADataphoria, FileDesignBuffer.GetLocatorName(AFileName))
		{ }

		public FileDesignBuffer(IDataphoria ADataphoria, DebugLocator ALocator)
			: base(ADataphoria, ALocator)
		{
			FFileName = ALocator.Locator.Substring(FileDesignBuffer.CFileLocatorPrefix.Length);
		}

		// FileName

		private string FFileName;
		public string FileName
		{
			get { return FFileName; }
		}

		public override string GetDescription()
		{
			return Path.GetFileName(FFileName);
		}

		public override bool Equals(object AObject)
		{
			FileDesignBuffer LBuffer = AObject as FileDesignBuffer;
			if (LBuffer != null && String.Equals(LBuffer.FileName, FileName, StringComparison.OrdinalIgnoreCase))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return FileName.ToLowerInvariant().GetHashCode();	// may not be in case-insensitive hashtable so make insensitive through ToLower()
		}

		public FileDesignBuffer PromptForBuffer(IDesigner ADesigner)
		{
			return Dataphoria.PromptForFileBuffer(ADesigner, FileName);
		}

		// Data

		private void EnsureWritable(string AFileName)
		{
			if (File.Exists(AFileName))
			{
				FileAttributes LAttributes = File.GetAttributes(AFileName);
				if ((LAttributes & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(FFileName, LAttributes & ~FileAttributes.ReadOnly);
			}
		}

		public override void SaveData(string AData)
		{
			EnsureWritable(FileName);
			using (StreamWriter LWriter = new StreamWriter(new FileStream(FileName, FileMode.Create, FileAccess.Write)))
			{
				LWriter.Write(AData);
			}
		}

		public override void SaveBinaryData(Stream AData)
		{
			EnsureWritable(FileName);
			using (Stream LStream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
			{
				AData.Position = 0;
				StreamUtility.CopyStream(AData, LStream);
			}

		}

		public override string LoadData()
		{
			using (Stream LStream = File.OpenRead(FileName))
			{
				return new StreamReader(LStream).ReadToEnd();
			}
		}

		public override void LoadData(Stream AData)
		{
			using (Stream LStream = File.OpenRead(FileName))
			{
				AData.Position = 0;
				StreamUtility.CopyStream(LStream, AData);
			}
		}

		public const string CFileLocatorPrefix = "file:";
		
		public override bool LocatorNameMatches(string AName)
		{
			return
				AName != null
					&& AName.StartsWith(CFileLocatorPrefix)
					&& String.Equals(FFileName, AName.Substring(CFileLocatorPrefix.Length), StringComparison.OrdinalIgnoreCase);
		}
		
		public static bool IsFileLocator(string AName)
		{
			return AName.StartsWith(CFileLocatorPrefix);
		}

		public static DebugLocator GetLocatorName(string AFileName)
		{
			return new DebugLocator(CFileLocatorPrefix + AFileName, 1, 1);
		}
	}
}
