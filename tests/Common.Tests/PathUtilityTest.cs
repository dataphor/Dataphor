/*
	Dataphor
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;

using NUnit.Framework;

using Alphora.Dataphor.Windows;

namespace Alphora.Dataphor.Common.Tests
{
	[TestFixture]
	public class PathUtilityTest
	{
		/// <summary>
		/// The server resolves its default library folder, relative library folders and system library files through this rule:
		/// a relative name is in the folder that holds the host's own assemblies, and a rooted name is used as it is.
		/// </summary>
		[Test]
		public void GetFullFileName()
		{
			string assembly = typeof(PathUtility).Assembly.Location;
			Assert.That(PathUtility.GetFullFileName(Path.GetFileName(assembly)), Is.EqualTo(assembly).IgnoreCase);

			string rooted = Path.Combine(Path.GetTempPath(), "x.dll");
			Assert.That(PathUtility.GetFullFileName(rooted), Is.EqualTo(rooted));
		}
	}
}
