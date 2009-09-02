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

		private void Debugger_PropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			try
			{
				if (AArgs.PropertyName == "IsStarted" || AArgs.PropertyName == "IsPaused" || AArgs.PropertyName == "SelectedProcessID")
					UpdateDataView();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
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
			FDebugProcessDataView.Session = FDataphoria.DataSession;
			InitializeParamGroup();
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
				FDebugProcessDataView.Refresh();
		}

		private void UpdateDataView()
		{
			if (Dataphoria.Debugger.IsStarted)
			{
				FSelectedProcessIDParam.Value = FDataphoria.Debugger.SelectedProcessID;
				FDebugProcessDataView.Open();
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
			var LHasRow = FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty();
			FDetachButton.Enabled = LHasRow;
			FDetachContextMenuItem.Enabled = LHasRow;
			FSelectButton.Enabled = LHasRow;
			FSelectContextMenuItem.Enabled = LHasRow;
		}

		private void FSelectButton_Click(object sender, EventArgs e)
		{
			if (FDebugProcessDataView.Active && !FDebugProcessDataView.IsEmpty())
				FDataphoria.Debugger.SelectedProcessID = FDebugProcessDataView["Process_ID"].AsInt32;
		}

		private void InitializeParamGroup()
		{
			if (FGroup == null)
			{
				FGroup = new DataSetParamGroup();
				FSelectedProcessIDParam = new DataSetParam() { Name = "ASelectedProcessID", DataType = FDataphoria.UtilityProcess.DataTypes.SystemInteger };
				FGroup.Params.Add(FSelectedProcessIDParam);
				FDebugProcessDataView.ParamGroups.Add(FGroup);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (FGroup != null)
			{
				FDebugProcessDataView.ParamGroups.Remove(FGroup);
				FGroup.Dispose();
				FGroup = null;
			}
		}

		private DataSetParam FSelectedProcessIDParam;
		private DataSetParamGroup FGroup;
	}
}
