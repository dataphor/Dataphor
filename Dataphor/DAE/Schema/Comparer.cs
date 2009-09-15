/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
	/// <summary>Performs schema comparisons.</summary>
	public class SchemaComparer
	{
		public SchemaComparer(DAE.Server.Engine AOldServer, DAE.Server.Engine ANewServer, string ALibraryName)
		{
			FOldServer = AOldServer;
			FNewServer = ANewServer;
			FLibraryName = (ALibraryName == null ? String.Empty : ALibraryName);
		}

		private DAE.Server.Engine FOldServer;
		private DAE.Server.Engine FNewServer;
		private string FLibraryName;
/*
		public void EmitCreateObject(EmissionContext AContext, Schema.Object AObject)
		{
			if (!AContext.EmittedObjects.Contains(AObject) && AContext.ShouldEmit(AObject))
			{
				
			}
		}

		public void EmitAlterObject(EmissionContext AContext, Schema.Object AOldObject, Schema.Object ANewObject)
		{
//			if (!AContext.EmittedObjects.Contains(AObject) && AContext.ShouldEmit(AObject))
//			{
//			}
		}

		public void EmitDropObject(EmissionContext AContext, Schema.Object AObject)
		{
			if (!AContext.EmittedObjects.Contains(AObject) && AContext.ShouldEmit(AObject))
			{
			}
		}
*/
		public Statement EmitChanges()
		{

/*		TODO: Finish schema comparison
			EmissionContext LContext = new EmissionContext(EmitMode.ForCopy, new Dictionary<?,?>(), FLibraryName, false, false, false, true);

			Objects LOldObjects = new Objects();
			LOldObjects.AddRange(FOldServer.Catalog.Objects);

			Schema.Object LOldObject;
			foreach (Schema.Object LNewObject in FNewServer.Catalog.Objects)
			{
				if (FOldServer.Catalog.Objects.ContainsName(LNewObject.Name))
				{
					LOldObject = FOldServer.Catalog.Objects[LNewObject.Name];
					EmitAlterObject(LContext, LOldObject, LNewObject);
					LOldObjects.Remove(LOldObject);
				}
				else
					EmitCreateObject(LContext, LNewObject);
			}

			foreach (Schema.Object LObject in LOldObjects)
				EmitDropObject(LContext, LObject);

			return LContext.Block;
  */
			return new EmptyStatement();
		}
	}
}
