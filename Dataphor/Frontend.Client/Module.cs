/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Web;
using System.Net;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client
{
	[DesignRoot()]
	[ListInDesigner(false)]
	public class Module : Node, IModule
	{
		public override bool IsValidChild(Type AChildType)
		{
			return true;
		}
	}
}