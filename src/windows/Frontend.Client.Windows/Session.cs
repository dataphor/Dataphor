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
using WinForms = System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using Alphora.Dataphor.DAE;

namespace Alphora.Dataphor.Frontend.Client.Windows
{
	/// <summary> Session represents the client session with the application server. </summary>
	public class Session : Frontend.Client.Session
	{
		public static string CCachePath = @"Dataphor\WindowsClientCache\";
		public static string CHelpPath = @"Help";
		public static int CDefaultDocumentCacheSize = 800;
		public static int CDefaultImageCacheSize = 60;

		public const string ClientName = "Windows";
		public const string FormDesignerNodeTypesExpression = ".Frontend.GetNodeTypes('" + ClientName + "', Frontend.FormDesignerLibraries)";
		public const string LibraryNodeTypesExpression = ".Frontend.GetLibraryNodeTypes('" + ClientName + "', ALibraryName)";
		public const string GetFormDesignerLibraryFilesExpression = @".Frontend.GetLibraryFiles(.Frontend.ClientTypes['" + ClientName + "'].Environment, FormDesignerLibraries)";
		public const string SettingsExpression = ".Frontend.GetWindowsSettings(AApplicationID)";

		public Session(Alphora.Dataphor.DAE.Client.DataSession dataSession, bool ownsDataSession) : base(dataSession, ownsDataSession) 
		{
			_toolTip = new WinForms.ToolTip();
			try
			{
				_toolTip.Active = true;
			}
			catch
			{
				_toolTip.Dispose();
				_toolTip = null;
				throw;
			}

			// Ensure we are setup for SafelyInvoke.  This must happen on the main windows thread and thus is not in a static constructor.
			lock (_invokeControlLock)
			{
				if (_invokeControl == null)
				{
					_invokeControl = new WinForms.Control();
					_invokeControl.CreateControl();
				}
			}
			
			// Ensure the assembly resolver is loaded
			Alphora.Dataphor.Windows.AssemblyUtility.Initialize();
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
					if (_toolTip != null)
					{
						_toolTip.Dispose();
						_toolTip = null;
					}
				}
				finally
				{
					try
					{
						if (_notifyIcon != null)
						{
							_notifyIcon.Dispose();
							_notifyIcon = null;
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
									base.Dispose(disposing); 					
								}
							}
						}
					}
				}
			}
		}

		// ToolTip

		private WinForms.ToolTip _toolTip;
		
		public WinForms.ToolTip ToolTip
		{
			get { return _toolTip; }
		}
		
		// NotifyIcon

		private WinForms.NotifyIcon _notifyIcon;
		
		public WinForms.NotifyIcon NotifyIcon
		{
			get 
			{
				if (_notifyIcon == null)
				{
					System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BaseForm));
					_notifyIcon = new System.Windows.Forms.NotifyIcon();
					_notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("FNotifyIcon.Icon")));
					_notifyIcon.Visible = true;
				}
				return _notifyIcon; 
			}
		}
						   
		// Theme

		private Theme _theme = new Theme();

		public Theme Theme
		{
			get
			{
				if (_theme != null)
					return _theme;
				else
					return new Theme();
			}
		}

		// DefaultIcon

		private Icon _defaultIcon;
		public Icon DefaultIcon { get { return _defaultIcon; } }

		private void DisposeDefaultIcon()
		{
			if (_defaultIcon != null)
			{
				_defaultIcon.Dispose();
				_defaultIcon = null;
			}
		}
        
		// AfterFormActivate

		public event FormInterfaceHandler AfterFormActivate;

		public void DoAfterFormActivate(IFormInterface form)
		{
			if (AfterFormActivate != null)
				AfterFormActivate(form);
		}

		#region HelpProvider

		public bool IsContextHelpAvailable()
		{
			return ((_helpLoader != null) || (_helpFileName != String.Empty));
		}

		private string _helpFileName = String.Empty;
		/// <summary> The file to use when help is requested on controls. </summary>
		public string HelpFileName
		{
			get { return _helpFileName; }
			set { _helpFileName = (value == null ? String.Empty : value); }
		}

		private Dictionary<WinForms.Control,IElement> _helpControls = new Dictionary<System.Windows.Forms.Control,IElement>();
		private FileLoader _helpLoader;

		public void RegisterControlHelp(WinForms.Control control, IElement element)
		{
			if (!_helpControls.ContainsKey(control))
			{
				control.HelpRequested += new System.Windows.Forms.HelpEventHandler(ControlHelpRequested);
				control.Disposed += new EventHandler(HelpControlDisposed);
				_helpControls.Add(control, element);
			}
		}

		public void UnregisterControlHelp(WinForms.Control control)
		{
			if (_helpControls.ContainsKey(control))
			{
				control.HelpRequested -= new System.Windows.Forms.HelpEventHandler(ControlHelpRequested);
				control.Disposed -= new EventHandler(HelpControlDisposed);
				_helpControls.Remove(control);
			}
		}

		private bool InternalInvokeHelp(WinForms.Control control, string helpKeyword, HelpKeywordBehavior helpKeywordBehavior, string helpString, bool preferPopup)
		{
			WinForms.HelpNavigator navigator = (WinForms.HelpNavigator)helpKeywordBehavior;
			if 
			(
				(preferPopup || (helpKeyword == String.Empty)) 
					&& (helpString != String.Empty)
			)
				WinForms.Help.ShowPopup(control, helpString, WinForms.Control.MousePosition);
			else
			{
				if (_helpLoader != null)
					System.Media.SystemSounds.Beep.Play();
				else
				{
					if (_helpFileName != String.Empty)
					{
						if (helpKeyword != String.Empty)
							WinForms.Help.ShowHelp(control, _helpFileName, navigator, helpKeyword);
						else
							WinForms.Help.ShowHelp(control, _helpFileName, navigator);
					}
					else
						return false;
				}
			}
			return true;
		}

		public override void InvokeHelp(INode sender, string helpKeyword, HelpKeywordBehavior helpKeywordBehavior, string helpString)
		{
			WinForms.Control control = null;
			if (sender != null)
			{
				IWindowsControlElement element = sender as IWindowsControlElement;
				if (element != null)
					control = element.Control;
				else
					control = (WinForms.Control)((IWindowsFormInterface)sender.FindParent(typeof(IWindowsFormInterface))).Form;
			}

			InternalInvokeHelp(control, helpKeyword, helpKeywordBehavior, helpString, false);
		}

		private void ControlHelpRequested(object sender, System.Windows.Forms.HelpEventArgs args)
		{
			args.Handled |= FindAndInvokeHelp((WinForms.Control)sender);
		}

		private bool FindAndInvokeHelp(WinForms.Control control)
		{
			IElement element;
			if (_helpControls.TryGetValue(control, out element))
			{
				while ((element != null) && (element.HelpKeyword == "") && (element.HelpString == "") && (element.Parent != null) && (element.Parent is IElement))
					element = (IElement)element.Parent;
				string keyword = (element != null) ? element.HelpKeyword : "";
				HelpKeywordBehavior behavior = (element != null) ? element.HelpKeywordBehavior : HelpKeywordBehavior.KeywordIndex;
				string stringValue = (element != null) ? element.HelpString : "";
				return InternalInvokeHelp(control, keyword, behavior, stringValue, (WinForms.Control.MouseButtons != WinForms.MouseButtons.None));
			}
			else
				return (control.Parent != null) && FindAndInvokeHelp(control.Parent);
		}

		private void HelpControlDisposed(object sender, EventArgs args)
		{
			UnregisterControlHelp((WinForms.Control)sender);
		}

		private string GetHelpFileName(string document)
		{
			DocumentExpression expression = new DocumentExpression(document);
			if (expression.Type == DocumentType.Document)
			{
				return expression.DocumentArgs.LibraryName + "." + expression.DocumentArgs.DocumentName + "." 
					+ 
					(
						((DAE.Runtime.Data.Scalar)Evaluate
						(
							String.Format
							(
								".Frontend.GetDocumentType('{0}', '{1}')", 
								expression.DocumentArgs.LibraryName, 
								expression.DocumentArgs.DocumentName
							)
						)).AsString
					);
			}
			else
				return Path.ChangeExtension(document.GetHashCode().ToString(), ".chm");
		}

		/// <summary> Loads (to the client) the specified help document and sets <see cref="HelpFileName"/> to the local file name. </summary>
		public void LoadHelpDocument(string document)
		{
			_helpLoader = 
				new FileLoader
				(
					DataSession.ServerSession, 
					document, 
					Path.Combine
					(
						Path.Combine
						(
							AppDomain.CurrentDomain.BaseDirectory, 
							CHelpPath
						), 
						GetHelpFileName(document)
					), 
					new FileLoaderCompleteHandler(HelpFileLoaded)
				);
		}

		private void HelpFileLoaded(FileLoader loader)
		{
			_helpFileName = loader.FileName;
			_helpLoader = null;
		}

		#endregion

		#region Applications & Libraries

		public string SetApplication(string applicationID)
		{
			return SetApplication(applicationID, ClientName);
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

		public override string SetApplication(string applicationID, string clientType)
		{
			// Reset our current settings
			_theme = null;
			DisposeDefaultIcon();
			ClearDocumentCache();
			ClearImageCache();
			int documentCacheSize = CDefaultDocumentCacheSize;
			int imageCacheSize = CDefaultImageCacheSize;

			// Optimistically load the settings
			try
			{
				DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
				paramsValue.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "AApplicationID", applicationID));

				using (DAE.Runtime.Data.Row row = (DAE.Runtime.Data.Row)Evaluate(Pipe.Process, SettingsExpression, paramsValue))
				{
					if (row != null)
					{
						// Load the theme
						if (row.HasValue("Theme"))
							_theme = (Theme)new BOP.Deserializer().Deserialize((string)row["Theme"], null);

						// Load the default form icon
						if (row.HasValue("IconImage"))
							using (Stream iconStream = row.GetValue("IconImage").OpenStream())
							{
								Bitmap bitmap = System.Drawing.Image.FromStream(iconStream) as Bitmap;
								if (bitmap != null)
									_defaultIcon = Icon.FromHandle(bitmap.GetHicon());	// TODO: Should this bitmap be disposed after this?
							}

						// Load the document cache size
						if (row.HasValue("DocumentCacheSize"))
							documentCacheSize = (int)row["DocumentCacheSize"];

						// Load the image cache size
						if (row.HasValue("ImageCacheSize"))
							imageCacheSize = (int)row["ImageCacheSize"];

						// Load the help file
						if (row.HasValue("HelpDocument"))
						{
							string document = (string)row["HelpDocument"];
							if (document != String.Empty)
								LoadHelpDocument(document);
						}
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(new ClientException(ClientException.Codes.ErrorLoadingSettings, exception));
			}
			finally
			{
				if (_theme == null)
					_theme = new Theme();
			}

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

			// Set up the client-side document cache
			try
			{
				if (documentCacheSize > 0)
					Pipe.Cache = 
						new DocumentCache
						(
							Path.Combine
							(
								Path.Combine(System.IO.Path.GetTempPath(), CCachePath),
								@"App" + applicationID.ToString()
							), 
							documentCacheSize
						);
			}
			#if DEBUG
			catch (Exception exception)
			#else
			catch
			#endif
			{
				#if DEBUG
				HandleException(exception);	// Don't fail if we can't do this and only show something if under debug
				#endif
			}

			return base.SetApplication(applicationID, clientType);
		}

		public void SetLibrary(string libraryName)
		{
			DAE.Runtime.DataParams paramsValue = new DAE.Runtime.DataParams();
			paramsValue.Add(DAE.Runtime.DataParam.Create(Pipe.Process, "ALibraryName", libraryName));
			using 
			(
				DAE.Runtime.Data.Scalar nodeTable = 
					DataSession.Evaluate
					(
						LibraryNodeTypesExpression,
						paramsValue
					)
			)
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(nodeTable.AsString);
			}
			ValidateNodeTypeTable();
		}

		public void SetFormDesigner()
		{
			using (DAE.Runtime.Data.Scalar nodeTable = DataSession.Evaluate(FormDesignerNodeTypesExpression))
			{
				NodeTypeTable.Clear();
				NodeTypeTable.LoadFromString(nodeTable.AsString);
			}

			// Load the files required to register any nodes, if necessary				
			if (DataSession.Server is DAE.Server.LocalServer)
			{
				IServerCursor cursor = DataSession.OpenCursor(GetFormDesignerLibraryFilesExpression);
				try
				{
					using (DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow())
					{
						bool shouldLoad;
						List<string> filesToLoad = new List<string>();

						while (cursor.Next())
						{
							cursor.Select(row);
							string fullFileName = 
								((DAE.Server.LocalServer)DataSession.Server).GetFile
								(
									(DAE.Server.LocalProcess)cursor.Plan.Process, 
									(string)row["Library_Name"], 
									(string)row["Name"], 
									(DateTime)row["TimeStamp"], 
									(bool)row["IsDotNetAssembly"], 
									out shouldLoad
								);
							if (shouldLoad)
								filesToLoad.Add(fullFileName);
						}
						
						// Load each file to ensure they can be reached by the assembly resolver hack (see AssemblyUtility)
						foreach (string fullFileName in filesToLoad)
							Assembly.LoadFrom(fullFileName);
					}
				}
				finally
				{
					DataSession.CloseCursor(cursor);
				}
			}
		}

		#endregion

		#region Execution

		/// <remarks> Must call SetApplication or SetLibrary before calling Start().  Upon completion, this session will be disposed. </remarks>
		public void Start(string document)
		{
			TimingUtility.PushTimer(String.Format("Windows.Session.Start('{0}')", document));
			try
			{
				// Prepare the root forms host
				_rootFormHost = CreateHost();
				try
				{
					_rootFormHost.NextRequest = new Request(document);
					IWindowsFormInterface rootForm;
					do
					{
						rootForm = LoadNextForm(_rootFormHost);
						rootForm.ShowModal(FormMode.None);
					} while (_rootFormHost.NextRequest != null);
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
		public void StartCallback(string document, EventHandler onComplete)
		{
			TimingUtility.PushTimer(String.Format("Windows.Session.StartCallback('{0}')", document));
			try
			{
				// Prepare the root form's host
				_rootFormHost = CreateHost();
				try
				{
					_rootFormHost.NextRequest = new Request(document);
					_onComplete = onComplete;
					RootFormAdvance(null);
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
			}
			finally
			{
				TimingUtility.PopTimer();
			}
		}

		private IWindowsFormInterface LoadNextForm(IHost host)
		{
			IWindowsFormInterface form = (IWindowsFormInterface)CreateForm();
			try
			{
				form.SupressCloseButton = true;
				host.Close();
				host.LoadNext(form);
				host.Open();
				form.Form.Closing += new System.ComponentModel.CancelEventHandler(MainFormClosing);
				return form;
			}
			catch
			{
				form.Dispose();
				form = null;
				throw;
			}
		}

		private IHost _rootFormHost;
		private EventHandler _onComplete;

		private void RootFormAdvance(IFormInterface form)
		{
			if (_rootFormHost.NextRequest != null)
				LoadNextForm(_rootFormHost).Show(new FormInterfaceHandler(RootFormAdvance));
			else
			{
				if (_onComplete != null)
				{
					_onComplete(this, EventArgs.Empty);
					_onComplete = null;
				}
				Dispose();
			}
		}

		#endregion

		#region Shut-down

		private void MainFormClosing(object sender, System.ComponentModel.CancelEventArgs args)
		{
			try
			{
				if (!args.Cancel && !CloseAllForms(_rootFormHost, CloseBehavior.AcceptOrClose))
					args.Cancel = true;
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}

		#endregion

		#region Client.Session

		public override Client.IHost CreateHost()
		{
			Host host = new Host(this);
			host.OnDeserializationErrors += new DeserializationErrorsHandler(ReportErrors);
			return host;
		}

		public new IWindowsFormInterface LoadForm(INode node, string document)
		{
			return (IWindowsFormInterface)base.LoadForm(node, document, null);
		}

		public new IWindowsFormInterface LoadForm(INode node, string document, FormInterfaceHandler beforeActivate)
		{
			return (IWindowsFormInterface)base.LoadForm(node, document, beforeActivate);
		}

		public event DeserializationErrorsHandler OnDeserializationErrors;

		private bool HasErrors(ErrorList errorList)
		{
			for (int index = 0; index < errorList.Count; index++)
				if 
				(
					(!(errorList[index] is Alphora.Dataphor.DAE.Compiling.CompilerException)) 
						|| 
						(
							(((Alphora.Dataphor.DAE.Compiling.CompilerException)errorList[index]).ErrorLevel == Alphora.Dataphor.DAE.Compiling.CompilerErrorLevel.Fatal) 
								|| (((Alphora.Dataphor.DAE.Compiling.CompilerException)errorList[index]).ErrorLevel == Alphora.Dataphor.DAE.Compiling.CompilerErrorLevel.NonFatal)
						)
				)
					return true;
			return false;
		}
		
		public override void ReportErrors(IHost host, ErrorList errorList)
		{
			if (OnDeserializationErrors != null)
				OnDeserializationErrors(host, errorList);

			if ((host != null) && (host.Children.Count > 0))
			{
				IFormInterface formInterface = host.Children[0] as IFormInterface;
				if (formInterface == null)
					formInterface = (IFormInterface)host.Children[0].FindParent(typeof(IFormInterface));

				if (formInterface != null)
				{
					formInterface.EmbedErrors(errorList);
					return;
				}
			}

			#if (DEBUG)
			if (HasErrors(errorList))
				ErrorListForm.ShowErrorList(errorList, true);
			#endif
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

		public static void HandleException(Exception exception)
		{
			try
			{
				if (!(exception is AbortException))
				{
                    using (var exceptionForm = new ExceptionForm())
					{
						exceptionForm.Exception = exception;
						exceptionForm.ShowDialog();
					}                    
				}
			}
			catch
			{
                // Do nothing... Do not throw! Throwing here closes the app, which is probably worse than eating an exception (gulp).
			}
		}

		public static object _invokeControlLock = new Object();
		public static WinForms.Control _invokeControl;

		/// <summary> Executes a delegate in the context of the main thread. </summary>
		public static object SafelyInvoke(Delegate delegateValue, object[] arguments)
		{
			WinForms.Control control = _invokeControl;	// thread safe... int read

			if ((control != null) && control.InvokeRequired)
				return control.Invoke(delegateValue, arguments);
			else
				return delegateValue.DynamicInvoke(arguments);
		}

		#endregion
	}

	public delegate void FileLoaderCompleteHandler(FileLoader ALoader);

	public class FileLoader
	{
		public FileLoader(Alphora.Dataphor.DAE.IServerSession session, string document, string fileName, FileLoaderCompleteHandler onComplete)
		{
			_session = session;
			_document = document;
			_fileName = fileName;
			_onComplete = onComplete;
			_process = session.StartProcess(new DAE.ProcessInfo(session.SessionInfo));
			Thread loadHelpThread = new Thread(new ThreadStart(AsyncLoad));
			loadHelpThread.IsBackground = true;
			loadHelpThread.Start();
		}

		private DAE.IServerSession _session;
		private FileLoaderCompleteHandler _onComplete;
		private DAE.IServerProcess _process;

		private string _document;
		public string Document { get { return _document; } }
		
		private string _fileName;
		public string FileName { get { return _fileName; } }

		public void AsyncLoad()
		{
			try
			{
				uint cRC32 = 0;
				if (System.IO.File.Exists(_fileName))
				{
					// compute the CRC of the existing file
					using (FileStream stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
						cRC32 = CRC32Utility.GetCRC32(stream);
				}
				using
				(					
					DAE.Runtime.Data.Row row = (DAE.Runtime.Data.Row)_process.Evaluate
					(
						String.Format
						(
							"LoadIfNecessary('{0}', {1})",
							_document.Replace("'", "''"),
							((int)cRC32).ToString()
						), 
						null
					)
				)
				{
					if (!(bool)row["CRCMatches"])
					{
						Directory.CreateDirectory(Path.GetDirectoryName(_fileName));
						using (Stream sourceStream = row.GetValue("Value").OpenStream())
							using (FileStream targetStream = new FileStream(_fileName, FileMode.Create, FileAccess.Write))
								StreamUtility.CopyStream(sourceStream, targetStream);
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

		private void AsyncFinish(bool success)
		{
			try
			{
				_session.StopProcess(_process);
			}
			finally
			{
				if (success && (_onComplete != null))
					_onComplete(this);
			}
		}
	}
}
