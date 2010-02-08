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

namespace Alphora.Dataphor.Dataphoria
{
	public partial class Dataphoria : Form, IDataphoria
	{
		public const string CConfigurationFileName = "Dataphoria{0}.config";

		public Dataphoria()
		{
			InitializeComponent();

			ICSharpCode.TextEditor.Document.HighlightingManager.Manager.AddSyntaxModeFileProvider(new ICSharpCode.TextEditor.Document.FileSyntaxModeProvider(PathUtility.GetBinDirectory()));

			FServices.Add(typeof(DAE.Client.Controls.Design.IPropertyTextEditorService), new PropertyTextEditorService());

			CreateDebugger();

			FExplorer = new DataTree();
			FExplorer.AllowDrop = true;
			FExplorer.BorderStyle = BorderStyle.None;
			FExplorer.CausesValidation = false;
			FExplorer.HideSelection = false;
			FExplorer.ImageIndex = 0;
			FExplorer.ImageList = FTreeImageList;
			FExplorer.Name = "FExplorer";
			FExplorer.SelectedImageIndex = 0;
			FExplorer.ShowRootLines = false;			
			FExplorer.TabIndex = 1;
			FExplorer.HelpRequested += FExplorer_HelpRequested;
			FExplorer.Dataphoria = this;
			FExplorer.Select();
			FExplorer.Dock = DockStyle.Fill;

			FDockContentExplorer = new DockContent();
			FDockContentExplorer.Controls.Add(FExplorer);
			FDockContentExplorer.HideOnClose = true;
			FDockContentExplorer.TabText = "Dataphor Explorer";
			FDockContentExplorer.Text = "Dataphor Explorer - Dataphoria";
			FDockContentExplorer.ShowHint = DockState.DockLeft;

			FErrorListView = new ErrorListView();
			FErrorListView.OnErrorsAdded += ErrorsAdded;
			FErrorListView.OnWarningsAdded += WarningsAdded;
			FErrorListView.Dock = DockStyle.Fill;

			FDockContentErrorListView = new DockContent();
			FDockContentErrorListView.HideOnClose = true;
			FDockContentErrorListView.Controls.Add(FErrorListView);
			FDockContentErrorListView.TabText = "Errors/Warnings ";	// HACK: Space is to work around last character being cut-off in tab
			FDockContentErrorListView.Text = "Errors/Warnings - Dataphoria";
			FDockContentErrorListView.ShowHint = DockState.DockBottomAutoHide;

			FDockContentExplorer.Show(this.FDockPanel);
			FDockContentErrorListView.Show(this.FDockPanel);

			FDockPanel.DockLeftPortion = 240;
			FDockPanel.DockRightPortion = 240;
			FDockPanel.DockTopPortion = 240;
			FDockPanel.DockBottomPortion = 240;

			LoadSettings();
		}

		#if TRACEFOCUS

		private static string WalkParentControls(Control AControl)
		{
			if (AControl == null)
				return "";
			else
				return 
					WalkParentControls(AControl.Parent)
						+ "->"
						+ (AControl.Parent != null ? "(" + AControl.Parent.Controls.IndexOf(AControl).ToString() + ")" : "")
						+ (AControl.Name != null ? AControl.Name : "")
						+ (AControl.Text != null ? "\"" + AControl.Name + "\"" : "")
						+ "[" + AControl.GetType().Name + "]";
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", CharSet=System.Runtime.InteropServices.CharSet.Auto, ExactSpelling=true)]
		public static extern IntPtr GetFocus();
 
		private bool ProcessQueryFocus(Form AForm, Keys AKey)
		{
			IntPtr LFocusPtr = GetFocus();
			if (LFocusPtr != IntPtr.Zero)
			{
				Control LControl = Control.FromHandle(LFocusPtr);
				if (LControl != null)
					System.Diagnostics.Trace.WriteLine("Focus: " + WalkParentControls(LControl));
			}
			return true;
		}

		protected override bool ProcessDialogKey(Keys AKeyData)
		{
			if (AKeyData == (Keys.Control | Keys.Shift | Keys.Alt | Keys.F))
				ProcessQueryFocus(this, AKeyData);
			return base.ProcessDialogKey(AKeyData);
		}
		
		#endif

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			// HACK: Something in the WinForms validation process is returning false.  We don't care so always make sure Cancel is false at the beginning of OnClosing
			AArgs.Cancel = false;
			base.OnClosing(AArgs);
		}

		private DataTree FExplorer;
		private ErrorListView FErrorListView;
		private SessionsView FSessionsView;
		private CallStackView FCallStackView;
		private DebugProcessesView FDebugProcessesView;
		private ProcessesView FProcessesView;
		private StackView FStackView;

		private DockContent FDockContentExplorer;
		private DockContent FDockContentErrorListView;
		private DockContent FDockContentSessionsView;
		private DockContent FDockContentCallStackView;
		private DockContent FDockContentDebugProcessesView;
		private DockContent FDockContentProcessesView;
		private DockContent FDockContentStackView;

		#region Settings

		private Settings FSettings;
		public Settings Settings { get { return FSettings; } }

		// when the form state is saved when maximized, the restore size needs to be remembered
		private Rectangle FNormalBounds = Rectangle.Empty;

		public string GetConfigurationFileName(string AType)
		{
			return Path.Combine(PathUtility.UserAppDataPath(), String.Format(CConfigurationFileName, AType));
		}

		private ServerNode FServerNode;
		
		public void RefreshLibraries()
		{
			FServerNode.Build();
			FServerNode.LibraryNode.Refresh();
		}
		
		public void RefreshDocuments(string ALibraryName)
		{
			FServerNode.Build();
			LibraryNode LLibraryNode = (LibraryNode)FServerNode.LibraryNode.FindByText(ALibraryName);
			if ((LLibraryNode != null) && (LLibraryNode.DocumentListNode != null))
				LLibraryNode.DocumentListNode.Refresh();
		}

		protected override void OnLoad(EventArgs AArgs)
		{
			base.OnLoad(AArgs);
			RestoreBoundsAndState();
		}

