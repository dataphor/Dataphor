/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define NILPROPOGATION

namespace Alphora.Dataphor.DAE.Runtime.Instructions
{
	using System;
	using System.Linq;
	using System.Reflection;

	public class MethodInfoStatics
	{
		// Ambiguous to use GetMethod, even with types given.  Has overloads that return different things.
		public static readonly MethodInfo DecimalToDoubleConversion = typeof(Decimal).GetMethods()
			.Where(m =>
				m.Name == "op_Explicit"
					&& m.ReturnType == typeof(double)
					&& m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(decimal) })
			).Single();

		// Ambiguous to use GetMethod, even with types given.  Has overloads that return different things.
		public static readonly MethodInfo DecimalToInt64Conversion = typeof(Decimal).GetMethods()
			.Where(m =>
				m.Name == "op_Explicit"
					&& m.ReturnType == typeof(long)
					&& m.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(decimal) })
			).Single();
	}
}

