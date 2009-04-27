/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Threading;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Data;
using System.Text;

using SD = ICSharpCode.TextEditor;

using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Runtime.Data;
using Alphora.Dataphor.DAE.Client.Provider;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;

using Syncfusion.Windows.Forms.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> D4 text editor. </summary>
	public partial class D4Editor : TextEditor, IErrorSource
	{

		private Alphora.Dataphor.Dataphoria.TextEditor.ResultPanel FResultPanel;

        private ToolStripMenuItem FScriptMenu;
        private ToolStripMenuItem FExecuteMenuItem;
        private ToolStripMenuItem FExecuteLineMenuItem;
        private ToolStripMenuItem FPrepareLineMenuItem;
        private ToolStripMenuItem FSelectBlockMenuItem;
        private ToolStripMenuItem FPriorBlockMenuItem;
        private ToolStripMenuItem FNextBlockMenuItem;
        private ToolStripMenuItem FPrepareMenuItem;
        private ToolStripMenuItem FAnalyzeMenuItem;
        private ToolStripMenuItem FAnalyzeLineMenuItem;
        private ToolStripMenuItem FInjectMenuItem;
        private ToolStripMenuItem FExecuteSchemaMenuItem;
        private ToolStripMenuItem FExportDataMenuItem;
        private ToolStripMenuItem FExecuteBothMenuItem;
        private ToolStripMenuItem FShowResultsMenuItem;
        private ToolStripMenuItem FExportMenu;
        private ToolStripMenuItem FCancelMenuItem;

        protected DockContent FDockContentResultPanel;


		public D4Editor() : base()	// dummy constructor for SyncFusion's MDI menu merging
		{
			InitializeComponent();
            InitializeDocking();
		}

		public D4Editor(IDataphoria ADataphoria, string ADesignerID) : base(ADataphoria, ADesignerID)
		{
			InitializeComponent();

            InitializeDocking();



			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemQuestion] = new ToggleBlockDelimiter();
			FTextEdit.EditActions[Keys.Control | Keys.Oemcomma] = new PriorBlock();
			FTextEdit.EditActions[Keys.Control | Keys.OemPeriod] = new NextBlock();
			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.Oemcomma] = new SelectPriorBlock();
			FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemPeriod] = new SelectNextBlock();

			FResultPanel.BeginningFind += new EventHandler(BeginningFind);
			FResultPanel.ReplacementsPerformed += new ReplacementsPerformedHandler(ReplacementsPerformed);
			FResultPanel.TextNotFound += new EventHandler(TextNotFound);
		}

        private void InitializeDocking()
        {
            this.FResultPanel = new Alphora.Dataphor.Dataphoria.TextEditor.ResultPanel();

            // 
            // FResultPanel
            // 
            this.FResultPanel.BackColor = System.Drawing.SystemColors.Control;
            this.FResultPanel.CausesValidation = false;
            this.FResultPanel.EnableFolding = false;
            //this.FResultPanel.Encoding = ((System.Text.Encoding)(resources.GetObject("FResultPanel.Encoding")));
            this.FResultPanel.IndentStyle = ICSharpCode.TextEditor.Document.IndentStyle.Auto;
            this.FResultPanel.IsIconBarVisible = false;
            this.FResultPanel.Location = new System.Drawing.Point(1, 21);
            this.FResultPanel.Name = "FResultPanel";
            this.FResultPanel.ShowInvalidLines = false;
            this.FResultPanel.ShowLineNumbers = false;
            this.FResultPanel.ShowVRuler = true;
            this.FResultPanel.Size = new System.Drawing.Size(453, 212);
            this.FResultPanel.TabIndent = 3;
            this.FResultPanel.TabIndex = 1;
            this.FResultPanel.TabStop = false;
            this.FResultPanel.VRulerRow = 100;
            this.FResultPanel.Dock = DockStyle.Fill;

            FDockContentResultPanel = new DockContent();
            FDockContentResultPanel.Controls.Add(FResultPanel);
            FDockContentResultPanel.Text = "Results";
            FDockContentResultPanel.TabText = "D4 Results";
            FDockContentResultPanel.ShowHint = DockState.DockBottomAutoHide;
            FDockContentResultPanel.Show(this.FDockPanel);
        }


        private void InitializeExtendendMenu() {
            this.FScriptMenu = new ToolStripMenuItem();
            this.FExecuteMenuItem = new ToolStripMenuItem();            
            this.FCancelMenuItem = new ToolStripMenuItem();
            this.FPrepareMenuItem = new ToolStripMenuItem();
            this.FAnalyzeMenuItem = new ToolStripMenuItem();
            this.FInjectMenuItem = new ToolStripMenuItem();
            this.FExportMenu = new ToolStripMenuItem();
            this.FExecuteSchemaMenuItem = new ToolStripMenuItem();
            this.FExportDataMenuItem = new ToolStripMenuItem();
            this.FExecuteBothMenuItem = new ToolStripMenuItem();
            this.FExecuteLineMenuItem = new ToolStripMenuItem();
            this.FPrepareLineMenuItem = new ToolStripMenuItem();
            this.FAnalyzeLineMenuItem = new ToolStripMenuItem();
            this.FSelectBlockMenuItem = new ToolStripMenuItem();
            this.FPriorBlockMenuItem = new ToolStripMenuItem();
            this.FNextBlockMenuItem = new ToolStripMenuItem();
            this.FShowResultsMenuItem = new ToolStripMenuItem();

            // 
            // FViewMenu
            // 
            this.FMenuStrip.Items.AddRange(new ToolStripMenuItem[] {
            this.FShowResultsMenuItem});
            ;
            // 
            // FScriptMenu
            // 
            this.FScriptMenu.DropDownItems.AddRange(new ToolStripMenuItem[] {
            this.FExecuteMenuItem,
            this.FCancelMenuItem,
            this.FPrepareMenuItem,
            this.FAnalyzeMenuItem,
            this.FInjectMenuItem,
            this.FExportMenu});
            this.FScriptMenu.Text = "&Script";
            // 
            // FExecuteMenuItem
            // 

            this.FExecuteMenuItem.ImageIndex = 0;

            this.FExecuteMenuItem.Text = "&Execute";           
            // 
            // FCancelMenuItem
            // 

            this.FCancelMenuItem.Enabled = false;
            this.FCancelMenuItem.Text = "&Cancel Execute";
            // 
            // FPrepareMenuItem
            //                                     
            this.FPrepareMenuItem.Text = "&Prepare";
            // 
            // FAnalyzeMenuItem
            // 

            this.FAnalyzeMenuItem.ImageIndex = 4;
            this.FAnalyzeMenuItem.Text = "&Analyze";
            // 
            // FInjectMenuItem
            // 

            this.FInjectMenuItem.ImageIndex = 3;
            this.FInjectMenuItem.Text = "&Inject As Upgrade";
            // 
            // FExportMenu
            // 

            this.FExportMenu.DropDownItems.AddRange(new ToolStripMenuItem[] {
            this.FExecuteSchemaMenuItem,
            this.FExportDataMenuItem,
            this.FExecuteBothMenuItem});
            this.FExportMenu.Text = "E&xport";
            this.FExportMenu.Visible = false;
            // 
            // FExecuteSchemaMenuItem
            // 

            this.FExecuteSchemaMenuItem.Text = "&Schema Only...";
            // 
            // FExportDataMenuItem
            // 
            this.FExportDataMenuItem.Text = "&Data Only...";
            // 
            // FExecuteBothMenuItem
            // 


            this.FExecuteBothMenuItem.Text = "S&chema and Data...";
            // 
            // FExecuteLineMenuItem
            // 
            this.FExecuteLineMenuItem.Text = "E&xecute Line";
            // 
            // FPrepareLineMenuItem
            // 
            this.FPrepareLineMenuItem.Text = "P&repare Line";
            // 
            // FAnalyzeLineMenuItem
            // 
            this.FAnalyzeLineMenuItem.Text = "A&nalyze Line";
            // 
            // FSelectBlockMenuItem
            // 
            this.FSelectBlockMenuItem.Text = "Select &Block";
            // 
            // FPriorBlockMenuItem
            // 
            this.FPriorBlockMenuItem.Text = "&Prior Block";
            // 
            // FNextBlockMenuItem
            // 
            this.FNextBlockMenuItem.Text = "&Next Block";
            // 
            // FShowResultsMenuItem
            // 
            this.FShowResultsMenuItem.Text = "&Results";
            // 
            // D4Editor
            // 
        }

		protected override SD.Document.IHighlightingStrategy GetHighlightingStrategy()
		{
			switch (DesignerID)
			{
				case "SQL" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("SQL");
				default : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("D4");
			}
		}

		private void ShowResults()
		{
			//FDockingManager.SetDockVisibility(FResultPanel, true);
			// TODO: There is a problem with Syncfusion that crashes the whole app if the FResultPanel is pinned and the following code runs
//			FDockingManager.ActivateControl(FResultPanel);
		}

		private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
		{
			try
			{
				switch (AArgs.ClickedBarItem.ID)
				{
					case "SelectBlock" : new SelectBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "PriorBlock": new PriorBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "NextBlock" : new NextBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "Execute" : Execute(); break;
					case "ExecuteLine" : ExecuteLine(); break;
					case "Cancel" : CancelExecute(); break;
					case "Prepare" : Prepare(); break;
					case "PrepareLine" : PrepareLine(); break;
					case "Analyze" : Analyze(); break;
					case "AnalyzeLine" : AnalyzeLine(); break;
					case "Inject" : Inject(); break;
					case "ExportData" : PromptAndExport(ExportType.Data); break;
					case "ExportBoth" : PromptAndExport(ExportType.Data | ExportType.Schema); break;
					case "ExportSchema" : PromptAndExport(ExportType.Schema); break;
					case "ShowResults" : ShowResults(); break;
				}
			}
			catch (AbortException)
			{
				// do nothing
			}
		}

		#region Execution

		private void AppendResultPanel(string AResults)
		{
			ShowResults();
			FResultPanel.AppendText(AResults);
		}

		private void HideResultPanel()
		{
			FResultPanel.Clear();
			//FDockingManager.SetDockVisibility(FResultPanel, false);
		}

		private string GetTextToExecute() 
		{
			return 
			(
				FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected ? 
					FTextEdit.ActiveTextAreaControl.SelectionManager.SelectedText : 
					FTextEdit.Document.TextContent
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

		private Point GetSelectionPosition()
		{
			if (FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
				return FTextEdit.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].StartPosition;
			else
				return Point.Empty;
		}

		private void ProcessErrors(ErrorList AErrors)
		{
			Point LOffset = GetSelectionPosition();

			for (int LIndex = AErrors.Count - 1; LIndex >= 0; LIndex--)
			{
				Exception LException = AErrors[LIndex];
			
				bool LIsWarning;
				DAE.Language.D4.CompilerException LCompilerException = LException as DAE.Language.D4.CompilerException;
				if (LCompilerException != null)
					LIsWarning = LCompilerException.ErrorLevel == DAE.Language.D4.CompilerErrorLevel.Warning;
				else
					LIsWarning = false;

				// Adjust the offset of the exception to account for the current selection
				DAE.ILocatedException LLocatedException = LException as DAE.ILocatedException;
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
		
		public void Inject()
		{
			PrepareForExecute();

			SetStatus
			(
				String.Format
				(
					Strings.ScriptInjected, 
					Dataphoria.EvaluateQuery("System.LibraryName();").AsString,
					Dataphoria.EvaluateQuery
					(
						String.Format
						(
							@"System.InjectUpgrade(System.LibraryName(), ""{0}"");",
							GetTextToExecute().Replace(@"""", @"""""")
						)
					).AsString
				)
			);
		}
		
		public void Prepare()
		{
			FResultPanel.Clear();
			PrepareForExecute();

			StringBuilder LResult = new StringBuilder();

			ErrorList LErrors = new ErrorList();
			try
			{
				using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.ProcessingQuery))
				{
					bool LAttemptExecute = true;
					try
					{
						DateTime LStartTime = DateTime.Now;
						try
						{
							IServerScript LScript;
							IServerProcess LProcess = DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.ServerSession.SessionInfo));
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
													LAttemptExecute &= ScriptExecutionUtility.ConvertCompilerErrors(LPlan.Messages, LErrors);
													if (LAttemptExecute)
													{
														LResult.AppendFormat(Strings.PrepareSuccessful, new object[]{LPlan.Statistics.PrepareTime.ToString(), LPlan.Statistics.CompileTime.ToString(), LPlan.Statistics.OptimizeTime.ToString(), LPlan.Statistics.BindingTime.ToString()});
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
													LAttemptExecute &= ScriptExecutionUtility.ConvertCompilerErrors(LPlan.Messages, LErrors);
													if (LAttemptExecute)
													{
														LResult.AppendFormat(Strings.PrepareSuccessful, new object[]{LPlan.Statistics.PrepareTime.ToString(), LPlan.Statistics.CompileTime.ToString(), LPlan.Statistics.OptimizeTime.ToString(), LPlan.Statistics.BindingTime.ToString()});
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
			ErrorList LErrors = new ErrorList();
			try
			{
				using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.ProcessingQuery))
				{
					DateTime LStartTime = DateTime.Now;
					try
					{
						DAE.Runtime.DataParams LParams = new DAE.Runtime.DataParams();
						LParams.Add(DAE.Runtime.DataParam.Create(Dataphoria.UtilityProcess, "AQuery", GetTextToExecute()));
						LPlan = Dataphoria.EvaluateQuery("ShowPlan(AQuery)", LParams).AsString;
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

			Analyzer.Analyzer LAnalyzer = (Analyzer.Analyzer)Dataphoria.OpenDesigner(Dataphoria.GetDefaultDesigner("pla"), null);
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

		private ScriptExecutor FExecutor;

		public void Execute()
		{
			CancelExecute();

			PrepareForExecute();
			FResultPanel.Clear();

			FExecuteMenuItem.Enabled = false;
			FExecuteLineMenuItem.Enabled = false;
			FCancelMenuItem.Enabled = true;
			FWorkingAnimation.Visible = true;

			FExecutor = new ScriptExecutor(DataSession.ServerSession, GetTextToExecute(), new ReportScriptProgressHandler(ExecutorProgress), new ExecuteFinishedHandler(ExecutorFinished));
			FExecutor.Start();

		}

		public bool SelectLine()
		{
			if (FTextEdit.Document.TextLength > 0)
			{
				// Select the current line
				SD.Document.LineSegment LLine = FTextEdit.Document.GetLineSegment(FTextEdit.ActiveTextAreaControl.Caret.Line);
				FTextEdit.ActiveTextAreaControl.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						FTextEdit.Document, 
						new Point(0, FTextEdit.ActiveTextAreaControl.Caret.Line),
						new Point(LLine.Length, FTextEdit.ActiveTextAreaControl.Caret.Line)
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

		void IErrorSource.ErrorHighlighted(Exception AException)
		{
			// nothing
		}

		private void GotoPosition(int ALine, int ALinePos)
		{
			if (ALine >= 1)
			{
				if (ALinePos < 1)
					ALinePos = 1;
				FTextEdit.ActiveTextAreaControl.SelectionManager.ClearSelection();
				FTextEdit.ActiveTextAreaControl.Caret.Position = new Point(ALinePos - 1, ALine - 1);
				FTextEdit.ActiveTextAreaControl.Caret.ValidateCaretPos();
				FTextEdit.ActiveTextAreaControl.TextArea.SetDesiredColumn();
				FTextEdit.ActiveTextAreaControl.Invalidate();
			}
		}

		void IErrorSource.ErrorSelected(Exception AException)
		{
			ILocatedException LException = AException as ILocatedException;
			if (LException != null)
				GotoPosition(LException.Line, LException.LinePos);
			SelectNextControl(this, true, true, true, false);
		}

		protected override void OnClosing(CancelEventArgs AArgs)
		{
			base.OnClosing(AArgs);
			if (FExecutor != null)
			{
				if (MessageBox.Show(Strings.AttemptingToCloseWhileExecuting, Strings.AttemptingToCloseWhileExecutingCaption, MessageBoxButtons.OKCancel) == DialogResult.OK)
					CancelExecute();
				else
					AArgs.Cancel = true;
			}
		}

		#endregion

		#region Exporting

		private void PromptAndExport(ExportType ATypes)
		{
			using (SaveFileDialog LSaveDialog = new SaveFileDialog())
			{
				LSaveDialog.Title = Strings.ExportSaveDialogTitle;
				LSaveDialog.Filter = "XML Schema (*.xsd)|*.xsd|XML File (*.xml)|*.xml|All Files (*.*)|*.*";
				if (ATypes == ExportType.Schema)	// schema only
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

				this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
				try
				{
					Execute(ATypes, LSaveDialog.FileName);
				}
				finally
				{
					this.Cursor = System.Windows.Forms.Cursors.Default;
				}
			}

		}

		private void Execute(ExportType AExportType, string AFileName)
		{
			// Make sure our server is connected...
			Dataphoria.EnsureServerConnection();

			using (Frontend.Client.Windows.StatusForm LStatusForm = new Frontend.Client.Windows.StatusForm(Strings.Exporting))
			{
				using (DAEConnection LConnection = new DAEConnection())
				{
					if (DesignerID == "SQL")
						SwitchToSQL();
					try
					{
						using (DAEDataAdapter LAdapter = new DAEDataAdapter(GetTextToExecute(), LConnection))
						{
							using (System.Data.DataSet LDataSet = new System.Data.DataSet())
							{
								IServerProcess LProcess = (IServerProcess)DataSession.ServerSession.StartProcess(new ProcessInfo(DataSession.ServerSession.SessionInfo));
								try
								{
									LConnection.Open((IServer)DataSession.Server, (IServerSession)DataSession.ServerSession, LProcess);
									try
									{
										switch (AExportType)
										{
											case ExportType.Data : 
												LAdapter.Fill(LDataSet);
												LDataSet.WriteXml(AFileName, XmlWriteMode.IgnoreSchema);
												break;
											case ExportType.Schema :
												LAdapter.FillSchema(LDataSet, SchemaType.Source);
												LDataSet.WriteXmlSchema(AFileName);
												break;
											default :
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

		private PictureBox FWorkingAnimation;
		
		protected Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel FExecutionTimeStatus;
		protected Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel FWorkingStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();
			
			FExecutionTimeStatus = new Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel();
			FExecutionTimeStatus.Text = "0:00:00";
			FExecutionTimeStatus.SizeToContent = true;
			FExecutionTimeStatus.HAlign = Syncfusion.Windows.Forms.Tools.HorzFlowAlign.Right;
			FExecutionTimeStatus.Alignment = HorizontalAlignment.Center;
			FExecutionTimeStatus.BorderStyle = BorderStyle.FixedSingle;
			FExecutionTimeStatus.BorderColor = Color.Yellow;

			FWorkingAnimation = new PictureBox();
			FWorkingAnimation.Image = System.Drawing.Image.FromStream(GetType().Assembly.GetManifestResourceStream("Alphora.Dataphor.Dataphoria.Images.Rider.gif"));
			FWorkingAnimation.SizeMode = PictureBoxSizeMode.AutoSize;
			FWorkingAnimation.Visible = false;

			FWorkingStatus = new Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel();
			FWorkingStatus.Text = "";
			FWorkingStatus.HAlign = Syncfusion.Windows.Forms.Tools.HorzFlowAlign.Right;
			FWorkingStatus.Alignment = HorizontalAlignment.Center;
			FWorkingStatus.BorderStyle = BorderStyle.None;
			FWorkingStatus.Controls.Add(FWorkingAnimation);
			FWorkingStatus.Size = FWorkingAnimation.Size;
		}

		protected override void DisposeStatusBar()
		{
			base.DisposeStatusBar();

			FWorkingAnimation.Image.Dispose();	//otherwise the animation thread will still be hanging around
			
			FWorkingStatus.Dispose();
			FWorkingStatus = null;

			FExecutionTimeStatus.Dispose();
			FExecutionTimeStatus = null;
		}

		public override void Merge(Control AStatusBar)
		{
			base.Merge(AStatusBar);

			AStatusBar.Controls.Add(FExecutionTimeStatus);
			AStatusBar.Controls.Add(FWorkingStatus);
		}

		public override void Unmerge(Control AStatusBar)
		{
			base.Unmerge(AStatusBar);

			AStatusBar.Controls.Remove(FExecutionTimeStatus);
			AStatusBar.Controls.Remove(FWorkingStatus);
		}

		#endregion

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
	}

	public class ToggleBlockDelimiter : SD.Actions.AbstractEditAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			// Add a block delimiter on the current line

			Point LPosition = ATextArea.Caret.Position;
			SD.Document.LineSegment LSegment = ATextArea.Document.GetLineSegment(LPosition.Y);
			string LTextContent = ATextArea.Document.GetText(LSegment);

			if (LTextContent.IndexOf("//*") == 0)
			{
				ATextArea.Document.Remove(LSegment.Offset, 3);
				ATextArea.Refresh();	// Doesn't refresh properly (if on the last line) without this
			}
			else
			{
				ATextArea.Document.Insert(LSegment.Offset, "//*");
				if (LPosition.X == 0)
				{
					ATextArea.Caret.Position = new Point(3, LPosition.Y);
					ATextArea.SetDesiredColumn();
				}
			}
		}
	}

	public abstract class BaseBlockAction : SD.Actions.AbstractEditAction
	{
		protected int GetPriorBlockOffset(SD.TextArea ATextArea)
		{
			string LTextContent = ATextArea.Document.TextContent.Substring(0, ATextArea.Caret.Offset);
			int LPriorBlock = LTextContent.LastIndexOf("//*");
			if (LPriorBlock < 0)
				LPriorBlock = 0;

			return LPriorBlock;
		}

		protected int GetNextBlockOffset(SD.TextArea ATextArea)
		{
			bool AAtBlockStart;
			return GetNextBlockOffset(ATextArea, out AAtBlockStart);
		}

		protected int GetNextBlockOffset(SD.TextArea ATextArea, out bool AAtBlockStart)
		{
			AAtBlockStart = false;
			string LTextContent = ATextArea.Document.TextContent.Substring(ATextArea.Caret.Offset);
			int LNextBlock = LTextContent.IndexOf("//*");
			if (LNextBlock == 0)
			{
				AAtBlockStart = true;
				LTextContent = LTextContent.Substring(LNextBlock + 3);
				LNextBlock = LTextContent.IndexOf("//*");
				if (LNextBlock >= 0)
					LNextBlock += 3;
			}

			if (LNextBlock < 0)
				LNextBlock = ATextArea.Document.TextLength;
			else
				LNextBlock = ATextArea.Caret.Offset + LNextBlock;

			return LNextBlock;
		}
	}

	public class SelectBlock : BaseBlockAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			if (!ATextArea.SelectionManager.HasSomethingSelected && (ATextArea.Document.TextLength > 0))
			{
				int LCurrentOffset = ATextArea.Caret.Offset;
				bool AAtBlockStart;
				int LEndOffset = GetNextBlockOffset(ATextArea, out AAtBlockStart);
				int LStartOffset;
				if (AAtBlockStart)
					LStartOffset = LCurrentOffset;
				else
					LStartOffset = GetPriorBlockOffset(ATextArea);

				ATextArea.SelectionManager.SetSelection
				(
					new SD.Document.DefaultSelection
					(
						ATextArea.Document,
						ATextArea.Document.OffsetToPosition(LStartOffset),
						ATextArea.Document.OffsetToPosition(LEndOffset)
					)
				);
			}
		}
	}

	public class SelectPriorBlock : BaseBlockAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			if (ATextArea.Document.TextLength > 0)
			{
				int LCurrentOffset = ATextArea.Caret.Offset;
				int LStartOffset = GetPriorBlockOffset(ATextArea);
				
				// Move the caret
				ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LStartOffset);
				ATextArea.SetDesiredColumn();

				// Set or extend the selection
				ATextArea.AutoClearSelection = false;
				if (ATextArea.SelectionManager.HasSomethingSelected)
				{
					// Extend the selection
					ATextArea.SelectionManager.ExtendSelection
					(
						ATextArea.Document.OffsetToPosition(LCurrentOffset),
						ATextArea.Document.OffsetToPosition(LStartOffset)
					);
				}
				else
				{
					// Select from the current caret position to the beginning of the block
					ATextArea.SelectionManager.SetSelection
					(
						new SD.Document.DefaultSelection
						(
							ATextArea.Document,
							ATextArea.Document.OffsetToPosition(LStartOffset),
							ATextArea.Document.OffsetToPosition(LCurrentOffset)
						)
					);
				}
			}
		}
	}

	public class PriorBlock : BaseBlockAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			if (ATextArea.Document.TextLength > 0)
			{
				int LCurrentOffset = ATextArea.Caret.Offset;
				int LPriorBlock = GetPriorBlockOffset(ATextArea);
				ATextArea.AutoClearSelection = true;
				ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LPriorBlock);
				ATextArea.SetDesiredColumn();
			}
		}
	}

	public class NextBlock : BaseBlockAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			if (ATextArea.Document.TextLength > 0)
			{
				int LCurrentOffset = ATextArea.Caret.Offset;
				int LNextBlock = GetNextBlockOffset(ATextArea);
				ATextArea.AutoClearSelection = true;
				ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LNextBlock);
				ATextArea.SetDesiredColumn();
			}
		}
	}

	public class SelectNextBlock : BaseBlockAction
	{
		public override void Execute(SD.TextArea ATextArea)
		{
			if (ATextArea.Document.TextLength > 0)
			{
				int LCurrentOffset = ATextArea.Caret.Offset;
				int LEndOffset = GetNextBlockOffset(ATextArea);

				// Move the caret
				ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LEndOffset);
				ATextArea.SetDesiredColumn();

				// Set or extend the selection
				ATextArea.AutoClearSelection = false;
				if (ATextArea.SelectionManager.HasSomethingSelected)
				{
					// Extend the selection
					ATextArea.SelectionManager.ExtendSelection
					(
						ATextArea.Document.OffsetToPosition(LCurrentOffset),
						ATextArea.Document.OffsetToPosition(LEndOffset)
					);
				}
				else
				{
					// Select from the current position to the end of the block
					ATextArea.SelectionManager.SetSelection
					(
						new SD.Document.DefaultSelection
						(
							ATextArea.Document,
							ATextArea.Document.OffsetToPosition(LCurrentOffset),
							ATextArea.Document.OffsetToPosition(LEndOffset)
						)
					);
				}
			}
		}
	}

	[Flags]
	public enum ExportType { Data = 1, Schema = 2 }

	/// <summary> Asyncronously executes queries. </summary>
	/// <remarks> Should not be used multiple times (create another instance). </remarks>
	internal class ScriptExecutor : Object
	{
		public const int CStopTimeout = 10000;	// ten seconds to synchronously stop

		public ScriptExecutor(IServerSession ASession, String AScript, ReportScriptProgressHandler AExecuteProgress, ExecuteFinishedHandler AExecuteFinished)
		{
			FSession = ASession;
			FScript = AScript;
			FExecuteFinished = AExecuteFinished;
			FExecuteProgress = AExecuteProgress;
		}

		// Inputs to ExecuteAsync
		private IServerSession FSession;
		private string FScript;
		private IServerProcess FProcess;
		private ExecuteFinishedHandler FExecuteFinished;
		private ReportScriptProgressHandler FExecuteProgress;

		private Thread FAsyncThread;
		private int FProcessID;

		public void Start()
		{
			if (!FIsRunning)
			{
				CleanupProcess();
				FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
				FProcessID = FProcess.ProcessID;
				FIsRunning = true;
				FAsyncThread = new Thread(new ThreadStart(ExecuteAsync));
				FAsyncThread.Start();
			}
		}

		private void CleanupProcess()
		{
			if (FProcess != null)
			{
				try
				{
					FSession.StopProcess(FProcess);
				}
				catch
				{
					// Don't rethrow, the session may have already been stopped
				}
				FProcess = null;
				FProcessID = 0;
			}
		}

		public void Stop()
		{
			if (FIsRunning)
			{
				FIsRunning = false;

				// Asyncronously request that the server process be stopped
				new AsyncStopHandler(AsyncStop).BeginInvoke(FProcessID, FSession, null, null);
				FProcess = null;
				FProcessID = 0;
			}
		}

		private delegate void AsyncStopHandler(int AProcessID, IServerSession ASession);

		private void AsyncStop(int AProcessID, IServerSession ASession)
		{
			IServerProcess LProcess = ASession.StartProcess(new ProcessInfo(FSession.SessionInfo));
			try
			{
				LProcess.Execute("StopProcess(" + AProcessID.ToString() + ")", null);
			}
			finally
			{
				ASession.StopProcess(LProcess);
			}
		}

		private bool FIsRunning = false;
		public bool IsRunning
		{
			get { return FIsRunning; }
		}

		private void ExecuteAsync()
		{
			try
			{
				ErrorList LErrors = null;
				TimeSpan LElapsed = TimeSpan.Zero;
				string LResult = String.Empty;

				try
				{
					ScriptExecutionUtility.ExecuteScript
					(
						FProcess,
						FScript,
						ScriptExecuteOption.All,
						out LErrors,
						out LElapsed,
						delegate(PlanStatistics AStatistics, string AResults)
						{
							Session.SafelyInvoke(new ReportScriptProgressHandler(AsyncProgress), new object[] { AStatistics, AResults });
						}
					);
				}
				finally
				{
					Session.SafelyInvoke(new ExecuteFinishedHandler(AsyncFinish), new object[] { LErrors, LElapsed });
				}
			}
			catch
			{
				// Don't allow exceptions to go unhandled... the framework will abort the application
			}
		}

		private void AsyncProgress(PlanStatistics AStatistics, string AResults)
		{
			// Return what results we got even if stopped.
			FExecuteProgress(AStatistics, AResults);
		}

		private void AsyncFinish(ErrorList AErrors, TimeSpan AElapsedTime)
		{
			CleanupProcess();
			if (FIsRunning)
				FExecuteFinished(AErrors, AElapsedTime);
		}
	}

	internal delegate void ExecuteFinishedHandler(ErrorList AErrors, TimeSpan AElapsedTime);
}
