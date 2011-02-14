/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.Dataphoria.Designers
{
	public interface IDesignService
	{  		
		IDataphoria Dataphoria { get; }
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
		void StartAutoSave();
		void StopAutoSave();
		void SaveAs();
		event RequestHandler AfterSaveAsDocument;
		void SaveAsDocument();
		void SaveAsFile();
		DebugLocator GetLocator();
		bool LocatorNameMatches(string AName);
		event LocateEventHandler LocateRequested;
		void RequestLocate(DebugLocator LLocator);
	}

	public class DesignService : IDesignService
	{
		public const int DefaultSaveInterval = 120;	// 2 Minutes
		
		public DesignService(IDataphoria dataphoria, IDesigner designer)
		{
			_dataphoria = dataphoria;
			_designer = designer;
			if (designer != null)
				designer.Disposed += new EventHandler(DesignerDisposed);
		}
			  
		// Dataphoria

		private IDataphoria _dataphoria;
		public IDataphoria Dataphoria
		{
			get { return _dataphoria; }
		}

		// Designer

		private IDesigner _designer;
		public IDesigner Designer
		{
			get { return _designer; }
		}

		private void DesignerDisposed(object sender, EventArgs e)
		{
			StopAutoSave(); 
			if (_buffer != null)
				UnregisterDesigner(_buffer);  			
		}

		public void RegisterDesigner(DesignBuffer buffer)
		{
			Dataphoria.RegisterDesigner(buffer, _designer);
		}

		public void UnregisterDesigner(DesignBuffer buffer)
		{
			Dataphoria.UnregisterDesigner(buffer);
		}

		// IsModified

		private bool _isModified;
		/// <remarks> ModifiedChanged will not occur if IsModified is set directly. </remarks>
		public bool IsModified
		{
			get { return _isModified; }
			set { _isModified = value; }
		}

		/// <summary> Returns true if the service or any of the dependant services (transitively) are modified. </summary>
		public bool AnyModified 
		{ 
			get { return _isModified || _dependants.Exists(delegate(IDesignService AService) { return AService.AnyModified; }); } 
		}

		public void SetModified(bool tempValue)
		{
			if (tempValue != _isModified)
			{
				_isModified = tempValue;
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
				switch (MessageBox.Show(String.Format(Strings.DocumentModifiedText, GetDescription()), Strings.DocumentModifiedCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1))
				{
					case DialogResult.Yes : Save(); break;
					case DialogResult.Cancel : throw new AbortException();
				}
		}

		private List<IDesignService> _dependants = new List<IDesignService>();
		public List<IDesignService> Dependants
		{
			get { return _dependants; }
		}

		// Data callbacks

		public event RequestHandler OnRequestLoad;
		protected virtual void RequestLoad(DesignBuffer buffer)
		{
			if (OnRequestLoad != null)
				OnRequestLoad(this, buffer);
		}

		public event RequestHandler OnRequestSave;
		protected virtual void RequestSave(DesignBuffer buffer)
		{
			foreach (IDesignService dependant in _dependants)
				dependant.Save();

			if (OnRequestSave != null)
				OnRequestSave(this, buffer);
		}

		// DesignBuffer

		private DesignBuffer _buffer;
		public DesignBuffer Buffer
		{
			get { return _buffer; }
		}

		public void SetBuffer(DesignBuffer buffer)
		{
			if (buffer != _buffer)
			{
				if (_buffer != null)
					UnregisterDesigner(_buffer);
				_buffer = buffer;
				if (_buffer != null)
					RegisterDesigner(_buffer);
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
			if (_buffer == null)
				return Strings.UntitledDocumentDescription;
			else
				return _buffer.GetDescription();
		}

		public void ValidateBuffer(DesignBuffer buffer)
		{
			Dataphoria.CheckNotRegistered(buffer);
		}

		// I/O

		public void Open(DesignBuffer buffer)
		{
			ValidateBuffer(buffer);
			CheckModified();
			RequestLoad(buffer);
			SetBuffer(buffer);
			SetModified(false);
			if (buffer.Locator != null)
				RequestLocate(buffer.Locator);
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
			if (_buffer == null)
				SaveAs();
			else
				Save(_buffer);
		}

		Timer _timer;
		public void StartAutoSave()
		{
			if (_timer == null)
			{
				_timer = new Timer();
				_timer.Tick += new EventHandler(AutoSaveTimer_Tick);
				_timer.Interval = DefaultSaveInterval * 1000;
			}
			_timer.Start();			
		}

		private void AutoSaveTimer_Tick(object sender, EventArgs e)
		{
			if (IsModified)
				Save();
		} 
		
		public void StopAutoSave()
		{
			if (_timer != null)
			{
				try
				{
					_timer.Stop();
				}
				finally
				{
					_timer = null;
				}
			}
		}

		private void Save(DesignBuffer buffer)
		{
			RequestSave(buffer);
			SetBuffer(buffer);
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
			DocumentDesignBuffer buffer;
			DocumentDesignBuffer current = _buffer as DocumentDesignBuffer;
			if (current != null)
				buffer = current.PromptForBuffer(_designer);
			else
				buffer = Dataphoria.PromptForDocumentBuffer(_designer, Dataphoria.GetCurrentLibraryName(), String.Empty);
			ValidateBuffer(buffer);
			Save(buffer);
			if (AfterSaveAsDocument != null)
				AfterSaveAsDocument(this, buffer);
		}

		public void SaveAsFile()
		{
			FileDesignBuffer buffer;
			FileDesignBuffer current = _buffer as FileDesignBuffer;
			if (current != null)
				buffer = current.PromptForBuffer(_designer);
			else
				buffer = Dataphoria.PromptForFileBuffer(_designer, String.Empty);
			ValidateBuffer(buffer);
			Save(buffer);
		}

		private void PreparePromptForFileOrDocument(Frontend.Client.IFormInterface form)
		{
			form.MainSource.OpenState = DAE.Client.DataSetState.Edit;
			form.MainSource.RefreshAfterPost = false;
		}

		private bool PromptFileOrDocument()
		{
			Frontend.Client.Windows.IWindowsFormInterface form = 
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
				if (form.ShowModal(Frontend.Client.FormMode.Edit) != DialogResult.OK)
					throw new AbortException();
				return form.MainSource.DataView["Main.Value"].AsBoolean;
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		public event LocateEventHandler LocateRequested;

		public void RequestLocate(DebugLocator locator)
		{
			if (LocateRequested != null)
				LocateRequested(this, locator);
		}

		public DebugLocator GetLocator()
		{
			if (_buffer == null || _buffer.Locator == null)
				return null;
			else
				return new DebugLocator(_buffer.Locator.Locator, 1, 1);
		}

		public bool LocatorNameMatches(string name)
		{
			if (_buffer == null)
				return false;
			else
				return _buffer.LocatorNameMatches(name);
		}
		
		
	}

	public delegate void RequestHandler(DesignService AService, DesignBuffer ABuffer);
	public delegate void LocateEventHandler(DesignService AService, DebugLocator ALocator);
}
