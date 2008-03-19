/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.CodeDom;
using System.ComponentModel;
using Alphora.Dataphor.DAE.Server;
using System.ComponentModel.Design.Serialization;

namespace Alphora.Dataphor.DAE.Client.Design
{
	/// <summary> Serializes and Deserializes the Active property last. </summary>
	public class ActiveLastSerializer : PropertyLastSerializer
	{
		public ActiveLastSerializer() : base()
		{
			PropertyName = "Active";
		}
	}
}
