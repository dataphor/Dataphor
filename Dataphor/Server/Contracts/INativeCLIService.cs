/*
	Dataphor
	© Copyright 2000-2009 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Text;
using System.Collections.Generic;
using System.ServiceModel;

using Alphora.Dataphor.DAE.NativeCLI;

namespace Alphora.Dataphor.DAE.Contracts
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
	public interface INativeCLIService
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
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		NativeSessionHandle StartSession(NativeSessionInfo ASessionInfo);
		
		/// <summary>
		/// Stops a session
		/// </summary>
		/// <remarks>
		/// Once a session has been stopped, the session handle is no longer valid.
		/// </remarks>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		void StopSession(NativeSessionHandle ASessionHandle);

		#endregion
		
		#region Transaction Management

        /// <summary>
		/// Begins a new transaction.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
        void BeginTransaction(NativeSessionHandle ASessionHandle, NativeIsolationLevel AIsolationLevel);
        
        /// <summary>
		/// Prepares a transaction for commit.
		/// </summary>
        /// <remarks>
        /// Validates that all data within the transaction is consistent, and prepares the transaction for commit.
        /// It is not necessary to call this to commit the transaction, it is exposed to allow the process to participate in
        /// 2PC distributed transactions.
        /// </remarks>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
        void PrepareTransaction(NativeSessionHandle ASessionHandle);
        
        /// <summary>
		/// Commits the currently active transaction.
		/// </summary>
        /// <remarks>
        /// Commits the currently active transaction.  
        /// Reduces the transaction nesting level by one.  
        /// Will raise if no transaction is currently active.
        /// </remarks>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
        void CommitTransaction(NativeSessionHandle ASessionHandle);
        
        /// <summary>
		/// Rolls back the currently active transaction.
		/// </summary>
        /// <remarks>
        /// Reduces the transaction nesting level by one.
        /// Will raise if no transaction is currently active.
        /// </remarks>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
        void RollbackTransaction(NativeSessionHandle ASessionHandle);
        
        /// <summary>
		/// Returns the number of active transactions.
		/// </summary>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
        int GetTransactionCount(NativeSessionHandle ASessionHandle);

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
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		NativeResult ExecuteStatement(NativeSessionInfo ASessionInfo, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions);

		/// <summary>
		/// Executes a series of D4 statements and the returns the results, if any.
		/// </summary>
		/// <remarks>
		/// Note that this overload does not require a session and is a stateless call. If transactional
		/// control is necessary, use the session-specific overload.
		/// </remarks>
		/// <param name="AOperations">The operations to be executed.</param>
		/// <returns>An array of NativeResult objects containing the results of the executions, if any.</returns>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		NativeResult[] ExecuteStatements(NativeSessionInfo ASessionInfo, NativeExecuteOperation[] AOperations);
		
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
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		NativeResult SessionExecuteStatement(NativeSessionHandle ASessionHandle, string AStatement, NativeParam[] AParams, NativeExecutionOptions AOptions);

		/// <summary>
		/// Executes a series of D4 statements and returns the results, if any.
		/// </summary>
		/// <param name="AOperations">The operations to be executed.</param>
		/// <returns>An array of NativeResult objects containing the results of the executions, if any.</returns>
		[OperationContract]
		[FaultContract(typeof(NativeCLIFault))]
		NativeResult[] SessionExecuteStatements(NativeSessionHandle ASessionHandle, NativeExecuteOperation[] AOperations);
		
		#endregion
	}
}
