/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt

	TODO: Client-side caching
*/

using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;

using Alphora.Dataphor;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Streams;
using Alphora.Dataphor.DAE.Language;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime.Data;
using Schema = Alphora.Dataphor.DAE.Schema;

namespace Alphora.Dataphor.Frontend.Client
{
	/// <summary> Used in PipeRequest as a callback when the request is complete. </summary>
	/// <seealso cref="PipeRequest"/>
	public delegate void PipeResponseHandler(PipeRequest ARequest, Pipe APipe);
	/// <summary> Used in PipeRequest as a callback when a request fails. </summary>
	/// <seealso cref="PipeRequest"/>
	public delegate void PipeErrorHandler(PipeRequest ARequest, Pipe APipe, Exception AError);

	/// <summary> Used for asynchronous requests. </summary>
	public class PipeRequest
	{
		/// <summary> Creates a new PipeRequest with a reference string, responsehandler, and error handler delegates. </summary>
		public PipeRequest(string ADocument, PipeResponseHandler AResponseHandler, PipeErrorHandler AErrorHandler) : base()
		{
			Document = ADocument;
			ResponseHandler = AResponseHandler;
			ErrorHandler = AErrorHandler;
		}

		private string FDocument = String.Empty;
		/// <summary> The expression for the document to be requested. </summary>
		public string Document
		{
			get { return FDocument; }
			set { FDocument = value; }
		}

		/// <summary> Event to be called when the request is complete. </summary>
		public PipeResponseHandler ResponseHandler;

		/// <summary> Event to be called when the request fails. </summary>
		public PipeErrorHandler ErrorHandler;

		internal Scalar FResult;
		/// <summary> This will contain the data after the request is complete. </summary>
		/// <remarks> This scalar result will automatically be Disposed by the async handler. </remarks>
		public Scalar Result { get { return FResult; } }
	}

	/// <summary> Used to invoke a given delegate with the given arguments. </summary>
	public delegate object InvokeHandler(Delegate ADelegate, object[] AArguments);
	
	/// <summary> Allows synchronous and queued asynchronous retrieval of data from a document resource. </summary>
	/// <remarks>
	///		The pipe utilizes up to two connections at a time; one performing 
	///		synchronous requests, the other handling the currently active
	///		asynchronous request.  The pipe queues asynchronous requests ensuring
	///		the usage of one connection for all asynchronous requests.
	/// </remarks>
	public class Pipe : Disposable
	{
		public Pipe(IServerSession AServerSession)
		{
			FServerSession = AServerSession;

			ProcessInfo LProcessInfo = new ProcessInfo(FServerSession.SessionInfo);
			LProcessInfo.DefaultIsolationLevel = DAE.IsolationLevel.Browse;
			FSyncProcess = FServerSession.StartProcess(LProcessInfo);

			InitializeAsync();
		}

		protected override void Dispose(bool ADisposing)
		{
			lock (FAsyncLock)
			{
				CancelAll();
				FStoppingAsync = true;
				FQueueSignal.Set();
			}
			// Don't bother waiting around for the thread to finish, it will close the signal when it completes 
			//  and blocking here might (will) cause deadlock when the thread tries to sync back onto the main thread

			base.Dispose(ADisposing);
			
			if (FAsyncProcess != null)
				FServerSession.StopProcess(FAsyncProcess);
			FServerSession.StopProcess(FSyncProcess);
		}

		private DAE.IServerSession FServerSession;
		private DAE.IServerProcess FSyncProcess;

		public DAE.IServerProcess Process
		{
			get { return FSyncProcess; }
		}

		#region Caches

		private FixedSizeCache<string, byte[]> FImageCache;
		public FixedSizeCache<string, byte[]> ImageCache
		{
			get { return FImageCache; }
			set { FImageCache = value; }
		}

		private IDocumentCache FCache;
		/// <summary> Optional document cache associated with this pipe. </summary>
		/// <remarks> The pipe does not own this cache. </remarks>
		public IDocumentCache Cache
		{
			get { return FCache; }
			set { FCache = value; }
		}

		private DAE.Schema.RowType FCacheRowType;	// shared across threads (synced using the pipe instance)

