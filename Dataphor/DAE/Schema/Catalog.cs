/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Security.Permissions;
using System.Security.Cryptography;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using D4 = Alphora.Dataphor.DAE.Language.D4;

namespace Alphora.Dataphor.DAE.Schema
{
    /// <summary> Catalog </summary>
	public class Catalog : Objects
    {
		public Catalog() : base() 
		{
			FDataTypes = new DataTypes(this);
		}
		
		// Used by serializer
		public Objects Objects { get { return this; } }
		
		// Libraries
		private Libraries FLibraries;
		public Libraries Libraries 
		{ 
			get 
			{
				if (FLibraries == null)
					FLibraries = new Libraries(); 
				return FLibraries; 
			} 
		}
		
		// LoadedLibraries
		private LoadedLibraries FLoadedLibraries;
		public LoadedLibraries LoadedLibraries 
		{ 
			get 
			{ 
				if (FLoadedLibraries == null)
					FLoadedLibraries = new LoadedLibraries();
				return FLoadedLibraries; 
			} 
		}
		
		// ClassLoader
		private ClassLoader FClassLoader;
		public ClassLoader ClassLoader
		{
			get
			{
				if (FClassLoader == null)
					FClassLoader = new ClassLoader();
				return FClassLoader;
			}
		}
		
        // Operators
        private OperatorMaps FOperatorMaps = new OperatorMaps();
        public OperatorMaps OperatorMaps { get { return FOperatorMaps; } }
        
		// DataTypes
        private DataTypes FDataTypes;
        public DataTypes DataTypes { get { return FDataTypes; } }

		// TimeStamp
		protected long FTimeStamp = 0;
		/// <summary>This time stamp is used to coordinate the catalog device cache tables with the actual system catalog.</summary>
		/// <remarks>
		/// This time stamp is incremented whenever any catalog changing event takes place.
		/// </remarks>
		public long TimeStamp { get { return FTimeStamp; } }

		/// <summary>Updates the catalog device cache coordination timestamp for this catalog.</summary>		
		public void UpdateTimeStamp()
		{
			lock (this)
			{
				FTimeStamp += 1;
			}
		}
		
		// CacheTimeStamp
		protected long FCacheTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate client-side catalog caches.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made to an existing object that is potentially stored in a client-side cache.
		/// For a more complete description of this property, refer to the CLI documentation.
		/// </remarks>
		public long CacheTimeStamp { get { return FCacheTimeStamp; } }
		
		/// <summary>Updates the client-side cache coordination timestamp for this catalog.</summary>
		public void UpdateCacheTimeStamp()
		{
			lock (this)
			{
				FCacheTimeStamp += 1;
			}
		}
		
		// PlanCacheTimeStamp
		protected long FPlanCacheTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate the server-side plan cache.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made to an existing object that is potentially referenced by a cached plan.
		/// </remarks>
		public long PlanCacheTimeStamp { get { return FPlanCacheTimeStamp; } }
		
		/// <summary>Updates the server-side plan cache coordination timestamp for this catalog.</summary>
		public void UpdatePlanCacheTimeStamp()
		{
			lock (this)
			{
				FPlanCacheTimeStamp += 1;
			}
		}

		// DerivationTimeStamp
		protected long FDerivationTimeStamp = 0;
		/// <summary>This timestamp is used to coordinate the derivation cache maintained by the Frontend server.</summary>
		/// <remarks>
		/// This timestamp is incremented whenever a change is made that could affect a derived paged stored in the Frontend server derivation cache.
		/// For a more complete description of this property, refer to the CLI documentation.
		/// </remarks>
		public long DerivationTimeStamp { get { return FDerivationTimeStamp; } }

		/// <summary>Updates the derivation cache coordination timestamp for this catalog.</summary>
		public void UpdateDerivationTimeStamp()
		{
			lock (this)
			{
				FDerivationTimeStamp += 1;
			}
		}
		
		#if USEOBJECTVALIDATE
		protected override void Validate(Object AItem)
        {
            if 
                (
                    !(
						(AItem is ScalarType) ||
                        (AItem is Operator) ||
                        (AItem is Device) ||
                        (AItem is ServerLink) ||
                        (AItem is TableVar) ||
                        (AItem is Reference) || 
                        (AItem is CatalogConstraint) ||
                        (AItem is Role) ||
                        (AItem is Sort) ||
                        (AItem is EventHandler) ||
						(AItem is Conversion) ||
						(AItem is DeviceObject)
                    )
                )
            {
                throw new SchemaException(SchemaException.Codes.TopLevelContainer);
            }
            
            if ((AItem.Name == null) || (AItem.Name == String.Empty))
				throw new SchemaException(SchemaException.Codes.ObjectNameRequired);
				
			Error.AssertFail(AItem.ID > 0, "Object '{0}' does not have an ID and cannot be tracked in a catalog.", AItem.Name);
			
			if ((AItem is Operator) && (FOperatorMaps.ContainsOperator((Operator)AItem)))
				throw new SchemaException(SchemaException.Codes.DuplicateOperator, AItem.ToString());
            base.Validate(AItem);
        }
        #endif

        protected override void Adding(Object AItem, int AIndex)
        {
			base.Adding(AItem, AIndex);
			if (AItem is Operator)
			{
				try
				{
					FOperatorMaps.AddOperator((Operator)AItem);
				}
				catch
				{
					RemoveAt(AIndex);
					throw;
				}
			}
        }
        
        protected override void Removing(Object AItem, int AIndex)
        {
			if ((AItem is Operator) && (FOperatorMaps.ContainsOperator((Operator)AItem)))
				FOperatorMaps.RemoveOperator((Operator)AItem);
			base.Removing(AItem, AIndex);
        }
        
