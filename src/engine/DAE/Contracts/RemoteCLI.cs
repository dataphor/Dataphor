/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;

using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Debug;

namespace Alphora.Dataphor.DAE.Contracts
{
    /// <nodoc/>
	/// <summary> A server interface designed to be utilized through remoting. </summary>
    public interface IRemoteServer : IServerBase
    {
		/// <summary> Establishes a managed network-level connection to the server. </summary>
		IRemoteServerConnection Establish(string AConnectionName, string AHostName);
		
		/// <summary> Relinquishes the connection to the server. </summary>
		void Relinquish(IRemoteServerConnection AConnection);
    }
    
    /// <nodoc/>
    /// <summary> Represents a network-level managed connection to the server. </summary>
    public interface IRemoteServerConnection : IDisposableNotify, IPing
    {
		/// <summary> Returns the name of the connection given when the connection was established. </summary>
		string ConnectionName { get; }

		/// <summary> Provides the remote equivalent to the Connect method of IServer. </summary>
		/// <returns> A new IRemoteServerSession interface. </returns>
		IRemoteServerSession Connect(SessionInfo ASessionInfo);
		
		/// <summary> Disconnects an active remote session. </summary>
		void Disconnect(IRemoteServerSession ASession);
    }
    
	/// <nodoc/>
	/// <summary> A session interface designed to be utilized through remoting. </summary>
    public interface IRemoteServerSession : IServerSessionBase
    {
		/// <value> A reference to the IRemoteServer object associated with this session. </value>
		IRemoteServer Server { get; }

        /// <summary> Starts a new process on the session. </summary>
        IRemoteServerProcess StartProcess(ProcessInfo AProcessInfo, out int AProcessID);
        
        /// <summary> Stops the given process. </summary>
        void StopProcess(IRemoteServerProcess AProcess);
    }

	/// <nodoc/>
	public interface IRemoteServerProcess : IServerProcessBase
    {
		IRemoteServerSession Session { get; }

		/// <summary>Begins an application transaction and returns its unique identifier.</summary>        
		/// <param name="AShouldJoin">Indicates whether the process should auto-enlist in the newly created application transaction.</param>
		/// <param name="AIsInsert">If joining the new application transaction, should it join as insert participant.</param>
        Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert, ProcessCallInfo ACallInfo);
        
