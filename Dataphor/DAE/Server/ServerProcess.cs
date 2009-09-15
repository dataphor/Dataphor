/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define LOADFROMLIBRARIES
#define PROCESSSTREAMSOWNED // Determines whether or not the process will deallocate all streams allocated by the process.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Alphora.Dataphor.Logging;

namespace Alphora.Dataphor.DAE.Server
{
	using Alphora.Dataphor.DAE.Compiling;
	using Alphora.Dataphor.DAE.Debug;
	using Alphora.Dataphor.DAE.Device.ApplicationTransaction;
	using Alphora.Dataphor.DAE.Device.Catalog;
	using Alphora.Dataphor.DAE.Language;
	using Alphora.Dataphor.DAE.Language.D4;
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Runtime.Data;
	using Alphora.Dataphor.DAE.Runtime.Instructions;
	using Alphora.Dataphor.DAE.Streams;

	// ServerProcess
	public class ServerProcess : ServerChildObject, IServerProcess
	{
		private static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(ServerProcess));
		
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
			FScripts = new ServerScripts();
			FParser = new Parser();
			FProcessID = GetHashCode();
			FProcessInfo = AProcessInfo;
			FDeviceSessions = new Schema.DeviceSessions(false);
			FStreamManager = (IStreamManager)this;
			FValueManager = new ValueManager(this, this);
			#if PROCESSSTREAMSOWNED
			FOwnedStreams = new Dictionary<StreamID, bool>();
			#endif

			FExecutingPrograms = new Programs();
			FProcessLocalStack = new List<DataParams>();
			PushProcessLocals();
			FProcessProgram = new Program(this);
			FProcessProgram.Start(null);
			
			if (FServerSession.DebuggedByID > 0)
				FServerSession.Server.GetDebugger(FServerSession.DebuggedByID).Attach(this);

