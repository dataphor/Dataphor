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
			_iD = Session.GenerateID();
		}

		private string _iD;
		public string ID
		{
			get { return _iD; }
		}

		protected Web.Session WebSession
		{
			get { return ((Web.Session)HostNode.Session); }
		}
	}
}