		private void RestoreBoundsAndState()
		{
			try
			{
				Rectangle LBounds = SystemInformation.WorkingArea;
				LBounds = (Rectangle)FSettings.GetSetting("Dataphoria.Bounds", typeof(Rectangle), LBounds);
				FNormalBounds = LBounds;
				Bounds = LBounds;

				FormWindowState LState = (FormWindowState)FSettings.GetSetting("Dataphoria.WindowState", typeof(FormWindowState), FormWindowState.Normal);
				if (LState == FormWindowState.Minimized)  // don't start minimized, because it gets messed up
					LState = FormWindowState.Normal;
				WindowState = LState;
			}
			catch (Exception LException)
			{
				Warnings.AppendError(this, LException, true);
				// don't rethrow
			}
		}

		private void LoadSettings()
		{
			try
			{
				string LFileName = GetConfigurationFileName(String.Empty);
				// Load configuration settings
				try
				{
					FSettings = new Settings(LFileName);
				}
				catch
				{
					FSettings = new Settings();
					throw;
				}

			}
			catch (Exception LException)
			{
				Warnings.AppendError(this, LException, true);
				// don't rethrow
			}
		}

		private void SaveSettings()
		{
			if (FSettings != null)
			{
				// Save the configuration settings
				FSettings.SetSetting("Dataphoria.WindowState", WindowState);
				if (WindowState == FormWindowState.Normal)
					FSettings.SetSetting("Dataphoria.Bounds", Bounds);
				else
				{
					if (FNormalBounds != Rectangle.Empty)
						FSettings.SetSetting("Dataphoria.Bounds", FNormalBounds);
				}
				FSettings.SaveSettings(GetConfigurationFileName(String.Empty));
			}
		}

		protected override void OnSizeChanged(EventArgs AArgs)
		{
			if (WindowState == FormWindowState.Normal)
				FNormalBounds = Bounds;
			base.OnSizeChanged(AArgs);
		}

		#endregion

		#region Connection

		private DataSession FDataSession;
		public DataSession DataSession { get { return FDataSession; } }
		
		private Frontend.Client.Windows.Session FFrontendSession;
		public Frontend.Client.Windows.Session FrontendSession { get { return FFrontendSession; } }
		
		private IServerProcess FUtilityProcess;
		public IServerProcess UtilityProcess { get { return FUtilityProcess; } }

		public void EnsureServerConnection()
		{
			if (FDataSession == null)
			{
				InstanceConfiguration LInstanceConfiguration = InstanceManager.LoadConfiguration();
				ServerConfiguration LDefaultInstance = LInstanceConfiguration.Instances[Engine.CDefaultServerName];
				if (LInstanceConfiguration.Instances.Count == 0)
				{
					LDefaultInstance = ServerConfiguration.DefaultLocalInstance();
					LInstanceConfiguration.Instances.Add(LDefaultInstance);
					InstanceManager.SaveConfiguration(LInstanceConfiguration);
				}
				
				AliasConfiguration LConfiguration = AliasManager.LoadConfiguration();
				if (LConfiguration.Aliases.Count == 0)
				{
					InProcessAlias LAlias = new InProcessAlias();
					LAlias.Name = LDefaultInstance == null ? ServerConfiguration.CDefaultLocalInstanceName : LDefaultInstance.Name;
					LAlias.InstanceName = LDefaultInstance == null ? ServerConfiguration.CDefaultLocalInstanceName : LDefaultInstance.Name;
					LConfiguration.Aliases.Add(LAlias);
				}

				ServerConnectForm.Execute(LConfiguration);

				using (var LStatusForm = new StatusForm(Strings.Connecting))
				{
					FDataSession = new DataSession();
					try
					{
						FDataSession.AliasName = LConfiguration.DefaultAliasName;
						FDataSession.SessionInfo.Environment = "WindowsClient";
						FDataSession.Open();

						FUtilityProcess = FDataSession.ServerSession.StartProcess(new ProcessInfo(FDataSession.ServerSession.SessionInfo));
						try
						{
							EnsureFrontendRegistered();
							
							FFrontendSession = new Frontend.Client.Windows.Session(FDataSession, false);
							try
							{
								FFrontendSession.SetLibrary("Frontend");
								FFrontendSession.OnDeserializationErrors += FrontendSessionDeserializationErrors;
								FServerNode = new ServerNode(FDataSession.Server != null);
								FServerNode.Text = FDataSession.Alias.ToString();
								FExplorer.AddBaseNode(FServerNode);
								try
								{
									OnConnected(EventArgs.Empty);
									FServerNode.Expand();
									FConnectToolStripMenuItem.Visible = false;
									FDisconnectToolStripMenuItem.Visible = true;									
									Text = Strings.DataphoriaTitle + " - " + FDataSession.Alias;
									FDockContentExplorer.Show(FDockPanel);
									FExplorer.Focus();
								}
								catch
								{
									FServerNode = null;
									FExplorer.Nodes.Clear();
									throw;
								}
							}
							catch
							{
								FFrontendSession.Dispose();
								FFrontendSession = null;
								throw;
							}
						}
						catch
						{
							if (FUtilityProcess != null)
							{
								FDataSession.ServerSession.StopProcess(FUtilityProcess);
								FUtilityProcess = null;
							}
							throw;
						}
					}
					catch
					{						
						FDataSession.Dispose();
						FDataSession = null;						
						throw;
					}
				}
			}
		}

		public event EventHandler Connected;

		private void OnConnected(EventArgs AArgs)
		{
			if (Connected != null)
				Connected(this, AArgs);
		}

		private void Dataphoria_Shown(object ASender, EventArgs AArgs)
		{
			EnsureServerConnection();
		}

