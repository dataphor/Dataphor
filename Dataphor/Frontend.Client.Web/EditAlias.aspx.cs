/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Alphora.Dataphor.DAE.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	/// <summary>
	/// Summary description for EditAlias.
	/// </summary>
	public class EditAlias : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Label Label1;
		protected System.Web.UI.WebControls.Label Label2;
		protected System.Web.UI.WebControls.Label Label3;
		protected System.Web.UI.WebControls.Label Label4;
		protected System.Web.UI.WebControls.Label Label6;
		protected System.Web.UI.WebControls.TextBox FAliasNameTextBox;
		protected System.Web.UI.WebControls.RadioButton FConnectionRadioButton;
		protected System.Web.UI.WebControls.Button FAcceptButton;
		protected System.Web.UI.WebControls.Button FRejectButton;
		protected System.Web.UI.WebControls.TextBox FHostNameTextBox;
		protected System.Web.UI.WebControls.TextBox FPortNumberConnectionTextBox;
		protected System.Web.UI.WebControls.TextBox FPortNumberInProcessTextBox;
		protected System.Web.UI.WebControls.TextBox FCatalogDirectoryTextBox;
		protected System.Web.UI.WebControls.RadioButton FInProcessRadioButton;
		protected System.Web.UI.WebControls.TextBox FLibraryDirectoryTextBox;
		protected System.Web.UI.WebControls.Label Label8;
		protected System.Web.UI.WebControls.TextBox FPassword;
		protected System.Web.UI.WebControls.Label Label5;
		protected System.Web.UI.WebControls.TextBox FUserID;
		protected System.Web.UI.WebControls.Label Label7;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			FConfiguration = (AliasConfiguration)Session["AliasConfiguration"];
			if (FConfiguration == null)
				Response.Redirect("Connect.aspx");

			FMode = Request.QueryString["Mode"].ToLower();
			if (((FMode == null) || (FMode == String.Empty)) && (FMode != "edit"))
				FMode = "add";

			if (!IsPostBack)
			{
				if (FMode == "add")
					SetFromAlias(new ConnectionAlias());
				else
					SetFromAlias(FConfiguration.Aliases[FConfiguration.DefaultAliasName]);
			}
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    
			this.FAcceptButton.Click += new System.EventHandler(this.FAcceptButton_Click);
			this.FRejectButton.Click += new System.EventHandler(this.FRejectButton_Click);
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion

		// Configuration

		private AliasConfiguration FConfiguration;

		public AliasConfiguration Configuration
		{
			get { return FConfiguration; }
		}

		// Mode

		private string FMode;

		public string Mode
		{
			get { return FMode; }
		}

		public void SetFromAlias(ServerAlias AAlias)
		{
			FAliasNameTextBox.Text = AAlias.Name;

			InProcessAlias LInProcess = AAlias as InProcessAlias;
			ConnectionAlias LConnection = AAlias as ConnectionAlias;
			if (LInProcess != null)
			{
				FInProcessRadioButton.Checked = true;
				FPortNumberInProcessTextBox.Text = AAlias.PortNumber.ToString();
				FCatalogDirectoryTextBox.Text = LInProcess.CatalogDirectory;
				FLibraryDirectoryTextBox.Text = LInProcess.LibraryDirectory;
			}
			else
			{
				FConnectionRadioButton.Checked = true;
				FPortNumberConnectionTextBox.Text = AAlias.PortNumber.ToString();
				FHostNameTextBox.Text = LConnection.HostName;
			}

			FPassword.Text = AAlias.SessionInfo.Password;
			FUserID.Text = AAlias.SessionInfo.UserID;
		}

		public ServerAlias CreateAlias()
		{
			ServerAlias LResult;
			if (FInProcessRadioButton.Checked)
			{
				InProcessAlias LInProcess = new InProcessAlias();
				LInProcess.PortNumber = Int32.Parse(FPortNumberInProcessTextBox.Text);
				LInProcess.CatalogDirectory = FCatalogDirectoryTextBox.Text;
				LInProcess.LibraryDirectory = FLibraryDirectoryTextBox.Text;
				LResult = LInProcess;
			}
			else
			{
				ConnectionAlias LConnection = new ConnectionAlias();
				LConnection.PortNumber = Int32.Parse(FPortNumberConnectionTextBox.Text);
				LConnection.HostName = FHostNameTextBox.Text;
				LResult = LConnection;
			}
			
			LResult.SessionInfo.Password = FPassword.Text;
			LResult.SessionInfo.UserID = FUserID.Text;

			LResult.Name = FAliasNameTextBox.Text;

			return LResult;
		}

		private void FAcceptButton_Click(object sender, System.EventArgs e)
		{
			ServerAlias LAlias = CreateAlias();
			if (FMode == "edit")
				Configuration.Aliases.Remove(Configuration.DefaultAliasName);
			Configuration.Aliases.Add(LAlias);
			Configuration.DefaultAliasName = LAlias.Name;
			Configuration.Save(AliasManager.CAliasConfigurationFileName);

			Response.Redirect("Connect.aspx");
		}

		private void FRejectButton_Click(object sender, System.EventArgs e)
		{
			Response.Redirect("Connect.aspx");
		}
	}
}
