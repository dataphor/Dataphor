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
using Alphora.Dataphor.Frontend.Client.Web;

namespace Alphora.Dataphor.Frontend.Client.Web.Application
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
		protected System.Web.UI.WebControls.TextBox _aliasNameTextBox;
		protected System.Web.UI.WebControls.TextBox _instanceNameTextBox;
		protected System.Web.UI.WebControls.TextBox _inProcessInstanceNameTextBox;
		protected System.Web.UI.WebControls.RadioButton _connectionRadioButton;
		protected System.Web.UI.WebControls.Button _acceptButton;
		protected System.Web.UI.WebControls.Button _rejectButton;
		protected System.Web.UI.WebControls.TextBox _hostNameTextBox;
		protected System.Web.UI.WebControls.TextBox _portNumberConnectionTextBox;
		protected System.Web.UI.WebControls.TextBox _portNumberInProcessTextBox;
		protected System.Web.UI.WebControls.TextBox _catalogDirectoryTextBox;
		protected System.Web.UI.WebControls.RadioButton _inProcessRadioButton;
		protected System.Web.UI.WebControls.TextBox _libraryDirectoryTextBox;
		protected System.Web.UI.WebControls.Label Label8;
		protected System.Web.UI.WebControls.TextBox _password;
		protected System.Web.UI.WebControls.Label Label5;
		protected System.Web.UI.WebControls.TextBox _userID;
		protected System.Web.UI.WebControls.Label Label7;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			_configuration = (AliasConfiguration)Session["AliasConfiguration"];
			if (_configuration == null)
				Response.Redirect("Connect.aspx");

			_mode = Request.QueryString["Mode"].ToLower();
			if (((_mode == null) || (_mode == String.Empty)) && (_mode != "edit"))
				_mode = "add";

			this.Form.Action = "EditAlias.aspx?Mode=" + _mode;
			if (!IsPostBack)
			{
				if (_mode == "add")
					SetFromAlias(new ConnectionAlias());
				else
					SetFromAlias(_configuration.Aliases[_configuration.DefaultAliasName]);
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
			this._acceptButton.Click += new System.EventHandler(this.FAcceptButton_Click);
			this._rejectButton.Click += new System.EventHandler(this.FRejectButton_Click);
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion

		// Configuration

		private AliasConfiguration _configuration;

		public AliasConfiguration Configuration
		{
			get { return _configuration; }
		}

		// Mode

		private string _mode;

		public string Mode
		{
			get { return _mode; }
		}

		public void SetFromAlias(ServerAlias alias)
		{
			_aliasNameTextBox.Text = alias.Name;

			InProcessAlias inProcess = alias as InProcessAlias;
			ConnectionAlias connection = alias as ConnectionAlias;
			if (inProcess != null)
			{
				_inProcessRadioButton.Checked = true;
				_inProcessInstanceNameTextBox.Text = inProcess.InstanceName;
			}
			else
			{
				_connectionRadioButton.Checked = true;
				_hostNameTextBox.Text = connection.HostName;
				_instanceNameTextBox.Text = connection.InstanceName;
				_portNumberConnectionTextBox.Text = connection.OverridePortNumber.ToString();
			}

			_password.Text = alias.SessionInfo.Password;
			_userID.Text = alias.SessionInfo.UserID;
		}

		public ServerAlias CreateAlias()
		{
			ServerAlias result;
			if (_inProcessRadioButton.Checked)
			{
				InProcessAlias inProcess = new InProcessAlias();
				inProcess.InstanceName = _inProcessInstanceNameTextBox.Text;
				result = inProcess;
			}
			else
			{
				ConnectionAlias connection = new ConnectionAlias();
				connection.HostName = _hostNameTextBox.Text;
				connection.InstanceName = _instanceNameTextBox.Text;
				connection.OverridePortNumber = Int32.Parse(_portNumberConnectionTextBox.Text);
				result = connection;
			}
			
			result.SessionInfo.Password = _password.Text;
			result.SessionInfo.UserID = _userID.Text;

			result.Name = _aliasNameTextBox.Text;

			return result;
		}

		private void FAcceptButton_Click(object sender, System.EventArgs e)
		{
			ServerAlias alias = CreateAlias();
			if (_mode == "edit")
				Configuration.Aliases.Remove(Configuration.DefaultAliasName);
			Configuration.Aliases.Add(alias);
			Configuration.DefaultAliasName = alias.Name;
			Configuration.Save(AliasManager.AliasConfigurationFileName);

			Response.Redirect("Connect.aspx");
		}

		private void FRejectButton_Click(object sender, System.EventArgs e)
		{
			Response.Redirect("Connect.aspx");
		}
	}
}
