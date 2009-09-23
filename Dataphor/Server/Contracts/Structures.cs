using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Contracts
{
	[Serializable]
	/// <nodoc/>
	public struct RemoteRowHeader
    {
		public string[] Columns;
    }
    
	[Serializable]
	/// <nodoc/>
	public struct RemoteRowBody
    {
		public byte[] Data;
    }
    
	[Serializable]
	/// <nodoc/>
	public struct RemoteRow
    {
		public RemoteRowHeader Header;
		public RemoteRowBody Body;
    }

	/* HACK: We have discovered that while remoting, some types of complicated nested structs are not able to be
	 * passed through the remoting layer.  A fix up error is given.  MOS noticed that there are at least two
	 * different errors that can be received.
	 * One case is where a struct contains an array of a different struct
	 * in which that struct has an array of a native data type (or more complicated than this).
	 * The other case is when a struct contains a struct which contains an enum.  This is a case in our system.
	 * To overcome this bug, in two of our structs below, the enums were replaced with bytes (for they both fit in
	 * a byte) and in the places that they are referenced they are cast from enum to byte or vice versa.
	 * This bug was reported to microsoft, and should be fixed in version 1.1 of the framework.
	 * MOS
	*/	

	[Serializable]
	/// <nodoc/>
	public struct RemoteFetchData
    {
		public RemoteRowBody[] Body;
		public byte Flags; // hack: changed from 'CursorGetFlags' to 'byte' in order fix fixup error
	}

	[Serializable]
	/// <nodoc/>
	public struct RemoteProposeData
    {
		public bool Success;
		public RemoteRowBody Body;
    }

    [Serializable]
	/// <nodoc/>
	public struct RemoteParam
    {
		public string Name;
		public string TypeName;
		public byte Modifier; // hack: changed from 'Modifier' to 'byte' to fix fixup error
    }
    
    [Serializable]
	/// <nodoc/>
	public struct RemoteParamData
    {
		public RemoteParam[] Params;
		public RemoteRowBody Data;
    }

    [Serializable]
	/// <nodoc/>
	public struct RemoteMoveData
    {
		public int Count;
		public CursorGetFlags Flags;
    }
    
	[Serializable]
	/// <nodoc/>
	public struct RemoteGotoData
    {
		public bool Success;
		public CursorGetFlags Flags;
    }
}
