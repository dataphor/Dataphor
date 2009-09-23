/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.Contracts
{
	[ServiceContract]
	public interface IDataphorService
	{
		// Session
		#region Session

		/// <summary>
		/// Initiates a connection to the Dataphor server.
		/// </summary>
		/// <param name="ASessionInfo">The session information used to authenticate and describe the session.</param>
		/// <returns>A session descriptor that describes the new session.</returns>
		SessionDescriptor Connect(SessionInfo ASessionInfo);

		/// <summary>
		/// Disconnects an active Dataphor session.
		/// </summary>
		/// <param name="ASessionHandle">The handle to the session to be disconnected.</param>
		void Disconnect(int ASessionHandle);
		
		#endregion
		
		// Process
		#region Process
		
		/// <summary>
		/// Starts a server process.
		/// </summary>
		/// <param name="AProcessInfo">The process information used to describe the new process.</param>
		/// <returns>A process descriptor that describes the new process.</returns>
		ProcessDescriptor StartProcess(ProcessInfo AProcessInfo);
		
		/// <summary>
		/// Stops a server process.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process to be stopped.</param>
		void StopProcess(int AProcessHandle);
		
		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the transaction will be started.</param>
		/// <param name="AIsolationLevel">The isolation level of the new transaction.</param>
		void BeginTransaction(int AProcessHandle, IsolationLevel AIsolationLevel);
		
		/// <summary>
		/// Prepares an active transaction to be committed.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be prepared.</param>
		void PrepareTransaction(int AProcessHandle);
		
		/// <summary>
		/// Commits an active transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be committed.</param>
		void CommitTransaction(int AProcessHandle);
		
		/// <summary>
		/// Rolls back an active transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process on which the current transaction will be rolled back.</param>
		void RollbackTransaction(int AProcessHandle);
		
		/// <summary>
		/// Gets the number of active transactions.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process for which the number of active transactions will be returned.</param>
		/// <returns>The number of active transactions.</returns>
		int GetTransactionCount(int AProcessHandle);

		/// <summary>
		/// Begins an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process that will start the application transaction.</param>
		/// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AShouldJoin">Whether or not the process initiating the application transaction should immediately join the new transaction.</param>
		/// <param name="AIsInsert">Whether or not the process should join in insert mode.</param>
		/// <returns>The ID of the new application transaction.</returns>
        Guid BeginApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, bool AShouldJoin, bool AIsInsert);
        
        /// <summary>
        /// Prepares an application transaction to be committed.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process that will perform the prepare.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AID">The ID of the application transaction to be prepared.</param>
        void PrepareApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);

		/// <summary>
		/// Commits an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process that will perform the commit.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AID">The ID of the application transaction to be committed.</param>
        void CommitApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);
        
        /// <summary>
        /// Rolls back an application transaction.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process that will perform the rollback.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AID">The ID of the application transaction to be rolled back.</param>
        void RollbackApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID);
        
        /// <summary>
        /// Gets the ID of the application transaction in which this process is currently participating, if any.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process.</param>
        /// <returns>The ID of the application transaction.</returns>
        Guid GetApplicationTransactionID(int AProcessHandle);

		/// <summary>
		/// Joins the process to an application transaction.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AID">The ID of the application transaction to join.</param>
		/// <param name="AIsInsert">Whether or not to join in insert mode.</param>
        void JoinApplicationTransaction(int AProcessHandle, ProcessCallInfo ACallInfo, Guid AID, bool AIsInsert);
        
        /// <summary>
        /// Leaves an application transaction.
        /// </summary>
        /// <param name="AProcessHandle">The handle to the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
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
		PlanDescriptor PrepareStatement(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AStatement, RemoteParam[] AParams, DebugLocator ALocator);
		
		/// <summary>
		/// Executes a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the prepared plan to be executed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the plan.</param>
		/// <param name="AExecuteTime">The execution time.</param>
		void ExecutePlan(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime);

		/// <summary>
		/// Unprepares a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		void UnprepareStatement(int APlanHandle);
		
		/// <summary>
		/// Executes a statement.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AStatement">The statement to be executed.</param>
		/// <param name="AParams">The parameters to the statement.</param>
		/// <param name="ALocator">A debug locator describing the source of the statement.</param>
		void Execute(int AProcessHandle, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo, string AStatement, ref RemoteParamData AParams, DebugLocator ALocator);
		
		/// <summary>
		/// Prepares an expression for evaluation.
		/// </summary>
		/// <param name="AProcessHandle">The handle to the process.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression to be prepared.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="ALocator">A debug locator describing the source of the expression.</param>
		/// <returns>A PlanDescriptor describing the prepared plan.</returns>
		PlanDescriptor PrepareExpression(int AProcessHandle, ProcessCleanupInfo ACleanupInfo, string AExpression, RemoteParam[] AParams, DebugLocator ALocator);
		
		/// <summary>
		/// Evaluates a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be evaluated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="AExecuteTime">The execute time.</param>
		/// <returns>The result of evaluating the plan in it's physical representation.</returns>
		byte[] EvaluatePlan(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime);

		/// <summary>
		/// Opens a cursor from a prepared plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be evaluated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="AExecuteTime">The execution time.</param>
		/// <param name="ABookmarks">A list of bookmarks associated with the inital fetch.</param>
		/// <param name="ACount">The number of rows to be fetched as part of the open.</param>
		/// <param name="AFetchData">A FetchData describing the results of the initial fetch.</param>
		/// <returns>A CursorDescriptor describing the new cursor.</returns>
		CursorDescriptor OpenPlanCursor(int APlanHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams, out TimeSpan AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData);
		
		/// <summary>
		/// Unprepares a prepared expression plan.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		void UnprepareExpression(int APlanHandle);
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression to be evaluated.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="ALocator">A debug locator describing the source of the expression.</param>
		/// <param name="ADescriptor">A plan descriptor describing the plan that was prepared to perform the evaluation.</param>
		/// <returns>The result of evaluating the plan in it's physical representation.</returns>
		byte[] Evaluate(int AProcessHandle, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo, string AExpression, ref RemoteParamData AParams, DebugLocator ALocator, out PlanDescriptor ADescriptor);
		
		#endregion
		
		// Cursor
		#region Cursor
		
		/// <summary>
		/// Opens a cursor based on an expression.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ACleanupInfo">A CleanupInfo containing information to be processed prior to the call.</param>
		/// <param name="AExpression">The expression that describes the cursor to be opened.</param>
		/// <param name="AParams">The parameters to the expression.</param>
		/// <param name="ALocator">A debug locator describing the source of the expression.</param>
		/// <param name="ADescriptor">A plan descriptor describing the plan that was prepared to open the cursor.</param>
		/// <param name="ABookmarks">A list of bookmarks associated with the initial fetch.</param>
		/// <param name="ACount">The number of rows to be fetched as part of the open.</param>
		/// <param name="AFetchData">A FetchData describing the result of the initial fetch.</param>
		/// <returns>A CursorDescriptor describing the new cursor.</returns>
		CursorDescriptor OpenCursor(int AProcessHandle, ProcessCallInfo ACallInfo, ProcessCleanupInfo ACleanupInfo, string AExpression, ref RemoteParamData AParams, DebugLocator ALocator, out PlanDescriptor ADescriptor, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData);

		/// <summary>
		/// Closes an active cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be closed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		void CloseCursor(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Selects a full row from a cursor.
		/// </summary>
		/// <param name="AHandle">The handle of the cursor from which the row will be selected.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteRowBody describing the row in it's physical representation.</returns>
		RemoteRowBody Select(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Selects a row with a specific set of columns from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the row will be selected.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AHeader">A RemoteRowHeader describing the set of columns to be included in the resulting row.</param>
		/// <returns>A RemoteRowBody describing the row in it's physical representation.</returns>
		RemoteRowBody SelectSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader);
		
		/// <summary>
		/// Fetches a number of rows from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the rows will be fetched.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmarks">A list of bookmarks associated with the fetched rows.</param>
		/// <param name="ACount">The number of rows to be fetched.</param>
		/// <returns>A RemoteFetchData describing the results of the fetch.</returns>
		RemoteFetchData Fetch(int ACursorHandle, ProcessCallInfo ACallInfo, out Guid[] ABookmarks, int ACount);
		
		/// <summary>
		/// Fetches a number of rows with a specific set of columns from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the rows will be fetched.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AHeader">A RemoteRowHeader describing the set of columns to be included in the fetched rows.</param>
		/// <param name="ABookmarks">A list of bookmarks associated with the fetched rows.</param>
		/// <param name="ACount">The number of rows to be fetched.</param>
		/// <returns>A RemoteFetchData describing the results of the fetch.</returns>
		RemoteFetchData FetchSpecific(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount);
		
		/// <summary>
		/// Gets the current navigation state of a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the navigation state will be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteCursorGetFlags describing the navigation state of the cursor.</returns>
		RemoteCursorGetFlags GetFlags(int ACursorHandle, ProcessCallInfo ACallInfo);
		
		/// <summary>
		/// Moves a cursor a number of rows.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ADelta">The number of rows to navigate, forward or backward (negative number)</param>
		/// <returns>A RemoteMoveData describing the results of the move.</returns>
		RemoteMoveData MoveBy(int ACursorHandle, ProcessCallInfo ACallInfo, int ADelta);

		/// <summary>
		/// Positions a cursor before the first row.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteCursorGetFlags describing the navigation state of the cursor.</returns>
        RemoteCursorGetFlags First(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Positions a cursor after the last row.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteCursorGetFlags describing the navigation state of the cursor.</returns>
        RemoteCursorGetFlags Last(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Resets a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be reset.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>A RemoteCursorGetFlags describing the navigation state of the cursor.</returns>
        RemoteCursorGetFlags Reset(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Inserts a row into a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor in which the row will be inserted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The row to be inserted.</param>
		/// <param name="AValueFlags">A value flags array indicating which columns are explicitly specified in the row.</param>
        void Insert(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags);

		/// <summary>
		/// Updates the current row in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor in which a row will be updated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The new values for the row.</param>
		/// <param name="AValueFlags">A value flags array indicating which columns are to be updated in the row.</param>
        void Update(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow, BitArray AValueFlags);
        
        /// <summary>
        /// Deletes the current row in a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the row will be deleted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        void Delete(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Gest a bookmark for the current row in a cursor.
		/// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the bookmark will be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>The bookmark for the current row of the cursor.</returns>
        Guid GetBookmark(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Positions a cursor on a bookmark.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark">The bookmark of the row on which the cursor will be positioned.</param>
		/// <param name="AForward">A hint indicating the intended direction of navigation after the positioning call.</param>
		/// <returns>A RemoteGotoData describing the results of the navigation.</returns>
		RemoteGotoData GotoBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark, bool AForward);

		/// <summary>
		/// Compares two bookmarks from a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor from which the bookmarks to be compared were retrieved.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark1">The first bookmark to be compared.</param>
		/// <param name="ABookmark2">The second bookmark to be compared.</param>
		/// <returns>0 if the bookmarks are equal, -1 if the first bookmark is less than the second bookmark, and 1 if the first bookmark is greater than the second bookmark.</returns>
        int CompareBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark1, Guid ABookmark2);

		/// <summary>
		/// Disposes a bookmark for a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the bookmark is to be disposed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmark">The bookmark to be disposed.</param>
		void DisposeBookmark(int ACursorHandle, ProcessCallInfo ACallInfo, Guid ABookmark);

		/// <summary>
		/// Disposes a list of bookmarks for a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the bookmarks are to be disposed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ABookmarks">The list of bookmarks to be disposed.</param>
		void DisposeBookmarks(int ACursorHandle, ProcessCallInfo ACallInfo, Guid[] ABookmarks);

		/// <summary>
		/// Gets a string representing the ordering of a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor for which the order is to be returned.</param>
		/// <returns>The order of the cursor as a string.</returns>
        string GetOrder(int ACursorHandle);
        
        /// <summary>
        /// Gets a key for the current position of a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor from which the key is to be returned.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <returns>A RemoteRow representing the key in it's physical representation.</returns>
        RemoteRow GetKey(int ACursorHandle, ProcessCallInfo ACallInfo);
        
        /// <summary>
        /// Attempts to position a cursor on a key.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AKey">The key on which the cursor should be positioned.</param>
        /// <returns>A RemoteGotoData describing the results of the navigation.</returns>
        RemoteGotoData FindKey(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey);
        
        /// <summary>
        /// Attempts to position a cursor on the nearest matching row.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be navigated.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="AKey">The key on which the should be positioned.</param>
        /// <returns>A RemoteCursorGetFlags describing the resulting navigation state of the cursor.</returns>
        RemoteCursorGetFlags FindNearest(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow AKey);
        
        /// <summary>
        /// Refreshes a cursor.
        /// </summary>
        /// <param name="ACursorHandle">The handle of the cursor to be refreshed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
        /// <param name="ARow">The row on which the cursor should be positioned after the refresh.</param>
        /// <returns>A RemoteGotoData describing the results of the refresh.</returns>
        RemoteGotoData Refresh(int ACursorHandle, ProcessCallInfo ACallInfo, RemoteRow ARow);

		/// <summary>
		/// Gets the number of rows in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle of the cursor to be counted.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <returns>The number of rows in the cursor.</returns>
        int GetRowCount(int ACursorHandle, ProcessCallInfo ACallInfo);

		/// <summary>
		/// Performs default processing for a row in a cursor.
		/// </summary>
		/// <param name="ACursorHandle">The handle for the cursor that will perform the default processing.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="ARow">The row to be defaulted.</param>
		/// <param name="AColumn">The name of a column that is being defaulted. Use the empty string to indicate that the entire row is being defaulted.</param>
		/// <returns>A RemoteProposeData containing the results of the call.</returns>
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
		ScriptDescriptor PrepareScript(int AProcessHandle, string AScript, DebugLocator ALocator);

		/// <summary>
		/// Unprepares a script for execution.
		/// </summary>
		/// <param name="AScriptHandle">The handle of the script to be unprepared.</param>
		void UnprepareScript(int AScriptHandle);

		/// <summary>
		/// Executes a script.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the execution will be performed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AScript">The script to be executed.</param>
		/// <param name="ALocator">A debug locator describing the source of the script.</param>
		void ExecuteScript(int AProcessHandle, ProcessCallInfo ACallInfo, string AScript, DebugLocator ALocator);
		
		/// <summary>
		/// Gets whether or not a batch is an expression.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch to be queried.</param>
		/// <returns>True if the batch is an expression, false otherwise.</returns>
		bool GetBatchIsExpression(int ABatchHandle);
		
		/// <summary>
		/// Gets the text of a batch.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch for which the text is to be returned.</param>
		/// <returns>The text of the batch as a string.</returns>
		string GetBatchText(int ABatchHandle);
		
		/// <summary>
		/// Gets the starting line number of a batch.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch for which the starting line number is to be returned.</param>
		/// <returns>The starting line number of the batch.</returns>
		int GetBatchLine(int ABatchHandle);
		
		/// <summary>
		/// Prepares a batch for execution.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch to be prepared.</param>
		/// <param name="AParams">The parameters to the batch.</param>
		/// <returns>A PlanDescriptor describing the prepared plan.</returns>
		PlanDescriptor PrepareBatch(int ABatchHandle, RemoteParam[] AParams);
		
		/// <summary>
		/// Unprepares a batch.
		/// </summary>
		/// <param name="APlanHandle">The handle of the plan to be unprepared.</param>
		void UnprepareBatch(int APlanHandle);
		
		/// <summary>
		/// Executes a batch.
		/// </summary>
		/// <param name="ABatchHandle">The handle of the batch to be executed.</param>
        /// <param name="ACallInfo">A CallInfo containing information to be processed prior to the call.</param>
		/// <param name="AParams">The parameters to the batch.</param>
		void ExecuteBatch(int ABatchHandle, ProcessCallInfo ACallInfo, ref RemoteParamData AParams);
		
		#endregion
		
		// Catalog Support
		#region Catalog Support
		
		/// <summary>
		/// Gets a script that can be used to create the repository version of a catalog object.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AName">The name of the object to be created.</param>
		/// <param name="ACacheTimeStamp">The cache time stamp.</param>
		/// <param name="AClientCacheTimeStamp">The client cache time stamp.</param>
		/// <param name="ACacheChanged">Indicates whether or not the cache changed.</param>
		/// <returns>A D4 script to create the necessary objects.</returns>
        string GetCatalog(int AProcessHandle, string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged);

		/// <summary>
		/// Returns the fully qualified class name for a registered class.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AClassName">The registered class name.</param>
		/// <returns>The fully qualified class name as a string.</returns>
		string GetClassName(int AProcessHandle, string AClassName);

		/// <summary>
		/// Gets the set of files required to support instantiation of a registered class name.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="AClassName">The name of the registered class that needs to be instantiated.</param>
		/// <returns>A list of ServerFileInfo describing the necessary files.</returns>
		ServerFileInfo[] GetFileNames(int AProcessHandle, string AClassName);

		/// <summary>
		/// Gets a file.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process to be used for the call.</param>
		/// <param name="ALibraryName">The name of the library that contains the file.</param>
		/// <param name="AFileName">The name of the file to be retrieved.</param>
		/// <returns>A byte[] containing the contents of the file.</returns>
		byte[] GetFile(int AProcessHandle, string ALibraryName, string AFileName);
		
		#endregion

		// Stream Support
		#region	Stream Support
		
		/// <summary>
		/// Allocates a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be allocated.</param>
		/// <returns>The ID of the new stream.</returns>
		StreamID AllocateStream(int AProcessHandle);
		
		/// <summary>
		/// References a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be referenced.</param>
		/// <param name="AStreamID">The ID of the stream to be referenced.</param>
		/// <returns>The ID of the new stream.</returns>
		StreamID ReferenceStream(int AProcessHandle, StreamID AStreamID);
		
		/// <summary>
		/// Deallocates a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be deallocated.</param>
		/// <param name="AStreamID">The ID of the stream to be deallocated.</param>
		void DeallocateStream(int AProcessHandle, StreamID AStreamID);
		
		/// <summary>
		/// Opens a stream.
		/// </summary>
		/// <param name="AProcessHandle">The handle of the process on which the stream will be opened.</param>
		/// <param name="AStreamID">The ID of the stream to be opened.</param>
		/// <returns>The handle of the new stream.</returns>
		int OpenStream(int AProcessHandle, StreamID AStreamID);
		
		/// <summary>
		/// Closes an open stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be closed.</param>
		void CloseStream(int AStreamHandle);
		
		/// <summary>
		/// Gets the length of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the length will be returned.</param>
		/// <returns>The number of bytes in the stream as a long.</returns>
		long GetStreamLength(int AStreamHandle);
		
		/// <summary>
		/// Sets the length of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the length will be set.</param>
		/// <param name="AValue">The new length of the stream.</param>
		void SetStreamLength(int AStreamHandle, long AValue);
		
		/// <summary>
		/// Gets the current position of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the current position is to be returned.</param>
		/// <returns>The current position of the stream as a long.</returns>
		long GetStreamPosition(int AStreamHandle);
		
		/// <summary>
		/// Sets the current position of a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream for which the current position is to be set.</param>
		/// <param name="APosition">The new position of the stream.</param>
		void SetStreamPosition(int AStreamHandle, long APosition);
		
		/// <summary>
		/// Flushes a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be flushed.</param>
		void FlushStream(int AStreamHandle);

		/// <summary>
		/// Reads from a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be read.</param>
		/// <param name="ACount">The number of bytes to read.</param>
		/// <returns>A byte[] containing the bytes read.</returns>
		byte[] ReadStream(int AStreamHandle, int ACount);
		
		/// <summary>
		/// Seeks on a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be navigated.</param>
		/// <param name="AOffset">The number of bytes to seek.</param>
		/// <param name="AOrigin">The origin of the seek.</param>
		/// <returns>The new position of the stream.</returns>
		long SeekStream(int AStreamHandle, long AOffset, SeekOrigin AOrigin);

		/// <summary>
		/// Writes to a stream.
		/// </summary>
		/// <param name="AStreamHandle">The handle of the stream to be written.</param>
		/// <param name="AData">A byte[] containing the data to be written.</param>
		void WriteStream(int AStreamHandle, byte[] AData);
		
		#endregion
	}
}
