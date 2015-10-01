using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class DebugProcessesView : UserControl
	{
		public DebugProcessesView()
		{
			InitializeComponent();
		}

		private IDataphoria _dataphoria;
		public IDataphoria Dataphoria
		{
			get { return _dataphoria; }
			set
			{
				if (_dataphoria != value)
				{
					if (_dataphoria != null)
					{
						_dataphoria.Disconnected -= new EventHandler(FDataphoria_Disconnected);
						_dataphoria.Connected -= new EventHandler(FDataphoria_Connected);
						_dataphoria.Debugger.PropertyChanged -= Debugger_PropertyChanged;
						_dataphoria.Debugger.SessionAttached -= new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.SessionDetached -= new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.ProcessAttached -= new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.ProcessDetached -= new EventHandler(Debugger_AttachmentChanged);
					}
					_dataphoria = value;
					if (_dataphoria != null)
					{
						_dataphoria.Disconnected += new EventHandler(FDataphoria_Disconnected);
						_dataphoria.Connected += new EventHandler(FDataphoria_Connected);
						_dataphoria.Debugger.PropertyChanged += Debugger_PropertyChanged;
						_dataphoria.Debugger.SessionAttached += new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.SessionDetached += new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.ProcessAttached += new EventHandler(Debugger_AttachmentChanged);
						_dataphoria.Debugger.ProcessDetached += new EventHandler(Debugger_AttachmentChanged);
						if (_dataphoria.IsConnected)
						{
							FDataphoria_Connected(this, EventArgs.Empty);
							UpdateDataView();
						}
					}
				}
			}
		}

		private void Debugger_AttachmentChanged(object sender, EventArgs e)
		{
			RefreshDataView();
		}

		private void Debugger_PropertyChanged(object sender, string[] propertyNames)
		{
			try
			{
				if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "IsStarted" || AItem == "IsPaused" || AItem == "SelectedProcessID"; }))
					UpdateDataView();
			}
			catch (Exception exception)
			{
				_dataphoria.Warnings.AppendError(null, exception, false);
			}
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			FDebugProcessDataView.Close();
			FDebugProcessDataView.Session = null;
			DeinitializeParamGroup();
		}
		
		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FDebugProcessDataView.Session = _dataphoria.DataSession;
			InitializeParamGroup();
		}

		private void FDetachButton_Click(object sender, EventArgs e)
		{
			if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
				_dataphoria.Debugger.DetachProcess(FDebugProcessDataView["ID"].AsInt32);
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshDataView();
		}
		
		private void RefreshDataView()
		{
			if (FDebugProcessDataView.Active)
				FDebugProcessDataView.Refresh();
		}

		private void UpdateDataView()
		{
			if (Dataphoria.Debugger.IsStarted)
			{
				// Save old postion
				IRow old = null;
				if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
					old = FDebugProcessDataView.ActiveRow;
					
				// Update the selected process
				_selectedProcessIDParam.Value = _dataphoria.Debugger.SelectedProcessID;
				
				// Open the DataView
				FDebugProcessDataView.Open();

				// Attempt to seek to old position
				if (old != null)
					FDebugProcessDataView.Refresh(old);
			}
			else
				FDebugProcessDataView.Close();
		}

		private void FDebugProcessDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			FRefreshButton.Enabled = FDebugProcessDataView.Active;
			FRefreshContextMenuItem.Enabled = FRefreshButton.Enabled;
			var hasRow = FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty();
			FDetachButton.Enabled = hasRow;
			FDetachContextMenuItem.Enabled = hasRow;
			FSelectButton.Enabled = hasRow;
			FSelectContextMenuItem.Enabled = hasRow;
		}

		private void FSelectButton_Click(object sender, EventArgs e)
		{
			if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
				_dataphoria.Debugger.SelectedProcessID = FDebugProcessDataView["Process_ID"].AsInt32;
		}

		private void InitializeParamGroup()
		{
			if (_group == null)
			{
				_group = new DataSetParamGroup();
				_selectedProcessIDParam = new DataSetParam() { Name = "ASelectedProcessID", DataType = _dataphoria.UtilityProcess.DataTypes.SystemInteger };
				_group.Params.Add(_selectedProcessIDParam);
				FDebugProcessDataView.ParamGroups.Add(_group);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (_group != null)
			{
				FDebugProcessDataView.ParamGroups.Remove(_group);
				_group.Dispose();
				_group = null;
			}
		}

		private DataSetParam _selectedProcessIDParam;
		private DataSetParamGroup _group;
	}
}
