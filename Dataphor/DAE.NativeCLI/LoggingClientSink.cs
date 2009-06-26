/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Remoting.Channels;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	#if DEBUG
	public class LoggingClientSinkProvider : IClientChannelSinkProvider
	{
		public IClientChannelSink CreateSink(IChannelSender AChannel, string AURL, object ARemoteChannelData)
		{
			return new LoggingClientSink(FNext == null ? null : FNext.CreateSink(AChannel, AURL, ARemoteChannelData));
		}

		private IClientChannelSinkProvider FNext;
		public IClientChannelSinkProvider Next
		{
			get { return FNext; }
			set { FNext = value; }
		}
	}

	public class LoggingClientSink : BaseChannelSinkWithProperties, IClientChannelSink, IChannelSinkBase
	{
		public LoggingClientSink(IClientChannelSink ANextSink)
		{
			FNextChannelSink = ANextSink;
		}
		
		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders headers, System.IO.Stream stream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.AsyncProcessRequest(sinkStack, msg, headers, stream);
		}

		public void ProcessMessage(System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders requestHeaders, System.IO.Stream requestStream, out ITransportHeaders responseHeaders, out System.IO.Stream responseStream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.ProcessMessage(msg, requestHeaders, requestStream, out responseHeaders, out responseStream);
			else
			{
				responseHeaders = null;
				responseStream = null;
			}
		}

		public void AsyncProcessResponse(IClientResponseChannelSinkStack sinkStack, object state, ITransportHeaders headers, System.IO.Stream stream)
		{
			if (FNextChannelSink != null)
				FNextChannelSink.AsyncProcessResponse(sinkStack, state, headers, stream);
		}

		public System.IO.Stream GetRequestStream(System.Runtime.Remoting.Messaging.IMessage msg, ITransportHeaders headers)
		{
			if (FNextChannelSink != null)
				return FNextChannelSink.GetRequestStream(msg, headers);
			return null;
		}

		private IClientChannelSink FNextChannelSink;
		public IClientChannelSink NextChannelSink { get { return FNextChannelSink; } }
	}
	#endif
}
