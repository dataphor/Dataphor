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

	/// <summary> Catalog </summary>
	public class Catalog : Objects
    {
		public Catalog() : base() 
		{
			_dataTypes = new DataTypes(this);
		}
		
		// Used by serializer
		public Objects Objects { get { return this; } }

		// Libraries
		private Libraries _libraries;
		public Libraries Libraries 
		{ 
			get 
			{
				if (_libraries == null)
					_libraries = new Libraries(); 
				return _libraries; 
			} 
		}
		
		// LoadedLibraries
		private LoadedLibraries _loadedLibraries;
		public LoadedLibraries LoadedLibraries 
		{ 
			get 
			{ 
				if (_loadedLibraries == null)
					_loadedLibraries = new LoadedLibraries();
				return _loadedLibraries; 
			} 
		}
		
		// ClassLoader
		private ClassLoader _classLoader;
		public ClassLoader ClassLoader
		{
			get
			{
				if (_classLoader == null)
					_classLoader = new ClassLoader();
				return _classLoader;
			}
		}
		
        // Operators
        private OperatorMaps _operatorMaps = new OperatorMaps();
        public OperatorMaps OperatorMaps { get { return _operatorMaps; } }
        
		// DataTypes
        private DataTypes _dataTypes;
        public DataTypes DataTypes { get { return _dataTypes; } }

		// TimeStamp
		protected long _timeStamp = 0;
		/// <summary>This time stamp is used to coordinate the catalog device cache tables with the actual system catalog.</summary>
		/// <remarks>
		/// This time stamp is incremented whenever any catalog changing event takes place.
		/// </remarks>
		public long TimeStamp { get { return _timeStamp; } }

		/// <summary>Updates the catalog device cache coordination timestamp for this catalog.</summary>		
		public void UpdateTimeStamp()
		{
			lock (this)
			{
				_timeStamp += 1;
			}
		}
		
		// CacheTimeStamp
		protected long _cacheTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate client-side catalog caches.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made to an existing object that is potentially stored in a client-side cache.
		/// For a more complete description of this property, refer to the CLI documentation.
		/// </remarks>
		public long CacheTimeStamp { get { return _cacheTimeStamp; } }
		
		/// <summary>Updates the client-side cache coordination timestamp for this catalog.</summary>
		public void UpdateCacheTimeStamp()
		{
			lock (this)
			{
				_cacheTimeStamp += 1;
			}
		}
		
		// PlanCacheTimeStamp
		protected long _planCacheTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate the server-side plan cache.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made to an existing object that is potentially referenced by a cached plan.
		/// </remarks>
		public long PlanCacheTimeStamp { get { return _planCacheTimeStamp; } }
		
		/// <summary>Updates the server-side plan cache coordination timestamp for this catalog.</summary>
		public void UpdatePlanCacheTimeStamp()
		{
			lock (this)
			{
				_planCacheTimeStamp += 1;
			}
		}

		// DerivationTimeStamp
		protected long _derivationTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate the derivation cache maintained by the Frontend server.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made that could affect a derived paged stored in the Frontend server derivation cache.
		/// For a more complete description of this property, refer to the CLI documentation.
		/// </remarks>
		public long DerivationTimeStamp { get { return _derivationTimeStamp; } }

		/// <summary>Updates the derivation cache coordination timestamp for this catalog.</summary>
		public void UpdateDerivationTimeStamp()
		{
			lock (this)
			{
				_derivationTimeStamp += 1;
			}
		}
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object item)
        {
            if 
                (
                    !(
						(item is ScalarType) ||
                        (item is Operator) ||
                        (item is Device) ||
                        (item is ServerLink) ||
                        (item is TableVar) ||
                        (item is Reference) || 
                        (item is CatalogConstraint) ||
                        (item is Role) ||
                        (item is Sort) ||
                        (item is EventHandler) ||
						(item is Conversion) ||
						(item is DeviceObject)
                    )
                )
            {
                throw new SchemaException(SchemaException.Codes.TopLevelContainer);
            }
            
            if ((item.Name == null) || (item.Name == String.Empty))
				throw new SchemaException(SchemaException.Codes.ObjectNameRequired);
				
			Error.AssertFail(item.ID > 0, "Object '{0}' ({1}) does not have an ID and cannot be tracked in a catalog.", item.Name, item.Description);
			
			if ((item is Operator) && (_operatorMaps.ContainsOperator((Operator)item)))
				throw new SchemaException(SchemaException.Codes.DuplicateOperator, item.ToString());
            base.Validate(item);
        }
        #endif

        protected override void Adding(Object item, int index)
        {
			base.Adding(item, index);
			if (item is Operator)
			{
				try
				{
					_operatorMaps.AddOperator((Operator)item);
				}
				catch
				{
					RemoveAt(index);
					throw;
				}
			}
        }
        
        protected override void Removing(Object item, int index)
        {
			if ((item is Operator) && (_operatorMaps.ContainsOperator((Operator)item)))
				_operatorMaps.RemoveOperator((Operator)item);
			base.Removing(item, index);
        }
        
        // Scalar Conversion Path Cache
        private ScalarConversionPathCache _conversionPathCache;
        public ScalarConversionPathCache ConversionPathCache 
        {
			get 
			{
				if (_conversionPathCache == null)
					_conversionPathCache = new ScalarConversionPathCache();
				return _conversionPathCache; 
			} 
		}
		
		// Operator Resolution Cache
		private OperatorResolutionCache _operatorResolutionCache;
		public OperatorResolutionCache OperatorResolutionCache
		{
			get
			{
				if (_operatorResolutionCache == null)
					_operatorResolutionCache = new OperatorResolutionCache();
				return _operatorResolutionCache;
			}
		}
		
        public string ShowMap(string operatorName)
        {
			return _operatorMaps[_operatorMaps.IndexOf(operatorName)].ShowMap();
        }
        
        public string ShowMaps()
        {
			return _operatorMaps.ShowMaps();
        }
        
        public void IncludeDependencies(CatalogDeviceSession session, Schema.Catalog sourceCatalog, Schema.Object objectValue, EmitMode mode)
        {
			objectValue.IncludeDependencies(session, sourceCatalog, this, mode);
			foreach (Object localObjectValue in this)
				localObjectValue.IncludeHandlers(session, sourceCatalog, this, mode);
        }
        
        public void IncludeDependencies(CatalogDeviceSession session, Schema.Catalog sourceCatalog, Schema.IDataType dataType, EmitMode mode)
        {
			dataType.IncludeDependencies(session, sourceCatalog, this, mode);
        }

        // Catalog Emission
        private void EmitDependencies(EmissionContext context, Object objectValue)
        {
			if (objectValue.HasDependencies())
				for (int index = 0; index < objectValue.Dependencies.Count; index++)
					EmitObject(context, objectValue.Dependencies.ResolveObject(context.Session, index));
        }
        
        private void EmitTableVarChildDependencies(EmissionContext context, TableVar tableVar)
        {
			foreach (TableVarColumn column in tableVar.Columns)
			{
				if ((column.Default != null) && column.Default.IsRemotable)
					EmitDependencies(context, column.Default);
						
				foreach (Constraint constraint in column.Constraints)
					EmitDependencies(context, constraint);
			}

			if (tableVar.HasConstraints())
				foreach (Constraint constraint in tableVar.Constraints)
					if ((constraint.ConstraintType == ConstraintType.Row) && ((context.Mode != EmitMode.ForRemote) || constraint.IsRemotable))
						EmitDependencies(context, constraint);

			foreach (Order order in tableVar.Orders)
			{
				OrderColumn orderColumn;
				for (int index = 0; index < order.Columns.Count; index++)
				{
					orderColumn = order.Columns[index];
					if ((orderColumn.Sort != null) && ((context.Mode != EmitMode.ForRemote) || orderColumn.Sort.IsRemotable))
						EmitDependencies(context, orderColumn.Sort);
				}
			}
        }
        
        private void EmitChildDependencies(EmissionContext context, Object objectValue)
        {
			if (objectValue is ScalarType)
			{
				// Emit Dependencies of representations
				ScalarType scalarType = (ScalarType)objectValue;
				
				foreach (Representation representation in scalarType.Representations)
				{
					if 
					(
						(!representation.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && 
							!representation.HasExternalDependencies()
					)
					{
						EmitDependencies(context, representation);
						
						if (representation.Selector != null && representation.Selector.HasDependencies())
							for (int index = 0; index < representation.Selector.Dependencies.Count; index++)
								if (representation.Selector.Dependencies.IDs[index] != objectValue.ID)
									EmitObject(context, representation.Selector.Dependencies.ResolveObject(context.Session, index));
						
						foreach (Property property in representation.Properties)
						{
							EmitDependencies(context, property);
							if (property.ReadAccessor != null && property.ReadAccessor.HasDependencies())
								for (int index = 0; index < property.ReadAccessor.Dependencies.Count; index++)
									if (property.ReadAccessor.Dependencies.IDs[index] != objectValue.ID)
										EmitObject(context, property.ReadAccessor.Dependencies.ResolveObject(context.Session, index));
							if (property.WriteAccessor != null && property.WriteAccessor.HasDependencies())
								for (int index = 0; index < property.WriteAccessor.Dependencies.Count; index++)
									if (property.WriteAccessor.Dependencies.IDs[index] != objectValue.ID)
										EmitObject(context, property.WriteAccessor.Dependencies.ResolveObject(context.Session, index));
						}
					}
				}
			}
			else if (objectValue is TableVar)
			{
				EmitTableVarChildDependencies(context, (TableVar)objectValue);
			}
        }
        
        private void EmitChildren(EmissionContext context, Object objectValue)
        {
			if (context.Mode != EmitMode.ForStorage)
			{
				ScalarType scalarType = objectValue as ScalarType;
				if (scalarType != null)
				{
					// Emit the default, constraints, non-system representations, and specials for this scalar type
					foreach (Schema.Representation representation in scalarType.Representations)
					{
						if 
						(
							(!representation.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && 
								representation.HasExternalDependencies()
						)
						{
							EmitDependencies(context, representation);
							foreach (Property property in representation.Properties)
								EmitDependencies(context, property);
							if (!context.EmittedObjects.ContainsKey(representation.ID))
							{
								context.Block.Statements.Add(representation.EmitStatement(context.Mode));
								context.EmittedObjects.Add(representation.ID, representation);
							}
						}
					}
					
					EmitDependencies(context, scalarType.IsSpecialOperator);
					foreach (Schema.Special special in scalarType.Specials)
					{
						if ((!special.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && context.ShouldEmitWithLibrary(special))
						{
							EmitDependencies(context, special);
							EmitDependencies(context, special.Selector);
							EmitDependencies(context, special.Comparer);
							if (!context.EmittedObjects.ContainsKey(special.ID))
							{
								context.Block.Statements.Add(special.EmitStatement(context.Mode));
								context.EmittedObjects.Add(special.ID, special);
							}
						}
					}
					
					if 
					(
						(scalarType.Default != null) && 
						((context.Mode != EmitMode.ForRemote) || scalarType.Default.IsRemotable) && 
						(!scalarType.Default.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && 
						context.ShouldEmitWithLibrary(scalarType.Default)
					)
					{
						EmitDependencies(context, scalarType.Default);
						if (!context.EmittedObjects.ContainsKey(scalarType.Default.ID))
						{
							context.Block.Statements.Add(scalarType.Default.EmitStatement(context.Mode));
							context.EmittedObjects.Add(scalarType.Default.ID, scalarType.Default);
						}
					}

					foreach (Constraint constraint in scalarType.Constraints)
						if 
						(
							((context.Mode != EmitMode.ForRemote) || constraint.IsRemotable) && 
							(!constraint.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && 
							context.ShouldEmitWithLibrary(constraint)
						)
						{
							EmitDependencies(context, constraint);
							if (!context.EmittedObjects.ContainsKey(constraint.ID))
							{
								context.Block.Statements.Add(constraint.EmitStatement(context.Mode));
								context.EmittedObjects.Add(constraint.ID, constraint);
							}
						}
				}
				
				TableVar tableVar = objectValue as TableVar;
				if (tableVar != null)
				{
					foreach (TableVarColumn column in tableVar.Columns)
					{
						if 
						(
							(objectValue is BaseTableVar) && 
							(column.Default != null) && 
							!column.Default.IsRemotable && 
							(context.Mode != EmitMode.ForRemote) && 
							(!column.Default.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && 
							context.ShouldEmitWithLibrary(column.Default)
						)
						{
							EmitDependencies(context, column.Default);
							if (!context.EmittedObjects.ContainsKey(column.Default.ID))
							{
								context.Block.Statements.Add(column.Default.EmitStatement(context.Mode));
								context.EmittedObjects.Add(column.Default.ID, column.Default);
							}
						}
					}

					if (tableVar.HasConstraints())
						foreach (Constraint constraint in tableVar.Constraints)
							if ((!constraint.IsGenerated || context.IncludeGenerated || (context.Mode == EmitMode.ForStorage)) && (constraint.ConstraintType == ConstraintType.Database) && ((context.Mode != EmitMode.ForRemote) || constraint.IsRemotable) && context.ShouldEmitWithLibrary(constraint))
							{
								EmitDependencies(context, constraint);
								if (!context.EmittedObjects.ContainsKey(constraint.ID))
								{
									context.Block.Statements.Add(constraint.EmitStatement(context.Mode));
									context.EmittedObjects.Add(constraint.ID, constraint);
								}
							}
				}				
			}
        }

        private void EmitObject(EmissionContext context, Object objectValue)
        {
			if (context.ShouldEmit(objectValue))
			{
				if (context.ShouldEmitWithLibrary(objectValue))
				{
					if (!context.EmittedObjects.ContainsKey(objectValue.ID))
					{
						EmitDependencies(context, objectValue);
						EmitChildDependencies(context, objectValue);
					}
					
					if (!context.EmittedObjects.ContainsKey(objectValue.ID))
					{
						context.Block.Statements.Add(objectValue.EmitStatement(context.Mode));
						context.EmittedObjects.Add(objectValue.ID, objectValue);
						EmitChildren(context, objectValue);
					}
				}
				else
				{
					if (!context.EmittedObjects.ContainsKey(objectValue.ID))
					{
						context.EmittedObjects.Add(objectValue.ID, objectValue);
						EmitChildren(context, objectValue);
					}
				}
			}
        }
        
        public void EmitLibrary(EmissionContext context, LoadedLibrary library)
        {
			if (!context.EmittedLibraries.Contains(library.Name))
			{
				foreach (LoadedLibrary localLibrary in library.RequiredLibraries)
					EmitLibrary(context, localLibrary);

				if (library.Name != Engine.SystemLibraryName)
				{
					ExpressionStatement statement = new ExpressionStatement();
					statement.Expression = new CallExpression("LoadLibrary", new Expression[]{new ValueExpression(library.Name)});
					context.Block.Statements.Add(statement);
				}
				context.EmittedLibraries.Add(library);
			}
        }
        
        public void EmitLibraries(EmissionContext context, Schema.Objects<LoadedLibrary> libraries)
        {
			foreach (LoadedLibrary library in libraries)
				EmitLibrary(context, library);
        }
        
        private void GatherDependents(CatalogDeviceSession session, Schema.Object objectValue, ObjectList dependents)
        {
			List<Schema.DependentObjectHeader> headers = session.SelectObjectDependents(objectValue.ID, true);
			for (int index = 0; index < headers.Count; index++)
				if (!dependents.Contains(headers[index].ID))
				{
					Schema.Object localObjectValue = session.ResolveObject(headers[index].ID);
					dependents.Add(localObjectValue.ID, localObjectValue);
				}
        }
        
		/// <summary>Emits a statement to reconstruct the catalog based on the given parameters.</summary>
		/// <param name="mode">Specifies the mode for statement emission.</param>
		/// <param name="requestedObjectNames">Specifies a list of object names to be serialized.  If this list is empty, the entire catalog is emitted.</param>
		/// <param name="libraryName">Specifies the name of the library to be emitted.  If this is the empty string, the system library will be emitted.</param>
		/// <param name="includeSystem">Specifies whether system objects should be included in the emitted catalog.</param>
		/// <param name="includeDependents">Specifies whether the dependents of the objects should be included in the emitted catalog.</param>
		/// <param name="includeObject">Specifies whether the object itself should be included in the emitted catalog.</param>
		/// <remarks>
		///	This is the main EmitStatement overload which all other EmitStatement overloads call.
		/// </remarks>
        public Statement EmitStatement
        (
			CatalogDeviceSession session,
			EmitMode mode, 
			string[] requestedObjectNames, 
			string libraryName, 
			bool includeSystem, 
			bool includeGenerated, 
			bool includeDependents, 
			bool includeObject
		)
        {
			ObjectList requestedObjects = new ObjectList();
			
			for (int index = 0; index < requestedObjectNames.Length; index++)
			{
				int objectIndex = IndexOf(requestedObjectNames[index]);
				if (objectIndex >= 0)
				{
					Schema.Object objectValue = this[objectIndex];
					if ((includeObject) && !requestedObjects.Contains(objectValue.ID))
						requestedObjects.Add(objectValue.ID, objectValue);
					if (includeDependents)
						GatherDependents(session, objectValue, requestedObjects);
				}
			}
			
			EmissionContext context = new EmissionContext(session, this, mode, requestedObjects, libraryName, includeSystem, includeGenerated, includeDependents, includeObject);
			if (requestedObjects.Count == 0)
			{
				if (libraryName == String.Empty)
					EmitLibraries(context, _loadedLibraries);
			}

			if (requestedObjects.Count > 0)
			{
				for (int index = 0; index < requestedObjects.Count; index++)
					EmitObject(context, requestedObjects.ResolveObject(session, index));
			}
			else
			{
				foreach (CatalogObjectHeader header in session.SelectLibraryCatalogObjects(libraryName))
					EmitObject(context, session.ResolveCatalogObject(header.ID));
			}

			return context.Block;
        }
        
		/// <summary>Emits a statement to reconstruct the entire catalog.</summary>
        public Statement EmitStatement(CatalogDeviceSession session, EmitMode mode, bool includeSystem)
        {
			return EmitStatement(session, mode, new string[0], String.Empty, includeSystem, false, false, true);
        }

		/// <summary>Emits a statement to reconstruct the catalog for the given library.</summary>        
        public Statement EmitStatement(CatalogDeviceSession session, EmitMode mode, string libraryName, bool includeSystem)
        {
			return EmitStatement(session, mode, new string[0], libraryName, includeSystem, false, false, true);
        }

		/// <summary>Emits a statement to reconstruct the specified list of catalog objects.</summary>        
        public Statement EmitStatement(CatalogDeviceSession session, EmitMode mode, string[] requestedObjectNames)
        {
			return EmitStatement(session, mode, requestedObjectNames, String.Empty, true, false, false, true);
        }
        
        protected void ReportDroppedObject(EmissionContext context, Schema.Object objectValue)
        {
			if (!context.EmittedObjects.ContainsKey(objectValue.ID))
				context.EmittedObjects.Add(objectValue.ID, objectValue);
        }
        
        protected void ReportDroppedObjects(EmissionContext context, Schema.Object objectValue)
        {
			if (objectValue is ScalarType)
			{
				ScalarType scalarType = (ScalarType)objectValue;
				if (scalarType.Default != null)
					ReportDroppedObject(context, scalarType.Default);
					
				foreach (Constraint constraint in scalarType.Constraints)
					ReportDroppedObject(context, constraint);
					
				foreach (Special special in scalarType.Specials)
					ReportDroppedObject(context, special);
					
				if (scalarType.EqualityOperator != null)
					ReportDroppedObject(context, scalarType.EqualityOperator);
					
				if (scalarType.ComparisonOperator != null)
					ReportDroppedObject(context, scalarType.ComparisonOperator);
					
				foreach (Schema.Representation representation in scalarType.Representations)
					ReportDroppedObjects(context, representation);
			}
			else if (objectValue is Representation)
			{
				Schema.Representation representation = (Schema.Representation)objectValue;

				foreach (Schema.Property property in representation.Properties)
				{
					if (property.ReadAccessor != null)
						ReportDroppedObject(context, property.ReadAccessor);
					
					if (property.WriteAccessor != null)
						ReportDroppedObject(context, property.WriteAccessor);
				}
				
				if (representation.Selector != null)
					ReportDroppedObject(context, representation.Selector);
			}
			else if (objectValue is TableVarColumn)
			{
				TableVarColumn tableVarColumn = (TableVarColumn)objectValue;
				if (tableVarColumn.Default != null)
					ReportDroppedObject(context, tableVarColumn.Default);
					
				foreach (Constraint constraint in tableVarColumn.Constraints)
					ReportDroppedObject(context, constraint);
			}
			else if (objectValue is TableVar)
			{	
				TableVar tableVar = (TableVar)objectValue;
				foreach (TableVarColumn column in tableVar.Columns)
					ReportDroppedObjects(context, column);
					
				if (tableVar.HasConstraints())
					foreach (Constraint constraint in tableVar.Constraints)
						ReportDroppedObject(context, constraint);
			}
			else if (objectValue is Reference)
			{
				Reference reference = (Reference)objectValue;
				if (reference.SourceConstraint != null)
					ReportDroppedObject(context, reference.SourceConstraint);
				if (reference.TargetConstraint != null)
					ReportDroppedObject(context, reference.TargetConstraint);
			}
			else if (objectValue is DeviceScalarType)
			{
				Schema.CatalogObjectHeaders headers = context.Session.SelectGeneratedObjects(objectValue.ID);
				for (int index = 0; index < headers.Count; index++)
					ReportDroppedObject(context, context.Session.ResolveCatalogObject(headers[index].ID));
			}
        }
        
        public void EmitDropObject(EmissionContext context, Schema.Object objectValue)
        {
			if (context.ShouldEmitDrop(objectValue))
			{
				if (context.IncludeObject || !context.RequestedObjects.Contains(objectValue.ID))
				{
					// Should be a safe drop if the object is an AT or session object
					Statement statement = objectValue.EmitDropStatement(context.Mode);
					if (((objectValue.IsATObject || objectValue.IsSessionObject)) && ((objectValue is Schema.TableVar) || (objectValue is Schema.Operator)))
						statement = 
							new IfStatement
							(
								new CallExpression
								(
									".System.ObjectExists", 
									new Expression[]{new CallExpression(".System.Name", new Expression[]{new ValueExpression(Schema.Object.EnsureRooted(objectValue.Name), TokenType.String)})}
								),
								new ExpressionStatement
								(
									new CallExpression
									(
										".System.Execute",
										new Expression[]{new ValueExpression(new D4TextEmitter().Emit(statement), TokenType.String)}
									)
								),
								null
							);
						
					if (objectValue.IsATObject)
					{
						Schema.TableVar tableVar = null;
						if (objectValue is Schema.TableVarColumnDefault)
							tableVar = ((Schema.TableVarColumnDefault)objectValue).TableVarColumn.TableVar;
							
						if (objectValue is Schema.TableVarColumnConstraint)
							tableVar = ((Schema.TableVarColumnConstraint)objectValue).TableVarColumn.TableVar;
							
						if (objectValue is Schema.TableVarConstraint)
							tableVar = ((Schema.TableVarConstraint)objectValue).TableVar;
							
						if (objectValue is Schema.TableVarEventHandler)
							tableVar = ((Schema.TableVarEventHandler)objectValue).TableVar;
							
						if (objectValue is Schema.TableVarColumnEventHandler)
							tableVar = ((Schema.TableVarColumnEventHandler)objectValue).TableVarColumn.TableVar;
							
						if (objectValue.IsATObject && (tableVar != null))
							statement = 
								new IfStatement
								(
									new CallExpression
									(
										".System.ObjectExists", 
										new Expression[]{new CallExpression(".System.Name", new Expression[]{new ValueExpression(Schema.Object.EnsureRooted(tableVar.Name), TokenType.String)})}
									), 
									new ExpressionStatement
									(
										new CallExpression
										(
											".System.Execute",
											new Expression[]{new ValueExpression(new D4TextEmitter().Emit(statement), TokenType.String)}
										)
									),
									null
								);
					}

					context.Block.Statements.Add(statement);
				}
			}
        }
        
        public void EmitUnregisterLibrary(EmissionContext context, LoadedLibrary library)
        {
			if (!context.EmittedLibraries.Contains(library) && !library.IsSystem)
			{
				// Unregister all requiredby libraries
				foreach (LoadedLibrary localLibrary in library.RequiredByLibraries)
					EmitUnregisterLibrary(context, localLibrary);
	
				// Unregister the library				
				context.Block.Statements.Add(new ExpressionStatement(new CallExpression("UnregisterLibrary", new Expression[]{new ValueExpression(library.Name)})));
				context.EmittedLibraries.Add(library);
			}
        }
        
        public Statement EmitDropStatement
        (
			CatalogDeviceSession session,
			string[] requestedObjectNames, 
			string libraryName, 
			bool includeSystem, 
			bool includeGenerated, 
			bool includeDependents, 
			bool includeObject
		)
        {
			ObjectList requestedObjects = new ObjectList();
			foreach (string objectName in requestedObjectNames)
			{
				int objectIndex = IndexOf(objectName);
				if (objectIndex >= 0)
					requestedObjects.Add(this[objectIndex].ID, this[objectIndex]);
				else
				{
					Schema.CatalogObject objectValue = session.ResolveName(objectName, session.ServerProcess.ServerSession.NameResolutionPath, new List<string>());
					if (objectValue != null)
						requestedObjects.Add(objectValue.ID, objectValue);
				}
			}
			
			EmissionContext context = new EmissionContext(session, this, EmitMode.ForCopy, requestedObjects, libraryName, includeSystem, includeGenerated, includeDependents, includeObject);
			
			ObjectList dropList = ((requestedObjectNames.Length > 0) && (requestedObjects.Count == 0)) ? new ObjectList() : BuildDropList(context);
			
			for (int index = 0; index < dropList.Count; index++)
				EmitDropObject(context, dropList.ResolveObject(session, index));
			
			if (requestedObjectNames.Length == 0)
			{
				if (libraryName == String.Empty)
				{			
					// UnregisterLibraries
					foreach (LoadedLibrary library in _loadedLibraries)
						EmitUnregisterLibrary(context, library);
				}
			}
			
			return context.Block;
        }

		private ObjectList BuildDropList(EmissionContext LContext)
		{
			ObjectList dropList = new ObjectList();
			
			if (LContext.RequestedObjects.Count == 0) 
			{
				foreach (CatalogObjectHeader header in LContext.Session.SelectLibraryCatalogObjects(LContext.LibraryName))
					BuildObjectDropList(LContext, dropList, LContext.Session.ResolveCatalogObject(header.ID));
			}
			else
			{
				for (int index = 0; index < LContext.RequestedObjects.Count; index++)
					BuildObjectDropList(LContext, dropList, LContext.RequestedObjects.ResolveObject(LContext.Session, index));
			}

			return dropList;
		}

		private void BuildObjectDropList(EmissionContext context, ObjectList dropList, Schema.Object objectValue)
		{
			if (!context.EmittedObjects.ContainsKey(objectValue.ID))
			{
				context.EmittedObjects.Add(objectValue.ID, objectValue);

				// Add each dependent
				if (context.IncludeDependents)
					BuildDependentDropList(context, dropList, objectValue);
				
				dropList.Ensure(objectValue);
				
				ReportDroppedObjects(context, objectValue);
			}
		}
		
		private void BuildDependentDropList(EmissionContext context, ObjectList dropList, Schema.Object objectValue)
		{
			List<Schema.DependentObjectHeader> headers = context.Session.SelectObjectDependents(objectValue.ID, false);
			for (int index = 0; index < headers.Count; index++)
			{
				Schema.Object localObjectValue = context.Session.ResolveObject(headers[index].ID);
				if ((localObjectValue is Schema.Representation) && localObjectValue.IsGenerated)
					BuildObjectDropList(context, dropList, ((Schema.Representation)localObjectValue).ScalarType);
				else if (localObjectValue is Schema.Property)
				{
					if (((Schema.Property)localObjectValue).Representation.IsGenerated)
						BuildObjectDropList(context, dropList, ((Schema.Property)localObjectValue).Representation.ScalarType);
					else
						BuildObjectDropList(context, dropList, ((Schema.Property)localObjectValue).Representation);
				}
				else if ((headers[index].GeneratorObjectID >= 0) && !context.IncludeGenerated)
				{
					Schema.Object generatorObject = context.Session.ResolveObject(headers[index].GeneratorObjectID);
					if ((generatorObject is Schema.Representation) && generatorObject.IsGenerated)
						BuildObjectDropList(context, dropList, ((Schema.Representation)generatorObject).ScalarType);
					else if (generatorObject is Schema.Property)
					{
						if (((Schema.Property)generatorObject).Representation.IsGenerated)
							BuildObjectDropList(context, dropList, ((Schema.Property)generatorObject).Representation.ScalarType);
						else
							BuildObjectDropList(context, dropList, ((Schema.Property)generatorObject).Representation);
					}
					else
						BuildObjectDropList(context, dropList, generatorObject);
				}
				else
					BuildObjectDropList(context, dropList, localObjectValue);
			}
				
			// Drop child object depenendents
			BuildChildDependentDropList(context, dropList, objectValue);
		}
		
		private void BuildChildDependentDropList(EmissionContext context, ObjectList dropList, Schema.Object objectValue)
		{
			if (objectValue is Schema.ScalarType)
			{
				ScalarType scalarType = (ScalarType)objectValue;
				
				if (scalarType.Default != null)
					BuildDependentDropList(context, dropList, scalarType.Default);
					
				foreach (Constraint constraint in scalarType.Constraints)
					BuildDependentDropList(context, dropList, constraint);
					
				foreach (Special special in scalarType.Specials)
					BuildDependentDropList(context, dropList, special);
					
				if (scalarType.EqualityOperator != null)
					BuildDependentDropList(context, dropList, scalarType.EqualityOperator);
					
				if (scalarType.ComparisonOperator != null)
					BuildDependentDropList(context, dropList, scalarType.ComparisonOperator);
					
				foreach (Representation representation in scalarType.Representations)
				{
					BuildDependentDropList(context, dropList, representation);
					foreach (Property property in representation.Properties)
					{
						BuildDependentDropList(context, dropList, property);
						if (property.ReadAccessor != null)
							BuildDependentDropList(context, dropList, property.ReadAccessor);
							
						if (property.WriteAccessor != null)
							BuildDependentDropList(context, dropList, property.WriteAccessor);
					}
					
					if (representation.Selector != null)
						BuildDependentDropList(context, dropList, representation.Selector);
				}
			}
			else if (objectValue is TableVarColumn)
			{
				TableVarColumn tableVarColumn = (TableVarColumn)objectValue;
				if (tableVarColumn.Default != null)
					BuildDependentDropList(context, dropList, tableVarColumn.Default);
					
				foreach (Constraint constraint in tableVarColumn.Constraints)
					BuildDependentDropList(context, dropList, constraint);
			}
			else if (objectValue is TableVar)
			{
				TableVar tableVar = (TableVar)objectValue;
				
				foreach (TableVarColumn column in tableVar.Columns)
					BuildDependentDropList(context, dropList, column);
					
				if (tableVar.HasConstraints())
					foreach (Constraint constraint in tableVar.Constraints)
						if (constraint.ConstraintType == ConstraintType.Database)
							BuildDependentDropList(context, dropList, constraint);
						else
							BuildDependentDropList(context, dropList, constraint);
			}
		}
        
        public Statement EmitDropStatement(CatalogDeviceSession session, string[] requestedObjectNames, string libraryName)
        {
			return EmitDropStatement(session, requestedObjectNames, libraryName, false, false, true, true);
        }
        
        public Statement EmitDropStatement(CatalogDeviceSession session, string libraryName)
        {
			return EmitDropStatement(session, new string[]{}, libraryName, false, false, true, true);
        }
        
        public Statement EmitDropStatement(CatalogDeviceSession session)
        {
			return EmitDropStatement(session, new string[]{}, String.Empty, false, false, true, true);
        }
	}
    
    public delegate void CatalogLookupFailedEvent(Schema.Catalog ACatalog, string AName);
    
    public class DataTypes : System.Object
    {
		public DataTypes(Catalog catalog) : base()
		{
			_catalog = catalog;
		}
		
		[Reference]
		private Catalog _catalog;
		
		public event CatalogLookupFailedEvent OnCatalogLookupFailed;
		protected void DoCatalogLookupFailed(string name)
		{
			if (OnCatalogLookupFailed != null)
				OnCatalogLookupFailed(_catalog, name);
		}
		
		protected Object CatalogLookup(string name)
		{
			int index = _catalog.IndexOf(name);
			if (index < 0)
			{
				DoCatalogLookupFailed(name);
				return _catalog[name];
			}
			return _catalog[index];
		}

		// do not localize
		public const string SystemGenericName = "System.Generic";
		#if USETYPEINHERITANCE
		public const string CSystemAlpha = "System.Alpha";
		public const string CSystemOmega = "System.Omega";
		#endif
		public const string SystemScalarName = "System.Scalar";
		public const string SystemBooleanName = "System.Boolean";
		public const string SystemDecimalName = "System.Decimal";
		public const string SystemLongName = "System.Long";
		public const string SystemIntegerName = "System.Integer";
		public const string SystemShortName = "System.Short";
		public const string SystemByteName = "System.Byte";
		public const string SystemStringName = "System.String";
		#if USEISTRING
		public const string SystemIStringName = "System.IString";
		#endif
		public const string SystemMoneyName = "System.Money";
		public const string SystemGuidName = "System.Guid";
		public const string SystemTimeSpanName = "System.TimeSpan";
		public const string SystemDateTimeName = "System.DateTime";
		public const string SystemDateName = "System.Date";
		public const string SystemTimeName = "System.Time";
		public const string SystemBinaryName = "System.Binary";
		public const string SystemGraphicName = "System.Graphic";
		public const string SystemErrorName = "System.Error";
		public const string SystemNameName = "System.Name";

		#if USETYPEINHERITANCE
		private ScalarType FSystemAlpha;
		public ScalarType SystemAlpha
		{
			get
			{
				if (FSystemAlpha == null)
					FSystemAlpha = (ScalarType)CatalogLookup(CSystemAlpha);
				return FSystemAlpha;
			}
		}
		
		private ScalarType FSystemOmega;
		public ScalarType SystemOmega
		{
			get
			{
				if (FSystemOmega == null)
					FSystemOmega = (ScalarType)CatalogLookup(CSystemOmega);
				return FSystemOmega;
			}
		}
		#endif
		
		[Reference]
		private ScalarType _systemScalar;
		public ScalarType SystemScalar
		{
			get
			{
				if (_systemScalar == null)
					_systemScalar = (ScalarType)CatalogLookup(SystemScalarName);
				return _systemScalar;
			}
			set { _systemScalar = value; }
		}
		
		[Reference]
		private ScalarType _systemBoolean;
		public ScalarType SystemBoolean
		{
			get
			{
				if (_systemBoolean == null)
					_systemBoolean = (ScalarType)CatalogLookup(SystemBooleanName);
				return _systemBoolean;
			}
			set { _systemBoolean = value; }
		}
		
		[Reference]
		private ScalarType _systemDecimal;
		public ScalarType SystemDecimal
		{
			get
			{
				if (_systemDecimal == null)
					_systemDecimal = (ScalarType)CatalogLookup(SystemDecimalName);
				return _systemDecimal;
			}
			set { _systemDecimal = value; }
		}
		
		[Reference]
		private ScalarType _systemLong;
		public ScalarType SystemLong
		{
			get
			{
				if (_systemLong == null)
					_systemLong = (ScalarType)CatalogLookup(SystemLongName);
				return _systemLong;
			}
			set { _systemLong = value; }
		}
		
		[Reference]
		private ScalarType _systemInteger;
		public ScalarType SystemInteger
		{
			get
			{
				if (_systemInteger == null)
					_systemInteger = (ScalarType)CatalogLookup(SystemIntegerName);
				return _systemInteger;
			}
			set { _systemInteger = value; }
		}
		
		[Reference]
		private ScalarType _systemShort;
		public ScalarType SystemShort
		{
			get
			{
				if (_systemShort == null)
					_systemShort = (ScalarType)CatalogLookup(SystemShortName);
				return _systemShort;
			}
			set { _systemShort = value; }
		}
		
		[Reference]
		private ScalarType _systemByte;
		public ScalarType SystemByte
		{
			get
			{
				if (_systemByte == null)
					_systemByte = (ScalarType)CatalogLookup(SystemByteName);
				return _systemByte;
			}
			set { _systemByte = value; }
		}
		
		[Reference]
		private ScalarType _systemString;
		public ScalarType SystemString
		{
			get
			{
				if (_systemString == null)
					_systemString = (ScalarType)CatalogLookup(SystemStringName);
				return _systemString;
			}
			set { _systemString = value; }
		}
		
		#if USEISTRING
		[Reference]
		private ScalarType FSystemIString;
		public ScalarType SystemIString
		{
			get
			{
				if (FSystemIString == null)
					FSystemIString = (ScalarType)CatalogLookup(CSystemIString);
				return FSystemIString;
			}
		}
		#endif
		
		[Reference]
		private ScalarType _systemMoney;
		public ScalarType SystemMoney
		{
			get
			{
				if (_systemMoney == null)
					_systemMoney = (ScalarType)CatalogLookup(SystemMoneyName);
				return _systemMoney;
			}
			set { _systemMoney = value; }
		}
		
		[Reference]
		private ScalarType _systemGuid;
		public ScalarType SystemGuid
		{
			get
			{
				if (_systemGuid == null)
					_systemGuid = (ScalarType)CatalogLookup(SystemGuidName);
				return _systemGuid;
			}
			set { _systemGuid = value; }
		}
		
		[Reference]
		private ScalarType _systemTimeSpan;
		public ScalarType SystemTimeSpan
		{
			get
			{
				if (_systemTimeSpan == null)
					_systemTimeSpan = (ScalarType)CatalogLookup(SystemTimeSpanName);
				return _systemTimeSpan;
			}
			set { _systemTimeSpan = value; }
		}
		
		[Reference]
		private ScalarType _systemDateTime;
		public ScalarType SystemDateTime
		{
			get
			{
				if (_systemDateTime == null)
					_systemDateTime = (ScalarType)CatalogLookup(SystemDateTimeName);
				return _systemDateTime;
			}
			set { _systemDateTime = value; }
		}
		
		[Reference]
		private ScalarType _systemDate;
		public ScalarType SystemDate
		{
			get
			{
				if (_systemDate == null)
					_systemDate = (ScalarType)CatalogLookup(SystemDateName);
				return _systemDate;
			}
			set { _systemDate = value; }
		}
		
		[Reference]
		private ScalarType _systemTime;
		public ScalarType SystemTime
		{
			get
			{
				if (_systemTime == null)
					_systemTime = (ScalarType)CatalogLookup(SystemTimeName);
				return _systemTime;
			}
			set { _systemTime = value; }
		}
		
		[Reference]
		private ScalarType _systemBinary;
		public ScalarType SystemBinary
		{
			get
			{
				if (_systemBinary == null)
					_systemBinary = (ScalarType)CatalogLookup(SystemBinaryName);
				return _systemBinary;
			}
			set { _systemBinary = value; }
		}
		
		[Reference]
		private ScalarType _systemGraphic;
		public ScalarType SystemGraphic
		{
			get
			{
				if (_systemGraphic == null)
					_systemGraphic = (ScalarType)CatalogLookup(SystemGraphicName);
				return _systemGraphic;
			}
			set { _systemGraphic = value; }
		}
		
		[Reference]
		private ScalarType _systemError;
		public ScalarType SystemError
		{
			get
			{
				if (_systemError == null)
					_systemError = (ScalarType)CatalogLookup(SystemErrorName);
				return _systemError;
			}
			set { _systemError = value; }
		}
		
		[Reference]
		private ScalarType _systemName;
		public ScalarType SystemName
		{
			get
			{
				if (_systemName == null)
					_systemName = (ScalarType)CatalogLookup(SystemNameName);
				return _systemName;
			}
			set { _systemName = value; }
		}
		
		private IGenericType _systemGeneric;
		public IGenericType SystemGeneric
		{
			get
			{
				if (_systemGeneric == null)
					_systemGeneric = new GenericType();
				return _systemGeneric;
			}
		}
		
		private IGenericType _systemNilGeneric;
		public IGenericType SystemNilGeneric
		{
			get
			{
				if (_systemNilGeneric == null)
					_systemNilGeneric = new GenericType(true);
				return _systemNilGeneric;
			}
		}

		private IRowType _systemRow;
		public IRowType SystemRow
		{
			get 
			{
				if (_systemRow == null)
				{
					_systemRow = new RowType();
					_systemRow.IsGeneric = true;
				}
				return _systemRow;
			}
		}
		
		private ITableType _systemTable;
		public ITableType SystemTable
		{
			get
			{
				if (_systemTable == null)
				{
					_systemTable = new TableType();
					_systemTable.IsGeneric = true;
				}
				return _systemTable;
			}
		}

		private IListType _systemList;
		public IListType SystemList
		{
			get
			{
				if (_systemList == null)
				{
					_systemList = new ListType(SystemGeneric);
					_systemList.IsGeneric = true;
				}
				return _systemList;
			}
		}
		
		private ICursorType _systemCursor;
		public ICursorType SystemCursor
		{
			get
			{
				if (_systemCursor == null)
				{
					_systemCursor = new CursorType(SystemTable);
					_systemCursor.IsGeneric = true;
				}
				return _systemCursor;
			}
		}
    }
}

