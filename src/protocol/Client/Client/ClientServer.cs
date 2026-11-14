/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

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

		public ClientServer(string hostName, string clientConfigurationName)
		{
			_hostName = hostName;
			_clientConfigurationName = clientConfigurationName;

			#if SILVERLIGHT
			System.Net.WebRequest.RegisterPrefix("http://", System.Net.Browser.WebRequestCreator.ClientHttp);
			System.Net.WebRequest.RegisterPrefix("https://", System.Net.Browser.WebRequestCreator.ClientHttp);
			#endif
			
			Open();
		}

		private string _clientConfigurationName;
		public string ClientConfigurationName
		{
			get { return _clientConfigurationName; }
			set
			{
				CheckInactive();
				_clientConfigurationName = value;
			}
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
			{
				if (!String.IsNullOrEmpty(_clientConfigurationName))
				{
					_channelFactory = new ChannelFactory<IClientDataphorService>(_clientConfigurationName);
				}
				else
				{
					if (_overridePortNumber == 0)
					{
						Uri uri = new Uri(ListenerFactory.GetInstanceURI(_hostName, _overrideListenerPortNumber, _instanceName));
						_channelFactory =
							new ChannelFactory<IClientDataphorService>
							(
								DataphorServiceUtility.GetBinding(), 
								new EndpointAddress(uri, DataphorServiceUtility.BuildEndpointIdentityCall())
							);
					}
					else
					{
						_channelFactory = 
							new ChannelFactory<IClientDataphorService>
							(
								DataphorServiceUtility.GetBinding(), 
								new EndpointAddress
								(
									new Uri(DataphorServiceUtility.BuildInstanceURI(_hostName, _overridePortNumber, _instanceName)), 
									DataphorServiceUtility.BuildEndpointIdentityCall()
								)
							);
					}
				}

				DataphorServiceUtility.ServiceEndpointHook(_channelFactory.Endpoint);
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
			try
			{
				ICommunicationObject localChannel = (ICommunicationObject)channel;
				if (localChannel.State == CommunicationState.Opened)
					localChannel.Close();
				else
					localChannel.Abort();
			}
			catch
			{
				// Ignore exceptions here, there's nothing we can do about it anyway.
			}
		}
		
		private void CloseChannelFactory()
		{
			try
			{
				if (_channelFactory.State == CommunicationState.Opened)
					_channelFactory.Close();
				else
					_channelFactory.Abort();
			}
			catch
			{
				// Ignore exceptions here, there's nothing we can do about it anyway.
			}
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

		public void ReportCommunicationError()
		{
			// A communication failure has occurred, reset the channel
			// The communication object is supposed to be reporting Faulted, but in some cases, it still indicates it's open, even though any call will result in a CommunicationException
			// If the server is gone, this is actually worse, because it attempts to reconnect and times out everytime, so unwinding takes ten times as long...
			//if (_channel != null)
			//	CloseChannel(_channel);
			//SetChannel(null);
		}

		#region IRemoteServer Members

		public IRemoteServerConnection Establish(string connectionName, string hostName)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginOpenConnection(connectionName, hostName, null, null);
				result.AsyncWaitHandle.WaitOne();
				return new ClientConnection(this, connectionName, hostName, channel.EndOpenConnection(result));
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
			}
		}

		public void Relinquish(IRemoteServerConnection connection)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginCloseConnection(((ClientConnection)connection).ConnectionHandle, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndCloseConnection(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetServerName(null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetServerName(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		public void Start()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginStart(null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndStart(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public void Stop()
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginStop(null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndStop(result);
			}
			catch (FaultException<DataphorFault> fault)
			{
				throw DataphorFaultUtility.FaultToException(fault.Detail);
			}
		}

		public ServerState State
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetState(null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetState(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
			}
		}

		public long CacheTimeStamp
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetCacheTimeStamp(null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetCacheTimeStamp(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		public long DerivationTimeStamp
		{
			get 
			{ 
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetDerivationTimeStamp(null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetDerivationTimeStamp(result);
				}
				catch (FaultException<DataphorFault> fault)
				{
					throw DataphorFaultUtility.FaultToException(fault.Detail);
				}
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
				}
			}
		}

		#endregion
	}
}
