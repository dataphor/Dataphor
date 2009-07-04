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
using Alphora.Dataphor.Logging;
using WinForms = System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Session represents the client session with the application server. </summary>
	public class Session : Frontend.Client.Session
	{
		public static string CCachePath = @"Dataphor\WindowsClientCache\";
		public static string CHelpPath = @"Help";
		public static int CDefaultDocumentCacheSize = 800;
		public static int CDefaultImageCacheSize = 60;

		public const string CFormDesignerNodeTypesExpression = @".Frontend.GetNodeTypes('Windows', Frontend.FormDesignerLibraries)";
		public const string CLibraryNodeTypesExpression = @".Frontend.GetLibraryNodeTypes('Windows', ALibraryName)";
		public const string CSettingsExpression = @".Frontend.GetWindowsSettings(AApplicationID)";

        static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof(Session));

		public Session(Alphora.Dataphor.DAE.Client.DataSession ADataSession, bool AOwnsDataSession) : base(ADataSession, AOwnsDataSession) 
		{
			FToolTip = new WinForms.ToolTip();
			try
			{
				FToolTip.Active = true;
			}
			catch
			{
				FToolTip.Dispose();
				FToolTip = null;
				throw;
			}

			// Ensure we are setup for SafelyInvoke.  This must happen on the main windows thread and thus is not in a static constructor.
			lock (FInvokeControlLock)
			{
				if (FInvokeControl == null)
				{
					FInvokeControl = new WinForms.Control();
					FInvokeControl.CreateControl();
				}
			}
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
					if (FToolTip != null)
					{
						FToolTip.Dispose();
						FToolTip = null;
					}
				}
				finally
				{
					try
					{
						DisposeDefaultIcon();
					}
					finally
					{
						try
						{
							ClearDocumentCache();	// this must happen before the call to base (because we need the pipe)
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
				}
			}
		}

		// ToolTip

		private WinForms.ToolTip FToolTip;
		
		public WinForms.ToolTip ToolTip
		{
			get { return FToolTip; }
		}

		// Theme

		private Theme FTheme = new Theme();

		public Theme Theme
		{
			get
			{
				if (FTheme != null)
					return FTheme;
				else
					return new Theme();
			}
		}

		// DefaultIcon

		private Icon FDefaultIcon;
		public Icon DefaultIcon { get { return FDefaultIcon; } }

		private void DisposeDefaultIcon()
		{
			if (FDefaultIcon != null)
			{
				FDefaultIcon.Dispose();
				FDefaultIcon = null;
			}
		}
        
		// AfterFormActivate

		public event FormInterfaceHandler AfterFormActivate;

		public void DoAfterFormActivate(IFormInterface AForm)
		{
			if (AfterFormActivate != null)
				AfterFormActivate(AForm);
		}

		#region HelpProvider

		public bool IsContextHelpAvailable()
		{
			return ((FHelpLoader != null) || (FHelpFileName != String.Empty));
		}

		private string FHelpFileName = String.Empty;
		/// <summary> The file to use when help is requested on controls. </summary>
		public string HelpFileName
		{
			get { return FHelpFileName; }
			set { FHelpFileName = (value == null ? String.Empty : value); }
		}

		private Dictionary<WinForms.Control,IElement> FHelpControls = new Dictionary<System.Windows.Forms.Control,IElement>();
		private FileLoader FHelpLoader;

		public void RegisterControlHelp(WinForms.Control AControl, IElement AElement)
		{
			if (!FHelpControls.ContainsKey(AControl))
			{
				AControl.HelpRequested += new System.Windows.Forms.HelpEventHandler(ControlHelpRequested);
				AControl.Disposed += new EventHandler(HelpControlDisposed);
				FHelpControls.Add(AControl, AElement);
			}
		}

		public void UnregisterControlHelp(WinForms.Control AControl)
		{
			if (FHelpControls.ContainsKey(AControl))
			{
				AControl.HelpRequested -= new System.Windows.Forms.HelpEventHandler(ControlHelpRequested);
				AControl.Disposed -= new EventHandler(HelpControlDisposed);
				FHelpControls.Remove(AControl);
			}
		}

		private bool InternalInvokeHelp(WinForms.Control AControl, string AHelpKeyword, HelpKeywordBehavior AHelpKeywordBehavior, string AHelpString, bool APreferPopup)
		{
			WinForms.HelpNavigator LNavigator = (WinForms.HelpNavigator)AHelpKeywordBehavior;
			if 
			(
				(APreferPopup || (AHelpKeyword == String.Empty)) 
					&& (AHelpString != String.Empty)
			)
				WinForms.Help.ShowPopup(AControl, AHelpString, WinForms.Control.MousePosition);
			else
			{
				if (FHelpLoader != null)
					System.Media.SystemSounds.Beep.Play();
				else
				{
					if (FHelpFileName != String.Empty)
					{
						if (AHelpKeyword != String.Empty)
							WinForms.Help.ShowHelp(AControl, FHelpFileName, LNavigator, AHelpKeyword);
						else
							WinForms.Help.ShowHelp(AControl, FHelpFileName, LNavigator);
					}
					else
						return false;
				}
			}
			return true;
		}

		public override void InvokeHelp(INode ASender, string AHelpKeyword, HelpKeywordBehavior AHelpKeywordBehavior, string AHelpString)
		{
			WinForms.Control LControl = null;
			if (ASender != null)
			{
				IWindowsControlElement LElement = ASender as IWindowsControlElement;
				if (LElement != null)
					LControl = LElement.Control;
				else
					LControl = (WinForms.Control)((IWindowsFormInterface)ASender.FindParent(typeof(IWindowsFormInterface))).Form;
			}

			InternalInvokeHelp(LControl, AHelpKeyword, AHelpKeywordBehavior, AHelpString, false);
		}

		private void ControlHelpRequested(object ASender, System.Windows.Forms.HelpEventArgs AArgs)
		{
			AArgs.Handled |= FindAndInvokeHelp((WinForms.Control)ASender);
		}

		private bool FindAndInvokeHelp(WinForms.Control AControl)
		{
			IElement LElement;
			if (FHelpControls.TryGetValue(AControl, out LElement))
			{
				while ((LElement != null) && (LElement.HelpKeyword == "") && (LElement.HelpString == "") && (LElement.Parent != null) && (LElement.Parent is IElement))
					LElement = (IElement)LElement.Parent;
				string LKeyword = (LElement != null) ? LElement.HelpKeyword : "";
				HelpKeywordBehavior LBehavior = (LElement != null) ? LElement.HelpKeywordBehavior : HelpKeywordBehavior.KeywordIndex;
				string LString = (LElement != null) ? LElement.HelpString : "";
				return InternalInvokeHelp(AControl, LKeyword, LBehavior, LString, (WinForms.Control.MouseButtons != WinForms.MouseButtons.None));
			}
			else
				return (AControl.Parent != null) && FindAndInvokeHelp(AControl.Parent);
		}

		private void HelpControlDisposed(object ASender, EventArgs AArgs)
		{
			UnregisterControlHelp((WinForms.Control)ASender);
		}

		private string GetHelpFileName(string ADocument)
		{
			DocumentExpression LExpression = new DocumentExpression(ADocument);
			if (LExpression.Type == DocumentType.Document)
			{
				return LExpression.DocumentArgs.LibraryName + "." + LExpression.DocumentArgs.DocumentName + "." 
					+ 
					(
						Evaluate
						(
							String.Format
							(
								".Frontend.GetDocumentType('{0}', '{1}')", 
								LExpression.DocumentArgs.LibraryName, 
								LExpression.DocumentArgs.DocumentName
							)
						).AsString
					);
			}
			else
				return Path.ChangeExtension(ADocument.GetHashCode().ToString(), ".chm");
		}

		/// <summary> Loads (to the client) the specified help document and sets <see cref="HelpFileName"/> to the local file name. </summary>
		public void LoadHelpDocument(string ADocument)
		{
			FHelpLoader = 
				new FileLoader
				(
					DataSession.ServerSession, 
					ADocument, 
					Path.Combine
					(
						Path.Combine
						(
							AppDomain.CurrentDomain.BaseDirectory, 
							CHelpPath
						), 
						GetHelpFileName(ADocument)
					), 
					new FileLoaderCompleteHandler(HelpFileLoaded)
				);
		}

		private void HelpFileLoaded(FileLoader ALoader)
		{
			FHelpFileName = ALoader.FileName;
			FHelpLoader = null;
		}

		#endregion

		#region Applications & Libraries

		public string SetApplication(string AApplicationID)
		{
			return SetApplication(AApplicationID, "Windows");
		}

		private void ClearDocumentCache()
		{
			if (Pipe.Cache != null)
			{
				Pipe.Cache.Dispose();
				Pipe.Cache = null;
			}
		}

		private void ClearImageCache()
		{
			Pipe.ImageCache = null;
		}

		public override string SetApplication(string AApplicationID, string AClientType)
		{
			// Reset our current settings
			FTheme = null;
			DisposeDefaultIcon();
			ClearDocumentCache();
			ClearImageCache();
			int LDocumentCacheSize = CDefaultDocumentCacheSize;
			int LImageCacheSize = CDefaultImageCacheSize;

			// Optimistically load the settings
			try
			{
				DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
				LParams.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AApplicationID", AApplicationID));

				using (DAE.Runtime.Data.Row LRow = (DAE.Runtime.Data.Row)Evaluate(Pipe.Process, CSettingsExpression, LParams))
				{
					if (LRow != null)
					{
						// Load the theme
						if (LRow.HasValue("Theme"))
							FTheme = (Theme)new BOP.Deserializer().Deserialize(LRow["Theme"].AsString, null);

						// Load the default form icon
						if (LRow.HasValue("IconImage"))
							using (Stream LIconStream = LRow["IconImage"].OpenStream())
							{
								Bitmap LBitmap = System.Drawing.Image.FromStream(LIconStream) as Bitmap;
								if (LBitmap != null)
									FDefaultIcon = Icon.FromHandle(LBitmap.GetHicon());	// TODO: Should this bitmap be disposed after this?
							}

						// Load the document cache size
						if (LRow.HasValue("DocumentCacheSize"))
							LDocumentCacheSize = LRow["DocumentCacheSize"].AsInt32;

						// Load the image cache size
						if (LRow.HasValue("ImageCacheSize"))
							LImageCacheSize = LRow["ImageCacheSize"].AsInt32;

						// Load the help file
						if (LRow.HasValue("HelpDocument"))
						{
							string LDocument = LRow["HelpDocument"].AsString;
							if (LDocument != String.Empty)
								LoadHelpDocument(LDocument);
						}
					}
				}
			}
			catch (Exception LException)
			{
				HandleException(new ClientException(ClientException.Codes.ErrorLoadingSettings, LException));
			}
			finally
			{
				if (FTheme == null)
					FTheme = new Theme();
			}

			// Setup the image cache
			try
			{
				if (LImageCacheSize > 0)
					Pipe.ImageCache = new ImageCache(LImageCacheSize);
			}
			catch (Exception LException)
			{
				HandleException(LException);	// Don't fail, just warn
			}

			// Set up the client-side document cache
			try
			{
				if (LDocumentCacheSize > 0)
					Pipe.Cache = 
						new DocumentCache
						(
							Path.Combine
							(
								Path.Combine(System.IO.Path.GetTempPath(), CCachePath),
								@"App" + AApplicationID.ToString()
							), 
							LDocumentCacheSize
						);
			}
			catch (Exception LException)
			{
				#if DEBUG
				HandleException(LException);	// Don't fail if we can't do this and only show something if under debug
				#endif
			}

			return base.SetApplication(AApplicationID, AClientType);
		}

		public void SetLibrary(string ALibraryName)
		{
			DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
			LParams.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "ALibraryName", ALibraryName));
			using 
			(
				DAE.Runtime.Data.DataValue LNodeTable = 
					DataSession.Evaluate
					(
						CLibraryNodeTypesExpression,
						LParams
					)
			)
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(LNodeTable.AsString);
			}
			ValidateNodeTypeTable();
		}

		public void SetFormDesigner()
		{
			using (DAE.Runtime.Data.DataValue LNodeTable = DataSession.Evaluate(CFormDesignerNodeTypesExpression))
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(LNodeTable.AsString);
			}
		}

		#endregion

		#region Execution

		/// <remarks> Must call SetApplication or SetLibrary before calling Start().  Upon completion, this session will be disposed. </remarks>
		public void Start(string ADocument)
		{
			TimingUtility.PushTimer(String.Format("Windows.Session.Start('{0}')", ADocument));
			try
			{
				// Prepare the root forms host
				FRootFormHost = CreateHost();
				try
				{
					FRootFormHost.NextRequest = new Request(ADocument);
					IWindowsFormInterface LRootForm;
					do
					{
						LRootForm = LoadNextForm(FRootFormHost);
						LRootForm.ShowModal(FormMode.None);
					} while (FRootFormHost.NextRequest != null);
				}
				finally
				{
					Dispose();
				}
			}
			finally
			{
				TimingUtility.PopTimer();
			}
		}

		/// <remarks> The session should already be prepared before invoking this routine.  Upon completion, this session will be disposed. </remarks>
		public void StartCallback(string ADocument, EventHandler AOnComplete)
		{
			TimingUtility.PushTimer(String.Format("Windows.Session.StartCallback('{0}')", ADocument));
			try
			{
				// Prepare the root form's host
				FRootFormHost = CreateHost();
				try
				{
					FRootFormHost.NextRequest = new Request(ADocument);
					FOnComplete = AOnComplete;
					RootFormAdvance(null);
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

		private IWindowsFormInterface LoadNextForm(IHost AHost)
		{
			IWindowsFormInterface LForm = (IWindowsFormInterface)CreateForm();
			try
			{
				LForm.SupressCloseButton = true;
				AHost.LoadNext(LForm);
				AHost.Open();
				LForm.Form.Closing += new System.ComponentModel.CancelEventHandler(MainFormClosing);
				return LForm;
			}
			catch
			{
				LForm.Dispose();
				LForm = null;
				throw;
			}
		}

		private IHost FRootFormHost;
		private EventHandler FOnComplete;

		private void RootFormAdvance(IFormInterface AForm)
		{
			if (FRootFormHost.NextRequest != null)
				LoadNextForm(FRootFormHost).Show(new FormInterfaceHandler(RootFormAdvance));
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

		#region Shut-down

		/// <summary> Attempts to close the session's forms and returns true if they closed. </summary>
		/// <param name="AExclude"> When true, the given root form is omitted. </param>
		public bool CloseAllForms(IHost AExclude, CloseBehavior ABehavior)
		{
			Frontend.Client.Forms.FormStack LFormStack;
			Frontend.Client.Forms.FormStack LNext = this.Forms.First;
			while (LNext != null)
			{
				LFormStack = LNext;
				LNext = LNext.Next;	// remember the next item before it get's lost
				while 
				(
					!LFormStack.IsEmpty() && 
					(
						(AExclude == null) || 
						(LFormStack.GetTopmostForm().HostNode != AExclude)
					)
				)
					if (!LFormStack.GetTopmostForm().Close(ABehavior))
						return false;
			}
			return true;
		}

		/// <summary> Attempts to close all of a forms covering (child-modal) "children". </summary>
		/// <returns> True if any covering forms were closed. </returns>
		public bool UncoverForm(IFormInterface AForm, CloseBehavior ABehavior)
		{
			Frontend.Client.Forms.FormStack LFormStack = Forms.First;
			int i;
			while (LFormStack != null)
			{
				for (i = 0; i < LFormStack.Forms.Count; i++)
					if (LFormStack.Forms[i] == AForm)
					{
						for (int j = LFormStack.Forms.Count - 1; j > i; j--)
							if (!((IFormInterface)LFormStack.Forms[j]).Close(ABehavior))
								return false;
						return true;
					}
				LFormStack = LFormStack.Next;
			}
			return true;
		}

		private void MainFormClosing(object ASender, System.ComponentModel.CancelEventArgs AArgs)
		{
			try
			{
				if (!AArgs.Cancel && !CloseAllForms(FRootFormHost, CloseBehavior.AcceptOrClose))
					AArgs.Cancel = true;
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}

		#endregion

		#region Client.Session

		public override Client.IHost CreateHost()
		{
			Host LHost = new Host(this);
			LHost.OnDeserializationErrors += new DeserializationErrorsHandler(DeserializationErrors);
			return LHost;
		}

		public new IWindowsFormInterface LoadForm(INode ANode, string ADocument)
		{
			return (IWindowsFormInterface)base.LoadForm(ANode, ADocument, null);
		}

		public new IWindowsFormInterface LoadForm(INode ANode, string ADocument, FormInterfaceHandler ABeforeActivate)
		{
			return (IWindowsFormInterface)base.LoadForm(ANode, ADocument, ABeforeActivate);
		}

		/// <remarks> If this event is set, then the default behavior will not be invoked. </remarks>
		public event DeserializationErrorsHandler OnDeserializationErrors;

		private void DeserializationErrors(Host AHost, ErrorList AErrorList)
		{
			if (OnDeserializationErrors != null)
				OnDeserializationErrors(AHost, AErrorList);
			else
			{
				if ((AHost != null) && (AHost.Children.Count > 0))
				{
					IFormInterface LFormInterface = AHost.Children[0] as IFormInterface;
					if (LFormInterface == null)
						LFormInterface = (IFormInterface)AHost.Children[0].FindParent(typeof(IFormInterface));

					if (LFormInterface != null)
					{
						LFormInterface.EmbedErrors(AErrorList);
						return;
					}
				}
				
				ErrorListForm.ShowErrorList(AErrorList, true);
			}
		}
		
		public override void ReportErrors(IHost AHost, ErrorList AErrorList)
		{
			if (OnDeserializationErrors != null)
				OnDeserializationErrors((Host)AHost, AErrorList);
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

		#endregion

		#region Static Utilities

		public static void HandleException(Exception AException)
		{
			try
			{
				if (!(AException is AbortException))
				{
					using (var LExceptionForm = new ExceptionForm())
					{
						LExceptionForm.Exception = AException;
						LExceptionForm.ShowDialog();
					}
				}
			}
			catch(Exception LException)
			{
				SRFLogger.WriteLine(TraceLevel.Error,"Error trying to handle exception {0}",LException);
                // Do nothing... Do not throw! Throwing here closes the app, which is probably worse than eating an exception (gulp).
			}
		}

		public static object FInvokeControlLock = new Object();
		public static WinForms.Control FInvokeControl;

		/// <summary> Executes a delegate in the context of the main thread. </summary>
		public static object SafelyInvoke(Delegate ADelegate, object[] AArguments)
		{
			WinForms.Control LControl = FInvokeControl;	// thread safe... int read

			if ((LControl != null) && LControl.InvokeRequired)
				return LControl.Invoke(ADelegate, AArguments);
			else
				return ADelegate.DynamicInvoke(AArguments);
		}

		#endregion
	}

	public delegate void FileLoaderCompleteHandler(FileLoader ALoader);

	public class FileLoader
	{
		public FileLoader(Alphora.Dataphor.DAE.IServerSession ASession, string ADocument, string AFileName, FileLoaderCompleteHandler AOnComplete)
		{
			FSession = ASession;
			FDocument = ADocument;
			FFileName = AFileName;
			FOnComplete = AOnComplete;
			FProcess = ASession.StartProcess(new DAE.ProcessInfo(ASession.SessionInfo));
			Thread LLoadHelpThread = new Thread(new ThreadStart(AsyncLoad));
			LLoadHelpThread.IsBackground = true;
			LLoadHelpThread.Start();
		}

		private DAE.IServerSession FSession;
		private FileLoaderCompleteHandler FOnComplete;
		private DAE.IServerProcess FProcess;

		private string FDocument;
		public string Document { get { return FDocument; } }
		
		private string FFileName;
		public string FileName { get { return FFileName; } }

		public void AsyncLoad()
		{
			try
			{
				uint LCRC32 = 0;
				if (System.IO.File.Exists(FFileName))
				{
					// compute the CRC of the existing file
					using (FileStream LStream = new FileStream(FFileName, FileMode.Open, FileAccess.Read))
						LCRC32 = CRC32Utility.GetCRC32(LStream);
				}
				using
				(					
					DAE.Runtime.Data.Row LRow = (DAE.Runtime.Data.Row)FProcess.Evaluate
					(
						String.Format
						(
							"LoadIfNecessary('{0}', {1})",
							FDocument.Replace("'", "''"),
							((int)LCRC32).ToString()
						), 
						null
					)
				)
				{
					if (!LRow["CRCMatches"].AsBoolean)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(FFileName));
						using (Stream LSourceStream = LRow["Value"].OpenStream())
							using (FileStream LTargetStream = new FileStream(FFileName, FileMode.Create, FileAccess.Write))
								StreamUtility.CopyStream(LSourceStream, LTargetStream);
					}
				}
				Session.SafelyInvoke(new AsyncFinishHandler(AsyncFinish), new object[] {true});
			}
			catch
			{
				Session.SafelyInvoke(new AsyncFinishHandler(AsyncFinish), new object[] {false});
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private delegate void AsyncFinishHandler(bool ASuccess);

		private void AsyncFinish(bool ASuccess)
		{
			try
			{
				FSession.StopProcess(FProcess);
			}
			finally
			{
				if (ASuccess && (FOnComplete != null))
					FOnComplete(this);
			}
		}
	}
}
