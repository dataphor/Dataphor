/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// Describes the interface for the Dataphor listener.
	/// </summary>
	public interface IListener
	{
		/// <summary>
		/// Enumerates the available Dataphor instances.
		/// </summary>
		string[] EnumerateInstances();
		
		/// <summary>
		/// Returns the URI for an instance.
		/// </summary>
		string GetInstanceURI(string AInstanceName);
		
		/// <summary>
		/// Returns the URI for the standard or native CLI of an instance.
		/// </summary>
		string GetInstanceURI(string AInstanceName, bool AUseNativeCLI);
	}
}