		private DAE.Schema.RowType GetCacheRowType(IServerProcess AProcess)
		{
			if (FCacheRowType == null)
			{
				FCacheRowType = new DAE.Schema.RowType();
				FCacheRowType.Columns.Add(new DAE.Schema.Column("Value", AProcess.DataTypes.SystemScalar));
			}
			return FCacheRowType;
		}

		private Scalar LoadFromCache(string ADocument, IServerProcess AProcess)
		{
			byte[] LData;
			using (Stream LStream = Cache.Reference(ADocument))
			{
				int LLength = StreamUtility.ReadInteger(LStream);
				LData = new byte[LLength];
				LStream.Read(LData, 0, LLength);
			}
			using (DAE.Runtime.Data.Row LRow = ((DAE.Runtime.Data.Row)DataValue.FromPhysical(AProcess.GetServerProcess().ValueManager, GetCacheRowType(AProcess), LData, 0)))	// Uses GetServerProcess() as an optimization because this row is to remain local
			{
				return (Scalar)LRow.GetValue("Value").Copy();
			}
		}

		private void SaveToCache(string ADocument, IServerProcess AProcess, DAE.Runtime.Data.DataValue AValue, uint ACRC32)
		{
			using (Stream LTargetStream = Cache.Freshen(ADocument, ACRC32))
			{
				byte[] LBytes;
				using (DAE.Runtime.Data.Row LRow = new DAE.Runtime.Data.Row(AProcess.ValueManager, GetCacheRowType(AProcess)))
				{
					LRow["Value"] = AValue;
					LBytes = new byte[LRow.GetPhysicalSize(true)];
					LRow.WriteToPhysical(LBytes, 0, true);
				}
				StreamUtility.WriteInteger(LTargetStream, LBytes.Length);
				LTargetStream.Write(LBytes, 0, LBytes.Length);
			}
		}

		private bool IsImageRequest(string ADocument)
		{
			if ((ADocument != null) && (ADocument != String.Empty))
			{
				Expression LParsedExpression;
				CallExpression LCallExpression;
			
				Alphora.Dataphor.DAE.Language.D4.Parser LParser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				LParsedExpression = LParser.ParseExpression(ADocument);
				
				//check to see if its a qulifier expression first				
				QualifierExpression LQualifierExpression = LParsedExpression as QualifierExpression;
				if (LQualifierExpression != null)
				{
					if ((((IdentifierExpression)LQualifierExpression.LeftExpression).Identifier) == "Frontend" || 
						(((IdentifierExpression)LQualifierExpression.LeftExpression).Identifier) == ".Frontend")
					{
						LCallExpression = LQualifierExpression.RightExpression as CallExpression;
					}
					else 
						LCallExpression = LParsedExpression as CallExpression;
				}
				else 
					LCallExpression = LParsedExpression as CallExpression;
				
				return ((LCallExpression != null) && (LCallExpression.Identifier == "Image"));
			}
			return false;
		}

		private void SaveToImageCache(string ADocument, IServerProcess AProcess, Scalar LResult)
		{
			lock (ImageCache)
			{
				byte[] LValue = null;
				if ((LResult != null) && (!LResult.IsNil))
					LValue = LResult.AsByteArray;
				ImageCache.Add(ADocument, LValue);
			}
		}

		private Scalar LoadWithCache(string ADocument, IServerProcess AProcess)
		{
			Scalar LResult;
			lock (Cache)
			{
				uint LCRC32 = Cache.GetCRC32(ADocument);

				using
					(
						DAE.Runtime.Data.Row LRow = (DAE.Runtime.Data.Row)AProcess.Evaluate
						(
							String.Format
							(
								"LoadIfNecessary('{0}', {1})",
								ADocument.Replace("'", "''"),
								((int)LCRC32).ToString()
							),
							null
						)
					)
				{
					if ((bool)LRow["CRCMatches"])
						LResult = LoadFromCache(ADocument, AProcess);
					else
					{
						using (DAE.Runtime.Data.Scalar LValue = LRow.GetValue("Value") as DAE.Runtime.Data.Scalar)
						{
							SaveToCache(ADocument, AProcess, LValue, (uint)(int)LRow["ActualCRC32"]);
							LResult = (DAE.Runtime.Data.Scalar)LValue.Copy();
						}
					}
				}
			}
			return LResult;
		}

