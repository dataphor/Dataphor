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

using Alphora.Common.WCF;

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
		
		/// <summary>The two security-mode settings as the alias editor labels them, for the message that refuses a mode this version cannot honour.</summary>
		/// <remarks>
		/// An alias is where almost every one of these is set, and the person reading the refusal is the person who has to
		/// clear it, so the message names the field rather than the property.
		/// </remarks>
		private const string SecurityModeSettingName = "SecurityMode (\"Security Mode\" in the alias editor)";
		private const string ListenerSecurityModeSettingName = "ListenerSecurityMode (the listener's \"Security Mode\" in the alias editor)";

		private ChannelFactory<IClientDataphorService> _channelFactory;
		
		public bool IsActive { get { return _channelFactory != null; } }
		
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
					// The .NET WCF client cannot read endpoints from an application configuration file.
					throw new NotSupportedException
					(
						String.Format
						(
							"The connection setting ClientConfigurationName (\"Config Name\" in the alias editor) is set to \"{0}\", but endpoint configuration names (WCF endpoints read from an application configuration file) are not supported on this version of Dataphor. Clear the setting and connect with the host name, instance name and port settings instead.",
							_clientConfigurationName
						)
					);
				}
				else
				{
					// Both checks are made before the listener is contacted below: a connection that asks for protection
					// this version cannot give must make no unsecured call on its way to being refused.
					DataphorServiceUtility.CheckSecurityMode(_securityMode, SecurityModeSettingName);
					if (_overridePortNumber == 0)
					{
						// The listener's mode is checked only here, on the path that actually uses the listener; refusing
						// for a setting this connection never consults would be gratuitous.
						DataphorServiceUtility.CheckSecurityMode(_listenerSecurityMode, ListenerSecurityModeSettingName);
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

				// Read once into a local and captured, so that the holder creates channels on this factory whatever Close
				// does to the field afterwards.
				ChannelFactory<IClientDataphorService> channelFactory = _channelFactory;
				_channelHolder = new ChannelHolder<IClientDataphorService>(() => channelFactory.CreateChannel());
			}
		}
		
		public void Close()
		{
			if (IsActive)
			{
				if (_channelHolder != null)
				{
					_channelHolder.Dispose();
					_channelHolder = null;
				}
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
		
		private ChannelHolder<IClientDataphorService> _channelHolder;
		
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
		
		/// <summary>Returns a channel on which to call the far server, creating one if the held channel is unusable.</summary>
		/// <remarks>
		/// The holder field is read once into a local rather than tested for activity and then dereferenced: the keep-alive
		/// pings from a thread of its own, so a Close can land between the two, and what that used to raise was a
		/// NullReferenceException from the middle of a ping. A thread that took the local just before the Close now gets
		/// ObjectDisposedException from the holder, which SendKeepAlive already swallows. The error raised when the client
		/// was never opened, or was closed before the local was taken, is ServerInactive, as it was.
		/// </remarks>
		public IClientDataphorService GetServiceInterface()
		{
			ChannelHolder<IClientDataphorService> channelHolder = _channelHolder;
			if (channelHolder == null)
				throw new ServerException(ServerException.Codes.ServerInactive);
			return channelHolder.GetChannel();
		}

		public void ReportCommunicationError()
		{
			// NOTE: accepted tradeoff - a communication failure does not reset the channel here, and the caller is left to
			// its own retry. Closing the channel was tried and declined: the communication object is supposed to be
			// reporting Faulted, but in some cases it still reports Opened even though any call on it raises
			// CommunicationException, and when the server is gone closing it attempts to reconnect and times out every
			// time, so unwinding takes ten times as long. What the channel holder's state test now covers instead is a
			// channel reporting Faulted, Closing or Closed: the next call replaces it. The case still left to the caller
			// is the one this method was written for - a channel reporting Opened on which every call fails. Revisit if a
			// bounded time-out is ever set on the client binding, which would cap the cost of a close against a server
			// that has gone.
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
			// FaultException<DataphorFault> derives from CommunicationException, so the typed catch must stay first.
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
			catch (CommunicationException ce)
			{
				ReportCommunicationError();
				throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
				catch (CommunicationException ce)
				{
					ReportCommunicationError();
					throw new ServerException(ServerException.Codes.CommunicationFailure, ErrorSeverity.Environment, ce);
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
