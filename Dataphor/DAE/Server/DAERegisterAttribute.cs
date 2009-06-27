/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Server
{
	[AttributeUsage(AttributeTargets.Assembly)]
	public class DAERegisterAttribute : Attribute
	{
		public DAERegisterAttribute(string ARegisterClassName)
		{
			FRegisterClassName = ARegisterClassName;
		}
		
		private string FRegisterClassName;
		public string RegisterClassName
		{
			get { return FRegisterClassName; }
			set { FRegisterClassName = value; }
		}
	}
}
