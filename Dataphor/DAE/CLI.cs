/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;

using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.DAE.Runtime.Data;

namespace Alphora.Dataphor.DAE
{
	/*	
		Defines the Call Level Interface (CLI) to the Dataphor Server (DAE).
		If the Dataphor Server instance is in-process, the IServerXXX interfaces can be used to
		communicate directly with the instance.  If the Dataphor Server is running in another process,
		the IRemoteServerXXX interfaces can be used to communicate directly with the
		instance.  In either case the IServerXXXBase interfaces designate functionality 
		that is common to both.
		
		In-Process
		The classes that implement the actual Dataphor Server implement both the IServerXXX and 
		IRemoteServerXXX interfaces.  In process, the IServerXXX interfaces provide
		more efficient access to the Dataphor Server.
		
		Out-of-Process
		The IRemoteServerXXX interfaces are optimized for network usage.  
		The LocalServer classes implement the IServerXXX interfaces to provide
		transparent access to a Dataphor Server running in another process.  These classes wrap
		the IRemoteServerXXX interfaces exposed by the out-of-process Dataphor Server.  They also
		perform transparent data and schema caching to optimize access to the Dataphor Server.
		
		Overall Architecture
		The overall architecture of the CLI follows the following basic structure ->
		
			Server -> 
				Represents the Dataphor Server instance. Provides access to the highest level
				functionality, such as starting, stopping and connecting to the Dataphor Server instance.
				
				IServerBase, IServer and IRemoteServer are the interfaces implementing this level
				of the CLI.
				
			Session ->
				Represents a channel for communication with the Dataphor Server.  User authorization and
				SessionInfo settings are tied to the session.  Provides functionality for
				starting and stopping a Dataphor Server process.
				
				IServerSessionBase, IServerSession, and IRemoteServerSession are the interfaces
				implementing this level of the CLI.
				
			Process ->
				Represents a running process in the Dataphor Server.  All transaction support is tied to
				the Process.  Provides functionality for participating in transactions in the
				Dataphor Server, as well as preparing scripts, statements and expressions for 
				execution in the Dataphor Server.
				
				IServerProcessBase, IServerProcess, and IRemoteServerProcess are the interfaces
				implementing this level of the CLI.
				
			Plan ->
				Represents a compiled plan ready for execution in the Dataphor Server.  Provides interfaces
				for executing the plan, as well as opening a cursor against the process.
				Only one plan may be executing at a time in a single process.
				
				There are two types of plans, Statement plans and Expression plans.  Statement
				plans represent the execution of a single statement where there is no result, such
				as an Insert, Update, Delete, or statement block, or where no result is desired.
				Expression plans represent the execution of single expression returning a result
				set.  This may be a select statement, or simply a table expression.  The select
				statement is used by the CLI when processing scripts to determine whether a
				given batch is an expression.  It is not required to prepare an expression.  For
				example, the strings "select Employee" and "Employee" would both be valid arguments
				to the PrepareExpression methods.
				
				IServerPlanBase, IServerPlan, IServerStatementPlan, IServerExpressionPlan,
				IRemoteServerPlan, IRemoteServerStatementPlan, and IRemoteServerExpressionPlan are
				the interfaces implementing this level of the CLI.
				
			Cursor ->
				Represents an open cursor executing in the Dataphor Server.  Provides interfaces for navigating
				on the cursor, and for performing updates against the data it contains.  Also implements
				the proposable interfaces.
				
				IServerCursorBase, IServerCursor and IRemoteServerCursor are the interfaces implementing
				this level of the CLI.
				
			Script ->
				Represents a prepared script, ready for execution in the Dataphor Server.  A script consists of
				a set of batches, each of which may either be a statement or expression.  A script
				is parsed only, not compiled.  Each batch of a script is compiled separately and
				executed.  In this way the effects of a batch on the Dataphor Server will be available to
				subsequent batches.
				
				Scripts are processed by first parsing the entire script, then returning each top-level
				statement as a batch.  Each batch can then be executed sequentially.  If a batch contains 
				a select statement, it may also be opened as an expression.
				
				IServerScriptBase, IServerScript, and IRemoteServerScript are the interfaces implementing
				this level of the CLI.
			
			Batch ->
				Represents a prepared statement or expression within a prepared script.  Provides
				functionality for executing the batch, or for opening a cursor if it is an
				expression.
				
				IServerBatchBase, IServerBatch, and IRemoteServerBatch are the interfaces implementing this
				level of the CLI.
				
		The Classes that implement these interfaces in the Dataphor Server are re-entrant in the sense that only
		one thread is allowed to be active in a given server, session or process at a time.  All attempts 
		to access the interface from another thread will be blocked until the current thread has completed 
		execution.

		Alphora.Dataphor.IDisposableNotify
			|- IServerBase
			|	|- IServer
			|	|- IRemoteServer
			|- IServerSessionBase
			|	|- IServerSession
			|	|- IRemoteServerSession
			|- IServerProcessBase
			|	|- IServerProcess
			|	|- IRemoteServerProcess
			|- IServerPlanBase
			|	|- IServerPlan
			|	|	|- IServerStatementPlan
			|	|	|- IServerExpressionPlan
			|	|- IRemoteServerPlan
			|	|	|- IRemoteServerStatementPlan
			|	|	|- IRemoteServerExpressionPlan
			|- IServerCursorBase
			|	|- IServerCursor
			|	|- IRemoteServerCursor
			|- IServerScriptBase
			|	|- IServerScript
			|	|- IRemoteServerScript
			|- IServerBatchBase
			|	|- IServerBatch
			|	|- IRemoteServerBatch
    */
    
