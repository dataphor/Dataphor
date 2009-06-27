/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using RealSQL = Alphora.Dataphor.DAE.Language.RealSQL;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Device.Catalog;
using Alphora.Dataphor.DAE.Device.ApplicationTransaction;

namespace Alphora.Dataphor.DAE.Server
{
	// ServerProcess
	public class ServerProcess : ServerChildObject, IServerProcess, IRemoteServerProcess
	{
		internal ServerProcess(ServerSession AServerSession) : base()
		{
			InternalCreate(AServerSession, new ProcessInfo(AServerSession.SessionInfo));
		}
		
		internal ServerProcess(ServerSession AServerSession, ProcessInfo AProcessInfo) : base()
		{
			InternalCreate(AServerSession, AProcessInfo);
		}
		
		private void InternalCreate(ServerSession AServerSession, ProcessInfo AProcessInfo)
		{
			FServerSession = AServerSession;
			FPlans = new ServerPlans();
			FExecutingPlans = new ServerPlans();
			FScripts = new ServerScripts();
			FParser = new Parser();
			FProcessID = GetHashCode();
			FProcessInfo = AProcessInfo;
			FDeviceSessions = new Schema.DeviceSessions(false);
			FStreamManager = (IStreamManager)this;
			FContext = new Context(FServerSession.SessionInfo.DefaultMaxStackDepth, FServerSession.SessionInfo.DefaultMaxCallDepth);
			FProcessPlan = new ServerStatementPlan(this);
			PushExecutingPlan(FProcessPlan);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServerSession.Server.FProcessCounter != null)
				FServerSession.Server.FProcessCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
	
