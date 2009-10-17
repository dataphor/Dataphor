/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Listener;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientServer : ClientObject, IRemoteServer
	{
		public ClientServer() { }
		public ClientServer(string AHostName, string AInstanceName, int AOverridePortNumber, ConnectionSecurityMode ASecurityMode, int AOverrideListenerPortNumber, ConnectionSecurityMode AListenerSecurityMode)
		{
			FHostName = AHostName;
			FInstanceName = AInstanceName;
			FOverridePortNumber = AOverridePortNumber;
			FSecurityMode = ASecurityMode;
			FOverrideListenerPortNumber = AOverrideListenerPortNumber;
			FListenerSecurityMode = AListenerSecurityMode;
			
			#if SILVERLIGHT
			System.Net.WebRequest.RegisterPrefix("http://", System.Net.Browser.WebRequestCreator.ClientHttp);
			System.Net.WebRequest.RegisterPrefix("https://", System.Net.Browser.WebRequestCreator.ClientHttp);
			#endif
			
			Open();
		}
		
		private string FHostName;
		public string HostName
		{
			get	{ return FHostName; }
			set 
			{
				CheckInactive();
				FHostName = value;
			}
		}
		
		private string FInstanceName;
		public string InstanceName
		{
			get { return FInstanceName; }
			set
			{
				CheckInactive();
				FInstanceName = value;
			}
		}
		
		private int FOverridePortNumber;
		public int OverridePortNumber
		{
			get { return FOverridePortNumber; }
			set
			{
				CheckInactive();
				FOverridePortNumber = value; 
			}
		}
		
		private ConnectionSecurityMode FSecurityMode;
		public ConnectionSecurityMode SecurityMode
		{
			get { return FSecurityMode; }
			set
			{
				CheckInactive();
				FSecurityMode = value;
			}
		}
		
		private int FOverrideListenerPortNumber;
		public int OverrideListenerPortNumber
		{
			get { return FOverrideListenerPortNumber; }
			set
			{
				CheckInactive();
				FOverrideListenerPortNumber = value; 
			}
		}
		
		private ConnectionSecurityMode FListenerSecurityMode;
		public ConnectionSecurityMode ListenerSecurityMode
		{
			get { return FListenerSecurityMode; }
			set
			{
				CheckInactive();
				FListenerSecurityMode = value;
			}
		}
		
		private ChannelFactory<IClientDataphorService> FChannelFactory;
		
		public bool IsActive { get { return FChannelFactory != null; } }
		
		private void CheckActive()
		{
			if (!IsActive)
				throw new ServerException(ServerException.Codes.ServerInactive);
		}
		
		private void CheckInactive()
		{
			if (IsActive)
				throw new ServerException(ServerException.Codes.ServerActive);
		}
		
		public void Open()
		{
			if (!IsActive)
				if (FOverridePortNumber == 0)
				{
					Uri LUri = new Uri(ListenerFactory.GetInstanceURI(FHostName, FOverrideListenerPortNumber, FListenerSecurityMode, FInstanceName, FSecurityMode));
					FChannelFactory =
						new ChannelFactory<IClientDataphorService>
						(
							DataphorServiceUtility.GetBinding(LUri.Scheme == Uri.UriSchemeHttps), 
							new EndpointAddress(LUri)
						);
				}
				else
				{
					FChannelFactory = 
						new ChannelFactory<IClientDataphorService>
						(
							DataphorServiceUtility.GetBinding(FSecurityMode == ConnectionSecurityMode.Transport), 
							new EndpointAddress(DataphorServiceUtility.BuildInstanceURI(FHostName, FOverridePortNumber, FSecurityMode == ConnectionSecurityMode.Transport, FInstanceName))
						);
				}
		}
		
		public void Close()
		{
			if (IsActive)
			{
				if (FChannel != null)
					CloseChannel(FChannel);
				SetChannel(null);
				if (FChannelFactory != null)
				{
					CloseChannelFactory();
					FChannelFactory = null;
				}
			}
		}

		protected override void InternalDispose()
		{
			Close();
		}
		
		private IClientDataphorService FChannel;
		
		private bool IsChannelFaulted(IClientDataphorService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Faulted;
		}
		
		private bool IsChannelValid(IClientDataphorService AChannel)
		{
			return ((ICommunicationObject)AChannel).State == CommunicationState.Opened;
		}
		
		private void CloseChannel(IClientDataphorService AChannel)
		{
			ICommunicationObject LChannel = (ICommunicationObject)AChannel;
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
		
		private void SetChannel(IClientDataphorService AChannel)
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
		
		public IClientDataphorService GetServiceInterface()
		{
			CheckActive();
			if ((FChannel == null) || !IsChannelValid(FChannel))
				SetChannel(FChannelFactory.CreateChannel());
			return FChannel;
		}

		#region IRemoteServer Members

		public IRemoteServerConnection Establish(string AConnectionName, string AHostName)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginOpenConnection(AConnectionName, AHostName, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				return new ClientConnection(this, AConnectionName, AHostName, GetServiceInterface().EndOpenConnection(LResult));
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		public void Relinquish(IRemoteServerConnection AConnection)
		{
			try
			{
				IAsyncResult LResult = GetServiceInterface().BeginCloseConnection(((ClientConnection)AConnection).ConnectionHandle, null, null);
				LResult.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCloseConnection(LResult);
			}
			catch (FaultException<DataphorFault> LFault)
			{
				throw DataphorFaultUtility.FaultToException(LFault.Detail);
			}
		}

		#endregion

		#region IServerBase Members

		public string Name
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetServerName(null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetServerName(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		public void Start()
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}

		public ServerState State
		{
			get 
			{ 
				throw new NotImplementedException();
			}
		}

		public long CacheTimeStamp
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetCacheTimeStamp(null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetCacheTimeStamp(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		public long DerivationTimeStamp
		{
			get 
			{ 
				try
				{
					IAsyncResult LResult = GetServiceInterface().BeginGetDerivationTimeStamp(null, null);
					LResult.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetDerivationTimeStamp(LResult);
				}
				catch (FaultException<DataphorFault> LFault)
				{
					throw DataphorFaultUtility.FaultToException(LFault.Detail);
				}
			}
		}

		#endregion
	}
}
