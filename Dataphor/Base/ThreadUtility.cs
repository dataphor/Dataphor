/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using System.Reflection;

namespace Alphora.Dataphor
{
	public sealed class ThreadUtility
	{
		private static FieldInfo FThreadNameFieldInfo;
		private static FieldInfo GetThreadNameFieldInfo()
		{
			// This is not thread safe, but in the unlikely event that this is called concurrently, it will simply do an extra lookup
			if (FThreadNameFieldInfo == null)
				FThreadNameFieldInfo = typeof(System.Threading.Thread).GetField("m_Name", BindingFlags.NonPublic | BindingFlags.Instance);
			return FThreadNameFieldInfo;
		}

		/// <summary> HACK: This sets a thread's name working around the perf and write-once issues. </summary>
		/// <remarks> This method is only called if the DEBUG conditional is set.  The name argument is of type object so that it can be reset back to the initial null state. </remarks>
		[System.Diagnostics.Conditional("DEBUG")]
		public static void SetThreadName(Thread AThread, string AName)
		{
			#if !SILVERLIGHT
			GetThreadNameFieldInfo().SetValue(AThread, AName);
			#endif
		}
	}
}
