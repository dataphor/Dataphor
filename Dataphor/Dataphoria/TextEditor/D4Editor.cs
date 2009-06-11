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
using System.Threading;
using System.Windows.Forms;
using Alphora.Dataphor.DAE;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.DAE.Client.Provider;
using Alphora.Dataphor.DAE.Language.D4;
using Alphora.Dataphor.DAE.Runtime;
using Alphora.Dataphor.Frontend.Client.Windows;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using WeifenLuo.WinFormsUI.Docking;
using DataSet=System.Data.DataSet;
using Image=System.Drawing.Image;
using SD = ICSharpCode.TextEditor;

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
        private ToolStripMenuItem FInjectMenuItem;
        private ToolStripMenuItem FNextBlockMenuItem;
        private ToolStripMenuItem FPrepareLineMenuItem;
        private ToolStripMenuItem FPrepareMenuItem;
        private ToolStripMenuItem FPriorBlockMenuItem;
        private ResultPanel FResultPanel;

        private ToolStripMenuItem FScriptMenu;
        private ToolStripMenuItem FSelectBlockMenuItem;
        private ToolStripMenuItem FShowResultsMenuItem;


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

            FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemQuestion] = new ToggleBlockDelimiter();
            FTextEdit.EditActions[Keys.Control | Keys.Oemcomma] = new PriorBlock();
            FTextEdit.EditActions[Keys.Control | Keys.OemPeriod] = new NextBlock();
            FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.Oemcomma] = new SelectPriorBlock();
            FTextEdit.EditActions[Keys.Shift | Keys.Control | Keys.OemPeriod] = new SelectNextBlock();

            FResultPanel.BeginningFind += BeginningFind;
            FResultPanel.ReplacementsPerformed += ReplacementsPerformed;
            FResultPanel.TextNotFound += TextNotFound;
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
            FDockContentResultPanel.Controls.Add(FResultPanel);
            FDockContentResultPanel.Text = "Results";
            FDockContentResultPanel.TabText = "D4 Results";
            FDockContentResultPanel.ShowHint = DockState.DockBottomAutoHide;
            FDockContentResultPanel.Show(FDockPanel);
        }


        private void InitializeExtendendMenu()
        {
            FScriptMenu = new ToolStripMenuItem();
            FExecuteMenuItem = new ToolStripMenuItem();
            FCancelMenuItem = new ToolStripMenuItem();
            FPrepareMenuItem = new ToolStripMenuItem();
            FAnalyzeMenuItem = new ToolStripMenuItem();
            FInjectMenuItem = new ToolStripMenuItem();
            FExportMenu = new ToolStripMenuItem();
            FExecuteSchemaMenuItem = new ToolStripMenuItem();
            FExportDataMenuItem = new ToolStripMenuItem();
            FExecuteBothMenuItem = new ToolStripMenuItem();
            FExecuteLineMenuItem = new ToolStripMenuItem();
            FPrepareLineMenuItem = new ToolStripMenuItem();
            FAnalyzeLineMenuItem = new ToolStripMenuItem();
            FSelectBlockMenuItem = new ToolStripMenuItem();
            FPriorBlockMenuItem = new ToolStripMenuItem();
            FNextBlockMenuItem = new ToolStripMenuItem();
            FShowResultsMenuItem = new ToolStripMenuItem();

            // 
            // FViewMenu
            // 
            FMenuStrip.Items.AddRange(new[]
                                          {
                                              FScriptMenu
                                          });

            //
            //FViewToolStripMenuItem
            //
            FViewToolStripMenuItem.DropDownItems.Add(FShowResultsMenuItem);
            // 
            // FScriptMenu
            // 
            FScriptMenu.DropDownItems.AddRange(new[]
                                                   {
                                                       FExecuteMenuItem,
                                                       FCancelMenuItem,
                                                       FPrepareMenuItem,
                                                       FAnalyzeMenuItem,
                                                       FInjectMenuItem,
                                                       FExportMenu
                                                   });
            FScriptMenu.Text = "&Script";
            // 
            // FExecuteMenuItem
            // 

            FExecuteMenuItem.ImageIndex = 0;
            FExecuteMenuItem.Text = "&Execute";
            FExecuteMenuItem.Click += FMainMenuStrip_ItemClicked;
            FExecuteMenuItem.Image = MenuImages.Execute;
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
            // 
            // FAnalyzeMenuItem
            // 

            FAnalyzeMenuItem.ImageIndex = 4;
            FAnalyzeMenuItem.Text = "&Analyze";
            FAnalyzeMenuItem.Click += FMainMenuStrip_ItemClicked;
            FAnalyzeMenuItem.Image = MenuImages.Analyze;
            // 
            // FInjectMenuItem
            // 

            FInjectMenuItem.ImageIndex = 3;
            FInjectMenuItem.Text = "&Inject As Upgrade";
            FInjectMenuItem.Click += FMainMenuStrip_ItemClicked;
            FInjectMenuItem.Image = MenuImages.Inject;
            // 
            // FExportMenu
            // 

            FExportMenu.DropDownItems.AddRange(new[]
                                                   {
                                                       FExecuteSchemaMenuItem,
                                                       FExportDataMenuItem,
                                                       FExecuteBothMenuItem
                                                   });
            FExportMenu.Text = "E&xport";
            FExportMenu.Visible = false;            
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
            FExecuteBothMenuItem.Click += FMainMenuStrip_ItemClicked;
            // 
            // FPrepareLineMenuItem
            // 
            FPrepareLineMenuItem.Text = "P&repare Line";
            FPrepareLineMenuItem.Click += FMainMenuStrip_ItemClicked;
            // 
            // FAnalyzeLineMenuItem
            // 
            FAnalyzeLineMenuItem.Text = "A&nalyze Line";
            FAnalyzeLineMenuItem.Click += FMainMenuStrip_ItemClicked;
            // 
            // FSelectBlockMenuItem
            // 
            FSelectBlockMenuItem.Text = "Select &Block";
            FSelectBlockMenuItem.Click += FMainMenuStrip_ItemClicked;            
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
            //FDockingManager.SetDockVisibility(FResultPanel, true);
            // TODO: There is a problem with Syncfusion that crashes the whole app if the FResultPanel is pinned and the following code runs
//			FDockingManager.ActivateControl(FResultPanel);
        }


        private void FMainMenuStrip_ItemClicked(object ASender, EventArgs AArgs)
        {
            try
            {
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
                    new NextBlock().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
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
                else if (ASender == FInjectMenuItem)
                {
                    Inject();
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
            //FDockingManager.SetDockVisibility(FResultPanel, false);
        }

        private string GetTextToExecute()
        {
            return
                (
                    FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected
                        ?
                            FTextEdit.ActiveTextAreaControl.SelectionManager.SelectedText
                        :
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
            return Point.Empty;
        }

        private void ProcessErrors(ErrorList AErrors)
        {
            Point LOffset = GetSelectionPosition();

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
                                                        LResult.AppendFormat(Strings.PrepareSuccessful,
                                                                             new object[]
                                                                                 {
                                                                                     LPlan.Statistics.PrepareTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.CompileTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.OptimizeTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.BindingTime.
                                                                                         ToString()
                                                                                 });
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
                                                        LResult.AppendFormat(Strings.PrepareSuccessful,
                                                                             new object[]
                                                                                 {
                                                                                     LPlan.Statistics.PrepareTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.CompileTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.OptimizeTime.
                                                                                         ToString(),
                                                                                     LPlan.Statistics.BindingTime.
                                                                                         ToString()
                                                                                 });
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

            FExecutor = new ScriptExecutor(DataSession.ServerSession, GetTextToExecute(),
                                           ExecutorProgress,
                                           ExecutorFinished);
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

        protected override void OnClosing(CancelEventArgs AArgs)
        {
            base.OnClosing(AArgs);
            if (FExecutor != null)
            {
                if (
                    MessageBox.Show(Strings.AttemptingToCloseWhileExecuting,
                                    Strings.AttemptingToCloseWhileExecutingCaption, MessageBoxButtons.OKCancel) ==
                    DialogResult.OK)
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
                                    DataSession.ServerSession.StartProcess(
                                        new ProcessInfo(DataSession.ServerSession.SessionInfo));
                                try
                                {
                                    LConnection.Open(DataSession.Server,
                                                     DataSession.ServerSession, LProcess);
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

            /*FExecutionTimeStatus = new StatusBarAdvPanel
                                       {
                                           Text = "0:00:00",
                                           SizeToContent = true,
                                           HAlign = HorzFlowAlign.Right,
                                           Alignment = HorizontalAlignment.Center,
                                           BorderStyle = BorderStyle.FixedSingle,
                                           BorderColor = Color.Yellow
                                       };*/
            this.FExecutionTimeStatus = new ToolStripStatusLabel 
            {
                Name = "FStatusStrip",
                Dock = DockStyle.Bottom,
                Text = "0:00:00",                
            };

            const string LCResourceName = "Alphora.Dataphor.Dataphoria.Images.Rider.gif";
            Stream LManifestResourceStream = GetType().Assembly.GetManifestResourceStream(
                LCResourceName);
            Debug.Assert(LManifestResourceStream != null, "Resource must exist: " + LCResourceName);
                FWorkingAnimation = new PictureBox
                                        {
                                            Image = Image.FromStream(
                                                LManifestResourceStream),
                                            SizeMode = PictureBoxSizeMode.AutoSize                                            
                                        };
            FWorkingStatus = new ToolStripControlHost(FWorkingAnimation, "FWorkingStatus");
           
            /*FWorkingStatus = new StatusBarAdvPanel
                                 {
                                     Text = "",
                                     HAlign = HorzFlowAlign.Right,
                                     Alignment = HorizontalAlignment.Center,
                                     BorderStyle = BorderStyle.None
                                 };*/            
            FWorkingStatus.Size = FWorkingAnimation.Size;
            FWorkingStatus.Visible = false;
            
            FStatusStrip.Items.Add(FExecutionTimeStatus);
            FStatusStrip.Items.Add(FWorkingStatus);
        }

        protected override void DisposeStatusBar()
        {
            base.DisposeStatusBar();

            FWorkingAnimation.Image.Dispose(); //otherwise the animation thread will still be hanging around

            /*FWorkingStatus.Dispose();
            FWorkingStatus = null;

            FExecutionTimeStatus.Dispose();
            FExecutionTimeStatus = null;*/
        }

        

        #endregion
    }

    public class ToggleBlockDelimiter : AbstractEditAction
    {
        public override void Execute(TextArea ATextArea)
        {
            // Add a block delimiter on the current line

            Point LPosition = ATextArea.Caret.Position;
            LineSegment LSegment = ATextArea.Document.GetLineSegment(LPosition.Y);
            string LTextContent = ATextArea.Document.GetText(LSegment);

            if (LTextContent.IndexOf("//*") == 0)
            {
                ATextArea.Document.Remove(LSegment.Offset, 3);
                ATextArea.Refresh(); // Doesn't refresh properly (if on the last line) without this
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

    public abstract class BaseBlockAction : AbstractEditAction
    {
        protected int GetPriorBlockOffset(TextArea ATextArea)
        {
            string LTextContent = ATextArea.Document.TextContent.Substring(0, ATextArea.Caret.Offset);
            int LPriorBlock = LTextContent.LastIndexOf("//*");
            if (LPriorBlock < 0)
                LPriorBlock = 0;

            return LPriorBlock;
        }

        protected int GetNextBlockOffset(TextArea ATextArea)
        {
            bool LAtBlockStart;
            return GetNextBlockOffset(ATextArea, out LAtBlockStart);
        }

        protected int GetNextBlockOffset(TextArea ATextArea, out bool AAtBlockStart)
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
        public override void Execute(TextArea ATextArea)
        {
            if (!ATextArea.SelectionManager.HasSomethingSelected && (ATextArea.Document.TextLength > 0))
            {
                int LCurrentOffset = ATextArea.Caret.Offset;
                bool LAtBlockStart;
                int LEndOffset = GetNextBlockOffset(ATextArea, out LAtBlockStart);
                int LStartOffset;
                if (LAtBlockStart)
                    LStartOffset = LCurrentOffset;
                else
                    LStartOffset = GetPriorBlockOffset(ATextArea);

                ATextArea.SelectionManager.SetSelection
                    (
                    new DefaultSelection
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
        public override void Execute(TextArea ATextArea)
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
                        new DefaultSelection
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
        public override void Execute(TextArea ATextArea)
        {
            if (ATextArea.Document.TextLength > 0)
            {                
                int LPriorBlock = GetPriorBlockOffset(ATextArea);
                ATextArea.AutoClearSelection = true;
                ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LPriorBlock);
                ATextArea.SetDesiredColumn();
            }
        }
    }

    public class NextBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
        {
            if (ATextArea.Document.TextLength > 0)
            {                
                int LNextBlock = GetNextBlockOffset(ATextArea);
                ATextArea.AutoClearSelection = true;
                ATextArea.Caret.Position = ATextArea.Document.OffsetToPosition(LNextBlock);
                ATextArea.SetDesiredColumn();
            }
        }
    }

    public class SelectNextBlock : BaseBlockAction
    {
        public override void Execute(TextArea ATextArea)
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
                        new DefaultSelection
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
    public enum ExportType
    {
        Data = 1,
        Schema = 2
    }

    /// <summary> Asyncronously executes queries. </summary>
    /// <remarks> Should not be used multiple times (create another instance). </remarks>
    internal class ScriptExecutor : Object
    {
        public const int CStopTimeout = 10000; // ten seconds to synchronously stop
        private Thread FAsyncThread;

        private ExecuteFinishedHandler FExecuteFinished;
        private ReportScriptProgressHandler FExecuteProgress;
        private bool FIsRunning = false;
        private IServerProcess FProcess;

        private int FProcessID;
        private string FScript;
        private IServerSession FSession;

        public ScriptExecutor(IServerSession ASession, String AScript, ReportScriptProgressHandler AExecuteProgress,
                              ExecuteFinishedHandler AExecuteFinished)
        {
            FSession = ASession;
            FScript = AScript;
            FExecuteFinished = AExecuteFinished;
            FExecuteProgress = AExecuteProgress;
        }

        public bool IsRunning
        {
            get { return FIsRunning; }
        }

        public void Start()
        {
            if (!FIsRunning)
            {
                CleanupProcess();
                FProcess = FSession.StartProcess(new ProcessInfo(FSession.SessionInfo));
                FProcessID = FProcess.ProcessID;
                FIsRunning = true;
                FAsyncThread = new Thread(ExecuteAsync);
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

        private void AsyncStop(int AProcessID, IServerSession ASession)
        {
            IServerProcess LProcess = ASession.StartProcess(new ProcessInfo(FSession.SessionInfo));
            try
            {
                LProcess.Execute("StopProcess(" + AProcessID + ")", null);
            }
            finally
            {
                ASession.StopProcess(LProcess);
            }
        }

        private void ExecuteAsync()
        {
            try
            {
                ErrorList LErrors = null;
                TimeSpan LElapsed = TimeSpan.Zero;
                

                try
                {
                    ScriptExecutionUtility.ExecuteScript
                        (
                        FProcess,
                        FScript,
                        ScriptExecuteOption.All,
                        out LErrors,
                        out LElapsed,
                        (AStatistics, AResults) => Session.SafelyInvoke(new ReportScriptProgressHandler(AsyncProgress),
                                                                        new object[] {AStatistics, AResults})
                        );
                }
                finally
                {
                    Session.SafelyInvoke(new ExecuteFinishedHandler(AsyncFinish), new object[] {LErrors, LElapsed});
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

        #region Nested type: AsyncStopHandler

        private delegate void AsyncStopHandler(int AProcessID, IServerSession ASession);

        #endregion
    }

    internal delegate void ExecuteFinishedHandler(ErrorList AErrors, TimeSpan AElapsedTime);
}