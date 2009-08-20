using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class SessionView : UserControl
	{
		public SessionView()
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
					}
					FDataphoria = value;
					if (FDataphoria != null)
					{
						FDataphoria.Disconnected += new EventHandler(FDataphoria_Disconnected);
						FDataphoria.Connected += new EventHandler(FDataphoria_Connected);
					}
				}
			}
		}

		private void FDataphoria_Disconnected(object sender, EventArgs e)
		{
			FSessionDataView.Close();
			FSessionDataView.Session = null;
		}
		
		private void FDataphoria_Connected(object sender, EventArgs e)
		{
			FSessionDataView.Session = FDataphoria.DataSession;
			try
			{
				FSessionDataView.Open();
			}
			catch (Exception LException)
			{
				FDataphoria.Warnings.AppendError(null, LException, false);
			}
		}

		private void FAttachButton_Click(object sender, EventArgs e)
		{
			if (FDataphoria.Debugger != null)
				FDataphoria.Debugger.AttachSession(FSessionDataView["ID"].AsInt32);
		}

		private void FRefreshButton_Click(object sender, EventArgs e)
		{
			FSessionDataView.Refresh();
		}
	}
}
