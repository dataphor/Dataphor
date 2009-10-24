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

		public const string CClientName = "Silverlight";
		public const string CLibraryNodeTypesExpression = ".Frontend.GetLibraryNodeTypes('" + CClientName + "', ALibraryName)";

		public Session(Alphora.Dataphor.DAE.Client.DataSession ADataSession, bool AOwnsDataSession) 
			: base(ADataSession, AOwnsDataSession) 
		{
		}

		protected override void Dispose(bool ADisposing)
		{
			try
			{
				CloseAllForms(null, CloseBehavior.RejectOrClose);
				if (FRootFormHost != null)	// do this anyway because CloseAllForms may have failed
					FRootFormHost.Dispose();
			}
			finally
			{
				try
				{
					ClearImageCache();
				}
				finally
				{
					base.Dispose(ADisposing);
				}
			}
		}

		#region Applications & Libraries

		public string SetApplication(string AApplicationID)
		{
			return SetApplication(AApplicationID, CClientName);
		}

		private void ClearImageCache()
		{
			Pipe.ImageCache = null;
		}

		public override string SetApplication(string AApplicationID, string AClientType)
		{
			// Reset our current settings
			ClearImageCache();
			int LImageCacheSize = CDefaultImageCacheSize;

			// Optimistically load the settings
			// TODO: Load settings

			// Setup the image cache
			try
			{
				if (LImageCacheSize > 0)
					Pipe.ImageCache = new FixedSizeCache<string, byte[]>(LImageCacheSize);
			}
			catch (Exception LException)
			{
				HandleException(LException);	// Don't fail, just warn
			}

			return base.SetApplication(AApplicationID, AClientType);
		}

		#endregion

		#region Controls
		
		private SessionControl FSessionControl;
		
		protected virtual void CreateSessionControl()
		{
			FSessionControl = 
				(SessionControl)DispatchAndWait
				(
					(System.Func<SessionControl>)
					(
						() => 
						{ 
							var LControl = new SessionControl();
							InitializeSessionControl(LControl);
							return LControl;
						}
					)
				);
		}

		/// <summary> Prepares the session control. </summary>
		/// <remarks> This method is invoked on the main thread while the session thread is waiting. </remarks>
		protected virtual void InitializeSessionControl(SessionControl AControl)
		{
			// pure virtual
		}
		
		#endregion
		
		#region Forms
		
		public void Show(FormControl AForm, FormControl AParentForm)
		{
			if (AForm == null)
				throw new ArgumentNullException("AForm");
			
			Session.DispatcherInvoke
			(
				(System.Action)
				(
					() => 
					{
						FormStackControl LStack;
						if (AParentForm != null)
						{
							LStack = FSessionControl.FormStacks.Find(AParentForm);
							if (LStack == null)
								Error.Fail("The parent form for the form being shown is not a visible, top-level form.");
						}
						else
							LStack = FSessionControl.FormStacks.Create();
						LStack.FormStack.Push(AForm);
					}
				)
			);
		}

		public void DisposeFormHost(IHost AHost, bool AStack)
		{
			if (AStack)
			{
				if (AHost != null)
				{
					if (AHost.NextRequest != null)
						LoadNextForm(AHost).Show();
					else
						if (AHost == FRootFormHost)
						{
							SessionComplete();
							Dispose();
						}
						else
							AHost.Dispose();
				}
			}
			else
				if (AHost != null)
					AHost.Dispose();
		}	
			
		public void Close(FormControl AForm)
		{
			if (AForm == null)
				throw new ArgumentNullException("AForm");
			
			Session.DispatcherInvoke
			(
				(System.Action)
				(
					() => 
					{
						var LStack = FSessionControl.FormStacks.Find(AForm);
						if (LStack == null)
							Error.Fail("The form being closed is not a visible, top-level form.");
						LStack.FormStack.Pop();
						if (LStack.FormStack.IsEmpty)
							FSessionControl.FormStacks.Remove(LStack);
					}
				)
			);
		}
		
		#endregion
		
		#region Layout

		private static double FAverageCharacterWidth = 0d;
		
		public static double AverageCharacterWidth
		{
			get 
			{ 
				if (FAverageCharacterWidth == 0d)
				{
					FAverageCharacterWidth = (double)
						DispatchAndWait
						(
							(Func<double>)
							(
								() =>
								{
									var LTextBlock = new TextBlock();
									LTextBlock.Text = "abcdefghijklmnopqrstuvwzyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
									LTextBlock.Measure(new Size());
									return (double)(int)(LTextBlock.ActualWidth / LTextBlock.Text.Length);
								}
							)
						);
				}
				return FAverageCharacterWidth;
			}
		}
		
		#endregion
		
		#region Execution

		public event EventHandler OnComplete;

		private IHost FRootFormHost;
		private ContentControl FContainer;

		/// <remarks> Must call SetApplication or SetLibrary before calling Start().  Upon completion, the given 
		/// callback will be invoked and the session will be disposed.  Note that the session may leave its
		/// control in place and it is up to the caller to replace the content of the given container. </remarks>
		public void Start(string ADocument, ContentControl AContainer)
		{
			TimingUtility.PushTimer(String.Format("Silverlight.Session.Start('{0}')", ADocument));
			try
			{
				// Create the session control
				CreateSessionControl();

				// Prepare the root form's host
				FRootFormHost = CreateHost();
				try
				{
					FRootFormHost.NextRequest = new Request(ADocument);
					FContainer = AContainer;
					LoadNextForm(FRootFormHost).Show();
				}
				catch
				{
					if (FRootFormHost != null)
					{
						FRootFormHost.Dispose();
						FRootFormHost = null;
					}
					throw;
				}
				
				// Show the session control as the content of the given container
				DispatchAndWait((System.Action)(() => { if (FSessionControl != null) AContainer.Content = FSessionControl; }));
			}
			finally
			{
				TimingUtility.PopTimer();
			}
		}

		private ISilverlightFormInterface LoadNextForm(IHost AHost)
		{
			var LForm = (ISilverlightFormInterface)CreateForm();
			try
			{
				AHost.LoadNext(LForm);
				AHost.Open();
				return LForm;
			}
			catch
			{
				LForm.Dispose();
				LForm = null;
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
			Host LHost = new Host(this);
			LHost.OnDeserializationErrors += new DeserializationErrorsHandler(ReportErrors);
			return LHost;
		}
		
		#endregion
		
		#region Error handling

		public event DeserializationErrorsHandler OnErrors;

		public override void ReportErrors(IHost AHost, ErrorList AErrorList)
		{
			if (OnErrors != null)
				OnErrors(AHost, AErrorList);

			if ((AHost != null) && (AHost.Children.Count > 0))
			{
				IFormInterface LFormInterface = AHost.Children[0] as IFormInterface;
				if (LFormInterface == null)
					LFormInterface = (IFormInterface)AHost.Children[0].FindParent(typeof(IFormInterface));
				if (LFormInterface != null)
					LFormInterface.EmbedErrors(AErrorList);
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

		public void HandleException(Exception AException)
		{
			ReportErrors(null, new ErrorList() { AException });
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
		
		private static Dispatcher FDispatcher;
		
		/// <summary> Gets or sets the dispatcher used to synchronize onto the main UI thread. </summary>
		public static Dispatcher Dispatcher 
		{ 
			get
			{
				if (FDispatcher == null)
					FDispatcher = Deployment.Current.Dispatcher;
				return FDispatcher;
			}
			set { FDispatcher = value; }
		}

		/// <summary> Returns the verified non-null main UI thread dispatcher. </summary>
		public static Dispatcher CheckedDispatcher
		{
			get
			{
				Dispatcher LDispatcher = Dispatcher;	// Capture locally for thread safety
				if (LDispatcher == null)
					throw new SilverlightClientException(SilverlightClientException.Codes.MissingDispatcher);
				return LDispatcher;
			}
		}

		/// <summary> Executes a delegate in the context of the main thread. </summary>
		public static void DispatcherInvoke(Delegate ADelegate, params object[] AArguments)
		{
			CheckedDispatcher.BeginInvoke(ADelegate, AArguments);
		}
		
		/// <summary> Synchronously executes the given delegate on the main thread and returns the result. </summary>
		public static object DispatchAndWait(Delegate ADelegate, params object[] AArguments)
		{
			object LResult = null;
			Exception LException = null;
			var LEvent = new ManualResetEvent(false);
			CheckedDispatcher.BeginInvoke
			(
				() => 
				{ 
					try
					{
						LResult = ADelegate.DynamicInvoke(AArguments);
					}
					catch (Exception LError)
					{
						LException = LError;
					}
					finally
					{
						LEvent.Set();
					}
				}
			);
			LEvent.WaitOne();
			LEvent.Close();
			if (LException != null)
				throw LException;
			return LResult;
		}

		public static Queue<System.Action> FSessionQueue = new Queue<System.Action>();
		public static Thread FSessionThread;
		
		public static void QueueSessionAction(System.Action AAction)
		{
			if (AAction != null)
			{
				var LExecuteSync = false;
				
				lock (FSessionQueue)
				{
					if (FSessionThread == null)
					{
						FSessionThread = new Thread(new ThreadStart(ActionQueueServiceThread));
						FSessionThread.Start();
					}
					else
						LExecuteSync = (FSessionThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId);
					
					if (!LExecuteSync)
						FSessionQueue.Enqueue(AAction);
				}
			
				if (LExecuteSync)
					AAction();
			}
		}
		
		private static void ActionQueueServiceThread()
		{
			while (true)
			{
				System.Action LNextAction;
				lock (FSessionQueue)
				{
					if (FSessionQueue.Count > 0)
						LNextAction = FSessionQueue.Dequeue();
					else
					{
						FSessionThread = null;
						break;
					}
				}
				try
				{
					LNextAction();
				}
				catch  (Exception LException)
				{
					System.Diagnostics.Debug.WriteLine(LException.ToString());
					// Don't allow exceptions to leave this thread or the application will terminate
				}
			}
		}
		
		/// <summary> Synchronously executes the given delegate on the session thread and returns the result. </summary>
		/// <remarks> This method will throw an exception if invoked from the main thread. </remarks>
		public static object InvokeAndWait(Delegate ADelegate, params object[] AArguments)
		{
			// Ensure that this method isn't called from the main thread
			var LDispatcher = Silverlight.Session.Dispatcher;
			Error.DebugAssertFail(LDispatcher == null || !LDispatcher.CheckAccess(), "InvokeAndWait may not be called from the main thread.");
				
			object LResult = null;
			Exception LException = null;
			var LEvent = new ManualResetEvent(false);
			QueueSessionAction
			(
				() => 
				{ 
					try
					{
						LResult = ADelegate.DynamicInvoke(AArguments);
					}
					catch (Exception LError)
					{
						LException = LError;
					}
					finally
					{
						LEvent.Set();
					}
				}
			);
			LEvent.WaitOne();
			LEvent.Close();
			if (LException != null)
				throw LException;
			return LResult;
		}

		/// <summary> Executes the given delegate on the session thread. </summary>
		public static void Invoke(Delegate ADelegate, params object[] AArguments)
		{
			Dispatcher LDispatcher = CheckedDispatcher;	
			QueueSessionAction
			(
				() =>
				{
					try
					{
						ADelegate.DynamicInvoke(AArguments);
					}
					catch (Exception LException)
					{
						System.Diagnostics.Debug.WriteLine("Unhandled exception: ", LException);
					}
				}
			);
		}

		/// <summary> Executes the given delegate on the session thread, then calls back to the main thread for error or completion. </summary>
		public static void Invoke(Delegate ADelegate, ErrorHandler AOnError, System.Action AOnCompletion, params object[] AArguments)
		{
			Dispatcher LDispatcher = CheckedDispatcher;	
			QueueSessionAction
			(
				() =>
				{
					try
					{
						ADelegate.DynamicInvoke(AArguments);
						if (AOnCompletion != null)
							LDispatcher.BeginInvoke(AOnCompletion);
					}
					catch (Exception LException)
					{
						if (AOnError != null)
							LDispatcher.BeginInvoke(AOnError, LException is TargetInvocationException ? ((TargetInvocationException)LException).InnerException : LException);
						else
							System.Diagnostics.Debug.WriteLine("Unhandled exception: ", LException);
					}
				}
			);
		}

		/// <summary> Executes the given delegate on the session thread, then calls back to the main thread for error or completion. </summary>
		public static void Invoke<T>(Func<T> ADelegate, ErrorHandler AOnError, System.Action<T> AOnCompletion, params object[] AArguments)
		{
		    Dispatcher LDispatcher = CheckedDispatcher;	
		    QueueSessionAction
		    (
		        () =>
		        {
		            try
		            {
		                var LResult = (T)ADelegate.DynamicInvoke(AArguments);
		                if (AOnCompletion != null)
							LDispatcher.BeginInvoke(AOnCompletion, LResult);
		            }
		            catch (Exception LException)
		            {
		                if (AOnError != null)
		                    LDispatcher.BeginInvoke(AOnError, LException is TargetInvocationException ? ((TargetInvocationException)LException).InnerException : LException);
		                else
		                    System.Diagnostics.Debug.WriteLine("Unhandled exception: ", LException);
		            }
		        }
		    );
		}
		
		#endregion
	}
	
	public delegate void ErrorHandler(Exception AException);
	
	public static class SessionExtensions
	{
		public static void HandleException(this Frontend.Client.Session ASession, Exception AException)
		{
			((Silverlight.Session)ASession).HandleException(AException);
		}
		
		public static void HandleException(this Frontend.Client.Node ANode, Exception AException)
		{
			((Silverlight.Session)ANode.HostNode.Session).HandleException(AException);
		}
	}
}
