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
        private string _designerID;
        protected DockContent _dockContentTextEdit;
        private IDesignService _service;
        protected TextEdit _textEdit;

        public TextEditor() // for the designer
        {
            InitializeComponent();
            InitializeDocking();
        }

        public TextEditor(IDataphoria dataphoria, string designerID)
        {
            InitializeComponent();

            InitializeDocking();

            _designerID = designerID;

            _service = new DesignService(dataphoria, this);
            _service.OnModifiedChanged += NameOrModifiedChanged;
            _service.OnNameChanged += NameOrModifiedChanged;
            _service.OnRequestLoad += LoadData;
            _service.OnRequestSave += SaveData;
			_service.LocateRequested += LocateRequested;

            _textEdit.HelpRequested += FTextArea_HelpRequested;

            _textEdit.Document.HighlightingStrategy = GetHighlightingStrategy();
            _textEdit.DocumentChanged += DocumentChanged;
            TextEditInitialized(_textEdit, _textEdit.ActiveTextAreaControl);
            _textEdit.OnInitializeTextAreaControl += TextEditInitialized;
            _textEdit.BeginningFind += BeginningFind;
            _textEdit.ReplacementsPerformed += ReplacementsPerformed;
            _textEdit.TextNotFound += TextNotFound;

            UpdateLineNumberStatus();
            UpdateTitle();
        }

        // Dataphoria

        protected IDataphoria Dataphoria
        {
            get { return (_service == null ? null : _service.Dataphoria); }
        }

        // DataSession

        protected DataSession DataSession
        {
            get { return _service.Dataphoria.DataSession; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EditorText
        {
            get { return _textEdit.Document.TextContent; }
            set { _textEdit.SetText(value); }
        }

        #region IToolBarClient Members

        public virtual void MergeToolbarWith(ToolStrip parentToolStrip)
        {
            ToolStripManager.Merge(FToolStrip, parentToolStrip);
        }

        #endregion

        #region IDesigner Members

        [Browsable(false)]
        public IDesignService Service
        {
            get { return _service; }
        }

        // IDesigner

        public void Open(DesignBuffer buffer)
        {
            _service.Open(buffer);
        }

        public void New()
        {
            _service.New();
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
            get { return _designerID; }
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
            _textEdit = new TextEdit
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

        	
			
			_dockContentTextEdit = new DockContent();			
            _dockContentTextEdit.HideOnClose = true;
            _dockContentTextEdit.Controls.Add(_textEdit);
            _dockContentTextEdit.ShowHint = DockState.Document;
            _dockContentTextEdit.Show(FDockPanel);
            _dockContentTextEdit.Name = "DockContentTextEdit";
			_dockContentTextEdit.ActiveControl = _textEdit;
		}

		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			_dockContentTextEdit.ActiveControl = _textEdit;
			_textEdit.Focus();
		}
		
        protected virtual IHighlightingStrategy GetHighlightingStrategy()
        {
            switch (_designerID)
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

        private void TextEditInitialized(object sender, TextAreaControl newControl)
        {
            newControl.TextArea.Caret.PositionChanged += CaretPositionChanged;
            newControl.TextArea.Enter += TextAreaEnter;
        }

        // EditorText

        // Title

        private void UpdateTitle()
        {
            this.TabText = _service.GetDescription() + (_service.IsModified ? "*" : "");            
        }

        // Service callbacks

        protected virtual void NameOrModifiedChanged(object sender, EventArgs args)
        {
            UpdateTitle();
        }

        private void LoadData(DesignService service, DesignBuffer buffer)
        {
            EditorText = buffer.LoadData();
        }

        private void SaveData(DesignService service, DesignBuffer buffer)
        {
            buffer.SaveData(EditorText);
        }

		private void LocateRequested(DesignService service, Alphora.Dataphor.DAE.Debug.DebugLocator locator)
		{
			if (locator.Line >= 1)
				GotoPosition(locator.Line, 1);
		}

		// Status bar

        private void TextAreaEnter(object sender, EventArgs args)
        {
            UpdateLineNumberStatus();
        }

        private void CaretPositionChanged(object sender, EventArgs args)
        {
            UpdateLineNumberStatus();
        }

        private void UpdateLineNumberStatus()
        {
            _positionStatus.Text = _textEdit.GetLineNumberText();
        }

        // Form events

        public void DocumentChanged(object sender, DocumentEventArgs args)
        {
            _service.SetModified(true);
            SetStatus(String.Empty);
        }

        private void Print()
        {
            using (var dialog = new PrintDialog())
            {
                dialog.Document = _textEdit.PrintDocument;
                dialog.AllowSomePages = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                    _textEdit.PrintDocument.Print();
            }
        }

        private void Undo()
        {
            _textEdit.Undo();
        }

        private void Redo()
        {
            _textEdit.Redo();
        }

        private void Split()
        {
            _textEdit.Split();
        }

        protected void BeginningFind(object sender, EventArgs args)
        {
            SetStatus(String.Empty);
        }

        protected void ReplacementsPerformed(object sender, int count)
        {
            SetStatus(String.Format(Strings.ReplacementsPerformed, count));
        }

        protected void TextNotFound(object sender, EventArgs args)
        {
            SetStatus(Strings.FindTextNotFound);
        }

        private void Replace()
        {
            _textEdit.PromptFindReplace(true);
        }

        private void Find()
        {
            _textEdit.PromptFindReplace(false);
        }

        private void FindNext()
        {
            try
            {
                SetStatus(String.Empty);
                _textEdit.FindAgain();
            }
            catch (Exception exception)
            {
                SetStatus(exception.Message);
                // Don't rethrow
            }
        }

        protected override void OnActivated(EventArgs args)
        {
            ActiveControl = _textEdit; // always focus on the editor control
            base.OnActivated(args);
        }

        protected override void OnClosing(CancelEventArgs args)
        {
            base.OnClosing(args);
            _service.CheckModified();
        }

        private void ShowSaved()
        {
            SetStatus(Strings.Saved);
        }

        private void FMainMenuStrip_ItemClicked(object sender, EventArgs args)
        {
            try
            {
                if (sender == FCloseToolStripMenuItem)
                {
                    Close();
                }
                else if (sender == FPrintToolStripMenuItem || sender == FPrintToolStripButton)
                {
                    Print();
                }
                else if (sender == FCutToolStripMenuItem || sender == FCutToolStripButton)
                {
                    new Cut().Execute(_textEdit.ActiveTextAreaControl.TextArea);
                }
                else if (sender == FCopyToolStripMenuItem || sender == FCopyToolStripButton)
                {
                    new Copy().Execute(_textEdit.ActiveTextAreaControl.TextArea);
                }
                else if (sender == FPasteToolStripButton || sender == FPasteToolStripButton)
                {
                    new Paste().Execute(_textEdit.ActiveTextAreaControl.TextArea);
                }
                else if (sender == FSelectAllToolStripMenuItem)
                {
                    new SelectWholeDocument().Execute(_textEdit.ActiveTextAreaControl.TextArea);
                }
                else if (sender == FSaveAsDocumentToolStripMenuItem || sender == FSaveAsDocumentToolStripButton)
                {
                    _service.SaveAsDocument();
                    ShowSaved();
                }
                else if (sender == FSaveAsFileToolStripMenuItem || sender == FSaveAsFileToolStripButton)
                {
                    _service.SaveAsFile();
                    ShowSaved();
                }
                else if (sender == FSaveToolStripMenuItem || sender == FSaveToolStripButton)
                {
                    _service.Save();
                    ShowSaved();
                }
                else if (sender == FFindToolStripMenuItem || sender == FFindToolStripButton)
                {
                    Find();
                }
                else if (sender == FReplaceToolStripMenuItem || sender == FReplaceToolStripButton)
                {
                    Replace();
                }
                else if (sender == FFindNextToolStripMenuItem || sender == FFindNextToolStripButton)
                {
                    FindNext();
                }
                else if (sender == FUndoToolStripMenuItem || sender == FUndoToolStripButton)
                {
                    Undo();
                }
                else if (sender == FRedoToolStripMenuItem || sender == FRedoToolStripButton)
                {
                    Redo();
                }
                else if (sender == FSplitWindowToolStripMenuItem)
                {
                    Split();
                }
            }
            catch (AbortException)
            {
                // nothing
            }
        }


        private void FTextArea_HelpRequested(object sender, HelpEventArgs args)
        {
            if (_textEdit.ActiveTextAreaControl.SelectionManager.HasSomethingSelected)
                Dataphoria.InvokeHelp(_textEdit.ActiveTextAreaControl.SelectionManager.SelectedText);
            else
            {
                string word = "";
                // the next 7 lines added/modified to overcome bug in FindWordStart
                // runtime error if Caret is at end of document
                int caretOffset = _textEdit.ActiveTextAreaControl.Caret.Offset;
                int textLength = _textEdit.Document.TextLength;
                if (textLength > 0)
                {
                    if (caretOffset == textLength)
                        caretOffset--;
                    int first = TextEdit.FindWordStart(_textEdit.Document, caretOffset);
                    int last = TextEdit.FindWordEnd(_textEdit.Document, caretOffset);
                    word = _textEdit.Document.GetText(first, last - first);
                }
                if (word.Trim().Length == 0)
                    Dataphoria.InvokeHelp(DesignerID);
                else
                    Dataphoria.InvokeHelp(word);
            }
        }

		protected void GotoPosition(int line, int linePos)
		{
			if (line >= 1)
			{
				if (linePos < 1)
					linePos = 1;
				_textEdit.ActiveTextAreaControl.SelectionManager.ClearSelection();
				_textEdit.ActiveTextAreaControl.Caret.Position = new TextLocation(linePos - 1, line - 1);
				_textEdit.ActiveTextAreaControl.Caret.ValidateCaretPos();
				_textEdit.ActiveTextAreaControl.TextArea.SetDesiredColumn();
				_textEdit.ActiveTextAreaControl.Invalidate();
			}
		}

		#region StatusBar

        protected ToolStripStatusLabel _positionStatus;

        protected override void InitializeStatusBar()
        {
            base.InitializeStatusBar();

			this._positionStatus = new ToolStripStatusLabel
			{
				Text = "0:0",
				Name = "FPositionStatus",
				MergeIndex = 100,
				BorderSides = ToolStripStatusLabelBorderSides.All,
				Alignment = ToolStripItemAlignment.Right,
				DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
			};

			FStatusStrip.Items.Add(_positionStatus);
        }

        #endregion
    }
}