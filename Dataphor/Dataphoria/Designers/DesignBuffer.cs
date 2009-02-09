/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Text;
using System.Reflection;

using Alphora.Dataphor.Dataphoria;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public abstract class DesignBuffer
	{
		public DesignBuffer(IDataphoria ADataphoria)
		{
			FDataphoria = ADataphoria;
		}

		// Dataphoria

		private IDataphoria FDataphoria;
		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
		}

		public abstract string GetDescription();

		// Data

		public abstract void SaveData(string AData);

		public abstract void SaveBinaryData(Stream AData);

		public abstract string LoadData();

		public abstract void LoadData(Stream AData);

	}

	public class FileDesignBuffer : DesignBuffer
	{
		public FileDesignBuffer(IDataphoria ADataphoria, string AFileName) : base(ADataphoria) 
		{
			FFileName = ( AFileName == null ? String.Empty : AFileName );
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
			if ((LBuffer != null) && (String.Compare(LBuffer.FileName, FileName, true) == 0))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return FileName.ToLower().GetHashCode();	// may not be in case-insensitive hashtable so make insensitive through ToLower()
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
	}

	public class DocumentDesignBuffer : DesignBuffer
	{
		public DocumentDesignBuffer(IDataphoria ADataphoria, string ALibraryName, string ADocumentName) : base(ADataphoria) 
		{
			FLibraryName = (ALibraryName == null ? String.Empty : ALibraryName);
			FDocumentName = (ADocumentName == null ? String.Empty : ADocumentName);
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
			using (DAE.Runtime.Data.DataValue LScalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.Load('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
			{
				return LScalar.AsString;
			}
		}

		public override void LoadData(Stream AData)
		{
			using (DAE.Runtime.Data.DataValue LScalar = Dataphoria.FrontendSession.Pipe.RequestDocument(String.Format(".Frontend.LoadBinary('{0}', '{1}')", DAE.Schema.Object.EnsureRooted(LibraryName), DAE.Schema.Object.EnsureRooted(DocumentName))))
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
			DAE.Schema.ScalarType LType;
			if (ABinary)
				LType = Dataphoria.FrontendSession.Pipe.Process.DataTypes.SystemBinary;
			else
				LType = Dataphoria.FrontendSession.Pipe.Process.DataTypes.SystemString;
			Frontend.Client.Pipe LPipe = Dataphoria.FrontendSession.Pipe;
			using (DAE.Runtime.Data.Scalar LLibraryName = new DAE.Runtime.Data.Scalar(LPipe.Process, LPipe.Process.DataTypes.SystemName, DAE.Schema.Object.EnsureRooted(LibraryName)))
			{
				using (DAE.Runtime.Data.Scalar LDocumentName = new DAE.Runtime.Data.Scalar(LPipe.Process, LPipe.Process.DataTypes.SystemName, DAE.Schema.Object.EnsureRooted(DocumentName)))
				{
					using (DAE.Runtime.Data.Scalar LDocumentType = new DAE.Runtime.Data.Scalar(LPipe.Process, LPipe.Process.DataTypes.SystemString, DocumentType))
					{
						using 
						(
							DAE.Runtime.Data.Scalar LData = 
								(
									ABinary ? 
									new DAE.Runtime.Data.Scalar(LPipe.Process, LType, (Stream)AData) :
									new DAE.Runtime.Data.Scalar(LPipe.Process, LType, AData)
								)
						)
						{
							DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
							LParams.Add(new DAE.Runtime.DataParam("LibraryName", LPipe.Process.DataTypes.SystemString, DAE.Language.Modifier.Const, LLibraryName));
							LParams.Add(new DAE.Runtime.DataParam("DocumentName", LPipe.Process.DataTypes.SystemString, DAE.Language.Modifier.Const, LDocumentName));
							LParams.Add(new DAE.Runtime.DataParam("DocumentType", LPipe.Process.DataTypes.SystemString, DAE.Language.Modifier.Const, LDocumentType));
							LParams.Add(new DAE.Runtime.DataParam("Data", LType, DAE.Language.Modifier.Const, LData));
							
							DAE.IServerStatementPlan LPlan = LPipe.Process.PrepareStatement(".Frontend.CreateAndSave(LibraryName, DocumentName, DocumentType, Data)", LParams);
							try
							{
								LPlan.Execute(LParams);
							}
							finally
							{
								LPipe.Process.UnprepareStatement(LPlan);
							}
						}
					}
				}
			}
		}
	}

	public class PropertyDesignBuffer : DesignBuffer
	{
		public PropertyDesignBuffer(IDataphoria ADataphoria, object AInstance, PropertyDescriptor ADescriptor) : base(ADataphoria) 
		{
			FInstance = AInstance;
			FDescriptor = ADescriptor;
		}

		// Instance

		private object FInstance;
		public object Instance
		{
			get { return FInstance; }
		}

		// Descriptor

		private PropertyDescriptor FDescriptor;
		public PropertyDescriptor Descriptor
		{
			get { return FDescriptor; }
		}

		// DesignBuffer

		public override string GetDescription()
		{
			Frontend.Client.INode LNode = FInstance as Frontend.Client.INode;
			return (LNode != null ? LNode.Name + "." : String.Empty) + FDescriptor.Name;
		}

		public override bool Equals(object AObject)
		{
			PropertyDesignBuffer LBuffer = AObject as PropertyDesignBuffer;
			if ((LBuffer != null) && Object.ReferenceEquals(FInstance, LBuffer.Instance) && FDescriptor.Equals(LBuffer.Descriptor))
				return true;
			else
				return base.Equals(AObject);
		}

		public override int GetHashCode()
		{
			return FInstance.GetHashCode() ^ FDescriptor.GetHashCode();
		}

		// Data

		public override void SaveData(string AData)
		{
			FDescriptor.SetValue(FInstance, AData);
		}

		public override void SaveBinaryData(Stream AData)
		{
			Error.Fail("SaveBinaryData is not supported for PropertyDesignBuffer");
		}

		public override string LoadData()
		{
			return (string)FDescriptor.GetValue(FInstance);
		}

		public override void LoadData(Stream AData)
		{
			Error.Fail("LoadData(Stream) is not supported for PropertyDesignBuffer");
		}
	}
}
