/*
	Dataphor
	© Copyright 2000-2016 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor.DAE.Runtime.Data
{
	public static class TypeExtensions
	{
		public static bool IsNullableType(this Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static Type TypeOrUnderlyingNullableType(this Type type)
		{
			return type.IsNullableType() ? type.GetGenericArguments()[0] : type;
		}
	}
}
