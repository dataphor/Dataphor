using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class CallStackView : UserControl
	{
		public CallStackView()
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
					}
					_dataphoria = value;
					if (_dataphoria != null)
					{
						_dataphoria.Disconnected += new EventHandler(FDataphoria_Disconnected);
						_dataphoria.Connected += new EventHandler(FDataphoria_Connected);
						_dataphoria.Debugger.PropertyChanged += Debugger_PropertyChanged;
						if (_dataphoria.IsConnected)
						{
							FDataphoria_Connected(this, EventArgs.Empty);
							UpdateDataView();
						}
					}
				}
			}
		}

		private void Debugger_PropertyChanged(object sender, string[] propertyNames)
		{
			try
			{
				if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "SelectedProcessID" || AItem == "IsPaused" || AItem == "SelectedCallStackIndex"; }))
					UpdateDataView();
			}
			catch (Exception exception)
			{
				_dataphoria.Warnings.AppendError(null, exception, false);
			}
		}

		private void UpdateDataView()
		{
			if (_dataphoria.Debugger.IsPaused && _dataphoria.Debugger.SelectedProcessID >= 0)
			{
				// Save old postion
				IRow old = null;
				if (FCallStackDataView.Active && !FCallStackDataView.IsEmpty())
					old = FCallStackDataView.ActiveRow;

				// Update the selected process
				_processIDParam.Value = _dataphoria.Debugger.SelectedProcessID;
				_selectedIndexParam.Value = _dataphoria.Debugger.SelectedCallStackIndex;
				FCallStackDataView.Open();

				// Attempt to seek to old position
				if (old != null)
					FCallStackDataView.Refresh(old);
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
			FCallStackDataView.Session = _dataphoria.DataSession;
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
			if (_group == null)
			{
				_group = new DataSetParamGroup();
				_processIDParam = new DataSetParam() { Name = "AProcessID", DataType = _dataphoria.UtilityProcess.DataTypes.SystemInteger };
				_group.Params.Add(_processIDParam);
				_selectedIndexParam = new DataSetParam() { Name = "ASelectedIndex", DataType = _dataphoria.UtilityProcess.DataTypes.SystemInteger };
				_group.Params.Add(_selectedIndexParam);
				FCallStackDataView.ParamGroups.Add(_group);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (_group != null)
			{
				FCallStackDataView.ParamGroups.Remove(_group);
				_group.Dispose();
				_group = null;
			}
		}

		private DataSetParam _processIDParam;
		private DataSetParam _selectedIndexParam;
		private DataSetParamGroup _group;
	}
}
