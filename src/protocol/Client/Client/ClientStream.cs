/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.ServiceModel;
using System.Collections.Generic;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Contracts;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Client
{
	public class ClientStream : StreamBase, IRemoteStream
	{
		public ClientStream(ClientProcess clientProcess, int streamHandle)
		{
			_clientProcess = clientProcess;
			_streamHandle = streamHandle;
		}
		
		private ClientProcess _clientProcess;
		public ClientProcess ClientProcess { get { return _clientProcess; } }
		
		private IClientDataphorService GetServiceInterface()
		{
			return _clientProcess.ClientSession.ClientConnection.ClientServer.GetServiceInterface();
		}

		private void ReportCommunicationError()
		{
			_clientProcess.ClientSession.ClientConnection.ClientServer.ReportCommunicationError();
		}
		
		private int _streamHandle;
		public int StreamHandle { get { return _streamHandle; } }
		
		public override long Length
		{
			get
			{
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetStreamLength(_streamHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetStreamLength(result);
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
		
		public override void SetLength(long tempValue)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginSetStreamLength(_streamHandle, tempValue, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndSetStreamLength(result);
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
		
		public override long Position
		{
			get
			{
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginGetStreamPosition(_streamHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					return channel.EndGetStreamPosition(result);
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
			set
			{
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginSetStreamPosition(_streamHandle, value, null, null);
					result.AsyncWaitHandle.WaitOne();
					channel.EndSetStreamPosition(result);
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
		
		public override int Read(byte[] buffer, int offset, int count)
		{
			try
			{
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginReadStream(_streamHandle, count, null, null);
				result.AsyncWaitHandle.WaitOne();
				byte[] bytes = channel.EndReadStream(result);
				Array.Copy(bytes, 0, buffer, offset, bytes.Length); // ?? Should the buffer bytes beyond the read bytes be cleared?
				return bytes.Length;
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
		
		public override void Write(byte[] buffer, int offset, int count)
		{
			try
			{
				byte[] bytes = new byte[count];
				Array.Copy(buffer, offset, bytes, 0, count);
				var channel = GetServiceInterface();
				IAsyncResult result = channel.BeginWriteStream(_streamHandle, bytes, null, null);
				result.AsyncWaitHandle.WaitOne();
				channel.EndWriteStream(result);
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

		protected override void Dispose(bool disposing)
		{
			try
			{
				try
				{
					var channel = GetServiceInterface();
					IAsyncResult result = channel.BeginCloseStream(_streamHandle, null, null);
					result.AsyncWaitHandle.WaitOne();
					channel.EndCloseStream(result);
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
			finally
			{
				base.Dispose(disposing);
			}
		}

		#region IRemoteStream Members

		long IRemoteStream.Length
		{
			get { return Length; }
		}

		void IRemoteStream.SetLength(long tempValue)
		{
			SetLength(tempValue);
		}

		long IRemoteStream.Position
		{
			get { return Position; }
			set { Position = value; }
		}

		void IRemoteStream.Flush()
		{
			Flush();
		}

		bool IRemoteStream.CanRead
		{
			get { return CanRead; }
		}

		bool IRemoteStream.CanSeek
		{
			get { return CanSeek; }
		}

		bool IRemoteStream.CanWrite
		{
			get { return CanWrite; }
		}

		int IRemoteStream.Read(byte[] buffer, int offset, int count)
		{
			return Read(buffer, offset, count);
		}

		int IRemoteStream.ReadByte()
		{
			return ReadByte();
		}

		long IRemoteStream.Seek(long offset, SeekOrigin origin)
		{
			return Seek(offset, origin);
		}

		void IRemoteStream.Write(byte[] buffer, int offset, int count)
		{
			Write(buffer, offset, count);
		}

		void IRemoteStream.WriteByte(byte byteValue)
		{
			WriteByte(byteValue);
		}

		void IRemoteStream.Close()
		{
			Close();
		}

		#endregion

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			Close();
		}

		#endregion
	}
}
