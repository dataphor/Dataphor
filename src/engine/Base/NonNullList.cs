/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor
{
	/// <summary>
	/// Provides a generic list class that does not allow nulls.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class NonNullList<T> : ValidatingBaseList<T> where T : class
	{
		public NonNullList() : base() { }
		public NonNullList(int capacity) : base(capacity) { }

		protected override void Validate(T value)
		{
			if (value == null)
				throw new BaseException(BaseException.Codes.CannotAddNull);
			//base.Validate(AValue); // Base doesn't do anything, change this if that ever changes
		}
	}
}
