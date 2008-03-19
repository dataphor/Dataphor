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

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class Connect : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Button FAddButton;
		protected System.Web.UI.WebControls.Button FEditButton;
		protected System.Web.UI.WebControls.Label Label1;
		protected System.Web.UI.WebControls.TextBox UserIDTextBox;
		protected System.Web.UI.WebControls.Label Label2;
		protected System.Web.UI.WebControls.TextBox PasswordTextBox;
		protected System.Web.UI.WebControls.Button FDeleteButton;
		protected System.Web.UI.WebControls.Button FLogin;

		private string AliasConfigurationFileName
		{
			get { return Path.Combine(Request.PhysicalApplicationPath, AliasManager.CAliasConfigurationFileName); }
		}

		private void Page_Load(object sender, System.EventArgs e)
		{
			FConfiguration = (AliasConfiguration)Session["AliasConfiguration"];
			if (FConfiguration == null)
			{
				FConfiguration = AliasConfiguration.Load(AliasConfigurationFileName);
				Session["AliasConfiguration"] = FConfiguration;
			}

			// End any previous session
			Web.Session LSession = (Web.Session)Session["WebSession"];
			if (LSession != null)
			{
				try
				{
					LSession.Dispose();
				}
				finally
				{
					Session["WebSession"] = null;
				}
			}

			string LAliasName = Request.QueryString["Alias"];
			if (LAliasName == null)
				LAliasName = String.Empty;

			if (IsPostBack)
			{
				if (LAliasName != String.Empty)
				{
					UserIDTextBox.Text = FConfiguration.Aliases[LAliasName].SessionInfo.UserID;
					FConfiguration.DefaultAliasName = LAliasName;
				}

				string LDeleteAlias = Request.QueryString["Delete"];
				if ((LDeleteAlias != null) && (LDeleteAlias != String.Empty))
				{
					FConfiguration.Aliases.Remove(LDeleteAlias);
					if (String.Compare(LDeleteAlias, FConfiguration.DefaultAliasName, true) == 0)
						FConfiguration.DefaultAliasName = String.Empty;
					FConfiguration.Save(AliasConfigurationFileName);
				}
			}
			else
			{
				if (FConfiguration.DefaultAliasName != String.Empty)
				{
					ServerAlias LConnection = FConfiguration.Aliases[FConfiguration.DefaultAliasName];
					if (LConnection != null)
						UserIDTextBox.Text = LConnection.SessionInfo.UserID;
				}

				string LApplicationID = Request.QueryString["ApplicationID"];
				if ((LApplicationID != null) && (LApplicationID != String.Empty))
					Session["ApplicationID"] = LApplicationID;

				if (LAliasName != String.Empty)
				{
					FConfiguration.DefaultAliasName = LAliasName;
					AdvanceToApplication();
				}
			}

			FEditButton.Enabled = FConfiguration.Aliases.Count > 0;
			FDeleteButton.Enabled = FEditButton.Enabled;
			FDeleteButton.Attributes.Add
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
			this.FAddButton.Click += new System.EventHandler(this.FAddButton_Click);
			this.FEditButton.Click += new System.EventHandler(this.FEditButton_Click);
			this.FLogin.Click += new System.EventHandler(this.FLogin_Click);
			this.Load += new System.EventHandler(this.Page_Load);

		}
		#endregion

		// Configuration

		private AliasConfiguration FConfiguration;

		public AliasConfiguration Configuration
		{
			get { return FConfiguration; }
		}

		protected void WriteAliases()
		{
			HtmlTextWriter LWriter = new HtmlTextWriter(Response.Output);
			try
			{
				bool LCurrent;
				// TODO: Paging
				foreach (ServerAlias LAlias in Configuration.Aliases.Values)
				{
					LCurrent = String.Compare(LAlias.Name, Configuration.DefaultAliasName, true) == 0;
					if (LCurrent)
						LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrowcurrent");
					else
						LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
					LWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
					if (LCurrent)
						LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridcellcurrent");
					else
					{
						LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridcell");
						LWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('Connect.aspx?Alias={0}',event)", HttpUtility.UrlEncode(LAlias.Name).Replace("'", "\\'")), true);
					}
					LWriter.RenderBeginTag(HtmlTextWriterTag.Td);
					LWriter.Write(HttpUtility.HtmlEncode(LAlias.ToString()));
					LWriter.RenderEndTag();
					LWriter.RenderEndTag();
				}
			}
			finally
			{
				LWriter.Close();
			}
		}

		private void AdvanceToApplication()
		{
			DataSession LSession = new DataSession();
			LSession.Alias = FConfiguration.Aliases[FConfiguration.DefaultAliasName];
			LSession.Active = true;
			
			Web.Session LWebSession = new Web.Session(LSession, true);
			Session["WebSession"] = LWebSession;

			string LApplicationID = (string)Session["ApplicationID"];
			if ((LApplicationID != null) && (LApplicationID != String.Empty))
			{
				LWebSession.SetApplication(LApplicationID);
				Response.Redirect((string)Session["DefaultPage"]);
			}
			else
				Response.Redirect((string)Session["ApplicationsPage"]);
		}

		private void FLogin_Click(object sender, System.EventArgs e)
		{
			ServerAlias LAlias = FConfiguration.Aliases[FConfiguration.DefaultAliasName];
			LAlias.SessionInfo.UserID = UserIDTextBox.Text;

			FConfiguration.Save(AliasConfigurationFileName);
			
			LAlias.SessionInfo.Password = PasswordTextBox.Text;

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
