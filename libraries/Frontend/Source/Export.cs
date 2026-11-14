/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Web;
using System.Web.UI;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client.Provider;

/*
	TODO: Put this into a new XML library
*/

namespace Alphora.Dataphor.Frontend.Server
{
	public class DataExport: Page
	{
		protected override bool SupportAutoEvents 
		{
			get { return false; }
		}

		protected override void Render(HtmlTextWriter AOutput) 
		{
			string LExpression;
			if(Request["table"] != null)
				LExpression = String.Format("select {0};", Request["table"]);
			else 
			{
				if(Request["query"] != null)
					LExpression = Request["query"];
				else
					throw new Exception("You must set a \"table\" or \"query\" CGI variable.");
			}

			DAEConnection LConn = new DAEConnection();
			LConn.Open((IServer)Session["DataServer"], (IServerSession)Session["DataServerSession"], (IServerProcess)Session["DataServerProcess"]);
			DAEDataAdapter LAdapter = new DAEDataAdapter(LExpression, LConn);
			DataSet LDataSet = new DataSet();
			LAdapter.Fill(LDataSet);

			Response.ContentType = "text/xml";
			Response.Write("<?xml version=\"1.0\"?>\r\n");
			LDataSet.WriteXml(Response.OutputStream, XmlWriteMode.WriteSchema);
		}
	}
}