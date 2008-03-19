/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public interface IDesignService
	{
		Dataphoria Dataphoria { get; }
		IDesigner Designer { get; }
		void RegisterDesigner(DesignBuffer ABuffer);
		void UnregisterDesigner(DesignBuffer ABuffer);
		bool IsModified { get; }
		bool AnyModified { get; }
		void SetModified(bool AValue);
		event EventHandler OnModifiedChanged;
		void CheckModified();
		List<IDesignService> Dependants { get; }
		event RequestHandler OnRequestLoad;
		event RequestHandler OnRequestSave;
		DesignBuffer Buffer { get; }
		void SetBuffer(DesignBuffer ABuffer);
		event EventHandler OnNameChanged;
		string GetDescription();
		void ValidateBuffer(DesignBuffer ABuffer);
		void Open(DesignBuffer ABuffer);
		void New();
		void Save();
		void SaveAs();
		event RequestHandler AfterSaveAsDocument;
		void SaveAsDocument();
		void SaveAsFile();
	}

	public class DesignService : IDesignService
	{
		public DesignService(Dataphoria ADataphoria, IDesigner ADesigner)
		{
			FDataphoria = ADataphoria;
			FDesigner = ADesigner;
			if (ADesigner != null)
				ADesigner.Disposed += new EventHandler(DesignerDisposed);
		}

		// Dataphoria

		private Dataphoria FDataphoria;
		public Dataphoria Dataphoria
		{
			get { return FDataphoria; }
		}

		// Designer

		private IDesigner FDesigner;
		public IDesigner Designer
		{
			get { return FDesigner; }
		}

		private void DesignerDisposed(object sender, EventArgs e)
		{
			if (FBuffer != null)
				UnregisterDesigner(FBuffer);
		}

		public void RegisterDesigner(DesignBuffer ABuffer)
		{
			Dataphoria.RegisterDesigner(ABuffer, FDesigner);
		}

		public void UnregisterDesigner(DesignBuffer ABuffer)
		{
			Dataphoria.UnregisterDesigner(ABuffer);
		}

		// IsModified

		private bool FIsModified;
		/// <remarks> ModifiedChanged will not occur if IsModified is set directly. </remarks>
		public bool IsModified
		{
			get { return FIsModified; }
			set { FIsModified = value; }
		}

		/// <summary> Returns true if the service or any of the dependant services (transitively) are modified. </summary>
		public bool AnyModified 
		{ 
			get { return FIsModified || FDependants.Exists(delegate(IDesignService AService) { return AService.AnyModified; }); } 
		}

		public void SetModified(bool AValue)
		{
			if (AValue != FIsModified)
			{
				FIsModified = AValue;
				ModifiedChanged();
			}
		}

		public event EventHandler OnModifiedChanged;

		private void ModifiedChanged()
		{
			if (OnModifiedChanged != null)
				OnModifiedChanged(this, EventArgs.Empty);
		}

		public void CheckModified()
		{
			if (AnyModified)
				switch (MessageBox.Show(Strings.Get("DocumentModifiedText", GetDescription()), Strings.Get("DocumentModifiedCaption"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1))
				{
					case DialogResult.Yes : Save(); break;
					case DialogResult.Cancel : throw new AbortException();
				}
		}

		private List<IDesignService> FDependants = new List<IDesignService>();
		public List<IDesignService> Dependants
		{
			get { return FDependants; }
		}

		// Data callbacks

		public event RequestHandler OnRequestLoad;
		protected virtual void RequestLoad(DesignBuffer ABuffer)
		{
			if (OnRequestLoad != null)
				OnRequestLoad(this, ABuffer);
		}

		public event RequestHandler OnRequestSave;
		protected virtual void RequestSave(DesignBuffer ABuffer)
		{
			foreach (IDesignService LDependant in FDependants)
				LDependant.Save();

			if (OnRequestSave != null)
				OnRequestSave(this, ABuffer);
		}

		// DesignBuffer

		private DesignBuffer FBuffer;
		public DesignBuffer Buffer
		{
			get { return FBuffer; }
		}

		public void SetBuffer(DesignBuffer ABuffer)
		{
			if (ABuffer != FBuffer)
			{
				if (FBuffer != null)
					UnregisterDesigner(FBuffer);
				FBuffer = ABuffer;
				if (FBuffer != null)
					RegisterDesigner(FBuffer);
				NameChanged();
			}
		}

		public event EventHandler OnNameChanged;
		protected virtual void NameChanged()
		{
			if (OnNameChanged != null)
				OnNameChanged(this, EventArgs.Empty);
		}

		public string GetDescription()
		{
			if (FBuffer == null)
				return Strings.Get("UntitledDocumentDescription");
			else
				return FBuffer.GetDescription();
		}

		public void ValidateBuffer(DesignBuffer ABuffer)
		{
			Dataphoria.CheckNotRegistered(ABuffer);
		}

		// I/O

		public void Open(DesignBuffer ABuffer)
		{
			ValidateBuffer(ABuffer);
			CheckModified();
			RequestLoad(ABuffer);
			SetBuffer(ABuffer);
			SetModified(false);
		}

		public void New()
		{
			CheckModified();
			SetBuffer(null);
			SetModified(false);
		}

		public void Save()
		{
			//don't check if modified (always save)
			if (FBuffer == null)
				SaveAs();
			else
				Save(FBuffer);
		}

		private void Save(DesignBuffer ABuffer)
		{
			RequestSave(ABuffer);
			SetBuffer(ABuffer);
			SetModified(false);
		}

		public void SaveAs()
		{
			if (PromptFileOrDocument())
				SaveAsDocument();
			else
				SaveAsFile();
		}

		public event RequestHandler AfterSaveAsDocument;

		public void SaveAsDocument()
		{
			DocumentDesignBuffer LBuffer;
			DocumentDesignBuffer LCurrent = FBuffer as DocumentDesignBuffer;
			if (LCurrent != null)
				LBuffer = LCurrent.PromptForBuffer(FDesigner);
			else
				LBuffer = Dataphoria.PromptForDocumentBuffer(FDesigner, Dataphoria.GetCurrentLibraryName(), String.Empty);
			ValidateBuffer(LBuffer);
			Save(LBuffer);
			if (AfterSaveAsDocument != null)
				AfterSaveAsDocument(this, LBuffer);
		}

		public void SaveAsFile()
		{
			FileDesignBuffer LBuffer;
			FileDesignBuffer LCurrent = FBuffer as FileDesignBuffer;
			if (LCurrent != null)
				LBuffer = LCurrent.PromptForBuffer(FDesigner);
			else
				LBuffer = Dataphoria.PromptForFileBuffer(FDesigner, String.Empty);
			ValidateBuffer(LBuffer);
			Save(LBuffer);
		}

		private void PreparePromptForFileOrDocument(Frontend.Client.IFormInterface AForm)
		{
			AForm.MainSource.OpenState = DAE.Client.DataSetState.Edit;
			AForm.MainSource.RefreshAfterPost = false;
		}

		private bool PromptFileOrDocument()
		{
			Frontend.Client.Windows.IWindowsFormInterface LForm = 
				Dataphoria.FrontendSession.LoadForm
				(
					null, 
					@"
						.Frontend.Derive
						(
							'
								TableDee 
									add 
									{
										true Value tags { DAE.IsComputed = ''false'', Frontend.ElementType = ''Choice'', Frontend.Choice.Items = ''Document=True,File=False'' }
									}
									adorn tags { Frontend.Caption = ''Document or File?'' }
							', 
							'Edit'
						)
					",
					delegate(Frontend.Client.IFormInterface AForm)
					{
						AForm.MainSource.OpenState = DAE.Client.DataSetState.Edit;
						AForm.MainSource.RefreshAfterPost = false;
					}
				);
			try
			{
				if (LForm.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				return LForm.MainSource.DataView["Main.Value"].AsBoolean;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}
	}

	public delegate void RequestHandler(DesignService AService, DesignBuffer ABuffer);
}
