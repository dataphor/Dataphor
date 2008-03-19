/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;

using Alphora.Dataphor.Frontend.Client;

namespace Alphora.Dataphor.Frontend.Client.Web
{

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(Object sender, EventArgs e)
		{
		}

		protected void Session_Start(Object sender, EventArgs e)
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("WebClient/PageNames");
			Session["DefaultPage"] = "Default.aspx";
			Session["ApplicationsPage"] = "Applications.aspx";
			Session["ConnectPage"] = "Connect.aspx";
			if (LSettings != null)
			{
				string LValue = (string)LSettings["Default"];
				if ((LValue != null) && (LValue != String.Empty))
					Session["DefaultPage"] = LValue;
				LValue = (string)LSettings["Applications"];
				if ((LValue != null) && (LValue != String.Empty))
					Session["ApplicationsPage"] = LValue;
				LValue = (string)LSettings["Connect"];
				if ((LValue != null) && (LValue != String.Empty))
					Session["ConnectPage"] = LValue;
			}
		}

		protected void Application_BeginRequest(Object sender, EventArgs e)
		{
		}

		protected void Application_EndRequest(Object sender, EventArgs e)
		{
		}

		protected void Session_End(Object sender, EventArgs e)
		{
		}

		protected void Application_End(Object sender, EventArgs e)
		{
		}
	}
}