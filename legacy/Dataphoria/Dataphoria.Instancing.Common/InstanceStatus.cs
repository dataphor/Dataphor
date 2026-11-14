/*
	Alphora Dataphor
	© Copyright 2000-2014 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.Dataphoria.Instancing.Common
{
	/// <summary>
	/// The possible values indicating the state of the Dataphor Server.
	/// </summary>
	public enum InstanceState
	{
		/// <summary>
		/// The Dataphor Server state is not known.
		/// </summary>
		Unknown,

		/// <summary>
		/// The Dataphor Server is not running, either because it has not been started, or it has been stopped.
		/// </summary>
		Stopped,

		/// <summary>
		/// The Dataphor Server is in the process of starting in response to a Start command. 
		/// The Dataphor Server will not respond to connection requests while it is in this state.
		/// </summary>
		Starting,

		/// <summary>
		/// The Dataphor Server is running and ready to accept connection requests.
		/// </summary>
		Started,

		/// <summary>
		/// The Dataphor Server is in the process of stopping in response to a Stop command. 
		/// The Dataphor Server will not respond to connection requests while it is in this state.
		/// </summary>
		Stopping,

		/// <summary>
		/// The Dataphor Server is unresponsive.
		/// </summary>
		Unresponsive
	}
}
