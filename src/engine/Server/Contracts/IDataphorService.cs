/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Contracts
{
	using Alphora.Dataphor.DAE.Runtime;
	using Alphora.Dataphor.DAE.Server;
	using Alphora.Dataphor.DAE.Streams;
	using Alphora.Dataphor.DAE.Debug;

	[ServiceContract(Name = "IDataphorService", Namespace = "http://dataphor.org/dataphor/3.0/")]
	public interface IDataphorService
	{
		// Server
		#region Server
		
		/// <summary>
		/// Returns the name of the server.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		string GetServerName();

		/// <summary>
		/// Starts the server.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void Start();

		/// <summary>
		/// Stops the server.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void Stop();

		/// <summary>
		/// Gets the current state of the server.
		/// </summary>
		/// <returns></returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ServerState GetState();
		
		/// <summary>
		/// Returns the current cache timestamp of the catalog.
		/// </summary>
		/// <returns></returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		long GetCacheTimeStamp();
		
		/// <summary>
		/// Returns the current derivation timestamp of the catalog.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		long GetDerivationTimeStamp();
		
		#endregion
		
		// Connection
		#region Connection
		
		/// <summary>
		/// Opens a connection to the Dataphor server.
		/// </summary>
		/// <param name="AConnectionName">The name of the connection.</param>
		/// <param name="AHostName">The name of the machine hosting the connection.</param>
		/// <returns>A handle to the newly created connection.</returns>
		/// <remarks>
		/// <para>
		/// Establishing a connection allows multiple sessions from the same host
		/// to utilize the same catalog cache, rather than having to maintain
		/// separate catalog caches for each session.
		/// </para>
		/// <para>
		/// Sessions also perform lifetime management for the connection. If
		/// a connection has not received a ping within a timeout period, the
		/// connection is assumed to be inactive and cleaned up by the server.
		/// </para>
		/// </remarks>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		int OpenConnection(string AConnectionName, string AHostName);
		
		/// <summary>
		/// Performs a ping to notify the server that the connection is still alive.
		/// </summary>
		/// <param name="AConnectionHandle">The handle of the connection to be pinged.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void PingConnection(int AConnectionHandle);
		
		/// <summary>
		/// Closes a connection.
		/// </summary>
		/// <param name="AConnectionHandle">The handle to the connection to be closed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void CloseConnection(int AConnectionHandle);
		
		#endregion
		
		// Session
		#region Session

		/// <summary>
		/// Initiates a connection to the Dataphor server.
		/// </summary>
		/// <param name="AConnectionHandle">The handle to the connection on which the session will be created.</param>
		/// <param name="ASessionInfo">The session information used to authenticate and describe the session.</param>
		/// <returns>A session descriptor that describes the new session.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		SessionDescriptor Connect(int AConnectionHandle, SessionInfo ASessionInfo);

		/// <summary>
		/// Disconnects an active Dataphor session.
		/// </summary>
		/// <param name="ASessionHandle">The handle to the session to be disconnected.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void Disconnect(int ASessionHandle);
		
		#endregion
		
		// Process
		#region Process
		
		/// <summary>
		/// Starts a server process.
		/// </summary>
		/// <param name="ASessionHandle">The handle of the session that will be used to start the process.</param>
		/// <param name="AProcessInfo">The process information used to describe the new process.</param>
		/// <returns>A process descriptor that describes the new process.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ProcessDescriptor StartProcess(int ASessionHandle, ProcessInfo AProcessInfo);
		
		/// <summary>
		/// Stops a server process.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process to be stopped.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void StopProcess(int AProcessHandle);
		
		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the transaction will be started.</param>
		/// <param name="AIsolationLevel">The isolation level of the new transaction.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void BeginTransaction(int AProcessHandle, IsolationLevel AIsolationLevel);
		
		/// <summary>
		/// Prepares an active transaction to be committed.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be prepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void PrepareTransaction(int AProcessHandle);
		
		/// <summary>
		/// Commits an active transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be committed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void CommitTransaction(int AProcessHandle);
		
		/// <summary>
		/// Rolls back an active transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be rolled back.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void RollbackTransaction(int AProcessHandle);
		
		/// <summary>
		/// Gets the number of active transactions.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process for which the number of active transactions will be returned.</param>
		/// <returns>The number of active transactions.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		int GetTransactionCount(int AProcessHandle);

		/// <summary>
		/// Begins an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process that will start the application transaction.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AShouldJoin">Whether or not the process initiating the application transaction should immediately join the new transaction.</param>
		/// <param name="AIsInsert">Whether or not the process should join in insert mode.</param>
		/// <returns>The ID of the new application transaction.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        Guid BeginApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, bool AShouldJoin, bool AIsInsert);
        
        /// <summary>
        /// Prepares an application transaction to be committed.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process that will perform the prepare.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AID">The ID of the application transaction to be prepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void PrepareApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);

		/// <summary>
		/// Commits an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process that will perform the commit.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AID">The ID of the application transaction to be committed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void CommitApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);
        
        /// <summary>
        /// Rolls back an application transaction.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process that will perform the rollback.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AID">The ID of the application transaction to be rolled back.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void RollbackApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);
        
        /// <summary>
        /// Gets the ID of the application transaction in which this process is currently participating, if any.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process.</param>
        /// <returns>The ID of the application transaction.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        Guid GetApplicationTransactionID(int AProcessHandle);

		/// <summary>
		/// Joins the process to an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AID">The ID of the application transaction to join.</param>
		/// <param name="AIsInsert">Whether or not to join in insert mode.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void JoinApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID, bool AIsInsert);
        
        /// <summary>
        /// Leaves an application transaction.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void LeaveApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo);
		
		#endregion
		
		// Plan
		#region Plan
		
		/// <summary>
		/// Prepares a statement for execution.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process that will prepare the statement.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AStatement">The statement to be prepared.</param>
		/// <param name="AParams">Any parameters to the statement.</param>
		/// <param name="ALocator">A locator describing the source of the statement.</param>
		/// <returns>A PlanDescriptor describing the prepared plan.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		PlanDescriptor PrepareStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AStatement, RemoteParam[] AParams, DebugLocator ALocator);
		
		/// <summary>
		/// Executes a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the prepared plan to be executed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the plan.</param>
		/// <returns>An execute result describing the result of the execution.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ExecuteResult ExecutePlan(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams);

		/// <summary>
		/// Unprepares a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void UnprepareStatement(int APlanHandle);
		
		/// <summary>
		/// Executes a statement.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the statement will be executed.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AStatement">The statement to be executed.</param>
		/// <param name="AParams">Any parameters to the statement.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ExecuteResult ExecuteStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AStatement, RemoteParamData AParams);
		
		/// <summary>
		/// Prepares an expression for evaluation.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression to be prepared.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="ALocator">A debug locator describing the source of the expression.</param>
		/// <returns>A PlanDescriptor describing the prepared plan.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		PlanDescriptor PrepareExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AExpression, RemoteParam[] AParams, DebugLocator ALocator);
		
		/// <summary>
		/// Evaluates a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be evaluated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <returns>An EvaluateResult describing the results of the evaluation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		EvaluateResult EvaluatePlan(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams);

		/// <summary>
		/// Opens a cursor from a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be evaluated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <returns>A CursorDescriptor describing the new cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		CursorResult OpenPlanCursor(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams);
		
		/// <summary>
		/// Opens a cursor from a prepared plan, and fetches the first batch of rows.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be evaluated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="ACount">The number of rows to be fetched as part of the open.</param>
		/// <returns>A CursorWithFetchResult describing the results of the open.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		CursorWithFetchResult OpenPlanCursorWithFetch(int APlanHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams, int ACount);
		
		/// <summary>
		/// Unprepares a prepared expression plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void UnprepareExpression(int APlanHandle);
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the expression will be evaluated.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression to be evaluated.</param>
		/// <param name="AParams">Any parameters to the expression.</param>
		/// <returns>A DirectEvaluateResult describing the result of the evaluation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		DirectEvaluateResult EvaluateExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams);
		
		/// <summary>
		/// Opens a cursor.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the cursor will be opened.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression defining the cursor to be opened.</param>
		/// <param name="AParams">Any parameters to the expression.</param>
		/// <returns>A cursor descriptor describing the cursor that was opened.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		DirectCursorResult OpenCursor(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams);
		
		/// <summary>
		/// Opens a cursor.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the cursor will be opened.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression defining the cursor to be opened.</param>
		/// <param name="AParams">Any parameters to the expression.</param>
		/// <param name="ACount">The number of rows to be fetched as part of the open.</param>
		/// <returns>A DirectCursorWithFetchResult describing the result of the open.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		DirectCursorWithFetchResult OpenCursorWithFetch(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, ProcessCallInfo ACallInfo, string AExpression, RemoteParamData AParams, int ACount);
		
		#endregion
		
		// Cursor
		#region Cursor
		
		/// <summary>
		/// Closes an active cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be closed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void CloseCursor(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Selects a full row from a cursor.
		/// </summary>
		/// <param name="AHandle">The handle of the cursor from which the row will be selected.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteRowBody describing the row in it's physical representation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		RemoteRowBody Select(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Selects a row with a specific set of columns from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the row will be selected.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AHeader">A RemoteRowHeader describing the set of columns to be included in the resulting row.</param>
		/// <returns>A RemoteRowBody describing the row in it's physical representation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		RemoteRowBody SelectSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader);
		
		/// <summary>
		/// Fetches a number of rows from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the rows will be fetched.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ACount">The number of rows to be fetched.</param>
        /// <param name='ASkipCurrent'> True if the fetch should skip the current row of the cursor, false to include the current row in the fetch. </param>
		/// <returns>A FetchResult describing the results of the fetch.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		FetchResult Fetch(int ACursorHandle, ProcessCallInfo ACallInfo, int ACount, bool ASkipCurrent);
		
		/// <summary>
		/// Fetches a number of rows with a specific set of columns from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the rows will be fetched.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AHeader">A RemoteRowHeader describing the set of columns to be included in the fetched rows.</param>
		/// <param name="ACount">The number of rows to be fetched.</param>
        /// <param name='ASkipCurrent'> True if the fetch should skip the current row of the cursor, false to include the current row in the fetch. </param>
		/// <returns>A FetchResult describing the results of the fetch.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		FetchResult FetchSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader, int ACount, bool ASkipCurrent);
		
		/// <summary>
		/// Gets the current navigation state of a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the navigation state will be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A CursorGetFlags describing the navigation state of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		CursorGetFlags GetFlags(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Moves a cursor a number of rows.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ADelta">The number of rows to navigate, forward or backward (negative number)</param>
		/// <returns>A RemoteMoveData describing the results of the move.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		RemoteMoveData MoveBy(int ACursorHandle, ProcessCallInfo ACallInfo, int ADelta);

		/// <summary>
		/// Positions a cursor before the first row.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A CursorGetFlags describing the navigation state of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        CursorGetFlags First(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Positions a cursor after the last row.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A CursorGetFlags describing the navigation state of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        CursorGetFlags Last(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Resets a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be reset.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A CursorGetFlags describing the navigation state of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        CursorGetFlags Reset(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Inserts a row into a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor in which the row will be inserted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The row to be inserted.</param>
		/// <param name="AValueFlags">A value flags array indicating which columns are explicitly specified in the row.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void Insert(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags);

		/// <summary>
		/// Updates the current row in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor in which a row will be updated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The new values for the row.</param>
		/// <param name="AValueFlags">A value flags array indicating which columns are to be updated in the row.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void Update(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags);
        
        /// <summary>
        /// Deletes the current row in a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the row will be deleted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        void Delete(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Gest a bookmark for the current row in a cursor.
		/// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the bookmark will be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>The bookmark for the current row of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        Guid GetBookmark(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Positions a cursor on a bookmark.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark">The bookmark of the row on which the cursor will be positioned.</param>
		/// <param name="AForward">A hint indicating the intended direction of navigation after the positioning call.</param>
		/// <returns>A RemoteGotoData describing the results of the navigation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		RemoteGotoData GotoBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark, bool AForward);

		/// <summary>
		/// Compares two bookmarks from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the bookmarks to be compared were retrieved.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark1">The first bookmark to be compared.</param>
		/// <param name="ABookmark2">The second bookmark to be compared.</param>
		/// <returns>0 if the bookmarks are equal, -1 if the first bookmark is less than the second bookmark, and 1 if the first bookmark is greater than the second bookmark.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        int CompareBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark1, Guid ABookmark2);

		/// <summary>
		/// Disposes a bookmark for a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the bookmark is to be disposed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark">The bookmark to be disposed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void DisposeBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark);

		/// <summary>
		/// Disposes a list of bookmarks for a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the bookmarks are to be disposed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmarks">The list of bookmarks to be disposed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void DisposeBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid[] ABookmarks);

		/// <summary>
		/// Gets a string representing the ordering of a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the order is to be returned.</param>
		/// <returns>The order of the cursor as a string.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        string GetOrder(int ACursorHandle);
        
        /// <summary>
        /// Gets a key for the current position of a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the key is to be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <returns>A RemoteRow representing the key in it's physical representation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteRow GetKey(int ACursorHandle, ProcessCallInfo ACallInfo);
        
        /// <summary>
        /// Attempts to position a cursor on a key.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AKey">The key on which the cursor should be positioned.</param>
        /// <returns>A RemoteGotoData describing the results of the navigation.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteGotoData FindKey(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey);
        
        /// <summary>
        /// Attempts to position a cursor on the nearest matching row.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AKey">The key on which the should be positioned.</param>
        /// <returns>A CursorGetFlags describing the resulting navigation state of the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        CursorGetFlags FindNearest(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey);
        
        /// <summary>
        /// Refreshes a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be refreshed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="ARow">The row on which the cursor should be positioned after the refresh.</param>
        /// <returns>A RemoteGotoData describing the results of the refresh.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteGotoData Refresh(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow);

		/// <summary>
		/// Gets the number of rows in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be counted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>The number of rows in the cursor.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        int GetRowCount(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Performs default processing for a row in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle for the cursor that will perform the default processing.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The row to be defaulted.</param>
		/// <param name="AColumn">The name of a column that is being defaulted. Use the empty string to indicate that the entire row is being defaulted.</param>
		/// <returns>A RemoteProposeData containing the results of the call.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteProposeData Default(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody ARow, string AColumn);
        
        /// <summary>
        /// Performs change processing for a row in a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle for the cursor that will perform the change processing.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AOldRow">The row before the change that triggered the change call.</param>
        /// <param name="ANewRow">The row after the change that triggered the change call.</param>
        /// <param name="AColumn">The name of the column that triggered the change. Use the empty string to indicate that the entire row is being changed.</param>
        /// <returns>A RemoteProposeData containing the results of the call.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteProposeData Change(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn);

		/// <summary>
		/// Performs validation processing for a row in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle for the cursor that will perform the validation processing.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AOldRow">The row before the change that triggered the validation call.</param>
		/// <param name="ANewRow">The row after the change that triggered the validation call.</param>
		/// <param name="AColumn">The name of the column that triggered the validation. Use the empty string to indiate that the entire row is being validated.</param>
		/// <returns>A RemoteProposeData containing the results of the call.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        RemoteProposeData Validate(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn);

		#endregion
		
		// Script
		#region Script
		
		/// <summary>
		/// Prepares a script for execution.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the script will be prepared.</param>
		/// <param name="AScript">The script to be prepared.</param>
		/// <param name="ALocator">A debug locator describing the source of the script.</param>
		/// <returns>A ScriptDescriptor describing the new script.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ScriptDescriptor PrepareScript(int AProcessHandle, string AScript, DebugLocator ALocator);

		/// <summary>
		/// Unprepares a script for execution.
		/// </summary>
		/// <param name="AScriptHandle">The handle of the script to be unprepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void UnprepareScript(int AScriptHandle);

		/// <summary>
		/// Executes a script.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the execution will be performed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AScript">The script to be executed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void ExecuteScript(int AProcessHandle, ProcessCallInfo ACallInfo, string AScript);
		
		/// <summary>
		/// Gets the text of a batch.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch for which the text is to be returned.</param>
		/// <returns>The text of the batch as a string.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		string GetBatchText(int ABatchHandle);
		
		/// <summary>
		/// Prepares a batch for execution.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch to be prepared.</param>
		/// <param name="AParams">The parameters to the batch.</param>
		/// <returns>A PlanDescriptor describing the prepared plan.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		PlanDescriptor PrepareBatch(int ABatchHandle, RemoteParam[] AParams);
		
		/// <summary>
		/// Unprepares a batch.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void UnprepareBatch(int APlanHandle);
		
		/// <summary>
		/// Executes a batch.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch to be executed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the batch.</param>
		/// <returns>An ExecuteResult describing the result of the execution.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ExecuteResult ExecuteBatch(int ABatchHandle, ProcessCallInfo ACallInfo, RemoteParamData AParams);
		
		#endregion
		
		// Catalog Support
		#region Catalog Support
		
		/// <summary>
		/// Gets a script that can be used to create the repository version of a catalog object.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AName">The name of the object to be created.</param>
		/// <returns>A D4 script to create the necessary objects.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
        CatalogResult GetCatalog(int AProcessHandle, string AName);
        
		/// <summary>
		/// Returns the fully qualified class name for a registered class.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AClassName">The registered class name.</param>
		/// <returns>The fully qualified class name as a string.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		string GetClassName(int AProcessHandle, string AClassName);

		/// <summary>
		/// Gets the set of files required to support instantiation of a registered class name.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AClassName">The name of the registered class that needs to be instantiated.</param>
		/// <param name="AEnvironment">The client environment in which the class is to be instantiated.</param>
		/// <returns>A list of ServerFileInfo describing the necessary files.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		ServerFileInfo[] GetFileNames(int AProcessHandle, string AClassName, string AEnvironment);

		/// <summary>
		/// Gets a file.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="ALibraryName">The name of the library that contains the file.</param>
		/// <param name="AFileName">The name of the file to be retrieved.</param>
		/// <returns>The handle of the stream containing the contents of the file.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		int GetFile(int AProcessHandle, string ALibraryName, string AFileName);
		
		#endregion

		// Stream Support
		#region	Stream Support
		
		/// <summary>
		/// Allocates a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be allocated.</param>
		/// <returns>The ID of the new stream.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		StreamID AllocateStream(int AProcessHandle);
		
		/// <summary>
		/// References a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be referenced.</param>
		/// <param name="AStreamID">The ID of the stream to be referenced.</param>
		/// <returns>The ID of the new stream.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		StreamID ReferenceStream(int AProcessHandle, StreamID AStreamID);
		
		/// <summary>
		/// Deallocates a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be deallocated.</param>
		/// <param name="AStreamID">The ID of the stream to be deallocated.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void DeallocateStream(int AProcessHandle, StreamID AStreamID);
		
		/// <summary>
		/// Opens a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be opened.</param>
		/// <param name="AStreamID">The ID of the stream to be opened.</param>
		/// <param name="ALockMode">The locking to be used to open the stream.</param>
		/// <returns>The handle of the new stream.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		int OpenStream(int AProcessHandle, StreamID AStreamID, LockMode ALockMode);
		
		/// <summary>
		/// Closes an open stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be closed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void CloseStream(int AStreamHandle);
		
		/// <summary>
		/// Gets the length of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the length will be returned.</param>
		/// <returns>The number of bytes in the stream as a long.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		long GetStreamLength(int AStreamHandle);
		
		/// <summary>
		/// Sets the length of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the length will be set.</param>
		/// <param name="AValue">The new length of the stream.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void SetStreamLength(int AStreamHandle, long AValue);
		
		/// <summary>
		/// Gets the current position of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the current position is to be returned.</param>
		/// <returns>The current position of the stream as a long.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		long GetStreamPosition(int AStreamHandle);
		
		/// <summary>
		/// Sets the current position of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the current position is to be set.</param>
		/// <param name="APosition">The new position of the stream.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void SetStreamPosition(int AStreamHandle, long APosition);
		
		/// <summary>
		/// Flushes a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be flushed.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void FlushStream(int AStreamHandle);

		/// <summary>
		/// Reads from a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be read.</param>
		/// <param name="ACount">The number of bytes to read.</param>
		/// <returns>A byte[] containing the bytes read.</returns>
		/// <remarks>
		/// Note that the result byte[] may contain less than ACount bytes if the actual number of bytes available to be read in the stream
		/// was less than the requested number.
		/// </remarks>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		byte[] ReadStream(int AStreamHandle, int ACount);
		
		/// <summary>
		/// Seeks on a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be navigated.</param>
		/// <param name="offset">The number of bytes to seek.</param>
		/// <param name="AOrigin">The origin of the seek.</param>
		/// <returns>The new position of the stream.</returns>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		long SeekStream(int AStreamHandle, long offset, SeekOrigin AOrigin);

		/// <summary>
		/// Writes to a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be written.</param>
		/// <param name="AData">A byte[] containing the data to be written.</param>
		[OperationContract]
		[FaultContract(typeof(DataphorFault))]
		void WriteStream(int AStreamHandle, byte[] AData);
		
		#endregion
	}
}