		#endregion

		#region Synchronous requests

		private Scalar InternalRequest(string ADocument, IServerProcess AProcess)
		{
			bool LIsImageRequest = false;

			if (ImageCache != null)
			{
				LIsImageRequest = IsImageRequest(ADocument);
				if (LIsImageRequest)
					lock (ImageCache)
					{
						byte[] LData;
						if (ImageCache.TryGetValue(ADocument, out LData))
							return new Scalar(AProcess.ValueManager, AProcess.DataTypes.SystemGraphic, LData);
					}
			}

			Scalar LResult;
			
			if (Cache != null)
				LResult = LoadWithCache(ADocument, AProcess);
			else
				LResult = (Scalar)AProcess.Evaluate(ADocument, null);
			
			if (LIsImageRequest)
			{
				SaveToImageCache(ADocument, AProcess, LResult);
			}

			return LResult;
		}

		/// <summary> Requests a document and returns the scalar results. </summary>
		/// <param name="ADocument"> A document expression to request. </param>
		/// <returns> A scalar value.  The caller must Dispose this Scalar to free it's resources. </returns>
		public Scalar RequestDocument(string ADocument)
		{
			return InternalRequest(ADocument, FSyncProcess);
		}

		#endregion

		#region Asynchronous requests

		private DAE.IServerProcess FAsyncProcess;

		/// <summary> Asynchronous worker thread. </summary>
		private Thread FAsyncThread;
		
		/// <summary> Used internally to track which request is currently being processed. </summary>
		private PipeRequest FCurrentRequest;
		
		/// <summary> Internal list used for request queueing. </summary>
		private List<PipeRequest> FQueue = new List<PipeRequest>();

		/// <summary> Signaled while there is something in the queue. </summary>
		private ManualResetEvent FQueueSignal = new ManualResetEvent(false);
		
		/// <summary> Protects FQueue, FCurrentRequest, and FQueueSignal. </summary>
		private object FAsyncLock = new Object();

		/// <summary> Indicates that the current request has been cancelled. </summary>
		private bool FCancellingCurrent;

		/// <summary> Indicates that the asynchonous thread is to terminate. </summary>
		private bool FStoppingAsync;

		/// <summary> Queues up an asynchronous request for a document. </summary>
		public void QueueRequest(PipeRequest APipeRequest)
		{
			lock (FAsyncLock)
			{
				FQueue.Add(APipeRequest);
				FQueueSignal.Set();
			}
		}

		/// <summary> Removes a request from the queue or cancels the request if it is being processed. </summary>
		/// <remarks> The caller is not guaranteed that after calling this that a response will not be sent. </remarks>
		public void CancelRequest(PipeRequest APipeRequest)
		{
			lock (FAsyncLock)
			{
				if (APipeRequest == FCurrentRequest)
					CancelCurrent();
				else
				{
					FQueue.Remove(APipeRequest);
					if (FQueue.Count == 0)
						FQueueSignal.Reset();
				}
			}
		}

		private void InitializeAsync()
		{
			CreateAsyncProcess();

			FAsyncThread = new Thread(new ThreadStart(ProcessAsync));
			FAsyncThread.IsBackground = true;
			FAsyncThread.Start();
		}
		
		private void CreateAsyncProcess()
		{
			ProcessInfo LProcessInfo = new ProcessInfo(FServerSession.SessionInfo);
			LProcessInfo.DefaultIsolationLevel = DAE.IsolationLevel.Browse;
			if (!FStoppingAsync)
				FAsyncProcess = FServerSession.StartProcess(LProcessInfo);
			else
				FAsyncProcess = null;
		}

		/// <summary> Cancels all queued requests. </summary>
		public void CancelAll()
		{
			lock (FAsyncLock)
			{
				FQueue.Clear();
				FQueueSignal.Reset();
				if (FCurrentRequest != null)
					CancelCurrent();
			}
		}

