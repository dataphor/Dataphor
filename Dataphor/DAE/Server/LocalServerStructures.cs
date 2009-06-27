/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	public delegate void CacheClearedEvent(LocalServer AServer);

    public class LocalBookmark : System.Object
    {
		public LocalBookmark(Guid ABookmark)
		{
			FBookmark = ABookmark;
			ReferenceCount = 1;
		}
		
		private Guid FBookmark;
		public Guid Bookmark { get { return FBookmark; } }
		
		public int ReferenceCount;
    }
    
    public class LocalBookmarks : Hashtable
    {
		public void Add(LocalBookmark ABookmark)
		{
			Add(ABookmark.Bookmark, ABookmark);
		}
		
		public LocalBookmark this[Guid AGuid] { get { return (LocalBookmark)base[AGuid]; } }
    }
    
    public class LocalRow : Disposable
    {
		public LocalRow(Row ARow, Guid ABookmark)
		{
			FRow = ARow;
			FBookmark = ABookmark;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			if (FRow != null)
			{
				FRow.Dispose();
				FRow = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		protected Row FRow;
		public Row Row { get { return FRow; } }

		protected Guid FBookmark;
		public Guid Bookmark { get { return FBookmark; } }
    }

    public class LocalRows : DisposableTypedList
    {
		public LocalRows() : base(typeof(LocalRow), true, false){}

		public new LocalRow this[int AIndex]
		{
			get { return (LocalRow)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
		public BufferDirection BufferDirection;
    }
    
    public enum BufferDirection { Forward = 1, Backward = -1 }
}
