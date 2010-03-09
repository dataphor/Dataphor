/*
	Dataphor
	© Copyright 2000-2010 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Text;

namespace DAEServer
{
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Service;

	public class Program
	{
		private static DataphorServiceHost LServiceHost;
		
		private static string GetInstanceName(string[] AArgs)
		{
			for (int i = 0; i < AArgs.Length - 1; i++)
				if ((AArgs[i].ToLower() == "-name") || (AArgs[i].ToLower() == "-n"))
					return AArgs[i + 1];
					
			return null;
		}
		
		static void Main(string[] AArgs)
		{
			string LInstanceName = GetInstanceName(AArgs);
			if (LInstanceName == null)
				LInstanceName = Engine.CDefaultServerName;
				
			Console.WriteLine("Server starting...");
			DataphorServiceHost LHost = new DataphorServiceHost();
			LHost.InstanceName = LInstanceName;
			LHost.Start();
			try
			{
				Console.WriteLine("Server started.");
				Console.ReadLine();
			}
			finally
			{
				Console.WriteLine("Server stopping...");
				LHost.Stop();
			}
		}
	}
}
