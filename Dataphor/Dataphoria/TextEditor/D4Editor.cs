/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DataSet = System.Data.DataSet;
using Image = System.Drawing.Image;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Client.Provider;
using Alphora.Dataphor.DAE.Compiling;
using Alphora.Dataphor.DAE.Debug;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.Dataphoria.TextEditor.BlockActions;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.Logging;

using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

using WeifenLuo.WinFormsUI.Docking;


namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
	public partial class D4Editor : TextEditor, IErrorSource
	{
		private ToolStripMenuItem FAnalyzeLineMenuItem;
		private ToolStripMenuItem FAnalyzeMenuItem;
		private ToolStripMenuItem FCancelMenuItem;

		protected DockContent FDockContentResultPanel;
		private ToolStripMenuItem FExecuteBothMenuItem;
		private ToolStripMenuItem FExecuteLineMenuItem;
		private ToolStripMenuItem FExecuteMenuItem;
		private ToolStripMenuItem FExecuteSchemaMenuItem;
		private ToolStripMenuItem FExportDataMenuItem;
		private ToolStripMenuItem FExportMenu;
		private ToolStripMenuItem FNextBlockMenuItem;
		private ToolStripMenuItem FPrepareLineMenuItem;
		private ToolStripMenuItem FPrepareMenuItem;
		private ToolStripMenuItem FPriorBlockMenuItem;
		private ResultPanel FResultPanel;

		private ToolStripMenuItem FScriptMenu;
		private ToolStripMenuItem FSelectBlockMenuItem;
		private ToolStripMenuItem FShowResultsMenuItem;
		private ToolStripMenuItem FToggleBreakpointMenuItem;

		private static readonly ILogger SRFLogger = LoggerFactory.Instance.CreateLogger(typeof (D4Editor));


		public D4Editor() // dummy constructor for MDI menu merging
		{
			InitializeComponent();
			InitializeDocking();
			InitializeExtendendMenu();
		}

		public D4Editor(IDataphoria ADataphoria, string ADesignerID) : base(ADataphoria, ADesignerID)
		{
			InitializeComponent();
			InitializeDocking();
			InitializeExtendendMenu();
			InitializeDebugger();
			
			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemQuestion] = new ToggleBlockDelimiter();
			FTextEdit.EditActions[Keys.Control | Keys.Oemcomma] = new PriorBlock();
			FTextEdit.EditActions[Keys.Control | Keys.OemPeriod] = new NextBlock();
			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.Oemcomma] = new SelectPriorBlock();
			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemPeriod] = new SelectNextBlock();

			FResultPanel.BeginningFind += BeginningFind;
			FResultPanel.ReplacementsPerformed += ReplacementsPerformed;
			FResultPanel.TextNotFound += TextNotFound;

			UpdateCurrentLocation();
		}

		private void Deinitialize()
		{
			DeinitializeDebugger();
		}
		
		private void InitializeDocking()
		{
			FResultPanel = new ResultPanel();

			// 
			// FResultPanel
			// 
			FResultPanel.BackColor = SystemColors.Control;
			FResultPanel.CausesValidation = false;
			FResultPanel.EnableFolding = false;
			//this.FResultPanel.Encoding = ((System.Text.Encoding)(resources.GetObject("FResultPanel.Encoding")));
			FResultPanel.IndentStyle = IndentStyle.Auto;
			FResultPanel.IsIconBarVisible = false;
			FResultPanel.Location = new Point(1, 21);
			FResultPanel.Name = "FResultPanel";
			FResultPanel.ShowInvalidLines = false;
			FResultPanel.ShowLineNumbers = false;
			FResultPanel.ShowVRuler = true;
			FResultPanel.Size = new Size(453, 212);
			FResultPanel.TabIndent = 3;
			FResultPanel.TabIndex = 1;
			FResultPanel.TabStop = false;
			FResultPanel.VRulerRow = 100;
			FResultPanel.Dock = DockStyle.Fill;

			FDockContentResultPanel = new DockContent();
			FDockContentResultPanel.HideOnClose = true;
			FDockContentResultPanel.Controls.Add(FResultPanel);
			FDockContentResultPanel.Text = "Results";
			FDockContentResultPanel.TabText = "Results";
			FDockContentResultPanel.ShowHint = DockState.DockBottom;
			FDockContentResultPanel.DockPanel = FDockPanel;
			FDockContentResultPanel.Name = "DockContentResultPanel";
		}


		private void InitializeExtendendMenu()
		{
			FScriptMenu = new ToolStripMenuItem();
			FExecuteMenuItem = new ToolStripMenuItem();
			FCancelMenuItem = new ToolStripMenuItem();
			FPrepareMenuItem = new ToolStripMenuItem();
			FAnalyzeMenuItem = new ToolStripMenuItem();
			var LSep1 = new ToolStripSeparator();
			FExecuteLineMenuItem = new ToolStripMenuItem();
			FPrepareLineMenuItem = new ToolStripMenuItem();
			FAnalyzeLineMenuItem = new ToolStripMenuItem();
			var LSep3 = new ToolStripSeparator();
			FSelectBlockMenuItem = new ToolStripMenuItem();
			FPriorBlockMenuItem = new ToolStripMenuItem();
			FNextBlockMenuItem = new ToolStripMenuItem();
			var LSep4 = new ToolStripSeparator();
			FShowResultsMenuItem = new ToolStripMenuItem();
			var LSep5 = new ToolStripSeparator();
			FToggleBreakpointMenuItem = new ToolStripMenuItem();

			FExportMenu = new ToolStripMenuItem();
			FExecuteSchemaMenuItem = new ToolStripMenuItem();
			FExportDataMenuItem = new ToolStripMenuItem();
			FExecuteBothMenuItem = new ToolStripMenuItem();

			// 
			// FViewMenu
			// 
			FMenuStrip.Items.AddRange(new[]
										  {
											  FScriptMenu,
											  FExportMenu
										  });

			//
			//FViewToolStripMenuItem
			//
			FViewToolStripMenuItem.DropDownItems.Add(FShowResultsMenuItem);
			// 
			// FScriptMenu
			// 
			FScriptMenu.DropDownItems.AddRange(new ToolStripItem[]
												   {
													   FExecuteMenuItem,
													   FCancelMenuItem,
													   FPrepareMenuItem,
													   FAnalyzeMenuItem,
													   LSep1,
													   FExecuteLineMenuItem,
													   FPrepareLineMenuItem,
													   FAnalyzeLineMenuItem,
													   LSep3,
													   FSelectBlockMenuItem,
													   FPriorBlockMenuItem,
													   FNextBlockMenuItem,
													   LSep4,
													   FShowResultsMenuItem,
													   LSep5,
													   FToggleBreakpointMenuItem
												   });
			FScriptMenu.Text = "&Script";
			FScriptMenu.MergeAction = MergeAction.Insert;
			FScriptMenu.MergeIndex = 2;
			// 
			// FExecuteMenuItem
			// 

			FExecuteMenuItem.ImageIndex = 0;
			FExecuteMenuItem.Text = "&Execute";
			FExecuteMenuItem.Click += FMainMenuStrip_ItemClicked;
			FExecuteMenuItem.Image = MenuImages.Execute;
			FExecuteMenuItem.ShortcutKeys = Keys.Control | Keys.E;
			// 
			// FCancelMenuItem
			// 

			FCancelMenuItem.Enabled = false;
			FCancelMenuItem.Text = "&Cancel Execute";
			FCancelMenuItem.Click += FMainMenuStrip_ItemClicked;
			FCancelMenuItem.Image = MenuImages.CancelExecute;
			// 
			// FPrepareMenuItem
			//									 
			FPrepareMenuItem.Text = "&Prepare";
			FPrepareMenuItem.Click += FMainMenuStrip_ItemClicked;
			FPrepareMenuItem.Image = MenuImages.Prepare;
			FPrepareMenuItem.ShortcutKeys = Keys.Control | Keys.R;
			// 
			// FAnalyzeMenuItem
			// 

			FAnalyzeMenuItem.ImageIndex = 4;
			FAnalyzeMenuItem.Text = "&Analyze";
			FAnalyzeMenuItem.Click += FMainMenuStrip_ItemClicked;
			FAnalyzeMenuItem.Image = MenuImages.Analyze;
			FAnalyzeMenuItem.ShortcutKeys = Keys.Control | Keys.T;
			// 
			// FExportMenu
			// 

			FExportMenu.DropDownItems.AddRange(new[]
												   {
													   FExecuteSchemaMenuItem,
													   FExportDataMenuItem,
													   FExecuteBothMenuItem
												   });
			FScriptMenu.MergeAction = MergeAction.Insert;
			FScriptMenu.MergeIndex = 5;
			FExportMenu.Text = "E&xport";
			// 
			// FExecuteSchemaMenuItem
			// 

			FExecuteSchemaMenuItem.Text = "&Schema Only...";
			FExecuteSchemaMenuItem.Click += FMainMenuStrip_ItemClicked;			
			// 
			// FExportDataMenuItem
			// 
			FExportDataMenuItem.Text = "&Data Only...";
			FExportDataMenuItem.Click += FMainMenuStrip_ItemClicked;
			// 
			// FExecuteBothMenuItem
			// 
			FExecuteBothMenuItem.Text = "S&chema and Data...";
			FExecuteBothMenuItem.Click += FMainMenuStrip_ItemClicked;			
			// 
			// FExecuteLineMenuItem
			// 
			FExecuteLineMenuItem.Text = "E&xecute Line";
			FExecuteLineMenuItem.Click += FMainMenuStrip_ItemClicked;   
			FExecuteLineMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.E;		 
			// 
			// FPrepareLineMenuItem
			// 
			FPrepareLineMenuItem.Text = "P&repare Line";
			FPrepareLineMenuItem.Click += FMainMenuStrip_ItemClicked;
			FPrepareLineMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.R;
			// 
			// FAnalyzeLineMenuItem
			// 
			FAnalyzeLineMenuItem.Text = "A&nalyze Line";
			FAnalyzeLineMenuItem.Click += FMainMenuStrip_ItemClicked;
			FAnalyzeLineMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.T;
			// 
			// FSelectBlockMenuItem
			// 
			FSelectBlockMenuItem.Text = "Select &Block";
			FSelectBlockMenuItem.Click += FMainMenuStrip_ItemClicked;
			FSelectBlockMenuItem.ShortcutKeys = Keys.Control | Keys.D;
			// 
			// FPriorBlockMenuItem
			// 
			FPriorBlockMenuItem.Text = "&Prior Block";
			FPriorBlockMenuItem.Click += FMainMenuStrip_ItemClicked;
			// 
			// FNextBlockMenuItem
			// 
			FNextBlockMenuItem.Text = "&Next Block";
			FNextBlockMenuItem.Click += FMainMenuStrip_ItemClicked;
			// 
			// FShowResultsMenuItem
			// 
			FShowResultsMenuItem.Text = "&Results";
			FShowResultsMenuItem.Click += FMainMenuStrip_ItemClicked;
			FShowResultsMenuItem.ShortcutKeys = Keys.F7;
			//
			// FToggleBreakpointMenuItem
			//
			FToggleBreakpointMenuItem.Text = "Set Breakpoint";
			FToggleBreakpointMenuItem.Click += FMainMenuStrip_ItemClicked;
			FToggleBreakpointMenuItem.ShortcutKeys = Keys.F9;
			// 
			// D4Editor
			// 
		}

		protected override IHighlightingStrategy GetHighlightingStrategy()
		{
			switch (DesignerID)
			{
				case "SQL":
					return HighlightingStrategyFactory.CreateHighlightingStrategy("SQL");
				default:
					return HighlightingStrategyFactory.CreateHighlightingStrategy("D4");
			}
		}

		private void ShowResults()
		{
			FDockContentResultPanel.Show();
		}

		private void FMainMenuStrip_ItemClicked(object ASender, EventArgs AArgs)
		{
			try
			{
				SRFLogger.WriteLine(TraceLevel.Verbose, "Sender {0}", ASender);
	
				if (ASender == FSelectBlockMenuItem)
				{
					new SelectBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
				}
				else if (ASender == FPriorBlockMenuItem)
				{
					new PriorBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
				}
				else if (ASender == FNextBlockMenuItem)
				{
					new NextBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
				}
				else if (ASender == FExecuteMenuItem)
				{
					Execute();
				}
				else if (ASender == FExecuteLineMenuItem)
				{
					ExecuteLine();
				}
				else if (ASender == FCancelMenuItem)
				{
					CancelExecute();
				}
				else if (ASender == FPrepareMenuItem)
				{
					Prepare();
				}
				else if (ASender == FPrepareLineMenuItem)
				{
					PrepareLine();
				}
				else if (ASender == FAnalyzeMenuItem)
				{
					Analyze();
				}
				else if (ASender == FAnalyzeLineMenuItem)
				{
					AnalyzeLine();
				}
				else if (ASender == FExportDataMenuItem)
				{
					PromptAndExport(ExportType.Data);
				}
				else if (ASender == FExecuteBothMenuItem)
				{
					PromptAndExport(ExportType.Data | ExportType.Schema);
				}
				else if (ASender == FExecuteSchemaMenuItem)
				{
					PromptAndExport(ExportType.Schema);
				}
				else if (ASender == FShowResultsMenuItem)
				{
					ShowResults();
				}
				else if (ASender == FToggleBreakpointMenuItem)
				{
					ToggleBreakpoint();
				}
			}
			catch (AbortException)
			{
				// do nothing
			}
		}

		#region Execution

		private ScriptExecutor FExecutor;

		void IErrorSource.ErrorHighlighted(Exception AException)
		{
			// nothing
		}

		void IErrorSource.ErrorSelected(Exception AException)
		{
			var LException = AException as ILocatedException;
			if (LException != null)
				GotoPosition(LException.Line, LException.LinePos);
			SelectNextControl(this, true, true, true, false);
		}

		private void AppendResultPanel(string AResults)
		{
			ShowResults();
			FResultPanel.AppendText(AResults);
		}

		private void HideResultPanel()
		{
			FResultPanel.Clear();
			FDockContentResultPanel.Hide();			
		}

		private string GetTextToExecute()
		{
			return
				(
					FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected
						? FTextEdit.ActiveTextAreaControl.SelectionManager.SelectedText
						: FTextEdit.Document.TextContent
				);
		}

		private void SwitchToSQL()
		{
			Dataphoria.ExecuteScript("SetLanguage('RealSQL');");
		}

		private void SwitchFromSQL()
		{
			Dataphoria.ExecuteScript("SetLanguage('D4');");
		}

		private TextLocation GetSelectionPosition()
		{
			if (FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				return FTextEdit.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition;
			return TextLocation.Empty;
		}

		private void ProcessErrors(ErrorList AErrors)
		{
			TextLocation LOffset = GetSelectionPosition();

			for (int LIndex = AErrors.Count - 1; LIndex >= 0; LIndex--)
			{
				Exception LException = AErrors[LIndex];

				bool LIsWarning;
				var LCompilerException = LException as CompilerException;
				if (LCompilerException != null)
					LIsWarning = LCompilerException.ErrorLevel == CompilerErrorLevel.Warning;
				else
					LIsWarning = false;

				// Adjust the offset of the exception to account for the current selection
				var LLocatedException = LException as ILocatedException;
				if (LLocatedException != null)
				{
					if (LLocatedException.Line == 1)
						LLocatedException.LinePos += LOffset.X;
					if (LLocatedException.Line >= 0)
						LLocatedException.Line += LOffset.Y;
				}

				Dataphoria.Warnings.AppendError(this, LException, LIsWarning);
			}
		}

		public void Prepare()
		{
			FResultPanel.Clear();
			PrepareForExecute();

			var LResult = new StringBuilder();

			var LErrors = new ErrorList();
			try
			{
				using (var LStatusForm = new StatusForm(Strings.ProcessingQuery))
				{
					bool LAttemptExecute = true;
					try
					{
						DateTime LStartTime = DateTime.Now;
						try
						{
							IServerScript LScript;
							IServerProcess LProcess =
								DataSession.ServerSession.StartProcess(
									new ProcessInfo(DataSession.ServerSession.SessionInfo));
							try
							{
								LScript = LProcess.PrepareScript(GetTextToExecute());
								try
								{
									if (ScriptExecutionUtility.ConvertParserErrors(LScript.Messages, LErrors))
									{
										foreach (IServerBatch LBatch in LScript.Batches)
										{
											if (LBatch.IsExpression())
											{
												IServerExpressionPlan LPlan = LBatch.PrepareExpression(null);
												try
												{
													LAttemptExecute &=
														ScriptExecutionUtility.ConvertCompilerErrors(LPlan.Messages,
																									 LErrors);
													if (LAttemptExecute)
													{
														LResult.AppendFormat
														(
															Strings.PrepareSuccessful,
															new object[]
															{
																LPlan.PlanStatistics.PrepareTime.ToString(),
																LPlan.PlanStatistics.CompileTime.ToString(),
																LPlan.PlanStatistics.OptimizeTime.ToString(),
																LPlan.PlanStatistics.BindingTime.ToString()
															}
														);
														LResult.Append("\r\n");
													}
												}
												finally
												{
													LBatch.UnprepareExpression(LPlan);
												}
											}
											else
											{
												IServerStatementPlan LPlan = LBatch.PrepareStatement(null);
												try
												{
													LAttemptExecute &=
														ScriptExecutionUtility.ConvertCompilerErrors(LPlan.Messages,
																									 LErrors);
													if (LAttemptExecute)
													{
														LResult.AppendFormat
														(
															Strings.PrepareSuccessful,
															new object[]
															{
																LPlan.PlanStatistics.PrepareTime.ToString(),
																LPlan.PlanStatistics.CompileTime.ToString(),
																LPlan.PlanStatistics.OptimizeTime.ToString(),
																LPlan.PlanStatistics.BindingTime.ToString()
															}
														);
														LResult.Append("\r\n");
													}
												}
												finally
												{
													LBatch.UnprepareStatement(LPlan);
												}
											}

											AppendResultPanel(LResult.ToString());
											LResult.Length = 0;
										}
									}
								}
								finally
								{
									LProcess.UnprepareScript(LScript);
								}
							}
							finally
							{
								DataSession.ServerSession.StopProcess(LProcess);
							}
						}
						finally
						{
							TimeSpan LElapsed = DateTime.Now - LStartTime;
							FExecutionTimeStatus.Text = LElapsed.ToString();
						}

						if (LAttemptExecute)
							SetStatus(Strings.ScriptPrepareSuccessful);
						else
							SetStatus(Strings.ScriptPrepareFailed);
					}
					catch (Exception LException)
					{
						SetStatus(Strings.ScriptFailed);
						LErrors.Add(LException);
					}
				}
			}
			finally
			{
				ProcessErrors(LErrors);
			}
		}

		public void PrepareLine()
		{
			if (SelectLine())
				Prepare();
		}

		public void Analyze()
		{
			PrepareForExecute();

			string LPlan;
			var LErrors = new ErrorList();
			try
			{
				using (var LStatusForm = new StatusForm(Strings.ProcessingQuery))
				{
					DateTime LStartTime = DateTime.Now;
					try
					{
						var LParams = new DataParams();
						LParams.Add(DataParam.Create(Dataphoria.UtilityProcess, "AQuery", GetTextToExecute()));
						LPlan = ((DAE.Runtime.Data.Scalar)Dataphoria.EvaluateQuery("ShowPlan(AQuery)", LParams)).AsString;
					}
					finally
					{
						TimeSpan LElapsed = DateTime.Now - LStartTime;
						FExecutionTimeStatus.Text = LElapsed.ToString();
					}
				}
			}
			catch (Exception LException)
			{
				LErrors.Add(LException);
				ProcessErrors(LErrors);
				SetStatus(Strings.ScriptAnalyzeFailed);
				return;
			}

			SetStatus(Strings.ScriptAnalyzeSuccessful);

			var LAnalyzer = (Analyzer.Analyzer) Dataphoria.OpenDesigner(Dataphoria.GetDefaultDesigner("pla"), null);
			LAnalyzer.LoadPlan(LPlan);
		}

		public void AnalyzeLine()
		{
			if (SelectLine())
				Analyze();
		}

// TODO: RealSQL support will not work w/ asyncronous execution because we have no guarantees that no other scripts will be executed while in the "other" mode

		private void PrepareForExecute()
		{
			Dataphoria.EnsureServerConnection();
			Dataphoria.Warnings.ClearErrors(this);
			SetStatus(String.Empty);
		}

		public void Execute()
		{
			CancelExecute();

			PrepareForExecute();
			FResultPanel.Clear();

			FExecuteMenuItem.Enabled = false;
			FExecuteLineMenuItem.Enabled = false;
			FCancelMenuItem.Enabled = true;
			FWorkingAnimation.Visible = true;

			FExecutor = 
				new ScriptExecutor
				(
					DataSession.ServerSession, 
					GetTextToExecute(),
					ExecutorProgress,
					ExecutorFinished,
					GetExecuteLocator()
				);
			FExecutor.Start();
		}

		public bool SelectLine()
		{
			if (FTextEdit.Document.TextLength > 0)
			{
				// Select the current line
				LineSegment LLine = FTextEdit.Document.GetLineSegment(FTextEdit.ActiveTextAreaControl.Caret.Line);
				FTextEdit.ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new DefaultSelection
					(
						FTextEdit.Document,
						new TextLocation(0, FTextEdit.ActiveTextAreaControl.Caret.Line),
						new TextLocation(LLine.Length, FTextEdit.ActiveTextAreaControl.Caret.Line)
					)
				);
				return true;
			}
			return false;
		}

		public void ExecuteLine()
		{
			if (SelectLine())
				Execute();
		}

		private void ExecutorProgress(PlanStatistics AStatistics, string AResults)
		{
			if (!IsDisposed)
				AppendResultPanel(AResults);
		}

		private void ExecutorFinished(ErrorList AMessages, TimeSpan AElapsedTime)
		{
			// Stop animation & update controls
			FWorkingAnimation.Visible = false;
			FExecuteLineMenuItem.Enabled = true;
			FExecuteMenuItem.Enabled = true;
			FCancelMenuItem.Enabled = false;

			// Handler Error/Warnings
			ProcessErrors(AMessages);
			if (ScriptExecutionUtility.ContainsError(AMessages))
				SetStatus(Strings.ScriptFailed);
			else
				SetStatus(Strings.ScriptSuccessful);

			// Handle execution time
			FExecutionTimeStatus.Text = AElapsedTime.ToString();

			FExecutor = null;
		}

		private void CancelExecute()
		{
			if (FExecutor != null)
			{
				try
				{
					FExecutor.Stop();
					FExecutor = null;
				}
				finally
				{
					AppendResultPanel(Strings.UserAbort);
					ExecutorFinished(new ErrorList(), TimeSpan.Zero);
					SetStatus(Strings.UserAbort);
				}
			}
		}

		// IErrorSource

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			if (FExecutor != null)
			{
				if 
				(
					MessageBox.Show
					(
						Strings.AttemptingToCloseWhileExecuting,
						Strings.AttemptingToCloseWhileExecutingCaption, 
						MessageBoxButtons.OKCancel
					) == DialogResult.OK
				)
					CancelExecute();
				else
					AArgs.Cancel = true;
			}
		}

		#endregion

		#region Exporting

		private void PromptAndExport(ExportType ATypes)
		{
			using (var LSaveDialog = new SaveFileDialog())
			{
				LSaveDialog.Title = Strings.ExportSaveDialogTitle;
				LSaveDialog.Filter = "XML Schema (*.xsd)|*.xsd|XML File (*.xml)|*.xml|All Files (*.*)|*.*";
				if (ATypes == ExportType.Schema) // schema only
				{
					LSaveDialog.DefaultExt = "xsd";
					LSaveDialog.FilterIndex = 1;
				}
				else
				{
					LSaveDialog.DefaultExt = "xml";
					LSaveDialog.FilterIndex = 2;
				}
				LSaveDialog.RestoreDirectory = false;
				LSaveDialog.InitialDirectory = ".";
				LSaveDialog.OverwritePrompt = true;
				LSaveDialog.CheckPathExists = true;
				LSaveDialog.AddExtension = true;

				if (LSaveDialog.ShowDialog() != DialogResult.OK)
					throw new AbortException();

				Cursor = Cursors.WaitCursor;
				try
				{
					Execute(ATypes, LSaveDialog.FileName);
				}
				finally
				{
					Cursor = Cursors.Default;
				}
			}
		}

		private void Execute(ExportType AExportType, string AFileName)
		{
			// Make sure our server is connected...
			Dataphoria.EnsureServerConnection();

			using (var LStatusForm = new StatusForm(Strings.Exporting))
			{
				using (var LConnection = new DAEConnection())
				{
					if (DesignerID == "SQL")
						SwitchToSQL();
					try
					{
						using (var LAdapter = new DAEDataAdapter(GetTextToExecute(), LConnection))
						{
							using (var LDataSet = new DataSet())
							{
								var LProcess =
									DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.ServerSession.SessionInfo));
								try
								{
									LConnection.Open(DataSession.Server, DataSession.ServerSession, LProcess);
									try
									{
										switch (AExportType)
										{
											case ExportType.Data:
												LAdapter.Fill(LDataSet);
												LDataSet.WriteXml(AFileName, XmlWriteMode.IgnoreSchema);
												break;
											case ExportType.Schema:
												LAdapter.FillSchema(LDataSet, SchemaType.Source);
												LDataSet.WriteXmlSchema(AFileName);
												break;
											default:
												LAdapter.Fill(LDataSet);
												LDataSet.WriteXml(AFileName, XmlWriteMode.WriteSchema);
												break;
										}
									}
									finally
									{
										LConnection.Close();
									}
								}
								finally
								{
									DataSession.ServerSession.StopProcess(LProcess);
								}
							}
						}
					}
					finally
					{
						if (DesignerID == "SQL")
							SwitchFromSQL();
					}
				}
			}
		}

		#endregion

		#region StatusBar

		protected ToolStripStatusLabel FExecutionTimeStatus;
		private PictureBox FWorkingAnimation;
		protected ToolStripControlHost  FWorkingStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();

			this.FExecutionTimeStatus = new ToolStripStatusLabel 
			{
				Name = "FExecutionTimeStatus",
				Alignment = ToolStripItemAlignment.Right,
				MergeIndex = 150,
				BorderSides = ToolStripStatusLabelBorderSides.All,
				Text = "0:00:00",
				DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
			};

			const string LCResourceName = "Alphora.Dataphor.Dataphoria.Images.Rider.gif";
			Stream LManifestResourceStream = GetType().Assembly.GetManifestResourceStream(LCResourceName);
			Error.AssertFail(LManifestResourceStream != null, "Resource must exist: " + LCResourceName);
			FWorkingAnimation = 
				new PictureBox
				{
					Image = Image.FromStream(LManifestResourceStream),
					SizeMode = PictureBoxSizeMode.AutoSize											
				};
			FWorkingStatus = new ToolStripControlHost(FWorkingAnimation, "FWorkingStatus")
			{
				Size = FWorkingAnimation.Size,
				Visible = false,
				Alignment = ToolStripItemAlignment.Right,
			};
			
			FStatusStrip.Items.Add(FExecutionTimeStatus);
			FStatusStrip.Items.Add(FWorkingStatus);
		}

		protected override void DisposeStatusBar()
		{
			base.DisposeStatusBar();

			FWorkingAnimation.Image.Dispose(); //otherwise the animation thread will still be hanging around
		}

		#endregion

		#region Debugger

		private void InitializeDebugger()
		{
			Dataphoria.Debugger.PropertyChanged += Debugger_PropertyChanged;
			Dataphoria.Debugger.Breakpoints.Changed += Debugger_BreakpointsChanged;
			FTextEdit.ActiveTextAreaControl.TextArea.IconBarMargin.MouseDown += IconBarMouseDown;
		}
		
		private void DeinitializeDebugger()
		{
			Dataphoria.Debugger.PropertyChanged -= Debugger_PropertyChanged;
			Dataphoria.Debugger.Breakpoints.Changed -= Debugger_BreakpointsChanged;
		}

        protected override void NameOrModifiedChanged(object ASender, EventArgs AArgs)
        {
			base.NameOrModifiedChanged(ASender, AArgs);
			ClearBreakpointBookmarks();
			LoadBreakpointBookmarks();
			UpdateCurrentLocation();
		}

		private void ClearBreakpointBookmarks()
		{
			FTextEdit.Document.BookmarkManager.Clear();
		}

		private void IconBarMouseDown(AbstractMargin AIconBar, Point AMousePos, MouseButtons AMouseButtons)
		{
			if (AMouseButtons == MouseButtons.Left)
			{
				TextLocation ALogicPos = 
					AIconBar.TextArea.TextView.GetLogicalPosition
					(
						0, 
						AMousePos.Y - AIconBar.TextArea.TextView.DrawingPosition.Top
					);

				if (ALogicPos.Y >= 0 && ALogicPos.Y < AIconBar.TextArea.Document.TotalNumberOfLines)
					Dataphoria.Debugger.ToggleBreakpoint(new DebugLocator(Service.GetLocator().Locator, ALogicPos.Line + 1, -1));
			}
		}

		private void Debugger_PropertyChanged(object ASender, string[] APropertyNames)
		{
			if (Array.Exists<string>(APropertyNames, (string AItem) => { return AItem == "IsPaused" || AItem == "CurrentLocation"; }))
				UpdateCurrentLocation();
		}

		private CurrentLineBookmark FCurrentBookmark;
		
		private void UpdateCurrentLocation()
		{
			var LNewCurrent = Dataphoria.Debugger.CurrentLocation;
			
			// Remove the existing current bookmark if it is no longer accurate
 			if (FCurrentBookmark != null && (LNewCurrent == null || !FCurrentBookmark.Locator.Equals(LNewCurrent)))
 			{
 				FTextEdit.Document.BookmarkManager.RemoveMark(FCurrentBookmark);
 				FCurrentBookmark.RemoveMarker();
 				FCurrentBookmark = null;
 			}
 			
 			// Add a new current bookmark if needed and appropriate
			if 
			(
				FCurrentBookmark == null 
					&& LNewCurrent != null 
					&& Service.LocatorNameMatches(LNewCurrent.Locator) 
					&& LNewCurrent.Line >= 1 
					&& LNewCurrent.Line <= FTextEdit.Document.TotalNumberOfLines
			)
 			{
 				var LLocation = new TextLocation(LNewCurrent.LinePos < 1 ? 0 : LNewCurrent.LinePos - 1, LNewCurrent.Line - 1);
 				FCurrentBookmark = new CurrentLineBookmark(FTextEdit.Document, LLocation, LNewCurrent);
				AddBookmark(FCurrentBookmark);
 			}
		}

		private void AddBookmark(DebugBookmark ABookmark)
		{
			FTextEdit.Document.BookmarkManager.AddMark(ABookmark);
			FTextEdit.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, ABookmark.LineNumber, ABookmark.LineNumber));
			FTextEdit.Document.CommitUpdate();
			FTextEdit.ActiveTextAreaControl.TextArea.Refresh(FTextEdit.ActiveTextAreaControl.TextArea.IconBarMargin);
		}

		private void RemoveBookmark(BreakpointBookmark LOldBookmark)
		{
			FTextEdit.Document.BookmarkManager.RemoveMark(LOldBookmark);
			LOldBookmark.RemoveMarker();
			FTextEdit.ActiveTextAreaControl.TextArea.Refresh(FTextEdit.ActiveTextAreaControl.TextArea.IconBarMargin);
		}

		private void Debugger_BreakpointsChanged(NotifyingBaseList<DebugLocator> ASender, bool AIsAdded, DebugLocator AItem, int AIndex)
		{
			if (Service.LocatorNameMatches(AItem.Locator))
			{
				if (AIsAdded)
					AddBreakpointBookmark(AItem);
				else
				{
					var LOldBookmark = (BreakpointBookmark)FTextEdit.Document.BookmarkManager.GetFirstMark((Bookmark APredicate) => { return (APredicate is BreakpointBookmark) && ((BreakpointBookmark)APredicate).Locator == AItem; });
					if (LOldBookmark != null)
						RemoveBookmark(LOldBookmark);
				}
			}
		}
		
		private void LoadBreakpointBookmarks()
		{
			foreach (DebugLocator LItem in Dataphoria.Debugger.Breakpoints)
			if (Service.LocatorNameMatches(LItem.Locator))
				AddBreakpointBookmark(LItem);
		}

		private void AddBreakpointBookmark(DebugLocator AItem)
		{
			var LLocation = new TextLocation((AItem.LinePos >= 0 ? AItem.LinePos - 1 : 0), AItem.Line - 1);
			var LNewBookmark = new BreakpointBookmark(FTextEdit.Document, LLocation, AItem);
			AddBookmark(LNewBookmark);
		}

		private DebugLocator GetExecuteLocator()
		{
			var LLocator = Service.GetLocator();
			if (LLocator != null)
			{
				if (FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				{
					TextLocation LLocation = FTextEdit.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition;
					return 
						new DebugLocator
						(
							LLocator.Locator, 
							LLocation.Line + 1, 
							LLocation.Column + 1
						);
				}
				else
					return LLocator;
			}
			else
				return null;
		}

		private void ToggleBreakpoint()
		{
			var LLocator = Service.GetLocator();
			if (LLocator != null)
				Dataphoria.Debugger.ToggleBreakpoint
				(
					new DebugLocator(LLocator.Locator, FTextEdit.ActiveTextAreaControl.Caret.Line + 1, -1)
				);
		}

		#endregion
	}

	
}