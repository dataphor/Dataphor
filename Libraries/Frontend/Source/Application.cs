/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Runtime.Remoting;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Text;

using Alphora.Dataphor;
using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.Frontend.Server
{
	/*

		Application Request BNF:
	
		<frontend server request> ::=
			<derivation request> |
			<derivation image request> |
			<customization request>

		<derivation request> ::=
			<derivation page name>?<derivedpage arguments>[&<frontend server argument>]

		<derivation page name> ::=
			derivation.dil

		<derivedpage arguments> ::=
			query=<D4 expression>[&pagetype=<derived page type>][&elaborate=<boolean literal>][&masterkeynames=<column name commalist>][&detailkeynames=<column name commalist>]
			
		<derived page type> ::=
			add |
			edit |
			delete |
			view |
			browse |
			orderbrowse |
			browsereport

		<derivation image request> ::=
			derivation.<image name>.aspx

		<image name> ::=
			add | delete | detail | edit | offlight | onlight | view

		<customization request> ::=
			<save request> |
			<getraw request> |
			<getlineage request>

		<save request> ::=
			customization.save.aspx?uri={<dil filename>|<dilx filename>}[&addlinkmap=<linkmap source filename>]

		<getraw request> ::=
			customization.getraw.aspx?uri=<dilx filename>

		<getlineage request> ::=
			customization.getlineage.aspx

		<frontend server argument> ::=
			noremap

		--------------------------

		"noremap=True" - Requests that server-side link mappings are not to cause client-side redirection to occur.  
			If this option is not set, the server may automatically respond with a client side redirection caused 
			by a link mapping entry.  Link mapping entries allow pages requests to be automatically 
			redirected to another URI.

		<save request> - Saves the data embedded in the request http content body to a file specified by 
			the uri parameter.

		<retraw request> - Returns the pre-process DILX document.  Ordinary requests for DILX documents 
			result in server-side generated DIL based on the ancestry and content of the DILX document.

		<getlineage request> - Requires that the http content body be a DILX document containing the 
			desired ancestors.  The ancestors are merged and the response contains the resulting DIL content.

	
	*/

	/// <summary> Base class for applications wishing to participate in the features of the Dataphor Frontend Server. </summary>
	/// <remarks>
	///		Descending a web application from this class enables UI derivation and customization.
	///		When starting, this class reads it's setting from the frontend.config file and
	///		attempts to connect to the appropriate DAE Server specified therein.  The
	///		linktable.config file is also read if it exists and is monitored (auto-read when 
	///		changed).
	/// </remarks>
	public class ApplicationServer : HttpApplication
	{
		// Do not localize
		public const string CNoRemap = "noremap";
		public const string CStartupCategory = "Startup";
		public const string CEventLogSource = "Dataphor Frontend Server";
		public const string CLinkTableFileName = @"linktable.config";	//Dataphor Frontend Linktable file name

		protected virtual void Application_Start(Object sender, EventArgs e)
		{
			Application.Add("ApplicationPath", Context.Request.PhysicalApplicationPath); // required prior to reading settings
			
			try
			{
				UpdateLinkTable();

				// Monitor the LinkTable
				FileSystemWatcher LWatcher = new FileSystemWatcher(Context.Request.PhysicalApplicationPath, CLinkTableFileName);
				LWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
				LWatcher.Changed += new FileSystemEventHandler(LinkTableChanged);
				LWatcher.Renamed += new RenamedEventHandler(LinkTableRenamed);
				LWatcher.Deleted += new FileSystemEventHandler(LinkTableChanged);
				LWatcher.Created += new FileSystemEventHandler(LinkTableChanged);
				Application.Add("LinkTableWatcher", LWatcher);
				LWatcher.EnableRaisingEvents = true;
			}
			catch (Exception LException)
			{
				LogException(LException);
			}
		}

		protected virtual void Application_End(Object sender, EventArgs e)
		{
			((FileSystemWatcher)Session["LinkTableWatcher"]).Dispose();
		}

		protected virtual void Session_Start(Object sender, EventArgs e)
		{
		}

		protected virtual void Application_BeginRequest(Object sender, EventArgs e)
		{
			string LNoRemap = Request.QueryString[CNoRemap];
			string LRelativeUri = Request.Url.PathAndQuery.Remove(0, Request.ApplicationPath.Length + 1);
			if (((LNoRemap == null) || (!Boolean.Parse(LNoRemap))) && IsRemapped(LRelativeUri))
				Response.Redirect(RemapLink(LRelativeUri));
		}

		protected virtual void Application_EndRequest(Object sender, EventArgs e)
		{
		}

		protected virtual void Session_End(Object sender, EventArgs e)
		{
			IServer LServer = (IServer)Session["DataServer"];
			if (LServer != null)
			{
				IServerSession LServerSession = (IServerSession)Session["DataServerSession"];
				LServerSession.StopProcess((IServerProcess)Session["DataServerProcess"]);
				LServer.Disconnect(LServerSession);
				ServerFactory.Disconnect(LServer);
			}

			if (Session["DerivationCacheWatcher"] != null)
				((FileSystemWatcher)Session["DerivationCacheWatcher"]).Dispose();

			Session.Clear();
		}

		// Exceptions / Logging

		public static void LogException(Exception AException)
		{
			string LMessage = String.Empty;
			while (AException != null)
			{
				LMessage += AException.Message + "\n";
				AException = AException.InnerException;
			}
			System.Diagnostics.EventLog.WriteEntry(CEventLogSource, LMessage, EventLogEntryType.Error); 
		}

		// Links

		/// <summary> Refreshes the Linkmap Table from the file. </summary>
		public void UpdateLinkTable()
		{
			try
			{
				Customization.LinkTable LLinkTable = (Customization.LinkTable)Application["LinkTable"];
				if (LLinkTable == null)
				{
					LLinkTable = new Customization.LinkTable();
					Application.Add("LinkTable", LLinkTable);
				}
				else
					LLinkTable.Clear();

				string LLinkTableFileName = (string)Application["ApplicationPath"] + '\\' + CLinkTableFileName;
				if (File.Exists(LLinkTableFileName))
				{
					using (FileStream LStream = new FileStream(LLinkTableFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
						LLinkTable.Read(LStream);
				}
			}
			catch (Exception LException)
			{
				LogException(LException);
			}
		}

		private void LinkTableChanged(object ASender, FileSystemEventArgs AArgs)
		{
			// TODO: Filter out one of the "changed" events for the linktable.config file when modified.
			UpdateLinkTable();
		}

		private void LinkTableRenamed(object ASender, RenamedEventArgs AArgs)
		{
			UpdateLinkTable();
		}

		/// <summary> Allows default derivation links to be altered or remapped. </summary>
		/// <param name="AUri"> The URI of the original (unmapped) resource. </param>
		/// <returns> The original URI or an altered/remapped URI. </returns>
		public virtual string RemapLink(string AUri)
		{
			// Make the request absolute and sort it for comparison
			string LRemap = ((Customization.LinkTable)Application["LinkTable"])[WebUtility.SortUri(AUri)];

			if (LRemap != null)
			{
				// Check the LinkTable for a remappings
				do	// do used to avoid extra comparison
				{
					AUri = LRemap;
					LRemap = ((Customization.LinkTable)Application["LinkTable"])[LRemap];
				} while (LRemap != null);
			}

			return AUri;
		}

		/// <summary> Determines if a particular URI has been remapped. </summary>
		/// <param name="AUri"> The relative or full URI to test. </param>
		/// <returns> True if the URI has been remapped. </returns>
		public virtual bool IsRemapped(string AUri)
		{
			// Make the request absolute and sort it for comparison
			AUri = WebUtility.SortUri(AUri);

			return ((Customization.LinkTable)Application["LinkTable"])[AUri] != null;
		}

		public virtual void AddLinkMap(string ASource, string ATarget)
		{
			FileSystemWatcher LWatcher = (FileSystemWatcher)Application["LinkTableWatcher"];
			lock(LWatcher)
			{
				LWatcher.EnableRaisingEvents = false;
				try
				{
					Customization.LinkTable LTable = (Customization.LinkTable)Application["LinkTable"];

					// Add the link
					LTable.Add(ASource, ATarget);

					// Save the link map table
					string LLinkTableFileName = (string)Application["ApplicationPath"] + '\\' + CLinkTableFileName;
					using (FileStream LStream = new FileStream(LLinkTableFileName, FileMode.Create, FileAccess.Write, FileShare.None))
						LTable.Write(LStream);
				}
				finally
				{
					LWatcher.EnableRaisingEvents = true;
				}
			}
		}
	}
}