	/// <summary>
	/// The possible values indicating the state of the Dataphor Server.
	/// </summary>
	public enum ServerState
	{
		/// <summary>
		/// The Dataphor Server is not running, either because it has not been started, or it has been stopped.
		/// </summary>
		Stopped,

		/// <summary>
		/// The Dataphor Server is in the process of starting in response to a Start command. 
		/// The Dataphor Server will not respond to connection requests while it is in this state.
		/// </summary>
		Starting,

		/// <summary>
		/// The Dataphor Server is running and ready to accept connection requests.
		/// </summary>
		Started,

		/// <summary>
		/// The Dataphor Server is in the process of stopping in response to a Stop command. 
		/// The Dataphor Server will not respond to connection requests while it is in this state.
		/// </summary>
		Stopping
	}
	
    /// <summary> Contains the members common to both remote and local server interfaces </summary>
    public interface IServerBase : IDisposableNotify
    {
		/// <summary> The name of the Dataphor Server instance. </summary>
		/// <value> <para>String</para>
		/// <para> Default: Dataphor</para> </value>
		/// <remarks> 
		/// Returns the name of this Dataphor Server instance. 
		/// This value can only be set when the Dataphor Server is not running.
		/// </remarks>
		string Name { get; }
		
        /// <summary> Starts the Dataphor Server instance. </summary>
        /// <remarks> If the Dataphor Server instance is already running, this call has no effect. </remarks>
        void Start();
        
        /// <summary> Stops the Dataphor Server instance. </summary>
        /// <remarks> If the Dataphor Server instance is not running, this call has no effect. </remarks>
        void Stop();
        
        /// <summary> Retrieves the state of the Dataphor Server. </summary>
        /// <value> ServerState: Starting|Started|Stopping|Stopped </value>
		/// <remarks>
		///  <para>Returns a <see cref="ServerState"/> value that is one of the </para>
		///  <para>ServerState enum values indicating the state of the Dataphor Server.</para>
        ///  <para>Stopped -> The Dataphor Server is not running, either because 
        ///  it has not been started, or it has been stopped.</para>
        ///	 <para>Starting -> The Dataphor Server is in the process of starting 
        ///	 in response to a Start command.  The Dataphor Server will not
        ///  respond to connection requests while it is in this state.</para>
        ///	 <para>Started -> The Dataphor Server is running and ready to 
        ///	 accept connection requests.</para>
        ///	 <para>Stopping -> The Dataphor Server is in the process of 
        ///	 stopping in response to a Stop command.  The Dataphor Server will not
        ///	 respond to connection requests while it is in this state.</para>
        /// </remarks>
        ServerState State { get; }
        
        /// <summary>Retrieves the cache timestamp for this server instance.</summary>
		/// <value> 
        ///	Returns the cache timestamp for this server instance.  
        /// This timestamp is affected by changes to existing types, 
        /// operators, and table variables.
        /// </value>
        /// <remarks>
        ///	This time stamp is used to coordinate the consistency of a schema cache.  
        /// It is a monotonically increasing number set by the server whenever the following events occur:
        ///		A type, table, view, operator, or aggregate operator is altered.
        ///		A type, table, view, operator, or aggregate operator is dropped.
        ///	A <see cref="ServerException"/> will be thrown if the Dataphor Server is not running.
        /// </remarks>
        long CacheTimeStamp { get; }
        
		/// <summary>Retrieves the derivation timestamp for this server instance.</summary>
		/// <value> 
        /// Returns the derivation timestamp for this server instance.  
        /// This timestamp is affected by changes to existing types and table variables, 
        /// and by any reference DDL statement.
        /// </value>
        ///	<remarks>
		///	This time stamp is used to coordinate the derivation cache maintained by the frontend server.  
		/// It is a monotonically increasing number set by the server whenever the following events occur:
		///		A reference is created.
		///		A type, table, view, or reference is altered.
		///		A type, table, view, or reference is dropped.
		/// A <see cref="ServerException"/> will be thrown if the Dataphor Server is not running.
		/// </remarks>
        long DerivationTimeStamp { get; }
    }
    
    /// <summary> Local server interface. </summary>
    public interface IServer : IServerBase
    {
        /// <summary>
        /// Connects to the server using the given session configuration information.
        /// </summary>
        /// <param name='ASessionInfo'> A <see cref="SessionInfo"/> object describing session configuration information for the connection request. </param>
        /// <returns> An <see cref="IServerSession"/> interface to the open session. </returns>
        /// <remarks>
        /// This method will raise a <see cref="ServerException"/> if the Dataphor Server is not running.
        /// </remarks>
        IServerSession Connect(SessionInfo ASessionInfo);
        
        /// <summary> Disconnects an active session. </summary>
        /// <param name='ASession'> The <see cref="IServerSession"/> to be disconnected. </param>
        /// <remarks>
        ///	If the Dataphor Server is not running, a <see cref="ServerException"/> will be raised.
        /// If the given session is not a valid session for this Dataphor Server, a <see cref="ServerException"/> will be raised.
        /// </remarks>
        void Disconnect(IServerSession ASession);

		/// <summary>Retrieves the local or remote catalog for this server.</summary>
		/// <remarks>This may be a local or remote catalog and may thus contain a partial or complete representation of the schema.</remarks>
		Schema.Catalog Catalog { get; }
	}
    
    /// <summary> Contains the members common to both remote and local session interfaces </summary>
    public interface IServerSessionBase : IDisposableNotify
    {
		/// <value> Returns the Dataphor Server assigned ID for this session. </value>
		int SessionID { get; }
		
