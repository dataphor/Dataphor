/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Threading;
#if TRACEHANDLECOLLECTOR 
using System.Reflection;
using System.Diagnostics;
#endif

using Alphora.Dataphor.BOP;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Dataphoria.ObjectTree;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.Services;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.Dataphoria.ObjectTree.Nodes;
using Alphora.Dataphor.DAE.Server;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.Windows;

using WeifenLuo.WinFormsUI.Docking;
using Action=System.Action;

namespace Alphora.Dataphor.Dataphoria
{
	public partial class Dataphoria : Form, IDataphoria
	{
		public const string ConfigurationFileName = "Dataphoria{0}.config";
		public const string ScratchPadFileName = "ScratchPad.d4";		
		
		public Dataphoria()
		{
			InitializeComponent();

			ICSharpCode.TextEditor.Document.HighlightingManager.Manager.AddSyntaxModeFileProvider(new ICSharpCode.TextEditor.Document.FileSyntaxModeProvider(PathUtility.GetBinDirectory()));

			_services.Add(typeof(DAE.Client.Controls.Design.IPropertyTextEditorService), new PropertyTextEditorService());

			CreateDebugger();

			_explorer = new DataTree();
			_explorer.AllowDrop = true;
			_explorer.BorderStyle = BorderStyle.None;
			_explorer.CausesValidation = false;
			_explorer.HideSelection = false;
			_explorer.ImageIndex = 0;
			_explorer.ImageList = FTreeImageList;
			_explorer.Name = "FExplorer";
			_explorer.SelectedImageIndex = 0;
			_explorer.ShowRootLines = false;			
			_explorer.TabIndex = 1;
			_explorer.HelpRequested += FExplorer_HelpRequested;
			_explorer.Dataphoria = this;
			_explorer.Select();
			_explorer.Dock = DockStyle.Fill;

			_dockContentExplorer = new DockContent();
			_dockContentExplorer.Controls.Add(_explorer);
			_dockContentExplorer.HideOnClose = true;
			_dockContentExplorer.TabText = "Dataphor Explorer";
			_dockContentExplorer.Text = "Dataphor Explorer - Dataphoria";
			_dockContentExplorer.ShowHint = DockState.DockLeft;

			_errorListView = new ErrorListView();
			_errorListView.OnErrorsAdded += ErrorsAdded;
			_errorListView.OnWarningsAdded += WarningsAdded;
			_errorListView.Dock = DockStyle.Fill;

			_dockContentErrorListView = new DockContent();
			_dockContentErrorListView.HideOnClose = true;
			_dockContentErrorListView.Controls.Add(_errorListView);
			_dockContentErrorListView.TabText = "Errors/Warnings ";	// HACK: Space is to work around last character being cut-off in tab
			_dockContentErrorListView.Text = "Errors/Warnings - Dataphoria";
			_dockContentErrorListView.ShowHint = DockState.DockBottomAutoHide;

			_dockContentExplorer.Show(this.FDockPanel);
			_dockContentErrorListView.Show(this.FDockPanel);

			FDockPanel.DockLeftPortion = 240;
			FDockPanel.DockRightPortion = 240;
			FDockPanel.DockTopPortion = 240;
			FDockPanel.DockBottomPortion = 240;

			LoadSettings();
		}

		#if TRACEFOCUS

		private static string WalkParentControls(Control control)
		{
			if (control == null)
				return "";
			else
				return 
					WalkParentControls(control.Parent)
						+ "->"
						+ (control.Parent != null ? "(" + control.Parent.Controls.IndexOf(control).ToString() + ")" : "")
						+ (control.Name != null ? control.Name : "")
						+ (control.Text != null ? "\"" + control.Name + "\"" : "")
						+ "[" + control.GetType().Name + "]";
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet=System.Runtime.InteropServices.CharSet.Auto, ExactSpelling=true)]
		public static extern IntPtr GetFocus();
 