        // Scalar Conversion Path Cache
        private ScalarConversionPathCache FConversionPathCache;
        public ScalarConversionPathCache ConversionPathCache 
        {
			get 
			{
				if (FConversionPathCache == null)
					FConversionPathCache = new ScalarConversionPathCache();
				return FConversionPathCache; 
			} 
		}
		
		// Operator Resolution Cache
		private OperatorResolutionCache FOperatorResolutionCache;
		public OperatorResolutionCache OperatorResolutionCache
		{
			get
			{
				if (FOperatorResolutionCache == null)
					FOperatorResolutionCache = new OperatorResolutionCache();
				return FOperatorResolutionCache;
			}
		}
		
        public string ShowMap(string AOperatorName)
        {
			return FOperatorMaps[FOperatorMaps.IndexOf(AOperatorName)].ShowMap();
        }
        
        public string ShowMaps()
        {
			return FOperatorMaps.ShowMaps();
        }
        
        // Catalog Emission
        private void EmitDependencies(EmissionContext AContext, Object AObject)
        {
			if (AObject.HasDependencies())
				for (int LIndex = 0; LIndex < AObject.Dependencies.Count; LIndex++)
					EmitObject(AContext, AObject.Dependencies.ResolveObject(AContext.Session, LIndex));
        }
        
        private void EmitTableVarChildDependencies(EmissionContext AContext, TableVar ATableVar)
        {
			foreach (TableVarColumn LColumn in ATableVar.Columns)
			{
				if ((LColumn.Default != null) && LColumn.Default.IsRemotable)
					EmitDependencies(AContext, LColumn.Default);
						
				foreach (Constraint LConstraint in LColumn.Constraints)
					EmitDependencies(AContext, LConstraint);
			}

			foreach (Constraint LConstraint in ATableVar.Constraints)
				if ((LConstraint.ConstraintType == ConstraintType.Row) && ((AContext.Mode != EmitMode.ForRemote) || LConstraint.IsRemotable))
					EmitDependencies(AContext, LConstraint);

			foreach (Order LOrder in ATableVar.Orders)
			{
				OrderColumn LOrderColumn;
				for (int LIndex = 0; LIndex < LOrder.Columns.Count; LIndex++)
				{
					LOrderColumn = LOrder.Columns[LIndex];
					if ((LOrderColumn.Sort != null) && ((AContext.Mode != EmitMode.ForRemote) || LOrderColumn.Sort.IsRemotable))
						EmitDependencies(AContext, LOrderColumn.Sort);
				}
			}
        }
        
