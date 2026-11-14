/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Alphora.Dataphor.DAE.NativeCLI
{
	/// <summary>
	/// Defines the interface for a simple, optionally stateless CLI for the Dataphor server.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Because the standard Dataphor CLI requires a locally running Dataphor repository, it
	/// is ill-suited for use in lightweight contexts such as cross-instance querying or
	/// browser-based applications. In addition, the amount of state required to be maintained
	/// for a standard CLI session limits the scalability of applications built using that CLI.
	/// </para>
	/// <para>
	/// The Native CLI described by this interface substantially reduces (and can eliminate) the 
	/// state required to be maintained by the client to communicate with a Dataphor server. The
	/// only state that is maintained is an optional session handle which can be used to obtain
	/// transactional control over the connection if desired. The session-less overloads of Execute
	/// can be used stand-alone to obtain a completely stateless communication channel to the Dataphor
	/// server.
	/// </para>
	/// </remarks>
	[ServiceContract(Name = "INativeCLIService", Namespace = "http://dataphor.org/dataphor/3.0/")]
	public interface IClientNativeCLIService
	{
		#region Session Management

		/// <summary>
		/// Starts a session
		/// </summary>
		/// <remarks>
		/// Note that a session is only required if transaction control is necessary.
		/// The Execute overloads that do not take a session will automatically
		/// establish a connection as necessary.
		/// </remarks>
		/// <returns>A session handle that can be used in subsequent calls.</returns>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginStartSession(NativeSessionInfo ASessionInfo, AsyncCallback ACallback, object AState);
		NativeSessionHandle EndStartSession(IAsyncResult AResult);
		
		/// <summary>
		/// Stops a session
		/// </summary>
		/// <remarks>
		/// Once a session has been stopped, the session handle is no longer valid.
		/// </remarks>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginStopSession(NativeSessionHandle ASessionHandle, AsyncCallback ACallback, object AState);
		void EndStopSession(IAsyncResult AResult);

		#endregion
		
		#region Transaction Management

        /// <summary>
		/// Begins a new transaction.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
        IAsyncResult BeginBeginTransaction(NativeSessionHandle ASessionHandle, NativeIsolationLevel AIsolationLevel, AsyncCallback ACallback, object AState);
        void EndBeginTransaction(IAsyncResult AResult);
        
        /// <summary>
		/// Prepares a transaction for commit.
		/// </summary>
        /// <remarks>
        /// Validates that all data within the transaction is consistent, and prepares the transaction for commit.
        /// It is not necessary to call this to commit the transaction, it is exposed to allow the process to participate in
        /// 2PC distributed transactions.
        /// </remarks>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
        IAsyncResult BeginPrepareTransaction(NativeSessionHandle ASessionHandle, AsyncCallback ACallback, object AState);
        void EndPrepareTransaction(IAsyncResult AResult);
        
        /// <summary>
		/// Commits the currently active transaction.
		/// </summary>
        /// <remarks>
        /// Commits the currently active transaction.  
        /// Reduces the transaction nesting level by one.  
        /// Will raise if no transaction is currently active.
        /// </remarks>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
        IAsyncResult BeginCommitTransaction(NativeSessionHandle ASessionHandle, AsyncCallback ACallback, object AState);
        void EndCommitTransaction(IAsyncResult AResult);
        
        /// <summary>
		/// Rolls back the currently active transaction.
		/// </summary>
        /// <remarks>
        /// Reduces the transaction nesting level by one.
        /// Will raise if no transaction is currently active.
        /// </remarks>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
        IAsyncResult BeginRollbackTransaction(NativeSessionHandle ASessionHandle, AsyncCallback ACallback, object AState);
        void EndRollbackTransaction(IAsyncResult AResult);
        
        /// <summary>
		/// Returns the number of active transactions.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
        IAsyncResult BeginGetTransactionCount(NativeSessionHandle ASessionHandle, AsyncCallback ACallback, object AState);
        int EndGetTransactionCount(IAsyncResult AResult);

		#endregion
		
		#region Statement Execution
		
		/// <summary>
		/// Executes a D4 statement and returns the result, if any.
		/// </summary>
		/// <param name="AStatement">The D4 statement to be executed.</param>
		/// <param name="AParams">The parameters to the statement, if any.</param>
		/// <remarks>
		/// <para>
		/// If the statement is an expression, the result will be a NativeResult descendent corresponding to
		/// the type of the expression, scalar, row, list, or table.
		/// </para>
		/// <para>
		/// Note that this overload does not require a session and is a stateless call. If transactional
		/// control is necessary, use the session-specific overload.
		/// </para>
		/// </remarks>
		/// <returns>The result of the execution, if the statement is an expression, null otherwise.</returns>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginExecuteStatement(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions, AsyncCallback ACallback, object AState);
		NativeResult EndExecuteStatement(IAsyncResult AResult);

		/// <summary>
		/// Executes a series of D4 statements and the returns the results, if any.
		/// </summary>
		/// <remarks>
		/// Note that this overload does not require a session and is a stateless call. If transactional
		/// control is necessary, use the session-specific overload.
		/// </remarks>
		/// <param name="AOperations">The operations to be executed.</param>
		/// <returns>An array of NativeResult objects containing the results of the executions, if any.</returns>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginExecuteStatements(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations, AsyncCallback ACallback, object AState);
		NativeResult[] EndExecuteStatements(IAsyncResult AResult);
		
		/// <summary>
		/// Executes a D4 statement and returns the result, if any.
		/// </summary>
		/// <param name="AStatement">The D4 statement to be executed.</param>
		/// <param name="AParams">The parameters to the statement, if any.</param>
		/// <remarks>
		/// <para>
		/// If the statement is an expression, the result will be a NativeResult descendent corresponding to
		/// the type of the expression, scalar, row, list, or table.
		/// </para>
		/// </remarks>
		/// <returns>The result of the execution, if the statement is an expression, null otherwise.</returns>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginSessionExecuteStatement(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions, AsyncCallback ACallback, object AState);
		NativeResult EndSessionExecuteStatement(IAsyncResult AResult);

		/// <summary>
		/// Executes a series of D4 statements and the returns the results, if any.
		/// </summary>
		/// <param name="AOperations">The operations to be executed.</param>
		/// <returns>An array of NativeResult objects containing the results of the executions, if any.</returns>
		[OperationContract(AsyncPattern = true)]
		[FaultContract(typeof(NativeCLIFault))]
		IAsyncResult BeginSessionExecuteStatements(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations, AsyncCallback ACallback, object AState);
		NativeResult[] EndSessionExecuteStatements(IAsyncResult AResult);
		
		#endregion
	}
}
