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

namespace Alphora.Dataphor.Frontend.Client.Web
{
	/// <summary>
	/// Summary description for Applications.
	/// </summary>
	public class Applications : System.Web.UI.Page
	{
		protected System.Web.UI.WebControls.Button Button1;
	
		private void Page_Load(object sender, System.EventArgs e)
		{
			FWebSession = (Web.Session)Session["WebSession"];
			if (FWebSession == null)
				Response.Redirect((string)Session["ConnectPage"]);

			// Look for an explicitly specified application ID
			string LApplicationID = Request.QueryString["ApplicationID"];

			// Look to see if there is only one application to choose from
			if ((LApplicationID == null) || (LApplicationID == String.Empty))
			{
				DAE.Runtime.Data.DataValue LID = FWebSession.Evaluate("if Count(Applications) = 1 then Applications[].ID else String(nil)");
				if ((LID != null) && !LID.IsNil)
					LApplicationID = LID.AsString;
			}

			// If found, skip this form and redirect to the main form
			if ((LApplicationID != null) && (LApplicationID != String.Empty))
			{
				FWebSession.SetApplication(LApplicationID);
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

		private Web.Session FWebSession;
		
		public Web.Session WebSession
		{
			get { return FWebSession; }
		}

		public void WriteApplications()
		{
			HtmlTextWriter LWriter = new HtmlTextWriter(Response.Output);
			try
			{
				// TODO: Paging
				Alphora.Dataphor.DAE.IServerExpressionPlan LPlan = WebSession.Pipe.Process.PrepareExpression("Frontend.Applications", null);
				try
				{
					Alphora.Dataphor.DAE.IServerCursor LCursor = LPlan.Open(null);
					try
					{
						using (Alphora.Dataphor.DAE.Runtime.Data.Row LRow = new Alphora.Dataphor.DAE.Runtime.Data.Row(WebSession.Pipe.Process, ((Alphora.Dataphor.DAE.Schema.TableType)LPlan.DataType).CreateRowType()))
						{
							while (LCursor.Next())
							{
								LCursor.Select(LRow);

								LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridrow");
								LWriter.AddAttribute(HtmlTextWriterAttribute.Onclick, String.Format("Submit('Applications.aspx?ApplicationID={0}',event)", HttpUtility.UrlEncode(LRow["ID"].AsString).Replace("'", @"\'")), true);
								LWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

								LWriter.AddAttribute(HtmlTextWriterAttribute.Class, "gridcell");
								LWriter.AddAttribute("onmouseover", "this.className = 'gridcellover'");
								LWriter.AddAttribute("onmouseleave", "this.className = 'gridcell'");
								LWriter.RenderBeginTag(HtmlTextWriterTag.Td);

								LWriter.Write(HttpUtility.HtmlEncode(LRow["Description"].AsDisplayString));

								LWriter.RenderEndTag();
								LWriter.RenderEndTag();
							}
						}
					}
					finally
					{
						LPlan.Close(LCursor);
					}
				}
				finally
				{
					WebSession.Pipe.Process.UnprepareExpression(LPlan);
				}
			}
			finally
			{
				LWriter.Close();
			}
		}
	}
}
