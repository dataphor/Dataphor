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

namespace Alphora.Dataphor.DAE.Server
{
	// TODO: Exception management
	//[ExceptionShielding("WCF Exception Shielding")]
	public class ListenerService : IListenerService
	{
		private static ServiceHost FListenerHost;
		
		/// <summary>
		/// Establishes a listener in the current process, using the listener configuration settings if available.
		/// </summary>
		/// <returns>True if a listener is established in this app domain.</returns>
		public static bool EstablishListener()
		{
			if (FListenerHost != null)
				return true;
				
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("listener");
			
			// Set shouldListen=false to turn off the listener for a process
			if ((LSettings != null) && LSettings.Contains("shouldListen") && !Convert.ToBoolean(LSettings["shouldListen"]))
				return false;
				
			FListenerHost = new ServiceHost(typeof(ListenerService));
			
			FListenerHost.AddServiceEndpoint
			(
				typeof(IListenerService), 
				new CustomBinding(new BinaryMessageEncodingBindingElement(), new HttpTransportBindingElement()), 
				DataphorServiceUtility.BuildListenerURI(Environment.MachineName)
			);
			
			try
			{
				FListenerHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
				return false;
			}
			
			return true;
		}
		
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
