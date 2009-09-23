/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Alphora.Dataphor.DAE.Schema
{
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;

	// TODO: Refactor these dependencies
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Server;

    public class EmissionContext : System.Object
    {
		public EmissionContext
		(
			CatalogDeviceSession ASession,
			Catalog ACatalog,
			EmitMode AMode, 
			ObjectList ARequestedObjects, 
			string ALibraryName, 
			bool AIncludeSystem, 
			bool AIncludeGenerated, 
			bool AIncludeDependents, 
			bool AIncludeObject
		) : base()
		{
			Session = ASession;
			Catalog = ACatalog;
			Mode = AMode;
			RequestedObjects = ARequestedObjects;
			LibraryName = ALibraryName;
			IncludeSystem = AIncludeSystem;
			IncludeGenerated = AIncludeGenerated;
			IncludeDependents = AIncludeDependents;
			IncludeObject = AIncludeObject;
		}

		public CatalogDeviceSession Session;		
		public Catalog Catalog;
		public EmitMode Mode;
		public ObjectList RequestedObjects;
		public string LibraryName;
		public bool IncludeSystem;
		public bool IncludeGenerated;
		public bool IncludeDependents;
		public bool IncludeObject;
		public Dictionary<int, Schema.Object> EmittedObjects = new Dictionary<int, Schema.Object>();
		public LoadedLibraries EmittedLibraries = new LoadedLibraries();
		public Block Block = new Block();
		
		public bool ShouldEmit(Schema.Object AObject)
		{
			return
				(
					(
						(RequestedObjects.Count == 0) || 
						(RequestedObjects.Contains(AObject.ID))
					) && 
					(IncludeSystem || !AObject.IsSystem) &&
					(!(AObject is DeviceObject) || (Mode != EmitMode.ForRemote)) &&
					(!AObject.IsGenerated || IncludeGenerated || (AObject.IsSessionObject) || (AObject.IsATObject && RequestedObjects.Contains(AObject.ID))) // an AT object will be generated, but must be emitted if specifically requested
				);
		}
		
		public bool ShouldEmitDrop(Schema.Object AObject)
		{
			return
				(
					(
						(AObject is CatalogObject) && 
						(
							IncludeSystem || 
							((LibraryName == String.Empty) && ((AObject.Library == null) || (AObject.Library.Name == Engine.CSystemLibraryName))) ||
							(LibraryName != Engine.CSystemLibraryName)
						) ||
						(
							!(AObject is CatalogObject) &&
							(IncludeSystem || !AObject.IsSystem)
						)
					) &&
					(!AObject.IsGenerated || IncludeGenerated || (AObject.IsSessionObject) || (RequestedObjects.Count > 0)) &&
					(
						(AObject is CatalogObject) ||
						(AObject is Schema.Representation) ||
						(AObject is Schema.Default) ||
						(AObject is Schema.Special) ||
						(AObject is Schema.Constraint)
					)
				);
		}
		
		public bool ShouldEmitWithLibrary(Schema.Object AObject)
		{
			return 
			(
				(LibraryName == String.Empty) || 
				(
					(LibraryName != String.Empty) && 
					(AObject.Library != null) &&
					Schema.Object.NamesEqual(AObject.Library.Name, LibraryName)
				)
			);
		}
    }
}
