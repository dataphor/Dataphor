/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.NativeCLI;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
	public class ListenerService : IListenerService
	{
		public string[] EnumerateInstances()
		{
			try
			{
				InstanceConfiguration LConfiguration = InstanceManager.LoadConfiguration();
				string[] LResult = new string[LConfiguration.Instances.Count];
				for (int LIndex = 0; LIndex < LConfiguration.Instances.Count; LIndex++)
					LResult[LIndex] = LConfiguration.Instances[LIndex].Name;
				return LResult;
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}
		
		public string GetInstanceURI(string AInstanceName)
		{
			return GetInstanceURI(AInstanceName, false);
		}
		
		private string GetInstanceURI(string AInstanceName, bool AUseNative)
		{
			try
			{
				if (AUseNative)
					throw new NotSupportedException();
					
				ServerConfiguration LServer = InstanceManager.LoadConfiguration().Instances[AInstanceName];
				return DataphorServiceUtility.BuildInstanceURI(Environment.MachineName, LServer.PortNumber, LServer.Name);
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}
		
		public string GetNativeInstanceURI(string AInstanceName)
		{
			return GetInstanceURI(AInstanceName, true);
		}
	}
}
