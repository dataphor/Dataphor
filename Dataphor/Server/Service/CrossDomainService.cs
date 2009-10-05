/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE.Contracts;
using System.Collections;
using System.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace Alphora.Dataphor.DAE.Service
{
	public class CrossDomainService : ICrossDomainService
	{
		private static ServiceHost FServiceHost;
		
		/// <summary>
		/// Starts a cross domain service in the current process, using the cross domain configuration settings if available.
		/// </summary>
		/// <returns>True if a listener is established in this app domain.</returns>
		public static bool StartCrossDomainService(string AHostName, int APortNumber)
		{
			if (FServiceHost != null)
				return true;
				
			IDictionary LSettings = (IDictionary)ConfigurationManager.GetSection("crossDomainService");
			
			// Set enableCrossDomainService=false to turn off the cross domain service for a process
			if ((LSettings != null) && LSettings.Contains("enableCrossDomainService") && !Convert.ToBoolean(LSettings["enableCrossDomainService"]))
				return false;
				
			FServiceHost = new WebServiceHost(typeof(CrossDomainService), new Uri(DataphorServiceUtility.BuildCrossDomainServiceURI(AHostName, APortNumber)));
			
			FServiceHost.AddServiceEndpoint
			(
				typeof(ICrossDomainService), 
				new WebHttpBinding(),
				""
			);
			
			try
			{
				FServiceHost.Open();
			}
			catch
			{
				// An error indicates the service could not be started because there is already a listener running in another process.
				return false;
			}
			
			return true;
		}
		
		public static void StopCrossDomainService()
		{
			if (FServiceHost != null)
			{
				FServiceHost.Close();
				FServiceHost = null;
			}
		}
		
		public Message GetPolicyFile()
		{
			using (FileStream LStream = File.Open("clientaccesspolicy.xml", FileMode.Open))
			{
				using (XmlReader LReader = XmlReader.Create(LStream))
				{
					Message LMessage = Message.CreateMessage(MessageVersion.None, "", LReader);
					
					using (MessageBuffer LBuffer = LMessage.CreateBufferedCopy(1000))
					{
						return LBuffer.CreateMessage();
					}
				}
			}
		}
	}
}
