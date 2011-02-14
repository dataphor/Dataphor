using System;
using System.IO;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class FileDesignBuffer : DesignBuffer
	{
		public FileDesignBuffer(IDataphoria dataphoria, string fileName)
			: this(dataphoria, FileDesignBuffer.GetLocatorName(fileName))
		{ }

		public FileDesignBuffer(IDataphoria dataphoria, DebugLocator locator)
			: base(dataphoria, locator)
		{
			_fileName = locator.Locator.Substring(FileDesignBuffer.FileLocatorPrefix.Length);
		}

		// FileName

		private string _fileName;
		public string FileName
		{
			get { return _fileName; }
		}

		public override string GetDescription()
		{
			return Path.GetFileName(_fileName);
		}

		public override bool Equals(object objectValue)
		{
			FileDesignBuffer buffer = objectValue as FileDesignBuffer;
			if (buffer != null && String.Equals(buffer.FileName, FileName, StringComparison.OrdinalIgnoreCase))
				return true;
			else
				return base.Equals(objectValue);
		}

		public override int GetHashCode()
		{
			return FileName.ToLowerInvariant().GetHashCode();	// may not be in case-insensitive hashtable so make insensitive through ToLower()
		}

		public FileDesignBuffer PromptForBuffer(IDesigner designer)
		{
			return Dataphoria.PromptForFileBuffer(designer, FileName);
		}

		// Data

		private void EnsureWritable(string fileName)
		{
			if (File.Exists(fileName))
			{
				FileAttributes attributes = File.GetAttributes(fileName);
				if ((attributes & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(_fileName, attributes & ~FileAttributes.ReadOnly);
			}
		}

		public override void SaveData(string data)
		{
			EnsureWritable(FileName);
			using (StreamWriter writer = new StreamWriter(new FileStream(FileName, FileMode.Create, FileAccess.Write)))
			{
				writer.Write(data);
			}
		}

		public override void SaveBinaryData(Stream data)
		{
			EnsureWritable(FileName);
			using (Stream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write))
			{
				data.Position = 0;
				StreamUtility.CopyStream(data, stream);
			}

		}

		public override string LoadData()
		{
			using (Stream stream = File.OpenRead(FileName))
			{
				return new StreamReader(stream).ReadToEnd();
			}
		}

		public override void LoadData(Stream data)
		{
			using (Stream stream = File.OpenRead(FileName))
			{
				data.Position = 0;
				StreamUtility.CopyStream(stream, data);
			}
		}
		
		public void EnsureFile()
		{
			if (!File.Exists(FileName))
				SaveData(String.Empty);
		}

		public const string FileLocatorPrefix = "file:";
		
		public override bool LocatorNameMatches(string name)
		{
			return
				name != null
					&& name.StartsWith(FileLocatorPrefix)
					&& String.Equals(_fileName, name.Substring(FileLocatorPrefix.Length), StringComparison.OrdinalIgnoreCase);
		}
		
		public static bool IsFileLocator(string name)
		{
			return name.StartsWith(FileLocatorPrefix);
		}

		public static DebugLocator GetLocatorName(string fileName)
		{
			return new DebugLocator(FileLocatorPrefix + fileName, 1, 1);
		}
	}
}