		private void InternalDisconnect()
		{
			using (StatusForm LStatusForm = new StatusForm(Strings.Disconnecting))
			{
				if (FDataSession != null)
				{
					try
					{
						Text = Strings.DataphoriaTitle;
						FConnectToolStripMenuItem.Visible = true;
						FDisconnectToolStripMenuItem.Visible = false;						
						FDockContentExplorer.Hide();

						FExplorer.Nodes.Clear();
						FServerNode = null;
						
						OnDisconnected(EventArgs.Empty);
					}
					finally
					{
						try
						{
							if (FUtilityProcess != null)
							{
								FDataSession.ServerSession.StopProcess(FUtilityProcess);
								FUtilityProcess = null;
							}
						}
						finally
						{
							try
							{
								if (FFrontendSession != null)
								{
									FFrontendSession.Dispose();	// Will dispose the connection
									FFrontendSession = null;
								}
							}
							finally
							{
								if (FDataSession != null)
								{
									DataSession LDataSession = FDataSession;
									FDataSession = null;
									LDataSession.Dispose(); // Set the DataSession null before the close to ensure the Dataphoria appears disconnected and cleanup executes properly.
								}
							}							
						}
					}
				}
			}
		}

		public event EventHandler Disconnected;
		
		private void OnDisconnected(EventArgs AArgs)
		{
			if (Disconnected != null)
				Disconnected(this, AArgs);
		}

		public void Disconnect()
		{
			// Make sure we can safely disconnect
			CloseChildren();
			if (MdiChildren.Length > 0)
				throw new AbortException();

			InternalDisconnect();
		}

		private void FrontendSessionDeserializationErrors(IHost AHost, ErrorList AErrors)
		{
			IErrorSource LSource = null;
			
			if ((AHost != null) && (AHost.Children.Count > 0))
				LSource = AHost.Children[0] as IErrorSource;

			Warnings.AppendErrors(LSource, AErrors, true);
		}
		
		public bool IsConnected
		{
			get { return FDataSession != null; }
		}

		#endregion

		#region Live Designer Support

		private Bitmap LoadBitmap(string AResourceName)
		{
			Stream LManifestResourceStream = GetType().Assembly.GetManifestResourceStream(AResourceName);
			if (LManifestResourceStream != null)
			{
				var LResult = new Bitmap(LManifestResourceStream);
				LResult.MakeTransparent();
				return LResult;
			}
			throw new ArgumentException("Could not get manifest resource stream for: " + AResourceName, "AResourceName");
		}

		public Frontend.Client.Windows.Session GetLiveDesignableFrontendSession()
		{
			Frontend.Client.Windows.Session LSession = new Frontend.Client.Windows.Session(FDataSession, false);
			LSession.AfterFormActivate += AfterFormActivated;
			LSession.OnDeserializationErrors += FrontendSessionDeserializationErrors;
			return LSession;
		}

		private Hashtable FDesignedForms = new Hashtable();

		private void AfterFormActivated(IFormInterface AInterface)
		{
			AInterface.HostNode.OnDocumentChanged += HostDocumentChanged;
			UpdateDesignerActions(AInterface);
		}

		private void HostDocumentChanged(object ASender, EventArgs AArgs)
		{
			UpdateDesignerActions((IFormInterface)((IHost)ASender).Children[0]);
		}

		private void UpdateDesignerActions(IFormInterface AInterface)
		{
			IWindowsFormInterface LForm = (IWindowsFormInterface)AInterface;

			LForm.ClearCustomActions();

			DocumentExpression LExpression = new DocumentExpression(AInterface.HostNode.Document);

			if (LExpression.Type != DocumentType.None)
				LForm.AddCustomAction
				(
					Strings.CustomizeMenuText, 
					LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Customize.bmp"), 
					CustomizeForm
				);

			LForm.AddCustomAction
			(
				Strings.EditCopyMenuText, 
				LoadBitmap("Alphora.Dataphor.Dataphoria.Images.EditCopy.bmp"), 
				EditCopyForm
			);

			if (LExpression.Type == DocumentType.Document)
			{
				LForm.AddCustomAction
				(
					Strings.EditMenuText,  
					LoadBitmap("Alphora.Dataphor.Dataphoria.Images.Edit.bmp"), 
					EditForm
				);
			}
		}

		private void CheckExclusiveDesigner(IFormInterface AInterface)
		{
			if (FDesignedForms[AInterface] != null) 
				throw new DataphoriaException(DataphoriaException.Codes.SingleDesigner);
		}

		public void AddDesignerForm(IFormInterface AInterface, IDesigner ADesigner)
		{
			FDesignedForms.Add(AInterface, ADesigner);
			ADesigner.Disposed += DesignerDisposed;
		}

