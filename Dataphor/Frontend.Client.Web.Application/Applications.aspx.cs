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

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.Frontend.Client.Web;

namespace Alphora.Dataphor.Frontend.Client.Web.Application
{
	/// <summary>
	/// Summary description for Applications.
	/// </summary>
	public class Applications : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Button Button1;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			_webSession = (Web.Session)Session["WebSession"];
			if (_webSession == null)
				Response.Redirect((string)Session["ConnectPage"]);

			// Look for an explicitly specified application ID
			string applicationID = Request.QueryString["ApplicationID"];

			// Look to see if there is only one application to choose from
			if ((applicationID == null) || (applicationID == String.Empty))
			{
				DAE.Runtime.Data.IDataValue iD = _webSession.Evaluate("if Count(Applications) = 1 then Applications[].ID else String(nil)");
				if ((iD != null) && !iD.IsNil)
					applicationID = ((DAE.Runtime.Data.Scalar)iD).AsString;
			}

			// If found, skip this form and redirect to the main form
			if ((applicationID != null) && (applicationID != String.Empty))
			{
				_webSession.SetApplication(applicationID);
				Response.Redirect((string)Session["DefaultPage"]);
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
		
		public Web.Session WebSession
		{
			get { return _webSession; }
		}

		public void WriteApplications()
		{
			HtmlTextWriter writer = new HtmlTextWriter(Response.Output);
			try
			{
				// TODO: Paging
				Alphora.Dataphor.DAE.IServerExpressionPlan plan = WebSession.Pipe.Process.PrepareExpression("Frontend.Applications", null);
				try
				{
					Alphora.Dataphor.DAE.IServerCursor cursor = plan.Open(null);
					try
					{
						using (Alphora.Dataphor.DAE.Runtime.Data.Row row = new Alphora.Dataphor.DAE.Runtime.Data.Row(WebSession.Pipe.Process.ValueManager, ((Alphora.Dataphor.DAE.Schema.TableType)plan.DataType).CreateRowType()))
						{
							while (cursor.Next())
							{
								cursor.Select(row);

								writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
								writer.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('Applications.aspx?ApplicationID={0}',event)", HttpUtility.UrlEncode((string)row["ID"]).Replace("'", @"\'")), true);
								writer.RenderBeginTag(HtmlTextWriterTag.Tr);

								writer.AddAttribute(HtmlTextWriterAttribute.Class, "gridcell");
								writer.AddAttribute("onmouseover", "this.className = 'gridcellover'");
								writer.AddAttribute("onmouseleave", "this.className = 'gridcell'");
								writer.RenderBeginTag(HtmlTextWriterTag.Td);

								writer.Write(HttpUtility.HtmlEncode((string)row["Description"]));

								writer.RenderEndTag();
								writer.RenderEndTag();
							}
						}
					}
					finally
					{
						plan.Close(cursor);
					}
				}
				finally
				{
					WebSession.Pipe.Process.UnprepareExpression(plan);
				}
			}
			finally
			{
				writer.Close();
			}
		}
	}
}
