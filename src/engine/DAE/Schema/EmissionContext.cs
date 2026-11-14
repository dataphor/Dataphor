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
			CatalogDeviceSession session,
			Catalog catalog,
			EmitMode mode, 
			ObjectList requestedObjects, 
			string libraryName, 
			bool includeSystem, 
			bool includeGenerated, 
			bool includeDependents, 
			bool includeObject
		) : base()
		{
			Session = session;
			Catalog = catalog;
			Mode = mode;
			RequestedObjects = requestedObjects;
			LibraryName = libraryName;
			IncludeSystem = includeSystem;
			IncludeGenerated = includeGenerated;
			IncludeDependents = includeDependents;
			IncludeObject = includeObject;
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
		
		public bool ShouldEmit(Schema.Object objectValue)
		{
			return
				(
					(
						(RequestedObjects.Count == 0) || 
						(RequestedObjects.Contains(objectValue.ID))
					) && 
					(IncludeSystem || !objectValue.IsSystem) &&
					(!(objectValue is DeviceObject) || (Mode != EmitMode.ForRemote)) &&
					(
						!(objectValue.IsGenerated && (objectValue.GeneratorID > 0)) // A sort will be generated, but will not have a generator and so must be emitted
							|| IncludeGenerated 
							|| (objectValue.IsSessionObject) 
							|| (objectValue.IsATObject && RequestedObjects.Contains(objectValue.ID))
					) // an AT object will be generated, but must be emitted if specifically requested
				);
		}
		
		public bool ShouldEmitDrop(Schema.Object objectValue)
		{
			return
				(
					(
						(objectValue is CatalogObject) && 
						(
							IncludeSystem || 
							((LibraryName == String.Empty) && ((objectValue.Library == null) || (objectValue.Library.Name == Engine.SystemLibraryName))) ||
							(LibraryName != Engine.SystemLibraryName)
						) ||
						(
							!(objectValue is CatalogObject) &&
							(IncludeSystem || !objectValue.IsSystem)
						)
					) &&
					(!objectValue.IsGenerated || IncludeGenerated || (objectValue.IsSessionObject) || (RequestedObjects.Count > 0)) &&
					(
						(objectValue is CatalogObject) ||
						(objectValue is Schema.Representation) ||
						(objectValue is Schema.Default) ||
						(objectValue is Schema.Special) ||
						(objectValue is Schema.Constraint)
					)
				);
		}
		
		public bool ShouldEmitWithLibrary(Schema.Object objectValue)
		{
			return 
			(
				(LibraryName == String.Empty) || 
				(
					(LibraryName != String.Empty) && 
					(objectValue.Library != null) &&
					Schema.Object.NamesEqual(objectValue.Library.Name, LibraryName)
				)
			);
		}
    }
}
