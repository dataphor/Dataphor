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
		public ClientServer(string hostName, string instanceName, int overridePortNumber, ConnectionSecurityMode securityMode, int overrideListenerPortNumber, ConnectionSecurityMode listenerSecurityMode)
		{
			_hostName = hostName;
			_instanceName = instanceName;
			_overridePortNumber = overridePortNumber;
			_securityMode = securityMode;
			_overrideListenerPortNumber = overrideListenerPortNumber;
			_listenerSecurityMode = listenerSecurityMode;
			
			#if SILVERLIGHT
			System.Net.WebRequest.RegisterPrefix("http://", System.Net.Browser.WebRequestCreator.ClientHttp);
			System.Net.WebRequest.RegisterPrefix("https://", System.Net.Browser.WebRequestCreator.ClientHttp);
			#endif
			
			Open();
		}
		
		private string _hostName;
		public string HostName
		{
			get	{ return _hostName; }
			set 
			{
				CheckInactive();
				_hostName = value;
			}
		}
		
		private string _instanceName;
		public string InstanceName
		{
			get { return _instanceName; }
			set
			{
				CheckInactive();
				_instanceName = value;
			}
		}
		
		private int _overridePortNumber;
		public int OverridePortNumber
		{
			get { return _overridePortNumber; }
			set
			{
				CheckInactive();
				_overridePortNumber = value; 
			}
		}
		
		private ConnectionSecurityMode _securityMode;
		public ConnectionSecurityMode SecurityMode
		{
			get { return _securityMode; }
			set
			{
				CheckInactive();
				_securityMode = value;
			}
		}
		
		private int _overrideListenerPortNumber;
		public int OverrideListenerPortNumber
		{
			get { return _overrideListenerPortNumber; }
			set
			{
				CheckInactive();
				_overrideListenerPortNumber = value; 
			}
		}
		
		private ConnectionSecurityMode _listenerSecurityMode;
		public ConnectionSecurityMode ListenerSecurityMode
		{
			get { return _listenerSecurityMode; }
			set
			{
				CheckInactive();
				_listenerSecurityMode = value;
			}
		}
		
		private ChannelFactory<IClientDataphorService> _channelFactory;
		
		public bool IsActive { get { return _channelFactory != null; } }
		
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
				if (_overridePortNumber == 0)
				{
					Uri uri = new Uri(ListenerFactory.GetInstanceURI(_hostName, _overrideListenerPortNumber, _instanceName));
					_channelFactory =
						new ChannelFactory<IClientDataphorService>
						(
							DataphorServiceUtility.GetBinding(), 
							new EndpointAddress(uri)
						);
				}
				else
				{
					_channelFactory = 
						new ChannelFactory<IClientDataphorService>
						(
							DataphorServiceUtility.GetBinding(), 
							new EndpointAddress(DataphorServiceUtility.BuildInstanceURI(_hostName, _overridePortNumber, _instanceName))
						);
				}
		}
		
		public void Close()
		{
			if (IsActive)
			{
				if (_channel != null)
					CloseChannel(_channel);
				SetChannel(null);
				if (_channelFactory != null)
				{
					CloseChannelFactory();
					_channelFactory = null;
				}
			}
		}

		protected override void InternalDispose()
		{
			Close();
		}
		
		private IClientDataphorService _channel;
		
		private bool IsChannelFaulted(IClientDataphorService channel)
		{
			return ((ICommunicationObject)channel).State == CommunicationState.Faulted;
		}
		
		private bool IsChannelValid(IClientDataphorService channel)
		{
			return ((ICommunicationObject)channel).State == CommunicationState.Opened;
		}
		
		private void CloseChannel(IClientDataphorService channel)
		{
			ICommunicationObject localChannel = (ICommunicationObject)channel;
			if (localChannel.State == CommunicationState.Opened)
				localChannel.Close();
			else
				localChannel.Abort();
		}
		
		private void CloseChannelFactory()
		{
			if (_channelFactory.State == CommunicationState.Opened)
				_channelFactory.Close();
			else
				_channelFactory.Abort();
		}
		
		private void SetChannel(IClientDataphorService channel)
		{
			if (_channel != null)
				((ICommunicationObject)_channel).Faulted -= new EventHandler(ChannelFaulted);
			_channel = channel;
			if (_channel != null)
				((ICommunicationObject)_channel).Faulted += new EventHandler(ChannelFaulted);
		}
		
		private void ChannelFaulted(object sender, EventArgs args)
		{
			((ICommunicationObject)sender).Faulted -= new EventHandler(ChannelFaulted);
			if (_channel == sender)
				_channel = null;
		}
		
		public IClientDataphorService GetServiceInterface()
		{
			CheckActive();
			if ((_channel == null) || !IsChannelValid(_channel))
				SetChannel(_channelFactory.CreateChannel());
			return _channel;
		}

		#region IRemoteServer Members

		public IRemoteServerConnection Establish(string connectionName, string hostName)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginOpenConnection(connectionName, hostName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientConnection(this, connectionName, hostName, GetServiceInterface().EndOpenConnection(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Relinquish(IRemoteServerConnection connection)
		{
			try
			{
				IAsyncResult result = GetServiceInterface().BeginCloseConnection(((ClientConnection)connection).ConnectionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				GetServiceInterface().EndCloseConnection(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
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
					IAsyncResult result = GetServiceInterface().BeginGetServerName(null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetServerName(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
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
					IAsyncResult result = GetServiceInterface().BeginGetCacheTimeStamp(null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetCacheTimeStamp(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		public long DerivationTimeStamp
		{
			get 
			{ 
				try
				{
					IAsyncResult result = GetServiceInterface().BeginGetDerivationTimeStamp(null, null);
					result.AsyncWaitHandle.WaitOne();
					return GetServiceInterface().EndGetDerivationTimeStamp(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		#endregion
	}
}