        private void EmitChildDependencies(EmissionContext AContext, Object AObject)
        {
			if (AObject is ScalarType)
			{
				// Emit Dependencies of representations
				ScalarType LScalarType = (ScalarType)AObject;
				
				foreach (Representation LRepresentation in LScalarType.Representations)
				{
					if 
					(
						(!LRepresentation.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && 
							!LRepresentation.HasExternalDependencies()
					)
					{
						EmitDependencies(AContext, LRepresentation);
						
						if (LRepresentation.Selector.HasDependencies())
							for (int LIndex = 0; LIndex < LRepresentation.Selector.Dependencies.Count; LIndex++)
								if (LRepresentation.Selector.Dependencies.IDs[LIndex] != AObject.ID)
									EmitObject(AContext, LRepresentation.Selector.Dependencies.ResolveObject(AContext.Session, LIndex));
						
						foreach (Property LProperty in LRepresentation.Properties)
						{
							EmitDependencies(AContext, LProperty);
							if (LProperty.ReadAccessor.HasDependencies())
								for (int LIndex = 0; LIndex < LProperty.ReadAccessor.Dependencies.Count; LIndex++)
									if (LProperty.ReadAccessor.Dependencies.IDs[LIndex] != AObject.ID)
										EmitObject(AContext, LProperty.ReadAccessor.Dependencies.ResolveObject(AContext.Session, LIndex));
							if (LProperty.WriteAccessor.HasDependencies())
								for (int LIndex = 0; LIndex < LProperty.WriteAccessor.Dependencies.Count; LIndex++)
									if (LProperty.WriteAccessor.Dependencies.IDs[LIndex] != AObject.ID)
										EmitObject(AContext, LProperty.WriteAccessor.Dependencies.ResolveObject(AContext.Session, LIndex));
						}
					}
				}
			}
			else if (AObject is TableVar)
			{
				EmitTableVarChildDependencies(AContext, (TableVar)AObject);
			}
        }
        
        private void EmitChildren(EmissionContext AContext, Object AObject)
        {
			if (AContext.Mode != EmitMode.ForStorage)
			{
				ScalarType LScalarType = AObject as ScalarType;
				if (LScalarType != null)
				{
					// Emit the default, constraints, non-system representations, and specials for this scalar type
					foreach (Schema.Representation LRepresentation in LScalarType.Representations)
					{
						if 
						(
							(!LRepresentation.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && 
								LRepresentation.HasExternalDependencies()
						)
						{
							EmitDependencies(AContext, LRepresentation);
							foreach (Property LProperty in LRepresentation.Properties)
								EmitDependencies(AContext, LProperty);
							if (!AContext.EmittedObjects.Contains(LRepresentation.ID))
							{
								AContext.Block.Statements.Add(LRepresentation.EmitStatement(AContext.Mode));
								AContext.EmittedObjects.Add(LRepresentation.ID, LRepresentation);
							}
						}
					}
					
					EmitDependencies(AContext, LScalarType.IsSpecialOperator);
					foreach (Schema.Special LSpecial in LScalarType.Specials)
					{
						if ((!LSpecial.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && AContext.ShouldEmitWithLibrary(LSpecial))
						{
							EmitDependencies(AContext, LSpecial);
							EmitDependencies(AContext, LSpecial.Selector);
							EmitDependencies(AContext, LSpecial.Comparer);
							if (!AContext.EmittedObjects.Contains(LSpecial.ID))
							{
								AContext.Block.Statements.Add(LSpecial.EmitStatement(AContext.Mode));
								AContext.EmittedObjects.Add(LSpecial.ID, LSpecial);
							}
						}
					}
					
					if 
					(
						(LScalarType.Default != null) && 
						((AContext.Mode != EmitMode.ForRemote) || LScalarType.Default.IsRemotable) && 
						(!LScalarType.Default.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && 
						AContext.ShouldEmitWithLibrary(LScalarType.Default)
					)
					{
						EmitDependencies(AContext, LScalarType.Default);
						if (!AContext.EmittedObjects.Contains(LScalarType.Default.ID))
						{
							AContext.Block.Statements.Add(LScalarType.Default.EmitStatement(AContext.Mode));
							AContext.EmittedObjects.Add(LScalarType.Default.ID, LScalarType.Default);
						}
					}

					foreach (Constraint LConstraint in LScalarType.Constraints)
						if 
						(
							((AContext.Mode != EmitMode.ForRemote) || LConstraint.IsRemotable) && 
							(!LConstraint.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && 
							AContext.ShouldEmitWithLibrary(LConstraint)
						)
						{
							EmitDependencies(AContext, LConstraint);
							if (!AContext.EmittedObjects.Contains(LConstraint.ID))
							{
								AContext.Block.Statements.Add(LConstraint.EmitStatement(AContext.Mode));
								AContext.EmittedObjects.Add(LConstraint.ID, LConstraint);
							}
						}
				}
				
				TableVar LTableVar = AObject as TableVar;
				if (LTableVar != null)
				{
					foreach (TableVarColumn LColumn in LTableVar.Columns)
					{
						if 
						(
							(AObject is BaseTableVar) && 
							(LColumn.Default != null) && 
							!LColumn.Default.IsRemotable && 
							(AContext.Mode != EmitMode.ForRemote) && 
							(!LColumn.Default.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && 
							AContext.ShouldEmitWithLibrary(LColumn.Default)
						)
						{
							EmitDependencies(AContext, LColumn.Default);
							if (!AContext.EmittedObjects.Contains(LColumn.Default.ID))
							{
								AContext.Block.Statements.Add(LColumn.Default.EmitStatement(AContext.Mode));
								AContext.EmittedObjects.Add(LColumn.Default.ID, LColumn.Default);
							}
						}
					}

					foreach (Constraint LConstraint in LTableVar.Constraints)
						if ((!LConstraint.IsGenerated || AContext.IncludeGenerated || (AContext.Mode == EmitMode.ForStorage)) && (LConstraint.ConstraintType == ConstraintType.Database) && ((AContext.Mode != EmitMode.ForRemote) || LConstraint.IsRemotable) && AContext.ShouldEmitWithLibrary(LConstraint))
						{
							EmitDependencies(AContext, LConstraint);
							if (!AContext.EmittedObjects.Contains(LConstraint.ID))
							{
								AContext.Block.Statements.Add(LConstraint.EmitStatement(AContext.Mode));
								AContext.EmittedObjects.Add(LConstraint.ID, LConstraint);
							}
						}
				}				
			}
        }

        private void EmitObject(EmissionContext AContext, Object AObject)
        {
			if (AContext.ShouldEmit(AObject))
			{
				if (AContext.ShouldEmitWithLibrary(AObject))
				{
					if (!AContext.EmittedObjects.Contains(AObject.ID))
					{
						EmitDependencies(AContext, AObject);
						EmitChildDependencies(AContext, AObject);
					}
					
					if (!AContext.EmittedObjects.Contains(AObject.ID))
					{
						AContext.Block.Statements.Add(AObject.EmitStatement(AContext.Mode));
						AContext.EmittedObjects.Add(AObject.ID, AObject);
						EmitChildren(AContext, AObject);
					}
				}
				else
				{
					if (!AContext.EmittedObjects.Contains(AObject.ID))
					{
						AContext.EmittedObjects.Add(AObject.ID, AObject);
						EmitChildren(AContext, AObject);
					}
				}
			}
        }
        
        public void EmitLibrary(EmissionContext AContext, LoadedLibrary ALibrary)
        {
			if (!AContext.EmittedLibraries.Contains(ALibrary.Name))
			{
				foreach (LoadedLibrary LLibrary in ALibrary.RequiredLibraries)
					EmitLibrary(AContext, LLibrary);

				if (ALibrary.Name != Server.Server.CSystemLibraryName)
				{
					ExpressionStatement LStatement = new ExpressionStatement();
					LStatement.Expression = new CallExpression("LoadLibrary", new Expression[]{new ValueExpression(ALibrary.Name)});
					AContext.Block.Statements.Add(LStatement);
				}
				AContext.EmittedLibraries.Add(ALibrary);
			}
        }
        
        public void EmitLibraries(EmissionContext AContext, Schema.Objects ALibraries)
        {
			foreach (LoadedLibrary LLibrary in ALibraries)
				EmitLibrary(AContext, LLibrary);
        }
        
        private void GatherDependents(CatalogDeviceSession ASession, Schema.Object AObject, ObjectList ADependents)
        {
			List<Schema.DependentObjectHeader> LHeaders = ASession.SelectObjectDependents(AObject.ID, true);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
				if (!ADependents.Contains(LHeaders[LIndex].ID))
				{
					Schema.Object LObject = ASession.ResolveObject(LHeaders[LIndex].ID);
					ADependents.Add(LObject.ID, LObject);
				}
        }
        
		/// <summary>Emits a statement to reconstruct the catalog based on the given parameters.</summary>
		/// <param name="AMode">Specifies the mode for statement emission.</param>
		/// <param name="ARequestedObjectNames">Specifies a list of object names to be serialized.  If this list is empty, the entire catalog is emitted.</param>
		/// <param name="ALibraryName">Specifies the name of the library to be emitted.  If this is the empty string, the system library will be emitted.</param>
		/// <param name="AIncludeSystem">Specifies whether system objects should be included in the emitted catalog.</param>
		/// <param name="AIncludeDependents">Specifies whether the dependents of the objects should be included in the emitted catalog.</param>
		/// <param name="AIncludeObject">Specifies whether the object itself should be included in the emitted catalog.</param>
		/// <remarks>
		///	This is the main EmitStatement overload which all other EmitStatement overloads call.
		/// </remarks>
        public Statement EmitStatement
        (
			CatalogDeviceSession ASession,
			EmitMode AMode, 
			string[] ARequestedObjectNames, 
			string ALibraryName, 
			bool AIncludeSystem, 
			bool AIncludeGenerated, 
			bool AIncludeDependents, 
			bool AIncludeObject
		)
        {
			ObjectList LRequestedObjects = new ObjectList();
			
			for (int LIndex = 0; LIndex < ARequestedObjectNames.Length; LIndex++)
			{
				int LObjectIndex = IndexOf(ARequestedObjectNames[LIndex]);
				if (LObjectIndex >= 0)
				{
					Schema.Object LObject = this[LObjectIndex];
					if ((AIncludeObject) && !LRequestedObjects.Contains(LObject.ID))
						LRequestedObjects.Add(LObject.ID, LObject);
					if (AIncludeDependents)
						GatherDependents(ASession, LObject, LRequestedObjects);
				}
			}
			
			EmissionContext LContext = new EmissionContext(ASession, this, AMode, LRequestedObjects, ALibraryName, AIncludeSystem, AIncludeGenerated, AIncludeDependents, AIncludeObject);
			if (LRequestedObjects.Count == 0)
			{
				if (ALibraryName == String.Empty)
					EmitLibraries(LContext, FLoadedLibraries);
			}

			if (LRequestedObjects.Count > 0)
			{
				for (int LIndex = 0; LIndex < LRequestedObjects.Count; LIndex++)
					EmitObject(LContext, LRequestedObjects.ResolveObject(ASession, LIndex));
			}
			else
			{
				foreach (CatalogObjectHeader LHeader in ASession.SelectLibraryCatalogObjects(ALibraryName))
					EmitObject(LContext, ASession.ResolveCatalogObject(LHeader.ID));
			}

			return LContext.Block;
        }
        
		/// <summary>Emits a statement to reconstruct the entire catalog.</summary>
        public Statement EmitStatement(CatalogDeviceSession ASession, EmitMode AMode, bool AIncludeSystem)
        {
			return EmitStatement(ASession, AMode, new string[0], String.Empty, AIncludeSystem, false, false, true);
        }

		/// <summary>Emits a statement to reconstruct the catalog for the given library.</summary>        
        public Statement EmitStatement(CatalogDeviceSession ASession, EmitMode AMode, string ALibraryName, bool AIncludeSystem)
        {
			return EmitStatement(ASession, AMode, new string[0], ALibraryName, AIncludeSystem, false, false, true);
        }

		/// <summary>Emits a statement to reconstruct the specified list of catalog objects.</summary>        
        public Statement EmitStatement(CatalogDeviceSession ASession, EmitMode AMode, string[] ARequestedObjectNames)
        {
			return EmitStatement(ASession, AMode, ARequestedObjectNames, String.Empty, true, false, false, true);
        }
        
        protected void ReportDroppedObject(EmissionContext AContext, Schema.Object AObject)
        {
			if (!AContext.EmittedObjects.Contains(AObject.ID))
				AContext.EmittedObjects.Add(AObject.ID, AObject);
        }
        
        protected void ReportDroppedObjects(EmissionContext AContext, Schema.Object AObject)
        {
			if (AObject is ScalarType)
			{
				ScalarType LScalarType = (ScalarType)AObject;
				if (LScalarType.Default != null)
					ReportDroppedObject(AContext, LScalarType.Default);
					
				foreach (Constraint LConstraint in LScalarType.Constraints)
					ReportDroppedObject(AContext, LConstraint);
					
				foreach (Special LSpecial in LScalarType.Specials)
					ReportDroppedObject(AContext, LSpecial);
					
				if (LScalarType.EqualityOperator != null)
					ReportDroppedObject(AContext, LScalarType.EqualityOperator);
					
				if (LScalarType.ComparisonOperator != null)
					ReportDroppedObject(AContext, LScalarType.ComparisonOperator);
					
				foreach (Schema.Representation LRepresentation in LScalarType.Representations)
					ReportDroppedObjects(AContext, LRepresentation);
			}
			else if (AObject is Representation)
			{
				Schema.Representation LRepresentation = (Schema.Representation)AObject;

				foreach (Schema.Property LProperty in LRepresentation.Properties)
				{
					if (LProperty.ReadAccessor != null)
						ReportDroppedObject(AContext, LProperty.ReadAccessor);
					
					if (LProperty.WriteAccessor != null)
						ReportDroppedObject(AContext, LProperty.WriteAccessor);
				}
				
				if (LRepresentation.Selector != null)
					ReportDroppedObject(AContext, LRepresentation.Selector);
			}
			else if (AObject is TableVarColumn)
			{
				TableVarColumn LTableVarColumn = (TableVarColumn)AObject;
				if (LTableVarColumn.Default != null)
					ReportDroppedObject(AContext, LTableVarColumn.Default);
					
				foreach (Constraint LConstraint in LTableVarColumn.Constraints)
					ReportDroppedObject(AContext, LConstraint);
			}
			else if (AObject is TableVar)
			{	
				TableVar LTableVar = (TableVar)AObject;
				foreach (TableVarColumn LColumn in LTableVar.Columns)
					ReportDroppedObjects(AContext, LColumn);
					
				foreach (Constraint LConstraint in LTableVar.Constraints)
					ReportDroppedObject(AContext, LConstraint);
			}
			else if (AObject is Reference)
			{
				Reference LReference = (Reference)AObject;
				if (LReference.SourceConstraint != null)
					ReportDroppedObject(AContext, LReference.SourceConstraint);
				if (LReference.TargetConstraint != null)
					ReportDroppedObject(AContext, LReference.TargetConstraint);
			}
			else if (AObject is DeviceScalarType)
			{
				Schema.CatalogObjectHeaders LHeaders = AContext.Session.SelectGeneratedObjects(AObject.ID);
				for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
					ReportDroppedObject(AContext, AContext.Session.ResolveCatalogObject(LHeaders[LIndex].ID));
			}
        }
        
        public void EmitDropObject(EmissionContext AContext, Schema.Object AObject)
        {
			if (AContext.ShouldEmitDrop(AObject))
			{
				if (AContext.IncludeObject || !AContext.RequestedObjects.Contains(AObject.ID))
				{
					// Should be a safe drop if the object is an AT or session object
					Statement LStatement = AObject.EmitDropStatement(AContext.Mode);
					if (((AObject.IsATObject || AObject.IsSessionObject)) && ((AObject is Schema.TableVar) || (AObject is Schema.Operator)))
						LStatement = 
							new IfStatement
							(
								new CallExpression
								(
									".System.ObjectExists", 
									new Expression[]{new CallExpression(".System.Name", new Expression[]{new ValueExpression(Schema.Object.EnsureRooted(AObject.Name), TokenType.String)})}
								),
								new ExpressionStatement
								(
									new CallExpression
									(
										".System.Execute",
										new Expression[]{new ValueExpression(new D4TextEmitter().Emit(LStatement), TokenType.String)}
									)
								),
								null
							);
						
					if (AObject.IsATObject)
					{
						Schema.TableVar LTableVar = null;
						if (AObject is Schema.TableVarColumnDefault)
							LTableVar = ((Schema.TableVarColumnDefault)AObject).TableVarColumn.TableVar;
							
						if (AObject is Schema.TableVarColumnConstraint)
							LTableVar = ((Schema.TableVarColumnConstraint)AObject).TableVarColumn.TableVar;
							
						if (AObject is Schema.TableVarConstraint)
							LTableVar = ((Schema.TableVarConstraint)AObject).TableVar;
							
						if (AObject is Schema.TableVarEventHandler)
							LTableVar = ((Schema.TableVarEventHandler)AObject).TableVar;
							
						if (AObject is Schema.TableVarColumnEventHandler)
							LTableVar = ((Schema.TableVarColumnEventHandler)AObject).TableVarColumn.TableVar;
							
						if (AObject.IsATObject && (LTableVar != null))
							LStatement = 
								new IfStatement
								(
									new CallExpression
									(
										".System.ObjectExists", 
										new Expression[]{new CallExpression(".System.Name", new Expression[]{new ValueExpression(Schema.Object.EnsureRooted(LTableVar.Name), TokenType.String)})}
									), 
									new ExpressionStatement
									(
										new CallExpression
										(
											".System.Execute",
											new Expression[]{new ValueExpression(new D4TextEmitter().Emit(LStatement), TokenType.String)}
										)
									),
									null
								);
					}

					AContext.Block.Statements.Add(LStatement);
				}
			}
        }
        
        public void EmitUnregisterLibrary(EmissionContext AContext, LoadedLibrary ALibrary)
        {
			if (!AContext.EmittedLibraries.Contains(ALibrary) && !ALibrary.IsSystem)
			{
				// Unregister all requiredby libraries
				foreach (LoadedLibrary LLibrary in ALibrary.RequiredByLibraries)
					EmitUnregisterLibrary(AContext, LLibrary);
	
				// Unregister the library				
				AContext.Block.Statements.Add(new ExpressionStatement(new CallExpression("UnregisterLibrary", new Expression[]{new ValueExpression(ALibrary.Name)})));
				AContext.EmittedLibraries.Add(ALibrary);
			}
        }
        
        public Statement EmitDropStatement
        (
			CatalogDeviceSession ASession,
			string[] ARequestedObjectNames, 
			string ALibraryName, 
			bool AIncludeSystem, 
			bool AIncludeGenerated, 
			bool AIncludeDependents, 
			bool AIncludeObject
		)
        {
			ObjectList LRequestedObjects = new ObjectList();
			foreach (string LObjectName in ARequestedObjectNames)
			{
				int LObjectIndex = IndexOf(LObjectName);
				if (LObjectIndex >= 0)
					LRequestedObjects.Add(this[LObjectIndex].ID, this[LObjectIndex]);
				else
				{
					// TODO: Refactor NameResolutionPath access here
					Schema.CatalogObject LObject = ASession.ResolveName(LObjectName, ASession.ServerProcess.Plan.NameResolutionPath, new StringCollection());
					if (LObject != null)
						LRequestedObjects.Add(LObject.ID, LObject);
				}
			}
			
			EmissionContext LContext = new EmissionContext(ASession, this, EmitMode.ForCopy, LRequestedObjects, ALibraryName, AIncludeSystem, AIncludeGenerated, AIncludeDependents, AIncludeObject);
			
			ObjectList LDropList = ((ARequestedObjectNames.Length > 0) && (LRequestedObjects.Count == 0)) ? new ObjectList() : BuildDropList(LContext);
			
			for (int LIndex = 0; LIndex < LDropList.Count; LIndex++)
				EmitDropObject(LContext, LDropList.ResolveObject(ASession, LIndex));
			
			if (ARequestedObjectNames.Length == 0)
			{
				if (ALibraryName == String.Empty)
				{			
					// UnregisterLibraries
					foreach (LoadedLibrary LLibrary in FLoadedLibraries)
						EmitUnregisterLibrary(LContext, LLibrary);
				}
			}
			
			return LContext.Block;
        }

		private ObjectList BuildDropList(EmissionContext LContext)
		{
			ObjectList LDropList = new ObjectList();
			
			if (LContext.RequestedObjects.Count == 0) 
			{
				foreach (CatalogObjectHeader LHeader in LContext.Session.SelectLibraryCatalogObjects(LContext.LibraryName))
					BuildObjectDropList(LContext, LDropList, LContext.Session.ResolveCatalogObject(LHeader.ID));
			}
			else
			{
				for (int LIndex = 0; LIndex < LContext.RequestedObjects.Count; LIndex++)
					BuildObjectDropList(LContext, LDropList, LContext.RequestedObjects.ResolveObject(LContext.Session, LIndex));
			}

			return LDropList;
		}

		private void BuildObjectDropList(EmissionContext AContext, ObjectList ADropList, Schema.Object AObject)
		{
			if (!AContext.EmittedObjects.Contains(AObject.ID))
			{
				AContext.EmittedObjects.Add(AObject.ID, AObject);

				// Add each dependent
				if (AContext.IncludeDependents)
					BuildDependentDropList(AContext, ADropList, AObject);
				
				ADropList.Ensure(AObject);
				
				ReportDroppedObjects(AContext, AObject);
			}
		}
		
		private void BuildDependentDropList(EmissionContext AContext, ObjectList ADropList, Schema.Object AObject)
		{
			List<Schema.DependentObjectHeader> LHeaders = AContext.Session.SelectObjectDependents(AObject.ID, false);
			for (int LIndex = 0; LIndex < LHeaders.Count; LIndex++)
			{
				Schema.Object LObject = AContext.Session.ResolveObject(LHeaders[LIndex].ID);
				if ((LObject is Schema.Representation) && LObject.IsGenerated)
					BuildObjectDropList(AContext, ADropList, ((Schema.Representation)LObject).ScalarType);
				else if (LObject is Schema.Property)
				{
					if (((Schema.Property)LObject).Representation.IsGenerated)
						BuildObjectDropList(AContext, ADropList, ((Schema.Property)LObject).Representation.ScalarType);
					else
						BuildObjectDropList(AContext, ADropList, ((Schema.Property)LObject).Representation);
				}
				else if ((LHeaders[LIndex].GeneratorObjectID >= 0) && !AContext.IncludeGenerated)
				{
					Schema.Object LGeneratorObject = AContext.Session.ResolveObject(LHeaders[LIndex].GeneratorObjectID);
					if ((LGeneratorObject is Schema.Representation) && LGeneratorObject.IsGenerated)
						BuildObjectDropList(AContext, ADropList, ((Schema.Representation)LGeneratorObject).ScalarType);
					else if (LGeneratorObject is Schema.Property)
					{
						if (((Schema.Property)LGeneratorObject).Representation.IsGenerated)
							BuildObjectDropList(AContext, ADropList, ((Schema.Property)LGeneratorObject).Representation.ScalarType);
						else
							BuildObjectDropList(AContext, ADropList, ((Schema.Property)LGeneratorObject).Representation);
					}
					else
						BuildObjectDropList(AContext, ADropList, LGeneratorObject);
				}
				else
					BuildObjectDropList(AContext, ADropList, LObject);
			}
				
			// Drop child object depenendents
			BuildChildDependentDropList(AContext, ADropList, AObject);
		}
		
		private void BuildChildDependentDropList(EmissionContext AContext, ObjectList ADropList, Schema.Object AObject)
		{
			if (AObject is Schema.ScalarType)
			{
				ScalarType LScalarType = (ScalarType)AObject;
				
				if (LScalarType.Default != null)
					BuildDependentDropList(AContext, ADropList, LScalarType.Default);
					
				foreach (Constraint LConstraint in LScalarType.Constraints)
					BuildDependentDropList(AContext, ADropList, LConstraint);
					
				foreach (Special LSpecial in LScalarType.Specials)
					BuildDependentDropList(AContext, ADropList, LSpecial);
					
				if (LScalarType.EqualityOperator != null)
					BuildDependentDropList(AContext, ADropList, LScalarType.EqualityOperator);
					
				if (LScalarType.ComparisonOperator != null)
					BuildDependentDropList(AContext, ADropList, LScalarType.ComparisonOperator);
					
				foreach (Representation LRepresentation in LScalarType.Representations)
				{
					BuildDependentDropList(AContext, ADropList, LRepresentation);
					foreach (Property LProperty in LRepresentation.Properties)
					{
						BuildDependentDropList(AContext, ADropList, LProperty);
						if (LProperty.ReadAccessor != null)
							BuildDependentDropList(AContext, ADropList, LProperty.ReadAccessor);
							
						if (LProperty.WriteAccessor != null)
							BuildDependentDropList(AContext, ADropList, LProperty.WriteAccessor);
					}
					
					if (LRepresentation.Selector != null)
						BuildDependentDropList(AContext, ADropList, LRepresentation.Selector);
				}
			}
			else if (AObject is TableVarColumn)
			{
				TableVarColumn LTableVarColumn = (TableVarColumn)AObject;
				if (LTableVarColumn.Default != null)
					BuildDependentDropList(AContext, ADropList, LTableVarColumn.Default);
					
				foreach (Constraint LConstraint in LTableVarColumn.Constraints)
					BuildDependentDropList(AContext, ADropList, LConstraint);
			}
			else if (AObject is TableVar)
			{
				TableVar LTableVar = (TableVar)AObject;
				
				foreach (TableVarColumn LColumn in LTableVar.Columns)
					BuildDependentDropList(AContext, ADropList, LColumn);
					
				foreach (Constraint LConstraint in LTableVar.Constraints)
					if (LConstraint.ConstraintType == ConstraintType.Database)
						BuildDependentDropList(AContext, ADropList, LConstraint);
					else
						BuildDependentDropList(AContext, ADropList, LConstraint);
			}
		}
        
        public Statement EmitDropStatement(CatalogDeviceSession ASession, string[] ARequestedObjectNames, string ALibraryName)
        {
			return EmitDropStatement(ASession, ARequestedObjectNames, ALibraryName, false, false, true, true);
        }
        
        public Statement EmitDropStatement(CatalogDeviceSession ASession, string ALibraryName)
        {
			return EmitDropStatement(ASession, new string[]{}, ALibraryName, false, false, true, true);
        }
        
        public Statement EmitDropStatement(CatalogDeviceSession ASession)
        {
			return EmitDropStatement(ASession, new string[]{}, String.Empty, false, false, true, true);
        }
        
        public void IncludeDependencies(CatalogDeviceSession ASession, Schema.Catalog ASourceCatalog, Schema.Object AObject, EmitMode AMode)
        {
			AObject.IncludeDependencies(ASession, ASourceCatalog, this, AMode);
			foreach (Object LObject in this)
				LObject.IncludeHandlers(ASession, ASourceCatalog, this, AMode);
        }
        
        public void IncludeDependencies(CatalogDeviceSession ASession, Schema.Catalog ASourceCatalog, Schema.IDataType ADataType, EmitMode AMode)
        {
			ADataType.IncludeDependencies(ASession, ASourceCatalog, this, AMode);
        }
	}
    
    public delegate void CatalogLookupFailedEvent(Schema.Catalog ACatalog, string AName);
    
    public class DataTypes : System.Object
    {
		public DataTypes(Catalog ACatalog) : base()
		{
			FCatalog = ACatalog;
		}
		
		[Reference]
		private Catalog FCatalog;
		
		public event CatalogLookupFailedEvent OnCatalogLookupFailed;
		protected void DoCatalogLookupFailed(string AName)
		{
			if (OnCatalogLookupFailed != null)
				OnCatalogLookupFailed(FCatalog, AName);
		}
		
		protected Object CatalogLookup(string AName)
		{
			int LIndex = FCatalog.IndexOf(AName);
			if (LIndex < 0)
			{
				DoCatalogLookupFailed(AName);
				return FCatalog[AName];
			}
			return FCatalog[LIndex];
		}

		// do not localize
		public const string CSystemGeneric = "System.Generic";
		#if USETYPEINHERITANCE
		public const string CSystemAlpha = "System.Alpha";
		public const string CSystemOmega = "System.Omega";
		#endif
		public const string CSystemScalar = "System.Scalar";
		public const string CSystemBoolean = "System.Boolean";
		public const string CSystemDecimal = "System.Decimal";
		public const string CSystemLong = "System.Long";
		public const string CSystemInteger = "System.Integer";
		public const string CSystemShort = "System.Short";
		public const string CSystemByte = "System.Byte";
		public const string CSystemString = "System.String";
		#if USEISTRING
		public const string CSystemIString = "System.IString";
		#endif
		public const string CSystemMoney = "System.Money";
		public const string CSystemGuid = "System.Guid";
		public const string CSystemTimeSpan = "System.TimeSpan";
		public const string CSystemDateTime = "System.DateTime";
		public const string CSystemDate = "System.Date";
		public const string CSystemTime = "System.Time";
		public const string CSystemBinary = "System.Binary";
		public const string CSystemGraphic = "System.Graphic";
		public const string CSystemError = "System.Error";
		public const string CSystemName = "System.Name";

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
		private ScalarType FSystemScalar;
		public ScalarType SystemScalar
		{
			get
			{
				if (FSystemScalar == null)
					FSystemScalar = (ScalarType)CatalogLookup(CSystemScalar);
				return FSystemScalar;
			}
			set { FSystemScalar = value; }
		}
		
		[Reference]
		private ScalarType FSystemBoolean;
		public ScalarType SystemBoolean
		{
			get
			{
				if (FSystemBoolean == null)
					FSystemBoolean = (ScalarType)CatalogLookup(CSystemBoolean);
				return FSystemBoolean;
			}
			set { FSystemBoolean = value; }
		}
		
		[Reference]
		private ScalarType FSystemDecimal;
		public ScalarType SystemDecimal
		{
			get
			{
				if (FSystemDecimal == null)
					FSystemDecimal = (ScalarType)CatalogLookup(CSystemDecimal);
				return FSystemDecimal;
			}
			set { FSystemDecimal = value; }
		}
		
		[Reference]
		private ScalarType FSystemLong;
		public ScalarType SystemLong
		{
			get
			{
				if (FSystemLong == null)
					FSystemLong = (ScalarType)CatalogLookup(CSystemLong);
				return FSystemLong;
			}
			set { FSystemLong = value; }
		}
		
		[Reference]
		private ScalarType FSystemInteger;
		public ScalarType SystemInteger
		{
			get
			{
				if (FSystemInteger == null)
					FSystemInteger = (ScalarType)CatalogLookup(CSystemInteger);
				return FSystemInteger;
			}
			set { FSystemInteger = value; }
		}
		
		[Reference]
		private ScalarType FSystemShort;
		public ScalarType SystemShort
		{
			get
			{
				if (FSystemShort == null)
					FSystemShort = (ScalarType)CatalogLookup(CSystemShort);
				return FSystemShort;
			}
			set { FSystemShort = value; }
		}
		
		[Reference]
		private ScalarType FSystemByte;
		public ScalarType SystemByte
		{
			get
			{
				if (FSystemByte == null)
					FSystemByte = (ScalarType)CatalogLookup(CSystemByte);
				return FSystemByte;
			}
			set { FSystemByte = value; }
		}
		
		[Reference]
		private ScalarType FSystemString;
		public ScalarType SystemString
		{
			get
			{
				if (FSystemString == null)
					FSystemString = (ScalarType)CatalogLookup(CSystemString);
				return FSystemString;
			}
			set { FSystemString = value; }
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
		private ScalarType FSystemMoney;
		public ScalarType SystemMoney
		{
			get
			{
				if (FSystemMoney == null)
					FSystemMoney = (ScalarType)CatalogLookup(CSystemMoney);
				return FSystemMoney;
			}
			set { FSystemMoney = value; }
		}
		
		[Reference]
		private ScalarType FSystemGuid;
		public ScalarType SystemGuid
		{
			get
			{
				if (FSystemGuid == null)
					FSystemGuid = (ScalarType)CatalogLookup(CSystemGuid);
				return FSystemGuid;
			}
			set { FSystemGuid = value; }
		}
		
		[Reference]
		private ScalarType FSystemTimeSpan;
		public ScalarType SystemTimeSpan
		{
			get
			{
				if (FSystemTimeSpan == null)
					FSystemTimeSpan = (ScalarType)CatalogLookup(CSystemTimeSpan);
				return FSystemTimeSpan;
			}
			set { FSystemTimeSpan = value; }
		}
		
		[Reference]
		private ScalarType FSystemDateTime;
		public ScalarType SystemDateTime
		{
			get
			{
				if (FSystemDateTime == null)
					FSystemDateTime = (ScalarType)CatalogLookup(CSystemDateTime);
				return FSystemDateTime;
			}
			set { FSystemDateTime = value; }
		}
		
		[Reference]
		private ScalarType FSystemDate;
		public ScalarType SystemDate
		{
			get
			{
				if (FSystemDate == null)
					FSystemDate = (ScalarType)CatalogLookup(CSystemDate);
				return FSystemDate;
			}
			set { FSystemDate = value; }
		}
		
		[Reference]
		private ScalarType FSystemTime;
		public ScalarType SystemTime
		{
			get
			{
				if (FSystemTime == null)
					FSystemTime = (ScalarType)CatalogLookup(CSystemTime);
				return FSystemTime;
			}
			set { FSystemTime = value; }
		}
		
		[Reference]
		private ScalarType FSystemBinary;
		public ScalarType SystemBinary
		{
			get
			{
				if (FSystemBinary == null)
					FSystemBinary = (ScalarType)CatalogLookup(CSystemBinary);
				return FSystemBinary;
			}
			set { FSystemBinary = value; }
		}
		
		[Reference]
		private ScalarType FSystemGraphic;
		public ScalarType SystemGraphic
		{
			get
			{
				if (FSystemGraphic == null)
					FSystemGraphic = (ScalarType)CatalogLookup(CSystemGraphic);
				return FSystemGraphic;
			}
			set { FSystemGraphic = value; }
		}
		
		[Reference]
		private ScalarType FSystemError;
		public ScalarType SystemError
		{
			get
			{
				if (FSystemError == null)
					FSystemError = (ScalarType)CatalogLookup(CSystemError);
				return FSystemError;
			}
			set { FSystemError = value; }
		}
		
		[Reference]
		private ScalarType FSystemName;
		public ScalarType SystemName
		{
			get
			{
				if (FSystemName == null)
					FSystemName = (ScalarType)CatalogLookup(CSystemName);
				return FSystemName;
			}
			set { FSystemName = value; }
		}
		
		private IGenericType FSystemGeneric;
		public IGenericType SystemGeneric
		{
			get
			{
				if (FSystemGeneric == null)
					FSystemGeneric = new GenericType();
				return FSystemGeneric;
			}
		}

		private IRowType FSystemRow;
		public IRowType SystemRow
		{
			get 
			{
				if (FSystemRow == null)
				{
					FSystemRow = new RowType();
					FSystemRow.IsGeneric = true;
				}
				return FSystemRow;
			}
		}
		
		private ITableType FSystemTable;
		public ITableType SystemTable
		{
			get
			{
				if (FSystemTable == null)
				{
					FSystemTable = new TableType();
					FSystemTable.IsGeneric = true;
				}
				return FSystemTable;
			}
		}

		private IListType FSystemList;
		public IListType SystemList
		{
			get
			{
				if (FSystemList == null)
				{
					FSystemList = new ListType(SystemGeneric);
					FSystemList.IsGeneric = true;
				}
				return FSystemList;
			}
		}
		
		private ICursorType FSystemCursor;
		public ICursorType SystemCursor
		{
			get
			{
				if (FSystemCursor == null)
				{
					FSystemCursor = new CursorType(SystemTable);
					FSystemCursor.IsGeneric = true;
				}
				return FSystemCursor;
			}
		}
    }
    
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
		public Hashtable EmittedObjects = new Hashtable();
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
							((LibraryName == String.Empty) && ((AObject.Library == null) || (AObject.Library.Name == Server.Server.CSystemLibraryName))) ||
							(LibraryName != Server.Server.CSystemLibraryName)
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

