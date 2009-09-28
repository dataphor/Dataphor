using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Alphora.Dataphor.DAE.Contracts;
using System.ServiceModel.Channels;

namespace Alphora.Dataphor.DAE.Listener
{
	public class ListenerClient : IDisposable
	{
		public ListenerClient(string AHostName)
		{
			FChannelFactory =
				new ChannelFactory<IClientListenerService>
				(
					new CustomBinding(new BinaryMessageEncodingBindingElement(), new HttpTransportBindingElement()), 
					new EndpointAddress(DataphorServiceUtility.BuildListenerURI(AHostName))
				);
		}
		
		public void Dispose()
		{
			if (FChannel != null)
			{
				CloseChannel();
				SetChannel(null);
			}
			
			if (FChannelFactory != null)
			{
				CloseChannelFactory();
				FChannelFactory = null;
			}
		}
		
		private ChannelFactory<IClientListenerService> FChannelFactory;
		private IClientListenerService FChannel;
		
		private bool IsChannelFaulted(IClientListenerService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Faulted;
		}
		
		private bool IsChannelValid(IClientListenerService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Opened;
		}
		
		private void SetChannel(IClientListenerService AChannel)
		{
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted -= new EventHandler(ChannelFaulted);
			FChannel = AChannel;
			if (FChannel != null)
				((ICommunicationObject)FChannel).Faulted += new EventHandler(ChannelFaulted);
		}
		
		private void ChannelFaulted(object ASender, EventArgs AArgs)
		{
			((ICommunicationObject)ASender).Faulted -= new EventHandler(ChannelFaulted);
			if (FChannel == ASender)
				FChannel = null;
		}
		
		private void CloseChannel()
		{
			ICommunicationObject LChannel = FChannel as ICommunicationObject;
			if (LChannel != null)
				if (LChannel.State == CommunicationState.Opened)
					LChannel.Close();
				else
					LChannel.Abort();
		}
		
		private void CloseChannelFactory()
		{
			if (FChannelFactory.State == CommunicationState.Opened)
				FChannelFactory.Close();
			else
				FChannelFactory.Abort();
		}
		
		private IClientListenerService GetInterface()
		{
			if ((FChannel == null) || !IsChannelValid(FChannel))
				SetChannel(FChannelFactory.CreateChannel());
			return FChannel;
		}

		public string[] EnumerateInstances(string AHostName)
		{
			IAsyncResult LResult = GetInterface().BeginEnumerateInstances(null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndEnumerateInstances(LResult);
		}
		
		public string GetInstanceURI(string AInstanceName)
		{
			IAsyncResult LResult = GetInterface().BeginGetInstanceURI(AInstanceName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndGetInstanceURI(LResult);
		}
		
		public string GetNativeInstanceURI(string AInstanceName)
		{
			IAsyncResult LResult = GetInterface().BeginGetNativeInstanceURI(AInstanceName, null, null);
			LResult.AsyncWaitHandle.WaitOne();
			return GetInterface().EndGetNativeInstanceURI(LResult);
		}
	}
}