        /// <value> Returns the <see cref="SessionInfo"/> object for this session. </value>
        SessionInfo SessionInfo { get; }
    }
    
    /// <summary> A local session interface. </summary>
    public interface IServerSession : IServerSessionBase
    {
        /// <value> Returns the <see cref="IServer"/> instance for this session. </value>
        IServer Server { get; }
        
        /// <summary> Starts a new process on the session. </summary>
        IServerProcess StartProcess(ProcessInfo AProcessInfo);
        
        /// <summary> Stops the given process. </summary>
        void StopProcess(IServerProcess AProcess);
    }
    
	/// <summary>Enumerates the set of Isolation Levels available in the Dataphor Server.</summary>
	/// <remarks>
	/// Isolation allows transactions to run as though they were the only transaction running on the system. 
	/// Isolation levels allow users of the system to control what level of concurrency a given transaction should use. 
	/// Isolation is achieved at the cost of concurrency. In other words, a completely isolated transaction must ensure
	/// no resources it relies on can change during the transaction, and therefore causes more contention. Lower isolation
	/// levels allow the transaction to ensure that only resources it has changed cannot be changed by other transactions,
	/// and control whether or not changes made by other transactions are visible within the transaction.
	/// </remarks>
	public enum IsolationLevel
	{
		/// <summary>
		/// Prevents lost updates but allows dirty reads. Data that is written is locked but data that is read may be uncommitted data from other transactions.
		/// </summary>
		Browse,
		
		/// <summary>
		/// Prevents lost updates and doesn't allow dirty reads. Data that is written is locked and data that is read is only committed data from other transactions.
		/// </summary>
		CursorStability,
		
		/// <summary>
		/// Prevents lost updates and ensures repeatable reads, which implies no dirty reads. This is the highest degree of isolation and provides complete isolation from other transactions.
		/// </summary>
		Isolated
	}
    
	/// <summary>Exposes the interface for communicating with a process in the Dataphor Server.</summary>
    public interface IServerProcessBase : IDisposableNotify, IStreamManager
    {
		/// <summary> A unique identifier for the process. </summary>
		int ProcessID { get; }

		///	<summary> Returns the <see cref="ProcessInfo"/> object for this process. </summary>
		ProcessInfo ProcessInfo { get; }

        /// <summary> Begins a new transaction on this process. </summary>
        void BeginTransaction(IsolationLevel AIsolationLevel);
        
        /// <summary> Prepares a transaction for commit. </summary>
        /// <remarks>
        /// Validates that all data within the transaction is consistent, and prepares the transaction for commit.
        /// It is not necessary to call this to commit the transaction, it is exposed to allow the process to participate in
        /// 2PC distributed transactions.
        /// </remarks>
        void PrepareTransaction();
        
        /// <summary> Commits the currently active transaction. </summary>
        /// <remarks>
        /// Commits the currently active transaction.  
        /// Reduces the transaction nesting level by one.  
        /// Will raise if no transaction is currently active.
        /// </remarks>
        void CommitTransaction();
        
        /// <summary>Rolls back the currently active transaction. </summary>
        /// <remarks>
        /// Reduces the transaction nesting level by one.
        /// Will raise if no transaction is currently active.
        /// </remarks>
        void RollbackTransaction();
        
        /// <value> Returns a boolean value indicating whether the process is currently participating in a transaction. </value>
        bool InTransaction { get; }
        
        /// <value> Returns the number of active transactions on the current process. </value>
        int TransactionCount { get; }
    }
    
    public interface IServerProcess : IServerProcessBase
    {
		IServerSession Session { get; }

		/// <summary>Begins an application transaction and returns its unique identifier.</summary>        
		/// <param name="AShouldJoin">Indicates whether the process should auto-enlist in the newly created application transaction.</param>
		/// <param name="AIsInsert">If joining the new application transaction, should it join as insert participant.</param>
        Guid BeginApplicationTransaction(bool AShouldJoin, bool AIsInsert);
        
        /// <summary>Prepares the application transaction for commit.</summary>
        void PrepareApplicationTransaction(Guid AID);

		/// <summary>Commits the application transaction with the given identifier.</summary>
        void CommitApplicationTransaction(Guid AID);
        
        /// <summary>Aborts the application transaction with the given identifier.</summary>
        void RollbackApplicationTransaction(Guid AID);
        
        /// <summary>Returns the ID of the application transaction this process is currently participating in, and Guid.Empty otherwise.</summary>
        Guid ApplicationTransactionID { get; }

        /// <summary>Joins this process to the given application transaction.</summary>
        /// <remarks>
        ///	Joins this process to the given application transaction. If the process joins the application transaction
        /// as an insert participant, then the data for the tables referenced in this process will not be copied into
        /// the application transaction space. All references to tables on this process will be referencing the tables
        /// in the application transaction space, rather than the actual database tables.
        /// </remarks>
        void JoinApplicationTransaction(Guid AID, bool AIsInsert);
        
        /// <summary>Leaves the application transaction that this process is participating in.</summary>
        /// <remarks>
        /// Once the process is no longer a participant in the application transaction, all table references will once again
        /// reference the table variables in the actual database.
        /// </remarks>
		void LeaveApplicationTransaction();

