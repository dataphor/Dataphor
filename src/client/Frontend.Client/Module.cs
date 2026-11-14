/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;

namespace Alphora.Dataphor.Frontend.Client
{
	[DesignRoot()]
	[ListInDesigner(false)]
	public class Module : Node, IModule
	{
		public override bool IsValidChild(Type childType)
		{
			return true;
		}
	}
}