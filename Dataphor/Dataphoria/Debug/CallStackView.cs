using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class CallStackView : UserControl
	{
		public CallStackView()
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
				if (AArgs.PropertyName == "SelectedProcessID" || AArgs.PropertyName == "IsPaused" || AArgs.PropertyName == "SelectedCallStackIndex")
					UpdateDataView();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}

		private void UpdateDataView()
		{
			if (FDataphoria.Debugger.IsPaused && FDataphoria.Debugger.SelectedProcessID >= 0)
			{
				FProcessIDParam.Value = FDataphoria.Debugger.SelectedProcessID;
				FSelectedIndexParam.Value = FDataphoria.Debugger.SelectedCallStackIndex;
				FCallStackDataView.Open();
			}
			else
				FCallStackDataView.Close();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			FCallStackDataView.Close();
			FCallStackDataView.Session = null;
			DeinitializeParamGroup();
		}

		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FCallStackDataView.Session = FDataphoria.DataSession;
			InitializeParamGroup();
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			if (FCallStackDataView.Active)
				FCallStackDataView.Refresh();
		}

		private void FSelectButton_Click(object sender, EventArgs e)
		{
			if (FCallStackDataView.Active && !FCallStackDataView.IsEmpty())
				Dataphoria.Debugger.SelectedCallStackIndex = FCallStackDataView["Index"].AsInt32;
		}

		private void FCallStackDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			FRefreshButton.Enabled = FCallStackDataView.Active;
			FSelectButton.Enabled = FCallStackDataView.Active && !FCallStackDataView.IsEmpty();
		}

		private void InitializeParamGroup()
		{
			if (FGroup == null)
			{
				FGroup = new DataSetParamGroup();
				FProcessIDParam = new DataSetParam() { Name = "AProcessID", DataType = FDataphoria.UtilityProcess.DataTypes.SystemInteger };
				FGroup.Params.Add(FProcessIDParam);
				FSelectedIndexParam = new DataSetParam() { Name = "ASelectedIndex", DataType = FDataphoria.UtilityProcess.DataTypes.SystemInteger };
				FGroup.Params.Add(FSelectedIndexParam);
				FCallStackDataView.ParamGroups.Add(FGroup);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (FGroup != null)
			{
				FCallStackDataView.ParamGroups.Remove(FGroup);
				FGroup.Dispose();
				FGroup = null;
			}
		}

		private DataSetParam FProcessIDParam;
		private DataSetParam FSelectedIndexParam;
		private DataSetParamGroup FGroup;
	}
}