        /// <summary> Prepares the given statement for execution. </summary>
        /// <param name='AStatement'> A single valid statement to prepare. </param>
        /// <returns> An <see cref="IServerStatementPlan"/> instance for the prepared statement. </returns>
        /// <remarks> 
        /// Only the first statement parsed from <c>AStatement</c> will be prepared.  If the
        /// input string contains more than a single statement, it will be ignored.
        /// </remarks>
        IServerStatementPlan PrepareStatement(string AStatement, DataParams AParams);
        IServerStatementPlan PrepareStatement(string AStatement, DataParams AParams, DebugLocator ALocator);
        
        /// <summary> Unprepares a statement plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareStatement. </param>
        void UnprepareStatement(IServerStatementPlan APlan);
        
        /// <summary>Executes the given statement.</summary>
        /// <param name='AStatement'> A single valid Dataphor statement to prepare. </param>
        /// <remarks> 
        /// <para>
        /// Only the first statement parsed from <c>AStatement</c> will be executed.  If the
        /// input string contains more than a single statement, it will be ignored.
        /// </para>
        /// <para>
        /// This call is equivalent to preparing the statement, executing the prepared plan,
        /// and unpreparing the plan. It is provided for convenience if multiple execution
        /// of the prepared statement is not necessary.
        /// </para>
        /// </remarks>
        void Execute(string AStatement, DataParams AParams);

        /// <summary> 
        ///	Prepares the given expression for selection.  
		///	</summary>
        /// <param name='AExpression'> A single valid expression to prepare. </param>
        /// <returns> An <see cref="IServerExpressionPlan"/> instance for the prepared expression. </returns>
        IServerExpressionPlan PrepareExpression(string AExpression, DataParams AParams);
        IServerExpressionPlan PrepareExpression(string AExpression, DataParams AParams, DebugLocator ALocator);
        
        /// <summary> Unprepares an expression plan. </summary>
        /// <param name="APlan"> A reference to a plan object returned from a call to PrepareExpression. </param>
        void UnprepareExpression(IServerExpressionPlan APlan);
        
		/// <summary>Evaluates the given expression and returns the result.</summary>
        /// <param name='AExpression'>A single valid expression to be evaluated.</param>
        /// <remarks>
        /// This call is equivalent to preparing the expression, evaluating the prepared plan,
        /// and unpreparing the plan.  It is provided for convenience if multiple evaluation
        /// of the prepared expression is not necessary.
        /// </remarks>
		IDataValue Evaluate(string AExpression, DataParams AParams);
		
        /// <summary>Opens a server-side cursor based on the prepared statement this plan represents.</summary>        
        /// <param name='AExpression'>A single table-valued expression to be retrieved.</param>
        /// <returns>An <see cref="IServerCursor"/> instance for the prepared expression.</returns>
        /// <remarks>
        /// This call is equivalent to preparing the expression and opening the cursor from the prepared plan.
        /// The prepared plan is available through the cursor.
        /// </remarks>
        IServerCursor OpenCursor(string AExpression, DataParams AParams);
        
        /// <summary>Closes a server-side cursor previously created using Open.</summary>
        /// <param name="ACursor">The cursor to close.</param>
        /// <remarks>
        /// This call is equivalent to closing the cursor and then unpreparing the 
        /// expression used to retrieve the cursor.
        /// </remarks>
        void CloseCursor(IServerCursor ACursor);

		/// <summary>Prepares the given script for execution.</summary>
		/// <param name="AScript">The script to be executed.</param>
		/// <returns>An <see cref="IServerScript"/> instance for the prepared script.</returns>
		/// <remarks>
		/// <c>AScript</c> may contain any number of statements to be executed.
		/// Each top-level statement in the script is considered a batch, and will
		/// be compiled and executed in isolation, in the order given in the script.
		/// </remarks>
        IServerScript PrepareScript(string AScript);
        IServerScript PrepareScript(string AScript, DebugLocator ALocator);
        
        /// <summary>Executes the given script as a whole.</summary>
        /// <param name="AScript">The script to be execute.</param>
        /// <remarks>
        /// <para>
		/// <c>AScript</c> may contain any number of statements to be executed.
		/// Each top-level statement in the script is considered a batch, and will
		/// be compiled and executed in isolation, in the order given in the script.
        /// </para>
        /// <para>
        ///	This call is equivalent to preparing the script, executing the resulting prepared
        /// script, and unpreparing the script.
        /// </para>
        /// </remarks>
        void ExecuteScript(string AScript);

		/// <summary>Unprepares an <see cref="IServerScript"/> previously prepared with a call to <c>PrepareScript</c></summary>
		void UnprepareScript(IServerScript AScript);

		/// <summary>A list of system data types.</summary>
		Schema.DataTypes DataTypes { get; }

		/// <summary>Creates the System.Type type descriptor for the given class definition.</summary>
		Type CreateType(ClassDefinition AClassDefinition);

		/// <summary>Creates an object based on the given class definition.</summary>		
		object CreateObject(ClassDefinition AClassDefinition, object[] AActualParameters);

		/// <nodoc/>
		IValueManager ValueManager { get; }

        /// <nodoc/>
		ServerProcess GetServerProcess();
    }

	/// <summary> Interface of common members between <see cref="IServerScript"/> and <see cref="IRemoteServerScript"/>. </summary>
    public interface IServerScriptBase : IDisposableNotify
    {
    }
    
	/// <summary> Exposes the representation of a list of IServerBatch instances. </summary>
	/// <remarks> IServerBatches is used by <see cref="IServerScript"/> to represent it's batch list. </remarks>
    public interface IServerBatches : IList
    {
		new IServerBatch this[int AIndex] { get; set; }
    }
    
