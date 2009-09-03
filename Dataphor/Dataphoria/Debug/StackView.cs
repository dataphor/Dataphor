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
						FDataphoria.Debugger.PropertyChanged -= Debugger_PropertyChanged;
					}
					FDataphoria = value;
					if (FDataphoria != null)
					{
						FDataphoria.Disconnected += new EventHandler(FDataphoria_Disconnected);
						FDataphoria.Connected += new EventHandler(FDataphoria_Connected);
						FDataphoria.Debugger.PropertyChanged += Debugger_PropertyChanged;
						if (FDataphoria.IsConnected)
						{
							FDataphoria_Connected(this, EventArgs.Empty);
							UpdateDataView();
						}
					}
				}
			}
		}

		private void Debugger_PropertyChanged(object ASender, string[] APropertyNames)
		{
			try
			{
				if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "SelectedProcessID" || AItem == "IsPaused" || AItem == "SelectedCallStackIndex"; }))
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
				// Save old postion
				Row LOld = null;
				if (FStackDataView.Active && !FStackDataView.IsEmpty())
					LOld = FStackDataView.ActiveRow;

				// Update the selected process
				FProcessIDParam.Value = FDataphoria.Debugger.SelectedProcessID;
				FCallStackIndexParam.Value = FDataphoria.Debugger.SelectedCallStackIndex;
				FStackDataView.Open();

				// Attempt to seek to old position
				if (LOld != null)
					FStackDataView.Refresh(LOld);
			}
			else
				FStackDataView.Close();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			FStackDataView.Close();
			FStackDataView.Session = null;
			DeinitializeParamGroup();
		}

		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FStackDataView.Session = FDataphoria.DataSession;
			InitializeParamGroup();
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			if (FStackDataView.Active)
				FStackDataView.Refresh();
		}

		private void FStackDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			FRefreshButton.Enabled = FStackDataView.Active;
		}

		private void InitializeParamGroup()
		{
			if (FGroup == null)
			{
				FGroup = new DataSetParamGroup();
				FProcessIDParam = new DataSetParam() { Name = "AProcessID", DataType = FDataphoria.UtilityProcess.DataTypes.SystemInteger };
				FGroup.Params.Add(FProcessIDParam);
				FCallStackIndexParam = new DataSetParam() { Name = "ACallStackIndex", DataType = FDataphoria.UtilityProcess.DataTypes.SystemInteger };
				FGroup.Params.Add(FCallStackIndexParam);
				FStackDataView.ParamGroups.Add(FGroup);
			}
		}

		private void DeinitializeParamGroup()
		{
			if (FGroup != null)
			{
				FStackDataView.ParamGroups.Remove(FGroup);
				FGroup.Dispose();
				FGroup = null;
			}
		}

		private DataSetParam FProcessIDParam;
		private DataSetParam FCallStackIndexParam;
		private DataSetParamGroup FGroup;
	}
}
