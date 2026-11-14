/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
/*
	public abstract class RowIndex : Disposable
	{
		public RowIndex(ServerProcess AProcess) : base()
		{
			Process = AProcess;
			ValuesOwned = true;
		}
		
		public RowIndex(ServerProcess AProcess, NativeRowIndex ARowIndex) : base()
		{
			Process = AProcess;
			ValuesOwned = false;
		}
		
		public ServerProcess Process;
		
		public bool ValuesOwned;
		
		protected override void Dispose(bool ADisposing)
		{
			if (Process != null)
			{
				if (ValuesOwned)
					DisposeValues();
				Process = null;
			}
			base.Dispose(ADisposing);
		}
		
		protected abstract void DisposeValues();
	}

	#if USETYPEDLIST
	public class RowTreeList : DisposableTypedList
	{
		public RowTreeList() : base(typeof(RowTree), true, false){}
		
		public new RowTree this[int AIndex]
		{
			get { return (RowTree)base[AIndex]; }
			set { base[AIndex] = value; }
		}
	}
	#else
	public class RowTreeList : DisposableList<RowTree> { }
	#endif
	
	public class RowTree : System.Object
	{
		public RowTree(ServerProcess AProcess, Schema.Order AKey, Schema.RowType AKeyRowType, Schema.RowType ADataRowType, int AFanout, int ACapacity, bool AIsClustered) : base()
		{
			FRowTree = new NativeRowTree(AKey, AKeyRowType, ADataRowType, AFanout, ACapacity, AIsClustered);
		}
		
		public RowTree(ServerProcess AProcess, NativeRowTree ARowTree) : base()
		{
			FRowTree = ARowTree;
		}
		
		private NativeRowTree FRowTree;
		public NativeRowTree Tree { get { return FRowTree; } }
		
	}
	
	public class RowHashTable : RowIndex
	{
	}
*/
}
