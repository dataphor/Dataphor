using System;
using System.IO;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public class DocumentDesignBuffer : DesignBuffer
	{
		public DocumentDesignBuffer(IDataphoria ADataphoria, string ALibraryName, string ADocumentName)
			: this(ADataphoria, new DebugLocator(DocumentDesignBuffer.GetLocatorName(ALibraryName, ADocumentName), 1, 1))
		{ }
		
		public DocumentDesignBuffer(IDataphoria ADataphoria, DebugLocator ALocator)
			: base(ADataphoria, ALocator)
		{
			var LSegments = ALocator.Locator.Split(':');
			if (LSegments.Length == 3)
			{
				FLibraryName = LSegments[1];
				FDocumentName = LSegments[2];
			}
			else
				Error.Fail("DocumentDesignBuffer given locator with other than 3 segments.");
		}

		private string FLibraryName = String.Empty;
		public string LibraryName
		{
			get { return FLibraryName; }
		}

		private string FDocumentName = String.Empty;
		public string DocumentName
		{
			get { return FDocumentName; }
		}

		private string FDocumentType = String.Empty;
		/// <remarks> Only used when creating a new document. </remarks>
		public string DocumentType
		{
			get { return FDocumentType; }
			set { FDocumentType = value; }
		}

		public override string GetDescription()
		{
			return String.Format(@"{0}: {1}", LibraryName, DocumentName);
		}

		public override bool Equals(object AObject)
		{
			DocumentDesignBuffer LBuffer = AObject as DocumentDesignBuffer;
			if ((LBuffer != null) && (LBuffer.LibraryName == LibraryName) && (LBuffer.DocumentName == DocumentName))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return LibraryName.GetHashCode() ^ DocumentName.GetHashCode();
		}

		public DocumentDesignBuffer PromptForBuffer(IDesigner ADesigner)
		{
			return Dataphoria.PromptForDocumentBuffer(ADesigner, LibraryName, DocumentName);
		}

		// Data

		public override void SaveData(string AData)
		{
			InternalSaveData(AData, false);
			Dataphoria.RefreshDocuments(FLibraryName);
		}

		public override void SaveBinaryData(Stream AData)
		{
			InternalSaveData(AData, true);
			Dataphoria.RefreshDocuments(FLibraryName);
		}

		public override string LoadData()
		{
			using (DAE.Runtime.Data.Scalar LScalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.Load('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
			{
				return LScalar.AsString;
			}
		}

		public override void LoadData(Stream AData)
		{
			using (DAE.Runtime.Data.Scalar LScalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.LoadBinary('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
			{
				AData.Position = 0;
				Stream LStream = LScalar.OpenStream();
				try
				{
					StreamUtility.CopyStream(LStream, AData);
				}
				finally
				{
					LStream.Close();
				}
			}
		}

		private void InternalSaveData(object AData, bool ABinary)
		{
			if (ABinary)
				throw new NotSupportedException("InternalSaveData called with ABinary true");

			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			LParams.Add(new DAE.Runtime.DataParam("LibraryName", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DAE.Schema.Object.EnsureRooted(LibraryName)));
			LParams.Add(new DAE.Runtime.DataParam("DocumentName", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DAE.Schema.Object.EnsureRooted(DocumentName)));
			LParams.Add(new DAE.Runtime.DataParam("DocumentType", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, DocumentType));
			LParams.Add(new DAE.Runtime.DataParam("Data", Dataphoria.UtilityProcess.DataTypes.SystemString, DAE.Language.Modifier.Const, AData));

			DAE.IServerStatementPlan LPlan = Dataphoria.UtilityProcess.PrepareStatement(".Frontend.CreateAndSave(LibraryName, DocumentName, DocumentType, Data)", LParams);
			try
			{
				LPlan.Execute(LParams);
			}
			finally
			{
				Dataphoria.UtilityProcess.UnprepareStatement(LPlan);
			}
		}

		public const string CDocLocatorPrefix = "doc:";

		public override bool LocatorNameMatches(string AName)
		{
			if (AName != null && AName.StartsWith(CDocLocatorPrefix))
			{
				var LSegments = AName.Split(':');
				return LSegments.Length == 3 && FLibraryName == LSegments[1] && FDocumentName == LSegments[2];
			}
			else
				return false;
		}
		
		public static bool IsDocumentLocator(string AName)
		{
			return AName.StartsWith(CDocLocatorPrefix);
		}

		public static string GetLocatorName(string ALibraryName, string ADocumentName)
		{
			return CDocLocatorPrefix + ALibraryName + ":" + ADocumentName;
		}
	}
}
