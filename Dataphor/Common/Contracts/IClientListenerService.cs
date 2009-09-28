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
	[ServiceContract(Name = "IListenerService")]
	public interface IClientListenerService
	{
		/// <summary>
		/// Enumerates the available Dataphor instances.
		/// </summary>
		[OperationContract(AsyncPattern = true, Action = "EnumerateInstances", ReplyAction = "EnumerateInstancesResponse")]
		IAsyncResult BeginEnumerateInstances(AsyncCallback ACallback, object AState);
		string[] EndEnumerateInstances(IAsyncResult AResult);
		
		/// <summary>
		/// Returns the URI for an instance.
		/// </summary>
		[OperationContract(AsyncPattern = true, Action = "GetInstanceURI", ReplyAction = "GetInstanceURIResponse")]
		IAsyncResult BeginGetInstanceURI(string AInstanceName, AsyncCallback ACallback, object AState);
		string EndGetInstanceURI(IAsyncResult AResult);
		
		/// <summary>
		/// Returns the URI for the standard or native CLI of an instance.
		/// </summary>
		[OperationContract(AsyncPattern = true, Action = "GetNativeInstanceURI", ReplyAction = "GetNativeInstanceURIResponse")]
		IAsyncResult BeginGetNativeInstanceURI(string AInstanceName, AsyncCallback ACallback, object AState);
		string EndGetNativeInstanceURI(IAsyncResult AResult);
	}
}
