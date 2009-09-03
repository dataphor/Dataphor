using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class SessionsView : UserControl
	{
		public SessionsView()
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
							FDataphoria_Connected(this, EventArgs.Empty);
					}
				}
			}
		}

		private bool FSupressDebuggerChange;
		
		private void Debugger_PropertyChanged(object ASender, string[] APropertyNames)
		{
			if (!FSupressDebuggerChange && Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "IsStarted" || AItem == "IsPaused"; }))
				RefreshDataView();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			try
			{
				FSessionDataView.Close();
				FSessionDataView.Session = null;
				UpdateButtonsEnabled();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}
		
		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FSessionDataView.Session = FDataphoria.DataSession;
			try
			{
				FSessionDataView.Open();
				UpdateButtonsEnabled();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}

		private void FAttachButton_Click(object sender, EventArgs e)
		{
			if (FSessionDataView.Active && !FSessionDataView.IsEmpty())
			{
				FSupressDebuggerChange = true;
				try
				{
					FDataphoria.Debugger.AttachSession(FSessionDataView["ID"].AsInt32);
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
			if (FSessionDataView.Active && !FSessionDataView.IsEmpty())
			{
				FSupressDebuggerChange = true;
				try
				{
					FDataphoria.Debugger.DetachSession(FSessionDataView["ID"].AsInt32);
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
			if (FSessionDataView.Active)
				FSessionDataView.Refresh();
		}

		private void FSessionDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonsEnabled();
		}

		private void UpdateButtonsEnabled()
		{
			var LHasRow = FSessionDataView.Active && !FSessionDataView.IsEmpty();
			var LIsAttached = LHasRow && (bool)FSessionDataView["IsAttached"];
			FAttachButton.Enabled = LHasRow && !LIsAttached;
			FAttachContextMenuItem.Enabled = LHasRow && !LIsAttached;
			FDetachButton.Enabled = LHasRow && LIsAttached;
			FDetachContextMenuItem.Enabled = LHasRow && LIsAttached;
		}
	}
}
