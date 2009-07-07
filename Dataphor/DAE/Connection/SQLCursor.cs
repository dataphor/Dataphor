/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Alphora.Dataphor;

namespace Alphora.Dataphor.DAE.Connection
{
	// Provides the behavior template for a forward only cursor
	// Values are returned in terms of native CLR values, with a null reference returned for a null column.
	// Deferred Read values are returned in terms of a random access read only stream on the data.
	// Deferred streams must be accessible after the Cursor is closed or navigated.
	public abstract class SQLCursor : Disposable
	{
		protected SQLCursor(SQLCommand ACommand) : base()
		{
			FCommand = ACommand;
			FCommand.SetActiveCursor(this);
		}
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FCommand != null)
				{
					try
					{
						FCommand.SetActiveCursor(null);
						FCommand.InternalClose();
					}
					finally
					{
						FCommand = null;
					}
				}
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		protected SQLCommand FCommand;
		public SQLCommand Command { get { return FCommand; } }
		
		protected abstract SQLTableSchema InternalGetSchema();
		protected SQLTableSchema GetSchema()
		{
			try
			{
				return InternalGetSchema();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "getschema", true);
				throw;
			}
		}
		
		protected SQLTableSchema FSchema;
		public SQLTableSchema Schema
		{
			get
			{
				if (FSchema == null)
					FSchema = GetSchema();
				return FSchema;
			}
		}
		
		protected abstract bool InternalNext();
		public bool Next()
		{
			try
			{
				return InternalNext();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "next", true);
				throw;
			}
		}		

		protected abstract int InternalGetColumnCount();
		public int ColumnCount 
		{ 
			get
			{
				try
				{
					return InternalGetColumnCount();
				}
				catch (Exception LException)
				{
					FCommand.Connection.WrapException(LException, "getcolumncount", true);
					throw;
				}
			}
		}
		
		protected abstract object InternalGetColumnValue(int AIndex);
		public object this[int AIndex] 
		{ 
			get
			{
				try
				{
					return InternalGetColumnValue(AIndex);
				}
				catch (Exception LException)
				{
					FCommand.Connection.WrapException(LException, "getcolumnvalue", true);
					throw;
				}
			}
		}

		protected abstract bool InternalIsNull(int AIndex);
		public bool IsNull(int AIndex)
		{
			try
			{
				return InternalIsNull(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "isnull", true);
				throw;
			}
		}
		
		protected abstract bool InternalIsDeferred(int AIndex);
		public bool IsDeferred(int AIndex)
		{
			try
			{
				return InternalIsDeferred(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "isdeferred", true);
				throw;
			}
		}

		protected abstract Stream InternalOpenDeferredStream(int AIndex);
		public Stream OpenDeferredStream(int AIndex)
		{
			try
			{
				return InternalOpenDeferredStream(AIndex);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "opendeferredstream", true);
				throw;
			}
		}

		protected virtual bool InternalFindKey(object[] AKey)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool FindKey(object[] AKey)
		{
			try
			{
				return InternalFindKey(AKey);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "findkey", true);
				throw;
			}
		}

		protected virtual void InternalFindNearest(object[] AKey)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public void FindNearest(object[] AKey)
		{
			try
			{
				InternalFindNearest(AKey);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "findnearest", true);
				throw;
			}
		}
		
		protected virtual string InternalGetFilter()
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public string GetFilter()
		{
			try
			{
				return InternalGetFilter();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "getfilter", true);
				throw;
			}
		}
		
		protected virtual bool InternalSetFilter(string AFilter)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool SetFilter(string AFilter)
		{
			try
			{
				return InternalSetFilter(AFilter);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "setfilter", true);
				throw;
			}
		}
		
		protected virtual void InternalInsert(string[] ANames, object[] AValues) 
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Insert(string[] ANames, object[] AValues) 
		{
			try
			{
				InternalInsert(ANames, AValues);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "insert", true);
				throw;
			}
		}

		protected virtual void InternalUpdate(string[] ANames, object[] AValues)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Update(string[] ANames, object[] AValues)
		{
			try
			{
				InternalUpdate(ANames, AValues);
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "update", false);
			}
		}

		protected virtual void InternalDelete()
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Delete()
		{
			try
			{
				InternalDelete();
			}
			catch (Exception LException)
			{
				FCommand.Connection.WrapException(LException, "delete", false);
			}
		}
	}
}
