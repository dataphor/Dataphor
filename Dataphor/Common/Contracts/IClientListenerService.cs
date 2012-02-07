/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Contracts
{
	[ServiceContract(Name = "IListenerService", Namespace = "http://dataphor.org/dataphor/3.0/")]
	public interface IClientListenerService
	{
		/// <summary>
		/// Enumerates the available Dataphor instances.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(ListenerFault))]
		IAsyncResult BeginEnumerateInstances(AsyncCallback ACallback, object AState);
		string[] EndEnumerateInstances(IAsyncResult AResult);
		
		/// <summary>
		/// Returns the URI for an instance.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(ListenerFault))]
		IAsyncResult BeginGetInstanceURI(string AHostName, string AInstanceName, AsyncCallback ACallback, object AState);
		string EndGetInstanceURI(IAsyncResult AResult);
		
		/// <summary>
		/// Returns the URI for the native CLI of an instance.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(ListenerFault))]
		IAsyncResult BeginGetNativeInstanceURI(string AHostName, string AInstanceName, AsyncCallback ACallback, object AState);
		string EndGetNativeInstanceURI(IAsyncResult AResult);
	}
}
