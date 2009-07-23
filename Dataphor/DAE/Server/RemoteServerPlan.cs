/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

#define ALLOWPROCESSCONTEXT

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerPlan	
	public abstract class RemoteServerPlan : RemoteServerChildObject, IServerPlanBase, IRemoteServerPlan
	{
		protected internal RemoteServerPlan(RemoteServerProcess AProcess, ServerPlan AServerPlan)
		{
			FProcess = AProcess;
			FServerPlan = AServerPlan;
			AttachServerPlan();
		}
		
		private void AttachServerPlan()
		{
			FServerPlan.Disposed += new EventHandler(FServerPlanDisposed);
		}

		private void FServerPlanDisposed(object ASender, EventArgs AArgs)
		{
			DetachServerPlan();
			FServerPlan = null;
			Dispose();
		}
		
		private void DetachServerPlan()
		{
			FServerPlan.Disposed -= new EventHandler(FServerPlanDisposed);
		}

		protected override void Dispose(bool ADisposing)
		{
			if (FServerPlan != null)
			{
				DetachServerPlan();
				FServerPlan.Dispose();
				FServerPlan = null;
			}
			
			base.Dispose(ADisposing);
		}
		
		protected RemoteServerProcess FProcess;
		public RemoteServerProcess Process { get { return FProcess; } }
		
		IRemoteServerProcess IRemoteServerPlan.Process { get { return FProcess; } }
		
		protected ServerPlan FServerPlan;
		internal ServerPlan ServerPlan { get { return FServerPlan; } }
		
		// Execution
		internal Exception WrapException(Exception AException)
		{
			return RemoteServer.WrapException(AException);
		}

		public Exception[] Messages
		{
			get
			{
				Exception[] LMessages = new Exception[FServerPlan.Messages.Count];
				for (int LIndex = 0; LIndex < FServerPlan.Messages.Count; LIndex++)
					LMessages[LIndex] = FServerPlan.Messages[LIndex];
				return LMessages;
			}
		}
		
		public PlanStatistics Statistics { get { return FServerPlan.Statistics; } }
		
		public void CheckCompiled()
		{
			FServerPlan.CheckCompiled();
		}
		
		public Guid ID { get { return FServerPlan.ID; } }
	}

	// RemoteServerStatementPlan	
	public class RemoteServerStatementPlan : RemoteServerPlan, IRemoteServerStatementPlan
	{
		protected internal RemoteServerStatementPlan(RemoteServerProcess AProcess, ServerStatementPlan APlan) : base(AProcess, APlan) { }
		
		internal ServerStatementPlan ServerStatementPlan { get { return (ServerStatementPlan)FServerPlan; } }
		
		public void Execute(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			try
			{
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				ServerStatementPlan.Execute(LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = ServerStatementPlan.Statistics.ExecuteTime;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
	}
	
	// RemoteServerExpressionPlan
	public class RemoteServerExpressionPlan : RemoteServerPlan, IRemoteServerExpressionPlan
	{
		protected internal RemoteServerExpressionPlan(RemoteServerProcess AProcess, ServerExpressionPlan AExpressionPlan) : base(AProcess, AExpressionPlan) {}
		
		internal ServerExpressionPlan ServerExpressionPlan { get { return (ServerExpressionPlan)FServerPlan; } }
		
		protected override void Dispose(bool ADisposing)
		{
			try
			{
				RemoveCacheReference();
			}
			finally
			{
				base.Dispose(ADisposing);
			}
		}
		
		public byte[] Evaluate(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			try
			{
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				DataValue LValue = ServerExpressionPlan.Evaluate(LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = ServerExpressionPlan.Statistics.ExecuteTime;
				if (LValue == null)
					return null;
				if (ServerExpressionPlan.DataType.Equivalent(LValue.DataType))
					return LValue.AsPhysical;
				return LValue.CopyAs(ServerExpressionPlan.DataType).AsPhysical;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
		/// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			
			try
			{
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				IServerCursor LServerCursor = ServerExpressionPlan.Open(LParams);
				RemoteServerCursor LCursor = new RemoteServerCursor(this, (ServerCursor)LServerCursor);
				try
				{
					LCursor.Open();
					FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
					AExecuteTime = ServerExpressionPlan.Statistics.ExecuteTime;
					return LCursor;
				}
				catch
				{
					Close(LCursor, FProcess.EmptyCallInfo());
					throw;
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData, ProcessCallInfo ACallInfo)
		{
			IRemoteServerCursor LServerCursor = Open(ref AParams, out AExecuteTime, ACallInfo);
			AFetchData = LServerCursor.Fetch(out ABookmarks, ACount, FProcess.EmptyCallInfo());
			return LServerCursor;
		}
		
		/// <summary> Closes a remote, server-side cursor previously created using Open. </summary>
		/// <param name="ACursor"> The cursor to close. </param>
		public void Close(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			try
			{
				ServerExpressionPlan.Close(((RemoteServerCursor)ACursor).ServerCursor);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		private bool ContainsParam(RemoteParam[] AParams, string AParamName)
		{
			if (AParams == null)
				return false;
				
			for (int LIndex = 0; LIndex < AParams.Length; LIndex++)
				if (AParams[LIndex].Name == AParamName)
					return true;
			return false;
		}

		public string GetCatalog(RemoteParam[] AParams, out string ACatalogObjectName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged)
		{
			ServerExpressionPlan.CheckCompiled();

			if (ServerExpressionPlan.Code.DataType is Schema.ICursorType)
			{
				FCacheObjectName = Schema.Object.NameFromGuid(ID);
				ACatalogObjectName = FCacheObjectName;
			}
			else
				ACatalogObjectName = ServerExpressionPlan.Code.DataType.Name;
				
			#if LOGCACHEEVENTS
			ServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for expression '{0}'.", Header.Statement));
			#endif

			ACacheChanged = true;
			ACacheTimeStamp = ServerExpressionPlan.ServerProcess.ServerSession.Server.CacheTimeStamp;
			string[] LRequiredObjects = FProcess.Session.Server.CatalogCaches.GetRequiredObjects(FProcess.Session, ServerExpressionPlan.Plan.PlanCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				if (ServerExpressionPlan.Code.DataType is Schema.ICursorType)
				{
					string[] LAllButCatalogObject = new string[LRequiredObjects.Length - 1];
					int LTargetIndex = 0;
					for (int LIndex = 0; LIndex < LRequiredObjects.Length; LIndex++)
						if (LRequiredObjects[LIndex] != ACatalogObjectName)
						{
							LAllButCatalogObject[LTargetIndex] = LRequiredObjects[LIndex];
							LTargetIndex++;
						}
						
					Block LStatement = LAllButCatalogObject.Length > 0 ? (Block)ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess, EmitMode.ForRemote, LAllButCatalogObject) : new Block();
					
					#if ALLOWPROCESSCONTEXT
					// Add variable declaration statements for any process context that may be being referenced by the plan
					for (int LIndex = ServerExpressionPlan.ServerProcess.ProcessContext.Count - 1; LIndex >= 0; LIndex--)
						if (!ContainsParam(AParams, ServerExpressionPlan.ServerProcess.ProcessContext[LIndex].Name))
							LStatement.Statements.Add(new VariableStatement(ServerExpressionPlan.ServerProcess.ProcessContext[LIndex].Name, ServerExpressionPlan.ServerProcess.ProcessContext[LIndex].DataType.EmitSpecifier(EmitMode.ForRemote)));
					#endif
					
					Block LCatalogObjectStatement = (Block)ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess, EmitMode.ForRemote, new string[]{ ACatalogObjectName });
					LStatement.Statements.AddRange(LCatalogObjectStatement.Statements);
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LStatement);
					return LCatalogString;
				}
				else
				{
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess, EmitMode.ForRemote, LRequiredObjects));
					return LCatalogString;
				}
			}
			return String.Empty;
		}
		
		private string FCacheObjectName;

		public void RemoveCacheReference()
		{
			// Remove the cache object describing the result set for the plan
			if (!String.IsNullOrEmpty(FCacheObjectName))
				FProcess.Session.Server.CatalogCaches.RemovePlanDescriptor(FProcess.Session, FCacheObjectName);
		}
		
		// Isolation
		public CursorIsolation Isolation { get { return ServerExpressionPlan.Isolation; } }

		// CursorType
		public CursorType CursorType { get { return ServerExpressionPlan.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return ServerExpressionPlan.Capabilities; } }

		public bool Supports(CursorCapability ACapability)
		{
			return ServerExpressionPlan.Supports(ACapability);
		}
	}
}