		private bool ProcessQueryFocus(Form form, Keys key)
		{
			IntPtr focusPtr = GetFocus();
			if (focusPtr != IntPtr.Zero)
			{
				Control control = Control.FromHandle(focusPtr);
				if (control != null)
					System.Diagnostics.Trace.WriteLine("Focus: " + WalkParentControls(control));
			}
			return true;
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Shift | Keys.Alt | Keys.F))
				ProcessQueryFocus(this, keyData);
			return base.ProcessDialogKey(keyData);
		}
		
		#endif

		protected override void OnClosing(CancelEventArgs args)   
		{	 		
			// HACK: Something in the WinForms validation process is returning false.  We don't care so always make sure Cancel is false at the beginning of OnClosing
			args.Cancel = false;
			base.OnClosing(args);
		}

		private DataTree _explorer;
		private ErrorListView _errorListView;
		private SessionsView _sessionsView;
		private CallStackView _callStackView;
		private DebugProcessesView _debugProcessesView;
		private ProcessesView _processesView;
		private StackView _stackView;

		private DockContent _dockContentExplorer;
		private DockContent _dockContentErrorListView;
		private DockContent _dockContentSessionsView;
		private DockContent _dockContentCallStackView;
		private DockContent _dockContentDebugProcessesView;
		private DockContent _dockContentProcessesView;
		private DockContent _dockContentStackView;

		#region Settings

		private Settings _settings;
		public Settings Settings { get { return _settings; } }

		// when the form state is saved when maximized, the restore size needs to be remembered
		private Rectangle _normalBounds = Rectangle.Empty;

		public string GetConfigurationFileName(string type)
		{
			return Path.Combine(PathUtility.UserAppDataPath(), String.Format(ConfigurationFileName, type));
		}
		
		public string GetScratchPadFileName()
		{
			return Path.Combine(PathUtility.UserAppDataPath(), ScratchPadFileName);
		}		

		private ServerNode _serverNode;
		
		public void RefreshLibraries()
		{
			_serverNode.Build();
			_serverNode.LibraryNode.Refresh();
		}
		
		public void RefreshDocuments(string libraryName)
		{
			libraryName = Alphora.Dataphor.DAE.Schema.Object.EnsureUnrooted(libraryName);
			_serverNode.Build();
			LibraryNode libraryNode = (LibraryNode)_serverNode.LibraryNode.FindByText(libraryName);
			if ((libraryNode != null) && (libraryNode.DocumentListNode != null))
				libraryNode.DocumentListNode.Refresh();
		}

		protected override void OnLoad(EventArgs args)
		{
			base.OnLoad(args);
			RestoreBoundsAndState();
		}

		private void RestoreBoundsAndState()
		{
			try
			{
				Rectangle bounds = SystemInformation.WorkingArea;
				bounds = (Rectangle)_settings.GetSetting("Dataphoria.Bounds", typeof(Rectangle), bounds);
				_normalBounds = bounds;
				Bounds = bounds;

				FormWindowState state = (FormWindowState)_settings.GetSetting("Dataphoria.WindowState", typeof(FormWindowState), FormWindowState.Normal);
				if (state == FormWindowState.Minimized)  // don't start minimized, because it gets messed up
					state = FormWindowState.Normal;
				WindowState = state;
			}
			catch (Exception exception)
			{
				Warnings.AppendError(this, exception, true);
				// don't rethrow
			}
		}

		private void LoadSettings()
		{
			try
			{
				string fileName = GetConfigurationFileName(String.Empty);
				// Load configuration settings
				try
				{
					_settings = new Settings(fileName);
				}
				catch
				{
					_settings = new Settings();
					throw;
				}

			}
			catch (Exception exception)
			{
				Warnings.AppendError(this, exception, true);
				// don't rethrow
			}
		}

		private void SaveSettings()
		{
			if (_settings != null)
			{
				// Save the configuration settings
				_settings.SetSetting("Dataphoria.WindowState", WindowState);
				if (WindowState == FormWindowState.Normal)
					_settings.SetSetting("Dataphoria.Bounds", Bounds);
				else
				{
					if (_normalBounds != Rectangle.Empty)
						_settings.SetSetting("Dataphoria.Bounds", _normalBounds);
				}
				_settings.SaveSettings(GetConfigurationFileName(String.Empty));
			}
		}
		
		private void EnsureScratchPad()
		{
			try
			{
				string fileName = GetScratchPadFileName();			
																	
				DesignerInfo info = GetDefaultDesigner(Program.DocumentTypeFromFileName(fileName));
				FileDesignBuffer buffer = new FileDesignBuffer(this, fileName);
				buffer.EnsureFile();
				IDesigner designer = OpenDesigner(info, buffer);	   									
				designer.Service.StartAutoSave();
			}
			catch (Exception exception)
			{
				Warnings.AppendError(this, exception, true);
				// don't rethrow
			} 
		}

		protected override void OnSizeChanged(EventArgs args)
		{
			if (WindowState == FormWindowState.Normal)
				_normalBounds = Bounds;
			base.OnSizeChanged(args);
		}

		#endregion

		#region Connection

		private DataSession _dataSession;
		public DataSession DataSession { get { return _dataSession; } }
		
		private Frontend.Client.Windows.Session _frontendSession;
		public Frontend.Client.Windows.Session FrontendSession { get { return _frontendSession; } }
		
		private IServerProcess _utilityProcess;
		public IServerProcess UtilityProcess { get { return _utilityProcess; } }

		public void EnsureServerConnection()
		{
			if (_dataSession == null)
			{
				InstanceConfiguration instanceConfiguration = InstanceManager.LoadConfiguration();
				ServerConfiguration defaultInstance = instanceConfiguration.Instances[Engine.DefaultServerName];
				if (instanceConfiguration.Instances.Count == 0)
				{
					defaultInstance = ServerConfiguration.DefaultLocalInstance();
					instanceConfiguration.Instances.Add(defaultInstance);
					InstanceManager.SaveConfiguration(instanceConfiguration);
				}
				
				AliasConfiguration configuration = AliasManager.LoadConfiguration();
				if (configuration.Aliases.Count == 0)
				{
					InProcessAlias alias = new InProcessAlias();
					alias.Name = defaultInstance == null ? ServerConfiguration.DefaultLocalInstanceName : defaultInstance.Name;
					alias.InstanceName = defaultInstance == null ? ServerConfiguration.DefaultLocalInstanceName : defaultInstance.Name;
					configuration.Aliases.Add(alias);
				}

				ServerConnectForm.Execute(configuration);

				using (var statusForm = new StatusForm(Strings.Connecting))
				{
					_dataSession = new DataSession();
					try
					{
						_dataSession.AliasName = configuration.DefaultAliasName;
						_dataSession.SessionInfo.Environment = "WindowsClient";
						_dataSession.Open();

						_utilityProcess = _dataSession.ServerSession.StartProcess(new ProcessInfo(_dataSession.ServerSession.SessionInfo));
						try
						{
							EnsureFrontendRegistered();
							
							_frontendSession = new Frontend.Client.Windows.Session(_dataSession, false);
							try
							{
								_frontendSession.SetLibrary("Frontend");
								_frontendSession.OnDeserializationErrors += FrontendSessionDeserializationErrors;
								_serverNode = new ServerNode(_dataSession.Server != null);
								_serverNode.Text = _dataSession.Alias.ToString();
								_explorer.AddBaseNode(_serverNode);
								try
								{
									OnConnected(EventArgs.Empty);
									_serverNode.Expand();
									FConnectToolStripMenuItem.Visible = false;
									FDisconnectToolStripMenuItem.Visible = true;									
									Text = Strings.DataphoriaTitle + " - " + _dataSession.Alias;
									_dockContentExplorer.Show(FDockPanel);
									EnsureScratchPad();
									_explorer.Focus();
								}
								catch
								{
									_serverNode = null;
									_explorer.Nodes.Clear();
									throw;
								}
							}
							catch
							{
								_frontendSession.Dispose();
								_frontendSession = null;
								throw;
							}
						}
						catch
						{
							if (_utilityProcess != null)
							{
								_dataSession.ServerSession.StopProcess(_utilityProcess);
								_utilityProcess = null;
							}
							throw;
						}
					}
					catch
					{						
						_dataSession.Dispose();
						_dataSession = null;						
						throw;
					}
				}
			}
		}

		public event EventHandler Connected;

		private void OnConnected(EventArgs args)
		{
			if (Connected != null)
				Connected(this, args);
		}

		private void Dataphoria_Shown(object sender, EventArgs args)
		{
			EnsureServerConnection();
		}

		private void InternalDisconnect()
		{
			using (StatusForm statusForm = new StatusForm(Strings.Disconnecting))
			{
				if (_dataSession != null)
				{
					try
					{
						Text = Strings.DataphoriaTitle;
						FConnectToolStripMenuItem.Visible = true;
						FDisconnectToolStripMenuItem.Visible = false;						
						_dockContentExplorer.Hide();

						_explorer.Nodes.Clear();
						_serverNode = null;
						
						OnDisconnected(EventArgs.Empty);
					}
					finally
					{
						try
						{
							if (_utilityProcess != null)
							{
								_dataSession.ServerSession.StopProcess(_utilityProcess);
								_utilityProcess = null;
							}
						}
						finally
						{
							try
							{
								if (_frontendSession != null)
								{
									_frontendSession.Dispose();	// Will dispose the connection
									_frontendSession = null;
								}
							}
							finally
							{
								if (_dataSession != null)
								{
									DataSession dataSession = _dataSession;
									_dataSession = null;
									dataSession.Dispose(); // Set the DataSession null before the close to ensure the Dataphoria appears disconnected and cleanup executes properly.
								}
							}							
						}
					}
				}
			}
		}

		public event EventHandler Disconnected;
		
		private void OnDisconnected(EventArgs args)
		{
			if (Disconnected != null)
				Disconnected(this, args);
		}

		public void Disconnect()
		{
			// Make sure we can safely disconnect
			CloseChildren();
			if (MdiChildren.Length > 0)
				throw new AbortException();

			InternalDisconnect();
		}

		private void FrontendSessionDeserializationErrors(IHost host, ErrorList errors)
		{
			IErrorSource source = null;
			
			if ((host != null) && (host.Children.Count > 0))
				source = host.Children[0] as IErrorSource;

			Warnings.AppendErrors(source, errors, true);
		}
		
		public bool IsConnected
		{
			get { return _dataSession != null; }
		}

		#endregion

		#region Live Designer Support

		private Bitmap LoadBitmap(string resourceName)
		{
			Stream manifestResourceStream = GetType().Assembly.GetManifestResourceStream(resourceName);
			if (manifestResourceStream != null)
			{
				var result = new Bitmap(manifestResourceStream);
				result.MakeTransparent();
				return result;
			}
			throw new ArgumentException("Could not get manifest resource stream for: " + resourceName, "AResourceName");
		}

		public Frontend.Client.Windows.Session GetLiveDesignableFrontendSession()
		{
			Frontend.Client.Windows.Session session = new Frontend.Client.Windows.Session(_dataSession, false);
			session.AfterFormActivate += AfterFormActivated;
			session.OnDeserializationErrors += FrontendSessionDeserializationErrors;
			return session;
		}

		private Hashtable _designedForms = new Hashtable();

		private void AfterFormActivated(IFormInterface interfaceValue)
		{
			interfaceValue.HostNode.OnDocumentChanged += HostDocumentChanged;
			UpdateDesignerActions(interfaceValue);
		}

		private void HostDocumentChanged(object sender, EventArgs args)
		{
			UpdateDesignerActions((IFormInterface)((IHost)sender).Children[0]);
		}

		private void UpdateDesignerActions(IFormInterface interfaceValue)
		{
			IWindowsFormInterface form = (IWindowsFormInterface)interfaceValue;

			if (form.Active)
			{
				form.ClearCustomActions();

				DocumentExpression expression = new DocumentExpression(interfaceValue.HostNode.Document);

				if (expression.Type != DocumentType.None)
					form.AddCustomAction
					(
						Strings.CustomizeMenuText,
						LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Customize.bmp"),
						CustomizeForm
					);

				form.AddCustomAction
				(
					Strings.EditCopyMenuText,
					LoadBitmap("Alphora.Dataphor.Dataphoria.Images.EditCopy.bmp"),
					EditCopyForm
				);

				if (expression.Type == DocumentType.Document)
				{
					form.AddCustomAction
					(
						Strings.EditMenuText,
						LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Edit.bmp"),
						EditForm
					);
				}
			}
		}

		private void CheckExclusiveDesigner(IFormInterface interfaceValue)
		{
			if (_designedForms[interfaceValue] != null) 
				throw new DataphoriaException(DataphoriaException.Codes.SingleDesigner);
		}

		public void AddDesignerForm(IFormInterface interfaceValue, IDesigner designer)
		{
			_designedForms.Add(interfaceValue, designer);
			designer.Disposed += DesignerDisposed;
		}

		private void EditForm(IFormInterface interfaceValue)
		{
			CheckExclusiveDesigner(interfaceValue);

			DocumentExpression expression = Program.GetDocumentExpression(interfaceValue.HostNode.Document);
			string documentType;
			using 
			(
				DAE.Runtime.Data.Scalar documentTypeValue = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							".Frontend.GetDocumentType('{0}', '{1}')", 
							expression.DocumentArgs.LibraryName, 
							expression.DocumentArgs.DocumentName
						)
					)
			)
			{
				documentType = documentTypeValue.AsString;
			}

			ILiveDesigner designer;
			switch (documentType)
			{
				case "dfd" : designer = new FormDesigner.FormDesigner(this, "DFD"); break;
				case "dfdx" : designer = new FormDesigner.CustomFormDesigner(this, "DFDX"); break;
				default : throw new DataphoriaException(DataphoriaException.Codes.DocumentTypeLiveEditNotSupported, documentType);
			}
			try
			{
				designer.Open(interfaceValue.HostNode);
				designer.Show();
				
				AddDesignerForm(interfaceValue, designer);
			}
			catch
			{
				designer.Dispose();
				throw;
			}
		}

		private void EditCopyForm(IFormInterface interfaceValue)
		{
			CheckExclusiveDesigner(interfaceValue);

			FormDesigner.FormDesigner designer = new FormDesigner.FormDesigner(this, "DFD");
			try
			{
				designer.New(interfaceValue.HostNode);
				((IDesigner)designer).Show();
				
				AddDesignerForm(interfaceValue, designer);
			}
			catch
			{
				designer.Dispose();
				throw;
			}
		}

		private void CustomizeForm(IFormInterface interfaceValue)
		{
			CheckExclusiveDesigner(interfaceValue);

			FormDesigner.CustomFormDesigner designer = new FormDesigner.CustomFormDesigner(this, "DFDX");
			try
			{
				designer.New(interfaceValue.HostNode);
				((IDesigner)designer).Show();
				
				AddDesignerForm(interfaceValue, designer);
			}
			catch
			{
				designer.Dispose();
				throw;
			}
		}

		private void DesignerDisposed(object sender, EventArgs args)
		{
			// Remove the designer once it is closed
			IDictionaryEnumerator enumerator = _designedForms.GetEnumerator();
			while (enumerator.MoveNext())
				if (enumerator.Value == sender)
				{
					_designedForms.Remove(enumerator.Key);
					break;
				}
		}

		#endregion

		#region Designer support

		private Hashtable _designers = new Hashtable();
		
		public void CheckNotRegistered(DesignBuffer buffer)
		{
			if (_designers[buffer] != null)
				throw new DataphoriaException(DataphoriaException.Codes.AlreadyDesigning, buffer.GetDescription());
		}

		public void RegisterDesigner(DesignBuffer buffer, IDesigner designer)
		{
			CheckNotRegistered(buffer);
			_designers.Add(buffer, designer);
		}

		public void UnregisterDesigner(DesignBuffer buffer)
		{
			_designers.Remove(buffer);
		}

		public IDesigner GetDesigner(DesignBuffer buffer)
		{
			return (IDesigner)_designers[buffer];
		}

		/// <summary> Opens up a new query window against the specified server. </summary>
		/// <returns> The newly created script editor. </returns>
		public IDesigner NewDesigner()
		{
			DesignerInfo info = new DesignerInfo();
			string selectDesigner = Strings.SelectDesigner;
			IWindowsFormInterface form = FrontendSession.LoadForm(null, String.Format(".Frontend.Derive('Designers adorn {{ ClassName tags {{ Frontend.Browse.Visible = ''false'' }} }} tags {{ Frontend.Caption = ''{0}'' }}')", selectDesigner));
			try
			{
				if (form.ShowModal(FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				info.ID = form.MainSource.DataView.Fields["Main.ID"].AsString;
				info.ClassName = form.MainSource.DataView.Fields["Main.ClassName"].AsString;
			}
			finally
			{
				form.HostNode.Dispose();
			}

			return OpenDesigner(info, null);
		}

		/// <summary> Determine the default designer for the specified document type ID. </summary>
		public DesignerInfo GetDefaultDesigner(string documentTypeID)
		{
			IServerCursor cursor = OpenCursor(String.Format("DocumentTypeDefaultDesigners where DocumentType_ID = '{0}' join Designers by ID = Default_Designer_ID over {{ ID, ClassName }}", documentTypeID));
			try
			{
				DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow();
				try
				{
					if (!cursor.Next())
						throw new DataphoriaException(DataphoriaException.Codes.NoDefaultDesignerForDocumentType, documentTypeID);
					cursor.Select(row);
					DesignerInfo result = new DesignerInfo();
					result.ID = (string)row["ID"];
					result.ClassName = (string)row["ClassName"];
					return result;
				}
				finally
				{
					cursor.Plan.ReleaseRow(row);
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
		}

		/// <summary> Allow the user to choose from the designers associated with the specified document type ID. </summary>
		public DesignerInfo ChooseDesigner(string documentTypeID)
		{
			IWindowsFormInterface form = 
				FrontendSession.LoadForm
				(
					null,
					String.Format
					(
						@"	
							Derive
							(
								'	
									Frontend.DocumentTypeDesigners 
										where DocumentType_ID = ''{0}'' 
										adorn {{ DocumentType_ID {{ default ''{0}'' }} }} 
										remove {{ DocumentType_ID }} 
										join (Frontend.Designers rename {{ ID Designer_ID }}) 
										adorn {{ ClassName tags {{ Frontend.Browse.Visible = ''false'' }} }}
								'
							)
						",
						documentTypeID
					)
				);
			try
			{
				form.Text = Strings.SelectDesigner;
				if (form.MainSource.DataView.IsEmpty())
					throw new DataphoriaException(DataphoriaException.Codes.NoDesignersForDocumentType, documentTypeID);
				if (form.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				DesignerInfo result = new DesignerInfo();
				result.ID = form.MainSource.DataView.Fields["Main.Designer_ID"].AsString;
				result.ClassName = form.MainSource.DataView.Fields["Main.ClassName"].AsString;
				return result;
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		public IDesigner OpenDesigner(DesignerInfo info, DesignBuffer buffer)
		{
			IDesigner designer = (IDesigner)Activator.CreateInstance(Type.GetType(info.ClassName, true), new object[] {this, info.ID});
			try
			{
				if (buffer != null)
					designer.Open(buffer);
				else
					designer.New();
				designer.Show();
				return designer;
			}
			catch
			{
				designer.Dispose();
				throw;
			}
		}

		/// <summary> Instantiates a new D4 text editor with the specified initial content. </summary>
		public TextEditor.TextEditor NewEditor(string text, string documentTypeID)
		{
			TextEditor.TextEditor editor = (TextEditor.TextEditor)OpenDesigner(GetDefaultDesigner(documentTypeID), null);
			try
			{
				editor.New();
				editor.EditorText = text;
				editor.Service.SetModified(false);
				return editor;
			}
			catch
			{
				editor.Dispose();
				throw;
			}
		}

		/// <summary> Evaluates a D4 expression that returns a D4 document, and shows the document in an editor. </summary>
		public TextEditor.TextEditor EvaluateAndEdit(string expression, string documentTypeID)
		{
			using (DAE.Runtime.Data.Scalar script = (DAE.Runtime.Data.Scalar)EvaluateQuery(expression))
			{
				return NewEditor(script.AsString, documentTypeID);
			}
		}

		// DesignBuffers

		private void SetInsertOpenState(Frontend.Client.IFormInterface form)
		{
			form.MainSource.OpenState = DAE.Client.DataSetState.Insert;
		}

		public DocumentDesignBuffer PromptForDocumentBuffer(IDesigner designer, string defaultLibraryName, string defaultDocumentName)
		{
			ExecuteScript
			(
				String.Format
				(
					@"	create session view FilteredDocumentTypes
							DocumentTypeDesigners where Designer_ID = '{0}' over {{ DocumentType_ID }}
								join (DocumentTypes rename {{ ID DocumentType_ID }})
							tags {{ Frontend.Title = 'Document Types' }};
						create session table NewDocument in System.Temp from Documents where false;
						create session reference NewDocument_Libraries NewDocument {{ Library_Name }} references Libraries {{ Name }} tags {{ Frontend.Detail.Visible = 'false', Frontend.Lookup.Title = 'Library' }};
						create session reference NewDocument_FilteredDocumentTypes NewDocument {{ Type_ID }} references FilteredDocumentTypes {{ DocumentType_ID }} tags {{ Frontend.Detail.Visible = 'false', Frontend.Lookup.Title = 'Document Type' }};
					",
					designer.DesignerID
				)
			);
			try
			{
				IWindowsFormInterface form = FrontendSession.LoadForm(null, ".Frontend.Derive('NewDocument', 'Add')", SetInsertOpenState);
				try
				{
					form.Text = Strings.SaveAsDocumentFormTitle;
					form.MainSource.DataView.Edit();
					if (defaultLibraryName != String.Empty)
						form.MainSource.DataView["Main.Library_Name"].AsString = defaultLibraryName;
					if (defaultDocumentName != String.Empty)
						form.MainSource.DataView["Main.Name"].AsString = defaultDocumentName;
					form.MainSource.DataView["Main.Type_ID"].AsString = GetDefaultDocumentType(designer);
					form.MainSource.DataView.OnValidate += new EventHandler(SaveFormValidate);

					if (form.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
						throw new AbortException();

					DocumentDesignBuffer buffer = 
						new DocumentDesignBuffer
						(
							this,
							form.MainSource.DataView["Main.Library_Name"].AsString,
							form.MainSource.DataView["Main.Name"].AsString
						);
					buffer.DocumentType = form.MainSource.DataView["Main.Type_ID"].AsString;
					return buffer;
				}
				finally
				{
					form.HostNode.Dispose();
				}
			}
			finally
			{
				ExecuteScript
				(
					@"	drop reference NewDocument_FilteredDocumentTypes;
						drop reference NewDocument_Libraries;
						drop table NewDocument;
						drop view FilteredDocumentTypes;
					"
				);

			}
		}

		private void SaveFormValidate(object sender, EventArgs args)
		{
			var view = (DataView)sender;
			CheckDocumentOverwrite(view["Main.Library_Name"].AsString, view["Main.Name"].AsString);
		}

		public FileDesignBuffer PromptForFileBuffer(IDesigner designer, string defaultFileName)
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.InitialDirectory = (string)_settings.GetSetting("Dataphoria.SaveDirectory", typeof(string), ".");
				dialog.Filter = GetSaveFilter(designer);
				dialog.FilterIndex = 0;
				dialog.RestoreDirectory = false;
				dialog.Title = Strings.SaveDialogTitle;
				dialog.AddExtension = true;
				if (defaultFileName != String.Empty)
					dialog.DefaultExt = Path.GetExtension(defaultFileName);
				else
				{
					string defaultDocumentType = GetDefaultDocumentType(designer);
					if (defaultDocumentType.Length != 0)
						dialog.DefaultExt = "." + defaultDocumentType;
					else
					{
						dialog.DefaultExt = String.Empty;
						dialog.AddExtension = false;
					}
				}
				dialog.CheckPathExists = true;
				dialog.OverwritePrompt = true;

				if (dialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				_settings.SetSetting("Dataphoria.SaveDirectory", Path.GetDirectoryName(dialog.FileName));

				return new FileDesignBuffer(this, dialog.FileName);
			}
		}

		public void SaveAll()
		{
			
			
			IDesigner designer;
			foreach (Form form in this.MdiChildren)
			{
				designer = form as IDesigner;
				if ((designer != null) && designer.Service.IsModified)
					designer.Service.Save();
			}
		}	   		

		public string GetDocumentType(string libraryName, string documentName)
		{
			return
			(
				(Alphora.Dataphor.DAE.Runtime.Data.Scalar)EvaluateQuery
				(
					String.Format(".Frontend.GetDocumentType('{0}', '{1}')", libraryName, documentName)
				)
			).AsString;
		}

		#endregion

		#region IServiceProvider Members

		private Hashtable _services = new Hashtable();
		public Hashtable Services { get { return _services; } }

		public new virtual object GetService(Type serviceType)
		{
			object result = base.GetService(serviceType);
			if (result != null)
				return result;
			return _services[serviceType];
		}

		#endregion

		#region File Support

		private string GetOpenFilter()
		{
			StringBuilder filter = new StringBuilder();
			filter.Append(Strings.AllFilesFilter);
			IServerCursor cursor = OpenCursor("DocumentTypes over { ID, Description }");
			try
			{
				DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow();
				try
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						filter.AppendFormat("|{1} (*.{0})|*.{0}", (string)row["ID"], (string)row["Description"]);
					}
				}
				finally
				{
					cursor.Plan.ReleaseRow(row);
				}
			}
			finally
			{
				CloseCursor(cursor);
			}
			return filter.ToString();
		}

		private string GetSaveFilter(IDesigner designer)
		{
			StringBuilder filter = new StringBuilder();
			IServerCursor cursor = 
				OpenCursor
				(
					String.Format
					(
						"DocumentTypeDesigners where Designer_ID = '{0}' join DocumentTypes by ID = DocumentType_ID over {{ ID, Description }}",
						designer.DesignerID
					)
				);
			try
			{
				DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow();
				try
				{
					while (cursor.Next())
					{
						cursor.Select(row);
						if (filter.Length > 0)
							filter.Append('|');
						filter.AppendFormat("{1} (*.{0})|*.{0}", (string)row["ID"], (string)row["Description"]);
					}
				}
				finally
				{
					cursor.Plan.ReleaseRow(row);
				}
			}
			finally
			{
				cursor.Plan.Close(cursor);
			}
			if (filter.Length > 0)
				filter.Append('|');
			filter.Append(Strings.AllFilesFilter);
			return filter.ToString();
		}

		private string[] FileOpenPrompt(bool allowMultiple)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.InitialDirectory = (string)_settings.GetSetting("Dataphoria.OpenDirectory", typeof(string), ".");
				dialog.Filter = GetOpenFilter();
				dialog.FilterIndex = 0;
				dialog.RestoreDirectory = false;
				dialog.Title = Strings.FileOpenTitle;
				dialog.Multiselect = allowMultiple;
				dialog.CheckFileExists = true;

				if (dialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				_settings.SetSetting("Dataphoria.OpenDirectory", Path.GetDirectoryName(dialog.FileName));

				return dialog.FileNames;
			}
		}

		public void OpenFiles(string[] fileNames)
		{
			string fileName;
			FileDesignBuffer buffer;

			for (int i = 0; i < fileNames.Length; i++)
			{
				fileName = fileNames[i];
				DesignerInfo info = GetDefaultDesigner(Program.DocumentTypeFromFileName(fileName));
				buffer = new FileDesignBuffer(this, fileName);
				try
				{
					OpenDesigner
					(
						info, 
						buffer
					);
				}
				catch (Exception exception)
				{
					Program.HandleException(exception);
				}
			}
		}

		public void OpenFile()
		{
			OpenFiles(FileOpenPrompt(true));
		}

		public void OpenFileWith()
		{
			string[] fileNames = FileOpenPrompt(false);
			string fileName = fileNames[0];

			DesignerInfo info = ChooseDesigner(Program.DocumentTypeFromFileName(fileName));

			FileDesignBuffer buffer = new FileDesignBuffer(this, fileName);

			OpenDesigner(info, buffer);
		}
		
		public void SaveCatalog()
		{
			ExecuteScript(".System.SaveCatalog();");
		}

		public void BackupCatalog()
		{
			ExecuteScript(".System.BackupCatalog();");
		}
		
		public void UpgradeLibraries()
		{
			ExecuteScript(".System.UpgradeLibraries();");
		}

		#endregion

		#region Child Forms

		public void AttachForm(BaseForm form) 
		{
			form.Show(this.FDockPanel);
			form.SelectNextControl(form, true, true, true, false);	
		}

		private void CloseChildren()
		{
			Form[] forms = this.MdiChildren;
			foreach (Form form in forms)
				form.Close();
		}

		private void DisposeChildren()
		{
			Form[] forms = this.MdiChildren;
			foreach (Form form in forms)
				form.Dispose();
		}

		protected override void OnMdiChildActivate(EventArgs args)
		{
			base.OnMdiChildActivate(args);
			MergeOrRevertMergeOfToolbars();
			MergeOrRevertMergeOfStatusBars();
		}
		
		private void MergeOrRevertMergeOfStatusBars()
		{
			ToolStripManager.RevertMerge(this.FStatusStrip);
			var childForm = ActiveMdiChild as IStatusBarClient;
			if (childForm != null)
			{
				childForm.MergeStatusBarWith(this.FStatusStrip);
			}
		}

		private void MergeOrRevertMergeOfToolbars()
		{
			ToolStripManager.RevertMerge(this.FToolStrip);			
			var childForm = ActiveMdiChild as IToolBarClient;
			if (childForm != null)
			{
				childForm.MergeToolbarWith(this.FToolStrip);
			}
		}

		#endregion

		#region DAE & Frontend Server Helpers

		// these two methods should be moved to serverconnection or even higher
		public void ExecuteScript(string script)
		{
			ExecuteScript(script, null);
		}

		/// <summary> Executes a string on the dataphor server. </summary>
		public void ExecuteScript(string script, DAE.Runtime.DataParams paramsValue)
		{
			if (script != String.Empty)
			{
				Cursor oldCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				try
				{
					IServerScript localScript = _utilityProcess.PrepareScript(script);
					try
					{
						localScript.Execute(paramsValue);
					}
					finally
					{
						_utilityProcess.UnprepareScript(localScript);
					}
				}
				finally
				{
					Cursor.Current = oldCursor;
				}
			}
		}

		public IServerCursor OpenCursor(string query)
		{
			return OpenCursor(query, null);
		}

        public void Execute(string query, DAE.Runtime.DataParams paramsValue, Action<DAE.Runtime.Data.IRow> action)
        {
            IServerCursor cursor = this.OpenCursor(query, paramsValue);
            
            try
            {
                DAE.Runtime.Data.IRow row = cursor.Plan.RequestRow();
                try
                {
                    while (cursor.Next())
                    {
                        cursor.Select(row);
                        action(row);
                    }
                }
                finally
                {
                    cursor.Plan.ReleaseRow(row);
                }
            }
            finally
            {
                CloseCursor(cursor);
            }          
        }

	    public IServerCursor OpenCursor(string query, DAE.Runtime.DataParams paramsValue)
		{
			Cursor oldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				IServerExpressionPlan plan = _utilityProcess.PrepareExpression(query, paramsValue);
				try
				{
					return plan.Open(paramsValue);
				}
				catch
				{
					_utilityProcess.UnprepareExpression(plan);
					throw;
				}
			}
			finally
			{
				Cursor.Current = oldCursor;
			}
		}

		public void CloseCursor(IServerCursor cursor)
		{
			IServerExpressionPlan plan = cursor.Plan;
			plan.Close(cursor);
			_utilityProcess.UnprepareExpression(plan);
		}

		public DAE.Runtime.Data.IDataValue EvaluateQuery(string query)
		{
			return EvaluateQuery(query, null);
		}

		public DAE.Runtime.Data.IDataValue EvaluateQuery(string query, DAE.Runtime.DataParams paramsValue)
		{
			Cursor oldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				var plan = _utilityProcess.PrepareExpression(query, paramsValue);
				DAE.Runtime.Data.IDataValue result;
				try
				{
					result = plan.Evaluate(paramsValue);
				}
				finally
				{
					_utilityProcess.UnprepareExpression(plan);				
				}
				return result;
			}
			finally
			{
				Cursor.Current = oldCursor;
			}
		}

		public bool DocumentExists(string libraryName, string documentName)
		{
			using 
			(
				DAE.Runtime.Data.Scalar documentExistsData = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							@".Frontend.DocumentExists('{0}', '{1}')",
							DAE.Schema.Object.EnsureRooted(libraryName),
							DAE.Schema.Object.EnsureRooted(documentName)
						)
					)
			)
			{
				return documentExistsData.AsBoolean;
			}
		}

		public void CheckDocumentOverwrite(string libraryName, string documentName)
		{
			if (DocumentExists(libraryName, documentName))
				if 
				(
					MessageBox.Show
					(
						String.Format
						(
							Strings.SaveAsDialogReplaceText,
							libraryName,
							documentName
						), 
						Strings.SaveAsDocumentFormTitle, 
						MessageBoxButtons.YesNo, 
						MessageBoxIcon.Warning, 
						MessageBoxDefaultButton.Button1
					) != DialogResult.Yes
				)
					throw new AbortException();
		}

		public void EnsureFrontendRegistered()
		{
			ExecuteScript
			(
				@"
					begin
						var LLibraryName := LibraryName();
						try
							System.EnsureLibraryRegistered('Frontend');
							System.UpgradeLibrary('Frontend');
						finally
							SetLibrary(LLibraryName);
						end;
					end;
				"
			);
		}
		
		public void EnsureSecurityRegistered()
		{
			ExecuteScript
			(
				@"
					begin
						var LLibraryName := LibraryName();
						try
							System.EnsureLibraryRegistered('Security');
						finally
							SetLibrary(LLibraryName);
						end;
					end;
				"
			);
		}

		public string GetCurrentLibraryName()
		{
			using (DAE.Runtime.Data.Scalar scalar = (DAE.Runtime.Data.Scalar)EvaluateQuery("LibraryName()"))
			{
				return scalar.AsDisplayString;
			}
		}

		public string GetDefaultDocumentType(IDesigner designer)
		{
			using 
			(
				DAE.Runtime.Data.Scalar defaultTypeData = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							@".Frontend.GetDefaultDocumentType('{0}')",
							designer.DesignerID
						)
					)
			)
			{
				return defaultTypeData.AsString;
			}
		}

		#endregion

		#region Warnings

		/// <summary> Provides access to the warnings / errors list pane. </summary>
		public ErrorListView Warnings
		{
			get
			{
				return _errorListView; 
			}
		}

		private void ShowWarnings()
		{
			_dockContentErrorListView.Show(FDockPanel);
			_dockContentErrorListView.Activate();								   
		}

		private void WarningsAdded(object sender, EventArgs args)
		{
			ShowWarnings();
		}

		private void ErrorsAdded(object sender, EventArgs args)
		{
			ShowWarnings();
			_dockContentErrorListView.Show(FDockPanel);			
		}

		private void ClearWarnings()
		{
			_errorListView.ClearErrors();
		}

		// IErrorSource

		private DebugLocator GetInnermostLocator(Exception exception)
		{
			var inner = exception != null ? GetInnermostLocator(exception.InnerException) : null;
			if (inner != null)
				return inner;
			var locator = exception as ILocatorException;
			if (locator != null && locator.Line > -1 && locator.LinePos > -1)
				return new DebugLocator(locator.Locator, locator.Line, locator.LinePos);
			else
				return null;
		}
		
		public bool LocateToError(Exception exception)
		{
			var locator = GetInnermostLocator(exception);
			if (locator != null)
			{
				try
				{
					OpenLocator(locator);
					return true;
				}
				catch
				{
					// ignore exceptions trying to locate, the locator may no longer even be valid
					return false;
				}
			}
			return false;
		}
		
		void IErrorSource.ErrorHighlighted(Exception exception)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception exception)
		{
			LocateToError(exception);
		}

		#endregion

		#region Commands

		private EventHandler _onFormDesignerLibrariesChanged;

		public event EventHandler OnFormDesignerLibrariesChanged {
			add {
				this._onFormDesignerLibrariesChanged += value;
			}
			remove {
				this._onFormDesignerLibrariesChanged -= value;
			}
		}

		private void BrowseDesignerLibraries()
		{
			IWindowsFormInterface form = FrontendSession.LoadForm(null, ".Frontend.Derive('FormDesignerLibraries')");
			try
			{
				form.ShowModal(FormMode.None);
			}
			finally
			{
				form.HostNode.Dispose();
			}
			if (_onFormDesignerLibrariesChanged != null)
				_onFormDesignerLibrariesChanged(this, EventArgs.Empty);
		}

		private void BrowseDocumentTypes()
		{
			IWindowsFormInterface form = FrontendSession.LoadForm(null, ".Frontend.Derive('DocumentTypes')", null);
			try
			{
				form.ShowModal(FormMode.None);
			}
			finally
			{
				form.HostNode.Dispose();
			}
		}

		private void Documentation()
		{
			string fileName = GetHelpFileName();
			try 
			{
				System.Diagnostics.Process.Start(fileName);
			}
			catch (Exception exception)
			{
				Program.HandleException(new DataphoriaException(DataphoriaException.Codes.UnableToOpenHelp, exception, fileName));
			}
		}

		private void About()
		{
			using (AboutForm aboutForm = new AboutForm())
			{
				aboutForm.ShowDialog(this);
			}
		}

		private void LaunchForm()
		{
			Frontend.Client.Windows.Session session = GetLiveDesignableFrontendSession();
			try
			{
				session.SetFormDesigner();
				IWindowsFormInterface form = session.LoadForm(null, ".Frontend.Form('.Frontend', 'DerivedFormLauncher')");
				try
				{
					form.Show();
				}
				catch
				{
					form.HostNode.Dispose();
					throw;
				}
			}
			catch
			{
				session.Dispose();
				throw;
			}
		}

		public void ShowDataphorExplorer()
		{
			_dockContentExplorer.Show(FDockPanel);
			_dockContentExplorer.Activate();			
		}

		public void LaunchAlphoraWebsite()
		{
			System.Diagnostics.Process.Start(@"http://www.alphora.com/");
		}

		public void LaunchWebDocumentation()
		{
			System.Diagnostics.Process.Start(@"http://docs.alphora.com/");
		}

		public void LaunchAlphoraGroups()
		{
			System.Diagnostics.Process.Start(@"http://news.alphora.com/");
		}

		private void FToolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs args)
		{
			switch (args.ClickedItem.Name)
			{
				case "FClearWarningsToolStripMenuItem" :
					ClearWarnings();
					break;
				case "FConnectToolStripMenuItem" :
					EnsureServerConnection();
					break;
				case "FDisconnectToolStripMenuItem" :
					Disconnect();
					break;
				case "FNewToolStripMenuItem" :
				case "FNewToolStripButton" :
					NewDesigner();
					break;
				case "FNewScriptToolStripMenuItem" :
				case "FNewScriptToolStripButton" :
					NewEditor(String.Empty, "d4");
					break;
				case "FOpenFileToolStripMenuItem" :
				case "FOpenFileToolStripButton" :
					OpenFile();
					break;
				case "FOpenFileWithToolStripMenuItem" :
				case "FOpenFileWithToolStripButton" :
					OpenFileWith();
					break;
				case "FSaveAllToolStripMenuItem" :
					SaveAll();
					break;
				case "FLaunchFormToolStripMenuItem" :
				case "FLaunchFormToolStripButton" :
					LaunchForm();
					break;
				case "FExitToolStripMenuItem" :
					Close();
					break;
				case "FDataphorExplorerToolStripMenuItem" :
					ShowDataphorExplorer();
					break;
				case "FWarningsErrorsToolStripMenuItem" :
					ShowWarnings();
					break;
				case "FDesignerLibrariesToolStripMenuItem" :
					BrowseDesignerLibraries();
					break;
				case "FDataphorDocumentationToolStripMenuItem" :
					Documentation();
					break;
				case "FAboutToolStripMenuItem" :
					About();
					break;
				case "FAlphoraWebSiteToolStripMenuItem" :
					LaunchAlphoraWebsite();
					break;
				case "FWebDocumentationToolStripMenuItem" :
					LaunchWebDocumentation();
					break;
				case "FAlphoraDiscussionGroupsToolStripMenuItem" :
					LaunchAlphoraGroups();
					break;
				case "FDocumentTypesToolStripMenuItem" :
					BrowseDocumentTypes(); 
					break;
			}
		}

		private void ClearWarningsClicked(object sender, System.EventArgs args)
		{
			ClearWarnings();
		}

		#endregion

		#region Debugger

		private Debugger _debugger;
		public Debugger Debugger { get { return _debugger; } }

		private void CreateDebugger()
		{
			if (_debugger == null)
			{
				_debugger = new Debugger(this);
				_debugger.PropertyChanged += DebuggerPropertyChanged;
			}
		}

		private void DebuggerPropertyChanged(object sender, string[] propertyNames)
		{
			if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "IsStarted" || AItem == "IsPaused"; }))
 			{
 				UpdateDebuggerState();
 				UpdateBreakOnException();
 				UpdateBreakOnStart();
 			}
			else 
			{
			if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "BreakOnException"; }))
				UpdateBreakOnException();
				else if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "BreakOnStart"; }))
					UpdateBreakOnStart();
		}
			if (Array.Exists<string>(propertyNames, (string AItem) => { return AItem == "CurrentLocation"; }))
				EnsureEditorForCurrentLocation();
			
		}

		private void EnsureEditorForCurrentLocation()
		{
			if (Debugger.CurrentLocation != null && !String.IsNullOrEmpty(Debugger.CurrentLocation.Locator))
			{
				try
				{
					OpenLocator(Debugger.CurrentLocation);
				}
				catch (Exception exception)
				{
					// Log errors, don't pop-up
					Warnings.AppendError(null, exception, false);
				}
			}
		}

		public void OpenLocator(DebugLocator locator)
		{
			DesignerInfo info;
			DesignBuffer buffer = DesignBufferFromLocator(out info, locator);
			if (buffer != null)
			{
				IDesigner designer = this.GetDesigner(buffer);
				if (designer != null)
				{
					designer.Service.RequestLocate(locator);
					designer.Select();
				}
				else
					OpenDesigner(info, buffer);
			}
		}

		public DesignBuffer DesignBufferFromLocator(out DesignerInfo info, DebugLocator locator)
		{
			info = new DesignerInfo();
			DesignBuffer buffer = null;
			
			// TODO: Introduce a buffer factory for locators
			
			// Files
			if (FileDesignBuffer.IsFileLocator(locator.Locator))
			{
				var fileBuffer = new FileDesignBuffer(this, locator);
				info = GetDefaultDesigner(Program.DocumentTypeFromFileName(fileBuffer.FileName));
				buffer = fileBuffer;
			}
			// Documents
			else if (DocumentDesignBuffer.IsDocumentLocator(locator.Locator))
			{
				var documentBuffer = new DocumentDesignBuffer(this, locator);
				info = GetDefaultDesigner(GetDocumentType(documentBuffer.LibraryName, documentBuffer.DocumentName));
				buffer = documentBuffer;
			}
			else if (ProgramDesignBuffer.IsProgramLocator(locator.Locator))
			{
				buffer = new ProgramDesignBuffer(this, locator);
				info = GetDefaultDesigner("d4");
			}
			
			return buffer;
		}

		private void DebugMenuItemClicked(object sender, ToolStripItemClickedEventArgs args)
		{
			switch (args.ClickedItem.Name)
			{
				case "FDebugStopMenuItem" :
				case "FDebugStopButton" :
					Debugger.Stop();
					break;
				case "FDebugPauseMenuItem":
				case "FDebugPauseButton":
					Debugger.Pause();
					break;
				case "FDebugRunMenuItem":
				case "FDebugRunButton":
					Debugger.Run();
					break;
				case "FDebugStepOverMenuItem":
				case "FDebugStepOverButton":
					Debugger.StepOver();
					break;
				case "FDebugStepIntoMenuItem":
				case "FDebugStepIntoButton":
					Debugger.StepInto();
					break;
				case "FBreakOnExceptionMenuItem":
				case "FBreakOnExceptionButton":
					Debugger.BreakOnException = !Debugger.BreakOnException;
					break;
				case "FBreakOnStartMenuItem":
				case "FBreakOnStartButton":
					Debugger.BreakOnStart = !Debugger.BreakOnStart;
					break;
				case "FViewSessionsMenuItem":
				case "FViewSessionsButton" :
					EnsureSessionView();
					_dockContentSessionsView.Show(FDockPanel);
					break;
				case "FViewProcessesMenuItem":
				case "FViewProcessesButton":
					EnsureProcessesView();
					_dockContentProcessesView.Show(FDockPanel);
					break;
				case "FViewDebugProcessesMenuItem" :
				case "FViewDebugProcessesButton":
					EnsureDebugProcessesView();
					_dockContentDebugProcessesView.Show(FDockPanel);
					break;
				case "FViewCallStackMenuItem":
				case "FViewCallStackButton" :
					EnsureCallStackView();
					_dockContentCallStackView.Show(FDockPanel);
					break;
				case "FViewStackMenuItem":
				case "FViewStackButton":
					EnsureStackView();
					_dockContentStackView.Show(FDockPanel);
					break;
			}
		}

		private void UpdateDebuggerState()
		{
			FDebugStopButton.Enabled = IsConnected && Debugger.IsStarted;
			FDebugStopMenuItem.Enabled = FDebugStopButton.Enabled;
			FDebugPauseButton.Enabled = IsConnected && Debugger.IsStarted && !Debugger.IsPaused;
			FDebugPauseMenuItem.Enabled = FDebugPauseButton.Enabled;
			FViewSessionsButton.Enabled = IsConnected;
			FViewSessionsMenuItem.Enabled = FViewSessionsButton.Enabled;
			FViewProcessesButton.Enabled = IsConnected;
			FViewProcessesMenuItem.Enabled = FViewProcessesButton.Enabled;
			FViewDebugProcessesButton.Enabled = IsConnected && Debugger.IsStarted;
			FViewDebugProcessesMenuItem.Enabled = FViewDebugProcessesButton.Enabled;
			FViewCallStackButton.Enabled = IsConnected && Debugger.IsStarted;
			FViewCallStackMenuItem.Enabled = FViewCallStackButton.Enabled;
			FDebugRunButton.Enabled = IsConnected && Debugger.IsPaused;
			FDebugRunMenuItem.Enabled = FDebugRunButton.Enabled;
			FDebugStepOverButton.Enabled = IsConnected && Debugger.IsPaused;
			FDebugStepOverMenuItem.Enabled = FDebugStepOverButton.Enabled;
			FDebugStepIntoButton.Enabled = IsConnected && Debugger.IsPaused;
			FDebugStepIntoMenuItem.Enabled = FDebugStepIntoButton.Enabled;
			FViewStackButton.Enabled = IsConnected && Debugger.IsPaused;
			FViewStackMenuItem.Enabled = FViewStackButton.Enabled;
		}

		private void UpdateBreakOnException()
		{
			FBreakOnExceptionButton.Enabled = IsConnected;
			FBreakOnExceptionMenuItem.Enabled = FBreakOnExceptionButton.Enabled;
			FBreakOnExceptionButton.Checked = IsConnected && Debugger.BreakOnException;
			FBreakOnExceptionMenuItem.Checked = FBreakOnExceptionButton.Checked;
		}

		private void UpdateBreakOnStart()
		{
			FBreakOnStartButton.Enabled = IsConnected;
			FBreakOnStartMenuItem.Enabled = FBreakOnStartButton.Enabled;
			FBreakOnStartButton.Checked = IsConnected && Debugger.BreakOnStart;
			FBreakOnStartMenuItem.Checked = FBreakOnStartButton.Checked;
		}

		private void EnsureSessionView()
		{
			if (_sessionsView == null)
			{
				_sessionsView = new SessionsView();
				_sessionsView.Dataphoria = this;
				_sessionsView.Name = "FSessionView";
				_sessionsView.Dock = DockStyle.Fill;

				_dockContentSessionsView = new DockContent();
				_dockContentSessionsView.HideOnClose = true;
				_dockContentSessionsView.Controls.Add(_sessionsView);
				_dockContentSessionsView.TabText = "Sessions";
				_dockContentSessionsView.Text = "Sessions - Dataphoria";
				_dockContentSessionsView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureProcessesView()
		{
			if (_processesView == null)
			{
				_processesView = new ProcessesView();
				_processesView.Dataphoria = this;
				_processesView.Name = "FProcessesView";
				_processesView.Dock = DockStyle.Fill;

				_dockContentProcessesView = new DockContent();
				_dockContentProcessesView.HideOnClose = true;
				_dockContentProcessesView.Controls.Add(_processesView);
				_dockContentProcessesView.TabText = "Processes";
				_dockContentProcessesView.Text = "Processes - Dataphoria";
				_dockContentProcessesView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureDebugProcessesView()
		{
			if (_debugProcessesView == null)
			{
				_debugProcessesView = new DebugProcessesView();
				_debugProcessesView.Dataphoria = this;
				_debugProcessesView.Name = "FDebugProcessesView";
				_debugProcessesView.Dock = DockStyle.Fill;

				_dockContentDebugProcessesView = new DockContent();
				_dockContentDebugProcessesView.HideOnClose = true;
				_dockContentDebugProcessesView.Controls.Add(_debugProcessesView);
				_dockContentDebugProcessesView.TabText = "Debug Processes ";
				_dockContentDebugProcessesView.Text = "Debug Processes - Dataphoria";
				_dockContentDebugProcessesView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureCallStackView()
		{
			if (_callStackView == null)
			{
				_callStackView = new CallStackView();
				_callStackView.Dataphoria = this;
				_callStackView.Name = "FCallStackView";
				_callStackView.Dock = DockStyle.Fill;

				_dockContentCallStackView = new DockContent();
				_dockContentCallStackView.HideOnClose = true;
				_dockContentCallStackView.Controls.Add(_callStackView);
				_dockContentCallStackView.TabText = "Call Stack";
				_dockContentCallStackView.Text = "Call Stack - Dataphoria";
				_dockContentCallStackView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureStackView()
		{
			if (_stackView == null)
			{
				_stackView = new StackView();
				_stackView.Dataphoria = this;
				_stackView.Name = "_stackView";
				_stackView.Dock = DockStyle.Fill;

				_dockContentStackView = new DockContent();
				_dockContentStackView.HideOnClose = true;
				_dockContentStackView.Controls.Add(_stackView);
				_dockContentStackView.TabText = "Stack";
				_dockContentStackView.Text = "Stack - Dataphoria";
				_dockContentStackView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		#endregion
				
		#region Help

		public const string DefaultHelpFileName = @"..\Documentation\Dataphor.chm";

		private string GetHelpFileName()
		{
			return Path.Combine(Application.StartupPath, (string)_settings.GetSetting("HelpFileName", typeof(string), DefaultHelpFileName));
		}

		public void InvokeHelp(string keyword)
		{
			Help.ShowHelp(null, GetHelpFileName(), HelpNavigator.KeywordIndex, keyword.Trim());
		}

		protected override void OnHelpRequested(HelpEventArgs args)
		{
			base.OnHelpRequested(args);
			InvokeHelp("Dataphoria");
		}

		private void FErrorListView_HelpRequested(object sender, HelpEventArgs args)
		{
			string keyword = "Errors and Warnings";
			DataphorException exception = _errorListView.SelectedError as DataphorException;
			if (exception != null)
				keyword = exception.Code.ToString();

			InvokeHelp(keyword);
		}

		private void FExplorer_HelpRequested(object sender, HelpEventArgs hlpEvent)
		{
			InvokeHelp("Dataphor Explorer");
		}

		#endregion
	}
}
