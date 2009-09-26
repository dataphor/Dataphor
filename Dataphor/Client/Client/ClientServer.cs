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

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientServer : ClientObject, IRemoteServer
	{
		public ClientServer() { }
		public ClientServer(string AHostName, int APortNumber, string AInstanceName)
		{
			FHostName = AHostName;
			FPortNumber = APortNumber;
			FInstanceName = AInstanceName;
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
		
		private int FPortNumber;
		public int PortNumber
		{
			get { return FPortNumber; }
			set
			{
				CheckInactive();
				FPortNumber = value; 
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
				FChannelFactory = 
					new ChannelFactory<IClientDataphorService>
					(
						new CustomBinding(new BinaryMessageEncodingBindingElement(), new HttpTransportBindingElement()), 
						DataphorServiceUtility.BuildURI(FHostName, FPortNumber, FInstanceName)
					);
		}
		
		public void Close()
		{
			if (IsActive)
			{
				SetChannel(null);
				FChannelFactory = null;
			}
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
			return new ClientConnection(this, AConnectionName, AHostName);
		}

		public void Relinquish(IRemoteServerConnection AConnection)
		{
			// Nothing to do here
		}

		#endregion

		#region IServerBase Members

		public string Name
		{
			get { throw new NotImplementedException(); }
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
			get { throw new NotImplementedException(); }
		}

		public Guid InstanceID
		{
			get { throw new NotImplementedException(); }
		}

		public long CacheTimeStamp
		{
			get { throw new NotImplementedException(); }
		}

		public long DerivationTimeStamp
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}