	/// <summary> A server script interface. </summary>
	/// <remarks>
	///	Scripts are made up of a list of parsed, but not compiled, batches.  The
	///	script can be executed as a whole, or each batch can be enumerated and
	///	executed independently.
	///	</remarks>
    public interface IServerScript : IServerScriptBase
    {
		/// <summary> The IServerProcess from which this script was prepared. </summary>
		IServerProcess Process { get; }

		/// <summary> Executes the IServerScript as a whole.</summary>
		/// <remarks> Use <see cref="IServerScriptBase.Batches"/> to execute or enumerate batches independently. </remarks>
		void Execute(DataParams AParams);
		
		/// <summary> Provides access to the exceptions encountered when parsing this script, if any. </summary>
		ParserMessages Messages { get; }

		/// <summary> A list of batches within this script. </summary>
		IServerBatches Batches { get; }
    }
    
    /// <summary> The members which are common between <see cref="IServerBatch"/> and <see cref="IRemoteServerBatch"/>. </summary>
    public interface IServerBatchBase : IDisposableNotify
    {
		/// <summary> Returns true if the batch is a select statement. </summary>
		bool IsExpression();

		/// <summary> Retrieves the statement or expression emitted as text (from the parse tree). </summary>
		string GetText();
		
		/// <summary> Indicates which line of the script the batch starts on. </summary>
		int Line { get; }
    }

    /// <summary> An individual batch within a script. </summary>
    public interface IServerBatch : IServerBatchBase
    {
		/// <summary> The <see cref="IServerScript"/> that this batch is part of. </summary>
		IServerScript ServerScript { get; }
		
		/// <summary> Prepares this batch as an expression or statement appropriately. </summary>
		IServerPlan Prepare(DataParams AParams);
		
		/// <summary> Unprepares the batch. </summary>
		void Unprepare(IServerPlan APlan);

		/// <summary> Prepares an expression plan from the batch. </summary>
		IServerExpressionPlan PrepareExpression(DataParams AParams);
		
		/// <summary> Unprepares an expression plan, previously prepared with <see cref="PrepareExpression"/>. </summary>
		void UnprepareExpression(IServerExpressionPlan APlan);

		/// <summary> Prepares a statement plan from the batch. </summary>
		IServerStatementPlan PrepareStatement(DataParams AParams);
		
		/// <summary> Unprepares a statement plan, previously prepared with <see cref="PrepareStatement"/>. </summary>
		void UnprepareStatement(IServerStatementPlan APlan);

		/// <summary> Executes the batch. </summary>
		/// <remarks> Use this if you aren't concerned with the plan, or with retrieving results from an expression. </remarks>
		void Execute(DataParams AParams);
    }

	[DataContract]
	public class PlanStatistics
	{
		/// <summary>Returns the total prepare time for the plan.</summary>
		/// <remarks>PrepareTime is the total of CompileTime, OptimizeTime, and BindingTime, plus any incidental overhead between these phases.</remarks>
		[DataMember]
		public TimeSpan PrepareTime;

		/// <summary>Returns the compile time for the plan.</summary>
		[DataMember]
		public TimeSpan CompileTime;

		/// <summary>Returns the optimize time for the plan.</summary>
		[DataMember]
		public TimeSpan OptimizeTime;

		/// <summary>Returns the bind time for the plan. (Binding time is no longer used, all time is reported to the OptimizeTime statistic)</summary>
		[DataMember]
		public TimeSpan BindingTime;

/*
		/// <summary>Returns the total execution time for the plan.</summary>
		/// <remarks>
		///	Execution time is the total time spent executing on this plan. 
		/// This includes all calls made through this plan, or cursors opened from this plan.
		/// </remarks>
		[DataMember]
		public TimeSpan ExecuteTime;
		
		/// <summary>Returns the amount of time spent executing in devices.</summary>
		/// <remarks>
		/// This statistic tracks the total amount of time spent waiting for execution on other systems, 
		/// as opposed to time spent within the Dataphor query processor.
		/// </remarks>
		[DataMember]
		public TimeSpan DeviceExecuteTime;
*/
	}
	
	[DataContract]
	public class ProgramStatistics
	{
		/// <summary>Returns the total execution time for the program.</summary>
		/// <remarks>
		///	Execution time is the total time spent executing on this program. 
		/// This includes all calls made through this program, or cursors based on the program.
		/// </remarks>
		[DataMember]
		public TimeSpan ExecuteTime;
		
		/// <summary>Returns the amount of time spent executing in devices.</summary>
		/// <remarks>
		/// This statistic tracks the total amount of time spent waiting for execution on other systems, 
		/// as opposed to time spent within the Dataphor query processor.
		/// </remarks>
		[DataMember]
		public TimeSpan DeviceExecuteTime;
	}
	
	/// <summary> Exposes the base functionality for a plan in the CLI. </summary>
    public interface IServerPlanBase : IDisposableNotify
    {
		/// <summary> Returns a globally unique identifier for this plan. </summary>
		Guid ID { get; }

		/// <summary> Ensures that this plan was successfully compiled.  Raises an error containing the messages encountered by the compiler, if any. </summary>
		void CheckCompiled();
		
		/// <summary> Returns statistics about plan preparation times. </summary>
		PlanStatistics PlanStatistics { get; }
		
		/// <summary> Returns statistics about plan execution times. </summary>
		ProgramStatistics ProgramStatistics { get; }
   }

    /// <summary> Prepared and compiled execution plan. </summary>
    /// <seealso cref="IServerStatementPlan"/> <seealso cref="IServerExpressionPlan"/>
    public interface IServerPlan : IServerPlanBase
    {
        /// <value> Returns the <see cref="IServerSession"/> instance for this plan. </value>
		IServerProcess Process { get; }
		