		private void EditForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			DocumentExpression LExpression = Program.GetDocumentExpression(AInterface.HostNode.Document);
			string LDocumentType;
			using 
			(
				DAE.Runtime.Data.Scalar LDocumentTypeValue = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							".Frontend.GetDocumentType('{0}', '{1}')", 
							LExpression.DocumentArgs.LibraryName, 
							LExpression.DocumentArgs.DocumentName
						)
					)
			)
			{
				LDocumentType = LDocumentTypeValue.AsString;
			}

			ILiveDesigner LDesigner;
			switch (LDocumentType)
			{
				case "dfd" : LDesigner = new FormDesigner.FormDesigner(this, "DFD"); break;
				case "dfdx" : LDesigner = new FormDesigner.CustomFormDesigner(this, "DFDX"); break;
				default : throw new DataphoriaException(DataphoriaException.Codes.DocumentTypeLiveEditNotSupported, LDocumentType);
			}
			try
			{
				LDesigner.Open(AInterface.HostNode);
				LDesigner.Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void EditCopyForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			FormDesigner.FormDesigner LDesigner = new FormDesigner.FormDesigner(this, "DFD");
			try
			{
				LDesigner.New(AInterface.HostNode);
				((IDesigner)LDesigner).Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void CustomizeForm(IFormInterface AInterface)
		{
			CheckExclusiveDesigner(AInterface);

			FormDesigner.CustomFormDesigner LDesigner = new FormDesigner.CustomFormDesigner(this, "DFDX");
			try
			{
				LDesigner.New(AInterface.HostNode);
				((IDesigner)LDesigner).Show();
				
				AddDesignerForm(AInterface, LDesigner);
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		private void DesignerDisposed(object ASender, EventArgs AArgs)
		{
			// Remove the designer once it is closed
			IDictionaryEnumerator LEnumerator = FDesignedForms.GetEnumerator();
			while (LEnumerator.MoveNext())
				if (LEnumerator.Value == ASender)
				{
					FDesignedForms.Remove(LEnumerator.Key);
					break;
				}
		}

		#endregion

		#region Designer support

		private Hashtable FDesigners = new Hashtable();
		
		public void CheckNotRegistered(DesignBuffer ABuffer)
		{
			if (FDesigners[ABuffer] != null)
				throw new DataphoriaException(DataphoriaException.Codes.AlreadyDesigning, ABuffer.GetDescription());
		}

		public void RegisterDesigner(DesignBuffer ABuffer, IDesigner ADesigner)
		{
			CheckNotRegistered(ABuffer);
			FDesigners.Add(ABuffer, ADesigner);
		}

		public void UnregisterDesigner(DesignBuffer ABuffer)
		{
			FDesigners.Remove(ABuffer);
		}

		public IDesigner GetDesigner(DesignBuffer ABuffer)
		{
			return (IDesigner)FDesigners[ABuffer];
		}

		/// <summary> Opens up a new query window against the specified server. </summary>
		/// <returns> The newly created script editor. </returns>
		public IDesigner NewDesigner()
		{
			DesignerInfo LInfo = new DesignerInfo();
			string LSelectDesigner = Strings.SelectDesigner;
			IWindowsFormInterface LForm = FrontendSession.LoadForm(null, String.Format(".Frontend.Derive('Designers adorn {{ ClassName tags {{ Frontend.Browse.Visible = ''false'' }} }} tags {{ Frontend.Caption = ''{0}'' }}')", LSelectDesigner));
			try
			{
				if (LForm.ShowModal(FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				LInfo.ID = LForm.MainSource.DataView.Fields["Main.ID"].AsString;
				LInfo.ClassName = LForm.MainSource.DataView.Fields["Main.ClassName"].AsString;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}

			return OpenDesigner(LInfo, null);
		}

		/// <summary> Determine the default designer for the specified document type ID. </summary>
		public DesignerInfo GetDefaultDesigner(string ADocumentTypeID)
		{
			IServerCursor LCursor = OpenCursor(String.Format("DocumentTypeDefaultDesigners where DocumentType_ID = '{0}' join Designers by ID = Default_Designer_ID over {{ ID, ClassName }}", ADocumentTypeID));
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					if (!LCursor.Next())
						throw new DataphoriaException(DataphoriaException.Codes.NoDefaultDesignerForDocumentType, ADocumentTypeID);
					LCursor.Select(LRow);
					DesignerInfo LResult = new DesignerInfo();
					LResult.ID = (string)LRow["ID"];
					LResult.ClassName = (string)LRow["ClassName"];
					return LResult;
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
		}

		/// <summary> Allow the user to choose from the designers associated with the specified document type ID. </summary>
		public DesignerInfo ChooseDesigner(string ADocumentTypeID)
		{
			IWindowsFormInterface LForm = 
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
						ADocumentTypeID
					)
				);
			try
			{
				LForm.Text = Strings.SelectDesigner;
				if (LForm.MainSource.DataView.IsEmpty())
					throw new DataphoriaException(DataphoriaException.Codes.NoDesignersForDocumentType, ADocumentTypeID);
				if (LForm.ShowModal(Frontend.Client.FormMode.Query) != DialogResult.OK)
					throw new AbortException();
				DesignerInfo LResult = new DesignerInfo();
				LResult.ID = LForm.MainSource.DataView.Fields["Main.Designer_ID"].AsString;
				LResult.ClassName = LForm.MainSource.DataView.Fields["Main.ClassName"].AsString;
				return LResult;
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		public IDesigner OpenDesigner(DesignerInfo AInfo, DesignBuffer ABuffer)
		{
			IDesigner LDesigner = (IDesigner)Activator.CreateInstance(Type.GetType(AInfo.ClassName, true), new object[] {this, AInfo.ID});
			try
			{
				if (ABuffer != null)
					LDesigner.Open(ABuffer);
				else
					LDesigner.New();
				LDesigner.Show();
				return LDesigner;
			}
			catch
			{
				LDesigner.Dispose();
				throw;
			}
		}

		/// <summary> Instantiates a new D4 text editor with the specified initial content. </summary>
		public TextEditor.TextEditor NewEditor(string AText, string ADocumentTypeID)
		{
			TextEditor.TextEditor LEditor = (TextEditor.TextEditor)OpenDesigner(GetDefaultDesigner(ADocumentTypeID), null);
			try
			{
				LEditor.New();
				LEditor.EditorText = AText;
				LEditor.Service.SetModified(false);
				return LEditor;
			}
			catch
			{
				LEditor.Dispose();
				throw;
			}
		}

		/// <summary> Evaluates a D4 expression that returns a D4 document, and shows the document in an editor. </summary>
		public TextEditor.TextEditor EvaluateAndEdit(string AExpression, string ADocumentTypeID)
		{
			using (DAE.Runtime.Data.Scalar LScript = (DAE.Runtime.Data.Scalar)EvaluateQuery(AExpression))
			{
				return NewEditor(LScript.AsString, ADocumentTypeID);
			}
		}

		// DesignBuffers

		private void SetInsertOpenState(Frontend.Client.IFormInterface AForm)
		{
			AForm.MainSource.OpenState = DAE.Client.DataSetState.Insert;
		}

		public DocumentDesignBuffer PromptForDocumentBuffer(IDesigner ADesigner, string ADefaultLibraryName, string ADefaultDocumentName)
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
					ADesigner.DesignerID
				)
			);
			try
			{
				IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('NewDocument', 'Add')", SetInsertOpenState);
				try
				{
					LForm.Text = Strings.SaveAsDocumentFormTitle;
					LForm.MainSource.DataView.Edit();
					if (ADefaultLibraryName != String.Empty)
						LForm.MainSource.DataView["Main.Library_Name"].AsString = ADefaultLibraryName;
					if (ADefaultDocumentName != String.Empty)
						LForm.MainSource.DataView["Main.Name"].AsString = ADefaultDocumentName;
					LForm.MainSource.DataView["Main.Type_ID"].AsString = GetDefaultDocumentType(ADesigner);
					LForm.MainSource.DataView.OnValidate += new EventHandler(SaveFormValidate);

					if (LForm.ShowModal(Frontend.Client.FormMode.Insert) != DialogResult.OK)
						throw new AbortException();

					DocumentDesignBuffer LBuffer = 
						new DocumentDesignBuffer
						(
							this,
							LForm.MainSource.DataView["Main.Library_Name"].AsString,
							LForm.MainSource.DataView["Main.Name"].AsString
						);
					LBuffer.DocumentType = LForm.MainSource.DataView["Main.Type_ID"].AsString;
					return LBuffer;
				}
				finally
				{
					LForm.HostNode.Dispose();
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

		private void SaveFormValidate(object ASender, EventArgs AArgs)
		{
			var LView = (DataView)ASender;
			CheckDocumentOverwrite(LView["Main.Library_Name"].AsString, LView["Main.Name"].AsString);
		}

		public FileDesignBuffer PromptForFileBuffer(IDesigner ADesigner, string ADefaultFileName)
		{
			using (SaveFileDialog LDialog = new SaveFileDialog())
			{
				LDialog.InitialDirectory = (string)FSettings.GetSetting("Dataphoria.SaveDirectory", typeof(string), ".");
				LDialog.Filter = GetSaveFilter(ADesigner);
				LDialog.FilterIndex = 0;
				LDialog.RestoreDirectory = false;
				LDialog.Title = Strings.SaveDialogTitle;
				LDialog.AddExtension = true;
				if (ADefaultFileName != String.Empty)
					LDialog.DefaultExt = Path.GetExtension(ADefaultFileName);
				else
				{
					string LDefaultDocumentType = GetDefaultDocumentType(ADesigner);
					if (LDefaultDocumentType.Length != 0)
						LDialog.DefaultExt = "." + LDefaultDocumentType;
					else
					{
						LDialog.DefaultExt = String.Empty;
						LDialog.AddExtension = false;
					}
				}
				LDialog.CheckPathExists = true;
				LDialog.OverwritePrompt = true;

				if (LDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				FSettings.SetSetting("Dataphoria.SaveDirectory", Path.GetDirectoryName(LDialog.FileName));

				return new FileDesignBuffer(this, LDialog.FileName);
			}
		}

		public void SaveAll()
		{
			
			
			IDesigner LDesigner;
			foreach (Form LForm in this.MdiChildren)
			{
				LDesigner = LForm as IDesigner;
				if ((LDesigner != null) && LDesigner.Service.IsModified)
					LDesigner.Service.Save();
			}
		}

		public string GetDocumentType(string ALibraryName, string ADocumentName)
		{
			return
			(
				(Alphora.Dataphor.DAE.Runtime.Data.Scalar)EvaluateQuery
				(
					String.Format(".Frontend.GetDocumentType('{0}', '{1}')", ALibraryName, ADocumentName)
				)
			).AsString;
		}

		#endregion

		#region IServiceProvider Members

		private Hashtable FServices = new Hashtable();
		public Hashtable Services { get { return FServices; } }

		public new virtual object GetService(Type AServiceType)
		{
			object LResult = base.GetService(AServiceType);
			if (LResult != null)
				return LResult;
			return FServices[AServiceType];
		}

		#endregion

		#region File Support

		private string GetOpenFilter()
		{
			StringBuilder LFilter = new StringBuilder();
			LFilter.Append(Strings.AllFilesFilter);
			IServerCursor LCursor = OpenCursor("DocumentTypes over { ID, Description }");
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						LFilter.AppendFormat("|{1} (*.{0})|*.{0}", (string)LRow["ID"], (string)LRow["Description"]);
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				CloseCursor(LCursor);
			}
			return LFilter.ToString();
		}

		private string GetSaveFilter(IDesigner ADesigner)
		{
			StringBuilder LFilter = new StringBuilder();
			IServerCursor LCursor = 
				OpenCursor
				(
					String.Format
					(
						"DocumentTypeDesigners where Designer_ID = '{0}' join DocumentTypes by ID = DocumentType_ID over {{ ID, Description }}",
						ADesigner.DesignerID
					)
				);
			try
			{
				DAE.Runtime.Data.Row LRow = LCursor.Plan.RequestRow();
				try
				{
					while (LCursor.Next())
					{
						LCursor.Select(LRow);
						if (LFilter.Length > 0)
							LFilter.Append('|');
						LFilter.AppendFormat("{1} (*.{0})|*.{0}", (string)LRow["ID"], (string)LRow["Description"]);
					}
				}
				finally
				{
					LCursor.Plan.ReleaseRow(LRow);
				}
			}
			finally
			{
				LCursor.Plan.Close(LCursor);
			}
			if (LFilter.Length > 0)
				LFilter.Append('|');
			LFilter.Append(Strings.AllFilesFilter);
			return LFilter.ToString();
		}

		private string[] FileOpenPrompt(bool AAllowMultiple)
		{
			using (OpenFileDialog LDialog = new OpenFileDialog())
			{
				LDialog.InitialDirectory = (string)FSettings.GetSetting("Dataphoria.OpenDirectory", typeof(string), ".");
				LDialog.Filter = GetOpenFilter();
				LDialog.FilterIndex = 0;
				LDialog.RestoreDirectory = false;
				LDialog.Title = Strings.FileOpenTitle;
				LDialog.Multiselect = AAllowMultiple;
				LDialog.CheckFileExists = true;

				if (LDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				FSettings.SetSetting("Dataphoria.OpenDirectory", Path.GetDirectoryName(LDialog.FileName));

				return LDialog.FileNames;
			}
		}

		public void OpenFiles(string[] AFileNames)
		{
			string LFileName;
			FileDesignBuffer LBuffer;

			for (int i = 0; i < AFileNames.Length; i++)
			{
				LFileName = AFileNames[i];
				DesignerInfo LInfo = GetDefaultDesigner(Program.DocumentTypeFromFileName(LFileName));
				LBuffer = new FileDesignBuffer(this, LFileName);
				try
				{
					OpenDesigner
					(
						LInfo, 
						LBuffer
					);
				}
				catch (Exception LException)
				{
					Program.HandleException(LException);
				}
			}
		}

		public void OpenFile()
		{
			OpenFiles(FileOpenPrompt(true));
		}

		public void OpenFileWith()
		{
			string[] LFileNames = FileOpenPrompt(false);
			string LFileName = LFileNames[0];

			DesignerInfo LInfo = ChooseDesigner(Program.DocumentTypeFromFileName(LFileName));

			FileDesignBuffer LBuffer = new FileDesignBuffer(this, LFileName);

			OpenDesigner(LInfo, LBuffer);
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

		public void AttachForm(BaseForm AForm) 
		{
			AForm.Show(this.FDockPanel);
			AForm.SelectNextControl(AForm, true, true, true, false);	
		}

		private void CloseChildren()
		{
			Form[] LForms = this.MdiChildren;
			foreach (Form LForm in LForms)
				LForm.Close();
		}

		private void DisposeChildren()
		{
			Form[] LForms = this.MdiChildren;
			foreach (Form LForm in LForms)
				LForm.Dispose();
		}

		protected override void OnMdiChildActivate(EventArgs AArgs)
		{
			base.OnMdiChildActivate(AArgs);
			MergeOrRevertMergeOfToolbars();
			MergeOrRevertMergeOfStatusBars();
		}
		
		private void MergeOrRevertMergeOfStatusBars()
		{
			ToolStripManager.RevertMerge(this.FStatusStrip);
			var LChildForm = ActiveMdiChild as IStatusBarClient;
			if (LChildForm != null)
			{
				LChildForm.MergeStatusBarWith(this.FStatusStrip);
			}
		}

		private void MergeOrRevertMergeOfToolbars()
		{
			ToolStripManager.RevertMerge(this.FToolStrip);			
			var LChildForm = ActiveMdiChild as IToolBarClient;
			if (LChildForm != null)
			{
				LChildForm.MergeToolbarWith(this.FToolStrip);
			}
		}

		#endregion

		#region DAE & Frontend Server Helpers

		// these two methods should be moved to serverconnection or even higher
		public void ExecuteScript(string AScript)
		{
			ExecuteScript(AScript, null);
		}

		/// <summary> Executes a string on the dataphor server. </summary>
		public void ExecuteScript(string AScript, DAE.Runtime.DataParams AParams)
		{
			if (AScript != String.Empty)
			{
				Cursor LOldCursor = Cursor.Current;
				Cursor.Current = Cursors.WaitCursor;
				try
				{
					IServerScript LScript = FUtilityProcess.PrepareScript(AScript);
					try
					{
						LScript.Execute(AParams);
					}
					finally
					{
						FUtilityProcess.UnprepareScript(LScript);
					}
				}
				finally
				{
					Cursor.Current = LOldCursor;
				}
			}
		}

		public IServerCursor OpenCursor(string AQuery)
		{
			return OpenCursor(AQuery, null);
		}

		public IServerCursor OpenCursor(string AQuery, DAE.Runtime.DataParams AParams)
		{
			Cursor LOldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				IServerExpressionPlan LPlan = FUtilityProcess.PrepareExpression(AQuery, AParams);
				try
				{
					return LPlan.Open(AParams);
				}
				catch
				{
					FUtilityProcess.UnprepareExpression(LPlan);
					throw;
				}
			}
			finally
			{
				Cursor.Current = LOldCursor;
			}
		}

		public void CloseCursor(IServerCursor ACursor)
		{
			IServerExpressionPlan LPlan = ACursor.Plan;
			LPlan.Close(ACursor);
			FUtilityProcess.UnprepareExpression(LPlan);
		}

		public DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery)
		{
			return EvaluateQuery(AQuery, null);
		}

		public DAE.Runtime.Data.DataValue EvaluateQuery(string AQuery, DAE.Runtime.DataParams AParams)
		{
			Cursor LOldCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
			try
			{
				var LPlan = FUtilityProcess.PrepareExpression(AQuery, AParams);
				DAE.Runtime.Data.DataValue LResult;
				try
				{
					LResult = LPlan.Evaluate(AParams);
				}
				finally
				{
					FUtilityProcess.UnprepareExpression(LPlan);				
				}
				return LResult;
			}
			finally
			{
				Cursor.Current = LOldCursor;
			}
		}

		public bool DocumentExists(string ALibraryName, string ADocumentName)
		{
			using 
			(
				DAE.Runtime.Data.Scalar LDocumentExistsData = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							@".Frontend.DocumentExists('{0}', '{1}')",
							DAE.Schema.Object.EnsureRooted(ALibraryName),
							DAE.Schema.Object.EnsureRooted(ADocumentName)
						)
					)
			)
			{
				return LDocumentExistsData.AsBoolean;
			}
		}

		public void CheckDocumentOverwrite(string ALibraryName, string ADocumentName)
		{
			if (DocumentExists(ALibraryName, ADocumentName))
				if 
				(
					MessageBox.Show
					(
						String.Format
						(
							Strings.SaveAsDialogReplaceText,
							ALibraryName,
							ADocumentName
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
			using (DAE.Runtime.Data.Scalar LScalar = (DAE.Runtime.Data.Scalar)EvaluateQuery("LibraryName()"))
			{
				return LScalar.AsDisplayString;
			}
		}

		public string GetDefaultDocumentType(IDesigner ADesigner)
		{
			using 
			(
				DAE.Runtime.Data.Scalar LDefaultTypeData = 
					(DAE.Runtime.Data.Scalar)EvaluateQuery
					(
						String.Format
						(
							@".Frontend.GetDefaultDocumentType('{0}')",
							ADesigner.DesignerID
						)
					)
			)
			{
				return LDefaultTypeData.AsString;
			}
		}

		#endregion

		#region Warnings

		/// <summary> Provides access to the warnings / errors list pane. </summary>
		public ErrorListView Warnings
		{
			get
			{
				return FErrorListView; 
			}
		}

		private void ShowWarnings()
		{
			FDockContentErrorListView.Show(FDockPanel);
			FDockContentErrorListView.Activate();								   
		}

		private void WarningsAdded(object ASender, EventArgs AArgs)
		{
			ShowWarnings();
		}

		private void ErrorsAdded(object ASender, EventArgs AArgs)
		{
			ShowWarnings();
			FDockContentErrorListView.Show(FDockPanel);			
		}

		private void ClearWarnings()
		{
			FErrorListView.ClearErrors();
		}

		// IErrorSource

		private DebugLocator GetInnermostLocator(Exception AException)
		{
			var LInner = AException != null ? GetInnermostLocator(AException.InnerException) : null;
			if (LInner != null)
				return LInner;
			var LLocator = AException as ILocatorException;
			if (LLocator != null)
				return new DebugLocator(LLocator.Locator, LLocator.Line, LLocator.LinePos);
			else
				return null;
		}
		
		public bool LocateToError(Exception AException)
		{
			var LLocator = GetInnermostLocator(AException);
			if (LLocator != null)
			{
				try
				{
					OpenLocator(LLocator);
				}
				catch
				{
					// ignore exceptions trying to locate, the locator may no longer even be valid
					return false;
				}
			}
			return true;
		}
		
		void IErrorSource.ErrorHighlighted(Exception AException)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception AException)
		{
			LocateToError(AException);
		}

		#endregion

		#region Commands

		private EventHandler FOnFormDesignerLibrariesChanged;

		public event EventHandler OnFormDesignerLibrariesChanged {
			add {
				this.FOnFormDesignerLibrariesChanged += value;
			}
			remove {
				this.FOnFormDesignerLibrariesChanged -= value;
			}
		}

		private void BrowseDesignerLibraries()
		{
			IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('FormDesignerLibraries')");
			try
			{
				LForm.ShowModal(FormMode.None);
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
			if (FOnFormDesignerLibrariesChanged != null)
				FOnFormDesignerLibrariesChanged(this, EventArgs.Empty);
		}

		private void BrowseDocumentTypes()
		{
			IWindowsFormInterface LForm = FrontendSession.LoadForm(null, ".Frontend.Derive('DocumentTypes')", null);
			try
			{
				LForm.ShowModal(FormMode.None);
			}
			finally
			{
				LForm.HostNode.Dispose();
			}
		}

		private void Documentation()
		{
			string LFileName = GetHelpFileName();
			try 
			{
				System.Diagnostics.Process.Start(LFileName);
			}
			catch (Exception LException)
			{
				Program.HandleException(new DataphoriaException(DataphoriaException.Codes.UnableToOpenHelp, LException, LFileName));
			}
		}

		private void About()
		{
			using (AboutForm LAboutForm = new AboutForm())
			{
				LAboutForm.ShowDialog(this);
			}
		}

		private void LaunchForm()
		{
			Frontend.Client.Windows.Session LSession = GetLiveDesignableFrontendSession();
			try
			{
				LSession.SetFormDesigner();
				IWindowsFormInterface LForm = LSession.LoadForm(null, ".Frontend.Form('.Frontend', 'DerivedFormLauncher')");
				try
				{
					LForm.Show();
				}
				catch
				{
					LForm.HostNode.Dispose();
					throw;
				}
			}
			catch
			{
				LSession.Dispose();
				throw;
			}
		}

		public void ShowDataphorExplorer()
		{
			FDockContentExplorer.Show(FDockPanel);
			FDockContentExplorer.Activate();			
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

		private void FToolStrip_ItemClicked(object ASender, ToolStripItemClickedEventArgs AArgs)
		{
			switch (AArgs.ClickedItem.Name)
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

		private void ClearWarningsClicked(object ASender, System.EventArgs AArgs)
		{
			ClearWarnings();
		}

		#endregion

		#region Debugger

		private Debugger FDebugger;
		public Debugger Debugger { get { return FDebugger; } }

		private void CreateDebugger()
		{
			if (FDebugger == null)
			{
				FDebugger = new Debugger(this);
				FDebugger.PropertyChanged += DebuggerPropertyChanged;
			}
		}

		private void DebuggerPropertyChanged(object ASender, string[] APropertyNames)
		{
			if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "IsStarted" || AItem == "IsPaused"; }))
 			{
 				UpdateDebuggerState();
 				UpdateBreakOnException();
 				UpdateBreakOnStart();
 			}
			else 
			{
			if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "BreakOnException"; }))
				UpdateBreakOnException();
				else if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "BreakOnStart"; }))
					UpdateBreakOnStart();
		}
			if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "CurrentLocation"; }))
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
				catch (Exception LException)
				{
					// Log errors, don't pop-up
					Warnings.AppendError(null, LException, false);
				}
			}
		}

		public void OpenLocator(DebugLocator ALocator)
		{
			DesignerInfo LInfo;
			DesignBuffer LBuffer = DesignBufferFromLocator(out LInfo, ALocator);
			if (LBuffer != null)
			{
				IDesigner LDesigner = this.GetDesigner(LBuffer);
				if (LDesigner != null)
				{
					LDesigner.Service.RequestLocate(ALocator);
					LDesigner.Select();
				}
				else
					OpenDesigner(LInfo, LBuffer);
			}
		}

		public DesignBuffer DesignBufferFromLocator(out DesignerInfo AInfo, DebugLocator ALocator)
		{
			AInfo = new DesignerInfo();
			DesignBuffer LBuffer = null;
			
			// TODO: Introduce a buffer factory for locators
			
			// Files
			if (FileDesignBuffer.IsFileLocator(ALocator.Locator))
			{
				var LFileBuffer = new FileDesignBuffer(this, ALocator);
				AInfo = GetDefaultDesigner(Program.DocumentTypeFromFileName(LFileBuffer.FileName));
				LBuffer = LFileBuffer;
			}
			// Documents
			else if (DocumentDesignBuffer.IsDocumentLocator(ALocator.Locator))
			{
				var LDocumentBuffer = new DocumentDesignBuffer(this, ALocator);
				AInfo = GetDefaultDesigner(GetDocumentType(LDocumentBuffer.LibraryName, LDocumentBuffer.DocumentName));
				LBuffer = LDocumentBuffer;
				}
			else if (ProgramDesignBuffer.IsProgramLocator(ALocator.Locator))
			{
				LBuffer = new ProgramDesignBuffer(this, ALocator);
				AInfo = GetDefaultDesigner("d4");
			}
			
			return LBuffer;
		}

		private void DebugMenuItemClicked(object ASender, ToolStripItemClickedEventArgs AArgs)
		{
			switch (AArgs.ClickedItem.Name)
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
					FDockContentSessionsView.Show(FDockPanel);
					break;
				case "FViewProcessesMenuItem":
				case "FViewProcessesButton":
					EnsureProcessesView();
					FDockContentProcessesView.Show(FDockPanel);
					break;
				case "FViewDebugProcessesMenuItem" :
				case "FViewDebugProcessesButton":
					EnsureDebugProcessesView();
					FDockContentDebugProcessesView.Show(FDockPanel);
					break;
				case "FViewCallStackMenuItem":
				case "FViewCallStackButton" :
					EnsureCallStackView();
					FDockContentCallStackView.Show(FDockPanel);
					break;
				case "FViewStackMenuItem":
				case "FViewStackButton":
					EnsureStackView();
					FDockContentStackView.Show(FDockPanel);
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
			if (FSessionsView == null)
			{
				FSessionsView = new SessionsView();
				FSessionsView.Dataphoria = this;
				FSessionsView.Name = "FSessionView";
				FSessionsView.Dock = DockStyle.Fill;

				FDockContentSessionsView = new DockContent();
				FDockContentSessionsView.HideOnClose = true;
				FDockContentSessionsView.Controls.Add(FSessionsView);
				FDockContentSessionsView.TabText = "Sessions";
				FDockContentSessionsView.Text = "Sessions - Dataphoria";
				FDockContentSessionsView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureProcessesView()
		{
			if (FProcessesView == null)
			{
				FProcessesView = new ProcessesView();
				FProcessesView.Dataphoria = this;
				FProcessesView.Name = "FProcessesView";
				FProcessesView.Dock = DockStyle.Fill;

				FDockContentProcessesView = new DockContent();
				FDockContentProcessesView.HideOnClose = true;
				FDockContentProcessesView.Controls.Add(FProcessesView);
				FDockContentProcessesView.TabText = "Processes";
				FDockContentProcessesView.Text = "Processes - Dataphoria";
				FDockContentProcessesView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureDebugProcessesView()
		{
			if (FDebugProcessesView == null)
			{
				FDebugProcessesView = new DebugProcessesView();
				FDebugProcessesView.Dataphoria = this;
				FDebugProcessesView.Name = "FDebugProcessesView";
				FDebugProcessesView.Dock = DockStyle.Fill;

				FDockContentDebugProcessesView = new DockContent();
				FDockContentDebugProcessesView.HideOnClose = true;
				FDockContentDebugProcessesView.Controls.Add(FDebugProcessesView);
				FDockContentDebugProcessesView.TabText = "Debug Processes ";
				FDockContentDebugProcessesView.Text = "Debug Processes - Dataphoria";
				FDockContentDebugProcessesView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureCallStackView()
		{
			if (FCallStackView == null)
			{
				FCallStackView = new CallStackView();
				FCallStackView.Dataphoria = this;
				FCallStackView.Name = "FCallStackView";
				FCallStackView.Dock = DockStyle.Fill;

				FDockContentCallStackView = new DockContent();
				FDockContentCallStackView.HideOnClose = true;
				FDockContentCallStackView.Controls.Add(FCallStackView);
				FDockContentCallStackView.TabText = "Call Stack";
				FDockContentCallStackView.Text = "Call Stack - Dataphoria";
				FDockContentCallStackView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		private void EnsureStackView()
		{
			if (FStackView == null)
			{
				FStackView = new StackView();
				FStackView.Dataphoria = this;
				FStackView.Name = "FStackView";
				FStackView.Dock = DockStyle.Fill;

				FDockContentStackView = new DockContent();
				FDockContentStackView.HideOnClose = true;
				FDockContentStackView.Controls.Add(FStackView);
				FDockContentStackView.TabText = "Stack";
				FDockContentStackView.Text = "Stack - Dataphoria";
				FDockContentStackView.ShowHint = DockState.DockBottomAutoHide;
			}
		}

		#endregion
				
		#region Help

		public const string CDefaultHelpFileName = @"..\Documentation\Dataphor.chm";

		private string GetHelpFileName()
		{
			return Path.Combine(Application.StartupPath, (string)FSettings.GetSetting("HelpFileName", typeof(string), CDefaultHelpFileName));
		}

		public void InvokeHelp(string AKeyword)
		{
			Help.ShowHelp(null, GetHelpFileName(), HelpNavigator.KeywordIndex, AKeyword.Trim());
		}

		protected override void OnHelpRequested(HelpEventArgs AArgs)
		{
			base.OnHelpRequested(AArgs);
			InvokeHelp("Dataphoria");
		}

		private void FErrorListView_HelpRequested(object ASender, HelpEventArgs AArgs)
		{
			string LKeyword = "Errors and Warnings";
			DataphorException LException = FErrorListView.SelectedError as DataphorException;
			if (LException != null)
				LKeyword = LException.Code.ToString();

			InvokeHelp(LKeyword);
		}

		private void FExplorer_HelpRequested(object ASender, HelpEventArgs AHlpEvent)
		{
			InvokeHelp("Dataphor Explorer");
		}

		#endregion
	}
}
