/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

namespace Alphora.Dataphor.Frontend.Server
{
	public sealed class Strings
	{
		public const string BaseName = "Alphora.Dataphor.Frontend.Server.Strings";

		public static string Get(string resourceName, params object[] args)
		{
			return String.Format(Get(resourceName), args);
		}

		private static ResourceManager _manager = new ResourceManager(BaseName, typeof(Strings).Assembly);

		public static string Get(string resourceName)
		{
			return _manager.GetString(resourceName);
		}
	}
}