		/// <value> Returns the list of compiler messages encountered while preparing this plan. </value>
		CompilerMessages Messages { get; }
    }
    
	/// <summary> Prepared statement execution plan. </summary>
    public interface IServerStatementPlan : IServerPlan
    {
        /// <summary> Executes the prepared statement this plan represents. </summary>
        void Execute(DataParams AParams);
	}
	
	/// <summary> Interface for proposing changes to and performing validation on a row. </summary>
    public interface IProposable
    {
        /// <summary>Requests the default values for a new row in the cursor.</summary>        
        /// <param name='ARow'>A <see cref="Row"/> to be filled in with default values.</param>
		/// <param name='AColumnName'>The name of the column to default in <paramref name="ABuffer"/>.  If empty, the default is being requested for the full row.</param>
		/// <returns>A boolean value indicating whether any column was defaulted in <paramref name="ARow"/>.</returns>
        bool Default(IRow ARow, string AColumnName);
        
        /// <summary>Ensures that the given row is valid.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the values of the row before the change. May be null if this is a table-level validate.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed values.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any column was changed in <paramref name="ANewRow"/>.</returns>
        bool Validate(IRow AOldRow, IRow ANewRow, string AColumnName);
        
        /// <summary>Requests the affect of a change to the given row.</summary>
        /// <param name='AOldRow'>A <see cref="Row"/> containing the values of the row before the change.</param>
        /// <param name='ANewRow'>A <see cref="Row"/> containing the changed row.</param>
        /// <param name='AColumnName'>The name of the column which changed in <paramref name="ANewRow"/>.  If empty, the change affected more than one column.</param>
        /// <returns>A boolean value indicating whether any column was changed in <paramref name="ANewRow"/>.</returns>
        bool Change(IRow AOldRow, IRow ANewRow, string AColumnName);
    }

    /// <summary> An expression plan interface. </summary>
    public interface IServerExpressionPlan : IServerPlan, IServerCursorBehavior
    {
		/// <summary> Evaluates the expression and returns the result. </summary>
		IDataValue Evaluate(DataParams AParams);
		
        /// <summary> Opens a server-side cursor based on the prepared statement this plan represents. </summary>        
        /// <returns> An <see cref="IServerCursor"/> instance for the prepared statement. </returns>
        IServerCursor Open(DataParams AParams);
        
        /// <summary> Closes a server-side cursor previously created using Open. </summary>
        /// <param name="ACursor"> The cursor to close. </param>
        void Close(IServerCursor ACursor);
        
        /// <value> Returns the supporting <see cref="Catalog"/> for the result type. </value>
        Schema.Catalog Catalog { get; }

		/// <value> Returns the <see cref="DataType"/> of the compiled expression. </value>
        Schema.IDataType DataType { get; }
        
        /// <value> Returns the <see cref="TableVar"/> describing the result of the expression. </value>
        Schema.TableVar TableVar { get; }
        
        /// <summary> Returns the order of the result of the expression. </summary>
        Schema.Order Order { get; }

        /// <summary> Returns a fully resolved syntax tree for the expression. </summary>
        Statement EmitStatement();
        
        /// <summary> Returns a row for use in selecting from the cursor for this plan. </summary>
        IRow RequestRow();
        
        /// <summary> Releases a row previously requested with RequestRow. </summary>
        void ReleaseRow(IRow ARow);
    }
    
	[Flags]
    public enum CursorCapability : byte 
    { 
		None = 0,

		/// <summary>
		/// Indicates that the cursor supports basic navigational access.
		/// </summary>
		/// <remarks>
		/// All cursors support this basic level of access.
		/// The following methods are included in the Navigable cursor capability:
		///		Select
		///		Next
		///		Last
		///		BOF
		///		EOF
		///		IsEmpty
		///		Reset
		/// </remarks>
		Navigable = 1,
		
		/// <summary>
		/// Indicates that the cursor supports backwards navigation.
		/// </summary> 
		/// <remarks>
		///	The following methods are included in the BackwardsNavigable cursor capability:
		///		Prior
		///		First
		/// </remarks>
		BackwardsNavigable = 2,
		
		/// <summary>
		/// Indicates that the cursor supports bookmarks.
		/// </summary>
		/// <remarks>
		/// The following methods are included in the Bookmarkable cursor capability:
		///		GetBookmark
		///		GotoBookmark
		///		CompareBookmarks
		///		DisposeBookmark
		/// </remarks>
		Bookmarkable = 4, 
		
		/// <summary>
		/// Indicates that the cursor supports searching.
		/// </summary>
		/// <remarks>
		/// The following methods are included in the Searchable cursor capability:
		///		Order
		///		GetKey
		///		FindKey
		///		FindNearest
		///		Refresh
		/// </remarks>
		Searchable = 8,
		
		/// <summary> Indicates that the cursor is updateable. </summary>
		/// <remarks>
		///	The following methods are included in the Updateable cursor capability:
		///		Insert
		///		Update
		///		Delete
		///		Default
		///		Change
		///		Validate
		/// </remarks> 
		Updateable = 16, 
		
		/// <summary> Indicates that the cursor supports truncation. </summary>
		/// <remarks>
		/// The following methods are included in the Truncateable cursor capability:
		///		Truncate
		/// </remarks>
		Truncateable = 32,
		
		/// <summary> Indicates that the cursor supports row count. </summary>
		/// <remarks>
		///	The following methods are included in the Countable cursor capability:
		///		RowCount
		/// </remarks>
		Countable = 64,

