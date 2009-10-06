/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alphora.Dataphor
{
	/// <summary>
	/// Utility class to perform assembly name parsing.
	/// </summary>
	/// <remarks>
	/// The Silverlight Framework has prevented us from running
	/// Assembly.GetName() or using the AssemblyName constructor
	/// that would perform AssemblyName parsing, so this utility
	/// class performs those security critical tasks.
	/// </remarks>
	public static class AssemblyNameUtility
	{
		public static string GetName(string AFullName)
		{
			return AFullName.Split(',')[0];
		}
		
		public static Version GetVersion(string AFullName)
		{
			string[] LParts = AFullName.Split(',');
			for (int LIndex = 0; LIndex < LParts.Length; LIndex++)
				if (LParts[LIndex].Trim().StartsWith("Version"))
					return new Version(LParts[LIndex].Split('=')[1]);
					
			throw new ArgumentException("Invalid Assembly Name Format");
		}
	}
}
