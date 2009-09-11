/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Server
{
	/// <summary>
	/// Defines the interface expected for a distributed-transaction coordinator transaction handler
	/// </summary>
	public interface IServerDTCTransaction
	{
		void Dispose();
		void Commit();
		void Rollback();
		IsolationLevel IsolationLevel { get; set; }
	}
}
