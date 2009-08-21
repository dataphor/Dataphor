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
			if (!FSupressDebuggerChange && AArgs.PropertyName == "IsStarted")
				RefreshSessions();
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			try
			{
				FSessionDataView.Close();
				FSessionDataView.Session = null;
				UpdateButtonEnabled();
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
				UpdateButtonEnabled();
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
				RefreshSessions();
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
				RefreshSessions();
			}
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			RefreshSessions();
		}

		private void RefreshSessions()
		{
			if (FSessionDataView.Active)
				FSessionDataView.Refresh();
		}

		private void FSessionDataView_DataChanged(object sender, EventArgs e)
		{
			UpdateButtonEnabled();
		}

		private void UpdateButtonEnabled()
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
