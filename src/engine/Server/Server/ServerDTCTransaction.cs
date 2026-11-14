/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.EnterpriseServices;

namespace Alphora.Dataphor.DAE.Server
{
	[Transaction(TransactionOption.RequiresNew)]
	public class ServerDTCTransaction : ServicedComponent
	{
		protected override void Dispose(bool disposing)
		{
			Rollback();
			base.Dispose(disposing);
		}
		
		public void Commit()
		{
			ContextUtil.SetComplete();
		}
		
		public void Rollback()
		{
			ContextUtil.SetAbort();
		}
		
		private IsolationLevel _isolationLevel;
		public IsolationLevel IsolationLevel
		{
			get { return _isolationLevel; }
			set { _isolationLevel = value; }
		}
	}	
}