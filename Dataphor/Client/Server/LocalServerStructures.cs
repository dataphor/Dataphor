/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define USESPINLOCK
#define LOGFILECACHEEVENTS

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
		public LocalBookmark(Guid bookmark)
		{
			_bookmark = bookmark;
			ReferenceCount = 1;
		}
		
		private Guid _bookmark;
		public Guid Bookmark { get { return _bookmark; } }
		
		public int ReferenceCount;
    }
    
    public class LocalBookmarks : Dictionary<Guid, LocalBookmark>
    {
		public void Add(LocalBookmark bookmark)
		{
			Add(bookmark.Bookmark, bookmark);
		}
    }
    
    public class LocalRow : Disposable
    {
		public LocalRow(Row row, Guid bookmark)
		{
			_row = row;
			_bookmark = bookmark;
		}
		
		protected override void Dispose(bool disposing)
		{
			if (_row != null)
			{
				_row.Dispose();
				_row = null;
			}
			
			base.Dispose(disposing);
		}
		
		protected Row _row;
		public Row Row { get { return _row; } }

		protected Guid _bookmark;
		public Guid Bookmark { get { return _bookmark; } }
    }

	#if USETYPEDLIST
    public class LocalRows : DisposableTypedList
    {
		public LocalRows() : base(typeof(LocalRow), true, false){}

		public new LocalRow this[int AIndex]
		{
			get { return (LocalRow)base[AIndex]; }
			set { base[AIndex] = value; }
		}
		
	#else
	public class LocalRows : DisposableList<LocalRow>
	{
	#endif
		public BufferDirection BufferDirection;
    }
    
    public enum BufferDirection { Forward = 1, Backward = -1 }
}
