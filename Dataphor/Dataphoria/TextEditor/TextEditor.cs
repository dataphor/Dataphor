/*
	Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/
using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using SD = ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Frontend.Client.Windows;
using WeifenLuo.WinFormsUI.Docking;

namespace Alphora.Dataphor.Dataphoria.TextEditor
{
	/// <summary> Text Editor form for Dataphoria. </summary>
    public partial class TextEditor : BaseForm, IDesigner, IChildFormWithToolBar
	{



        protected DockContent FDockContentTextEdit;
		protected Alphora.Dataphor.Frontend.Client.Windows.TextEdit FTextEdit;

		public TextEditor()	// for the designer
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
            FService.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
            FService.OnNameChanged += new EventHandler(NameOrModifiedChanged);
            FService.OnRequestLoad += new RequestHandler(LoadData);
            FService.OnRequestSave += new RequestHandler(SaveData);

            FTextEdit.HelpRequested += new HelpEventHandler(FTextArea_HelpRequested);

            FTextEdit.Document.HighlightingStrategy = GetHighlightingStrategy();
            FTextEdit.Document.DocumentChanged += new SD.Document.DocumentEventHandler(DocumentChanged);
            TextEditInitialized(FTextEdit, FTextEdit.ActiveTextAreaControl);
            FTextEdit.OnInitializeTextAreaControl += new InitializeTextAreaControlHandler(TextEditInitialized);
            FTextEdit.BeginningFind += new EventHandler(BeginningFind);
            FTextEdit.ReplacementsPerformed += new ReplacementsPerformedHandler(ReplacementsPerformed);
            FTextEdit.TextNotFound += new EventHandler(TextNotFound);
			//FEditorPanel.Controls.Add(FTextEdit);

			UpdateLineNumberStatus();
			UpdateTitle();
		}

        private void InitializeDocking()
        {
            // 
            // FTextEdit
            // 
            this.FTextEdit = new Alphora.Dataphor.Frontend.Client.Windows.TextEdit();
            this.FTextEdit.CausesValidation = false;
            this.FTextEdit.EnableFolding = false;
            this.FTextEdit.IndentStyle = ICSharpCode.TextEditor.Document.IndentStyle.Auto;
            this.FTextEdit.Location = new System.Drawing.Point(0, 0);
            this.FTextEdit.Name = "FTextEdit";
            this.FTextEdit.ShowInvalidLines = false;
            this.FTextEdit.ShowLineNumbers = false;
            this.FTextEdit.ShowVRuler = true;
            this.FTextEdit.Size = new System.Drawing.Size(455, 376);
            this.FTextEdit.TabIndent = 3;

            this.FTextEdit.VRulerRow = 100;
            this.FTextEdit.Dock = DockStyle.Fill;

            FDockContentTextEdit = new DockContent();
            FDockContentTextEdit.Controls.Add(FTextEdit);
            FDockContentTextEdit.ShowHint = DockState.Document;
            FDockContentTextEdit.Show(FDockPanel);

          
        }
		

		protected virtual SD.Document.IHighlightingStrategy GetHighlightingStrategy()
		{
			switch (FDesignerID)
			{
				case "XML" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("XML");
				case "CS" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("CS");
				case "VB" : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("VBNET");
				default : return SD.Document.HighlightingStrategyFactory.CreateHighlightingStrategy("Default");
			}
		}

		private void TextEditInitialized(object ASender, ICSharpCode.TextEditor.TextAreaControl ANewControl)
		{
			ANewControl.TextArea.Caret.PositionChanged += new EventHandler(CaretPositionChanged);
			ANewControl.TextArea.Enter += new EventHandler(TextAreaEnter);
		}

		// Service

		private IDesignService FService;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return FService; }
		}

		// Dataphoria

		protected IDataphoria Dataphoria
		{
			get { return (FService == null ? null : FService.Dataphoria); }
		}

		// DataSession

		protected DAE.Client.DataSession DataSession
		{
			get { return FService.Dataphoria.DataSession; }
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

		private string FDesignerID;
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

		// EditorText

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string EditorText
		{
			get { return FTextEdit.Document.TextContent; }
			set { FTextEdit.SetText(value); }
		}

		// Title

		private void UpdateTitle()
		{
			Text = FService.GetDescription() + (FService.IsModified ? "*" : "");
		}

		// Service callbacks

		private void NameOrModifiedChanged(object ASender, EventArgs AArgs)
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

		// Status bar

		private void TextAreaEnter(object sender, EventArgs e)
		{
			UpdateLineNumberStatus();
		}

		private void CaretPositionChanged(object sender, EventArgs e)
		{
			UpdateLineNumberStatus();
		}

		private void UpdateLineNumberStatus()
		{
			FPositionStatus.Text = FTextEdit.GetLineNumberText();
		}

		// Form events

		public void DocumentChanged(object ASender, SD.Document.DocumentEventArgs AArgs) 
		{
			FService.SetModified(true);
			SetStatus(String.Empty);
		}

		private void Print()
		{
			using (PrintDialog LDialog = new PrintDialog()) 
			{
				LDialog.Document  = FTextEdit.PrintDocument;
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

		protected void BeginningFind(object sender, EventArgs e)
		{
			SetStatus(String.Empty);
		}

		protected void ReplacementsPerformed(object ASender, int ACount)
		{
			SetStatus(String.Format(Strings.ReplacementsPerformed, ACount));
		}

		protected void TextNotFound(object sender, EventArgs e)
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

		protected override void OnActivated(EventArgs e)
		{
			ActiveControl = FTextEdit;  // always focus on the editor control
			base.OnActivated (e);
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

		private void FrameBarManagerItemClicked(object ASender, Syncfusion.Windows.Forms.Tools.XPMenus.BarItemClickedEventArgs AArgs)
		{
			try
			{
				switch (AArgs.ClickedBarItem.ID)
				{
					case "Close" : Close(); break;
					case "Print" : Print(); break;
					case "Cut" : new SD.Actions.Cut().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "Copy" : new SD.Actions.Copy().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "Paste" : new SD.Actions.Paste().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "SelectAll" : new SD.Actions.SelectWholeDocument().Execute(FTextEdit.ActiveTextAreaControl.TextArea); break;
					case "SaveAsDocument" : 			
						FService.SaveAsDocument();
						ShowSaved();
						break;
					case "SaveAsFile" :
						FService.SaveAsFile();
						ShowSaved();
						break;
					case "Save" :
						FService.Save();
						ShowSaved();
						break;
					case "Find" : Find(); break;
					case "Replace" : Replace(); break;
					case "FindNext" : FindNext(); break;
					case "Undo" : Undo(); break;
					case "Redo" : Redo(); break;
					case "Split" : Split(); break;
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

		#region StatusBar

		protected Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel FPositionStatus;

		protected override void InitializeStatusBar()
		{
			base.InitializeStatusBar();

			FPositionStatus = new Syncfusion.Windows.Forms.Tools.StatusBarAdvPanel();
			FPositionStatus.Text = "0:0";
			FPositionStatus.SizeToContent = true;
			FPositionStatus.HAlign = Syncfusion.Windows.Forms.Tools.HorzFlowAlign.Right;
			FPositionStatus.Alignment = HorizontalAlignment.Center;
			FPositionStatus.BorderStyle = BorderStyle.FixedSingle;
			FPositionStatus.BorderColor = Color.Gray;
		}

		protected override void DisposeStatusBar()
		{
			FPositionStatus.Dispose();
			FPositionStatus = null;

			base.DisposeStatusBar();
		}

		public override void Merge(Control AStatusBar)
		{
			base.Merge(AStatusBar);

			AStatusBar.Controls.Add(FPositionStatus);
		}

		public override void Unmerge(Control AStatusBar)
		{
			base.Unmerge(AStatusBar);

			AStatusBar.Controls.Remove(FPositionStatus);
		}

		#endregion

        #region IChildFormWithToolBar Members

        public void MergeWith(ToolStrip AParentToolStrip)
        {
            ToolStripManager.Merge(this.FToolStrip, AParentToolStrip);
        }

        #endregion
    }
}
