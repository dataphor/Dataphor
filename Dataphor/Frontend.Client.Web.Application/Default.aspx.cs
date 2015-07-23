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
using System.Text;
using System.Net;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Web;

namespace Alphora.Dataphor.Frontend.Client.Web.Application
{
	// Do not put classes above WebClient

	/// <summary> WebClient is what the web client page inherits from. </summary>
	public class Default : System.Web.UI.Page
	{
		private void Page_Load(object sender, System.EventArgs e)
		{
			_webSession = (Web.Session)Session["WebSession"];
			if (_webSession == null)
				Response.Redirect((string)Session["ConnectPage"]);

			Response.Buffer = true;
			Response.CacheControl = "no-cache";

			_webSession.ProcessRequest(Context);

			if (_webSession.Forms.IsEmpty())
			{
				_webSession.Dispose();
				_webSession = null;
				Session.Remove("WebSession");
				string destination = (string)Session["CompletedPage"];
				if ((destination == null) || (destination == String.Empty))
					destination = (string)Session["ConnectPage"];
				Response.Redirect(destination);
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
			this.Load += new System.EventHandler(this.Page_Load);
		}
		#endregion

		// WebSession

		private Web.Session _webSession;
		public Web.Session WebSession { get { return _webSession; } }

		public void WriteBodyAttributes()
		{
			if (!WebSession.Forms.IsEmpty())
			{
				string imageID = ((IWebFormInterface)WebSession.Forms.GetTopmostForm()).BackgroundImageID;
				if (imageID != String.Empty)
					Response.Write(@" background='ViewImage.aspx?ImageID=" + imageID + "'");
			}

			// handle repositioning of page to same scroll position after submit
			Response.Write(@" onscroll=""document.getElementById('ScrollPosition').value = MainBody.scrollTop;""");

			string position = Request.Form["ScrollPosition"];
			if ((position == null) || (position == String.Empty))
				position = "0";
			
			Response.Write(String.Format(@" onload=""OnLoad(document.getElementById('Default'), MainBody, {0});""", Convert.ToInt32(position)));
		}
	}
}