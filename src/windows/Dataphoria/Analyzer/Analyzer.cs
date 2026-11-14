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

	public class Analyzer : BaseForm, Alphora.Dataphor.Dataphoria.Designers.IDesigner, IToolBarClient
	{
		private AnalyzerControl _analyzerControl;
		private MenuStrip MenuStip;
		private ToolStrip _toolStrip;
		private ToolStripButton toolStripButton1;
		private ToolStripButton _expandOnDemand;

		public Analyzer()	// dummy constructor for MDI menu merging?
		{
			InitializeComponent();
		}

		public Analyzer(IDataphoria dataphoria, string designerID)
		{
			InitializeComponent();

			_designerID = designerID;

			InitializeService(dataphoria);

			SetExpandOnDemand((bool)dataphoria.Settings.GetSetting("Analyzer.ExpandOnDemand", typeof(bool), true));
		}

		protected override void Dispose(bool disposed)
		{
			if (!IsDisposed && (Dataphoria != null))
			{
				try
				{
					Dataphoria.Settings.SetSetting("Analyzer.ExpandOnDemand", _analyzerControl.MainSurface.ExpandOnDemand);
				}
				finally
				{
					base.Dispose(disposed);
				}
			}
		}

		// Dataphoria

		[Browsable(false)]
		public IDataphoria Dataphoria
		{
			get { return (_service == null ? null : _service.Dataphoria); }
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
			this._expandOnDemand = new System.Windows.Forms.ToolStripButton();
			this._analyzerControl = new Alphora.Dataphor.Dataphoria.Analyzer.AnalyzerControl();
			this.MenuStip = new System.Windows.Forms.MenuStrip();
			this._toolStrip = new System.Windows.Forms.ToolStrip();
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
			this._toolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// FBottomToolStripPanel
			// 
			this.FBottomToolStripPanel.Location = new System.Drawing.Point(0, 496);
			this.FBottomToolStripPanel.Size = new System.Drawing.Size(687, 22);
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            saveToolStripMenuItem,
            saveAsFileToolStripMenuItem,
            saveAsDocumentToolStripMenuItem,
            closeToolStripMenuItem});
			toolStripMenuItem1.MergeAction = System.Windows.Forms.MergeAction.MatchOnly;
			toolStripMenuItem1.MergeIndex = 10;
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
			toolStripMenuItem1.Text = "&File";
			// 
			// saveToolStripMenuItem
			// 
			saveToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.Save;
			saveToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			saveToolStripMenuItem.MergeIndex = 90;
			saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			saveToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
			saveToolStripMenuItem.Text = "&Save";
			saveToolStripMenuItem.Click += new System.EventHandler(this.SaveClicked);
			// 
			// saveAsFileToolStripMenuItem
			// 
			saveAsFileToolStripMenuItem.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.SaveFile;
			saveAsFileToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			saveAsFileToolStripMenuItem.MergeIndex = 90;
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
			saveAsDocumentToolStripMenuItem.MergeAction = System.Windows.Forms.MergeAction.Insert;
			saveAsDocumentToolStripMenuItem.MergeIndex = 90;
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
			closeToolStripMenuItem.MergeIndex = 120;
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
			viewToolStripMenuItem.MergeIndex = 20;
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
			this._expandOnDemand.Checked = true;
			this._expandOnDemand.CheckState = System.Windows.Forms.CheckState.Checked;
			this._expandOnDemand.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._expandOnDemand.Image = global::Alphora.Dataphor.Dataphoria.MenuImages.ExpandOnDemand;
			this._expandOnDemand.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._expandOnDemand.Name = "FExpandOnDemand";
			this._expandOnDemand.Size = new System.Drawing.Size(23, 22);
			this._expandOnDemand.Text = "Expand On &Demand";
			this._expandOnDemand.Click += new System.EventHandler(this.ExpandOnDemandClicked);
			// 
			// FAnalyzerControl
			// 
			this._analyzerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this._analyzerControl.Location = new System.Drawing.Point(0, 0);
			this._analyzerControl.Name = "FAnalyzerControl";
			this._analyzerControl.Size = new System.Drawing.Size(687, 518);
			this._analyzerControl.TabIndex = 4;
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
			this.MenuStip.Visible = false;
			// 
			// FToolStrip
			// 
			this._toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            toolStripButton2,
            toolStripButton3,
            toolStripButton4,
            toolStripButton5,
            this._expandOnDemand});
			this._toolStrip.Location = new System.Drawing.Point(0, 0);
			this._toolStrip.Name = "FToolStrip";
			this._toolStrip.Size = new System.Drawing.Size(687, 25);
			this._toolStrip.TabIndex = 10;
			this._toolStrip.Visible = false;
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
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(687, 518);
			this.Controls.Add(this._toolStrip);
			this.Controls.Add(this._analyzerControl);
			this.Controls.Add(this.MenuStip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.MenuStip;
			this.Name = "Analyzer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Controls.SetChildIndex(this.MenuStip, 0);
			this.Controls.SetChildIndex(this._analyzerControl, 0);
			this.Controls.SetChildIndex(this._toolStrip, 0);
			this.Controls.SetChildIndex(this.FBottomToolStripPanel, 0);
			this.MenuStip.ResumeLayout(false);
			this.MenuStip.PerformLayout();
			this._toolStrip.ResumeLayout(false);
			this._toolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		protected override void OnClosing(System.ComponentModel.CancelEventArgs args) 
		{
			base.OnClosing(args);
			try
			{
				_service.CheckModified();
			}
			catch
			{
				args.Cancel = true;
				throw;
			}
		}

		public virtual void MergeToolbarWith(ToolStrip parentToolStrip)
		{
			ToolStripManager.Merge(_toolStrip, parentToolStrip);
		}

		#region Service

		public void InitializeService(IDataphoria dataphoria)
		{
			_service = new DesignService(dataphoria, this);
			_service.OnModifiedChanged += new EventHandler(NameOrModifiedChanged);
			_service.OnNameChanged += new EventHandler(NameOrModifiedChanged);
			_service.OnRequestLoad += new RequestHandler(RequestLoad);
			_service.OnRequestSave += new RequestHandler(RequestSave);
		}

		private IDesignService _service;
		[Browsable(false)]
		public IDesignService Service
		{
			get { return _service; }
		}

		private void NameOrModifiedChanged(object sender, EventArgs args)
		{
			UpdateTitle();
		}

		protected virtual void RequestLoad(DesignService service, DesignBuffer buffer)
		{
			LoadPlan(buffer.LoadData());
		}

		protected virtual void RequestSave(DesignService service, DesignBuffer buffer)
		{
			buffer.SaveData(SavePlan());
		}

		#endregion

		#region IDesigner, New, Loading, Saving

		private string _designerID;
		[Browsable(false)]
		public string DesignerID
		{
			get { return _designerID; }
		}

		public void Open(DesignBuffer buffer)
		{
			_service.Open(buffer);
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
			_analyzerControl.Clear();
		}

		public virtual void LoadPlan(string plan)
		{
			_analyzerControl.Load(plan);
		}

		public virtual string SavePlan()
		{
			return _analyzerControl.Save();
		}

		public bool CloseSafely()
		{
			Close();
			return IsDisposed;
		}

		public void Save()
		{
			_service.Save();
		}

		public void SaveAsFile()
		{
			_service.SaveAsFile();
		}

		public void SaveAsDocument()
		{
			_service.SaveAsDocument();
		}

		protected void InternalNew(IHost host, bool owner)
		{
			_service.SetBuffer(null);
			_service.SetModified(false);
		}

		public void New(IHost host)
		{
			InternalNew(host, false);
		}

		private void UpdateTitle()
		{
			Text = _service.GetDescription() + (_service.IsModified ? "*" : "");
		}
		
		#endregion

		#region Commands

		private void ZoomIn()
		{
			_analyzerControl.ZoomIn();
		}

		private void ZoomOut()
		{
			_analyzerControl.ZoomOut();
		}

		private void SetExpandOnDemand(bool tempValue)
		{
			_analyzerControl.MainSurface.ExpandOnDemand = tempValue;
			_expandOnDemand.Checked = tempValue;
		}

		private void ToggleExpandOnDemand()
		{
			SetExpandOnDemand(!_analyzerControl.MainSurface.ExpandOnDemand);
		}

		protected override void OnHelpRequested(HelpEventArgs args)
		{
			base.OnHelpRequested(args);
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
