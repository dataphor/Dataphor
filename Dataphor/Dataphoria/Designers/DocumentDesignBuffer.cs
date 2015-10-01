using System;
using System.IO;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class DocumentDesignBuffer : DesignBuffer
	{
		public DocumentDesignBuffer(IDataphoria dataphoria, string libraryName, string documentName)
			: this(dataphoria, new DebugLocator(DocumentDesignBuffer.GetLocatorName(libraryName, documentName), 1, 1))
		{ }
		
		public DocumentDesignBuffer(IDataphoria dataphoria, DebugLocator locator)
			: base(dataphoria, locator)
		{
			var segments = locator.Locator.Split(':');
			if (segments.Length == 3)
			{
				_libraryName = DAE.Schema.Object.EnsureRooted(segments[1]);
				_documentName = segments[2];
			}
			else
				Error.Fail("DocumentDesignBuffer given locator with other than 3 segments.");
		}

		private string _libraryName = String.Empty;
		public string LibraryName
		{
			get { return _libraryName; }
		}

		private string _documentName = String.Empty;
		public string DocumentName
		{
			get { return _documentName; }
		}

		private string _documentType = String.Empty;
		/// <remarks> Only used when creating a new document. </remarks>
		public string DocumentType
		{
			get { return _documentType; }
			set { _documentType = value; }
		}

		public override string GetDescription()
		{
			return String.Format(@"{0}: {1}", LibraryName, DocumentName);
		}

		public override bool Equals(object objectValue)
		{
			DocumentDesignBuffer buffer = objectValue as DocumentDesignBuffer;
			if ((buffer != null) && (buffer.LibraryName == LibraryName) && (buffer.DocumentName == DocumentName))
				return true;
			else
				return base.Equals(objectValue);
		}

		public override int GetHashCode()
		{
			return LibraryName.GetHashCode() ^ DocumentName.GetHashCode();
		}

		public DocumentDesignBuffer PromptForBuffer(IDesigner designer)
		{
			return Dataphoria.PromptForDocumentBuffer(designer, LibraryName, DocumentName);
		}

		// Data

		public override void SaveData(string data)
		{
			InternalSaveData(data, false);
			Dataphoria.RefreshDocuments(_libraryName);
		}

		public override void SaveBinaryData(Stream data)
		{
			InternalSaveData(data, true);
			Dataphoria.RefreshDocuments(_libraryName);
		}

		public override string LoadData()
		{
			using (DAE.Runtime.Data.IScalar scalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.Load('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
			{
				return scalar.AsString;
			}
		}

		public override void LoadData(Stream data)
		{
			using (DAE.Runtime.Data.IScalar scalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.LoadBinary('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
			{
				data.Position = 0;
				Stream stream = scalar.OpenStream();
				try
				{
					StreamUtility.CopyStream(stream, data);
				}
				finally
				{
					stream.Close();
				}
			}
		}

		private void InternalSaveData(object data, bool binary)
		{
			if (binary)
				throw new NotSupportedException("InternalSaveData called with ABinary true");

			DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
			paramsValue.Add(new DAE.Runtime.DataParam("LibraryName", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DAE.Schema.Object.EnsureRooted(LibraryName)));
			paramsValue.Add(new DAE.Runtime.DataParam("DocumentName", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DAE.Schema.Object.EnsureRooted(DocumentName)));
			paramsValue.Add(new DAE.Runtime.DataParam("DocumentType", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DocumentType));
			paramsValue.Add(new DAE.Runtime.DataParam("Data", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, data));

			DAE.IServerStatementPlan plan = Dataphoria.UtilityProcess.PrepareStatement(".Frontend.CreateAndSave(LibraryName, DocumentName, DocumentType, Data)", paramsValue);
			try
			{
				plan.Execute(paramsValue);
			}
			finally
			{
				Dataphoria.UtilityProcess.UnprepareStatement(plan);
			}
		}

		public const string DocLocatorPrefix = "doc:";

		public override bool LocatorNameMatches(string name)
		{
			if (name != null && name.StartsWith(DocLocatorPrefix))
			{
				var segments = name.Split(':');
				return segments.Length == 3 && _libraryName == segments[1] && _documentName == segments[2];
			}
			else
				return false;
		}
		
		public static bool IsDocumentLocator(string name)
		{
			return name.StartsWith(DocLocatorPrefix);
		}

		public static string GetLocatorName(string libraryName, string documentName)
		{
			return DocLocatorPrefix + libraryName + ":" + documentName;
		}
	}
}
