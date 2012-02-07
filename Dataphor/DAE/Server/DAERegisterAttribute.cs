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
		public DAERegisterAttribute(string registerClassName)
		{
			_registerClassName = registerClassName;
		}
		
		private string _registerClassName;
		public string RegisterClassName
		{
			get { return _registerClassName; }
			set { _registerClassName = value; }
		}
	}
}
