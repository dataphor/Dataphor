/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;

using Alphora.Dataphor.DAE.Server;


namespace Alphora.Dataphor.DAE.Service
{
	/// <summary>Summary description for ServerServices.</summary>
	public class ServerServices	: TypedList
	{
		public ServerService():  base (typeof(ServerService)){}

		public new ServerService this [int AIndex]
		{
			get{(ServerService)base [AIndex];}
			set{base [AIndex] = value;}
		}
			
	}
}
