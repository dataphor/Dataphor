/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;

namespace Alphora.Dataphor
{
	public sealed class ThreadUtility
	{
		/// <summary> Sets a thread's name, for debugging. </summary>
		/// <remarks> This method is only called if the DEBUG conditional is set. A null name clears the name. Thread.Name can be assigned any number of times on .NET 10, so the old write-once workaround (reflection on a private field) is no longer needed. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void SetThreadName(Thread thread, string name)
		{
			thread.Name = name;
		}
	}
}
