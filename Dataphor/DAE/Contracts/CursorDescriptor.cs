/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Alphora.Dataphor.DAE.Contracts
{
	[DataContract]
	public class CursorDescriptor
	{
		public CursorDescriptor
		(
			int handle,
			CursorCapability capabilities,
			CursorType cursorType,
			CursorIsolation cursorIsolation
		) : base()
		{
			_handle = handle;
			_capabilities = capabilities;
			_cursorType = cursorType;
			_cursorIsolation = cursorIsolation;
		}
		
		[DataMember]
		internal int _handle;
		public int Handle { get { return _handle; } }

		[DataMember]
		internal CursorCapability _capabilities;
		public CursorCapability Capabilities { get { return _capabilities; } }
		
		[DataMember]
		internal CursorType _cursorType;
		public CursorType CursorType { get { return _cursorType; } }

		[DataMember]
		internal CursorIsolation _cursorIsolation;
		public CursorIsolation CursorIsolation { get { return _cursorIsolation; } }
	}
}
