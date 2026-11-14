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
	public partial class StackView : UserControl
	{
		public StackView()
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
				if (_stackDataView.Active && !_stackDataView.IsEmpty())
					old = _stackDataView.ActiveRow;

				// Update the selected process
				_processIDParam.Value = _dataphoria.Debugger.SelectedProcessID;
				_callStackIndexParam.Value = _dataphoria.Debugger.SelectedCallStackIndex;
				_stackDataView.Open();

				// Attempt to seek to old position
				if (old != null)
					_stackDataView.Refresh(old);
			}
			else
				_stackDataView.Close();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			_stackDataView.Close();
			_stackDataView.Session = null;
			DeinitializeParamGroup();
		}

		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			_stackDataView.Session = _dataphoria.DataSession;
			InitializeParamGroup();
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			if (_stackDataView.Active)
				_stackDataView.Refresh();
		}

		private void FCopyMenuItem_Click(object sender, EventArgs e)
		{
			Clipboard.SetData(DataFormats.UnicodeText, _stackDataView["Value"].AsString);
		}

		private void _stackDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			FRefreshButton.Enabled = _stackDataView.Active;
			FCopyButton.Enabled = _stackDataView.Active && !_stackDataView.IsEmpty();
		}

		private void InitializeParamGroup()
		{
			if (_group == null)
			{
				_group = new DataSetParamGroup();
				_processIDParam = new DataSetParam() { Name = "AProcessID", DataType = _dataphoria.UtilityProcess.DataTypes.SystemInteger };
				_group.Params.Add(_processIDParam);
				_callStackIndexParam = new DataSetParam() { Name = "ACallStackIndex", DataType = _dataphoria.UtilityProcess.DataTypes.SystemInteger };
				_group.Params.Add(_callStackIndexParam);
				_stackDataView.ParamGroups.Add(_group);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (_group != null)
			{
				_stackDataView.ParamGroups.Remove(_group);
				_group.Dispose();
				_group = null;
			}
		}

		private DataSetParam _processIDParam;
		private DataSetParam _callStackIndexParam;
		private DataSetParamGroup _group;
	}
}
