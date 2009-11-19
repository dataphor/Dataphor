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
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
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
				throw new FaultException<ListenerFault>(ListenerFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		private string GetInstanceURI(string AHostName, string AInstanceName, ConnectionSecurityMode ASecurityMode, bool AUseNative)
		{
			try
			{
				ServerConfiguration LServer = InstanceManager.LoadConfiguration().Instances[AInstanceName];
					
				bool LSecure = false;
				switch (ASecurityMode)
				{
					case ConnectionSecurityMode.Default : 
						LSecure = LServer.RequireSecureConnection;
					break;
					
					case ConnectionSecurityMode.None :
						if (LServer.RequireSecureConnection)
							throw new NotSupportedException("The URI cannot be determined because the instance does not support unsecured connections.");
					break;

					case ConnectionSecurityMode.Transport :
						LSecure = true;
					break;
				}
					
				if (AUseNative)
					return DataphorServiceUtility.BuildNativeInstanceURI(AHostName, LServer.PortNumber, LSecure, LServer.Name);
				
				return DataphorServiceUtility.BuildInstanceURI(AHostName, LServer.PortNumber, LSecure, LServer.Name);
			}
			catch (Exception LException)
			{
				throw new FaultException<ListenerFault>(ListenerFaultUtility.ExceptionToFault(LException), LException.Message);
			}
		}
		
		public string GetInstanceURI(string AHostName, string AInstanceName, ConnectionSecurityMode ASecurityMode)
		{
			return GetInstanceURI(AHostName, AInstanceName, ASecurityMode, false);
		}
		
		public string GetNativeInstanceURI(string AHostName, string AInstanceName, ConnectionSecurityMode ASecurityMode)
		{
			return GetInstanceURI(AHostName, AInstanceName, ASecurityMode, true);
		}
	}
}
