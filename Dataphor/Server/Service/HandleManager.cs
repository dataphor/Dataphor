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
		private int FNextHandle = Int32.MaxValue / 2;

		private object FHandleSync = new object();

		private Dictionary<int, object> FHandles = new Dictionary<int, object>();
		
		private Dictionary<object, int> FHandleIndex = new Dictionary<object, int>(new ReferenceEqualityComparer());

		/// <summary>
		/// Gets a handle for the given object and registers it in the handle dictionary.
		/// </summary>
		/// <param name="AObject"></param>
		/// <returns></returns>
		public int GetHandle(object AObject)
		{
			lock (FHandleSync)
			{
				int LResult = FNextHandle;
				FNextHandle++;
				FHandles.Add(LResult, AObject);

				IDisposableNotify LDisposableNotify = AObject as IDisposableNotify;
				if (LDisposableNotify != null)
				{
					LDisposableNotify.Disposed += new EventHandler(ObjectDisposed);
					FHandleIndex.Add(LDisposableNotify, LResult);
				}

				return LResult;
			}
		}

		private void ObjectDisposed(object ASender, EventArgs AArgs)
		{
			((IDisposableNotify)ASender).Disposed -= new EventHandler(ObjectDisposed);

			lock (FHandleSync)
			{
				int LHandle;
				if (FHandleIndex.TryGetValue(ASender, out LHandle))
				{
					FHandles.Remove(LHandle);
					FHandleIndex.Remove(ASender);
				}
			}
		}
		
		private object GetObject(int AHandle)
		{
			lock (FHandleSync)
			{
				object LObject;
				if (!FHandles.TryGetValue(AHandle, out LObject))
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System);
				return LObject;
			}
		}
		
		public T GetObject<T>(int AHandle)
		{
			return (T)GetObject(AHandle);
		}
		
		private object ReleaseObject(int AHandle)
		{
			lock (FHandleSync)
			{
				object LObject;
				if (!FHandles.TryGetValue(AHandle, out LObject))
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System);
				FHandles.Remove(AHandle);

				IDisposableNotify LDisposableNotify = LObject as IDisposableNotify;
				if (LDisposableNotify != null)
				{
					LDisposableNotify.Disposed -= new EventHandler(ObjectDisposed);
					FHandleIndex.Remove(LDisposableNotify);
				}

				return LObject;
			}
		}
		
		public T ReleaseObject<T>(int AHandle)
		{
			return (T)ReleaseObject(AHandle);
		}
	}
}
