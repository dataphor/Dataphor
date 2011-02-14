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
		private static string GetInstanceName(string[] args)
		{
			for (int i = 0; i < args.Length - 1; i++)
				if ((args[i].ToLower() == "-name") || (args[i].ToLower() == "-n"))
					return args[i + 1];
					
			return null;
		}
		
		static void Main(string[] args)
		{
			string instanceName = GetInstanceName(args);
			if (instanceName == null)
				instanceName = Engine.DefaultServerName;
				
			Console.WriteLine("Server starting...");
			DataphorServiceHost host = new DataphorServiceHost();
			host.InstanceName = instanceName;
			host.Start();
			try
			{
				Console.WriteLine("Server started.");
				Console.ReadLine();
			}
			finally
			{
				Console.WriteLine("Server stopping...");
				host.Stop();
			}
		}
	}
}
