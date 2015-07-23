/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
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
	public class Connect : System.Web.UI.Page
	{
		protected Button _addButton;
		protected Button _editButton;
		protected Label Label1;
		protected System.Web.UI.WebControls.TextBox UserIDTextBox;
		protected Label Label2;
		protected System.Web.UI.WebControls.TextBox PasswordTextBox;
		protected Button _deleteButton;
		protected Button _login;

		private string AliasConfigurationFileName
		{
			get { return Path.Combine(Request.PhysicalApplicationPath, AliasManager.AliasConfigurationFileName); }
		}

		private void Page_Load(object sender, System.EventArgs e)
		{
			_configuration = (AliasConfiguration)Session["AliasConfiguration"];
			if (_configuration == null)
			{
				_configuration = AliasConfiguration.Load(AliasConfigurationFileName);
				Session["AliasConfiguration"] = _configuration;
			}

			// End any previous session
			Web.Session session = (Web.Session)Session["WebSession"];
			if (session != null)
			{
				try
				{
					session.Dispose();
				}
				finally
				{
					Session["WebSession"] = null;
				}
			}

			string aliasName = Request.QueryString["Alias"];
			if (aliasName == null)
				aliasName = String.Empty;

			if (IsPostBack)
			{
				if (aliasName != String.Empty)
				{
					UserIDTextBox.Text = _configuration.Aliases[aliasName].SessionInfo.UserID;
					_configuration.DefaultAliasName = aliasName;
				}

				string deleteAlias = Request.QueryString["Delete"];
				if ((deleteAlias != null) && (deleteAlias != String.Empty))
				{
					_configuration.Aliases.Remove(deleteAlias);
					if (String.Compare(deleteAlias, _configuration.DefaultAliasName, true) == 0)
						_configuration.DefaultAliasName = String.Empty;
					_configuration.Save(AliasConfigurationFileName);
				}
			}
			else
			{
				if (_configuration.DefaultAliasName != String.Empty)
				{
					ServerAlias connection = _configuration.Aliases[_configuration.DefaultAliasName];
					if (connection != null)
						UserIDTextBox.Text = connection.SessionInfo.UserID;
				}

				string applicationID = Request.QueryString["ApplicationID"];
				if ((applicationID != null) && (applicationID != String.Empty))
					Session["ApplicationID"] = applicationID;

				if (aliasName != String.Empty)
				{
					_configuration.DefaultAliasName = aliasName;
					AdvanceToApplication();
				}
			}

			_editButton.Enabled = _configuration.Aliases.Count > 0;
			_deleteButton.Enabled = _editButton.Enabled;
			_deleteButton.Attributes.Add
			(
				"onclick", 
				HttpUtility.HtmlAttributeEncode
				(
					String.Format
					(
						@"if (confirm('{0}')) Submit('Connect.aspx?Delete={1}',event)",
						Strings.Get("ConfirmAliasDeleteMessage"),
						HttpUtility.UrlEncode(Configuration.DefaultAliasName).Replace("'", "\\'")
					)
				)
			);
			UserIDTextBox.Attributes.Add("onkeydown", "TrapKeyDown(FLogin, event)");
			PasswordTextBox.Attributes.Add("onkeydown", "TrapKeyDown(FLogin, event)");
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
			this._addButton.Click += new System.EventHandler(this.FAddButton_Click);
			this._editButton.Click += new System.EventHandler(this.FEditButton_Click);
			this._login.Click += new System.EventHandler(this.FLogin_Click);
			this.Load += new System.EventHandler(this.Page_Load);
		}
		#endregion

		// Configuration

		private AliasConfiguration _configuration;

		public AliasConfiguration Configuration
		{
			get { return _configuration; }
		}

		protected void WriteAliases()
		{
			HtmlTextWriter writer = new HtmlTextWriter(Response.Output);
			try
			{
				bool current;
				// TODO: Paging
				foreach (ServerAlias alias in Configuration.Aliases.Values)
				{
					current = String.Compare(alias.Name, Configuration.DefaultAliasName, true) == 0;
					if (current)
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowcurrent");
					else
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
					writer.RenderBeginTag(HtmlTextWriterTag.Tr);
					if (current)
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridcellcurrent");
					else
					{
						writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridcell");
						writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('Connect.aspx?Alias={0}',event)", HttpUtility.UrlEncode(alias.Name).Replace("'", "\\'")), true);
					}
					writer.RenderBeginTag(HtmlTextWriterTag.Td);
					writer.Write(HttpUtility.HtmlEncode(alias.ToString()));
					writer.RenderEndTag();
					writer.RenderEndTag();
				}
			}
			finally
			{
				writer.Close();
			}
		}

		private void AdvanceToApplication()
		{
			DataSession session = new DataSession();
			session.Alias = _configuration.Aliases[_configuration.DefaultAliasName];
			session.Active = true;
			session.SessionInfo.Environment = "WindowsClient";
			
			Web.Session webSession = new Web.Session(session, true);
			Session["WebSession"] = webSession;

			string applicationID = (string)Session["ApplicationID"];
			if ((applicationID != null) && (applicationID != String.Empty))
			{
				webSession.SetApplication(applicationID);
				Response.Redirect((string)Session["DefaultPage"]);
			}
			else
				Response.Redirect((string)Session["ApplicationsPage"]);
		}

		private void FLogin_Click(object sender, System.EventArgs e)
		{
			ServerAlias alias = _configuration.Aliases[_configuration.DefaultAliasName];
			alias.SessionInfo.UserID = UserIDTextBox.Text;

			_configuration.Save(AliasConfigurationFileName);
			
			alias.SessionInfo.Password = PasswordTextBox.Text;

			AdvanceToApplication();
		}

		private void FAddButton_Click(object sender, System.EventArgs e)
		{
			Response.Redirect("EditAlias.aspx?Mode=Add");
		}

		private void FEditButton_Click(object sender, System.EventArgs e)
		{
			Response.Redirect("EditAlias.aspx?Mode=Edit");
		}

	}
}
