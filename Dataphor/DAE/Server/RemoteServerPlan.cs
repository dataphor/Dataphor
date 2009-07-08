/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

//#define TRACEEVENTS // Enable this to turn on tracing
#define ALLOWPROCESSCONTEXT
#define LOADFROMLIBRARIES

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerPlan	
	public abstract class RemoteServerPlan : ServerPlanBase, IRemoteServerPlan
	{
		protected internal RemoteServerPlan(ServerProcess AProcess) : base(AProcess) {}

		public IRemoteServerProcess Process { get { return (IRemoteServerProcess)FProcess; } }
		
		public Exception[] Messages
		{
			get
			{
				Exception[] LMessages = new Exception[Plan.Messages.Count];
				for (int LIndex = 0; LIndex < Plan.Messages.Count; LIndex++)
					LMessages[LIndex] = Plan.Messages[LIndex];
				return LMessages;
			}
		}
	}

	// RemoteServerStatementPlan	
	public class RemoteServerStatementPlan : RemoteServerPlan, IRemoteServerStatementPlan
	{
		protected internal RemoteServerStatementPlan(ServerProcess AProcess) : base(AProcess) {}
		
		public void Execute(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				FProcess.Execute(this, FCode, LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = Statistics.ExecuteTime;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
	}
	
	// RemoteServerExpressionPlan
	public class RemoteServerExpressionPlan : RemoteServerPlan, IRemoteServerExpressionPlan
	{
		protected internal RemoteServerExpressionPlan(ServerProcess AProcess) : base(AProcess) {}
		
		protected override void Dispose(bool ADisposing)
		{
			RemoveCacheReference();
			base.Dispose(ADisposing);
		}
		
		public byte[] Evaluate(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				DataVar LVar = ServerProcess.Execute(this, Code, LParams);
				FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
				AExecuteTime = Statistics.ExecuteTime;
				if (LVar.Value == null)
					return null;
				if (LVar.DataType.Equivalent(LVar.Value.DataType))
					return LVar.Value.AsPhysical;
				return LVar.Value.CopyAs(LVar.DataType).AsPhysical;
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(LException);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
		}
		
		/// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
		/// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		public IRemoteServerCursor Open(ref RemoteParamData AParams, out TimeSpan AExecuteTime, ProcessCallInfo ACallInfo)
		{
			FProcess.ProcessCallInfo(ACallInfo);
			IRemoteServerCursor LServerCursor;
			//ServerProcess.RaiseTraceEvent(TraceCodes.BeginOpenCursor, "Begin Open Cursor");
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				CheckCompiled();
				DataParams LParams = FProcess.RemoteParamDataToDataParams(AParams);
				RemoteServerCursor LCursor = new RemoteServerCursor(this, LParams);
				try
				{
					LCursor.Open();
					FProcess.DataParamsToRemoteParamData(LParams, ref AParams);
					AExecuteTime = Statistics.ExecuteTime;
					LServerCursor = (IRemoteServerCursor)LCursor;
				}
				catch
				{
					Close((IRemoteServerCursor)LCursor, FProcess.EmptyCallInfo());
					throw;
				}
			}
			catch (Exception E)
			{
				if (Header != null)
					Header.IsInvalidPlan = true;
					
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
			}
			//ServerProcess.RaiseTraceEvent(TraceCodes.EndOpenCursor, "End Open Cursor");
			return LServerCursor;
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
			Exception LException = null;
			int LNestingLevel = FProcess.BeginTransactionalCall();
			try
			{
				((RemoteServerCursor)ACursor).Dispose();
			}
			catch (Exception E)
			{
				LException = E;
				throw WrapException(E);
			}
			finally
			{
				FProcess.EndTransactionalCall(LNestingLevel, LException);
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
			CheckCompiled();

			if (Code.DataType is Schema.ICursorType)
				ACatalogObjectName = Schema.Object.NameFromGuid(ID);
			else
				ACatalogObjectName = Code.DataType.Name;
				
			#if LOGCACHEEVENTS
			ServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for expression '{0}'.", Header.Statement));
			#endif

			ACacheChanged = true;
			ACacheTimeStamp = ServerProcess.ServerSession.Server.CacheTimeStamp;
			string[] LRequiredObjects = ServerProcess.ServerSession.Server.CatalogCaches.GetRequiredObjects(ServerProcess.ServerSession, Plan.PlanCatalog, ACacheTimeStamp, out AClientCacheTimeStamp);
			if (LRequiredObjects.Length > 0)
			{
				if (Code.DataType is Schema.ICursorType)
				{
					string[] LAllButCatalogObject = new string[LRequiredObjects.Length - 1];
					int LTargetIndex = 0;
					for (int LIndex = 0; LIndex < LRequiredObjects.Length; LIndex++)
						if (LRequiredObjects[LIndex] != ACatalogObjectName)
						{
							LAllButCatalogObject[LTargetIndex] = LRequiredObjects[LIndex];
							LTargetIndex++;
						}
						
					Block LStatement = LAllButCatalogObject.Length > 0 ? (Block)Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, LAllButCatalogObject) : new Block();
					
					// Add variable declaration statements for any process context that may be being referenced by the plan
					for (int LIndex = ServerProcess.Context.Count - 1; LIndex >= 0; LIndex--)
						if (!ContainsParam(AParams, ServerProcess.Context[LIndex].Name))
							LStatement.Statements.Add(new VariableStatement(ServerProcess.Context[LIndex].Name, ServerProcess.Context[LIndex].DataType.EmitSpecifier(EmitMode.ForRemote)));
					
					Block LCatalogObjectStatement = (Block)Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, new string[]{ ACatalogObjectName });
					LStatement.Statements.AddRange(LCatalogObjectStatement.Statements);
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(LStatement);
					return LCatalogString;
				}
				else
				{
					string LCatalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(Plan.PlanCatalog.EmitStatement(ServerProcess, EmitMode.ForRemote, LRequiredObjects));
					return LCatalogString;
				}
			}
			return String.Empty;
		}

		public void RemoveCacheReference()
		{
			// Remove the cache object describing the result set for the plan
			if ((Code != null) && (Code.DataType is Schema.ICursorType))
				ServerProcess.ServerSession.Server.CatalogCaches.RemovePlanDescriptor(ServerProcess.ServerSession, Schema.Object.NameFromGuid(ID));
		}
		
		// SourceNode
		public TableNode SourceNode 
		{ 
			get 
			{ 
				CheckCompiled();
				return (TableNode)Code.Nodes[0]; 
			} 
		}

		// Isolation
		public CursorIsolation Isolation 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorIsolation; 
			} 
		}
		
		// CursorType
		public CursorType CursorType 
		{ 
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorType; 
			} 
		}

		// Capabilities		
		public CursorCapability Capabilities 
		{
			get 
			{ 
				CheckCompiled();
				return SourceNode.CursorCapabilities; 
			} 
		}

		public bool Supports(CursorCapability ACapability)
		{
			return (Capabilities & ACapability) != 0;
		}
	}
}
