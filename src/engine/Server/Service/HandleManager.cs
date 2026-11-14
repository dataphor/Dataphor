/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Server;

namespace Alphora.Dataphor.DAE.Service
{
	public class HandleManager
	{
		private int _nextHandle = Int32.MaxValue / 2;

		private object _handleSync = new object();

		private Dictionary<int, object> _handles = new Dictionary<int, object>();
		
		private Dictionary<object, int> _handleIndex = new Dictionary<object, int>(new ReferenceEqualityComparer());

		/// <summary>
		/// Gets a handle for the given object and registers it in the handle dictionary.
		/// </summary>
		/// <param name="objectValue"></param>
		/// <returns></returns>
		public int GetHandle(object objectValue)
		{
			lock (_handleSync)
			{
				int result = _nextHandle;
				_nextHandle++;
				_handles.Add(result, objectValue);

				IDisposableNotify disposableNotify = objectValue as IDisposableNotify;
				if (disposableNotify != null)
				{
					disposableNotify.Disposed += new EventHandler(ObjectDisposed);
					_handleIndex.Add(disposableNotify, result);
				}

				return result;
			}
		}

		private void ObjectDisposed(object sender, EventArgs args)
		{
			((IDisposableNotify)sender).Disposed -= new EventHandler(ObjectDisposed);

			lock (_handleSync)
			{
				int handle;
				if (_handleIndex.TryGetValue(sender, out handle))
				{
					_handles.Remove(handle);
					_handleIndex.Remove(sender);
				}
			}
		}
		
		private object GetObject(int handle)
		{
			lock (_handleSync)
			{
				object objectValue;
				if (!_handles.TryGetValue(handle, out objectValue))
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System);
				return objectValue;
			}
		}
		
		public T GetObject<T>(int handle)
		{
			return (T)GetObject(handle);
		}
		
		private object ReleaseObject(int handle)
		{
			lock (_handleSync)
			{
				object objectValue;
				if (!_handles.TryGetValue(handle, out objectValue))
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System);
				_handles.Remove(handle);

				IDisposableNotify disposableNotify = objectValue as IDisposableNotify;
				if (disposableNotify != null)
				{
					disposableNotify.Disposed -= new EventHandler(ObjectDisposed);
					_handleIndex.Remove(disposableNotify);
				}

				return objectValue;
			}
		}
		
		public T ReleaseObject<T>(int handle)
		{
			return (T)ReleaseObject(handle);
		}
	}
}
