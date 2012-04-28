/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE.Schema;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Device.Memory;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.DAE.Device.Fastore
{
	public class FastoreDevice : MemoryDevice
	{
		protected override void InternalStart(ServerProcess process)
		{
			

			base.InternalStart(process);
		}

		protected override void InternalStop(ServerProcess process)
		{
			
			base.InternalStop(process);
		}

		// Session
		protected override DeviceSession InternalConnect(ServerProcess serverProcess, DeviceSessionInfo deviceSessionInfo)
		{
			return new FastoreDeviceSession(this, serverProcess, deviceSessionInfo);
		}
	}

	public class FastoreDeviceSession : MemoryDeviceSession
	{
		protected internal FastoreDeviceSession
		(
			Schema.Device device, 
			ServerProcess serverProcess, 
			Schema.DeviceSessionInfo deviceSessionInfo
		) : base(device, serverProcess, deviceSessionInfo){}
	}
}
