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
		protected SQLCursor(SQLCommand command) : base()
		{
			_command = command;
			_command.SetActiveCursor(this);
		}
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (_command != null)
				{
					try
					{
						_command.SetActiveCursor(null);
						_command.InternalClose();
					}
					finally
					{
						_command = null;
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		protected SQLCommand _command;
		public SQLCommand Command { get { return _command; } }
		
		protected abstract SQLTableSchema InternalGetSchema();
		protected SQLTableSchema GetSchema()
		{
			try
			{
				return InternalGetSchema();
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "getschema", true);
				throw;
			}
		}
		
		protected SQLTableSchema _schema;
		public SQLTableSchema Schema
		{
			get
			{
				if (_schema == null)
					_schema = GetSchema();
				return _schema;
			}
		}
		
		protected abstract bool InternalNext();
		public bool Next()
		{
			try
			{
				return InternalNext();
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "next", true);
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
				catch (Exception exception)
				{
					_command.Connection.WrapException(exception, "getcolumncount", true);
					throw;
				}
			}
		}
		
		protected abstract string InternalGetColumnName(int index);
		public string GetColumnName(int index)
		{
			try
			{
				return InternalGetColumnName(index);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "getcolumnname", true);
				throw;
			}
		}
		
		protected abstract object InternalGetColumnValue(int index);
		public object this[int index] 
		{ 
			get
			{
				try
				{
					return InternalGetColumnValue(index);
				}
				catch (Exception exception)
				{
					_command.Connection.WrapException(exception, "getcolumnvalue", true);
					throw;
				}
			}
		}

		protected abstract bool InternalIsNull(int index);
		public bool IsNull(int index)
		{
			try
			{
				return InternalIsNull(index);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "isnull", true);
				throw;
			}
		}
		
		protected abstract bool InternalIsDeferred(int index);
		public bool IsDeferred(int index)
		{
			try
			{
				return InternalIsDeferred(index);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "isdeferred", true);
				throw;
			}
		}

		protected abstract Stream InternalOpenDeferredStream(int index);
		public Stream OpenDeferredStream(int index)
		{
			try
			{
				return InternalOpenDeferredStream(index);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "opendeferredstream", true);
				throw;
			}
		}

		protected virtual bool InternalFindKey(object[] key)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool FindKey(object[] key)
		{
			try
			{
				return InternalFindKey(key);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "findkey", true);
				throw;
			}
		}

		protected virtual void InternalFindNearest(object[] key)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public void FindNearest(object[] key)
		{
			try
			{
				InternalFindNearest(key);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "findnearest", true);
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
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "getfilter", true);
				throw;
			}
		}
		
		protected virtual bool InternalSetFilter(string filter)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedSearchableCall);
		}
		
		public bool SetFilter(string filter)
		{
			try
			{
				return InternalSetFilter(filter);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "setfilter", true);
				throw;
			}
		}
		
		protected virtual void InternalInsert(string[] names, object[] values) 
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Insert(string[] names, object[] values) 
		{
			try
			{
				InternalInsert(names, values);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "insert", true);
				throw;
			}
		}

		protected virtual void InternalUpdate(string[] names, object[] values)
		{
			throw new ConnectionException(ConnectionException.Codes.UnsupportedUpdateableCall);
		}

		public void Update(string[] names, object[] values)
		{
			try
			{
				InternalUpdate(names, values);
			}
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "update", false);
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
			catch (Exception exception)
			{
				_command.Connection.WrapException(exception, "delete", false);
			}
		}
	}
}
