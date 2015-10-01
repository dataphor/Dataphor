/*
	Alphora Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.Text;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Instructions;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Contracts;

namespace Alphora.Dataphor.DAE.Server
{
	// RemoteServerPlan	
	public abstract class RemoteServerPlan : RemoteServerChildObject, IServerPlanBase, IRemoteServerPlan
	{
		protected internal RemoteServerPlan(RemoteServerProcess process, ServerPlan serverPlan)
		{
			_process = process;
			_serverPlan = serverPlan;
			AttachServerPlan();
		}
		
		private void AttachServerPlan()
		{
			_serverPlan.Disposed += new EventHandler(FServerPlanDisposed);
			_serverPlan.Released += new EventHandler(FServerPlanDisposed);
		}
		
		private void FServerPlanDisposed(object sender, EventArgs args)
		{
			Dispose();
		}
		
		private void DetachServerPlan()
		{
			_serverPlan.Disposed -= new EventHandler(FServerPlanDisposed);
			_serverPlan.Released -= new EventHandler(FServerPlanDisposed);
		}

		protected override void Dispose(bool disposing)
		{
			if (_serverPlan != null)
			{
				DetachServerPlan();
				//FServerPlan.Dispose(); // Do not dispose the base plan, disposal will be handled by the server (it may be cached)
				_serverPlan = null;
			}
			
			base.Dispose(disposing);
		}
		
		protected RemoteServerProcess _process;
		public RemoteServerProcess Process { get { return _process; } }
		
		IRemoteServerProcess IRemoteServerPlan.Process { get { return _process; } }
		
		protected ServerPlan _serverPlan;
		internal ServerPlan ServerPlan { get { return _serverPlan; } }
		
		public abstract void Unprepare();
		
		// Execution
		internal Exception WrapException(Exception exception)
		{
			return RemoteServer.WrapException(exception);
		}

		public Exception[] Messages
		{
			get
			{
				Exception[] messages = new Exception[_serverPlan.Messages.Count];
				for (int index = 0; index < _serverPlan.Messages.Count; index++)
					messages[index] = _serverPlan.Messages[index];
				return messages;
			}
		}
		
		public PlanStatistics PlanStatistics { get { return _serverPlan.PlanStatistics; } }
		
		public ProgramStatistics ProgramStatistics { get { return _serverPlan.ProgramStatistics; } }
		
		public void CheckCompiled()
		{
			_serverPlan.CheckCompiled();
		}
		
		public Guid ID { get { return _serverPlan.ID; } }
	}

	// RemoteServerStatementPlan	
	public class RemoteServerStatementPlan : RemoteServerPlan, IRemoteServerStatementPlan
	{
		protected internal RemoteServerStatementPlan(RemoteServerProcess process, ServerStatementPlan plan) : base(process, plan) { }
		
		internal ServerStatementPlan ServerStatementPlan { get { return (ServerStatementPlan)_serverPlan; } }
		
		public override void Unprepare()
		{
			_process.UnprepareStatement(this);
		}
		
		public void Execute(ref RemoteParamData paramsValue, out ProgramStatistics executeTime, ProcessCallInfo callInfo)
		{
			_process.ProcessCallInfo(callInfo);
			try
			{
				DataParams localParamsValue = _process.RemoteParamDataToDataParams(paramsValue);
				ServerStatementPlan.Execute(localParamsValue);
				_process.DataParamsToRemoteParamData(localParamsValue, ref paramsValue);
				executeTime = ServerStatementPlan.ProgramStatistics;
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
		protected internal RemoteServerExpressionPlan(RemoteServerProcess process, ServerExpressionPlan expressionPlan) : base(process, expressionPlan) {}
		
		internal ServerExpressionPlan ServerExpressionPlan { get { return (ServerExpressionPlan)_serverPlan; } }
		
		protected override void Dispose(bool disposing)
		{
			try
			{
				RemoveCacheReference();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		public override void Unprepare()
		{
			_process.UnprepareExpression(this);
		}
		
		public byte[] Evaluate(ref RemoteParamData paramsValue, out ProgramStatistics executeTime, ProcessCallInfo callInfo)
		{
			_process.ProcessCallInfo(callInfo);
			try
			{
				DataParams localParamsValue = _process.RemoteParamDataToDataParams(paramsValue);
				IDataValue tempValue = ServerExpressionPlan.Evaluate(localParamsValue);
				_process.DataParamsToRemoteParamData(localParamsValue, ref paramsValue);
				executeTime = ServerExpressionPlan.ProgramStatistics;
				if (tempValue == null)
					return null;
				if (ServerExpressionPlan.DataType.Equivalent(tempValue.DataType))
					return tempValue.AsPhysical;
				return tempValue.CopyAs(ServerExpressionPlan.DataType).AsPhysical;
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		/// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
		/// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		public IRemoteServerCursor Open(ref RemoteParamData paramsValue, out ProgramStatistics executeTime, ProcessCallInfo callInfo)
		{
			_process.ProcessCallInfo(callInfo);
			
			try
			{
				DataParams localParamsValue = _process.RemoteParamDataToDataParams(paramsValue);
				IServerCursor serverCursor = ServerExpressionPlan.Open(localParamsValue);
				RemoteServerCursor cursor = new RemoteServerCursor(this, (ServerCursor)serverCursor);
				try
				{
					cursor.Open();
					_process.DataParamsToRemoteParamData(localParamsValue, ref paramsValue);
					executeTime = ServerExpressionPlan.ProgramStatistics;
					return cursor;
				}
				catch
				{
					Close(cursor, _process.EmptyCallInfo());
					throw;
				}
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		public IRemoteServerCursor Open(ref RemoteParamData paramsValue, out ProgramStatistics executeTime, out Guid[] bookmarks, int count, out RemoteFetchData fetchData, ProcessCallInfo callInfo)
		{
			IRemoteServerCursor serverCursor = Open(ref paramsValue, out executeTime, callInfo);
			fetchData = serverCursor.Fetch(out bookmarks, count, true, _process.EmptyCallInfo());
			return serverCursor;
		}
		
		/// <summary> Closes a remote, server-side cursor previously created using Open. </summary>
		/// <param name="cursor"> The cursor to close. </param>
		public void Close(IRemoteServerCursor cursor, ProcessCallInfo callInfo)
		{
			_process.ProcessCallInfo(callInfo);
			try
			{
				ServerExpressionPlan.Close(((RemoteServerCursor)cursor).ServerCursor);
			}
			catch (Exception E)
			{
				throw WrapException(E);
			}
		}
		
		private bool ContainsParam(RemoteParam[] paramsValue, string paramName)
		{
			if (paramsValue == null)
				return false;
				
			for (int index = 0; index < paramsValue.Length; index++)
				if (paramsValue[index].Name == paramName)
					return true;
			return false;
		}

		public string GetCatalog(RemoteParam[] paramsValue, out string catalogObjectName, out long cacheTimeStamp, out long clientCacheTimeStamp, out bool cacheChanged)
		{
			ServerExpressionPlan.CheckCompiled();

			if (ServerExpressionPlan.ActualDataType is Schema.ICursorType)
			{
				_cacheObjectName = Schema.Object.NameFromGuid(ID);
				catalogObjectName = _cacheObjectName;
			}
			else
				catalogObjectName = ServerExpressionPlan.DataType.Name;
				
			#if LOGCACHEEVENTS
			ServerProcess.ServerSession.Server.LogMessage(String.Format("Getting catalog for expression '{0}'.", Header.Statement));
			#endif

			cacheChanged = true;
			cacheTimeStamp = ServerExpressionPlan.ServerProcess.ServerSession.Server.CacheTimeStamp;
			string[] requiredObjects = _process.Session.Server.CatalogCaches.GetRequiredObjects(_process.Session, ServerExpressionPlan.Plan.PlanCatalog, cacheTimeStamp, out clientCacheTimeStamp);
			if (requiredObjects.Length > 0)
			{
				if (ServerExpressionPlan.ActualDataType is Schema.ICursorType)
				{
					string[] allButCatalogObject = new string[requiredObjects.Length - 1];
					int targetIndex = 0;
					for (int index = 0; index < requiredObjects.Length; index++)
						if (requiredObjects[index] != catalogObjectName)
						{
							allButCatalogObject[targetIndex] = requiredObjects[index];
							targetIndex++;
						}
						
					Block statement = allButCatalogObject.Length > 0 ? (Block)ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess.CatalogDeviceSession, EmitMode.ForRemote, allButCatalogObject) : new Block();
					
					// Add variable declaration statements for any process context that may be being referenced by the plan
					for (int index = ServerExpressionPlan.ServerProcess.ProcessLocals.Count - 1; index >= 0; index--)
						if (!ContainsParam(paramsValue, ServerExpressionPlan.ServerProcess.ProcessLocals[index].Name))
							statement.Statements.Add(new VariableStatement(ServerExpressionPlan.ServerProcess.ProcessLocals[index].Name, ServerExpressionPlan.ServerProcess.ProcessLocals[index].DataType.EmitSpecifier(EmitMode.ForRemote)));
					
					Block catalogObjectStatement = (Block)ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess.CatalogDeviceSession, EmitMode.ForRemote, new string[]{ catalogObjectName });
					statement.Statements.AddRange(catalogObjectStatement.Statements);
					string catalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(statement);
					return catalogString;
				}
				else
				{
					string catalogString = new D4TextEmitter(EmitMode.ForRemote).Emit(ServerExpressionPlan.Plan.PlanCatalog.EmitStatement(ServerExpressionPlan.ServerProcess.CatalogDeviceSession, EmitMode.ForRemote, requiredObjects));
					return catalogString;
				}
			}
			return String.Empty;
		}
		
		private string _cacheObjectName;

		public void RemoveCacheReference()
		{
			// Remove the cache object describing the result set for the plan
			if (!String.IsNullOrEmpty(_cacheObjectName))
				_process.Session.Server.CatalogCaches.RemovePlanDescriptor(_process.Session, _cacheObjectName);
		}
		
		// Isolation
		public CursorIsolation Isolation { get { return ServerExpressionPlan.Isolation; } }

		// CursorType
		public CursorType CursorType { get { return ServerExpressionPlan.CursorType; } }

		// Capabilities		
		public CursorCapability Capabilities { get { return ServerExpressionPlan.Capabilities; } }

		public bool Supports(CursorCapability capability)
		{
			return ServerExpressionPlan.Supports(capability);
		}
	}
}
