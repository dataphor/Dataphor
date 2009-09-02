using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class ProcessesView : UserControl
	{
		public ProcessesView()
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

		private bool FSupressDebuggerChange;
		
		private void Debugger_PropertyChanged(object ASender, PropertyChangedEventArgs AArgs)
		{
			if (!FSupressDebuggerChange && (AArgs.PropertyName == "IsStarted" || AArgs.PropertyName == "IsPaused"))
				RefreshDataView();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			try
			{
				FProcessDataView.Close();
				FProcessDataView.Session = null;
				UpdateButtonsEnabled();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}
		
		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FProcessDataView.Session = FDataphoria.DataSession;
			try
			{
				FProcessDataView.Open();
				UpdateButtonsEnabled();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}

		private void FAttachButton_Click(object sender, EventArgs e)
		{
			if (FProcessDataView.Active && !FProcessDataView.IsEmpty())
			{
				FSupressDebuggerChange = true;
				try
				{
					FDataphoria.Debugger.AttachProcess(FProcessDataView["ID"].AsInt32);
				}
				finally
				{
					FSupressDebuggerChange = false;
				}
				RefreshDataView();
			}
		}

		private void FDetachButton_Click(object sender, EventArgs e)
		{
			if (FProcessDataView.Active && !FProcessDataView.IsEmpty())
			{
				FSupressDebuggerChange = true;
				try
				{
					FDataphoria.Debugger.DetachProcess(FProcessDataView["ID"].AsInt32);
				}
				finally
				{
					FSupressDebuggerChange = false;
				}
				RefreshDataView();
			}
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshDataView();
		}

		private void RefreshDataView()
		{
			if (FProcessDataView.Active)
				FProcessDataView.Refresh();
		}

		private void FSessionDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			var LHasRow = FProcessDataView.Active && !FProcessDataView.IsEmpty();
			var LIsAttached = LHasRow && (bool)FProcessDataView["IsAttached"];
			FAttachButton.Enabled = LHasRow && !LIsAttached;
			FAttachContextMenuItem.Enabled = LHasRow && !LIsAttached;
			FDetachButton.Enabled = LHasRow && LIsAttached;
			FDetachContextMenuItem.Enabled = LHasRow && LIsAttached;
		}
	}
}
