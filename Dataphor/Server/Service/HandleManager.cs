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
				return LResult;
			}
		}
		
		private object GetObject(int AHandle)
		{
			lock (FHandleSync)
			{
				object LObject;
				if (!FHandles.TryGetValue(AHandle, out LObject))
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System, AHandle);
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
					throw new ServerException(ServerException.Codes.UnknownObjectHandle, ErrorSeverity.System, AHandle);
				FHandles.Remove(AHandle);
				return LObject;
			}
		}
		
		public T ReleaseObject<T>(int AHandle)
		{
			return (T)ReleaseObject(AHandle);
		}
	}
}
