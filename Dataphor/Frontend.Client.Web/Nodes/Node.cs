/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Web;

namespace Alphora.Dataphor.Frontend.Client.Web
{
	public class Node : Client.Node
	{
		public Node()
		{
			FID = Session.GenerateID();
		}

		private string FID;
		public string ID
		{
			get { return FID; }
		}

		protected Web.Session WebSession
		{
			get { return ((Web.Session)HostNode.Session); }
		}
	}
}