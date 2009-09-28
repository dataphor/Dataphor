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
	[ServiceContract]
	public interface IListenerService
	{
		/// <summary>
		/// Enumerates the available Dataphor instances.
		/// </summary>
		[OperationContract]
		string[] EnumerateInstances();
		
		/// <summary>
		/// Returns the URI for an instance.
		/// </summary>
		[OperationContract]
		string GetInstanceURI(string AInstanceName);
		
		/// <summary>
		/// Returns the URI for the native CLI of an instance.
		/// </summary>
		[OperationContract]
		string GetNativeInstanceURI(string AInstanceName);
	}
}