		#if ALLOWPROCESSCONTEXT	
		protected void DeallocateContextVariables()
		{
			for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
			{
				DataVar LDataVar = FContext[LIndex];
				if ((LDataVar != null) && LDataVar.DataType.IsDisposable && (LDataVar.Value != null))
					((IDisposable)LDataVar.Value).Dispose();
			}
		}
		#endif
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				try
				{
					try
					{
						try
						{
							try
							{
								try
								{
									try
									{
										try
										{
											try
											{
												try
												{
													try
													{
														if (FApplicationTransactionID != Guid.Empty)
															LeaveApplicationTransaction();
													}
													finally
													{
														if (FTransactions != null)
														{
															// Rollback all active transactions
															while (InTransaction)
																RollbackTransaction();
																
															FTransactions.UnprepareDeferredConstraintChecks();
														}
													}
												}
												finally
												{
													if (FPlans != null)
														foreach (ServerPlanBase LPlan in FPlans)
														{
															if (LPlan.ActiveCursor != null)
																LPlan.ActiveCursor.Dispose();
														}
												}
											}
											finally
											{
												if (FContext != null)
												{
													#if ALLOWPROCESSCONTEXT
													DeallocateContextVariables();
													#endif
													FContext.Dispose();
													FContext = null;
												}
											}
										}
										finally
										{
											if (FDeviceSessions != null)
											{
												try
												{
													CloseDeviceSessions();
												}
												finally
												{
													FDeviceSessions.Dispose();
													FDeviceSessions = null;
												}
											}
										}
									}
									finally
									{
										if (FRemoteSessions != null)
										{
											try
											{
												CloseRemoteSessions();
											}
											finally
											{
												FRemoteSessions.Dispose();
												FRemoteSessions = null;
											}
										}
									}
								}
								finally
								{
									if (FTransactions != null)
									{
										FTransactions.Dispose();
										FTransactions = null;
									}
								}
							}
							finally
							{
								if (FExecutingPlans != null)
								{
									while (FExecutingPlans.Count > 0)
										FExecutingPlans.DisownAt(0);
									FExecutingPlans.Dispose();
									FExecutingPlans = null;
								}
							}
						}
						finally
						{
							if (FScripts != null)
							{
								try
								{
									UnprepareScripts();
								}
								finally
								{
									FScripts.Dispose();
									FScripts = null;
								}
							}
						}
					}
					finally
					{
						if (FPlans != null)
						{
							try
							{
								UnpreparePlans();
							}
							finally
							{
								FPlans.Dispose();
								FPlans = null;
							}
						}
						
						if (!FDisposed)
						{
							#if !DISABLE_PERFORMANCE_COUNTERS
							if (FServerSession.Server.FProcessCounter != null)
								FServerSession.Server.FProcessCounter.Decrement();
							#endif
							FDisposed = true;
						}
					}
				}
				finally
				{
					try
					{
						if (FProcessPlan != null)
						{
							FProcessPlan.Dispose();
							FProcessPlan = null;
						}
					}
					finally
					{
						ReleaseLocks();
					}
				}
			}
			finally
			{
				FSQLParser = null;
				FSQLCompiler = null;
				FStreamManager = null;
				FServerSession = null;
				FProcessID = -1;
				FParser = null;
				base.Dispose(ADisposing);
			}
		}
		
		// ServerSession
		private ServerSession FServerSession;
		public ServerSession ServerSession { get { return FServerSession; } }
		
		private ServerPlanBase FProcessPlan;
		
		// ProcessID
		private int FProcessID = -1;
		public int ProcessID { get { return FProcessID; } }
		
		// ProcessInfo
		private ProcessInfo FProcessInfo;
		public ProcessInfo ProcessInfo { get { return FProcessInfo; } }
		
		// Plan State Exposed from ExecutingPlan
		public Plan Plan { get { return ExecutingPlan.Plan; } }
		
		// Context		
		private Context FContext;
		public Context Context { get { return FContext; } }
		
		public Context SwitchContext(Context AContext)
		{
			Context LContext = FContext;
			FContext = AContext;
			return LContext;
		}
		
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
		[System.ComponentModel.DefaultValue(IsolationLevel.CursorStability)]
		[System.ComponentModel.Description("Determines the default isolation level for transactions on this process.")]
		public IsolationLevel DefaultIsolationLevel
		{
			get { return FProcessInfo.DefaultIsolationLevel; }
			set { FProcessInfo.DefaultIsolationLevel = value; }
		}
		
		/// <summary>Returns the isolation level of the current transaction, if one is active. Otherwise, returns the default isolation level.</summary>
		public IsolationLevel CurrentIsolationLevel()
		{
			return FTransactions.Count > 0 ? FTransactions.CurrentTransaction().IsolationLevel : DefaultIsolationLevel;
		}
		
		// NonLogged - Indicates whether logging is performed within the device.  
		// Is is an error to attempt non logged operations against a device that does not support non logged operations
		private int FNonLoggedCount;
		public bool NonLogged
		{
			get { return FNonLoggedCount > 0; }
			set { FNonLoggedCount += (value ? 1 : (NonLogged ? -1 : 0)); }
		}
		
		// IServerProcess.GetServerProcess()
		ServerProcess IServerProcess.GetServerProcess()
		{
			return this;
		}

		// IServerProcess.Session
		IServerSession IServerProcess.Session { get { return FServerSession; } }
		
		// IRemoteServerProcess.Session
		IRemoteServerSession IRemoteServerProcess.Session { get { return FServerSession; } }

		// Parsing
		private Parser FParser;		
		private RealSQL.Parser FSQLParser;
		private RealSQL.Compiler FSQLCompiler;
		
		private void EnsureSQLCompiler()
		{
			if (FSQLParser == null)
			{
				FSQLParser = new RealSQL.Parser();
				FSQLCompiler = new RealSQL.Compiler();
			}
		}

		public Statement ParseScript(string AScript, ParserMessages AMessages)
		{
			Statement LStatement;								  
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				LStatement = FSQLCompiler.Compile(FSQLParser.ParseScript(AScript));
			}
			else
				LStatement = FParser.ParseScript(AScript, AMessages);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LStatement;
		}
		
		public Statement ParseStatement(string AStatement, ParserMessages AMessages)
		{
			Statement LStatement;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				LStatement = FSQLCompiler.Compile(FSQLParser.ParseStatement(AStatement));
			}
			else
				LStatement = FParser.ParseStatement(AStatement, AMessages);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LStatement;
		}
		
		public Expression ParseExpression(string AExpression)
		{
			Expression LExpression;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginParse, "Begin Parse");
			#endif
			if (FServerSession.SessionInfo.Language == QueryLanguage.RealSQL)
			{
				EnsureSQLCompiler();
				Statement LStatement = FSQLCompiler.Compile(FSQLParser.ParseStatement(AExpression));
				if (LStatement is SelectStatement)
					LExpression = ((SelectStatement)LStatement).CursorDefinition;
				else
					throw new CompilerException(CompilerException.Codes.TableExpressionExpected);
			}
			else
				LExpression = FParser.ParseCursorDefinition(AExpression);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndParse, "End Parse");
			#endif
			return LExpression;
		}

		// Plans
		private ServerPlans FPlans;
		public ServerPlans Plans { get { return FPlans; } }
        
		private void UnpreparePlans()
		{
			Exception LException = null;
			while (FPlans.Count > 0)
			{
				try
				{
					ServerPlanBase LPlan = FPlans.DisownAt(0) as ServerPlanBase;
					if (!ServerSession.ReleaseCachedPlan(this, LPlan))
						LPlan.Dispose();
				}
				catch (Exception E)
				{
					LException = E;
				}
			}
			if (LException != null)
				throw LException;
		}
        
		// Scripts
		private ServerScripts FScripts;
		public ServerScripts Scripts { get { return FScripts; } }

		private void UnprepareScripts()
		{
			Exception LException = null;
			while (FScripts.Count > 0)
			{
				try
				{
					FScripts.DisownAt(0).Dispose();
				}
				catch (Exception E)
				{
					LException = E;
				}
			}
			if (LException != null)
				throw LException;
		}
		
		#if USEROWMANAGER
		// RowManager
		public RowManager RowManager { get { return FServerSession.Server.RowManager; } }
		#endif
		
		#if USESCALARMANAGER
		// ScalarManager
		public ScalarManager ScalarManager { get { return FServerSession.Server.ScalarManager; } }
		#endif
		
		// LockManager
		public void Lock(LockID ALockID, LockMode AMode)
		{
			FServerSession.Server.LockManager.Lock(FProcessID, ALockID, AMode);
		}
		
		public bool LockImmediate(LockID ALockID, LockMode AMode)
		{
			return FServerSession.Server.LockManager.LockImmediate(FProcessID, ALockID, AMode);
		}
		
		public void Unlock(LockID ALockID)
		{
			FServerSession.Server.LockManager.Unlock(FProcessID, ALockID);
		}
		
		// Release any locks associated with the process that have not yet been released
		private void ReleaseLocks()
		{
			// TODO: Release locks taken for a given process (presumably keep a hash table of locks acquired on this process, which means every request for a lock must go through the process)
			//FServerSession.Server.LockManager.Unlock(FProcessID);
		}

		// IStreamManager
		private IStreamManager FStreamManager;
		public IStreamManager StreamManager { get { return FStreamManager; } }
		
		StreamID IStreamManager.Allocate()
		{
			return ServerSession.Server.StreamManager.Allocate();
		}
		
		StreamID IStreamManager.Reference(StreamID AStreamID)
		{
			return ServerSession.Server.StreamManager.Reference(AStreamID);
		}
		
		void IStreamManager.Deallocate(StreamID AStreamID)
		{
			ServerSession.Server.StreamManager.Deallocate(AStreamID);
		}
		
		Stream IStreamManager.Open(StreamID AStreamID, LockMode ALockMode)
		{
			return ServerSession.Server.StreamManager.Open(FProcessID, AStreamID, ALockMode);
		}

		IRemoteStream IStreamManager.OpenRemote(StreamID AStreamID, LockMode ALockMode)
		{
			return ServerSession.Server.StreamManager.OpenRemote(FProcessID, AStreamID, ALockMode);
		}

		#if UNMANAGEDSTREAM
		void IStreamManager.Close(StreamID AStreamID)
		{
			ServerSession.Server.StreamManager.Close(FProcessID, AStreamID);
		}
		#endif
		
		// ClassLoader
		object IServerProcess.CreateObject(ClassDefinition AClassDefinition, object[] AArguments)
		{
			return FServerSession.Server.Catalog.ClassLoader.CreateObject(AClassDefinition, AArguments);
		}
		
		Type IServerProcess.CreateType(ClassDefinition AClassDefinition)
		{
			return FServerSession.Server.Catalog.ClassLoader.CreateType(AClassDefinition);
		}
		
		string IRemoteServerProcess.GetClassName(string AClassName)
		{
			return FServerSession.Server.Catalog.ClassLoader.Classes[AClassName].ClassName;
		}
		
		private void GetFileNames(Schema.Library ALibrary, StringCollection ALibraryNames, StringCollection AFileNames, ArrayList AFileDates)
		{
			foreach (Schema.FileReference LReference in ALibrary.Files)
				if (!AFileNames.Contains(LReference.FileName))
				{
					ALibraryNames.Add(ALibrary.Name);
					AFileNames.Add(LReference.FileName);
					AFileDates.Add(File.GetLastWriteTimeUtc(GetFullFileName(ALibrary, LReference.FileName)));
				} 
			
			foreach (Schema.LibraryReference LLibrary in ALibrary.Libraries)
				GetFileNames(FServerSession.Server.Catalog.Libraries[LLibrary.Name], ALibraryNames, AFileNames, AFileDates);
		}
		
		private void GetAssemblyFileNames(Schema.Library ALibrary, StringCollection AFileNames)
		{
			Schema.Libraries LLibraries = new Schema.Libraries();
			LLibraries.Add(ALibrary);
			
			while (LLibraries.Count > 0)
			{
				Schema.Library LLibrary = LLibraries[0];
				LLibraries.RemoveAt(0);

				foreach (Schema.FileReference LReference in LLibrary.Files)
					if (LReference.IsAssembly && !AFileNames.Contains(LReference.FileName))
						AFileNames.Add(LReference.FileName);
						
				foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
					if (!LLibraries.Contains(LReference.Name))
						LLibraries.Add(FServerSession.Server.Catalog.Libraries[LReference.Name]);
			}
		}
		
		void IRemoteServerProcess.GetFileNames(string AClassName, out string[] ALibraryNames, out string[] AFileNames, out DateTime[] AFileDates, out string[] AAssemblyFileNames)
		{
			Schema.RegisteredClass LClass = FServerSession.Server.Catalog.ClassLoader.Classes[AClassName];

			StringCollection LLibraryNames = new StringCollection();
			StringCollection LFileNames = new StringCollection();
			StringCollection LAssemblyFileNames = new StringCollection();
			ArrayList LFileDates = new ArrayList();
			
			// Build the list of all files required to load the assemblies in all libraries required by the library for the given class
			Schema.Library LLibrary = FServerSession.Server.Catalog.Libraries[LClass.Library.Name];
			GetFileNames(LLibrary, LLibraryNames, LFileNames, LFileDates);
			GetAssemblyFileNames(LLibrary, LAssemblyFileNames);
			
			ALibraryNames = new string[LLibraryNames.Count];
			LLibraryNames.CopyTo(ALibraryNames, 0);
			
			AFileNames = new string[LFileNames.Count];
			LFileNames.CopyTo(AFileNames, 0);
			
			AFileDates = new DateTime[LFileDates.Count];
			LFileDates.CopyTo(AFileDates, 0);
			
			// Return the results in reverse order to ensure that dependencies are loaded in the correct order
			AAssemblyFileNames = new string[LAssemblyFileNames.Count];
			for (int LIndex = LAssemblyFileNames.Count - 1; LIndex >= 0; LIndex--)
				AAssemblyFileNames[LAssemblyFileNames.Count - LIndex - 1] = LAssemblyFileNames[LIndex];
		}
		
		public string GetFullFileName(Schema.Library ALibrary, string AFileName)
		{
			#if LOADFROMLIBRARIES
			return 
				Path.IsPathRooted(AFileName) 
					? AFileName 
					: 
						ALibrary.Name == Server.CSystemLibraryName
							? PathUtility.GetFullFileName(AFileName)
							: Path.Combine(ALibrary.GetLibraryDirectory(FServerSession.Server.LibraryDirectory), AFileName);
			#else
			return PathUtility.GetFullFileName(AFileName);
			#endif
		}
		
		IRemoteStream IRemoteServerProcess.GetFile(string ALibraryName, string AFileName)
		{
			return new CoverStream(new FileStream(GetFullFileName(FServerSession.Server.Catalog.Libraries[ALibraryName], AFileName), FileMode.Open, FileAccess.Read, FileShare.Read), true);
		}
		
		// DeviceSessions		
		private Schema.DeviceSessions FDeviceSessions;
		internal Schema.DeviceSessions DeviceSessions { get { return FDeviceSessions; } }

		public Schema.DeviceSession DeviceConnect(Schema.Device ADevice)
		{
			int LIndex = DeviceSessions.IndexOf(ADevice);
			if (LIndex < 0)
			{
				EnsureDeviceStarted(ADevice);
				Schema.DeviceSession LSession = ADevice.Connect(this, ServerSession.SessionInfo);
				try
				{
					#if REFERENCECOUNTDEVICESESSIONS
					LSession.FReferenceCount = 1;
					#endif
					while (LSession.Transactions.Count < FTransactions.Count)
						LSession.BeginTransaction(FTransactions[FTransactions.Count - 1].IsolationLevel);
					DeviceSessions.Add(LSession);
					return LSession;
				}
				catch
				{
					ADevice.Disconnect(LSession);
					throw;
				}
			}
			else
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[LIndex].FReferenceCount++;
				#endif
				return DeviceSessions[LIndex];
			}
		}
		
		internal void CloseDeviceSessions()
		{
			while (DeviceSessions.Count > 0)
				DeviceSessions[0].Device.Disconnect(DeviceSessions[0]);
		}
		
		public void DeviceDisconnect(Schema.Device ADevice)
		{
			int LIndex = DeviceSessions.IndexOf(ADevice);
			if (LIndex >= 0)
			{
				#if REFERENCECOUNTDEVICESESSIONS
				DeviceSessions[LIndex].FReferenceCount--;
				if (DeviceSessions[LIndex].FReferenceCount == 0)
				#endif
				ADevice.Disconnect(DeviceSessions[LIndex]);
			}
		}
		
		public DataVar DeviceExecute(Schema.Device ADevice, PlanNode APlanNode)
		{	
			if (IsReconciliationEnabled() || (APlanNode.DataType != null))
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				try
				{
					return DeviceConnect(ADevice).Execute(Plan.GetDevicePlan(APlanNode));
				}
				finally
				{
					RootExecutingPlan.Statistics.DeviceExecuteTime += TimingUtility.TimeSpanFromTicks(LStartTicks);
				}
			}

			return null;
		}
		
		public void EnsureDeviceStarted(Schema.Device ADevice)
		{
			ServerSession.Server.StartDevice(this, ADevice);
		}
		
		// CatalogDeviceSession
		public CatalogDeviceSession CatalogDeviceSession
		{
			get
			{
				CatalogDeviceSession LSession = DeviceConnect(ServerSession.Server.CatalogDevice) as CatalogDeviceSession;
				Error.AssertFail(LSession != null, "Could not connect to catalog device");
				return LSession;
			}
		}
		
		// RemoteSessions
		private RemoteSessions FRemoteSessions;
		internal RemoteSessions RemoteSessions
		{
			get 
			{ 
				if (FRemoteSessions == null)
					FRemoteSessions = new RemoteSessions();
				return FRemoteSessions; 
			}
		}
		
		internal RemoteSession RemoteConnect(Schema.ServerLink AServerLink)
		{
			int LIndex = RemoteSessions.IndexOf(AServerLink);
			if (LIndex < 0)
			{
				RemoteSession LSession = new RemoteSession(this, AServerLink);
				try
				{
					while (LSession.TransactionCount < FTransactions.Count)
						LSession.BeginTransaction(FTransactions[FTransactions.Count - 1].IsolationLevel);
					RemoteSessions.Add(LSession);
					return LSession;
				}
				catch
				{
					LSession.Dispose();
					throw;
				}
			}
			else
				return RemoteSessions[LIndex];
		}
		
		internal void RemoteDisconnect(Schema.ServerLink AServerLink)
		{
			int LIndex = RemoteSessions.IndexOf(AServerLink);
			if (LIndex >= 0)
				RemoteSessions[LIndex].Dispose();
			else
				throw new ServerException(ServerException.Codes.NoRemoteSessionForServerLink, AServerLink.Name);
		}
		
		internal void CloseRemoteSessions()
		{
			while (RemoteSessions.Count > 0)
				RemoteSessions[0].Dispose();
		}
		
		// Tracing
		#if TRACEEVENTS
		public void RaiseTraceEvent(string ATraceCode, string ADescription)
		{
			FServerSession.Server.RaiseTraceEvent(this, ATraceCode, ADescription);
		}
		#endif
		
		// Plans
		/// <summary>Indicates whether or not warnings encountered during compilation of plans on this process will be reported.</summary>
		public bool SuppressWarnings
		{
			get { return FProcessInfo.SuppressWarnings; }
			set { FProcessInfo.SuppressWarnings = value; }
		}
		
		private void CleanupPlans(ProcessCleanupInfo ACleanupInfo)
		{
			int LPlanIndex;
			for (int LIndex = 0; LIndex < ACleanupInfo.UnprepareList.Length; LIndex++)
			{
				LPlanIndex = FPlans.IndexOf((ServerChildObject)ACleanupInfo.UnprepareList[LIndex]);
				if (LPlanIndex >= 0)
					InternalUnprepare(FPlans[LPlanIndex]);
			}
		}

		#if ALLOWPROCESSCONTEXT
		private void PushProcessContext(Plan APlan)
		{
			APlan.Symbols.PushFrame();
			for (int LIndex = FContext.Count - 1; LIndex >= 0; LIndex--)
				APlan.Symbols.Push(FContext[LIndex]);
			if (FContext.AllowExtraWindowAccess)
				APlan.Symbols.AllowExtraWindowAccess = true;
		}
		
		private void PopProcessContext(Plan APlan)
		{
			APlan.Symbols.PopFrame();
			if (FContext.AllowExtraWindowAccess)
				APlan.Symbols.AllowExtraWindowAccess = false;
		}
		#endif
		
		private void Compile(ServerPlanBase AServerPlan, Statement AStatement, DataParams AParams, bool AIsExpression)
		{
			PushExecutingPlan(AServerPlan);
			try
			{
				#if ALLOWPROCESSCONTEXT
				// Push process context on the symbols for the plan
				PushProcessContext(AServerPlan.Plan);
				try
				{
				#endif
					#if TIMING
					DateTime LStartTime = DateTime.Now;
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile", DateTime.Now.ToString("hh:mm:ss.ffff")));
					#endif
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginCompile, "Begin Compile");
					#endif
					Schema.DerivedTableVar LTableVar = null;
					LTableVar = new Schema.DerivedTableVar(Schema.Object.GetNextObjectID(), Schema.Object.NameFromGuid(AServerPlan.ID));
					LTableVar.SessionObjectName = LTableVar.Name;
					LTableVar.SessionID = AServerPlan.ServerProcess.ServerSession.SessionID;
					AServerPlan.Plan.PushCreationObject(LTableVar);
					try
					{
						AServerPlan.Code = Compiler.Compile(AServerPlan.Plan, AStatement, AParams, AIsExpression);

						// Include dependencies for any process context that may be being referenced by the plan
						// This is necessary to support the variable declaration statements that will be added to support serialization of the plan
						for (int LIndex = Context.Count - 1; LIndex >= 0; LIndex--)
							if ((AParams == null) || !AParams.Contains(Context[LIndex].Name))
								AServerPlan.Plan.PlanCatalog.IncludeDependencies(this, AServerPlan.Plan.Catalog, Context[LIndex].DataType, EmitMode.ForRemote);

						if (!AServerPlan.Plan.Messages.HasErrors && AIsExpression)
						{
							if (AServerPlan.Code.DataType == null)
								throw new CompilerException(CompilerException.Codes.ExpressionExpected);
							AServerPlan.DataType = CopyDataType(AServerPlan, LTableVar);
						}
					}
					finally
					{
						AServerPlan.Plan.PopCreationObject();
					}
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndCompile, "End Compile");
					#endif
					#if TIMING
					System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile -- Compile Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
					#endif
				#if ALLOWPROCESSCONTEXT
				}
				finally
				{
					// Pop process context from the plan
					PopProcessContext(AServerPlan.Plan);
				}
				#endif
			}
			finally
			{
				PopExecutingPlan(AServerPlan);
			}
		}
        
		internal ServerPlanBase CompileStatement(Statement AStatement, ParserMessages AMessages, DataParams AParams)
		{
			ServerPlanBase LPlan = new ServerStatementPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AStatement, AParams, false);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}

		internal PlanDescriptor GetPlanDescriptor(IRemoteServerStatementPlan APlan)
		{
			PlanDescriptor LDescriptor = new PlanDescriptor();
			LDescriptor.ID = APlan.ID;
			LDescriptor.CacheTimeStamp = APlan.Process.Session.Server.CacheTimeStamp;
			LDescriptor.Statistics = APlan.Statistics;
			LDescriptor.Messages = APlan.Messages;
			return LDescriptor;
		}
		
		private void InternalUnprepare(ServerPlanBase APlan)
		{
			BeginCall();
			try
			{
				FPlans.Disown(APlan);
				if (!ServerSession.ReleaseCachedPlan(this, APlan))
					APlan.Dispose();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		private int GetContextHashCode(DataParams AParams)
		{
			StringBuilder LContextName = new StringBuilder();
			if (FContext != null)
				for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
					LContextName.Append(FContext.Peek(LIndex).DataType.Name);
			if (AParams != null)
				for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
					LContextName.Append(AParams[LIndex].DataType.Name);
			return LContextName.ToString().GetHashCode();
		}
		
		// IServerProcess.PrepareStatement        
		IServerStatementPlan IServerProcess.PrepareStatement(string AStatement, DataParams AParams)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerStatementPlan LPlan = ServerSession.GetCachedPlan(this, AStatement, LContextHashCode) as IServerStatementPlan;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
				#endif
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					ParserMessages LMessages = new ParserMessages();
					Statement LStatement = ParseStatement(AStatement, LMessages);
					LPlan = (IServerStatementPlan)CompileStatement(LStatement, LMessages, AParams);
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AStatement, LContextHashCode, (ServerPlanBase)LPlan);
				}
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
				#endif
				return LPlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// IServerProcess.UnprepareStatement
		void IServerProcess.UnprepareStatement(IServerStatementPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
        
		internal ServerPlanBase CompileRemoteStatement(Statement AStatement, ParserMessages AMessages, RemoteParam[] AParams)
		{
			ServerPlanBase LPlan = new RemoteServerStatementPlan(this);
			try
			{
				DataParams LParams = RemoteParamsToDataParams(AParams);
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AStatement, LParams, false);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		// IServerProcess.Execute
		void IServerProcess.Execute(string AStatement, DataParams AParams)
		{
			IServerStatementPlan LStatementPlan = ((IServerProcess)this).PrepareStatement(AStatement, AParams);
			try
			{
				LStatementPlan.Execute(AParams);
			}
			finally
			{
				((IServerProcess)this).UnprepareStatement(LStatementPlan);
			}
		}
		
		private int GetContextHashCode(RemoteParam[] AParams)
		{
			StringBuilder LContextName = new StringBuilder();
			if (FContext != null)
				for (int LIndex = 0; LIndex < FContext.Count; LIndex++)
					LContextName.Append(FContext.Peek(LIndex).DataType.Name);
			if (AParams != null)
				for (int LIndex = 0; LIndex < AParams.Length; LIndex++)
					LContextName.Append(AParams[LIndex].TypeName);
			return LContextName.ToString().GetHashCode();
		}
		
		// IRemoteServerProcess.PrepareStatement
		IRemoteServerStatementPlan IRemoteServerProcess.PrepareStatement(string AStatement, RemoteParam[] AParams, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			BeginCall();
			try
			{
				CleanupPlans(ACleanupInfo);
				int LContextHashCode = GetContextHashCode(AParams);
				IRemoteServerStatementPlan LPlan = ServerSession.GetCachedPlan(this, AStatement, LContextHashCode) as IRemoteServerStatementPlan;
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
					#endif
					ParserMessages LMessages = new ParserMessages();
					Statement LStatement = ParseStatement(AStatement, LMessages);
					LPlan = (IRemoteServerStatementPlan)CompileRemoteStatement(LStatement, LMessages, AParams);
					if (!((ServerPlanBase)LPlan).Plan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AStatement, LContextHashCode, (ServerPlanBase)LPlan);
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
					#endif
				}
				APlanDescriptor = GetPlanDescriptor(LPlan);
				return LPlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServerProcess.UnprepareStatement
		void IRemoteServerProcess.UnprepareStatement(IRemoteServerStatementPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IRemoteServerProcess.Execute
		void IRemoteServerProcess.Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo)
		{
			ProcessCallInfo(ACallInfo);
			PlanDescriptor LDescriptor;
			IRemoteServerStatementPlan LPlan = ((IRemoteServerProcess)this).PrepareStatement(AStatement, AParams.Params, out LDescriptor, ACleanupInfo);
			try
			{
				TimeSpan LExecuteTime;
				LPlan.Execute(ref AParams, out LExecuteTime, EmptyCallInfo());
				LDescriptor.Statistics.ExecuteTime = LExecuteTime;
			}
			finally
			{
				((IRemoteServerProcess)this).UnprepareStatement(LPlan);
			}
		}
        
		private Schema.IDataType CopyDataType(ServerPlanBase APlan, Schema.DerivedTableVar ATableVar)
		{
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerProcess.CopyDataType", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			if (APlan.Code.DataType is Schema.CursorType)
			{
				TableNode LSourceNode = (TableNode)APlan.Code.Nodes[0];	
				Schema.TableVar LSourceTableVar = LSourceNode.TableVar;

				AdornExpression LAdornExpression = new AdornExpression();
				LAdornExpression.MetaData = new MetaData();
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsChangeRemotable", (LSourceTableVar.IsChangeRemotable || !LSourceTableVar.ShouldChange).ToString()));
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsDefaultRemotable", (LSourceTableVar.IsDefaultRemotable || !LSourceTableVar.ShouldDefault).ToString()));
				LAdornExpression.MetaData.Tags.Add(new Tag("DAE.IsValidateRemotable", (LSourceTableVar.IsValidateRemotable || !LSourceTableVar.ShouldValidate).ToString()));

				if (LSourceNode.Order != null)
				{
					if (!LSourceTableVar.Orders.Contains(LSourceNode.Order))
						LSourceTableVar.Orders.Add(LSourceNode.Order);
					LAdornExpression.MetaData.Tags.Add(new Tag("DAE.DefaultOrder", LSourceNode.Order.Name));
				}
				
				foreach (Schema.TableVarColumn LColumn in LSourceNode.TableVar.Columns)
				{
					if (!((LColumn.IsDefaultRemotable || !LColumn.ShouldDefault) && (LColumn.IsValidateRemotable || !LColumn.ShouldValidate) && (LColumn.IsChangeRemotable || !LColumn.ShouldChange)))
					{
						AdornColumnExpression LColumnExpression = new AdornColumnExpression();
						LColumnExpression.ColumnName = Schema.Object.EnsureRooted(LColumn.Name);
						LColumnExpression.MetaData = new MetaData();
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsChangeRemotable", (LColumn.IsChangeRemotable || !LColumn.ShouldChange).ToString());
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsDefaultRemotable", (LColumn.IsDefaultRemotable || !LColumn.ShouldDefault).ToString());
						LColumnExpression.MetaData.Tags.AddOrUpdate("DAE.IsValidateRemotable", (LColumn.IsValidateRemotable || !LColumn.ShouldValidate).ToString());
						LAdornExpression.Expressions.Add(LColumnExpression);
					}
				}
				
				PlanNode LViewNode = LSourceNode;
				while (LViewNode is BaseOrderNode)
					LViewNode = LViewNode.Nodes[0];
					
				LViewNode = Compiler.EmitAdornNode(APlan.Plan, LViewNode, LAdornExpression);

				ATableVar.CopyTableVar((TableNode)LViewNode);
				ATableVar.CopyReferences((TableNode)LViewNode);
				ATableVar.InheritMetaData(((TableNode)LViewNode).TableVar.MetaData);
				#if USEORIGINALEXPRESSION
				ATableVar.OriginalExpression = (Expression)LViewNode.EmitStatement(EmitMode.ForRemote);
				#else
				ATableVar.InvocationExpression = (Expression)LViewNode.EmitStatement(EmitMode.ForRemote);
				#endif
				
				// Gather AT dependencies
				// Each AT object will be emitted remotely as the underlying object, so report the dependency so that the catalog emission happens in the correct order.
				Schema.Objects LATObjects = new Schema.Objects();
				if (ATableVar.HasDependencies())
					for (int LIndex = 0; LIndex < ATableVar.Dependencies.Count; LIndex++)
					{
						Schema.Object LObject = ATableVar.Dependencies.ResolveObject(APlan.ServerProcess, LIndex);
						if ((LObject != null) && LObject.IsATObject && ((LObject is Schema.TableVar) || (LObject is Schema.Operator)))
							LATObjects.Add(LObject);
					}

				foreach (Schema.Object LATObject in LATObjects)
				{
					Schema.TableVar LATTableVar = LATObject as Schema.TableVar;
					if (LATTableVar != null)
					{
						Schema.TableVar LTableVar = (Schema.TableVar)APlan.Plan.Catalog[APlan.Plan.Catalog.IndexOfName(LATTableVar.SourceTableName)];
						ATableVar.Dependencies.Ensure(LTableVar);
						continue;
					}

					Schema.Operator LATOperator = LATObject as Schema.Operator;
					if (LATOperator != null)
					{
						ATableVar.Dependencies.Ensure(ServerSession.Server.ATDevice.ResolveSourceOperator(this, LATOperator));
					}
				}
				
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.Plan.PlanCatalog.Remove(ATableVar);
				APlan.Plan.PlanCatalog.Add(ATableVar);
				return ATableVar.DataType;
			}
			else
			{
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.Plan.PlanCatalog.SafeRemove(ATableVar);
				APlan.Plan.PlanCatalog.IncludeDependencies(this, APlan.Plan.Catalog, APlan.Code.DataType, EmitMode.ForRemote);
				return APlan.Code.DataType;
			}
		}

		internal ServerPlanBase CompileExpression(Statement AExpression, ParserMessages AMessages, DataParams AParams)
		{
			ServerPlanBase LPlan = new ServerExpressionPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AExpression, AParams, true);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		internal PlanDescriptor GetPlanDescriptor(IRemoteServerExpressionPlan APlan, RemoteParam[] AParams)
		{
			PlanDescriptor LDescriptor = new PlanDescriptor();
			LDescriptor.ID = APlan.ID;
			LDescriptor.Statistics = APlan.Statistics;
			LDescriptor.Messages = APlan.Messages;
			if (((ServerPlanBase)APlan).DataType is Schema.ITableType)
			{
				LDescriptor.Capabilities = APlan.Capabilities;
				LDescriptor.CursorIsolation = APlan.Isolation;
				LDescriptor.CursorType = APlan.CursorType;
				if (((TableNode)((ServerPlanBase)APlan).Code.Nodes[0]).Order != null)
					LDescriptor.Order = ((TableNode)((ServerPlanBase)APlan).Code.Nodes[0]).Order.Name;
				else
					LDescriptor.Order = String.Empty;
			}
			LDescriptor.Catalog = ((RemoteServerExpressionPlan)APlan).GetCatalog(AParams, out LDescriptor.ObjectName, out LDescriptor.CacheTimeStamp, out LDescriptor.ClientCacheTimeStamp, out LDescriptor.CacheChanged);
			return LDescriptor;
		}
		
		// IServerProcess.PrepareExpression
		IServerExpressionPlan IServerProcess.PrepareExpression(string AExpression, DataParams AParams)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerExpressionPlan LPlan = ServerSession.GetCachedPlan(this, AExpression, LContextHashCode) as IServerExpressionPlan;
				if (LPlan != null)
					FPlans.Add(LPlan);
				else
				{
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
					#endif
					ParserMessages LMessages = new ParserMessages();
					LPlan = (IServerExpressionPlan)CompileExpression(ParseExpression(AExpression), LMessages, AParams);
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AExpression, LContextHashCode, (ServerPlanBase)LPlan);
					#if TRACEEVENTS
					RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
					#endif
				}
				return LPlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// IServerProcess.UnprepareExpression
		void IServerProcess.UnprepareExpression(IServerExpressionPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IServerProcess.Evaluate
		DataValue IServerProcess.Evaluate(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = ((IServerProcess)this).PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Evaluate(AParams);
			}
			finally
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
			}
		}
		
		// IServerProcess.OpenCursor
		IServerCursor IServerProcess.OpenCursor(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = ((IServerProcess)this).PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Open(AParams);
			}
			catch
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
				throw;
			}
		}
		
		// IServerProcess.CloseCursor
		void IServerProcess.CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor);
			}
			finally
			{
				((IServerProcess)this).UnprepareExpression(LPlan);
			}
		}
        
		internal ServerPlanBase CompileRemoteExpression(Statement AExpression, ParserMessages AMessages, RemoteParam[] AParams)
		{
			ServerPlanBase LPlan = new RemoteServerExpressionPlan(this);
			try
			{
				DataParams LParams = RemoteParamsToDataParams(AParams);
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan, AExpression, LParams, true);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch 
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		private IRemoteServerExpressionPlan InternalPrepareRemoteExpression(string AExpression, RemoteParam[] AParams, ProcessCleanupInfo ACleanupInfo)
		{
			CleanupPlans(ACleanupInfo);
			int LContextHashCode = GetContextHashCode(AParams);
			IRemoteServerExpressionPlan LPlan = ServerSession.GetCachedPlan(this, AExpression, LContextHashCode) as IRemoteServerExpressionPlan;
			if (LPlan != null)
				FPlans.Add(LPlan);
			else
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepare, "Begin Prepare");
				#endif
				ParserMessages LMessages = new ParserMessages();
				LPlan = (IRemoteServerExpressionPlan)CompileRemoteExpression(ParseExpression(AExpression), LMessages, AParams);
				if (!((ServerPlanBase)LPlan).Plan.Messages.HasErrors)
					ServerSession.AddCachedPlan(this, AExpression, LContextHashCode, (ServerPlanBase)LPlan);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepare, "End Prepare");
				#endif
			}
			return LPlan;
		}
		
		// IRemoteServerProcess.PrepareExpression
		IRemoteServerExpressionPlan IRemoteServerProcess.PrepareExpression(string AExpression, RemoteParam[] AParams, out PlanDescriptor APlanDescriptor, ProcessCleanupInfo ACleanupInfo)
		{
			BeginCall();
			try
			{
				IRemoteServerExpressionPlan LPlan = InternalPrepareRemoteExpression(AExpression, AParams, ACleanupInfo);
				APlanDescriptor = GetPlanDescriptor(LPlan, AParams);
				return LPlan;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// IRemoteServerProcess.UnprepareExpression
		void IRemoteServerProcess.UnprepareExpression(IRemoteServerExpressionPlan APlan)
		{
			InternalUnprepare((ServerPlanBase)APlan);
		}
		
		// IRemoteServerProcess.Evaluate
		byte[] IRemoteServerProcess.Evaluate
		(
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo
		)
		{
			ProcessCallInfo(ACallInfo);
			
			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
			
			#if LOGCACHEEVENTS
			FServerSession.Server.LogMessage(String.Format("Evaluating '{0}'.", AExpression));
			#endif
			
			try
			{
				TimeSpan LExecuteTime;
				byte[] LResult = APlan.Evaluate(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				
				#if LOGCACHEEVENTS
				FServerSession.Server.LogMessage("Expression evaluated.");
				#endif

				return LResult;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				throw;
			}
		}
		
		// IRemoteServerProcess.OpenCursor
		IRemoteServerCursor IRemoteServerProcess.OpenCursor
		(
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo
		)
		{
			ProcessCallInfo(ACallInfo);

			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}

			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// IRemoteServerProcess.OpenCursor
		IRemoteServerCursor IRemoteServerProcess.OpenCursor
		(
			string AExpression,
			ref RemoteParamData AParams,
			out IRemoteServerExpressionPlan APlan,
			out PlanDescriptor APlanDescriptor,
			ProcessCallInfo ACallInfo,
			ProcessCleanupInfo ACleanupInfo,
			out Guid[] ABookmarks,
			int ACount,
			out RemoteFetchData AFetchData
		)
		{
			ProcessCallInfo(ACallInfo);

			BeginCall();
			try
			{
				APlan = InternalPrepareRemoteExpression(AExpression, AParams.Params, ACleanupInfo);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
			
			try
			{
				TimeSpan LExecuteTime;
				IRemoteServerCursor LCursor = APlan.Open(ref AParams, out LExecuteTime, EmptyCallInfo());
				AFetchData = LCursor.Fetch(out ABookmarks, ACount, EmptyCallInfo());
				APlanDescriptor = GetPlanDescriptor(APlan, AParams.Params);
				APlanDescriptor.Statistics.ExecuteTime = LExecuteTime;
				return LCursor;
			}
			catch
			{
				((IRemoteServerProcess)this).UnprepareExpression(APlan);
				APlan = null;
				throw;
			}
		}
		
		// IRemoteServerProcess.CloseCursor
		void IRemoteServerProcess.CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			IRemoteServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor, ACallInfo);
			}
			finally
			{
				((IRemoteServerProcess)this).UnprepareExpression(LPlan);
			}
		}
		
		private ServerScript InternalPrepareScript(string AScript)
		{
			BeginCall();
			try
			{
				ServerScript LScript = new ServerScript(this, AScript);
				try
				{
					FScripts.Add(LScript);
					return LScript;
				}
				catch
				{
					LScript.Dispose();
					throw;
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		private void InternalUnprepareScript(ServerScript AScript)
		{
			BeginCall();
			try
			{
				AScript.Dispose();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// IServerProcess.PrepareScript
		IServerScript IServerProcess.PrepareScript(string AScript)
		{
			return (IServerScript)InternalPrepareScript(AScript);
		}
        
		// IServerProcess.UnprepareScript
		void IServerProcess.UnprepareScript(IServerScript AScript)
		{
			InternalUnprepareScript((ServerScript)AScript);
		}
		
		// IServerProcess.ExecuteScript
		void IServerProcess.ExecuteScript(string AScript)
		{
			IServerScript LScript = ((IServerProcess)this).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)this).UnprepareScript(LScript);
			}
		}
        
		// IRemoteServerProcess.PrepareScript
		IRemoteServerScript IRemoteServerProcess.PrepareScript(string AScript)
		{
			return (IRemoteServerScript)InternalPrepareScript(AScript);
		}
        
		// IRemoteServerProcess.UnprepareScript
		void IRemoteServerProcess.UnprepareScript(IRemoteServerScript AScript)
		{
			InternalUnprepareScript((ServerScript)AScript);
		}
		
		// IRemoteServerProcess.ExecuteScript
		void IRemoteServerProcess.ExecuteScript(string AScript, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			IServerScript LScript = ((IServerProcess)this).PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				((IServerProcess)this).UnprepareScript(LScript);
			}
		}

		// Transaction		
		private ServerDTCTransaction FDTCTransaction;
		private ServerTransactions FTransactions = new ServerTransactions();
		internal ServerTransactions Transactions { get { return FTransactions; } }

		internal ServerTransaction CurrentTransaction { get { return FTransactions.CurrentTransaction(); } }
		internal ServerTransaction RootTransaction { get { return FTransactions.RootTransaction(); } }
		
		internal void AddInsertTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			FTransactions.AddInsertTableVarCheck(ATableVar, ARow);
		}
		
		internal void AddUpdateTableVarCheck(Schema.TableVar ATableVar, Row AOldRow, Row ANewRow)
		{
			FTransactions.AddUpdateTableVarCheck(ATableVar, AOldRow, ANewRow);
		}
		
		internal void AddDeleteTableVarCheck(Schema.TableVar ATableVar, Row ARow)
		{
			FTransactions.AddDeleteTableVarCheck(ATableVar, ARow);
		}
		
		internal void RemoveDeferredConstraintChecks(Schema.TableVar ATableVar)
		{
			if (FTransactions != null)
				FTransactions.RemoveDeferredConstraintChecks(ATableVar);
		}
		
		internal void RemoveDeferredHandlers(Schema.EventHandler AHandler)
		{
			if (FTransactions != null)
				FTransactions.RemoveDeferredHandlers(AHandler);
		}
		
		internal void RemoveCatalogConstraintCheck(Schema.CatalogConstraint AConstraint)
		{
			if (FTransactions != null)
				FTransactions.RemoveCatalogConstraintCheck(AConstraint);
		}
		
		// TransactionCount
		public int TransactionCount { get { return FTransactions.Count; } }

		// InTransaction
		public bool InTransaction { get { return FTransactions.Count > 0; } }

		private void CheckInTransaction()
		{
			if (!InTransaction)
				throw new ServerException(ServerException.Codes.NoTransactionActive);
		}
        
		// UseDTC
		public bool UseDTC
		{
			get { return FProcessInfo.UseDTC; }
			set
			{
				lock (this)
				{
					if (InTransaction)
						throw new ServerException(ServerException.Codes.TransactionActive);

					if (value && (Environment.OSVersion.Version.Major < 5))
						throw new ServerException(ServerException.Codes.DTCNotSupported);
					FProcessInfo.UseDTC = value;
				}
			}
		}
		
		// UseImplicitTransactions
		public bool UseImplicitTransactions
		{
			get { return FProcessInfo.UseImplicitTransactions; }
			set { FProcessInfo.UseImplicitTransactions = value; }
		}

		// BeginTransaction
		public void BeginTransaction(IsolationLevel AIsolationLevel)
		{
			BeginCall();
			try
			{
				InternalBeginTransaction(AIsolationLevel);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// PrepareTransaction
		public void PrepareTransaction()
		{
			BeginCall();
			try
			{
				InternalPrepareTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
        
		// CommitTransaction
		public void CommitTransaction()
		{
			BeginCall();
			try
			{
				InternalCommitTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}

		// RollbackTransaction        
		public void RollbackTransaction()
		{
			BeginCall();
			try
			{
				InternalRollbackTransaction();
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
			finally
			{
				EndCall();
			}
		}
    
		protected internal void InternalBeginTransaction(IsolationLevel AIsolationLevel)
		{
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginBeginTransaction, "Begin BeginTransaction");
			#endif
			FTransactions.BeginTransaction(this, AIsolationLevel);

			if (UseDTC && (FDTCTransaction == null))
			{
				CloseDeviceSessions();
				FDTCTransaction = new ServerDTCTransaction();
				FDTCTransaction.IsolationLevel = AIsolationLevel;
			}
			
			// Begin a transaction on all remote sessions
			foreach (RemoteSession LRemoteSession in RemoteSessions)
				LRemoteSession.BeginTransaction(AIsolationLevel);

			// Begin a transaction on all device sessions
			foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
				LDeviceSession.BeginTransaction(AIsolationLevel);
				
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndBeginTransaction, "End BeginTransaction");
			#endif
		}
		
		protected internal void InternalPrepareTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginPrepareTransaction, "Begin PrepareTransaction");
			#endif

			ServerTransaction LTransaction = FTransactions.CurrentTransaction();
			if (!LTransaction.Prepared)
			{
				foreach (RemoteSession LRemoteSession in RemoteSessions)
					LRemoteSession.PrepareTransaction();
					
				foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
					LDeviceSession.PrepareTransaction();
					
				// Invoke event handlers for work done during this transaction
				LTransaction.InvokeDeferredHandlers(this);

				// Validate Constraints which have been affected by work done during this transaction
				FTransactions.ValidateDeferredConstraints(this);

				foreach (Schema.CatalogConstraint LConstraint in LTransaction.CatalogConstraints)
					LConstraint.Validate(this);
				
				LTransaction.Prepared = true;
			}

			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndPrepareTransaction, "End PrepareTransaction");
			#endif
		}
		
		protected internal void InternalCommitTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginCommitTransaction, "Begin CommitTransaction");
			#endif

			// Prepare Commit Phase
			InternalPrepareTransaction();

			try
			{
				// Commit the DTC if this is the Top Level Transaction
				if (UseDTC && (FTransactions.Count == 1))
				{
					try
					{
						FDTCTransaction.Commit();
					}
					finally
					{
						FDTCTransaction.Dispose();
						FDTCTransaction = null;
					}
				}
			}
			catch
			{
				RollbackTransaction();
				throw;
			}
			
			// Commit
			foreach (RemoteSession LRemoteSession in RemoteSessions)
				LRemoteSession.CommitTransaction();

			Schema.DeviceSession LCatalogDeviceSession = null;
			foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
			{
				if (LDeviceSession is CatalogDeviceSession)
					LCatalogDeviceSession = LDeviceSession;
				else
					LDeviceSession.CommitTransaction();
			}
			
			// Commit the catalog device session last. The catalog device session tracks devices
			// dropped during the session, and will defer the stop of those devices until after the
			// transaction has committed, allowing any active transactions in that device to commit.
			if (LCatalogDeviceSession != null)
				LCatalogDeviceSession.CommitTransaction();
				
			FTransactions.EndTransaction(true);
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndCommitTransaction, "End CommitTransaction");
			#endif
		}

		protected internal void InternalRollbackTransaction()
		{
			CheckInTransaction();
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginRollbackTransaction, "Begin RollbackTransaction");
			#endif
			try
			{
				FTransactions.CurrentTransaction().InRollback = true;
				try
				{
					if (UseDTC && (FTransactions.Count == 1))
					{
						try
						{
							FDTCTransaction.Rollback();
						}
						finally
						{
							FDTCTransaction.Dispose();
							FDTCTransaction = null;
						}
					}

					Exception LException = null;
					
					foreach (RemoteSession LRemoteSession in RemoteSessions)
					{
						try
						{
							if (LRemoteSession.InTransaction)
								LRemoteSession.RollbackTransaction();
						}
						catch (Exception E)
						{
							LException = E;
							ServerSession.Server.LogError(E);
						}
					}
					
					Schema.DeviceSession LCatalogDeviceSession = null;
					foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
					{
						try
						{
							if (LDeviceSession is CatalogDeviceSession)
								LCatalogDeviceSession = LDeviceSession;
							else
								if (LDeviceSession.InTransaction)
									LDeviceSession.RollbackTransaction();
						}
						catch (Exception E)
						{
							LException = E;
							this.ServerSession.Server.LogError(E);
						}
					}
					
					try
					{
						// Catalog device session transaction must be rolled back last
						// Well, it's not really correct to roll it back last, or first, or in any particular order,
						// but I don't have another option unless I start managing all transaction logging for
						// all devices, so rolling it back last seems like the best option (even at that I have
						// to ignore errors that occur during the rollback - see CatalogDeviceSession.InternalRollbackTransaction).
						if ((LCatalogDeviceSession != null) && LCatalogDeviceSession.InTransaction)
							LCatalogDeviceSession.RollbackTransaction();
					}
					catch (Exception E)
					{
						LException = E;
						this.ServerSession.Server.LogError(E);
					}
					
					if (LException != null)
						throw LException;
				}
				finally
				{
					FTransactions.CurrentTransaction().InRollback = false;
				}
			}
			finally
			{
				FTransactions.EndTransaction(false);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndRollbackTransaction, "End RollbackTransaction");
				#endif
			}
		}
		
		// Application Transactions
		private Guid FApplicationTransactionID = Guid.Empty;
		public Guid ApplicationTransactionID { get { return FApplicationTransactionID; } }
		
		private bool FIsInsert;
		/// <summary>Indicates whether this process is an insert participant in an application transaction.</summary>
		public bool IsInsert 
		{ 
			get { return FIsInsert; } 
			set { FIsInsert = value; }
		}
		
		private bool FIsOpeningInsertCursor;
		/// <summary>Indicates whether this process is currently opening an insert cursor.</summary>
		/// <remarks>
		/// This flag is used to implement an optimization to the insert process to prevent the actual
		/// opening of cursors unnecessarily. This flag is used by the devices involved in an expression
		/// to determine whether or not to issue the cursor open, or simply return an empty cursor.
		/// The optimization is valid because the insert cursor expression always has a contradictory
		/// restriction appended. In theory, the optimizers for the target systems should recognize the
		/// contradiction and the insert cursor open should not take any time, but in reality, it
		/// does take time with some complex cursors, and there is also the unnecessary network or RPC hit
		/// for extra-process devices.
		/// </remarks>
		public bool IsOpeningInsertCursor
		{
			get { return FIsOpeningInsertCursor; }
			set { FIsOpeningInsertCursor = value; }
		}
		
		// AddingTableVar
		private int FAddingTableVar;
		/// <summary>Indicates whether the current process is adding an A/T table map.</summary>
		/// <remarks>This is used to prevent the resolution process from attempting to add a table map for the table variable being created before the table map is done.</remarks>
		public bool InAddingTableVar { get { return FAddingTableVar > 0; } }
		
		public void PushAddingTableVar()
		{
			FAddingTableVar++;
		}
		
		public void PopAddingTableVar()
		{
			FAddingTableVar--;
		}
		
		public ApplicationTransaction GetApplicationTransaction()
		{
			return ApplicationTransactionUtility.GetTransaction(this, FApplicationTransactionID);
		}
		
		public void EnsureApplicationTransactionOperator(Schema.Operator AOperator)
		{
			if (FApplicationTransactionID != Guid.Empty && AOperator.IsATObject)
			{
				ApplicationTransaction LTransaction = GetApplicationTransaction();
				try
				{
					LTransaction.EnsureATOperatorMapped(this, AOperator);
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		public void EnsureApplicationTransactionTableVar(Schema.TableVar ATableVar)
		{
			if (FApplicationTransactionID != Guid.Empty && ATableVar.IsATObject)
			{
				ApplicationTransaction LTransaction = GetApplicationTransaction();
				try
				{
					LTransaction.EnsureATTableVarMapped(this, ATableVar);
				}
				finally
				{
					Monitor.Exit(LTransaction);
				}
			}
		}
		
		internal void ProcessCallInfo(ProcessCallInfo ACallInfo)
		{
			for (int LIndex = 0; LIndex < ACallInfo.TransactionList.Length; LIndex++)
				BeginTransaction(ACallInfo.TransactionList[LIndex]);
		}
		
		internal ProcessCallInfo EmptyCallInfo()
		{
			ProcessCallInfo LInfo = new ProcessCallInfo();
			LInfo.TransactionList = new IsolationLevel[0];
			return LInfo;
		}
		
		Guid IRemoteServerProcess.BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			return BeginApplicationTransaction(AShouldJoin, AIsInsert);
		}
		
		// BeginApplicationTransaction
		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginBeginApplicationTransaction, "Begin BeginApplicationTransaction");
				#endif
				Guid LATID = ApplicationTransactionUtility.BeginApplicationTransaction(this);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndBeginApplicationTransaction, "End BeginApplicationTransaction");
				#endif
				
				if (AShouldJoin)
					JoinApplicationTransaction(LATID, AIsInsert);

				return LATID;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			PrepareApplicationTransaction(AID);
		}
		
		// PrepareApplicationTransaction
		public void PrepareApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginPrepareApplicationTransaction, "Begin PrepareApplicationTransaction");
				#endif
				ApplicationTransactionUtility.PrepareApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndPrepareApplicationTransaction, "End PrepareApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			CommitApplicationTransaction(AID);
		}
		
		// CommitApplicationTransaction
		public void CommitApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginCommitApplicationTransaction, "Begin CommitApplicationTransaction");
				#endif
				ApplicationTransactionUtility.CommitApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndCommitApplicationTransaction, "End CommitApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			RollbackApplicationTransaction(AID);
		}
		
		// RollbackApplicationTransaction
		public void RollbackApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginRollbackApplicationTransaction, "Begin RollbackApplicationTransaction");
				#endif
				ApplicationTransactionUtility.RollbackApplicationTransaction(this, AID);
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndRollbackApplicationTransaction, "End RollbackApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}

		void IRemoteServerProcess.JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			JoinApplicationTransaction(AID, AIsInsert);
		}
		
		public void JoinApplicationTransaction(Guid AID, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginJoinApplicationTransaction, "Begin JoinApplicationTransaction");
				#endif
				ApplicationTransactionUtility.JoinApplicationTransaction(this, AID);
				FApplicationTransactionID = AID;
				FIsInsert = AIsInsert;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndJoinApplicationTransaction, "End JoinApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		void IRemoteServerProcess.LeaveApplicationTransaction(ProcessCallInfo ACallInfo)
		{
			ProcessCallInfo(ACallInfo);
			LeaveApplicationTransaction();
		}
		
		public void LeaveApplicationTransaction()
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.BeginLeaveApplicationTransaction, "Begin LeaveApplicationTransaction");
				#endif
				ApplicationTransactionUtility.LeaveApplicationTransaction(this);
				FApplicationTransactionID = Guid.Empty;
				FIsInsert = false;
				#if TRACEEVENTS
				RaiseTraceEvent(TraceCodes.EndLeaveApplicationTransaction, "End LeaveApplicationTransaction");
				#endif
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				EndTransactionalCall(LNestingLevel, LException);
			}
		}

		// HandlerDepth
		protected int FHandlerDepth;
		/// <summary>Indicates whether the process has entered an event handler.</summary>
		public bool InHandler { get { return FHandlerDepth > 0; } }
		
		public void PushHandler()
		{
			lock (this)
			{
				FHandlerDepth++;
			}
		}
		
		public void PopHandler()
		{
			lock (this)
			{
				FHandlerDepth--;
			}
		}
        
		protected int FTimeStampCount = 0;
		/// <summary>Indicates whether time stamps should be affected by alter and drop table variable and operator statements.</summary>
		public bool ShouldAffectTimeStamp { get { return FTimeStampCount == 0; } }
		
		public void EnterTimeStampSafeContext()
		{
			FTimeStampCount++;
		}
		
		public void ExitTimeStampSafeContext()
		{
			FTimeStampCount--;
		}
		
		// Execution
		private ServerPlans FExecutingPlans;

		private System.Threading.Thread FExecutingThread;
		public System.Threading.Thread ExecutingThread { get { return FExecutingThread; } }
		
		private int FExecutingThreadCount = 0;
		
		public ServerPlanBase ExecutingPlan
		{
			get 
			{
				if (FExecutingPlans.Count == 0)
					throw new ServerException(ServerException.Codes.NoExecutingPlan);
				return FExecutingPlans[FExecutingPlans.Count - 1];
			}
		}
		
		public ServerPlanBase RootExecutingPlan
		{
			get
			{
				if (FExecutingPlans.Count <= 1)
					return ExecutingPlan;
				return FExecutingPlans[1];
			}
		}
		
		public void PushExecutingPlan(ServerPlanBase APlan)
		{
			lock (this)
			{
				FExecutingPlans.Add(APlan);
			}
		}
		
		public void PopExecutingPlan(ServerPlanBase APlan)
		{
			lock (this)
			{
				if (ExecutingPlan != APlan)
					throw new ServerException(ServerException.Codes.PlanNotExecuting, APlan.ID.ToString());
				FExecutingPlans.DisownAt(FExecutingPlans.Count - 1);
			}
		}
		
		public void Start(ServerPlanBase APlan, DataParams AParams)
		{
			PushExecutingPlan(APlan);
			try
			{
				if (AParams != null)
					foreach (DataParam LParam in AParams)
						FContext.Push(new DataVar(LParam.Name, LParam.DataType, (LParam.Modifier == Modifier.In ? ((LParam.Value == null) ? null : LParam.Value.Copy()) : LParam.Value)));
			}
			catch
			{
				PopExecutingPlan(APlan);
				throw;
			}
		}
		
		public void Stop(ServerPlanBase APlan, DataParams AParams)
		{
			try
			{
				if (AParams != null)
					for (int LIndex = AParams.Count - 1; LIndex >= 0; LIndex--)
					{
						DataVar LDataVar = FContext.Pop();
						if (AParams[LIndex].Modifier != Modifier.In)
							AParams[LIndex].Value = LDataVar.Value;
					}
			}
			finally
			{
				PopExecutingPlan(APlan);
			}
		}
		
		public DataVar Execute(ServerPlanBase APlan, PlanNode ANode, DataParams AParams)
		{	
			DataVar LResult;
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.BeginExecute, "Begin Execute");
			#endif
			Start(APlan, AParams);
			try
			{
				long LStartTicks = TimingUtility.CurrentTicks;
				LResult = ANode.Execute(this);
				APlan.Plan.Statistics.ExecuteTime = TimingUtility.TimeSpanFromTicks(LStartTicks);
			}
			finally
			{
				Stop(APlan, AParams);
			}
			#if TRACEEVENTS
			RaiseTraceEvent(TraceCodes.EndExecute, "End Execute");
			#endif
			return LResult;
		}
		
		// SyncHandle is used to synchronize CLI calls to the process, ensuring that only one call originating in the CLI is allowed to run through the process at a time
		private object FSyncHandle = new System.Object();
		public object SyncHandle { get { return FSyncHandle; } }

		// ExecutionSyncHandle is used to synchronize server-level execution management services involving the executing thread of the process
		private object FExecutionSyncHandle = new System.Object();
		public object ExecutionSyncHandle { get { return FExecutionSyncHandle; } }
		
		// StopError is used to indicate that a system level stop error has occurred on the process, and all transactions should be rolled back
		private bool FStopError = false;
		public bool StopError 
		{ 
			get { return FStopError; } 
			set { FStopError = value; } 
		}
		
		private bool FIsAborted = false;
		public bool IsAborted
		{
			get { return FIsAborted; }
			set { lock (FExecutionSyncHandle) { FIsAborted = value; } }
		}
		
		public void CheckAborted()
		{
			if (FIsAborted)
				throw new ServerException(ServerException.Codes.ProcessAborted);
		}
		
		public bool IsRunning { get { return FExecutingThreadCount > 0; } }
		
		internal void BeginCall()
		{
			Monitor.Enter(FSyncHandle);
			try
			{
				if ((FExecutingThreadCount == 0) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.BeginProcessCall(this);
				Monitor.Enter(FExecutionSyncHandle);
				try
				{
					#if !DISABLE_PERFORMANCE_COUNTERS
					if (ServerSession.Server.FRunningProcessCounter != null)
						ServerSession.Server.FRunningProcessCounter.Increment();
					#endif

					FExecutingThreadCount++;
					if (FExecutingThreadCount == 1)
					{
						FExecutingThread = System.Threading.Thread.CurrentThread;
						FIsAborted = false;
						//FExecutingThread.ApartmentState = ApartmentState.STA; // This had no effect, the ADO thread lock is still occurring
						ThreadUtility.SetThreadName(FExecutingThread, String.Format("ServerProcess {0}", FProcessID.ToString()));
					}
				}
				finally
				{
					Monitor.Exit(FExecutionSyncHandle);
				}
			}
			catch
			{
				try
				{
					if (FExecutingThreadCount > 0)
					{
						Monitor.Enter(FExecutionSyncHandle);
						try
						{
							#if !DISABLE_PERFORMANCE_COUNTERS
							if (ServerSession.Server.FRunningProcessCounter != null)
								ServerSession.Server.FRunningProcessCounter.Decrement();
							#endif

							FExecutingThreadCount--;
							if (FExecutingThreadCount == 0)
							{
								ThreadUtility.SetThreadName(FExecutingThread, null);
								FExecutingThread = null;
							}
						}
						finally
						{
							Monitor.Exit(FExecutionSyncHandle);
						}
					}
				}
				finally
				{
					Monitor.Exit(FSyncHandle);
				}
                throw;
            }
		}
		
		internal void EndCall()
		{
			try
			{
				if ((FExecutingThreadCount == 1) && !ServerSession.User.IsAdminUser())
					ServerSession.Server.EndProcessCall(this);
			}
			finally
			{
				try
				{
					Monitor.Enter(FExecutionSyncHandle);
					try
					{
						#if !DISABLE_PERFORMANCE_COUNTERS
						if (ServerSession.Server.FRunningProcessCounter != null)
							ServerSession.Server.FRunningProcessCounter.Decrement();
						#endif

						FExecutingThreadCount--;
						if (FExecutingThreadCount == 0)
						{
							ThreadUtility.SetThreadName(FExecutingThread, null);
							FExecutingThread = null;
						}
					}
					finally
					{
						Monitor.Exit(FExecutionSyncHandle);
					}
				}
				finally
				{
					Monitor.Exit(FSyncHandle);
				}
			}
		}
		
		internal int BeginTransactionalCall()
		{
			BeginCall();
			try
			{
				FStopError = false;
				int LNestingLevel = FTransactions.Count;
				if ((FTransactions.Count == 0) && UseImplicitTransactions)
					InternalBeginTransaction(DefaultIsolationLevel);
				if (FTransactions.Count > 0)
					FTransactions.CurrentTransaction().Prepared = false;
				return LNestingLevel;
			}
			catch
			{
				EndCall();
				throw;
			}
		}
		
		internal void EndTransactionalCall(int ANestingLevel, Exception AException)
		{
			try
			{
				if (StopError)
					while (FTransactions.Count > 0)
						try
						{
							InternalRollbackTransaction();
						}
						catch (Exception E)
						{
							ServerSession.Server.LogError(E);
						}
				
				if (UseImplicitTransactions)
				{
					Exception LCommitException = null;
					while (FTransactions.Count > ANestingLevel)
						if ((AException != null) || (LCommitException != null))
							InternalRollbackTransaction();
						else
						{
							try
							{
								InternalPrepareTransaction();
								InternalCommitTransaction();
							}
							catch (Exception LException)
							{
								LCommitException = LException;
								InternalRollbackTransaction();
							}
						}

					if (LCommitException != null)
						throw LCommitException;
				}
			}
			catch (Exception E)
			{
				if (AException != null)
					// TODO: Should this actually attach the rollback exception as context information to the server exception object?
					throw new ServerException(ServerException.Codes.RollbackError, AException, E.ToString());
				else
					throw;
			}
			finally
			{
				EndCall();
			}
		}
		
		private Exception WrapException(Exception AException)
		{
			return FServerSession.WrapException(AException);
		}

		// ActiveCursor
		protected ServerCursorBase FActiveCursor;
		public ServerCursorBase ActiveCursor { get { return FActiveCursor; } }
		
		public void SetActiveCursor(ServerCursorBase AActiveCursor)
		{
			/*
			if (FActiveCursor != null)
				throw new ServerException(ServerException.Codes.PlanCursorActive);
			FActiveCursor = AActiveCursor;
			*/
		}
		
		public void ClearActiveCursor()
		{
			//FActiveCursor = null;
		}
		
		// Parameter Translation
		public DataParams RemoteParamsToDataParams(RemoteParam[] AParams)
		{
			if ((AParams != null) && (AParams.Length > 0))
			{
				DataParams LParams = new DataParams();
				foreach (RemoteParam LRemoteParam in AParams)
					LParams.Add(new DataParam(LRemoteParam.Name, (Schema.ScalarType)ServerSession.Server.Catalog[LRemoteParam.TypeName], (Modifier)LRemoteParam.Modifier));//hack: cast to fix fixup error

				return LParams;
			}
			else
				return null;
		}
		
		public DataParams RemoteParamDataToDataParams(RemoteParamData AParams)
		{
			if ((AParams.Params != null) && (AParams.Params.Length > 0))
			{
				DataParams LParams = new DataParams();
				Schema.RowType LRowType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
					LRowType.Columns.Add(new Schema.Column(AParams.Params[LIndex].Name, (Schema.ScalarType)ServerSession.Server.Catalog[AParams.Params[LIndex].TypeName]));
					
				Row LRow = new Row(this, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					LRow.AsPhysical = AParams.Data.Data;

					for (int LIndex = 0; LIndex < AParams.Params.Length; LIndex++)
						if (LRow.HasValue(LIndex))
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow[LIndex].DataType, (Modifier)AParams.Params[LIndex].Modifier, LRow[LIndex].Copy()));//Hack: cast to fix fixup error
						else
							LParams.Add(new DataParam(LRow.DataType.Columns[LIndex].Name, LRow.DataType.Columns[LIndex].DataType, (Modifier)AParams.Params[LIndex].Modifier, null));//Hack: cast to fix fixup error

					return LParams;
				}
				finally
				{
					LRow.Dispose();
				}
			}
			else
				return null;
		}
		
		public void DataParamsToRemoteParamData(DataParams AParams, ref RemoteParamData ARemoteParams)
		{
			if (AParams != null)
			{
				Schema.RowType LRowType = new Schema.RowType();
				for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
					LRowType.Columns.Add(new Schema.Column(AParams[LIndex].Name, AParams[LIndex].DataType));
					
				Row LRow = new Row(this, LRowType);
				try
				{
					LRow.ValuesOwned = false;
					for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
						LRow[LIndex] = AParams[LIndex].Value;
					
					ARemoteParams.Data.Data = LRow.AsPhysical;
				}
				finally
				{
					LRow.Dispose();
				}
			}
		}
		
		// Catalog
		public Schema.DataTypes DataTypes { get { return FServerSession.Server.Catalog.DataTypes; } }

		string IRemoteServerProcess.GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			ACacheTimeStamp = ServerSession.Server.CacheTimeStamp;

			Schema.Catalog LCatalog = new Schema.Catalog();
			LCatalog.IncludeDependencies(this, Plan.Catalog, Plan.Catalog[AName], EmitMode.ForRemote);
			
			#if LOGCACHEEVENTS
			ServerSession.Server.LogMessage(String.Format("Getting catalog for data type '{0}'.", AName));
			#endif

			ACacheChanged = true;
			string[] LRequiredObjects = ServerSession.Server.CatalogCaches.GetRequiredObjects(ServerSession, LCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LCatalog.EmitStatement(this, EmitMode.ForRemote, LRequiredObjects));
				return LCatalogString;
			}
			return String.Empty;
		}
		
		// Loading
		private LoadingContexts FLoadingContexts;
		
		public void PushLoadingContext(LoadingContext AContext)
		{
			if (FLoadingContexts == null)
				FLoadingContexts = new LoadingContexts();
				
			if (AContext.LibraryName != String.Empty)
			{
				AContext.FCurrentLibrary = ServerSession.CurrentLibrary;
				ServerSession.CurrentLibrary = ServerSession.Server.Catalog.LoadedLibraries[AContext.LibraryName];
			}
			
			AContext.FSuppressWarnings = SuppressWarnings;
			SuppressWarnings = true;

			if (!AContext.IsLoadingContext)
			{
				LoadingContext LCurrentLoadingContext = CurrentLoadingContext();
				if ((LCurrentLoadingContext != null) && !LCurrentLoadingContext.IsInternalContext)
					throw new Schema.SchemaException(Schema.SchemaException.Codes.InvalidLoadingContext, ErrorSeverity.System);
			}

			FLoadingContexts.Add(AContext);
		}
		
		public void PopLoadingContext()
		{
			LoadingContext LContext = FLoadingContexts[FLoadingContexts.Count - 1];
			FLoadingContexts.RemoveAt(FLoadingContexts.Count - 1);
			if (LContext.LibraryName != String.Empty)
				ServerSession.CurrentLibrary = LContext.FCurrentLibrary;
			SuppressWarnings = LContext.FSuppressWarnings;
		}
		
		private int FReconciliationDisabledCount = 0;
		public void DisableReconciliation()
		{
			FReconciliationDisabledCount++;
		}
		
		public void EnableReconciliation()
		{
			FReconciliationDisabledCount--;
		}
		
		public bool IsReconciliationEnabled()
		{
			return FReconciliationDisabledCount <= 0;
		}
		
		public int SuspendReconciliationState()
		{
			int LResult = FReconciliationDisabledCount;
			FReconciliationDisabledCount = 0;
			return LResult;
		}
		
		public void ResumeReconciliationState(int AReconciliationDisabledCount)
		{
			FReconciliationDisabledCount = AReconciliationDisabledCount;
		}
		
		/// <summary>Returns true if the Server-level loading flag is set, or this process is in a loading context</summary>
		public bool IsLoading()
		{
			return ServerSession.Server.LoadingCatalog || InLoadingContext();
		}
		
		/// <summary>Returns true if this process is in a loading context.</summary>
		public bool InLoadingContext()
		{
			return (FLoadingContexts != null) && (FLoadingContexts.Count > 0) && (FLoadingContexts[FLoadingContexts.Count - 1].IsLoadingContext);
		}
		
		public LoadingContext CurrentLoadingContext()
		{
			if ((FLoadingContexts != null) && FLoadingContexts.Count > 0)
				return FLoadingContexts[FLoadingContexts.Count - 1];
			return null;
		}
	}

	// ServerProcesses
	public class ServerProcesses : ServerChildObjects
	{		
		protected override void Validate(ServerChildObject AObject)
		{
			if (!(AObject is ServerProcess))
				throw new ServerException(ServerException.Codes.ServerProcessContainer);
		}
		
		public new ServerProcess this[int AIndex]
		{
			get { return (ServerProcess)base[AIndex]; } 
			set { base[AIndex] = value; } 
		}
		
		public ServerProcess GetProcess(int AProcessID)
		{
			foreach (ServerProcess LProcess in this)
				if (LProcess.ProcessID == AProcessID)
					return LProcess;
			throw new ServerException(ServerException.Codes.ProcessNotFound, AProcessID);
		}
	}
}
