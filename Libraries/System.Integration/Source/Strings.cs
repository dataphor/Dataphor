/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Resources;

namespace Alphora.Dataphor.Libraries.System.Integration
{
	public sealed class Strings
	{
		public const string CBaseName = "Alphora.Dataphor.Libraries.System.Integration.Strings";

		public static string Get(string AResourceName, params object[] AArgs)
		{
			return String.Format(Get(AResourceName), AArgs);
		}

		private static ResourceManager FManager = new ResourceManager(CBaseName, typeof(Strings).Assembly);

		public static string Get(string AResourceName)
		{
			return FManager.GetString(AResourceName);
		}
	}
}
