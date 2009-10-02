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

		#region Execution

		private IHost FRootFormHost;
		private EventHandler FOnComplete;
		private ContentControl FContainer;

		/// <remarks> Must call SetApplication or SetLibrary before calling Start().  Upon completion, the given callback will be invoked and the session will be disposed. </remarks>
		public Control Start(string ADocument, EventHandler AOnComplete, ContentControl AContainer)
		{
			TimingUtility.PushTimer(String.Format("Silverlight.Session.Start('{0}')", ADocument));
			try
			{
				// Prepare the root forms host
				FRootFormHost = CreateHost();
				try
				{
					FRootFormHost.NextRequest = new Request(ADocument);
					FOnComplete = AOnComplete;
					FContainer = AContainer;
					RootFormAdvance(null);
					return ((ISilverlightFormInterface)FRootFormHost.Children[0]).Control;
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
				LForm.SupressCloseButton = true;
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

		private void RootFormAdvance(IFormInterface AForm)
		{
			if (FRootFormHost.NextRequest != null)
				LoadNextForm(FRootFormHost).Show(new FormInterfaceHandler(RootFormAdvance), FContainer);
			else
			{
				if (FOnComplete != null)
				{
					FOnComplete(this, EventArgs.Empty);
					FOnComplete = null;
				}
				Dispose();
			}
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
			Pipe.OnSafelyInvoke += new InvokeHandler(SafelyInvoke);
		}

		protected override void UninitializePipe()
		{
			if (Pipe != null)
				Pipe.OnSafelyInvoke -= new InvokeHandler(SafelyInvoke);
			base.UninitializePipe();
		}

		public void HandleException(Exception AException)
		{
			ReportErrors(null, new ErrorList() { AException });
		}
		
		#endregion

		#region Dispatcher

		public static Dispatcher FDispatcher;

		/// <summary> Executes a delegate in the context of the main thread. </summary>
		public static object SafelyInvoke(Delegate ADelegate, object[] AArguments)
		{
			Dispatcher LDispatcher = FDispatcher;
			if (LDispatcher != null)
				return LDispatcher.BeginInvoke(ADelegate, AArguments);
			else
				throw new SilverlightClientException(SilverlightClientException.Codes.MissingDispatcher);
		}

		#endregion
	}
}
