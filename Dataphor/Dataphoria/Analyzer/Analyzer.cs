/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;

using Alphora.Dataphor.Dataphoria.Designers;
using Alphora.Dataphor.Dataphoria.Visual;
using Alphora.Dataphor.Frontend.Client;
using Alphora.Dataphor.Frontend.Client.Windows;
using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Dataphoria.Analyzer
{
	// Don't put any definitions above the Analyzer class

	public class Analyzer : BaseForm, Alphora.Dataphor.Dataphoria.Designers.IDesigner
	{
		private AnalyzerControl FAnalyzerControl;
		private MenuStrip MenuStip;
		private ToolStrip ToolStrip;
		private ToolStripButton toolStripButton1;
		private ToolStripButton FExpandOnDemand;

		public Analyzer()	// dummy constructor for MDI menu merging?
		{
			InitializeComponent();
		}

		public Analyzer(IDataphoria ADataphoria, string ADesignerID)
		{
			InitializeComponent();

			FDesignerID = ADesignerID;

			InitializeService(ADataphoria);

			SetExpandOnDemand((bool)ADataphoria.Settings.GetSetting("Analyzer.ExpandOnDemand", typeof(bool), true));
		}

		protected override void Dispose(bool ADisposed)
		{
			if (!IsDisposed && (Dataphoria != null))
			{
				try
				{
					Dataphoria.Settings.SetSetting("Analyzer.ExpandOnDemand", FAnalyzerControl.MainSurface.ExpandOnDemand);
				}
				finally
				{
					base.Dispose(ADisposed);
				}
			}
		}

		// Dataphoria

		[Browsable(false)]
		public IDataphoria Dataphoria
		{
			get { return (FService == null ? null : FService.Dataphoria); }
		}

		#region Windows Form Designer generated code
		/// <summary>
		///		Required method for Designer support - do not modify
		///		the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
			System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem saveAsFileToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem saveAsDocumentToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem zoomInToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem zoomOutToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem expandOnDemandToolStripMenuItem;
			System.Windows.Forms.ToolStripButton toolStripButton2;
			System.Windows.Forms.ToolStripButton toolStripButton3;
			System.Windows.Forms.ToolStripButton toolStripButton4;
			System.Windows.Forms.ToolStripButton toolStripButton5;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Analyzer));
			this.FExpandOnDemand = new System.Windows.Forms.ToolStripButton();
			this.FAnalyzerControl = new Alphora.Dataphor.Dataphoria.Analyzer.AnalyzerControl();
			this.MenuStip = new System.Windows.Forms.MenuStrip();
			this.ToolStrip = new System.Windows.Forms.ToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			saveAsFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			saveAsDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			zoomInToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			zoomOutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			expandOnDemandToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			toolStripButton4 = new System.Windows.Forms.ToolStripButton();
			toolStripButton5 = new System.Windows.Forms.ToolStripButton();
			this.MenuStip.SuspendLayout();
			this.ToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            saveToolStripMenuItem,
            saveAsFileToolStripMenuItem,
            saveAsDocumentToolStripMenuItem,
            closeToolStripMenuItem});
			toolStripMenuItem1.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			toolStripMenuItem1.MergeIndex = 1;
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
			toolStripMenuItem1.Text = "&File";
			// 
			// saveToolStripMenuItem
			// 
			saveToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			saveToolStripMenuItem.MergeIndex = 20;
			saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			saveToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			saveToolStripMenuItem.Text = "&Save";
			saveToolStripMenuItem.Click += new System.EventHandler(this.SaveClicked);
			// 
			// saveAsFileToolStripMenuItem
			// 
			saveAsFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			saveAsFileToolStripMenuItem.MergeIndex = 20;
			saveAsFileToolStripMenuItem.Name = "saveAsFileToolStripMenuItem";
			saveAsFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.F)));
			saveAsFileToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			saveAsFileToolStripMenuItem.Text = "Save As File...";
			saveAsFileToolStripMenuItem.Click += new System.EventHandler(this.SaveAsFileClicked);
			// 
			// saveAsDocumentToolStripMenuItem
			// 
			saveAsDocumentToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			saveAsDocumentToolStripMenuItem.MergeIndex = 20;
			saveAsDocumentToolStripMenuItem.Name = "saveAsDocumentToolStripMenuItem";
			saveAsDocumentToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
						| System.Windows.Forms.Keys.D)));
			saveAsDocumentToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			saveAsDocumentToolStripMenuItem.Text = "Save As &Document...";
			saveAsDocumentToolStripMenuItem.Click += new System.EventHandler(this.SaveAsDocumentClicked);
			// 
			// closeToolStripMenuItem
			// 
			closeToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Close;
			closeToolStripMenuItem.MergeIndex = 20;
			closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			closeToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F4)));
			closeToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			closeToolStripMenuItem.Text = "&Close";
			closeToolStripMenuItem.Click += new System.EventHandler(this.CloseClicked);
			// 
			// viewToolStripMenuItem
			// 
			viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            zoomInToolStripMenuItem,
            zoomOutToolStripMenuItem,
            expandOnDemandToolStripMenuItem});
			viewToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			viewToolStripMenuItem.MergeIndex = 10;
			viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			viewToolStripMenuItem.Text = "&View";
			// 
			// zoomInToolStripMenuItem
			// 
			zoomInToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Zoom_In;
			zoomInToolStripMenuItem.Name = "zoomInToolStripMenuItem";
			zoomInToolStripMenuItem.ShortcutKeyDisplayString = "+";
			zoomInToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
			zoomInToolStripMenuItem.Text = "Zoom &In";
			zoomInToolStripMenuItem.Click += new System.EventHandler(this.ZoomInClicked);
			// 
			// zoomOutToolStripMenuItem
			// 
			zoomOutToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Zoom_Out;
			zoomOutToolStripMenuItem.Name = "zoomOutToolStripMenuItem";
			zoomOutToolStripMenuItem.ShortcutKeyDisplayString = "-";
			zoomOutToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
			zoomOutToolStripMenuItem.Text = "Zoom &Out";
			zoomOutToolStripMenuItem.Click += new System.EventHandler(this.ZoomOutClicked);
			// 
			// expandOnDemandToolStripMenuItem
			// 
			expandOnDemandToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.ExpandOnDemand;
			expandOnDemandToolStripMenuItem.Name = "expandOnDemandToolStripMenuItem";
			expandOnDemandToolStripMenuItem.Size = new System.Drawing.Size(179, 22);
			expandOnDemandToolStripMenuItem.Text = "&Expand On Demand";
			expandOnDemandToolStripMenuItem.Click += new System.EventHandler(this.ExpandOnDemandClicked);
			// 
			// toolStripButton2
			// 
			toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton2.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton2.Name = "toolStripButton2";
			toolStripButton2.Size = new System.Drawing.Size(23, 22);
			toolStripButton2.Text = "Save As &File";
			toolStripButton2.ToolTipText = "Save As File...";
			toolStripButton2.Click += new System.EventHandler(this.SaveAsFileClicked);
			// 
			// toolStripButton3
			// 
			toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton3.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveDocument;
			toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton3.Name = "toolStripButton3";
			toolStripButton3.Size = new System.Drawing.Size(23, 22);
			toolStripButton3.Text = "Save As &Document";
			toolStripButton3.ToolTipText = "Save As Document...";
			toolStripButton3.Click += new System.EventHandler(this.SaveAsDocumentClicked);
			// 
			// toolStripButton4
			// 
			toolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton4.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Zoom_In;
			toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton4.Name = "toolStripButton4";
			toolStripButton4.Size = new System.Drawing.Size(23, 22);
			toolStripButton4.Text = "Zoom &In";
			toolStripButton4.Click += new System.EventHandler(this.ZoomInClicked);
			// 
			// toolStripButton5
			// 
			toolStripButton5.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton5.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Zoom_Out;
			toolStripButton5.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton5.Name = "toolStripButton5";
			toolStripButton5.Size = new System.Drawing.Size(23, 22);
			toolStripButton5.Text = "Zoom &Out";
			toolStripButton5.Click += new System.EventHandler(this.ZoomOutClicked);
			// 
			// FExpandOnDemand
			// 
			this.FExpandOnDemand.Checked = true;
			this.FExpandOnDemand.CheckState = System.Windows.Forms.CheckState.Checked;
			this.FExpandOnDemand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.FExpandOnDemand.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.ExpandOnDemand;
			this.FExpandOnDemand.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.FExpandOnDemand.Name = "FExpandOnDemand";
			this.FExpandOnDemand.Size = new System.Drawing.Size(23, 22);
			this.FExpandOnDemand.Text = "Expand On &Demand";
			this.FExpandOnDemand.Click += new System.EventHandler(this.ExpandOnDemandClicked);
			// 
			// FAnalyzerControl
			// 
			this.FAnalyzerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FAnalyzerControl.Location = new System.Drawing.Point(0, 24);
			this.FAnalyzerControl.Name = "FAnalyzerControl";
			this.FAnalyzerControl.Size = new System.Drawing.Size(687, 494);
			this.FAnalyzerControl.TabIndex = 4;
			// 
			// MenuStip
			// 
			this.MenuStip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            toolStripMenuItem1,
            viewToolStripMenuItem});
			this.MenuStip.Location = new System.Drawing.Point(0, 0);
			this.MenuStip.Name = "MenuStip";
			this.MenuStip.Size = new System.Drawing.Size(687, 24);
			this.MenuStip.TabIndex = 9;
			// 
			// ToolStrip
			// 
			this.ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            toolStripButton2,
            toolStripButton3,
            toolStripButton4,
            toolStripButton5,
            this.FExpandOnDemand});
			this.ToolStrip.Location = new System.Drawing.Point(0, 24);
			this.ToolStrip.Name = "ToolStrip";
			this.ToolStrip.Size = new System.Drawing.Size(687, 25);
			this.ToolStrip.TabIndex = 10;
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton1.Text = "&Save";
			this.toolStripButton1.ToolTipText = "Save";
			this.toolStripButton1.Click += new System.EventHandler(this.SaveClicked);
			// 
			// Analyzer
			// 
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(687, 518);
			this.Controls.Add(this.ToolStrip);
			this.Controls.Add(this.FAnalyzerControl);
			this.Controls.Add(this.MenuStip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.MenuStip;
			this.Name = "Analyzer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.MenuStip.ResumeLayout(false);
			this.MenuStip.PerformLayout();
			this.ToolStrip.ResumeLayout(false);
			this.ToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		protected override void OnClosing(System.ComponentModel.CancelEventArgs AArgs) 
		{
			base.OnClosing(AArgs);
			try
			{
				FService.CheckModified();
			}
			catch
			{
				AArgs.Cancel = true;
				throw;
			}
		}

		#region Service

		public void InitializeService(IDataphoria ADataphoria)
		{
			FService = new DesignService(ADataphoria, this);
			FService.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnNameChanged += new EventHandler(NameOrModifiedChanged);
			FService.OnRequestLoad += new RequestHandler(RequestLoad);
			FService.OnRequestSave += new RequestHandler(RequestSave);
		}

		private IDesignService FService;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return FService; }
		}

		private void NameOrModifiedChanged(object ASender, EventArgs AArgs)
		{
			UpdateTitle();
		}

		protected virtual void RequestLoad(DesignService AService, DesignBuffer ABuffer)
		{
			LoadPlan(ABuffer.LoadData());
		}

		protected virtual void RequestSave(DesignService AService, DesignBuffer ABuffer)
		{
			ABuffer.SaveData(SavePlan());
		}

		#endregion

		#region IDesigner, New, Loading, Saving

		private string FDesignerID;
		[Browsable(false)]
		public string DesignerID
		{
			get { return FDesignerID; }
		}

		public void Open(DesignBuffer ABuffer)
		{
			FService.Open(ABuffer);
		}

		/// <remarks> 
		///		Note that this method should not be confused with Form.Close().  
		///		Be sure to deal with a compile-time instance of type IDesigner 
		///		to invoke this method. 
		///	</remarks>
		void Dataphor.Dataphoria.Designers.IDesigner.Show()
		{
			UpdateTitle();
			Dataphoria.AttachForm(this);
		}

		public virtual void New()
		{
			FAnalyzerControl.Clear();
		}

		public virtual void LoadPlan(string APlan)
		{
			FAnalyzerControl.Load(APlan);
		}

		public virtual string SavePlan()
		{
			return FAnalyzerControl.Save();
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		public void Save()
		{
			FService.Save();
		}

		public void SaveAsFile()
		{
			FService.SaveAsFile();
		}

		public void SaveAsDocument()
		{
			FService.SaveAsDocument();
		}

		protected void InternalNew(IHost AHost, bool AOwner)
		{
			FService.SetBuffer(null);
			FService.SetModified(false);
		}

		public void New(IHost AHost)
		{
			InternalNew(AHost, false);
		}

		private void UpdateTitle()
		{
			Text = FService.GetDescription() + (FService.IsModified ? "*" : "");
		}
		
		#endregion

		#region Commands

		private void ZoomIn()
		{
			FAnalyzerControl.ZoomIn();
		}

		private void ZoomOut()
		{
			FAnalyzerControl.ZoomOut();
		}

		private void SetExpandOnDemand(bool AValue)
		{
			FAnalyzerControl.MainSurface.ExpandOnDemand = AValue;
			FExpandOnDemand.Checked = AValue;
		}

		private void ToggleExpandOnDemand()
		{
			SetExpandOnDemand(!FAnalyzerControl.MainSurface.ExpandOnDemand);
		}

		protected override void OnHelpRequested(HelpEventArgs AArgs)
		{
			base.OnHelpRequested(AArgs);
			Dataphoria.InvokeHelp("Analyzer");
			
		}

		private void SaveClicked(object sender, EventArgs e)
		{
			Save();
		}

		private void SaveAsFileClicked(object sender, EventArgs e)
		{
			SaveAsFile();
		}

		private void SaveAsDocumentClicked(object sender, EventArgs e)
		{
			SaveAsDocument();
		}

		private void CloseClicked(object sender, EventArgs e)
		{
			Close();
		}

		private void ZoomInClicked(object sender, EventArgs e)
		{
			ZoomIn();
		}

		private void ZoomOutClicked(object sender, EventArgs e)
		{
			ZoomOut();
		}

		private void ExpandOnDemandClicked(object sender, EventArgs e)
		{
			ToggleExpandOnDemand();
		}

		#endregion
	}
}
