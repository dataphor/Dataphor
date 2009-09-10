/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
    /// <remarks>Provides an interface to receive notification when an object has been dispose.</remarks>	
	public interface IDisposableNotify
	{
        /// <summary>Event to notify objects that this object has been disposed.</summary>
        event EventHandler Disposed;
	}
	
    /// <summary>Provides the base implementation for <see cref="IDisposable"/> and <see cref="IDisposableNotify"/>. </summary>
    /// <seealso cref="IDisposable"/>
	public abstract class Disposable : Object, IDisposable, IDisposableNotify
    {
		/// <summary> <c>Disposed</c> is invoked when this object is Disposed </summary>
		/// <seealso cref="IDisposableNotify"/>
		public event EventHandler Disposed;
		
		/// <summary> Disposes the object. </summary>
		/// <seealso cref="IDisposable"/>
		public void Dispose()
		{
			#if USEFINALIZER
			System.GC.SuppressFinalize(this);
			#endif
			Dispose(true);
		}

		/// <summary> Virtual dispose method allows descendents to perform cleanup. </summary>
		/// <remarks> Notifies other objects of the disposal. </remarks>
		/// <param name="ADisposing"> True if being called from Dispose() instead of finallizer. </param>
		protected virtual void Dispose(bool ADisposing)
		{
			if (Disposed != null)
				Disposed(this, EventArgs.Empty);
		}

//		/// <summary> Overridden to call dispose.  </summary>
//		/// <remarks> This shouldn't happen unless the Dispose call was mistakenly ommitted. </remarks>
		#if USEFINALIZER
		~Disposable()
		{
			#if THROWINFINALIZER
			throw new BaseException(BaseException.Codes.FinalizerInvoked);
			#else
			Dispose(false);
			#endif
		}
		#endif
	}

	public sealed class MathUtility
	{
		public static int IntegerCeilingDivide(int ADividend, int ADivisor)
		{
			return (ADividend / ADivisor) + ((ADividend % ADivisor) == 0 ? 0 : 1);
		}
	}

	/// <summary> ErrorList is a list of exceptions that can be thrown in aggregate. </summary>
	public class ErrorList : List<Exception>
	{
		public void Throw()
		{
			if (Count > 0)
				new AggregateException(this);
		}

		public override string ToString()
		{
			int LSize = 0;
			foreach (Exception LException in this)
			{
				if (LSize > 0)
					LSize += 4;	// CRLFCRLF
				LSize += LException.Message.Length;
			}
			System.Text.StringBuilder LResult = new System.Text.StringBuilder(LSize);
			foreach (Exception LException in this)
			{
				if (LResult.Length > 0)
					LResult.Append("\r\n\r\n");
				LResult.Append(LException.Message);
			}
			return LResult.ToString();
		}
	}
}