		/// <remarks> Caller should have the FAsyncLock.  Asynchronously request that the currently 
		/// executing process be stopped; we do not want to block the current thread waiting for the 
		/// shut-down.  We cannot guarantee that a callback will not occur once a cancel has been made.
		/// This because the FCancellingCurrent flag may change after the flag check but before the 
		/// invocation. </remarks>
		private void CancelCurrent()
		{
			if (FAsyncProcess != null)
			{
				FCancellingCurrent = true;
				ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncCancelCurrent), FAsyncProcess.ProcessID);
				CreateAsyncProcess();
			}
		}

		private void AsyncCancelCurrent(object AState)
		{
			try
			{
				// Stop the current process
				IServerProcess LProcess = FServerSession.StartProcess(new ProcessInfo(FServerSession.SessionInfo));
				try
				{
					LProcess.Execute("StopProcess(" + ((int)AState).ToString() + ")", null);
				}
				finally
				{
					FServerSession.StopProcess(LProcess);
				}
			}
			catch
			{
				// Do nothing, but catch all exceptions; the framework aborts the app if a thread leaves unhandled exceptions
			}
			finally
			{
				FCancellingCurrent = false;
			}
		}

		/// <summary> Main asynchronous thread loop. </summary>
		private void ProcessAsync()
		{
			for (;;)
			{
				try
				{
					// Wait until an item is enqueued or shut-down occurs
					FQueueSignal.WaitOne();

					// Check for shut-down
					if (FStoppingAsync)
						break;

					lock (FAsyncLock)
					{
						// Dequeue an item
						if (FQueue.Count > 0)
						{
							FCurrentRequest = FQueue[0];
							FQueue.RemoveAt(0);
							if (FQueue.Count == 0)
								FQueueSignal.Reset();
						}
					}
					try
					{
						try
						{
							// Make the request
							FCurrentRequest.FResult = InternalRequest(FCurrentRequest.Document, FAsyncProcess);
						}
						catch (Exception LException)
						{
							// Error callback
							// Can't take a lock to ensure that FCancellingCurrent doesn't change before we invoke (might dead-lock on the SafelyInvoke back to the main thread... which might be waiting on the FAsyncLock)
							if ((FCurrentRequest.ErrorHandler != null) && !FCancellingCurrent)
								SafelyInvoke(FCurrentRequest.ErrorHandler, new object[] { FCurrentRequest, this, LException });
							// Don't rethrow
							continue; // Skip the success callback
						}

						// Success callback
						// Can't take a lock to ensure that FCancellingCurrent doesn't change before we invoke (might dead-lock on the SafelyInvoke back to the main thread... which might be waiting on the FAsyncLock)
						if ((FCurrentRequest.ResponseHandler != null) && !FCancellingCurrent)
							SafelyInvoke(FCurrentRequest.ResponseHandler, new object[] { FCurrentRequest, this });
					}
					catch (ThreadAbortException)
					{
						// An abort above will cause this thread to terminate.  This is because a catch will not 
						//  prevent abort exception propagation.  Must prevent this by resetting the abort state.
						#if !SILVERLIGHT
						System.Threading.Thread.ResetAbort();
						#endif
						// Don't rethrow - unnecessary
					}
					finally
					{
						lock (FAsyncLock)
						{
							try
							{
								try
								{
									// Free up the result resources
									if (FCurrentRequest.FResult != null)
										FCurrentRequest.FResult.Dispose();
								}
								finally
								{
									// Clear the current request
									FCurrentRequest = null;
								}
							}
							finally
							{
/*
								if (FCancellingCurrent)
								{
									// Reset the cancel flag
									FCancellingCurrent = false;

									// Start a new process, the old one was stopped
									CreateAsyncProcess();
								}
*/
							}
						}
					}
				}
				catch (Exception LException)
				{
					Error.Warn(LException.ToString());
					// Don't rethrow.  We do not want an error here to stop the worker thread (and terminate the application).
				}
			}
			FQueueSignal.Close();
		}

		public event InvokeHandler OnSafelyInvoke;

		/// <summary> Attempts to execute the delegate on the main window thread. </summary>
		/// <remarks> If there is no main window, the delegate is invoked within this thread. </remarks>
		private object SafelyInvoke(Delegate ADelegate, object[] AArguments)
		{
			if (OnSafelyInvoke != null)
				return OnSafelyInvoke(ADelegate, AArguments);
			else
				return ADelegate.DynamicInvoke(AArguments);
		}

		#endregion
	}
}
