/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;

namespace Alphora.Dataphor.Frontend.Client.Silverlight
{
	/// <summary> Session represents the client session with the application server. </summary>
	/// <remarks> Most of the methods in this class make synchronous calls to the Dataphor server 
	/// and must therefore be invoked on a thread other than the main. </remarks>
	public class Session : Frontend.Client.Session
	{
		public static int CDefaultDocumentCacheSize = 800;
		public static int CDefaultImageCacheSize = 60;

		public const string ClientName = "Silverlight";
		public const string LibraryNodeTypesExpression = ".Frontend.GetLibraryNodeTypes('" + ClientName + "', ALibraryName)";

		public Session(Alphora.Dataphor.DAE.Client.DataSession dataSession, bool ownsDataSession) 
			: base(dataSession, ownsDataSession) 
		{
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				CloseAllForms(null, CloseBehavior.RejectOrClose);
				if (_rootFormHost != null)	// do this anyway because CloseAllForms may have failed
					_rootFormHost.Dispose();
			}
			finally
			{
				try
				{
					ClearImageCache();
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		#region Applications & Libraries

		public string SetApplication(string applicationID)
		{
			return SetApplication(applicationID, ClientName);
		}

		private void ClearImageCache()
		{
			Pipe.ImageCache = null;
		}

		public override string SetApplication(string applicationID, string clientType)
		{
			// Reset our current settings
			ClearImageCache();
			int imageCacheSize = CDefaultImageCacheSize;

			// Optimistically load the settings
			// TODO: Load settings

			// Setup the image cache
			try
			{
				if (imageCacheSize > 0)
					Pipe.ImageCache = new FixedSizeCache<string, byte[]>(imageCacheSize);
			}
			catch (Exception exception)
			{
				HandleException(exception);	// Don't fail, just warn
			}

			return base.SetApplication(applicationID, clientType);
		}

		#endregion

		#region Controls
		
		private SessionControl _sessionControl;
		
		protected virtual void CreateSessionControl()
		{
			_sessionControl = 
				(SessionControl)DispatchAndWait
				(
					(System.Func<SessionControl>)
					(
						() => 
						{ 
							var control = new SessionControl();
							InitializeSessionControl(control);
							return control;
						}
					)
				);
		}

		/// <summary> Prepares the session control. </summary>
		/// <remarks> This method is invoked on the main thread while the session thread is waiting. </remarks>
		protected virtual void InitializeSessionControl(SessionControl control)
		{
			// pure virtual
		}
		
		#endregion
		
		#region Forms
		
		public void Show(FormControl form, FormControl parentForm)
		{
			if (form == null)
				throw new ArgumentNullException("AForm");
			
			Session.DispatcherInvoke
			(
				(System.Action)
				(
					() => 
					{
						FormStackControl stack;
						if (parentForm != null)
						{
							stack = _sessionControl.FormStacks.Find(parentForm);
							if (stack == null)
								Error.Fail("The parent form for the form being shown is not a visible, top-level form.");
						}
						else
							stack = _sessionControl.FormStacks.Create();
						stack.FormStack.Push(form);
					}
				)
			);
		}

		public void DisposeFormHost(IHost host, bool stack)
		{
			if (stack)
			{
				if (host != null)
				{
					if (host.NextRequest != null)
						LoadNextForm(host).Show();
					else
						if (host == _rootFormHost)
						{
							SessionComplete();
							Dispose();
						}
						else
							host.Dispose();
				}
			}
			else
				if (host != null)
					host.Dispose();
		}	
			
		public void Close(FormControl form)
		{
			if (form == null)
				throw new ArgumentNullException("AForm");
			
			Session.DispatcherInvoke
			(
				(System.Action)
				(
					() => 
					{
						var stack = _sessionControl.FormStacks.Find(form);
						if (stack == null)
							Error.Fail("The form being closed is not a visible, top-level form.");
						stack.FormStack.Pop();
						if (stack.FormStack.IsEmpty)
							_sessionControl.FormStacks.Remove(stack);
					}
				)
			);
		}
		
		#endregion
		
		#region Layout

		private static double _averageCharacterWidth = 0d;
		
		public static double AverageCharacterWidth
		{
			get 
			{ 
				if (_averageCharacterWidth == 0d)
				{
					_averageCharacterWidth = (double)
						DispatchAndWait
						(
							(Func<double>)
							(
								() =>
								{
									var textBlock = new TextBlock();
									textBlock.Text = "abcdefghijklmnopqrstuvwzyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
									textBlock.Measure(new Size());
									return (double)(int)(textBlock.ActualWidth / textBlock.Text.Length);
								}
							)
						);
				}
				return _averageCharacterWidth;
			}
		}
		
		#endregion
		
		#region Execution

		public event EventHandler OnComplete;

		private IHost _rootFormHost;
		private ContentControl _container;

		/// <remarks> Must call SetApplication or SetLibrary before calling Start().  Upon completion, the given 
		/// callback will be invoked and the session will be disposed.  Note that the session may leave its
		/// control in place and it is up to the caller to replace the content of the given container. </remarks>
		public void Start(string document, ContentControl container)
		{
			TimingUtility.PushTimer(String.Format("Silverlight.Session.Start('{0}')", document));
			try
			{
				// Create the session control
				CreateSessionControl();

				// Prepare the root form's host
				_rootFormHost = CreateHost();
				try
				{
					_rootFormHost.NextRequest = new Request(document);
					_container = container;
					LoadNextForm(_rootFormHost).Show();
				}
				catch
				{
					if (_rootFormHost != null)
					{
						_rootFormHost.Dispose();
						_rootFormHost = null;
					}
					throw;
				}
				
				// Show the session control as the content of the given container
				DispatchAndWait((System.Action)(() => { if (_sessionControl != null) container.Content = _sessionControl; }));
			}
			finally
			{
				TimingUtility.PopTimer();
			}
		}

		private ISilverlightFormInterface LoadNextForm(IHost host)
		{
			var form = (ISilverlightFormInterface)CreateForm();
			try
			{
				host.LoadNext(form);
				host.Open();
				return form;
			}
			catch
			{
				form.Dispose();
				form = null;
				throw;
			}
		}

		protected virtual void SessionComplete()
		{
			if (OnComplete != null)
				OnComplete(this, EventArgs.Empty);
		}

		#endregion

		#region Client.Session

		public override Client.IHost CreateHost()
		{
			Host host = new Host(this);
			host.OnDeserializationErrors += new DeserializationErrorsHandler(ReportErrors);
			return host;
		}
		
		#endregion
		
		#region Error handling

		public event DeserializationErrorsHandler OnErrors;

		public override void ReportErrors(IHost host, ErrorList errorList)
		{
			if (OnErrors != null)
				OnErrors(host, errorList);

			if ((host != null) && (host.Children.Count > 0))
			{
				IFormInterface formInterface = host.Children[0] as IFormInterface;
				if (formInterface == null)
					formInterface = (IFormInterface)host.Children[0].FindParent(typeof(IFormInterface));
				if (formInterface != null)
					formInterface.EmbedErrors(errorList);
			}
		}

		protected override void InitializePipe()
		{
			base.InitializePipe();
			Pipe.OnSafelyInvoke += new InvokeHandler(InvokeAndWait);
		}

		protected override void UninitializePipe()
		{
			if (Pipe != null)
				Pipe.OnSafelyInvoke -= new InvokeHandler(InvokeAndWait);
			base.UninitializePipe();
		}

		public void HandleException(Exception exception)
		{
			ReportErrors(null, new ErrorList() { exception });
		}
		
		#endregion

		#region Threading
		
		/*
			Threading strategy:
				All threads in the SL client funnel into one of two threads: 
					1. The session thread - all node and communication logic happens here.
					2. The main (UI) thread - user interface related activity.
				The main thread must never wait for (be blocked by) the session thread, or
				the entire application may freeze.  This is because in general this is a 
				dead-lock state.  In order for communications activity to complete, the 
				message pump facilitated by the main thread must progress.  The reciprical
				wait is acceptable however; the session thread may wait on the main thread.
				
				The implementation if this strategy is provided through static dispatch 
				(main thread) and session invocation methods.  The session thread is a 
				virtual thread, not a dedicated thread.  A new thread is started to service 
				the session action queue as items are queued.  Once no items remain to be 
				queued, the temporary session thread terminates.  In keeping with the 
				strategy, the main thread will error if it ever calls InvokeAndWait on the 
				session thread.  There is, however, a DispatchAndWait method so that the
				session thread may wait on the main thread.  This is useful and important 
				when performing construction of new UI elements, which must be performed in 
				the main thread, but where the instance pointers are needed right away in
				the session thread in order to properly synchronize.
		*/
		
		private static Dispatcher _dispatcher;
		
		/// <summary> Gets or sets the dispatcher used to synchronize onto the main UI thread. </summary>
		public static Dispatcher Dispatcher 
		{ 
			get
			{
				if (_dispatcher == null)
					_dispatcher = Deployment.Current.Dispatcher;
				return _dispatcher;
			}
			set { _dispatcher = value; }
		}

		/// <summary> Returns the verified non-null main UI thread dispatcher. </summary>
		public static Dispatcher CheckedDispatcher
		{
			get
			{
				Dispatcher dispatcher = Dispatcher;	// Capture locally for thread safety
				if (dispatcher == null)
					throw new SilverlightClientException(SilverlightClientException.Codes.MissingDispatcher);
				return dispatcher;
			}
		}

		/// <summary> Executes a delegate in the context of the main thread. </summary>
		public static void DispatcherInvoke(Delegate delegateValue, params object[] arguments)
		{
			CheckedDispatcher.BeginInvoke(delegateValue, arguments);
		}
		
		/// <summary> Synchronously executes the given delegate on the main thread and returns the result. </summary>
		public static object DispatchAndWait(Delegate delegateValue, params object[] arguments)
		{
			object result = null;
			Exception exception = null;
			var eventValue = new ManualResetEvent(false);
			CheckedDispatcher.BeginInvoke
			(
				() => 
				{ 
					try
					{
						result = delegateValue.DynamicInvoke(arguments);
					}
					catch (Exception error)
					{
						exception = error;
					}
					finally
					{
						eventValue.Set();
					}
				}
			);
			eventValue.WaitOne();
			eventValue.Close();
			if (exception != null)
				throw exception;
			return result;
		}

		public static Queue<System.Action> _sessionQueue = new Queue<System.Action>();
		public static Thread _sessionThread;
		
		public static void QueueSessionAction(System.Action action)
		{
			if (action != null)
			{
				var executeSync = false;
				
				lock (_sessionQueue)
				{
					if (_sessionThread == null)
					{
						_sessionThread = new Thread(new ThreadStart(ActionQueueServiceThread));
						_sessionThread.Start();
					}
					else
						executeSync = (_sessionThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId);
					
					if (!executeSync)
						_sessionQueue.Enqueue(action);
				}
			
				if (executeSync)
					action();
			}
		}
		
		private static void ActionQueueServiceThread()
		{
			while (true)
			{
				System.Action nextAction;
				lock (_sessionQueue)
				{
					if (_sessionQueue.Count > 0)
						nextAction = _sessionQueue.Dequeue();
					else
					{
						_sessionThread = null;
						break;
					}
				}
				try
				{
					nextAction();
				}
				catch  (Exception exception)
				{
					System.Diagnostics.Debug.WriteLine(exception.ToString());
					// Don't allow exceptions to leave this thread or the application will terminate
				}
			}
		}
		
		/// <summary> Synchronously executes the given delegate on the session thread and returns the result. </summary>
		/// <remarks> This method will throw an exception if invoked from the main thread. </remarks>
		public static object InvokeAndWait(Delegate delegateValue, params object[] arguments)
		{
			// Ensure that this method isn't called from the main thread
			var dispatcher = Silverlight.Session.Dispatcher;
			Error.DebugAssertFail(dispatcher == null || !dispatcher.CheckAccess(), "InvokeAndWait may not be called from the main thread.");
				
			object result = null;
			Exception exception = null;
			var eventValue = new ManualResetEvent(false);
			QueueSessionAction
			(
				() => 
				{ 
					try
					{
						result = delegateValue.DynamicInvoke(arguments);
					}
					catch (Exception error)
					{
						exception = error;
					}
					finally
					{
						eventValue.Set();
					}
				}
			);
			eventValue.WaitOne();
			eventValue.Close();
			if (exception != null)
				throw exception;
			return result;
		}

		/// <summary> Executes the given delegate on the session thread. </summary>
		public static void Invoke(Delegate delegateValue, params object[] arguments)
		{
			Dispatcher dispatcher = CheckedDispatcher;	
			QueueSessionAction
			(
				() =>
				{
					try
					{
						delegateValue.DynamicInvoke(arguments);
					}
					catch (Exception exception)
					{
						System.Diagnostics.Debug.WriteLine("Unhandled exception: ", exception);
					}
				}
			);
		}

		/// <summary> Executes the given delegate on the session thread, then calls back to the main thread for error or completion. </summary>
		public static void Invoke(Delegate delegateValue, ErrorHandler onError, System.Action onCompletion, params object[] arguments)
		{
			Dispatcher dispatcher = CheckedDispatcher;	
			QueueSessionAction
			(
				() =>
				{
					try
					{
						delegateValue.DynamicInvoke(arguments);
						if (onCompletion != null)
							dispatcher.BeginInvoke(onCompletion);
					}
					catch (Exception exception)
					{
						if (onError != null)
							dispatcher.BeginInvoke(onError, exception is TargetInvocationException ? ((TargetInvocationException)exception).InnerException : exception);
						else
							System.Diagnostics.Debug.WriteLine("Unhandled exception: ", exception);
					}
				}
			);
		}

		/// <summary> Executes the given delegate on the session thread, then calls back to the main thread for error or completion. </summary>
		public static void Invoke<T>(Func<T> delegateValue, ErrorHandler onError, System.Action<T> onCompletion, params object[] arguments)
		{
		    Dispatcher dispatcher = CheckedDispatcher;	
		    QueueSessionAction
		    (
		        () =>
		        {
		            try
		            {
		                var result = (T)delegateValue.DynamicInvoke(arguments);
		                if (onCompletion != null)
							dispatcher.BeginInvoke(onCompletion, result);
		            }
		            catch (Exception exception)
		            {
		                if (onError != null)
		                    dispatcher.BeginInvoke(onError, exception is TargetInvocationException ? ((TargetInvocationException)exception).InnerException : exception);
		                else
		                    System.Diagnostics.Debug.WriteLine("Unhandled exception: ", exception);
		            }
		        }
		    );
		}
		
		#endregion
	}
	
	public delegate void ErrorHandler(Exception AException);
	
	public static class SessionExtensions
	{
		public static void HandleException(this Frontend.Client.Session session, Exception exception)
		{
			((Silverlight.Session)session).HandleException(exception);
		}
		
		public static void HandleException(this Frontend.Client.Node node, Exception exception)
		{
			((Silverlight.Session)node.HostNode.Session).HandleException(exception);
		}
	}
}