			#if !DISABLE_PERFORMANCE_COUNTERS
			if (FServerSession.Server.FProcessCounter != null)
				FServerSession.Server.FProcessCounter.Increment();
			#endif
		}
		
		private bool FDisposed;
		
		protected void DeallocateProcessLocals(DataParams AParams)
		{
			foreach (DataParam LParam in AParams)
				DataValue.DisposeValue(FValueManager, LParam.Value);
		}
	
		protected void DeallocateProcessLocalStack()
		{
			foreach (DataParams LParams in FProcessLocalStack)
				DeallocateProcessLocals(LParams);
		}
		
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
														if (FDebuggedBy != null)
															FDebuggedBy.Detach(this);
															
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
														foreach (ServerPlan LPlan in FPlans)
														{
															if (LPlan.ActiveCursor != null)
																LPlan.ActiveCursor.Dispose();
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
									if (FProcessProgram != null)
									{
										FProcessProgram.Stop(null);
										FProcessProgram = null;
									}
								}
							}
							finally
							{
								if (FProcessLocalStack != null)
								{
									DeallocateProcessLocalStack();
									FProcessLocalStack = null;
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
					ReleaseLocks();
				}
			}
			finally
			{
				#if PROCESSSTREAMSOWNED
				ReleaseStreams();
				#endif
				
				FExecutingPrograms = null;
				FStreamManager = null;
				FValueManager = null;
				FServerSession = null;
				FProcessID = -1;
				FParser = null;
				base.Dispose(ADisposing);
			}
		}
		
		// ServerSession
		private ServerSession FServerSession;
		public ServerSession ServerSession { get { return FServerSession; } }
		
		// This is an internal program used to ensure that there is always at
		// least on executing program on the process. At this point, this is used 
		// only by the value manager to evaluate sorts and perform scalar 
		// representation processing to avoid the overhead of having to create
		// a program each time a sort comparison or scalar transformation is
		// required. Even then, it is only necessary for out-of-process
		// processes, or in-process Dataphoria processes.
		private Program FProcessProgram;
		
		// ProcessID
		private int FProcessID = -1;
		public int ProcessID { get { return FProcessID; } }
		
		// ProcessInfo
		private ProcessInfo FProcessInfo;
		public ProcessInfo ProcessInfo { get { return FProcessInfo; } }
		
		/// <summary>Determines the default isolation level for transactions on this process.</summary>
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
		
		// MaxStackDepth
		public int MaxStackDepth
		{
			get { return FExecutingPrograms.Count == 0 ? FServerSession.SessionInfo.DefaultMaxStackDepth : ExecutingProgram.Stack.MaxStackDepth; }
			set 
			{ 
				if (FExecutingPrograms.Count == 0)
					FServerSession.SessionInfo.DefaultMaxStackDepth = value;
				else
					ExecutingProgram.Stack.MaxStackDepth = value;
			}
		}
		
		// MaxCallDepth
		public int MaxCallDepth
		{
			get { return FExecutingPrograms.Count == 0 ? FServerSession.SessionInfo.DefaultMaxCallDepth : ExecutingProgram.Stack.MaxCallDepth; }
			set
			{
				if (FExecutingPrograms.Count == 0)
					FServerSession.SessionInfo.DefaultMaxCallDepth = value;
				else
					ExecutingProgram.Stack.MaxCallDepth = value;
			}
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
		
		// Parsing
		private Parser FParser;		
		
		public Statement ParseScript(string AScript, ParserMessages AMessages)
		{
			Statement LStatement;								  
			LStatement = FParser.ParseScript(AScript, AMessages);
			return LStatement;
		}
		
		public Statement ParseStatement(string AStatement, ParserMessages AMessages)
		{
			Statement LStatement;
			LStatement = FParser.ParseStatement(AStatement, AMessages);
			return LStatement;
		}
		
		public Expression ParseExpression(string AExpression)
		{
			Expression LExpression;
			LExpression = FParser.ParseCursorDefinition(AExpression);
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
					ServerPlan LPlan = FPlans.DisownAt(0) as ServerPlan;
					if (!ServerSession.ReleaseCachedPlan(this, LPlan))
						LPlan.Dispose();
					else
						LPlan.NotifyReleased();
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
		
		// IValueManager
		private IValueManager FValueManager;
		public IValueManager ValueManager { get { return FValueManager; } }
		
		// IStreamManager
		private IStreamManager FStreamManager;
		public IStreamManager StreamManager { get { return FStreamManager; } }
		
		#if PROCESSSTREAMSOWNED
		private Dictionary<StreamID, bool> FOwnedStreams;
		
		private void ReleaseStreams()
		{
			if (FOwnedStreams != null)
			{
				foreach (StreamID LStreamID in FOwnedStreams.Keys)
					try
					{
						ServerSession.Server.StreamManager.Deallocate(LStreamID);
					}
					catch
					{
						// Just keep going, get rid of as many as possible.
					}

				FOwnedStreams = null;
			}
		}
		#endif
		
		StreamID IStreamManager.Allocate()
		{
			StreamID LStreamID = ServerSession.Server.StreamManager.Allocate();
			#if PROCESSSTREAMSOWNED
			FOwnedStreams.Add(LStreamID, false);
			#endif
			return LStreamID;
		}
		
		StreamID IStreamManager.Reference(StreamID AStreamID)
		{
			StreamID LStreamID = ServerSession.Server.StreamManager.Reference(AStreamID);
			#if PROCESSSTREAMSOWNED
			FOwnedStreams.Add(LStreamID, true);
			#endif
			return LStreamID;
		}
		
		public StreamID Register(IStreamProvider AStreamProvider)
		{
			StreamID LStreamID = ServerSession.Server.StreamManager.Register(AStreamProvider);
			#if PROCESSSTREAMSOWNED
			FOwnedStreams.Add(LStreamID, false);
			#endif
			return LStreamID;
		}
		
		void IStreamManager.Deallocate(StreamID AStreamID)
		{
			#if PROCESSSTREAMSOWNED
			FOwnedStreams.Remove(AStreamID);
			#endif
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
		
		internal ServerFileInfos GetFileNames(Schema.Library ALibrary)
		{
			ServerFileInfos LFileInfos = new ServerFileInfos();
			Schema.Libraries LLibraries = new Schema.Libraries();
			LLibraries.Add(ALibrary);
			
			while (LLibraries.Count > 0)
			{
				Schema.Library LLibrary = LLibraries[0];
				LLibraries.RemoveAt(0);
				
				foreach (Schema.FileReference LReference in ALibrary.Files)
				{
					if (!LFileInfos.Contains(LReference.FileName))
					{
						string LFullFileName = GetFullFileName(ALibrary, LReference.FileName);
						LFileInfos.Add
						(
							new ServerFileInfo 
							{ 
								LibraryName = ALibrary.Name, 
								FileName = LReference.FileName, 
								FileDate = File.GetLastWriteTimeUtc(LFullFileName), 
								IsDotNetAssembly = FileUtility.IsAssembly(LFullFileName), 
								ShouldRegister = LReference.IsAssembly 
							}
						);
					} 
				}
				
				foreach (Schema.LibraryReference LReference in LLibrary.Libraries)
					if (!LLibraries.Contains(LReference.Name))
						LLibraries.Add(FServerSession.Server.Catalog.Libraries[LReference.Name]);
			}
			
			return LFileInfos;
		}

		public string GetFullFileName(Schema.Library ALibrary, string AFileName)
		{
			#if LOADFROMLIBRARIES
			return 
				Path.IsPathRooted(AFileName) 
					? AFileName 
					: 
						ALibrary.Name == Engine.CSystemLibraryName
							? PathUtility.GetFullFileName(AFileName)
							: Path.Combine(ALibrary.GetLibraryDirectory(FServerSession.Server.LibraryDirectory), AFileName);
			#else
			return PathUtility.GetFullFileName(AFileName);
			#endif
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
		
		internal string RemoteSessionClassName = "Alphora.Dataphor.DAE.Server.RemoteSessionImplementation,Alphora.Dataphor.Server";
		
		internal RemoteSession RemoteConnect(Schema.ServerLink AServerLink)
		{
			int LIndex = RemoteSessions.IndexOf(AServerLink);
			if (LIndex < 0)
			{
				RemoteSession LSession = (RemoteSession)Activator.CreateInstance(Type.GetType(RemoteSessionClassName), this, AServerLink);
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
		
		// Plans
		/// <summary>Indicates whether or not warnings encountered during compilation of plans on this process will be reported.</summary>
		public bool SuppressWarnings
		{
			get { return FProcessInfo.SuppressWarnings; }
			set { FProcessInfo.SuppressWarnings = value; }
		}
		
		// ProcessLocals
		private List<DataParams> FProcessLocalStack;
		public DataParams ProcessLocals { get { return FProcessLocalStack[FProcessLocalStack.Count - 1]; } }
		
		public void PushProcessLocals()
		{
			FProcessLocalStack.Add(new DataParams());
		}
		
		public void PopProcessLocals()
		{
			DeallocateProcessLocals(ProcessLocals);
			FProcessLocalStack.RemoveAt(FProcessLocalStack.Count - 1);
		}
		
		public void AddProcessLocal(DataParam LParam)
		{
			DataParams LProcessLocals = ProcessLocals;
			int LParamIndex = LProcessLocals.IndexOf(LParam.Name);
			if (LParamIndex >= 0)
			{
				DataValue.DisposeValue(FValueManager, LProcessLocals[LParamIndex].Value);
				LProcessLocals[LParamIndex].Value = LParam.Value;
				LParam.Value = null;
			}
			else
				LProcessLocals.Add(LParam);
		}
		
		private void Compile(Plan APlan, Program AProgram, Statement AStatement, DataParams AParams, bool AIsExpression, SourceContext ASourceContext)
		{
			#if TIMING
			DateTime LStartTime = DateTime.Now;
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			Schema.DerivedTableVar LTableVar = null;
			LTableVar = new Schema.DerivedTableVar(Schema.Object.GetNextObjectID(), Schema.Object.NameFromGuid(AProgram.ID));
			LTableVar.SessionObjectName = LTableVar.Name;
			LTableVar.SessionID = APlan.SessionID;
			APlan.PushCreationObject(LTableVar);
			try
			{
				DataParams LParams = new DataParams();
				if (AProgram.ShouldPushLocals)
					foreach (DataParam LParam in ProcessLocals)
						LParams.Add(LParam);

				if (AParams != null)
					foreach (DataParam LParam in AParams)
						LParams.Add(LParam);

				AProgram.Code = Compiler.Compile(APlan, AStatement, LParams, AIsExpression, ASourceContext);
				AProgram.SetSourceContext(ASourceContext);

				// Include dependencies for any process context that may be being referenced by the plan
				// This is necessary to support the variable declaration statements that will be added to support serialization of the plan
				if (AProgram.ShouldPushLocals && APlan.NewSymbols != null)
					foreach (Symbol LSymbol in APlan.NewSymbols)
					{
						AProgram.ProcessLocals.Add(new DataParam(LSymbol.Name, LSymbol.DataType, Modifier.Var));
						APlan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, APlan.Catalog, LSymbol.DataType, EmitMode.ForRemote);
					}

				if (!APlan.Messages.HasErrors && AIsExpression)
				{
					if (AProgram.Code.DataType == null)
						throw new CompilerException(CompilerException.Codes.ExpressionExpected);
					AProgram.DataType = CopyDataType(APlan, AProgram.Code, LTableVar);
				}
			}
			finally
			{
				APlan.PopCreationObject();
			}
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerSession.Compile -- Compile Time: {1}", DateTime.Now.ToString("hh:mm:ss.ffff"), (DateTime.Now - LStartTime).ToString()));
			#endif
		}
        
		internal ServerPlan CompileStatement(Statement AStatement, ParserMessages AMessages, DataParams AParams, SourceContext ASourceContext)
		{
			ServerPlan LPlan = new ServerStatementPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan.Plan, LPlan.Program, AStatement, AParams, false, ASourceContext);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}

		private void InternalUnprepare(ServerPlan APlan)
		{
			BeginCall();
			try
			{
				FPlans.Disown(APlan);
				if (!ServerSession.ReleaseCachedPlan(this, APlan))
					APlan.Dispose();
				else
					APlan.NotifyReleased();
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
			for (int LIndex = 0; LIndex < ProcessLocals.Count; LIndex++)
				LContextName.Append(ProcessLocals[LIndex].DataType.Name);
			if (AParams != null)
				for (int LIndex = 0; LIndex < AParams.Count; LIndex++)
					LContextName.Append(AParams[LIndex].DataType.Name);
			return LContextName.ToString().GetHashCode();
		}
		
		public IServerStatementPlan PrepareStatement(string AStatement, DataParams AParams)
		{
			return PrepareStatement(AStatement, AParams, null);
		}
		
		// PrepareStatement        
		public IServerStatementPlan PrepareStatement(string AStatement, DataParams AParams, DebugLocator ALocator)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerStatementPlan LPlan = ServerSession.GetCachedPlan(this, AStatement, LContextHashCode) as IServerStatementPlan;
				if (LPlan != null)
				{
					FPlans.Add(LPlan);
					((ServerPlan)LPlan).Program.SetSourceContext(new SourceContext(AStatement, ALocator));
				}
				else
				{
					ParserMessages LMessages = new ParserMessages();
					Statement LStatement = ParseStatement(AStatement, LMessages);
					LPlan = (IServerStatementPlan)CompileStatement(LStatement, LMessages, AParams, new SourceContext(AStatement, ALocator));
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AStatement, LContextHashCode, (ServerPlan)LPlan);
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
        
		// UnprepareStatement
		public void UnprepareStatement(IServerStatementPlan APlan)
		{
			InternalUnprepare((ServerPlan)APlan);
		}
        
		// Execute
		public void Execute(string AStatement, DataParams AParams)
		{
			IServerStatementPlan LStatementPlan = PrepareStatement(AStatement, AParams);
			try
			{
				LStatementPlan.Execute(AParams);
			}
			finally
			{
				UnprepareStatement(LStatementPlan);
			}
		}
		
		private Schema.IDataType CopyDataType(Plan APlan, PlanNode ACode, Schema.DerivedTableVar ATableVar)
		{
			#if TIMING
			System.Diagnostics.Debug.WriteLine(String.Format("{0} -- ServerProcess.CopyDataType", DateTime.Now.ToString("hh:mm:ss.ffff")));
			#endif
			if (ACode.DataType is Schema.CursorType)
			{
				TableNode LSourceNode = (TableNode)ACode.Nodes[0];	
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
					
				LViewNode = Compiler.EmitAdornNode(APlan, LViewNode, LAdornExpression);

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
						Schema.Object LObject = ATableVar.Dependencies.ResolveObject(CatalogDeviceSession, LIndex);
						if ((LObject != null) && LObject.IsATObject && ((LObject is Schema.TableVar) || (LObject is Schema.Operator)))
							LATObjects.Add(LObject);
					}

				foreach (Schema.Object LATObject in LATObjects)
				{
					Schema.TableVar LATTableVar = LATObject as Schema.TableVar;
					if (LATTableVar != null)
					{
						Schema.TableVar LTableVar = (Schema.TableVar)APlan.Catalog[APlan.Catalog.IndexOfName(LATTableVar.SourceTableName)];
						ATableVar.Dependencies.Ensure(LTableVar);
						continue;
					}

					Schema.Operator LATOperator = LATObject as Schema.Operator;
					if (LATOperator != null)
					{
						ATableVar.Dependencies.Ensure(ServerSession.Server.ATDevice.ResolveSourceOperator(APlan, LATOperator));
					}
				}
				
				APlan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, APlan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.PlanCatalog.Remove(ATableVar);
				APlan.PlanCatalog.Add(ATableVar);
				return ATableVar.DataType;
			}
			else
			{
				APlan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, APlan.Catalog, ATableVar, EmitMode.ForRemote);
				APlan.PlanCatalog.SafeRemove(ATableVar);
				APlan.PlanCatalog.IncludeDependencies(CatalogDeviceSession, APlan.Catalog, ACode.DataType, EmitMode.ForRemote);
				return ACode.DataType;
			}
		}

		internal ServerPlan CompileExpression(Statement AExpression, ParserMessages AMessages, DataParams AParams, SourceContext ASourceContext)
		{
			ServerPlan LPlan = new ServerExpressionPlan(this);
			try
			{
				if (AMessages != null)
					LPlan.Plan.Messages.AddRange(AMessages);
				Compile(LPlan.Plan, LPlan.Program, AExpression, AParams, true, ASourceContext);
				FPlans.Add(LPlan);
				return LPlan;
			}
			catch
			{
				LPlan.Dispose();
				throw;
			}
		}
		
		public IServerExpressionPlan PrepareExpression(string AExpression, DataParams AParams)
		{
			return PrepareExpression(AExpression, AParams, null);
		}
		
		// PrepareExpression
		public IServerExpressionPlan PrepareExpression(string AExpression, DataParams AParams, DebugLocator ALocator)
		{
			BeginCall();
			try
			{
				int LContextHashCode = GetContextHashCode(AParams);
				IServerExpressionPlan LPlan = ServerSession.GetCachedPlan(this, AExpression, LContextHashCode) as IServerExpressionPlan;
				if (LPlan != null)
				{
					FPlans.Add(LPlan);
					((ServerPlan)LPlan).Program.SetSourceContext(new SourceContext(AExpression, ALocator));
				}
				else
				{
					ParserMessages LMessages = new ParserMessages();
					LPlan = (IServerExpressionPlan)CompileExpression(ParseExpression(AExpression), LMessages, AParams, new SourceContext(AExpression, ALocator));
					if (!LPlan.Messages.HasErrors)
						ServerSession.AddCachedPlan(this, AExpression, LContextHashCode, (ServerPlan)LPlan);
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
        
		// UnprepareExpression
		public void UnprepareExpression(IServerExpressionPlan APlan)
		{
			InternalUnprepare((ServerPlan)APlan);
		}
		
		// Evaluate
		public DataValue Evaluate(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Evaluate(AParams);
			}
			finally
			{
				UnprepareExpression(LPlan);
			}
		}
		
		// OpenCursor
		public IServerCursor OpenCursor(string AExpression, DataParams AParams)
		{
			IServerExpressionPlan LPlan = PrepareExpression(AExpression, AParams);
			try
			{
				return LPlan.Open(AParams);
			}
			catch
			{
				UnprepareExpression(LPlan);
				throw;
			}
		}
		
		// CloseCursor
		public void CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			try
			{
				LPlan.Close(ACursor);
			}
			finally
			{
				UnprepareExpression(LPlan);
			}
		}
		
		public IServerScript PrepareScript(string AScript)
		{
			return PrepareScript(AScript, null);
		}
        
		// PrepareScript
		public IServerScript PrepareScript(string AScript, DebugLocator ALocator)
		{
			BeginCall();
			try
			{
				ServerScript LScript = new ServerScript(this, AScript, ALocator);
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
        
		// UnprepareScript
		public void UnprepareScript(IServerScript AScript)
		{
			BeginCall();
			try
			{
				((ServerScript)AScript).Dispose();
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
		
		// ExecuteScript
		public void ExecuteScript(string AScript)
		{
			IServerScript LScript = PrepareScript(AScript);
			try
			{
				LScript.Execute(null);
			}
			finally
			{
				UnprepareScript(LScript);
			}
		}

		// Transaction		
		private IServerDTCTransaction FDTCTransaction;
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
			SRFLogger.WriteLine(TraceLevel.Verbose, "Will begin transaction {0}", AIsolationLevel);
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
			SRFLogger.WriteLine(TraceLevel.Verbose, "Will commit transaction");
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
			FTransactions.BeginTransaction(this, AIsolationLevel);

			if (UseDTC && (FDTCTransaction == null))
			{
				CloseDeviceSessions();
				FDTCTransaction = (IServerDTCTransaction)Activator.CreateInstance(Type.GetType("Alphora.Dataphor.DAE.Server.ServerDTCTransaction,Alphora.Dataphor.DAE.Server"));
				FDTCTransaction.IsolationLevel = AIsolationLevel;
			}
			
			// Begin a transaction on all remote sessions
			foreach (RemoteSession LRemoteSession in RemoteSessions)
				LRemoteSession.BeginTransaction(AIsolationLevel);

			// Begin a transaction on all device sessions
			foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
				LDeviceSession.BeginTransaction(AIsolationLevel);
		}
		
		protected internal void InternalPrepareTransaction()
		{
			CheckInTransaction();

			ServerTransaction LTransaction = FTransactions.CurrentTransaction();
			if (!LTransaction.Prepared)
			{
				foreach (RemoteSession LRemoteSession in RemoteSessions)
					LRemoteSession.PrepareTransaction();
					
				foreach (Schema.DeviceSession LDeviceSession in DeviceSessions)
					LDeviceSession.PrepareTransaction();
					
				// Create a new program to perform the deferred validation
				Program LProgram = new Program(this);
				LProgram.Start(null);
				try
				{
					// Invoke event handlers for work done during this transaction
					LTransaction.InvokeDeferredHandlers(LProgram);

					// Validate Constraints which have been affected by work done during this transaction
					FTransactions.ValidateDeferredConstraints(LProgram);

					foreach (Schema.CatalogConstraint LConstraint in LTransaction.CatalogConstraints)
						LConstraint.Validate(LProgram);
				}
				finally
				{
					LProgram.Stop(null);
				}
				
				LTransaction.Prepared = true;
			}
		}
		
		protected internal void InternalCommitTransaction()
		{
			CheckInTransaction();

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
		}

		protected internal void InternalRollbackTransaction()
		{
			CheckInTransaction();
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
		
		// BeginApplicationTransaction
		public Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				Guid LATID = ApplicationTransactionUtility.BeginApplicationTransaction(this);
				
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
		
		// PrepareApplicationTransaction
		public void PrepareApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.PrepareApplicationTransaction(this, AID);
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
		
		// CommitApplicationTransaction
		public void CommitApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.CommitApplicationTransaction(this, AID);
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
		
		// RollbackApplicationTransaction
		public void RollbackApplicationTransaction(Guid AID)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.RollbackApplicationTransaction(this, AID);
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

		public void JoinApplicationTransaction(Guid AID, bool AIsInsert)
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.JoinApplicationTransaction(this, AID);
				FApplicationTransactionID = AID;
				FIsInsert = AIsInsert;
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
		
		public void LeaveApplicationTransaction()
		{
			Exception LException = null;
			int LNestingLevel = BeginTransactionalCall();
			try
			{
				ApplicationTransactionUtility.LeaveApplicationTransaction(this);
				FApplicationTransactionID = Guid.Empty;
				FIsInsert = false;
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
		
		private Programs FExecutingPrograms;
		internal Programs ExecutingPrograms { get { return FExecutingPrograms; } }
		
		internal Program ExecutingProgram
		{
			get
			{
				if (FExecutingPrograms.Count == 0)
					throw new ServerException(ServerException.Codes.NoExecutingProgram);
				return FExecutingPrograms[FExecutingPrograms.Count - 1];
			}
		}
		
		internal void PushExecutingProgram(Program AProgram)
		{
			FExecutingPrograms.Add(AProgram);
		}
		
		internal void PopExecutingProgram(Program AProgram)
		{
			if (ExecutingProgram.ID != AProgram.ID)
				throw new ServerException(ServerException.Codes.ProgramNotExecuting, AProgram.ID.ToString());
			FExecutingPrograms.RemoveAt(FExecutingPrograms.Count - 1);
		}
		
		// Execution
		private System.Threading.Thread FExecutingThread;
		public System.Threading.Thread ExecutingThread { get { return FExecutingThread; } }
		
		private int FExecutingThreadCount = 0;
		
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
		
		// Debugging
		
		private Debugger FDebuggedBy;
		public Debugger DebuggedBy { get { return FDebuggedBy; } }
		
		internal void SetDebuggedBy(Debugger ADebuggedBy)
		{
			if ((FDebuggedBy != null) && (ADebuggedBy != null))
				throw new ServerException(ServerException.Codes.DebuggerAlreadyAttached, FProcessID);
			FDebuggedBy = ADebuggedBy;
		}
		
		private bool FBreakNext;
		private int FBreakOnProgramDepth;
		private int FBreakOnCallDepth;
		
		internal void SetStepOver()
		{
			FBreakOnProgramDepth = ExecutingPrograms.Count;
			FBreakOnCallDepth = ExecutingProgram.Stack.CallDepth;
		}
		
		internal void SetStepInto()
		{
			FBreakNext = true;
		}
		
		internal void ClearBreakInfo()
		{
			FBreakNext = false;
			FBreakOnProgramDepth = -1;
			FBreakOnCallDepth = -1;
		}
		
		private bool InternalShouldBreak()
		{
			if (FBreakNext)
				return true;
				
			if (ExecutingPrograms.Count < FBreakOnProgramDepth)
			{
				FBreakOnProgramDepth = ExecutingPrograms.Count;
				FBreakOnCallDepth = ExecutingProgram.Stack.CallDepth;
			}
			
			if (ExecutingProgram.Stack.CallDepth < FBreakOnCallDepth)
			{
				FBreakOnCallDepth = ExecutingProgram.Stack.CallDepth;
			}
				
			if ((FBreakOnProgramDepth == ExecutingPrograms.Count) && (FBreakOnCallDepth == ExecutingProgram.Stack.CallDepth))
				return true;
				
			return false;
		}
		
		internal bool ShouldBreak()
		{
			if (InternalShouldBreak())
			{
				ClearBreakInfo();
				return true;
			}
			
			return false;
		}
		
		// Catalog
		public Schema.Catalog Catalog { get { return FServerSession.Server.Catalog; } }
		
		public Schema.DataTypes DataTypes { get { return FServerSession.Server.Catalog.DataTypes; } }

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
		
		/// <summary>Returns true if the Server-level loading flag is set, or this process is in a loading context</summary>
		public bool IsLoading()
		{
			return InLoadingContext();
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