		///// <summary> Indicates that the cursor supports elaboration. </summary>
		///// <remarks>
		///// This capability determines whether or not the compiler will perform
		///// reference inference and other U/I elaboration related functionality
		///// to enable the resulting cursor definition to be used as the input
		///// to a form derivation process.
		///// </remarks>
		Elaborable = 128
	}
	
    public enum CursorType 
    { 
		/// <summary> Indicates that the cursor is insensitive to updates made 
		/// to the result set after the cursor has been opened. </summary>
		/// <remarks>
		///	In a static cursor, updates made to rows in the result set, either 
		///	by the user of the cursor, or updates that become visible based on 
		///	the isolation level of the cursor from other transactions, are not 
		///	visible.  The result set is fully materialized on open, and no 
		///	changes are made to this set.
		/// </remarks>
		Static, 
		
		/// <summary> Indicates that the cursor is sensitive to updates made 
		/// to rows in the result set after the cursor has been opened. </summary>
		/// <remarks>
		///	In a dynamic cursor, updates made to rows in the result set, either 
		///	by the user of the cursor, or from other transactions that become 
		///	visible based on the isolation level of the cursor, are visible 
		///	through the cursor. The result set is dynamically queried for as 
		///	it is requested.  Note that this is not a guarantee that updates made
		/// by other transactions will be visible, only that the system is not 
		/// required to exclude them.  Depending on how the query is processed, 
		/// and how the devices performing the processing manipulate the rows 
		/// in the result set, external updates may or may not be visible.  
		/// However, a dynamic cursor does guarantee that updates made through 
		/// the cursor are visible.
		/// </remarks>
		Dynamic 
	}
    
    /// <summary>Enumerates the set of cursor isolation levels provided by the Dataphor Server.</summary>
    /// <remarks>
    /// The isolation level of a cursor allows the cursor to control how concurrency control is implemented
    /// within the cursor. This control is still subject to the isolation level of the transaction in which
    /// the cursor is running.
    /// </remarks>
    public enum CursorIsolation 
    { 
		/// <summary> Indicates that the cursor runs at the isolation level of the current transaction. </summary>
		None,
		/// <summary> This cursor isolation level is deprecated and should not be used. When encountered, this isolation level is equivalent to Browse. </summary>
		Chaos, 
		/// <summary> Browse cursor isolation indicates that the cursor should use optimistic concurrency control </summary>
		Browse, 
		/// <summary> This cursor isolation level is deprecated and should not be used. When encountered, this isolation level is equivalent to Isolated. </summary>
		CursorStability, 
		/// <summary> Isolated cursor isolation indicates that the cursor should use pessimistic concurrency control </summary>
		Isolated 
	}
	
	public interface IServerCursorBehavior
	{
        /// <summary> Retrieves the capabilities of the cursor. </summary>
        CursorCapability Capabilities { get; }
        
        /// <summary> Retrieves the type of the cursor. </summary>
        CursorType CursorType { get; }

		/// <summary> Allows for querying the cursor's capabilities. </summary>
		/// <param name="ACapability"> A capability to query for. </param>
		/// <returns> True if the cursor supports the specified capability, false otherwise. </returns>
        bool Supports(CursorCapability ACapability);
        
        /// <summary> Retrieves the cursor's isolation. </summary>
        CursorIsolation Isolation { get; }
	}
    
	/// <summary> Represents a cursor. </summary>
	/// <seealso cref="IServerCursor"/> <seealso cref="IRemoteServerCursor"/>
    public interface IServerCursorBase : IDisposableNotify, IServerCursorBehavior 
    {
		/// <summary> Opens the cursor. </summary>
		void Open();
		
		/// <summary> Closes the cursor. </summary>
		void Close();
		
		/// <summary> Gets or sets whether the cursor is currently open (Active). </summary>
		bool Active { get; set; }
	}
    
    /// <summary> Local server "cracked" cursor interface. </summary>
    public interface IServerCursor : IServerCursorBase, IProposable
    {
        /// <value> Returns the <see cref="IServerExpressionPlan"/> instance for this cursor. </value>
        IServerExpressionPlan Plan { get; }
        
		// core cursor support

		/// <summary> Requeries for the result of the cursor, leaving the cursor on the BOF "crack". </summary>
        void Reset();

		/// <summary> Navigates to the next data row, or off of the BOF "crack" onto the first row. </summary>
		/// <returns> True if the cursor is positioned on a row, false otherwise. </returns>
        bool Next();

		/// <summary> Navigates to the EOF crack in the cursor. </summary>
        void Last();

		/// <summary> Determines whether the cursor is situated on the BOF "crack". </summary>
		/// <remarks>
		///	This will always be true when the cursor is first opened (or reset).
		///	Data cannot be retrieved while the cursor is positioned on a crack (BOF or EOF).
		///	</remarks>
        bool BOF();

		/// <summary> Determines whether the cursor is situated on the EOF "crack". </summary>
		/// <remarks>
		///	This will be true when the cursor is moved past the last data row (or last is called).
		///	Data cannot be retrieved while the cursor is positioned on a crack (BOF or EOF).
		///	</remarks>
		bool EOF();

		/// <summary> Returns true if the cursor has no rows. </summary>
        bool IsEmpty();

		/// <summary> Retrieves the current row from the cursor. </summary>
		/// <remarks>
		///	If the cursor is located on a "crack" (EOF or BOF), this method will throw an exception.
		///	Use this overload if you do not have a prepared row to select the data into.
		///	</remarks>
		///	<returns> A newly constructed <see cref="Row"/> containing the data for the current row of the cursor. </returns>
        IRow Select();