        /// <summary>Prepares the application transaction for commit.</summary>
        void PrepareApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo);

		/// <summary>Commits the application transaction with the given identifier.</summary>
        void CommitApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo);
        
        /// <summary>Aborts the application transaction with the given identifier.</summary>
        void RollbackApplicationTransaction(Guid AID, ProcessCallInfo ACallInfo);
        
        /// <summary>Returns the ID of the application transaction this process is currently participating in, and Guid.Empty otherwise.</summary>
        Guid ApplicationTransactionID { get; }

        /// <summary>Joins this process to the given application transaction.</summary>
        /// <remarks>
        ///	Joins this process to the given application transaction. If the process joins the application transaction
        /// as an insert participant, then the data for the tables referenced in this process will not be copied into
        /// the application transaction space. All references to tables on this process will be referencing the tables
        /// in the application transaction space, rather than the actual database tables.
        /// </remarks>
        void JoinApplicationTransaction(Guid AID, bool AIsInsert, ProcessCallInfo ACallInfo);
        
        /// <summary>Leaves the application transaction that this process is participating in.</summary>
        /// <remarks>
        /// Once the process is no longer a participant in the application transaction, all table references will once again
        /// reference the table variables in the actual database.
        /// </remarks>
		void LeaveApplicationTransaction(ProcessCallInfo ACallInfo);
		
        /// <summary> Prepares the given statement for remote execution. </summary>
        /// <param name='AStatement'> A single valid Dataphor statement to prepare. </param>
        /// <returns> An <see cref="IServerStatementPlan"/> instance for the prepared statement. </returns>
        IRemoteServerStatementPlan PrepareStatement(string AStatement, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo);
        
        /// <summary> Unprepares a statement plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareStatement. </param>
        void UnprepareStatement(IRemoteServerStatementPlan APlan);
        
        void Execute(string AStatement, ref RemoteParamData AParams, ProcessCallInfo ACallInfo, RemoteProcessCleanupInfo ACleanupInfo);
        
        /// <summary> Prepares the given expression for remote selection. </summary>
        /// <param name='AExpression'> A single valid Dataphor expression to prepare. </param>
        /// <returns> An <see cref="IServerExpressionPlan"/> instance for the prepared expression. </returns>
        IRemoteServerExpressionPlan PrepareExpression(string AExpression, RemoteParam[] AParams, DebugLocator ALocator, out PlanDescriptor APlanDescriptor, RemoteProcessCleanupInfo ACleanupInfo);
        
        /// <summary> Unprepares an expression plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareExpression. </param>
        void UnprepareExpression(IRemoteServerExpressionPlan APlan);
        
        byte[] Evaluate
        (
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			out ProgramStatistics AExecuteTime,
			ProcessCallInfo ACallInfo, 
			RemoteProcessCleanupInfo ACleanupInfo
		);
        
        /// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
        /// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		IRemoteServerCursor OpenCursor
		(
			string AExpression, 
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan, 
			out PlanDescriptor APlanDescriptor, 
			out ProgramStatistics AExecuteTime,
			ProcessCallInfo ACallInfo,
			RemoteProcessCleanupInfo ACleanupInfo
		);
		
		/// <summary>Opens a remote, server-side cursor based on the prepared statement this plan represents, and fetches count rows.</summary>
        /// <param name="ABookmarks"> A Guid array that will receive the bookmarks for the selected rows. </param>
        /// <param name="ACount"> The number of rows to fetch, with a negative number indicating backwards movement. </param>
        /// <param name="AFetchData"> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </param>
        IRemoteServerCursor OpenCursor
        (
			string AExpression,
			ref RemoteParamData AParams, 
			out IRemoteServerExpressionPlan APlan,
			out PlanDescriptor APlanDescriptor,
			out ProgramStatistics AExecuteTime,
			ProcessCallInfo ACallInfo,
			RemoteProcessCleanupInfo ACleanupInfo,
			out Guid[] ABookmarks, 
			int ACount, 
			out RemoteFetchData AFetchData
		);
		
		void CloseCursor(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo);

		/// <summary> Prepares a given script for remote execution. </summary>
        IRemoteServerScript PrepareScript(string AScript, DebugLocator ALocator);
        
		/// <summary> Unprepares a given script. </summary>
        void UnprepareScript(IRemoteServerScript AScript);
        
        void ExecuteScript(string AScript, ProcessCallInfo ACallInfo);

        /// <summary>Returns the D4 commands necessary to reconstruct the remote catalog required to support the object named AName, limited to the objects requested in AObjectNames.</summary>
        string GetCatalog(string AName, out long ACacheTimeStamp, out long AClientCacheTimeStamp, out bool ACacheChanged);

		/// <summary>Returns the fully qualified class name for the given registered class name.</summary>
		string GetClassName(string AClassName);
		
		/// <summary>Returns the names of the files and assemblies required to load the given registered class name.</summary>
		ServerFileInfo[] GetFileNames(string AClassName, string AEnvironment);
		
		/// <summary>Retrieves the file for the given file name.</summary>
		IRemoteStream GetFile(string ALibraryName, string AFileName);
	}

	/// <nodoc/>    
	/// <summary> Exposes the representation of a list of IRemoteServerBatch instances. </summary>
	/// <remarks> IRemoteServerBatches is used by <see cref="IRemoteServerScript"/> to represent it's batch list. </remarks>
    public interface IRemoteServerBatches : IList
    {
		new IRemoteServerBatch this[int AIndex] { get; set; }
    }
    
	/// <nodoc/>
	/// <summary> Interface for remotely accessing a script. </summary>
    public interface IRemoteServerScript : IServerScriptBase
    {	
		/// <summary> The IRemoteServerSession from which this script was prepared. </summary>
		IRemoteServerProcess Process { get; }
		
		/// <summary> Remotely executes the entire script. </summary>
		void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo);
		
		/// <summary> Provides access to the exceptions encountered when parsing this script, if any. </summary>
		Exception[] Messages { get; }

		/// <summary> A list of batches within this script. </summary>
		IRemoteServerBatches Batches { get; }
    }

	/// <nodoc/>
	/// <summary> Provides remote access to an <see cref="IServerBatch"/>. </summary>
    public interface IRemoteServerBatch : IServerBatchBase
    {
		/// <summary> Represents the <see cref="IRemoteServerScript"/> that this batch came from. </summary>
		IRemoteServerScript ServerScript { get; }
		
		/// <summary> Prepares a remote plan. </summary>
		IRemoteServerPlan Prepare(RemoteParam[] AParams);
		
		/// <summary> Unprepares the remote plan. </summary>
		void Unprepare(IRemoteServerPlan APlan);
		
		/// <summary> Prepares a remote expression. </summary>
		IRemoteServerExpressionPlan PrepareExpression(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor);
		
		/// <summary> Unprepares a remote expression. </summary>
		void UnprepareExpression(IRemoteServerExpressionPlan APlan);

		/// <summary> Prepares a remote statement. </summary>
		IRemoteServerStatementPlan PrepareStatement(RemoteParam[] AParams, out PlanDescriptor APlanDescriptor);
		
		/// <summary> Unprepares a remote statement. </summary>
		void UnprepareStatement(IRemoteServerStatementPlan APlan);

		/// <summary> Remotely executes the batch. </summary>
		void Execute(ref RemoteParamData AParams, ProcessCallInfo ACallInfo);
    }
    
	/// <nodoc/>
	/// <summary> Remote prepared execution plan. </summary>
	public interface IRemoteServerPlan : IServerPlanBase
	{
        /// <value> Returns the <see cref="IRemoteServerSession"/> instance for this plan. </value>
		IRemoteServerProcess Process { get; }
		
		Exception[] Messages { get; }
	}

	/// <nodoc/>
	/// <summary> Remote prepared statement execution plan. </summary>
    public interface IRemoteServerStatementPlan : IRemoteServerPlan
    {
        /// <summary> Executes the prepared statement this plan represents. </summary>
        void Execute(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo);
	}

	/// <nodoc/>
	public interface IRemoteProposable
    {
		/// <summary> Requests the default values for a new row in the cursor.  </summary>
        RemoteProposeData Default(RemoteRowBody ARow, string AColumn, ProcessCallInfo ACallInfo);
        
		/// <summary> Requests the affect of a change to the given row. </summary>
        RemoteProposeData Change(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo);

        /// <summary> Ensures that the given row is valid. </summary>
        RemoteProposeData Validate(RemoteRowBody AOldRow, RemoteRowBody ANewRow, string AColumn, ProcessCallInfo ACallInfo);
    }
    
	/// <nodoc/>
	/// <summary> An expression plan interface designed to be utilized through remoting. </summary>
    public interface IRemoteServerExpressionPlan : IRemoteServerPlan, IServerCursorBehavior
    {
		/// <summary> Evaluates the expression and returns the result. </summary>
		byte[] Evaluate(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo);
		
        /// <summary> Opens a remote, server-side cursor based on the prepared statement this plan represents. </summary>        
        /// <returns> An <see cref="IRemoteServerCursor"/> instance for the prepared statement. </returns>
		IRemoteServerCursor Open(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, ProcessCallInfo ACallInfo);
		
		/// <summary>Opens a remote, server-side cursor based on the prepared statement this plan represents, and fetches count rows.</summary>
        /// <param name="ABookmarks"> A Guid array that will receive the bookmarks for the selected rows. </param>
        /// <param name="ACount"> The number of rows to fetch, with a negative number indicating backwards movement. </param>
        /// <param name="AFetchData"> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </param>
        IRemoteServerCursor Open(ref RemoteParamData AParams, out ProgramStatistics AExecuteTime, out Guid[] ABookmarks, int ACount, out RemoteFetchData AFetchData, ProcessCallInfo ACallInfo);
		
        /// <summary> Closes a remote, server-side cursor previously created using Open. </summary>
        /// <param name="ACursor"> The cursor to close. </param>
		void Close(IRemoteServerCursor ACursor, ProcessCallInfo ACallInfo);
    }

    /// <summary> A cursor interface designed to be utilized through remoting. </summary>
	/// <nodoc/>
	public interface IRemoteServerCursor : IServerCursorBase, IRemoteProposable
    {
        /// <value> Returns the <see cref="IRemoteServerExpressionPlan"/> instance for this cursor. </value>
		IRemoteServerExpressionPlan Plan { get; }

        /// <summary> Returns the current row of the cursor. </summary>
        /// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
        /// <returns> A <see cref="RemoteRowBody"/> structure containing the row information. </returns>
        RemoteRowBody Select(RemoteRowHeader AHeader, ProcessCallInfo ACallInfo);
        
        RemoteRowBody Select(ProcessCallInfo ACallInfo);

        /// <summary> 
        /// Returns the requested number of rows from the cursor. 
        /// </summary>
        /// <param name="AHeader"> A <see cref="RemoteRowHeader"/> structure containing the columns to be returned. </param>
        /// <param name="ABookmarks"> An Guid array that will receive the bookmarks for the selected rows. </param>
        /// <param name='ACount'> The number of rows to fetch, with a negative number indicating backwards movement. </param>
        /// <param name='ASkipCurrent'> True if the fetch should skip the current row of the cursor, false to include the current row in the fetch. </param>
        /// <returns> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </returns>
        RemoteFetchData Fetch(RemoteRowHeader AHeader, out Guid[] ABookmarks, int ACount, bool ASkipCurrent, ProcessCallInfo ACallInfo);
        
        /// <summary> Returns the requested number of rows from the cursor. </summary>
        /// <param name="ABookmarks"> A Guid array that will receive the bookmarks for the selected rows. </param>
        /// <param name="ACount"> The number of rows to fetch, with a negative number indicating backwards movement. </param>
        /// <param name='ASkipCurrent'> True if the fetch should skip the current row of the cursor, false to include the current row in the fetch. </param>
        /// <returns> A <see cref="RemoteFetchData"/> structure containing the result of the fetch. </returns>
        RemoteFetchData Fetch(out Guid[] ABookmarks, int ACount, bool ASkipCurrent, ProcessCallInfo ACallInfo);

        /// <summary> Indicates whether the cursor is on the BOF crack, the EOF crack, or both, which indicates an empty cursor. </summary>
        /// <returns> A <see cref="CursorGetFlags"/> value indicating the current position of the cursor. </returns>
        CursorGetFlags GetFlags(ProcessCallInfo ACallInfo);

        /// <summary> Provides a mechanism for navigating the cursor by a specified number of rows. </summary>        
        /// <param name='ADelta'> The number of rows to move by, with a negative value indicating backwards movement. </param>
        /// <returns> A <see cref="RemoteMoveData"/> structure containing the result of the move. </returns>
        RemoteMoveData MoveBy(int ADelta, ProcessCallInfo ACallInfo);

        /// <summary> Positions the cursor on the BOF crack. </summary>
        /// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
        CursorGetFlags First(ProcessCallInfo ACallInfo);

        /// <summary> Positions the cursor on the EOF crack. </summary>
        /// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the move. </returns>
        CursorGetFlags Last(ProcessCallInfo ACallInfo);

        /// <summary> Resets the server-side cursor, causing any data to be re-read and leaving the cursor on the BOF crack. </summary>        
        /// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the reset. </returns>
        CursorGetFlags Reset(ProcessCallInfo ACallInfo);

        /// <summary> Inserts the given <see cref="RemoteRow"/> into the cursor. </summary>        
        /// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be inserted. </param>
        /// <param name="AValueFlags"> A BitArray indicating which columns of the row have been specified. May be null. </param>
        void Insert(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo);

        /// <summary> Updates the current row of the cursor using the given <see cref="RemoteRow"/>. </summary>        
        /// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the Row to be updated. </param>
        /// <param name="AValueFlags"> A BitArray indicating which columns of the row have been updated. May be null. </param>
        void Update(RemoteRow ARow, BitArray AValueFlags, ProcessCallInfo ACallInfo);
        
        /// <summary> Deletes the current row from the cursor. </summary>
        void Delete(ProcessCallInfo ACallInfo);

        /// <summary> Gets a bookmark for the current row suitable for use in the <c>GotoBookmark</c> and <c>CompareBookmark</c> methods. </summary>        
        /// <returns> A Guid value that is the bookmark. </returns>
        Guid GetBookmark(ProcessCallInfo ACallInfo);

        /// <summary> Positions the cursor on the row denoted by the given bookmark obtained from a previous call to <c> GetBookmark </c> . </summary>        
        /// <param name="ABookmark"> A Guid value that is the bookmark. </param>
        /// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the goto call. </returns>
		RemoteGotoData GotoBookmark(Guid ABookmark, bool AForward, ProcessCallInfo ACallInfo);

        /// <summary> Compares the value of two bookmarks obtained from previous calls to <c>GetBookmark</c> . </summary>        
        /// <param name="ABookmark1"> A Guid value that is the first bookmark to compare. </param>
        /// <param name="ABookmark2"> A Guid value that is the second bookmark to compare. </param>
        /// <returns> An integer value indicating whether the first bookmark was less than (negative), equal to (0) or greater than (positive) the second bookmark. </returns>
        int CompareBookmarks(Guid ABookmark1, Guid ABookmark2, ProcessCallInfo ACallInfo);

		/// <summary> Disposes a bookmark previously allocated with <see cref="GetBookmark"/>. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed.  </remarks>
        /// <seealso cref="DisposeBookmarks"/>
		void DisposeBookmark(Guid ABookmark, ProcessCallInfo ACallInfo);

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> Does nothing if the bookmark does not exist, or has already been disposed.  </remarks>
        /// <seealso cref="DisposeBookmark"/>
		void DisposeBookmarks(Guid[] ABookmarks, ProcessCallInfo ACallInfo);

        /// <value> Accesses the <see cref="Order"/> of the cursor. </value>
        string Order { get; }
        
        /// <returns> A <see cref="RemoteRow"/> structure containing the key for current row. </returns>
        RemoteRow GetKey(ProcessCallInfo ACallInfo);
        
        /// <summary> Attempts to position the cursor on the row matching the given key.  If the key is not found, the cursor position remains unchanged. </summary>
        /// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
        /// <returns> A <see cref="RemoteGotoData"/> structure containing the results of the find. </returns>
        RemoteGotoData FindKey(RemoteRow AKey, ProcessCallInfo ACallInfo);
        
        /// <summary> Positions the cursor on the row most closely matching the given key. </summary>
        /// <param name="AKey"> A <see cref="RemoteRow"/> structure containing the key to be found. </param>
        /// <returns> A <see cref="CursorGetFlags"/> value indicating the state of the cursor after the search. </returns>
        CursorGetFlags FindNearest(RemoteRow AKey, ProcessCallInfo ACallInfo);
        
        /// <summary> Refreshes the cursor and attempts to reposition it on the given row. </summary>
        /// <param name="ARow"> A <see cref="RemoteRow"/> structure containing the row to be positioned on after the refresh. </param>
        /// <returns> A <see cref="RemoteGotoData"/> structure containing the result of the refresh. </returns>
        RemoteGotoData Refresh(RemoteRow ARow, ProcessCallInfo ACallInfo);

		// Countable
        /// <returns>An integer value indicating the number of rows in the cursor.</returns>
        int RowCount(ProcessCallInfo ACallInfo);
    }

	/// <nodoc/>
	public interface IPing // note that the class that implements this will need to inherit from MarshalByRefObject
	{
		void Ping(); // this doesn't really need to do anything, just allow the server to contact this client or vise versa
	}

	/// <nodoc/>
	public struct RemoteProcessCleanupInfo
    {
		public IRemoteServerPlan[] UnprepareList;
    }
}

