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
using Alphora.Dataphor.Frontend.Client.Web;

namespace Alphora.Dataphor.Frontend.Client.Web.Application
{

	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(Object sender, EventArgs e)
		{
		}

		protected void Session_Start(Object sender, EventArgs e)
		{
			IDictionary settings = (IDictionary)ConfigurationManager.GetSection("WebClient/PageNames");
			Session["DefaultPage"] = "Default.aspx";
			Session["ApplicationsPage"] = "Applications.aspx";
			Session["ConnectPage"] = "Connect.aspx";
			if (settings != null)
			{
				string tempValue = (string)settings["Default"];
				if ((tempValue != null) && (tempValue != String.Empty))
					Session["DefaultPage"] = tempValue;
				tempValue = (string)settings["Applications"];
				if ((tempValue != null) && (tempValue != String.Empty))
					Session["ApplicationsPage"] = tempValue;
				tempValue = (string)settings["Connect"];
				if ((tempValue != null) && (tempValue != String.Empty))
					Session["ConnectPage"] = tempValue;
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