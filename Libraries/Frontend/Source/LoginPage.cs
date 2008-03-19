/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Web;
using System.Web.Security;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Server
{
	public class LoginPage : System.Web.UI.Page
	{
		public const string CDefaultDataServerUri = "tcp://localhost:8061/dataphor";
		public const string CDefaultLoginName = "Admin";
		public const string CDefaultPassword = "";
		public const string CLoginSuccessful = "Login Successful";

		private void Page_Load(object sender, System.EventArgs e)
		{
			if (Request.Params["DataServerUri"] == null || Request.Params["LoginName"] == null || Request.Params["Password"] == null)
			{
				Response.StatusCode = (int)HttpStatusCode.ResetContent; // This will tell the client that it needs to log back in.
				PrintLoginForm(Context);
			}
			else
			{
				// connect to DAE server
				IServer LServer;
				try
				{
					LServer = ServerFactory.Connect(Request.Params["DataServerUri"]);
				}
				catch (Exception LException)
				{
					throw new ServerException(ServerException.Codes.DatabaseUnreachable, LException);
				}
				Session.Add("DataServer", LServer);

				SessionInfo LSessionInfo = new SessionInfo(Request.Params["LoginName"], Request.Params["Password"]);
				Session.Add("DataSessionInfo", LSessionInfo);

				IServerSession LSession = LServer.Connect(LSessionInfo);
				Session.Add("DataServerSession", LSession);
				Session.Add("DataServerProcess", LSession.StartProcess());

				Session.Add("UseDerivationCache", Request.Params["UseDerivationCache"] == null ? true : Boolean.Parse(Request.Params["UseDerivationCache"]));
				Session.Add("DerivationCache", new Hashtable());
				Session.Add("DerivationTimeStamp", LServer.DerivationTimeStamp);

				// Monitor the App directory for changes
				FileSystemWatcher LWatcher = new FileSystemWatcher(Context.Request.PhysicalApplicationPath);
				LWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
				LWatcher.IncludeSubdirectories = true;
				LWatcher.Changed += new FileSystemEventHandler(ClearDerivationCache);
				LWatcher.Renamed += new RenamedEventHandler(ClearDerivationCacheDueToRename);
				LWatcher.Deleted += new FileSystemEventHandler(ClearDerivationCache);
				Session.Add("DerivationCacheWatcher", LWatcher);
				LWatcher.EnableRaisingEvents = true;

				Response.SetCookie(new HttpCookie("LastDataServerUri", Request.Params["DataServerUri"]));
				Response.SetCookie(new HttpCookie("LastLoginName", Request.Params["LoginName"]));
				if (Request.Params["ReturnUrl"] != null)
					FormsAuthentication.RedirectFromLoginPage(Request.Params["LoginName"], false);
				else
				{
					FormsAuthentication.SetAuthCookie(Request.Params["LoginName"], false);
					Response.Write(CLoginSuccessful);
				}
			}
		}

		private void ClearDerivationCache(object ASender, FileSystemEventArgs AArgs)
		{
			((Hashtable)Session["DerivationCache"]).Clear();
		}

		private void ClearDerivationCacheDueToRename(object ASender, RenamedEventArgs AArgs)
		{
			ClearDerivationCache(ASender, null);
		}

		public virtual void PrintLoginForm(HttpContext AContext)
		{
			AContext.Response.Write("<html><head><title>Dataphor Application Server Login</title></head>");
			AContext.Response.Write("<body>");
			AContext.Response.Write("<form method=post>");
			AContext.Response.Write("<table>");
			AContext.Response.Write(String.Format("<tr><td align=right>Data Server Uri: </td><td><input name=DataServerUri value='{0}'></td></tr>", AContext.Request.Cookies["LastDataServerUri"] == null ? CDefaultDataServerUri : AContext.Request.Cookies["LastDataServerUri"].Value));
			AContext.Response.Write(String.Format("<tr><td align=right>Login Name: </td><td><input name=LoginName value='{0}'></td></tr>", AContext.Request.Cookies["LastLoginName"] == null ? CDefaultLoginName : AContext.Request.Cookies["LastLoginName"].Value));
			AContext.Response.Write("<tr><td align=right>Password: </td><td><input type=password name=Password value=''></td></tr>");
			AContext.Response.Write("</table>");
			AContext.Response.Write("<p><input type=submit>");
			AContext.Response.Write("</form>");
			AContext.Response.Write("</body></html>");
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
	}
}
