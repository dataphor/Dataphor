using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class DebugProcessesView : UserControl
	{
		public DebugProcessesView()
		{
			InitializeComponent();
		}

		private IDataphoria FDataphoria;
		public IDataphoria Dataphoria
		{
			get { return FDataphoria; }
			set
			{
				if (FDataphoria != value)
				{
					if (FDataphoria != null)
					{
						FDataphoria.Disconnected -= new EventHandler(FDataphoria_Disconnected);
						FDataphoria.Connected -= new EventHandler(FDataphoria_Connected);
						FDataphoria.Debugger.PropertyChanged -= new PropertyChangedEventHandler(Debugger_PropertyChanged);
					}
					FDataphoria = value;
					if (FDataphoria != null)
					{
						FDataphoria.Disconnected += new EventHandler(FDataphoria_Disconnected);
						FDataphoria.Connected += new EventHandler(FDataphoria_Connected);
						FDataphoria.Debugger.PropertyChanged += new PropertyChangedEventHandler(Debugger_PropertyChanged);
					}
				}
			}
		}

		private bool FSupressSettingProcessID;
		
		private void Debugger_PropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			if (AArgs.PropertyName == "IsStarted")
				UpdateDataView();
			else if (!FSupressSettingProcessID && AArgs.PropertyName == "SelectedProcessID")
				UpdateSelected();
			else if (AArgs.PropertyName == "IsPaused")
				RefreshDataView();
		}

		private void UpdateSelected()
		{
			FSupressSettingProcessID = true;
			try
			{
				var LSelectedID = Dataphoria.Debugger.SelectedProcessID;
				if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty() && LSelectedID >= 0 && FDebugProcessDataView["Process_ID"].AsInt32 != LSelectedID)
				{
					foreach (Row LRow in FDebugProcessDataView)
						if ((int)LRow["Process_ID"] == LSelectedID)
							break;
				}	
			}
			finally
			{
				FSupressSettingProcessID = false;
			}
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			FDebugProcessDataView.Session = null;
		}
		
		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FDebugProcessDataView.Session = FDataphoria.DataSession;
		}

		private void FDetachButton_Click(object sender, EventArgs e)
		{
			if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
			{
				FDataphoria.Debugger.DetachProcess(FDebugProcessDataView["ID"].AsInt32);
				RefreshDataView();
			}
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshDataView();
		}
		
		private void RefreshDataView()
		{
			if (FDebugProcessDataView.Active)
			{
				FSupressSettingProcessID = true;
				try
				{
					FDebugProcessDataView.Refresh();
				}
				finally
				{
					FSupressSettingProcessID = false;
				}
			}
		}

		private void UpdateDataView()
		{
			try
			{
				FSupressSettingProcessID = true;
				try
				{
					if (Dataphoria.Debugger.IsStarted)
					{
						FDebugProcessDataView.Open();
						UpdateButtonEnabled();
					}
					else
					{
						FDebugProcessDataView.Close();
						UpdateButtonEnabled();
					}
				}
				finally
				{
					FSupressSettingProcessID = false;
				}
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}

		private void DataViewDataChanged(object sender, EventArgs e)
		{
			UpdateButtonEnabled();
			if (!FSupressSettingProcessID && Dataphoria.Debugger.IsStarted && FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
				Dataphoria.Debugger.SelectedProcessID = FDebugProcessDataView["Process_ID"].AsInt32;
		}

		private void UpdateButtonEnabled()
		{
			FRefreshButton.Enabled = FDebugProcessDataView.Active;
			FRefreshContextMenuItem.Enabled = FRefreshButton.Enabled;
			var LHasRow = FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty();
			FDetachButton.Enabled = LHasRow;
			FDetachContextMenuItem.Enabled = LHasRow;
		}
	}
}
