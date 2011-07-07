/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ServiceModel;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Contracts
{
	/// <summary>
	/// Describes the interface for the Dataphor listener.
	/// </summary>
	[ServiceContract(Name = "IListenerService", Namespace = "http://dataphor.org/dataphor/3.0/")]
	public interface IListenerService
	{
		/// <summary>
		/// Enumerates the available Dataphor instances.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(ListenerFault))]
		string[] EnumerateInstances();
		
		/// <summary>
		/// Returns the URI for an instance.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(ListenerFault))]
		string GetInstanceURI(string AHostName, string AInstanceName);
		
		/// <summary>
		/// Returns the URI for the native CLI of an instance.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(ListenerFault))]
		string GetNativeInstanceURI(string AHostName, string AInstanceName);
	}
}