		/// <summary> Selects the values from the current row of the cursor into an existing <see cref="Row"/>. </summary>
		/// <remarks>
		/// <para>
		///	If the cursor is located on a "crack" (EOF or BOF), this method will throw an exception.
		///	</para>
		///	<para>Use this overload if you already have a prepared row to select the data 
		///	into. This is preferable if you are selecting multiple rows from the cursor.</para>
		///	</remarks>
		/// <param name="ARow"> The prepared <see cref="Row"/> to retrieve the data values into. </param>
        void Select(IRow ARow);
        
        // BackwardsNavigable

		/// <summary> Situates the cursor on the BOF "crack". </summary>
		/// <remarks> The cursor must be BackwardsNavigable. </remarks>
        void First();

		/// <summary> Navigates the cursor to the prior row or to the BOF "crack". </summary>
		/// <remarks> The cursor must be BackwardsNavigable. </remarks>
		/// <returns> False if after the navigation, the cursor is situated on the BOF "crack". </returns>
		bool Prior();
        
        // Bookmarkable 
		/// <summary> Retrieves a bookmark representing the current location of the cursor. </summary>
		/// <remarks> The cursor must be Bookmarkable. </remarks>
		/// <returns>
		///	A Guid that may be used as a handle in subsequent bookmarkable calls.  
		///	A bookmark requested using this method must be released by calling <see cref="DisposeBookmark"/>.
		///	</returns>
        Guid GetBookmark();

		/// <summary> Situates the cursor on the row corresponding to the given bookmark. </summary>
		/// <remarks> The cursor must be Bookmarkable. </remarks>
		/// <param name="AForward"> Hint regarding the intended navigation following positioning. </param>
		/// <returns> True if the cursor was successfully situated on the bookmark. </returns>
		bool GotoBookmark(Guid ABookmark, bool AForward);

		/// <summary> Returns a comparison of bookmark values. </summary>
		/// <remarks> The cursor must be Bookmarkable. </remarks>
		/// <returns>
		///	-1 if ABookmark1 is less than ABookmark2.
		///	0 if ABookmark1 is equal to ABookmark2.
		///	1 if ABookmark1 is greater than ABookmark2.
		///	 </returns>
        int CompareBookmarks(Guid ABookmark1, Guid ABookmark2);

		/// <summary> Disposes a bookmark previously allocated with <see cref="GetBookmark"/>. </summary>
		/// <remarks> This method has no effect if the bookmark does not exist, or has already been disposed. </remarks>
        /// <seealso cref="DisposeBookmarks"/>
		void DisposeBookmark(Guid ABookmark);

		/// <summary> Disposes a list of bookmarks. </summary>
		/// <remarks> This call has no effect if the bookmarks do not exist, or have already been disposed. </remarks>
        /// <seealso cref="DisposeBookmark"/>
		void DisposeBookmarks(Guid[] ABookmarks);
        
        // Searchable

		/// <summary> Represents the currently selected order of the cursor. </summary>
		/// <remarks> The cursor must be Searchable. </remarks>
        Schema.Order Order { get; }

		/// <summary> Retrieves the key value for the current position of the cursor. </summary>
		/// <remarks> The cursor must be Searchable. </remarks>
		IRow GetKey();

		/// <summary> Situates the cursor on the row identified by the specified key value. </summary>
		/// <remarks> The cursor must be Searchable. </remarks>
		/// <returns> True if the key was successfully located. </returns>
        bool FindKey(IRow AKey);

		/// <summary> Situates the cursor on the nearest row to the specified key value. </summary>
		/// <remarks> The cursor must be Searchable. </remarks>
		void FindNearest(IRow AKey);

		/// <summary> Performs a refresh operation combined with a <see cref="FindKey"/>. </summary>
		/// <remarks> The cursor must be Searchable. </remarks>
		/// <returns> True if the cursor is positioned on the given row, false otherwise. </returns>
		bool Refresh(IRow ARow);

		// Updateable

		/// <summary> Inserts a new row of data into the cursor. </summary>
		/// <remarks>
		///		<para>
		///		The cursor must support Updateable to perform this operation.
        ///     </para>
		///	</remarks>
		void Insert(IRow ARow);
		
		/// <summary> Inserts the given row into the cursor. </summary>
		/// <remarks>
		///		Value flags, if specified, indicates which columns of the row have values set.
		/// </remarks>
		void Insert(IRow ARow, BitArray AValueFlags);

		/// <summary> Updates the row where the cursor is currently situated. </summary>
		/// <remarks>
		///		<para>
		///		The cursor must not be on the EOF or BOF "crack" or an exception will be thrown.
		///		The cursor must support Updateable to perform this operation.
		///		</para>
		///	</remarks>
		void Update(IRow ARow);
		
		/// <summary> Updates the current row of the cursor with the given values. </summary>
		/// <remarks>
		///		Value flags, if specified, indicates which columns of the row are being changed.
		/// </remarks>
		void Update(IRow ARow, BitArray AValueFlags);

		/// <summary> Deletes the row where the cursor is currently situated. </summary>
		/// <remarks>
		///		<para>
		///		The cursor must not be on the EOF or BOF "crack" or an exception will be thrown.
		///		The cursor must support Updateable to perform this operation.
		///		</para>
		///	</remarks>
		void Delete();
        
		// Countable

        /// <summary> Returns an integer value indicating the number of rows in the cursor. </summary>
        /// <remarks> The cursor must support Countable. </remarks>
        int RowCount();
    }
    
}

