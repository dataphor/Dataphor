/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
    public class LocalPlan : LocalServerChildObject, IServerPlan
    {
		public LocalPlan(LocalProcess AProcess, IRemoteServerPlan APlan, PlanDescriptor APlanDescriptor) : base()
		{
			FProcess = AProcess;
			FPlan = APlan;
			FDescriptor = APlanDescriptor;
		}
		
		protected override void Dispose(bool ADisposing)
		{
			FDescriptor = null;
			FProcess = null;
			FPlan = null;
			base.Dispose(ADisposing);
		}
		
		private IRemoteServerPlan FPlan;
		
		protected PlanDescriptor FDescriptor;
		
		protected internal LocalProcess FProcess;
        /// <value>Returns the <see cref="IServerProcess"/> instance for this plan.</value>
        public IServerProcess Process { get { return FProcess; } } 

		public LocalServer LocalServer { get { return FProcess.FSession.FServer; } }
		
		public Guid ID { get { return FDescriptor.ID; } }
		
		private CompilerMessages FMessages;
		public CompilerMessages Messages
		{
			get
			{
				if (FMessages == null)
				{
					FMessages = new CompilerMessages();
					FMessages.AddRange(FDescriptor.Messages);
				}
				return FMessages;
			}
		}
		
		public void CheckCompiled()
		{
			FPlan.CheckCompiled();
		}
		
		// Statistics
		internal bool FStatisticsCached = true;
		public PlanStatistics Statistics 
		{ 
			get 
			{ 
				if (!FStatisticsCached)
				{
					FDescriptor.Statistics = FPlan.Statistics;
					FStatisticsCached = true;
				}
				return FDescriptor.Statistics; 
			} 
		}
	}
    
    public class LocalExpressionPlan : LocalPlan, IServerExpressionPlan
    {
		public LocalExpressionPlan(LocalProcess AProcess, IRemoteServerExpressionPlan APlan, PlanDescriptor APlanDescriptor, DataParams AParams) : base(AProcess, APlan, APlanDescriptor)
		{
			FPlan = APlan;
			FParams = AParams;
			GetDataType();
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				if (FDataType != null)
					DropDataType();
			}
			finally
			{
				FPlan = null;
				FParams = null;
				FDataType = null;
				base.Dispose(ADisposing);
			}
		}

		protected DataParams FParams;
		protected IRemoteServerExpressionPlan FPlan;
		public IRemoteServerExpressionPlan RemotePlan { get { return FPlan; } }
		
		// Isolation
		public CursorIsolation Isolation { get { return FDescriptor.CursorIsolation; } }
		
		// CursorType
		public CursorType CursorType { get { return FDescriptor.CursorType; } }
		
		// Capabilities		
		public CursorCapability Capabilities { get { return FDescriptor.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
		
		public DataValue Evaluate(DataParams AParams)
		{
			RemoteParamData LParams = FProcess.DataParamsToRemoteParamData(AParams);
			byte[] LResult = FPlan.Evaluate(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
			FStatisticsCached = false;
			FProcess.RemoteParamDataToDataParams(AParams, LParams);
			return LResult == null ? null : DataValue.FromPhysical(FProcess, DataType, LResult, 0);
		}

        /// <summary>Opens a server-side cursor based on the prepared statement this plan represents.</summary>        
        /// <returns>An <see cref="IServerCursor"/> instance for the prepared statement.</returns>
        public IServerCursor Open(DataParams AParams)
        {
			RemoteParamData LParams = ((LocalProcess)FProcess).DataParamsToRemoteParamData(AParams);
			IRemoteServerCursor LServerCursor;
			LocalCursor LCursor;
			if (FProcess.ProcessInfo.FetchAtOpen && (FProcess.ProcessInfo.FetchCount > 1) && Supports(CursorCapability.Bookmarkable))
			{
				Guid[] LBookmarks;
				RemoteFetchData LFetchData;
				LServerCursor = FPlan.Open(ref LParams, out FDescriptor.Statistics.ExecuteTime, out LBookmarks, FProcess.ProcessInfo.FetchCount, out LFetchData, FProcess.GetProcessCallInfo());
				FStatisticsCached = false;
				LCursor = new LocalCursor(this, LServerCursor);
				LCursor.ProcessFetchData(LFetchData, LBookmarks, true);
			}
			else
			{
				LServerCursor = FPlan.Open(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
				FStatisticsCached = false;
				LCursor = new LocalCursor(this, LServerCursor);
			}
			((LocalProcess)FProcess).RemoteParamDataToDataParams(AParams, LParams);
			return LCursor;
		}
		
        /// <summary>Closes a server-side cursor previously created using Open.</summary>
        /// <param name="ACursor">The cursor to close.</param>
        public void Close(IServerCursor ACursor)
        {
			try
			{
				FPlan.Close(((LocalCursor)ACursor).RemoteCursor, FProcess.GetProcessCallInfo());
			}
			catch
			{
				// ignore exceptions here
			}
			((LocalCursor)ACursor).Dispose();
		}
		
		private void DropDataType()
		{
			if (FTableVar is Schema.DerivedTableVar)
			{
				LocalServer.AcquireCacheLock(FProcess, LockMode.Exclusive);
				try
				{
					(new DropViewNode((Schema.DerivedTableVar)FTableVar)).Execute(FProcess.FInternalProcess);
				}
				finally
				{
					LocalServer.ReleaseCacheLock(FProcess, LockMode.Exclusive);
				}
			}
		}
		
		private Schema.IDataType GetDataType()
		{
			bool LTimeStampSet = false;
			try
			{
				LocalServer.WaitForCacheTimeStamp(FProcess, FDescriptor.CacheChanged ? FDescriptor.ClientCacheTimeStamp - 1 : FDescriptor.ClientCacheTimeStamp);
				LocalServer.AcquireCacheLock(FProcess, FDescriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				try
				{
					FProcess.FInternalProcess.Context.PushWindow(0);
					try
					{
						if (FDescriptor.CacheChanged)
						{
							LocalServer.EnsureCacheConsistent(FDescriptor.CacheTimeStamp);
							try
							{
								if (FDescriptor.Catalog != String.Empty)
								{
									IServerScript LScript = ((IServerProcess)FProcess.FInternalProcess).PrepareScript(FDescriptor.Catalog);
									try
									{
										LScript.Execute(FParams);
									}
									finally
									{
										((IServerProcess)FProcess.FInternalProcess).UnprepareScript(LScript);
									}
								}
							}
							finally
							{
								LocalServer.SetCacheTimeStamp(FProcess, FDescriptor.ClientCacheTimeStamp);
								LTimeStampSet = true;
							}
						}
						
						if (LocalServer.Catalog.ContainsName(FDescriptor.ObjectName))
						{
							Schema.Object LObject = LocalServer.Catalog[FDescriptor.ObjectName];
							if (LObject is Schema.TableVar)
							{
								FTableVar = (Schema.TableVar)LObject;
								Plan LPlan = new Plan(FProcess.FInternalProcess);
								try
								{
									if (FParams != null)
										foreach (DataParam LParam in FParams)
											LPlan.Symbols.Push(new DataVar(LParam.Name, LParam.DataType));
											
									for (int LIndex = FProcess.FInternalProcess.Context.Count - 1; LIndex >= 0; LIndex--)
										LPlan.Symbols.Push(FProcess.FInternalProcess.Context[LIndex]);
									FTableNode = (TableNode)Compiler.EmitTableVarNode(LPlan, FTableVar);
								}
								finally
								{
									LPlan.Dispose();
								}
								FDataType = FTableVar.DataType;			
							}
							else
								FDataType = (Schema.IDataType)LObject;
						}
						else
						{
							try
							{
								Plan LPlan = new Plan(FProcess.FInternalProcess);
								try
								{
									FDataType = Compiler.CompileTypeSpecifier(LPlan, new DAE.Language.D4.Parser().ParseTypeSpecifier(FDescriptor.ObjectName));
								}
								finally
								{
									LPlan.Dispose();
								}
							}
							catch
							{
								// Notify the server that the client cache is out of sync
								Process.Execute(".System.UpdateTimeStamps();", null);
								throw;
							}
						}
						
						return FDataType;
					}
					finally
					{
						FProcess.FInternalProcess.Context.PopWindow();
					}
				}
				finally
				{
					LocalServer.ReleaseCacheLock(FProcess, FDescriptor.CacheChanged ? LockMode.Exclusive : LockMode.Shared);
				}
			}
			catch (Exception E)
			{
				// Notify the server that the client cache is out of sync
				Process.Execute(".System.UpdateTimeStamps();", null);
				E = new ServerException(ServerException.Codes.CacheDeserializationError, E, FDescriptor.ClientCacheTimeStamp);
				LocalServer.FInternalServer.LogError(E);
				throw E;
			}
			finally
			{
				if (!LTimeStampSet)
					LocalServer.SetCacheTimeStamp(FProcess, FDescriptor.ClientCacheTimeStamp);
			}
		}
		
		public Schema.Catalog Catalog 
		{ 
			get 
			{ 
				if (FDataType == null)
					GetDataType();
				return LocalServer.Catalog;
			} 
		}

		protected Schema.TableVar FTableVar;		
		public Schema.TableVar TableVar
		{
			get
			{
				if (FDataType == null)
					GetDataType();
				return FTableVar;
			}
		}
		
		protected TableNode FTableNode;
		public TableNode TableNode
		{
			get
			{
				if (FDataType == null)
					GetDataType();
				return FTableNode;
			}
		}
		
		protected Schema.IDataType FDataType;
		public Schema.IDataType DataType
		{
			get
			{
				if (FDataType == null)
					return GetDataType(); 
				return FDataType;
			}
		}

		private Schema.Order FOrder;
		public Schema.Order Order
        {
			get 
			{
				if ((FOrder == null) && (FDescriptor.Order != String.Empty))
					FOrder = Compiler.CompileOrderDefinition(FProcess.FInternalProcess.Plan, TableVar, new Parser().ParseOrderDefinition(FDescriptor.Order), false);
				return FOrder; 
			}
		}
		
		Statement IServerExpressionPlan.EmitStatement()
		{
			throw new ServerException(ServerException.Codes.Unsupported);
		}
		
		public Row RequestRow()
		{
			return new Row(FProcess, TableVar.DataType.RowType);
		}
		
		public void ReleaseRow(Row ARow)
		{
			ARow.Dispose();
		}
    }
    
    public class LocalStatementPlan : LocalPlan, IServerStatementPlan
    {
		public LocalStatementPlan(LocalProcess AProcess, IRemoteServerStatementPlan APlan, PlanDescriptor APlanDescriptor) : base(AProcess, APlan, APlanDescriptor)
		{
			FPlan = APlan;
		}

		protected override void Dispose(bool ADisposing)
		{
			FPlan = null;
			base.Dispose(ADisposing);
		}

		protected IRemoteServerStatementPlan FPlan;
		public IRemoteServerStatementPlan RemotePlan { get { return FPlan; } }
		
        /// <summary>Executes the prepared statement this plan represents.</summary>
        public void Execute(DataParams AParams)
        {
			RemoteParamData LParams = FProcess.DataParamsToRemoteParamData(AParams);
			FPlan.Execute(ref LParams, out FDescriptor.Statistics.ExecuteTime, FProcess.GetProcessCallInfo());
			FStatisticsCached = false;
			FProcess.RemoteParamDataToDataParams(AParams, LParams);
		}
    }
}
