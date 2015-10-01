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
		public PipeRequest(string document, PipeResponseHandler responseHandler, PipeErrorHandler errorHandler) : base()
		{
			Document = document;
			ResponseHandler = responseHandler;
			ErrorHandler = errorHandler;
		}

		private string _document = String.Empty;
		/// <summary> The expression for the document to be requested. </summary>
		public string Document
		{
			get { return _document; }
			set { _document = value; }
		}

		/// <summary> Event to be called when the request is complete. </summary>
		public PipeResponseHandler ResponseHandler;

		/// <summary> Event to be called when the request fails. </summary>
		public PipeErrorHandler ErrorHandler;

		internal IScalar _result;
		/// <summary> This will contain the data after the request is complete. </summary>
		/// <remarks> This scalar result will automatically be Disposed by the async handler. </remarks>
		public IScalar Result { get { return _result; } }
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
		public Pipe(IServerSession serverSession)
		{
			_serverSession = serverSession;

			ProcessInfo processInfo = new ProcessInfo(_serverSession.SessionInfo);
			processInfo.DefaultIsolationLevel = DAE.IsolationLevel.Browse;
			_syncProcess = _serverSession.StartProcess(processInfo);

			InitializeAsync();
		}

		protected override void Dispose(bool disposing)
		{
			lock (_asyncLock)
			{
				CancelAll();
				_stoppingAsync = true;
				_queueSignal.Set();
			}
			// Don't bother waiting around for the thread to finish, it will close the signal when it completes 
			//  and blocking here might (will) cause deadlock when the thread tries to sync back onto the main thread

			base.Dispose(disposing);
			
			if (_asyncProcess != null)
				_serverSession.StopProcess(_asyncProcess);
			_serverSession.StopProcess(_syncProcess);
		}

		private DAE.IServerSession _serverSession;
		private DAE.IServerProcess _syncProcess;

		public DAE.IServerProcess Process
		{
			get { return _syncProcess; }
		}

		#region Caches

		private FixedSizeCache<string, byte[]> _imageCache;
		public FixedSizeCache<string, byte[]> ImageCache
		{
			get { return _imageCache; }
			set { _imageCache = value; }
		}

		private IDocumentCache _cache;
		/// <summary> Optional document cache associated with this pipe. </summary>
		/// <remarks> The pipe does not own this cache. </remarks>
		public IDocumentCache Cache
		{
			get { return _cache; }
			set { _cache = value; }
		}

		private DAE.Schema.RowType _cacheRowType;	// shared across threads (synced using the pipe instance)

		private DAE.Schema.RowType GetCacheRowType(IServerProcess process)
		{
			if (_cacheRowType == null)
			{
				_cacheRowType = new DAE.Schema.RowType();
				_cacheRowType.Columns.Add(new DAE.Schema.Column("Value", process.DataTypes.SystemScalar));
			}
			return _cacheRowType;
		}

		private IScalar LoadFromCache(string document, IServerProcess process)
		{
			byte[] data;
			using (Stream stream = Cache.Reference(document))
			{
				int length = StreamUtility.ReadInteger(stream);
				data = new byte[length];
				stream.Read(data, 0, length);
			}
			using (DAE.Runtime.Data.IRow row = ((DAE.Runtime.Data.IRow)DataValue.FromPhysical(process.GetServerProcess().ValueManager, GetCacheRowType(process), data, 0)))	// Uses GetServerProcess() as an optimization because this row is to remain local
			{
				return (IScalar)row.GetValue("Value").Copy();
			}
		}

		private void SaveToCache(string document, IServerProcess process, DAE.Runtime.Data.DataValue value, uint cRC32)
		{
			using (Stream targetStream = Cache.Freshen(document, cRC32))
			{
				byte[] bytes;
				using (DAE.Runtime.Data.Row row = new DAE.Runtime.Data.Row(process.ValueManager, GetCacheRowType(process)))
				{
					row["Value"] = value;
					bytes = new byte[row.GetPhysicalSize(true)];
					row.WriteToPhysical(bytes, 0, true);
				}
				StreamUtility.WriteInteger(targetStream, bytes.Length);
				targetStream.Write(bytes, 0, bytes.Length);
			}
		}

		private bool IsImageRequest(string document)
		{
			if ((document != null) && (document != String.Empty))
			{
				Expression parsedExpression;
				CallExpression callExpression;
			
				Alphora.Dataphor.DAE.Language.D4.Parser parser = new Alphora.Dataphor.DAE.Language.D4.Parser();
				parsedExpression = parser.ParseExpression(document);
				
				//check to see if its a qulifier expression first				
				QualifierExpression qualifierExpression = parsedExpression as QualifierExpression;
				if (qualifierExpression != null)
				{
					if ((((IdentifierExpression)qualifierExpression.LeftExpression).Identifier) == "Frontend" || 
						(((IdentifierExpression)qualifierExpression.LeftExpression).Identifier) == ".Frontend")
					{
						callExpression = qualifierExpression.RightExpression as CallExpression;
					}
					else 
						callExpression = parsedExpression as CallExpression;
				}
				else 
					callExpression = parsedExpression as CallExpression;
				
				return ((callExpression != null) && (callExpression.Identifier == "Image"));
			}
			return false;
		}

		private void SaveToImageCache(string document, IServerProcess process, IScalar LResult)
		{
			lock (ImageCache)
			{
				byte[] value = null;
				if ((LResult != null) && (!LResult.IsNil))
					value = LResult.AsByteArray;
				ImageCache.Add(document, value);
			}
		}

		private IScalar LoadWithCache(string document, IServerProcess process)
		{
			IScalar result;
			lock (Cache)
			{
				uint cRC32 = Cache.GetCRC32(document);

				using
					(
						DAE.Runtime.Data.Row row = (DAE.Runtime.Data.Row)process.Evaluate
						(
							String.Format
							(
								"LoadIfNecessary('{0}', {1})",
								document.Replace("'", "''"),
								((int)cRC32).ToString()
							),
							null
						)
					)
				{
					if ((bool)row["CRCMatches"])
						result = LoadFromCache(document, process);
					else
					{
						using (DAE.Runtime.Data.Scalar value = row.GetValue("Value") as DAE.Runtime.Data.Scalar)
						{
							SaveToCache(document, process, value, (uint)(int)row["ActualCRC32"]);
							result = (DAE.Runtime.Data.Scalar)value.Copy();
						}
					}
				}
			}
			return result;
		}

		#endregion

		#region Synchronous requests

		private IScalar InternalRequest(string document, IServerProcess process)
		{
			bool isImageRequest = false;

			if (ImageCache != null)
			{
				isImageRequest = IsImageRequest(document);
				if (isImageRequest)
					lock (ImageCache)
					{
						byte[] data;
						if (ImageCache.TryGetValue(document, out data))
							return new Scalar(process.ValueManager, process.DataTypes.SystemGraphic, data);
					}
			}

			IScalar result;
			
			if (Cache != null)
				result = LoadWithCache(document, process);
			else
				result = (IScalar)process.Evaluate(document, null);
			
			if (isImageRequest)
			{
				SaveToImageCache(document, process, result);
			}

			return result;
		}

		/// <summary> Requests a document and returns the scalar results. </summary>
		/// <param name="document"> A document expression to request. </param>
		/// <returns> A scalar value.  The caller must Dispose this Scalar to free it's resources. </returns>
		public IScalar RequestDocument(string document)
		{
			return InternalRequest(document, _syncProcess);
		}

		#endregion

		#region Asynchronous requests

		private DAE.IServerProcess _asyncProcess;

		/// <summary> Asynchronous worker thread. </summary>
		private Thread _asyncThread;
		
		/// <summary> Used internally to track which request is currently being processed. </summary>
		private PipeRequest _currentRequest;
		
		/// <summary> Internal list used for request queueing. </summary>
		private List<PipeRequest> _queue = new List<PipeRequest>();

		/// <summary> Signaled while there is something in the queue. </summary>
		private ManualResetEvent _queueSignal = new ManualResetEvent(false);
		
		/// <summary> Protects FQueue, FCurrentRequest, and FQueueSignal. </summary>
		private object _asyncLock = new Object();

		/// <summary> Indicates that the current request has been cancelled. </summary>
		private bool _cancellingCurrent;

		/// <summary> Indicates that the asynchonous thread is to terminate. </summary>
		private bool _stoppingAsync;

		/// <summary> Queues up an asynchronous request for a document. </summary>
		public void QueueRequest(PipeRequest pipeRequest)
		{
			lock (_asyncLock)
			{
				_queue.Add(pipeRequest);
				_queueSignal.Set();
			}
		}

		/// <summary> Removes a request from the queue or cancels the request if it is being processed. </summary>
		/// <remarks> The caller is not guaranteed that after calling this that a response will not be sent. </remarks>
		public void CancelRequest(PipeRequest pipeRequest)
		{
			lock (_asyncLock)
			{
				if (pipeRequest == _currentRequest)
					CancelCurrent();
				else
				{
					_queue.Remove(pipeRequest);
					if (_queue.Count == 0)
						_queueSignal.Reset();
				}
			}
		}

		private void InitializeAsync()
		{
			CreateAsyncProcess();

			_asyncThread = new Thread(new ThreadStart(ProcessAsync));
			_asyncThread.IsBackground = true;
			_asyncThread.Start();
		}
		
		private void CreateAsyncProcess()
		{
			ProcessInfo processInfo = new ProcessInfo(_serverSession.SessionInfo);
			processInfo.DefaultIsolationLevel = DAE.IsolationLevel.Browse;
			if (!_stoppingAsync)
				_asyncProcess = _serverSession.StartProcess(processInfo);
			else
				_asyncProcess = null;
		}

		/// <summary> Cancels all queued requests. </summary>
		public void CancelAll()
		{
			lock (_asyncLock)
			{
				_queue.Clear();
				_queueSignal.Reset();
				if (_currentRequest != null)
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
			if (_asyncProcess != null)
			{
				_cancellingCurrent = true;
				ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncCancelCurrent), _asyncProcess.ProcessID);
				CreateAsyncProcess();
			}
		}

		private void AsyncCancelCurrent(object state)
		{
			try
			{
				// Stop the current process
				IServerProcess process = _serverSession.StartProcess(new ProcessInfo(_serverSession.SessionInfo));
				try
				{
					process.Execute("StopProcess(" + ((int)state).ToString() + ")", null);
				}
				finally
				{
					_serverSession.StopProcess(process);
				}
			}
			catch
			{
				// Do nothing, but catch all exceptions; the framework aborts the app if a thread leaves unhandled exceptions
			}
			finally
			{
				_cancellingCurrent = false;
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
					_queueSignal.WaitOne();

					// Check for shut-down
					if (_stoppingAsync)
						break;

					lock (_asyncLock)
					{
						// Dequeue an item
						if (_queue.Count > 0)
						{
							_currentRequest = _queue[0];
							_queue.RemoveAt(0);
							if (_queue.Count == 0)
								_queueSignal.Reset();
						}
					}
					try
					{
						try
						{
							// Make the request
							_currentRequest._result = InternalRequest(_currentRequest.Document, _asyncProcess);
						}
						catch (Exception exception)
						{
							// Error callback
							// Can't take a lock to ensure that FCancellingCurrent doesn't change before we invoke (might dead-lock on the SafelyInvoke back to the main thread... which might be waiting on the FAsyncLock)
							if ((_currentRequest.ErrorHandler != null) && !_cancellingCurrent)
								SafelyInvoke(_currentRequest.ErrorHandler, new object[] { _currentRequest, this, exception });
							// Don't rethrow
							continue; // Skip the success callback
						}

						// Success callback
						// Can't take a lock to ensure that FCancellingCurrent doesn't change before we invoke (might dead-lock on the SafelyInvoke back to the main thread... which might be waiting on the FAsyncLock)
						if ((_currentRequest.ResponseHandler != null) && !_cancellingCurrent)
							SafelyInvoke(_currentRequest.ResponseHandler, new object[] { _currentRequest, this });
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
						lock (_asyncLock)
						{
							try
							{
								try
								{
									// Free up the result resources
									if (_currentRequest._result != null)
										_currentRequest._result.Dispose();
								}
								finally
								{
									// Clear the current request
									_currentRequest = null;
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
				catch (Exception exception)
				{
					Error.Warn(exception.ToString());
					// Don't rethrow.  We do not want an error here to stop the worker thread (and terminate the application).
				}
			}
			_queueSignal.Close();
		}

		public event InvokeHandler OnSafelyInvoke;

		/// <summary> Attempts to execute the delegate on the main window thread. </summary>
		/// <remarks> If there is no main window, the delegate is invoked within this thread. </remarks>
		private object SafelyInvoke(Delegate delegateValue, object[] arguments)
		{
			if (OnSafelyInvoke != null)
				return OnSafelyInvoke(delegateValue, arguments);
			else
				return delegateValue.DynamicInvoke(arguments);
		}

		#endregion
	}
}
