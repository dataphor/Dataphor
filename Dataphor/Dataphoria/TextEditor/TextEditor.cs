/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Alphora.Dataphor.DAE.Client;
using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using ICSharpCode.TextEditor.Document;
using WeifenLuo.WinFormsUI.Docking;
using SD = ICSharpCode.TextEditor;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
    /// <summary> Text Editor form for Dataphoria. </summary>
    public partial class TextEditor : BaseForm, IToolBarClient, IDesigner
    {
        private string FDesignerID;
        protected DockContent FDockContentTextEdit;
        private IDesignService FService;
        protected TextEdit FTextEdit;

        public TextEditor() // for the designer
        {
            InitializeComponent();
            InitializeDocking();
        }

        public TextEditor(IDataphoria ADataphoria, string ADesignerID)
        {
            InitializeComponent();

            InitializeDocking();

            FDesignerID = ADesignerID;

            FService = new DesignService(ADataphoria, this);
            FService.OnModifiedChanged += NameOrModifiedChanged;
            FService.OnNameChanged += NameOrModifiedChanged;
            FService.OnRequestLoad += LoadData;
            FService.OnRequestSave += SaveData;
			FService.LocateRequested += LocateRequested;

            FTextEdit.HelpRequested += FTextArea_HelpRequested;

            FTextEdit.Document.HighlightingStrategy = GetHighlightingStrategy();
            FTextEdit.DocumentChanged += DocumentChanged;
            TextEditInitialized(FTextEdit, FTextEdit.ActiveTextAreaControl);
            FTextEdit.OnInitializeTextAreaControl += TextEditInitialized;
            FTextEdit.BeginningFind += BeginningFind;
            FTextEdit.ReplacementsPerformed += ReplacementsPerformed;
            FTextEdit.TextNotFound += TextNotFound;

            UpdateLineNumberStatus();
            UpdateTitle();
        }

        // Dataphoria

        protected IDataphoria Dataphoria
        {
            get { return (FService == null ? null : FService.Dataphoria); }
        }

        // DataSession

        protected DataSession DataSession
        {
            get { return FService.Dataphoria.DataSession; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EditorText
        {
            get { return FTextEdit.Document.TextContent; }
            set { FTextEdit.SetText(value); }
        }

        #region IToolBarClient Members

        public virtual void MergeToolbarWith(ToolStrip AParentToolStrip)
        {
            ToolStripManager.Merge(FToolStrip, AParentToolStrip);
        }

        #endregion

        #region IDesigner Members

        [Browsable(false)]
        public IDesignService Service
        {
            get { return FService; }
        }

        // IDesigner

        public void Open(DesignBuffer ABuffer)
        {
            FService.Open(ABuffer);
        }

        public void New()
        {
            FService.New();
        }

        /// <remarks> 
        ///		Note that this method should not be confused with Form.Close().  
        ///		Be sure to deal with a compile-time instance of type IDesigner 
        ///		to invoke this method. 
        ///	</remarks>
        void IDesigner.Show()
        {
            Dataphoria.AttachForm(this);
        }

        [Browsable(false)]
        public string DesignerID
        {
            get { return FDesignerID; }
        }

        public bool CloseSafely()
        {
            Close();
            return IsDisposed;
        }

        #endregion

        private void InitializeDocking()
        {
            // 
            // FTextEdit
            // 
            FTextEdit = new TextEdit
                            {
                                CausesValidation = false,
                                EnableFolding = false,
                                IndentStyle = IndentStyle.Auto,
                                Location = new Point(0, 0),
                                Name = "FTextEdit",
                                ShowInvalidLines = false,
                                ShowLineNumbers = false,
                                ShowVRuler = true,
                                Size = new Size(455, 376),
                                TabIndent = 3,
                                VRulerRow = 100,
                                Dock = DockStyle.Fill
                            };

        	
			
			FDockContentTextEdit = new DockContent();			
            FDockContentTextEdit.HideOnClose = true;
            FDockContentTextEdit.Controls.Add(FTextEdit);
            FDockContentTextEdit.ShowHint = DockState.Document;
            FDockContentTextEdit.Show(FDockPanel);
            FDockContentTextEdit.Name = "DockContentTextEdit";
			FDockContentTextEdit.ActiveControl = FTextEdit;
		}

		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			FDockContentTextEdit.ActiveControl = FTextEdit;
			FTextEdit.Focus();
		}
		
        protected virtual IHighlightingStrategy GetHighlightingStrategy()
        {
            switch (FDesignerID)
            {
                case "XML":
                    return HighlightingStrategyFactory.CreateHighlightingStrategy("XML");
                case "CS":
                    return HighlightingStrategyFactory.CreateHighlightingStrategy("CS");
                case "VB":
                    return HighlightingStrategyFactory.CreateHighlightingStrategy("VBNET");
                default:
                    return HighlightingStrategyFactory.CreateHighlightingStrategy("Default");
            }
        }

        private void TextEditInitialized(object ASender, TextAreaControl ANewControl)
        {
            ANewControl.TextArea.Caret.PositionChanged += CaretPositionChanged;
            ANewControl.TextArea.Enter += TextAreaEnter;
        }

        // EditorText

        // Title

        private void UpdateTitle()
        {
            this.TabText = FService.GetDescription() + (FService.IsModified ? "*" : "");            
        }

        // Service callbacks

        protected virtual void NameOrModifiedChanged(object ASender, EventArgs AArgs)
        {
            UpdateTitle();
        }

        private void LoadData(DesignService AService, DesignBuffer ABuffer)
        {
            EditorText = ABuffer.LoadData();
        }

        private void SaveData(DesignService AService, DesignBuffer ABuffer)
        {
            ABuffer.SaveData(EditorText);
        }

		private void LocateRequested(DesignService AService, Alphora.Dataphor.DAE.Debug.DebugLocator ALocator)
		{
			if (ALocator.Line >= 1)
				GotoPosition(ALocator.Line, 1);
		}

		// Status bar

        private void TextAreaEnter(object ASender, EventArgs AArgs)
        {
            UpdateLineNumberStatus();
        }

        private void CaretPositionChanged(object ASender, EventArgs AArgs)
        {
            UpdateLineNumberStatus();
        }

        private void UpdateLineNumberStatus()
        {
            FPositionStatus.Text = FTextEdit.GetLineNumberText();
        }

        // Form events

        public void DocumentChanged(object ASender, DocumentEventArgs AArgs)
        {
            FService.SetModified(true);
            SetStatus(String.Empty);
        }

        private void Print()
        {
            using (var LDialog = new PrintDialog())
            {
                LDialog.Document = FTextEdit.PrintDocument;
                LDialog.AllowSomePages = true;
                if (LDialog.ShowDialog() == DialogResult.OK)
                    FTextEdit.PrintDocument.Print();
            }
        }

        private void Undo()
        {
            FTextEdit.Undo();
        }

        private void Redo()
        {
            FTextEdit.Redo();
        }

        private void Split()
        {
            FTextEdit.Split();
        }

        protected void BeginningFind(object ASender, EventArgs AArgs)
        {
            SetStatus(String.Empty);
        }

        protected void ReplacementsPerformed(object ASender, int ACount)
        {
            SetStatus(String.Format(Strings.ReplacementsPerformed, ACount));
        }

        protected void TextNotFound(object ASender, EventArgs AArgs)
        {
            SetStatus(Strings.FindTextNotFound);
        }

        private void Replace()
        {
            FTextEdit.PromptFindReplace(true);
        }

        private void Find()
        {
            FTextEdit.PromptFindReplace(false);
        }

        private void FindNext()
        {
            try
            {
                SetStatus(String.Empty);
                FTextEdit.FindAgain();
            }
            catch (Exception LException)
            {
                SetStatus(LException.Message);
                // Don't rethrow
            }
        }

        protected override void OnActivated(EventArgs AArgs)
        {
            ActiveControl = FTextEdit; // always focus on the editor control
            base.OnActivated(AArgs);
        }

        protected override void OnClosing(CancelEventArgs AArgs)
        {
            base.OnClosing(AArgs);
            FService.CheckModified();
        }

        private void ShowSaved()
        {
            SetStatus(Strings.Saved);
        }

        private void FMainMenuStrip_ItemClicked(object ASender, EventArgs AArgs)
        {
            try
            {
                if (ASender == FCloseToolStripMenuItem)
                {
                    Close();
                }
                else if (ASender == FPrintToolStripMenuItem || ASender == FPrintToolStripButton)
                {
                    Print();
                }
                else if (ASender == FCutToolStripMenuItem || ASender == FCutToolStripButton)
                {
                    new Cut().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
                }
                else if (ASender == FCopyToolStripMenuItem || ASender == FCopyToolStripButton)
                {
                    new Copy().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
                }
                else if (ASender == FPasteToolStripButton || ASender == FPasteToolStripButton)
                {
                    new Paste().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
                }
                else if (ASender == FSelectAllToolStripMenuItem)
                {
                    new SelectWholeDocument().Execute(FTextEdit.ActiveTextAreaControl.TextArea);
                }
                else if (ASender == FSaveAsDocumentToolStripMenuItem || ASender == FSaveAsDocumentToolStripButton)
                {
                    FService.SaveAsDocument();
                    ShowSaved();
                }
                else if (ASender == FSaveAsFileToolStripMenuItem || ASender == FSaveAsFileToolStripButton)
                {
                    FService.SaveAsFile();
                    ShowSaved();
                }
                else if (ASender == FSaveToolStripMenuItem || ASender == FSaveToolStripButton)
                {
                    FService.Save();
                    ShowSaved();
                }
                else if (ASender == FFindToolStripMenuItem || ASender == FFindToolStripButton)
                {
                    Find();
                }
                else if (ASender == FReplaceToolStripMenuItem || ASender == FReplaceToolStripButton)
                {
                    Replace();
                }
                else if (ASender == FFindNextToolStripMenuItem || ASender == FFindNextToolStripButton)
                {
                    FindNext();
                }
                else if (ASender == FUndoToolStripMenuItem || ASender == FUndoToolStripButton)
                {
                    Undo();
                }
                else if (ASender == FRedoToolStripMenuItem || ASender == FRedoToolStripButton)
                {
                    Redo();
                }
                else if (ASender == FSplitWindowToolStripMenuItem)
                {
                    Split();
                }
            }
            catch (AbortException)
            {
                // nothing
            }
        }


        private void FTextArea_HelpRequested(object ASender, HelpEventArgs AArgs)
        {
            if (FTextEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
                Dataphoria.InvokeHelp(FTextEdit.ActiveTextAreaControl.SelectionManager.SelectedText);
            else
            {
                string LWord = "";
                // the next 7 lines added/modified to overcome bug in FindWordStart
                // runtime error if Caret is at end of document
                int LCaretOffset = FTextEdit.ActiveTextAreaControl.Caret.Offset;
                int LTextLength = FTextEdit.Document.TextLength;
                if (LTextLength > 0)
                {
                    if (LCaretOffset == LTextLength)
                        LCaretOffset--;
                    int LFirst = TextEdit.FindWordStart(FTextEdit.Document, LCaretOffset);
                    int LLast = TextEdit.FindWordEnd(FTextEdit.Document, LCaretOffset);
                    LWord = FTextEdit.Document.GetText(LFirst, LLast - LFirst);
                }
                if (LWord.Trim().Length == 0)
                    Dataphoria.InvokeHelp(DesignerID);
                else
                    Dataphoria.InvokeHelp(LWord);
            }
        }

		protected void GotoPosition(int ALine, int ALinePos)
		{
			if (ALine >= 1)
			{
				if (ALinePos < 1)
					ALinePos = 1;
				FTextEdit.ActiveTextAreaControl.SelectionManager.ClearSelection();
				FTextEdit.ActiveTextAreaControl.Caret.Position = new TextLocation(ALinePos - 1, ALine - 1);
				FTextEdit.ActiveTextAreaControl.Caret.ValidateCaretPos();
				FTextEdit.ActiveTextAreaControl.TextArea.SetDesiredColumn();
				FTextEdit.ActiveTextAreaControl.Invalidate();
			}
		}

		#region StatusBar

        protected ToolStripStatusLabel FPositionStatus;

        protected override void InitializeStatusBar()
        {
            base.InitializeStatusBar();

			this.FPositionStatus = new ToolStripStatusLabel
			{
				Text = "0:0",
				Name = "FPositionStatus",
				MergeIndex = 100,
				BorderSides = ToolStripStatusLabelBorderSides.All,
				Alignment = ToolStripItemAlignment.Right,
				DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
			};

			FStatusStrip.Items.Add(FPositionStatus);
        }

        #endregion
    }
}