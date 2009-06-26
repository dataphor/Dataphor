/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Server
{
	public class Listener : MarshalByRefObject, IListener
	{
		public const string CDefaultListenerChannelName = "DAEListener";
		
		private static TcpChannel FChannel;

		/// <summary>
		/// Establishes a listener in the current process, using the listener configuration settings if available.
		/// </summary>
		/// <returns>True if a listener was established, false otherwise.</returns>
		public static bool EstablishListener()
		{
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("listener");
			
			// Set shouldListen=false to turn off the listener for a process
			if ((LSettings != null) && LSettings.Contains("shouldListen") && !Convert.ToBoolean(LSettings["shouldListen"]))
				return false;

			BinaryServerFormatterSinkProvider LProvider = new BinaryServerFormatterSinkProvider();
			LProvider.TypeFilterLevel = TypeFilterLevel.Full;
			if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)	// Must check, will throw if we attempt to set it again (regardless)
				RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

			IDictionary LProperties = new System.Collections.Specialized.ListDictionary();
			LProperties["port"] = (LSettings != null && LSettings.Contains("port")) ? LSettings["port"] : RemotingUtility.CDefaultListenerPort;
			LProperties["name"] = (LSettings != null && LSettings.Contains("name")) ? LSettings["name"] : CDefaultListenerChannelName;
			
			try
			{
				FChannel = new TcpChannel(LProperties, null, LProvider);
				ChannelServices.RegisterChannel(FChannel, false);
			}
			catch
			{
				// An error attempting to register the channel means there is another process with a listener already established
				FChannel = null;
				return false;
			}
			
			try
			{
				RemotingConfiguration.RegisterWellKnownServiceType(new WellKnownServiceTypeEntry(typeof(Listener), "DataphorListener", WellKnownObjectMode.SingleCall));
				return true;
			}
			catch
			{
				ChannelServices.UnregisterChannel(FChannel);
				FChannel = null;
				throw;
			}
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
		
		public string GetInstanceURI(string AInstanceName, bool AUseNative)
		{
			try
			{
				ServerConfiguration LServer = InstanceManager.LoadConfiguration().Instances[AInstanceName];
				return RemotingUtility.BuildInstanceURI(Environment.MachineName, LServer.PortNumber, LServer.Name, AUseNative);
			}
			catch (Exception LException)
			{
				throw NativeCLIUtility.WrapException(LException);
			}
		}
	}
}
